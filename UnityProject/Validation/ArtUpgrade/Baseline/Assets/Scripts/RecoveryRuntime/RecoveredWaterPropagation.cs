using System;
using System.Collections.Generic;

namespace Recovered.FlowWater
{
    // Input-trigger state and scaled-time output callbacks. Geometry and the
    // original controller delay fields are supplied by RecoveredWaterNetwork.
    public sealed class RecoveredWaterPropagation
    {
        public sealed class Channel
        {
            public string key;
            public int signature, expectedColor, sourceColor, sourceIncoming;
            public int inputMask = -1;
            public bool source;
            public float sourceDelay, createdAt, enteredAt;
            public int color, incoming;
            public object tag;
            public string diagnostic;
            public string parentKey, parentLink;
            public readonly List<Link> outputs = new List<Link>();
            internal Channel previous;
            internal int validation;
        }
        public sealed class Link
        {
            public Channel target;
            public string identity;
            public int incoming;
            public Func<int, float> delay;
        }
        sealed class Pending
        {
            public float time;
            public int order;
            public Channel source, target;
            public Link link;
        }
        sealed class Saved
        {
            public readonly Dictionary<string, SavedChannel> channels = new Dictionary<string, SavedChannel>(StringComparer.Ordinal);
        }
        sealed class SavedChannel
        {
            public int signature, color, incoming;
            public bool source;
            public float creationAge, entryAge;
            public string parentKey, parentLink;
        }
        Dictionary<string, Channel> channels = new Dictionary<string, Channel>(StringComparer.Ordinal);
        readonly List<Pending> pending = new List<Pending>();
        readonly Queue<string> diagnostics = new Queue<string>();
        public string[] RebuildDiagnostics { get { return diagnostics.ToArray(); } }
        public string[] PendingDiagnostics(float now)
        {
            var result = new List<string>();
            foreach (Pending item in pending) result.Add("now=" + now + " due=" + item.time + " source=" +
                (item.source == null ? "prime" : item.source.key + " entered=" + item.source.enteredAt) + " target=" + item.target.key +
                " created=" + item.target.createdAt + " " + item.target.diagnostic);
            return result.ToArray();
        }
        void Trace(string message)
        { if (diagnostics.Count >= 128) diagnostics.Dequeue(); diagnostics.Enqueue(message); }
        Action<Channel> enter, exit;
        Action<Channel, Channel> retain;
        int order;
        Saved restore;
        public int PendingCount { get { return pending.Count; } }
        public float NextTime { get { return pending.Count == 0 ? float.PositiveInfinity : pending[0].time; } }
        public float LastScheduledTime
        {
            get { float time = 0; foreach (Pending item in pending) time = Math.Max(time, item.time); return time; }
        }
        public void Reset(Action<Channel> remove = null)
        {
            foreach (Channel channel in channels.Values) if (channel.color != 0 && remove != null) remove(channel);
            channels.Clear(); pending.Clear(); restore = null; diagnostics.Clear();
        }
        public void Rebuild(IList<Channel> next, float now, Action<Channel> onEnter,
            Action<Channel> onExit, Action<Channel, Channel> onRetain)
        {
            enter = onEnter; exit = onExit; retain = onRetain;
            var previous = channels;
            channels = new Dictionary<string, Channel>(StringComparer.Ordinal);
            pending.Clear(); order = 0;
            foreach (Channel channel in next)
            {
                channels[channel.key] = channel;
                channel.createdAt = now;
                Channel old;
                SavedChannel saved;
                if (restore != null && restore.channels.TryGetValue(channel.key, out saved) && saved.signature == channel.signature)
                {
                    channel.createdAt = now - saved.creationAge; channel.enteredAt = now - saved.entryAge;
                    channel.color = saved.color; channel.incoming = saved.incoming;
                    channel.parentKey = saved.parentKey; channel.parentLink = saved.parentLink;
                }
                else if (restore == null && previous.TryGetValue(channel.key, out old) && old.signature == channel.signature)
                {
                    channel.previous = old; channel.createdAt = old.createdAt; channel.enteredAt = old.enteredAt;
                    channel.color = old.color; channel.incoming = old.incoming;
                    channel.parentKey = old.parentKey; channel.parentLink = old.parentLink;
                }
                else if (restore == null && previous.TryGetValue(channel.key, out old))
                    Trace("time=" + now + " signature-change key=" + channel.key + " old=" + old.diagnostic + " current=" + channel.diagnostic);
            }
            foreach (Channel channel in next) Validate(channel);
            var retained = new HashSet<Channel>();
            foreach (Channel channel in next)
            {
                if (channel.color == 0) continue;
                if (channel.previous != null)
                {
                    retained.Add(channel.previous);
                    if (retain != null) retain(channel.previous, channel);
                }
                else if (enter != null) enter(channel);
            }
            foreach (Channel old in previous.Values)
                if (old.color != 0 && !retained.Contains(old) && exit != null) exit(old);
            restore = null;
            foreach (Channel channel in next)
            {
                if (channel.color != 0) ScheduleOutputs(channel, now);
                else if (channel.source)
                    Push(new Pending { target = channel, time = Math.Max(now, channel.createdAt + channel.sourceDelay), order = order++ });
            }
            Advance(now);
        }
        bool Validate(Channel channel)
        {
            if (channel.validation != 0) return channel.validation == 2;
            channel.validation = 1;
            bool valid = channel.color != 0 && Accepts(channel, channel.incoming) && (channel.expectedColor == 0 || channel.expectedColor == channel.color);
            if (valid && channel.source)
                valid = channel.color == channel.sourceColor && channel.incoming == channel.sourceIncoming;
            else if (valid)
            {
                Channel parent;
                valid = channel.parentKey != null && channels.TryGetValue(channel.parentKey, out parent) &&
                    Validate(parent) && parent.color == channel.color && HasDelivery(parent, channel);
            }
            if (!valid) { channel.color = 0; channel.incoming = 0; channel.parentKey = channel.parentLink = null; }
            channel.validation = valid ? 2 : 3;
            return valid;
        }
        static bool Accepts(Channel channel, int incoming)
        { return incoming == 0 || incoming >= 1 && incoming <= 6 && (channel.inputMask & (1 << (incoming - 1))) != 0; }

