using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Recovered.FlowWater
{
    /// <summary>Actual native category controllers with retained, timed input-trigger state.</summary>
    public sealed class RecoveredPreviewWater : MonoBehaviour
    {
        const string Generated = "__Recovered preview water";
        sealed class Channel
        {
            public MonoBehaviour owner;
            public GameObject prototype, water;
            public Transform piece;
            public bool source, pedestal;
            public int expected, color, incoming, visualSignature;
            public float priming;
            public string key;
            public readonly bool[] ports = new bool[6];
            public int[] directions;
            public readonly Channel[] links = new Channel[6];
            public readonly Dictionary<int, List<RecoveredWaterJets.Route>> routes = new Dictionary<int, List<RecoveredWaterJets.Route>>();
            public List<RecoveredWaterJets.Route> jets = new List<RecoveredWaterJets.Route>();
            public Vector3 point;
            public RecoveredWaterPropagation.Channel arrival;
        }
        readonly List<GameObject> effects = new List<GameObject>();
        readonly List<Channel> channels = new List<Channel>();
        readonly Dictionary<Renderer, bool> originalRenderers = new Dictionary<Renderer, bool>();
        readonly RecoveredWaterPropagation propagation = new RecoveredWaterPropagation();
        static readonly Vector3[] Direction = { Vector3.left, Vector3.forward, Vector3.right, Vector3.back, Vector3.up, Vector3.down };
        float nextCheck, ground;
        int previousSignature;
        bool initialized;
        public int WetControllerCount { get; private set; }
        public int SourceCount { get; private set; }
        public int OriginalEffectCount { get; private set; }
        public int PendingWaterEventCount { get { return propagation.PendingCount; } }
        public bool HasPendingPropagation { get { return propagation.PendingCount != 0; } }
        public static RecoveredPreviewWater Attach(GameObject root)
        {
            if (root == null) return null;
            RecoveredPreviewWater driver = root.GetComponent<RecoveredPreviewWater>();
            return driver == null ? root.AddComponent<RecoveredPreviewWater>() : driver;
        }
        void LateUpdate()
        {
            if (RecoveredWaterNetwork.Instance == null) return;
            if (propagation.Advance(Time.time)) RefreshCounts();
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + .2f;
            int signature = (17 * 31 + UIManagerScript.confColor1) * 31 + UIManagerScript.confColor2;
            unchecked { signature = signature * 31 + UIManagerSelScript0.estadoResorte1; }
            foreach (MonoBehaviour component in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (!Relevant(component) || IsGenerated(component.transform)) continue;
                unchecked
                {
                    signature = signature * 31 + component.gameObject.activeInHierarchy.GetHashCode();
                    signature = signature * 31 + transform.InverseTransformPoint(component.transform.position).GetHashCode();
                    signature = signature * 31 + (Quaternion.Inverse(transform.rotation) * component.transform.rotation).GetHashCode();
                    signature = signature * 31 + ConfigurationSignature(component);
                }
            }
            if (initialized && signature == previousSignature) return;
            initialized = true; previousSignature = signature; Rebuild();
        }
        static bool Relevant(MonoBehaviour component)
        {
            return component is AguaControl || component is AguaControlC || component is AguaControlPFuente ||
                component is AguasControl || component is AguasControl3 || component is AguaControlFuente ||
                component is ControlPiezaFuente || component is activaEsfera || component is FuenteTest3;
        }
        bool IsGenerated(Transform target)
        {
            for (Transform current = target; current != null && current != transform; current = current.parent)
                if (current.name.StartsWith(Generated, StringComparison.Ordinal)) return true;
            return false;
        }
        string StableKey(MonoBehaviour owner)
        {
            string path = "";
            for (Transform current = owner.transform; current != null && current != transform; current = current.parent)
                path = "/" + current.GetSiblingIndex() + path;
            return path + ":" + Array.IndexOf(owner.GetComponents<MonoBehaviour>(), owner);
        }
        static int ConfigurationSignature(MonoBehaviour component)
        {
            unchecked
            {
                int hash = Number(component, "configFuente");
                foreach (string field in new[] { "configFuenteB", "configFuenteCentro", "tipoPieza", "colorCubeta", "color" }) hash = hash * 31 + Number(component, field);
                foreach (string field in new[] { "confColor", "configPit1", "configPit2", "configPit3", "configPit4", "configRes1", "configRes2", "configRes3", "configRes4", "configAnim", "configLi" })
                    hash = hash * 31 + (Text(component, field) ?? "").GetHashCode();
                return hash;
            }
        }
        void Rebuild()
        {
            channels.Clear(); SourceCount = 0; ground = Single.PositiveInfinity;
            MonoBehaviour[] components = GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour component in components)
            {
                if (component == null || IsGenerated(component.transform)) continue;
                if (component is AguaControlFuente) { HideOriginalWater(component, false); continue; }
                bool wall = component is ControlPiezaFuente;
                bool finished = component is AguasControl || component is AguasControl3;
                Transform waterRoot = component.transform.parent;
                if (finished && (waterRoot == null || waterRoot.GetComponent<AguaControl>() != null ||
                    waterRoot.GetComponent<AguaControlC>() != null || waterRoot.GetComponent<AguaControlPFuente>() != null)) continue;
                if (!(component is AguaControl) && !(component is AguaControlC) && !(component is AguaControlPFuente) && !wall && !finished) continue;
                HideOriginalWater(component, wall, finished ? waterRoot : null);
                if (!component.gameObject.activeInHierarchy) continue;
                var channel = new Channel { owner = component, source = wall, pedestal = component is AguaControlPFuente,
                    prototype = wall ? Reference<GameObject>(component, "prefabAguaControl") : finished ? waterRoot.gameObject : component.gameObject,
                    point = transform.InverseTransformPoint(component.transform.position), expected = wall ? Number(component, "color") : Number(component, "colorCubeta") };
                channel.key = StableKey(component);
                channel.directions = finished ? null : new int[4];
                if (finished)
                {
                    MonoBehaviour origin = FindOrigin(channel, 0);
                    if (origin != null) channel.expected = Number(origin, "colorCubeta");
                }
                bool any = false;
                for (int port = 0; port < 6; port++)
                {
                    channel.ports[port] = Boolean(component, (wall ? "cascada" : finished ? "origen" : "ori") + (port + 1)); any |= channel.ports[port];
                    if (port < 4 && channel.directions != null) channel.directions[port] = Number(component, "dirCentro" + (port + 1));
                }
                if (channel.prototype == null || !any) continue;
                channel.piece = wall ? component.transform : channel.prototype.transform.parent;
                channels.Add(channel); ground = Mathf.Min(ground, channel.point.y);
            }
            foreach (activaEsfera source in GetComponentsInChildren<activaEsfera>(true))
            {
                if (source == null || !source.gameObject.activeInHierarchy || source.esfera1 == null || IsGenerated(source.transform)) continue;
                Channel found = null;
                for (Transform parent = source.transform.parent; parent != null && parent != transform && found == null; parent = parent.parent)
                    foreach (Channel channel in channels)
                        if (channel.owner is AguaControlC && channel.owner.transform.parent == parent) { found = channel; break; }
                if (found == null) continue;
                found.source = true; found.incoming = 5; found.priming = .5f;
                string tag = source.esfera1.tag;
                if (tag.StartsWith("color", StringComparison.Ordinal) && tag.Length == 6) found.expected = tag[5] - '0';
            }
            var states = new List<RecoveredWaterPropagation.Channel>();
            foreach (Channel channel in channels)
            {
                for (int port = 0; port < 4; port++)
                    if (channel.ports[port]) channel.links[port] = Receiver(channel, channel.point + Direction[port] * 20, port, false);
                if (channel.ports[5] && !channel.pedestal) channel.links[5] = Receiver(channel, channel.point, 5, false);
                if (channel.pedestal) channel.links[4] = Receiver(channel, channel.point, 4, true);
                for (int incoming = 1; incoming <= 6; incoming++)
                {
                    MonoBehaviour origin = FindOrigin(channel, incoming);
                    List<RecoveredWaterJets.Route> routes = RecoveredWaterJets.ReadRoutes(origin == null ? channel.owner : origin);
                    foreach (RecoveredWaterJets.Route jet in routes)
                    {
                        Channel receiver = JetReceiver(channel, channel.point + jet.displacement);
                        if (receiver != null) jet.receiver = receiver.owner.transform;
                    }
                    channel.routes[incoming] = routes;
                }
                unchecked
                {
                    int signature = (channel.point.GetHashCode() * 31 + (Quaternion.Inverse(transform.rotation) * channel.owner.transform.rotation).GetHashCode()) * 31 + channel.expected;
                    for (int i = 0; i < 6; i++) signature = signature * 31 + (channel.ports[i] ? 1 : 0);
                    signature = signature * 31 + (channel.source ? 1 : 0);
                    channel.arrival = new RecoveredWaterPropagation.Channel { key = channel.key, signature = signature,
                        expectedColor = channel.expected, source = channel.source, sourceColor = Mathf.Clamp(channel.expected, 1, 4),
                        sourceIncoming = channel.incoming, sourceDelay = channel.priming, tag = channel };
                }
                channel.visualSignature = VisualSignature(channel); states.Add(channel.arrival);
                if (channel.source) SourceCount++;
            }
            foreach (Channel channel in channels)
            {
                for (int port = 0; port < 6; port++)
                {
                    int outlet = port; Channel receiver = channel.links[port];
                    if (receiver == null) continue;
                    int incoming = port == 4 ? 6 : receiver.point.y < channel.point.y - 3 && receiver.ports[4] ? 5 : port < 4 ? (port + 2) % 4 + 1 : 5;
                    AddIntakes(channel, receiver, incoming, "port" + port,
                        intake => intake == outlet + 1 ? Single.PositiveInfinity : DeliveryDelay(channel, outlet, intake));
                }
                foreach (var pair in channel.routes)
                    for (int i = 0; i < pair.Value.Count; i++)
                    {
                        int requiredIntake = pair.Key; RecoveredWaterJets.Route jet = pair.Value[i];
                        if (jet.receiver == null) continue;
                        foreach (Channel receiver in channels) if (receiver.owner.transform == jet.receiver)
                        {
                            AddIntakes(channel, receiver, 5, "jet" + requiredIntake + ":" + i,
                                intake => intake == requiredIntake ? OutletDelay(channel, intake) : Single.PositiveInfinity);
                            break;
                        }
                    }
            }
            propagation.Rebuild(states, Time.time, Enter, Exit, Retain);
            RecoveredNativePalette.ApplyHierarchy(transform, GetComponentInChildren<Resorte2d>(true) != null);
            RefreshCounts();
        }
        int VisualSignature(Channel channel)
        {
            unchecked
            {
                int hash = (ConfigurationSignature(channel.owner) * 31 + UIManagerScript.confColor1) * 31 + UIManagerScript.confColor2;
                foreach (MonoBehaviour component in channel.prototype.GetComponentsInChildren<MonoBehaviour>(true))
                    if (component is FuenteTest3 && !IsGenerated(component.transform)) hash = hash * 31 + ConfigurationSignature(component);
                for (int i = 0; i < 6; i++) hash = hash * 31 + (channel.links[i] == null ? 0 : channel.links[i].point.GetHashCode());
                foreach (var routes in channel.routes.Values) foreach (RecoveredWaterJets.Route route in routes)
                    hash = (hash * 31 + route.displacement.GetHashCode()) * 31 + (route.receiver == null ? 0 : route.receiver.GetInstanceID());
                return hash;
            }
        }
        void AddIntakes(Channel source, Channel receiver, int incoming, string identity, Func<int, float> delay)
        {
            foreach (Channel candidate in channels)
            {
                if (candidate != receiver && (candidate.piece == receiver.piece || candidate.source || !candidate.ports[incoming - 1] ||
                    Mathf.Abs(candidate.point.x - receiver.point.x) > .8001f || Mathf.Abs(candidate.point.z - receiver.point.z) > .8001f || Mathf.Abs(candidate.point.y - receiver.point.y) > .4f)) continue;
                source.arrival.outputs.Add(new RecoveredWaterPropagation.Link { target = candidate.arrival,
                    identity = identity + ":" + candidate.key, incoming = incoming, delay = delay });
            }
        }
        float OutletDelay(Channel channel, int incoming)
        { return RecoveredWaterTiming.Outlet(channel.owner, channel.prototype, incoming, channel.pedestal); }
        float DeliveryDelay(Channel channel, int port, int incoming)
        {
            if (channel.owner is ControlPiezaFuente) return 1;
            float delay = OutletDelay(channel, incoming);
            Channel receiver = channel.links[port];
            if (channel.pedestal && port == 4) return delay;
            if (port == 5 || receiver != null && receiver.point.y < channel.point.y - 3)
                delay += RecoveredWaterTiming.Cascade(channel.point.y - (receiver == null ? ground : receiver.point.y), port == 5);
            else delay += RecoveredWaterTiming.Secondary(channel.owner, incoming, port);
            return delay;
        }
        void Enter(RecoveredWaterPropagation.Channel state)
        {
            Channel channel = (Channel)state.tag;
            channel.color = state.color; channel.incoming = state.incoming;
            List<RecoveredWaterJets.Route> jets;
            channel.jets = channel.routes.TryGetValue(state.incoming, out jets) ? jets : new List<RecoveredWaterJets.Route>();
            var receivers = new MonoBehaviour[6];
            for (int i = 0; i < 6; i++) if (channel.links[i] != null) receivers[i] = channel.links[i].owner;
            GameObject effect = RecoveredWaterNetwork.Instance.RestorePreviewWaterNode(channel.owner, channel.prototype,
                transform, channel.color, channel.incoming, channel.ports, channel.directions, channel.source,
                channel.pedestal, ground, receivers, channel.jets, Mathf.Max(0, Time.time - state.enteredAt));
            if (effect == null) return;
            effect.name = Generated + " · " + channel.owner.GetType().Name; channel.water = effect; effects.Add(effect);
            RecoveredNativePalette.ApplyHierarchy(transform, GetComponentInChildren<Resorte2d>(true) != null);
        }
        void Retain(RecoveredWaterPropagation.Channel oldState, RecoveredWaterPropagation.Channel state)
        {
            Channel old = (Channel)oldState.tag, channel = (Channel)state.tag;
            channel.color = state.color; channel.incoming = state.incoming;
            List<RecoveredWaterJets.Route> jets;
            if (channel.routes.TryGetValue(state.incoming, out jets)) channel.jets = jets;
            if (old.water != null && old.visualSignature == channel.visualSignature) channel.water = old.water;
            else { Exit(oldState); Enter(state); }
        }
        void Exit(RecoveredWaterPropagation.Channel state)
        {
            Channel channel = (Channel)state.tag;
            if (channel.water == null) return;
            effects.Remove(channel.water); channel.water.SetActive(false); Destroy(channel.water); channel.water = null;
        }
        void RefreshCounts()
        {
            WetControllerCount = OriginalEffectCount = 0;
            foreach (Channel channel in channels) if (channel.color != 0 && channel.water != null)
            { WetControllerCount++; OriginalEffectCount += channel.water.GetComponentsInChildren<Renderer>(true).Length; }
        }
        Channel Receiver(Channel source, Vector3 expected, int port, bool upward)
        {
            Channel found = null; float best = upward ? Single.PositiveInfinity : Single.NegativeInfinity;
            foreach (Channel candidate in channels)
            {
                if (candidate == source || candidate.source || Mathf.Abs(candidate.point.x - expected.x) > .8001f || Mathf.Abs(candidate.point.z - expected.z) > .8001f) continue;
                float delta = candidate.point.y - source.point.y;
                bool accepts = upward ? candidate.ports[5] && delta > 3 && delta < 8.1f && candidate.point.y < best :
                    delta <= .45f && candidate.point.y > best && (port == 5 || delta < -3 ? delta < -3 && candidate.ports[4] : candidate.ports[(port + 2) % 4]);
                if (accepts) { best = candidate.point.y; found = candidate; }
            }
            return found;
        }
        Channel JetReceiver(Channel source, Vector3 expected)
        {
            Channel found = null; float height = Single.NegativeInfinity;
            foreach (Channel candidate in channels)
                if (candidate != source && !candidate.source && candidate.ports[4] && Mathf.Abs(candidate.point.x - expected.x) < .8001f &&
                    Mathf.Abs(candidate.point.z - expected.z) < .8001f && candidate.point.y <= source.point.y + 15.7f && candidate.point.y > height)
                    { found = candidate; height = candidate.point.y; }
            return found;
        }
        void HideOriginalWater(MonoBehaviour component, bool source, Transform waterRoot = null)
        {
            if (source) return;
            foreach (Renderer renderer in (waterRoot == null ? component.transform : waterRoot).GetComponentsInChildren<Renderer>(true))
            {
                if (IsGenerated(renderer.transform)) continue;
                if (!originalRenderers.ContainsKey(renderer)) originalRenderers[renderer] = renderer.enabled;
                renderer.enabled = false;
            }
        }
        static MonoBehaviour FindOrigin(Channel channel, int incoming)
        { return RecoveredWaterTiming.Origin(channel.prototype, incoming, channel.pedestal ? "FuenteCubeta2" : "FuenteTest"); }
        static object Value(object target, string field) { FieldInfo found = target.GetType().GetField(field); return found == null ? null : found.GetValue(target); }
        static int Number(object target, string field) { object value = Value(target, field); return value is int ? (int)value : 0; }
        static bool Boolean(object target, string field) { object value = Value(target, field); return value is bool && (bool)value; }
        static string Text(object target, string field) { return Value(target, field) as string; }
        static T Reference<T>(object target, string field) where T : UnityEngine.Object { return Value(target, field) as T; }
        void OnDestroy()
        {
            propagation.Reset(Exit);
            foreach (GameObject effect in effects) if (effect != null) { effect.SetActive(false); Destroy(effect); }
            effects.Clear();
            foreach (var pair in originalRenderers) if (pair.Key != null) pair.Key.enabled = pair.Value;
        }
    }
}
