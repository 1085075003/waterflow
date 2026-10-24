using System;
using System.Collections.Generic;
using UnityEngine;

namespace Recovered.FlowWater
{
    // Original FT3 obj13..16 belong to each intake and survive doExit as
    // inactive children. Their first world Instantiate basis then follows
    // their original parent, including while that parent is rotating.
    public sealed class RecoveredNativeJetCache : MonoBehaviour
    {
        internal sealed class Entry
        {
            public string key, origin;
            public int slot, color;
            public bool born;
            public Quaternion localBasis, nativeWorld;
            public Transform parking;
            public GameObject wrapper, model;
            public SavedEntry restore;
            public int phaseRestoredFrame = -1;
        }
        [Serializable] internal sealed class ParticlePhase
        {
            public uint seed; public float time;
            public bool playing, paused, autoSeed, hasTrails, hasCustomData;
            public ParticleSystem.PlaybackState playback;
            public ParticleSystem.Particle[] particles;
            public ParticleSystem.Trails trails;
            public List<Vector4> custom1, custom2;
        }
        [Serializable] internal sealed class SavedEntry
        {
            public string key, origin; public int slot, color;
            public bool born, active;
            public Quaternion localBasis, nativeWorld;
            public ParticlePhase[] particles;
        }
        sealed class OwnerState
        { public string owner, activeOrigin; public readonly List<SavedEntry> entries = new List<SavedEntry>(); }
        sealed class State { public readonly List<OwnerState> owners = new List<OwnerState>(); }
        readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
        readonly Dictionary<string, Transform> parking = new Dictionary<string, Transform>(StringComparer.Ordinal);
        MonoBehaviour controller;
        RecoveryPiece piece;
        string activeOrigin;
        public int CachedWrapperCount { get { int n = 0; foreach (Entry e in entries.Values) if (e.wrapper != null) n++; return n; } }