        static bool HasDelivery(Channel parent, Channel target)
        {
            foreach (Link link in parent.outputs)
                if (link.target == target && link.identity == target.parentLink && link.incoming == target.incoming) return true;
            return false;
        }
        void ScheduleOutputs(Channel channel, float now)
        {
            foreach (Link link in channel.outputs)
            {
                Channel target = link.target;
                if (target.color != 0 || target.source || !Accepts(target, link.incoming) || target.expectedColor != 0 && target.expectedColor != channel.color) continue;
                float delay = link.delay == null ? 0 : link.delay(channel.incoming);
                if (float.IsPositiveInfinity(delay)) continue;
                Push(new Pending { source = channel, target = target, link = link,
                    time = Math.Max(now, channel.enteredAt + Math.Max(0, delay)), order = order++ });
            }
        }
        public bool Advance(float now)
        {
            bool changed = false;
            while (pending.Count != 0 && pending[0].time <= now)
            {
                Pending item = Pop(); Channel target = item.target;
                if (target.color != 0) continue;
                int color = item.source == null ? target.sourceColor : item.source.color;
                if (color == 0 || !Accepts(target, item.link == null ? target.sourceIncoming : item.link.incoming) || target.expectedColor != 0 && target.expectedColor != color) continue;
                target.color = color; target.incoming = item.link == null ? target.sourceIncoming : item.link.incoming;
                target.enteredAt = now; target.validation = 2;
                target.parentKey = item.source == null ? null : item.source.key;
                target.parentLink = item.link == null ? null : item.link.identity;
                if (enter != null) enter(target);
                changed = true; ScheduleOutputs(target, now);
            }
            return changed;
        }
        void Push(Pending item)
        {
            int index = pending.Count; pending.Add(item);
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (!Before(item, pending[parent])) break;
                pending[index] = pending[parent]; index = parent;
            }
            pending[index] = item;
        }
        static bool Before(Pending a, Pending b) { return a.time < b.time || a.time == b.time && a.order < b.order; }
        Pending Pop()
        {
            Pending result = pending[0], last = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            if (pending.Count == 0) return result;
            int index = 0;
            while (index * 2 + 1 < pending.Count)
            {
                int child = index * 2 + 1;
                if (child + 1 < pending.Count && Before(pending[child + 1], pending[child])) child++;
                if (!Before(pending[child], last)) break;
                pending[index] = pending[child]; index = child;
            }
            pending[index] = last; return result;
        }
        public object CaptureState(float now)
        {
            var result = new Saved();
            foreach (Channel channel in channels.Values)
                result.channels[channel.key] = new SavedChannel { signature = channel.signature, color = channel.color,
                    incoming = channel.incoming, source = channel.source, creationAge = now - channel.createdAt,
                    entryAge = channel.color == 0 ? 0 : now - channel.enteredAt,
                    parentKey = channel.parentKey, parentLink = channel.parentLink };
            return result;
        }
        public void RestoreState(object state) { restore = state as Saved; }
    }
}