        public static RecoveredNativeJetCache For(MonoBehaviour source, bool create)
        {
            if (source == null || source.GetComponentInParent<RecoveryPiece>() == null) return null;
            var owner = source.GetComponent<RecoveredNativeJetCache>();
            if (owner == null && create) owner = source.gameObject.AddComponent<RecoveredNativeJetCache>();
            if (owner != null) { owner.controller = source; owner.piece = source.GetComponentInParent<RecoveryPiece>(); }
            return owner;
        }
        public static bool TryBasis(MonoBehaviour source, int slot, out Quaternion basis)
        {
            basis = Quaternion.identity;
            var owner = For(source, false); Entry entry;
            if (owner == null || owner.activeOrigin == null || !owner.entries.TryGetValue(owner.activeOrigin + ":" + slot, out entry) || !entry.born) return false;
            basis = owner.piece.transform.rotation * entry.localBasis; return true;
        }
        internal Entry Acquire(MonoBehaviour origin, int slot, out bool reused)
        {
            string name = origin.gameObject.name, key = name + ":" + slot;
            if (activeOrigin != name) { activeOrigin = name; Changed(); }
            Entry entry;
            if (!entries.TryGetValue(key, out entry))
            { entry = new Entry { key = key, origin = name, slot = slot }; entries.Add(key, entry); }
            if (entry.parking == null)
            {
                Transform anchor;
                if (!parking.TryGetValue(name, out anchor) || anchor == null)
                {
                    anchor = new GameObject("Original water · cached routing " + name).transform;
                    anchor.SetParent(controller.transform, false); anchor.position = origin.transform.position; anchor.rotation = origin.transform.rotation;
                    WorldScale(anchor, origin.transform.lossyScale); parking[name] = anchor;
                }
                entry.parking = anchor;
            }
            reused = entry.wrapper != null;
            if (reused) entry.wrapper.transform.SetParent(origin.transform, true);
            return entry;
        }
        internal void Bind(Entry entry, GameObject wrapper, Quaternion nativeWorld)
        {
            entry.wrapper = wrapper; entry.nativeWorld = nativeWorld;
            if (entry.born) wrapper.transform.rotation = piece.transform.rotation * entry.localBasis;
            else wrapper.transform.rotation = nativeWorld;
            var handle = wrapper.AddComponent<RecoveredNativeJetHandle>(); handle.owner = this; handle.entry = entry;
        }
        internal void Activated(Entry entry)
        {
            if (entry.wrapper == null) return;
            if (!entry.born)
            {
                // doActivarChorro creates only at its actual delayed callback.
                // A parent may have turned since the inlet first became wet.
                entry.wrapper.transform.rotation = entry.nativeWorld;
                entry.localBasis = Quaternion.Inverse(piece.transform.rotation) * entry.nativeWorld;
                entry.born = true; Changed();
            }
            if (entry.phaseRestoredFrame == Time.frameCount || ApplyRestoredPhase(entry)) return;
            if (entry.model != null)
                foreach (ParticleSystem ps in entry.model.GetComponentsInChildren<ParticleSystem>(true))
                    if (ps.gameObject.activeInHierarchy && (entry.restore == null || entry.restore.active)) ps.Play(false);
        }
        bool ApplyRestoredPhase(Entry entry)
        {
            if (entry.restore == null || entry.model == null || !entry.model.activeInHierarchy) return false;
            ParticleSystem[] particles = entry.model.GetComponentsInChildren<ParticleSystem>(true);
            ParticlePhase[] phases = entry.restore.particles;
            if (phases != null)
                for (int i = 0; i < particles.Length && i < phases.Length; i++)
                {
                    ParticleSystem ps = particles[i]; ParticlePhase phase = phases[i];
                    // SetActive may already have started playOnAwake. Both
                    // seed setters require a stopped, cleared system. Establish
                    // its saved play flags before replacing actual simulation
                    // state, rather than resimulating a stopped emitter's clock.
                    ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.useAutoRandomSeed = false; ps.randomSeed = phase.seed;
                    ps.useAutoRandomSeed = phase.autoSeed;
                    ps.Simulate(0, false, false, true);
                    if (phase.playing) ps.Play(false);
                    else if (phase.paused) ps.Pause(false);
                    else ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                    ps.SetPlaybackState(phase.playback);
                    if (phase.particles != null) ps.SetParticles(phase.particles, phase.particles.Length);
                    if (phase.hasTrails) ps.SetTrails(phase.trails);
                    if (phase.hasCustomData)
                    {
                        ps.SetCustomParticleData(phase.custom1, ParticleSystemCustomData.Custom1);
                        ps.SetCustomParticleData(phase.custom2, ParticleSystemCustomData.Custom2);
                    }
                }
            entry.restore = null; entry.phaseRestoredFrame = Time.frameCount;
            return true;
        }
        public static void NotifyActivation(GameObject target)
        {
            if (target == null) return;
            var handle = target.GetComponentInParent<RecoveredNativeJetHandle>(true);
            if (handle != null && handle.owner != null && handle.entry != null) handle.owner.Activated(handle.entry);
        }
        public static bool Owns(Transform target) { return target != null && target.GetComponentInParent<RecoveredNativeJetHandle>(true) != null; }
        public static void Park(Transform waterRoot, bool actualExit)
        {
            if (waterRoot == null) return;
            foreach (var handle in waterRoot.GetComponentsInChildren<RecoveredNativeJetHandle>(true))
            {
                if (handle.owner == null || handle.entry == null) continue;
                Entry entry = handle.entry;
                if (actualExit && !entry.born)
                {
                    entry.wrapper.SetActive(false); UnityEngine.Object.Destroy(entry.wrapper);
                    entry.wrapper = entry.model = null; handle.owner.entries.Remove(entry.key); continue;
                }
                if (actualExit) entry.wrapper.SetActive(false);
                entry.wrapper.transform.SetParent(entry.parking, true);
            }
        }
        void Changed()
        {
            var network = RecoveredWaterNetwork.Instance;
            if (network != null && network.CurrentPieceRoot != null && piece != null && piece.transform.IsChildOf(network.CurrentPieceRoot)) network.InvalidateNativeJetState();
        }
        static string OwnerKey(RecoveryPiece piece, MonoBehaviour source)
        {
            string path = "";
            for (Transform t = source.transform; t != null && t != piece.transform; t = t.parent) path = "/" + t.GetSiblingIndex() + path;
            return piece.Index + path + ":" + Array.IndexOf(source.GetComponents<MonoBehaviour>(), source);
        }
        public static object Capture(IEnumerable<RecoveryPiece> pieces)
        {
            var saved = new State(); if (pieces == null) return saved;
            foreach (RecoveryPiece piece in pieces)
            {
                if (piece == null) continue;
                foreach (var owner in piece.GetComponentsInChildren<RecoveredNativeJetCache>(true))
                {
                    if (owner.controller == null) continue;
                    var os = new OwnerState { owner = OwnerKey(piece, owner.controller), activeOrigin = owner.activeOrigin };
                    foreach (Entry entry in owner.entries.Values)
                    {
                        var es = new SavedEntry { key = entry.key, origin = entry.origin, slot = entry.slot, color = entry.color,
                            born = entry.born, localBasis = entry.localBasis, nativeWorld = entry.nativeWorld,
                            active = entry.wrapper != null && entry.wrapper.activeSelf };
                        if (entry.model != null)
                        {
                            ParticleSystem[] particles = entry.model.GetComponentsInChildren<ParticleSystem>(true); es.particles = new ParticlePhase[particles.Length];
                            for (int i = 0; i < particles.Length; i++)
                            {
                                ParticleSystem ps = particles[i];
                                var phase = new ParticlePhase { seed = ps.randomSeed, autoSeed = ps.useAutoRandomSeed,
                                    time = ps.time, playing = ps.isPlaying, paused = ps.isPaused,
                                    playback = ps.GetPlaybackState(), particles = new ParticleSystem.Particle[ps.particleCount],
                                    hasTrails = ps.trails.enabled, hasCustomData = ps.customData.enabled };
                                int count = ps.GetParticles(phase.particles);
                                if (count != phase.particles.Length) Array.Resize(ref phase.particles, count);
                                if (phase.hasTrails) phase.trails = ps.GetTrails();
                                if (phase.hasCustomData)
                                {
                                    phase.custom1 = new List<Vector4>(); phase.custom2 = new List<Vector4>();
                                    ps.GetCustomParticleData(phase.custom1, ParticleSystemCustomData.Custom1);
                                    ps.GetCustomParticleData(phase.custom2, ParticleSystemCustomData.Custom2);
                                }
                                es.particles[i] = phase;
                            }
                        }
                        else if (entry.restore != null) es.particles = entry.restore.particles;
                        os.entries.Add(es);
                    }
                    saved.owners.Add(os);
                }
            }
            return saved;
        }
        public static void Restore(object state, IEnumerable<RecoveryPiece> pieces)
        {
            var saved = state as State; if (saved == null || pieces == null) return;
            var owners = new Dictionary<string, MonoBehaviour>(StringComparer.Ordinal);
            foreach (RecoveryPiece piece in pieces)
                if (piece != null)
                    foreach (ControlPiezaC control in piece.GetComponentsInChildren<ControlPiezaC>(true))
                        if (!Owns(control.transform)) owners[OwnerKey(piece, control)] = control;
            var savedOwners = new HashSet<string>(StringComparer.Ordinal);
            foreach (OwnerState os in saved.owners) savedOwners.Add(os.owner);
            foreach (var pair in owners)
            {
                var existing = For(pair.Value, false);
                if (existing == null || savedOwners.Contains(pair.Key)) continue;
                foreach (Entry entry in existing.entries.Values) if (entry.wrapper != null) UnityEngine.Object.Destroy(entry.wrapper);
                existing.entries.Clear(); existing.activeOrigin = null;
            }
            foreach (OwnerState os in saved.owners)
            {
                MonoBehaviour control; if (!owners.TryGetValue(os.owner, out control)) continue;
                var owner = For(control, true); owner.activeOrigin = os.activeOrigin;
                var keep = new HashSet<string>(StringComparer.Ordinal);
                foreach (SavedEntry es in os.entries)
                {
                    keep.Add(es.key); Entry entry;
                    if (!owner.entries.TryGetValue(es.key, out entry)) owner.entries[es.key] = entry = new Entry { key = es.key, origin = es.origin, slot = es.slot };
                    if (!es.born && entry.born && entry.wrapper != null)
                    { UnityEngine.Object.Destroy(entry.wrapper); entry.wrapper = entry.model = null; }
                    entry.color = es.color; entry.born = es.born; entry.localBasis = es.localBasis; entry.nativeWorld = es.nativeWorld; entry.restore = es.active ? es : null; entry.phaseRestoredFrame = -1;
                    if (entry.wrapper != null) entry.wrapper.transform.rotation = owner.piece.transform.rotation * es.localBasis;
                }
                foreach (string key in new List<string>(owner.entries.Keys))
                    if (!keep.Contains(key)) { Entry e = owner.entries[key]; if (e.wrapper != null) UnityEngine.Object.Destroy(e.wrapper); owner.entries.Remove(key); }
            }
        }
        static void WorldScale(Transform target, Vector3 scale)
        { Vector3 b = target.parent == null ? Vector3.one : target.parent.lossyScale; target.localScale = new Vector3(scale.x / b.x, scale.y / b.y, scale.z / b.z); }
    }
}
