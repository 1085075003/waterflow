// Reconstructed controller using exported water meshes, materials, animation and particle assets.
// Original IL2CPP methods are unavailable; mechanism overrides are applied by the configuration layer.
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Recovered.FlowWater
{
    public sealed class RecoveredWaterNetwork : MonoBehaviour
    {
        sealed class Node
        {
            public RecoveryPiece piece;
            public MonoBehaviour controller;
            public MonoBehaviour builtInEmitter;
            public GameObject prefab, water;
            public readonly bool[] ports = new bool[6];
            public readonly Node[] links = new Node[6];
            public bool source, wallSource, basin, pedestal, target;
            public int[] centerDirections;
            public List<RecoveredWaterJets.Route> jets;
            public Transform previewRoot;
            public float previewGround;
            public object visualConfiguration;
            public int expectedColor, waterColor, incoming;
            public float sourceDelay;
            public bool upwardBlocked;
            public string key;
            public int visualSignature;
            public RecoveredWaterPropagation.Channel arrival;
            public Vector3 Position { get { return controller.transform.position; } }
            public Vector3 LevelPosition { get { return previewRoot == null ? Position : previewRoot.InverseTransformPoint(Position); } }
            public Quaternion Rotation { get { return previewRoot == null ? piece.transform.rotation * Quaternion.Inverse(piece.InitialRotation) : previewRoot.rotation; } }
            public Vector3 Offset(Vector3 offset) { return previewRoot == null ? Rotation * offset : previewRoot.TransformVector(offset); }
        }
        readonly List<Node> nodes = new List<Node>();
        readonly List<GameObject> visuals = new List<GameObject>();
        readonly Dictionary<RecoveryPiece, bool> sourceOverrides = new Dictionary<RecoveryPiece, bool>();
        readonly Dictionary<RecoveryPiece, Dictionary<int, bool>> portOverrides = new Dictionary<RecoveryPiece, Dictionary<int, bool>>();
        readonly HashSet<RecoveryPiece> movingPieces = new HashSet<RecoveryPiece>();
        readonly RecoveredWaterPropagation propagation = new RecoveredWaterPropagation();
        sealed class WaterState
        {
            public int level; public object propagation;
            public readonly Dictionary<int, bool> sources = new Dictionary<int, bool>();
            public readonly Dictionary<int, Dictionary<int, bool>> ports = new Dictionary<int, Dictionary<int, bool>>();
        }
        WaterState pendingRestore;
        List<GameObject> deferredEffects;
        float visualAge;
        RecoveredFlowGame game;
        RecoveredWaterCollision collision;
        float nextCheck;
        int signature;
        bool dirty = true;
        bool fountainSoundPlaying;
        AudioManager fountainAudioManager;
        public static RecoveredWaterNetwork Instance { get; private set; }
        public int WetPieceCount { get; private set; }
        public int FilledBasinCount { get; private set; }
        public int BasinCount { get; private set; }
        public int RequiredFountainCount { get; private set; }
        public int ActiveFountainCount { get { return FilledBasinCount; } }
        public int LevelId { get; private set; }
        public Transform CurrentPieceRoot { get; private set; }
        public int OriginalWaterPrefabCount { get; private set; }
        public int OriginalParticleSystemCount { get; private set; }
        public string[] BlockedFlowDiagnostics { get { return RecoveredWaterCollision.BlockedDiagnostics.ToArray(); } }
        public int PendingWaterEventCount { get { return propagation.PendingCount; } }
        public bool HasPendingPropagation { get { return propagation.PendingCount != 0; } }
        public float LastScheduledWaterEventTime { get { return propagation.LastScheduledTime; } }
        static readonly Vector3[] Directions = { Vector3.left, Vector3.forward, Vector3.right, Vector3.back, Vector3.up, Vector3.down };
        static readonly string[] ColorNames = { "", "Azul", "Verde", "Z", "Ro" };
        // AguaControl5's intake radius is .4 and its output sphere has radius
        // .2 at scale2: their native trigger overlap accepts .8 world units.
        const float PortAlignmentTolerance = .8001f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartNetwork()
        {
            if (Instance == null) new GameObject("Original water network").AddComponent<RecoveredWaterNetwork>();
        }
        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<RuntimeWaterMechanisms>();
        }
        void Update()
        {
            if (game == null) game = RecoveredFlowGame.Instance;
            if (game == null) return;
            if (dirty || Time.unscaledTime >= nextCheck || CurrentPieceRoot != game.PieceRoot)
            {
            nextCheck = Time.unscaledTime + .12f;
            int hash = game.CurrentLevelId;
            for (int group = 1; group <= 4; group++) unchecked { hash = hash * 31 + RecoveredWaterJets.Global("conResorte" + group); }
            unchecked { hash = (hash * 31 + UIManagerScript.confColor1) * 31 + UIManagerScript.confColor2; }
            if (game.PieceRoot != null)
            {
                unchecked { hash = hash * 31 + game.PieceRoot.GetInstanceID(); }
                foreach (RecoveryPiece piece in game.Pieces)
                    if (piece != null) unchecked { hash = (hash * 31 + piece.transform.position.GetHashCode()) * 31 + piece.transform.rotation.GetHashCode(); }
            }
            if (dirty || hash != signature) { signature = hash; dirty = false; Rebuild(); }
            }
            if (propagation.Advance(Time.time)) RefreshCounts();
            AudioManager manager = AudioManager.instance;
            bool audible = ActiveFountainCount > 0 && PlayerPrefs.GetInt("Soundlevel", 1) == 1;
            if (manager != fountainAudioManager || audible != fountainSoundPlaying)
            {
                fountainAudioManager = manager; fountainSoundPlaying = audible;
                if (manager != null)
                {
                    if (audible) manager.PlaySound("SonidoFuente");
                    else manager.StopSound("SonidoFuente");
                }
            }
        }
        public object CaptureState()
        {
            if (game == null) game = RecoveredFlowGame.Instance;
            if (game != null && (dirty || CurrentPieceRoot != game.PieceRoot || LevelId != game.CurrentLevelId)) Rebuild();
            var state = new WaterState { level = LevelId, propagation = propagation.CaptureState(Time.time) };
            if (game != null) foreach (RecoveryPiece piece in game.Pieces)
            {
                bool active; Dictionary<int, bool> ports;
                if (sourceOverrides.TryGetValue(piece, out active)) state.sources[piece.Index] = active;
                if (portOverrides.TryGetValue(piece, out ports)) state.ports[piece.Index] = new Dictionary<int, bool>(ports);
            }
            return state;
        }
        public void RestoreState(object state)
        {
            pendingRestore = state as WaterState;
            if (pendingRestore == null) return;
            Rebuild();
        }
        /// <summary>Original port numbering: 1..4 horizontal, 5 top, 6 bottom.</summary>
        public void SetPortEnabled(RecoveryPiece piece, int port, bool enabled)
        {
            if (piece == null || port < 1 || port > 6) return;
            Dictionary<int, bool> overrides;
            if (!portOverrides.TryGetValue(piece, out overrides)) portOverrides[piece] = overrides = new Dictionary<int, bool>();
            bool previous;
            if (overrides.TryGetValue(port, out previous) && previous == enabled) return;
            overrides[port] = enabled; dirty = true;
        }
        public void SetSourceEnabled(RecoveryPiece piece, bool enabled)
        {
            if (piece == null) return;
            bool previous;
            if (sourceOverrides.TryGetValue(piece, out previous) && previous == enabled) return;
            sourceOverrides[piece] = enabled; dirty = true;
        }
        public void SetPieceMoving(RecoveryPiece piece, bool moving)
        {
            if (piece == null) return;
            if (moving ? movingPieces.Add(piece) : movingPieces.Remove(piece)) dirty = true;
        }
        public int GetWaterColor(RecoveryPiece piece)
        {
            foreach (Node node in nodes) if (node.piece == piece && node.waterColor != 0) return node.waterColor;
            return 0;
        }
        public bool IsWet(RecoveryPiece piece) { return GetWaterColor(piece) != 0; }
        public bool IsPortWet(RecoveryPiece piece, int port)
        {
            if (port < 1 || port > 6) return false;
            foreach (Node node in nodes) if (node.piece == piece && node.ports[port - 1] && node.waterColor != 0) return true;
            return false;
        }
        public bool IsControllerIntakeWet(MonoBehaviour controller, int port)
        {
            if (port < 1 || port > 6) return false;
            foreach (Node node in nodes)
                if (node.controller == controller && node.waterColor != 0 && node.incoming == port) return true;
            return false;
        }
        public bool IsControllerPortWet(MonoBehaviour controller, int port)
        {
            if (port < 1 || port > 6) return false;
            foreach (Node node in nodes) if (node.controller == controller && node.ports[port - 1] && node.waterColor != 0) return true;
            return false;
        }
        /// <summary>Renders one original category-demo water controller without changing the board graph.</summary>
        public GameObject RestorePreviewWaterNode(MonoBehaviour controller, GameObject prototype, Transform basis,
            int color, int incoming, bool[] ports, int[] directions, bool source, bool pedestal,
            float minimumHeight, MonoBehaviour[] receivers, List<RecoveredWaterJets.Route> jets, float age = 0)
        {
            if (controller == null || prototype == null || basis == null) return null;
            int prefabCount = OriginalWaterPrefabCount, particleCount = OriginalParticleSystemCount;
            var node = new Node { controller = controller, prefab = prototype, previewRoot = basis,
                previewGround = minimumHeight, waterColor = color, incoming = incoming, source = source,
                wallSource = controller is ControlPiezaFuente, pedestal = pedestal, centerDirections = directions,
                jets = jets ?? new List<RecoveredWaterJets.Route>() };
            Array.Copy(ports, node.ports, Math.Min(ports.Length, 6));
            if (receivers != null)
                for (int i = 0; i < Math.Min(receivers.Length, 6); i++)
                    if (receivers[i] != null) node.links[i] = new Node { controller = receivers[i], previewRoot = basis };
            float previousAge = visualAge; visualAge = age;
            CreateOriginalWater(node);
            if (node.water != null) node.water.AddComponent<RecoveredWaterMotion>().RestoreAuthoredMotion(age);
            visualAge = previousAge;
            OriginalWaterPrefabCount = prefabCount; OriginalParticleSystemCount = particleCount;
            return node.water;
        }
        public void Rebuild()
        {
            if (game == null) game = RecoveredFlowGame.Instance;
            nodes.Clear();
            BasinCount = 0;
            RequiredFountainCount = 0;
            LevelId = game == null ? 0 : game.CurrentLevelId;
            Transform nextRoot = game == null ? null : game.PieceRoot;
            if (CurrentPieceRoot != nextRoot)
            {
                movingPieces.Clear(); propagation.Reset(RemoveArrivedWater); ClearVisuals();
                var actors = new HashSet<RecoveryPiece>(game == null ? new RecoveryPiece[0] : game.Pieces);
                foreach (RecoveryPiece piece in new List<RecoveryPiece>(sourceOverrides.Keys)) if (!actors.Contains(piece)) sourceOverrides.Remove(piece);
                foreach (RecoveryPiece piece in new List<RecoveryPiece>(portOverrides.Keys)) if (!actors.Contains(piece)) portOverrides.Remove(piece);
            }
            CurrentPieceRoot = nextRoot;
            if (game == null || game.PieceRoot == null) { RefreshCounts(); return; }
            if (pendingRestore != null && pendingRestore.level == LevelId)
                foreach (RecoveryPiece piece in game.Pieces)
                {
                    bool active; Dictionary<int, bool> ports;
                    if (pendingRestore.sources.TryGetValue(piece.Index, out active)) sourceOverrides[piece] = active;
                    else sourceOverrides.Remove(piece);
                    if (pendingRestore.ports.TryGetValue(piece.Index, out ports)) portOverrides[piece] = new Dictionary<int, bool>(ports);
                    else portOverrides.Remove(piece);
                }
            foreach (RecoveryPiece piece in game.Pieces)
            {
                if (piece == null) continue;
                // SaveLoadManager.doProcesoPiezas counts targets by their saved
                // names. Its required count is independent of the active-node rule.
                string name = piece.BasePrefab ?? piece.OriginalName ?? "";
                bool namedTarget = (name.Contains("Cu") && (name.Contains("CubaFuente") || name.Contains("CuFuente") || name.Contains("CubFuente"))) || name.Contains("BloqueChorro1");
                string config = piece.OriginalConfiguration ?? "";
                int springStart = config.IndexOf("st", StringComparison.Ordinal);
                int springEnd = springStart < 0 ? -1 : config.IndexOf("et", springStart + 2, StringComparison.Ordinal);
                string spring = springEnd < 0 ? "" : config.Substring(springStart + 2, springEnd - springStart - 2);
                if (namedTarget && spring != "9") RequiredFountainCount++;
                if (movingPieces.Contains(piece)) continue;
                // Crossings contain TWO ControlPieza components, one per independent water channel.
                foreach (MonoBehaviour behaviour in piece.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (IsGeneratedWater(behaviour.transform, piece.transform)) continue;
                    Node node = ReadPorts(piece, behaviour);
                    if (node == null) continue;
                    node.key = StableChannelKey(piece, behaviour);
                    nodes.Add(node); if (node.target) BasinCount++;
                }
            }
            using (var snapshot = new RecoveredWaterCollision(game.Pieces))
            {
            collision = snapshot;
            try
            {
            // Original Awake creates these center probes even for dry channels.
            // They control where cascades land on side-only or closed blocks.
            foreach (Node node in nodes)
                snapshot.AddWaterPrefabColliders(node.piece, node.controller.transform, node.prefab, node.Rotation, node.wallSource ? 1 : .1f);
            snapshot.SyncWaterPrefabColliders();
            foreach (Node node in nodes)
            {
                for (int port = 0; port < 4; port++)
                    if (node.ports[port]) node.links[port] = FindHorizontalReceiver(node, port);
                if (node.ports[5] && !node.pedestal) node.links[5] = FindBottomReceiver(node);
                // PiedraFuente5 is an elevating jet. Its visible exits are the
                // inherited side/bottom intakes; esferaSalida is a separate
                // upward outlet and feeds the next storey's bottom intake.
                if (node.pedestal && (node.builtInEmitter == null || node.ports[4])) node.links[4] = FindUpwardReceiver(node);
                node.jets = RecoveredWaterJets.ReadRoutes(node.controller);
                foreach (RecoveredWaterJets.Route jet in node.jets)
                {
                    Node receiver = FindJetReceiver(node, jet);
                    if (receiver != null) jet.receiver = receiver.controller.transform;
                }
            }
            var channels = new List<RecoveredWaterPropagation.Channel>();
            foreach (Node node in nodes)
            {
                bool enabled;
                bool flowing = node.source && (!sourceOverrides.TryGetValue(node.piece, out enabled) || enabled);
                node.arrival = new RecoveredWaterPropagation.Channel { key = node.key,
                    signature = ChannelSignature(node, flowing), expectedColor = node.expectedColor,
                    source = flowing, sourceColor = Mathf.Clamp(node.expectedColor, 1, 4),
                    sourceIncoming = node.incoming, sourceDelay = node.sourceDelay, tag = node };
                node.upwardBlocked = IsUpwardBlocked(node);
                node.visualSignature = VisualSignature(node);
                channels.Add(node.arrival);
            }
            foreach (Node node in nodes)
            {
                for (int port = 0; port < 6; port++)
                {
                    int outlet = port;
                    Node destination = node.links[port];
                    if (destination != null) AddIntakeDeliveries(node, destination, IncomingPort(node, destination, port),
                        "port" + port, incoming => incoming == outlet + 1 ? float.PositiveInfinity : DeliveryDelay(node, outlet, incoming));
                }
                for (int i = 0; i < node.jets.Count; i++)
                {
                    RecoveredWaterJets.Route jet = node.jets[i];
                    if (jet.receiver == null) continue;
                    foreach (Node destination in nodes)
                        if (destination.controller.transform == jet.receiver)
                        { AddIntakeDeliveries(node, destination, 5, "jet" + i, incoming => OutletDelay(node, incoming)); break; }
                }
            }
            if (pendingRestore != null)
            {
                if (pendingRestore.level == LevelId) propagation.RestoreState(pendingRestore.propagation);
                pendingRestore = null;
            }
            propagation.Rebuild(channels, Time.time, EnterArrivedWater, RemoveArrivedWater, RetainArrivedWater);
            RefreshCounts();
            }
            finally { collision = null; }
            }
        }
        void AddIntakeDeliveries(Node source, Node receiver, int incoming, string outlet, Func<int, float> delay)
        {
            foreach (Node candidate in nodes)
            {
                int candidateIncoming = incoming;
                if (candidate != receiver)
                {
                    // Several original saved layouts contain overlapping
                    // independent pieces. Their input triggers both receive
                    // the same physical stream. Crossings on one piece still
                    // have independent channels and never share this delivery.
                    if (candidate.piece == receiver.piece || candidate.source ||
                        Mathf.Abs(candidate.Position.x - receiver.Position.x) > PortAlignmentTolerance ||
                        Mathf.Abs(candidate.Position.z - receiver.Position.z) > PortAlignmentTolerance ||
                        Mathf.Abs(candidate.Position.y - receiver.Position.y) > .4f) continue;
                    candidateIncoming = 0;
                    Vector3 direction = receiver.Rotation * Directions[Mathf.Clamp(incoming - 1, 0, 5)];
                    for (int port = 0; port < 6; port++)
                        if (candidate.ports[port] && Vector3.Dot(candidate.Rotation * Directions[port], direction) > .9f)
                        { candidateIncoming = port + 1; break; }
                    if (candidateIncoming == 0) continue;
                }
                source.arrival.outputs.Add(new RecoveredWaterPropagation.Link { target = candidate.arrival,
                    identity = outlet + ":" + candidate.key, incoming = candidateIncoming, delay = delay });
            }
        }
        static bool IsGeneratedWater(Transform current, Transform piece)
        {
            for (; current != null && current != piece; current = current.parent)
                if (current.name.StartsWith("Original water · ", StringComparison.Ordinal)) return true;
            return false;
        }
        static string StableChannelKey(RecoveryPiece piece, MonoBehaviour controller)
        {
            string path = "";
            for (Transform current = controller.transform; current != null && current != piece.transform; current = current.parent)
                path = "/" + current.GetSiblingIndex() + path;
            MonoBehaviour[] components = controller.GetComponents<MonoBehaviour>();
            return piece.Index + path + ":" + Array.IndexOf(components, controller);
        }
        static int ChannelSignature(Node node, bool enabled)
        {
            unchecked
            {
                int hash = node.Position.GetHashCode() * 31 + node.Rotation.GetHashCode();
                hash = (hash * 31 + node.expectedColor) * 31 + (enabled ? 1 : 0);
                for (int i = 0; i < 6; i++) hash = hash * 31 + (node.ports[i] ? 1 : 0);
                return hash;
            }
        }
        static int VisualSignature(Node node)
        {
            unchecked
            {
                int hash = node.upwardBlocked ? 1 : 0;
                for (int i = 0; i < 6; i++)
                {
                    Node other = node.links[i];
                    hash = hash * 31 + (other == null ? 0 : other.key.GetHashCode());
                    if (other != null) hash = hash * 31 + other.Position.GetHashCode();
                }
                foreach (RecoveredWaterJets.Route jet in node.jets)
                    hash = ((hash * 31 + jet.displacement.GetHashCode()) * 31 + jet.mode) * 31 + jet.direction;
                return (hash * 31 + UIManagerScript.confColor1) * 31 + UIManagerScript.confColor2;
            }
        }
        bool IsUpwardBlocked(Node node)
        {
            if (!node.pedestal || collision == null) return false;
            MonoBehaviour emitter = node.builtInEmitter;
            if (emitter == null && node.prefab != null)
                foreach (MonoBehaviour component in node.prefab.GetComponentsInChildren<MonoBehaviour>(true))
                    if (component is CascadaTest1 || component is CascadaTest3) { emitter = component; break; }
            if (emitter == null) return false;
            Vector3 position = emitter.transform.position;
            if (node.builtInEmitter == null)
            {
                Vector3 local = node.prefab.transform.InverseTransformPoint(position);
                position = node.Position + node.Rotation * Vector3.Scale(local, node.controller.transform.lossyScale * .1f);
            }
            return collision.BlocksUpward(node.piece, node.links[4] == null ? null : node.links[4].piece,
                position + Vector3.up * ReadFloat(emitter, "distan_y"));
        }
        static MonoBehaviour PrefabOrigin(Node node, int incoming)
        {
            if (node.prefab == null) return null;
            return FindOrigin(node.prefab.GetComponentsInChildren<MonoBehaviour>(true), incoming,
                node.pedestal ? "FuenteCubeta2" : node.basin ? "FuenteCubeta" : "FuenteTest");
        }
        static float OutletDelay(Node node, int incoming)
        {
            if (node.wallSource || node.builtInEmitter != null) return 0;
            MonoBehaviour origin = PrefabOrigin(node, incoming);
            return origin == null ? 0 : Mathf.Max(0, ReadFloat(origin, "espera"));
        }
        float DeliveryDelay(Node node, int port, int incoming)
        {
            if (node.pedestal && port == 4)
                return node.upwardBlocked ? float.PositiveInfinity : OutletDelay(node, incoming);
            if (node.wallSource) return 1; // CascadaTest2.doControlCascada's Invoke.
            float delay = OutletDelay(node, incoming);
            Node receiver = node.links[port];
            if (port == 5 || receiver != null && receiver.Position.y < node.Position.y - 3)
            {
                float drop = node.Position.y - (receiver == null ? game.MinimumHeight : receiver.Position.y);
                int height = Mathf.Clamp(Mathf.RoundToInt(drop / 7.6f) + (port == 5 ? 1 : 0), 1, 5);
                // Native normal/bottom CascadaTest activation table at 0x621140.
                delay += height <= 3 ? 1 : height == 4 ? 1.1f : 1.2f;
            }
            else if (node.controller is ControlPiezaC && port < 4)
            {
                // FuenteTest3.doActivarEsfera first opens the entry axis and
                // invokes doActivarEsfera0 for the perpendicular pair at .1s.
                bool firstAxis = incoming == 1 || incoming == 3 || incoming >= 5;
                if ((port % 2 == 0) != firstAxis) delay += .1f;
            }
            return delay;
        }
        void EnterArrivedWater(RecoveredWaterPropagation.Channel channel)
        {
            Node node = (Node)channel.tag;
            node.waterColor = channel.color; node.incoming = channel.incoming;
            visualAge = Mathf.Max(0, Time.time - channel.enteredAt);
            CreateOriginalWater(node);
            if (node.water != null) RecoveredNativePalette.ApplyMaterials(node.piece, node.water);
            if (node.water != null) node.water.AddComponent<RecoveredWaterMotion>().RestoreAuthoredMotion(visualAge);
            visualAge = 0;
        }
        void RetainArrivedWater(RecoveredWaterPropagation.Channel previous, RecoveredWaterPropagation.Channel current)
        {
            Node old = (Node)previous.tag, node = (Node)current.tag;
            node.waterColor = current.color; node.incoming = current.incoming;
            if (old.water != null && old.visualSignature == node.visualSignature) node.water = old.water;
            else { RemoveArrivedWater(previous); EnterArrivedWater(current); }
        }
        void RemoveArrivedWater(RecoveredWaterPropagation.Channel channel)
        {
            Node node = channel.tag as Node;
            if (node == null || node.water == null) return;
            visuals.Remove(node.water); node.water.SetActive(false); Destroy(node.water); node.water = null;
        }
        void RefreshCounts()
        {
            var wet = new HashSet<RecoveryPiece>();
            FilledBasinCount = 0; OriginalWaterPrefabCount = 0; OriginalParticleSystemCount = 0;
            foreach (Node node in nodes)
                if (node.waterColor != 0) { wet.Add(node.piece); if (node.target) FilledBasinCount++; }
            WetPieceCount = wet.Count;
            for (int i = visuals.Count - 1; i >= 0; i--)
            {
                GameObject water = visuals[i];
                if (water == null) { visuals.RemoveAt(i); continue; }
                OriginalWaterPrefabCount++;
                foreach (Transform child in water.GetComponentsInChildren<Transform>(true))
                    if (child != water.transform && child.name.StartsWith("Original ", StringComparison.Ordinal)) OriginalWaterPrefabCount++;
                OriginalParticleSystemCount += water.GetComponentsInChildren<ParticleSystem>(true).Length;
            }
        }
        Node FindHorizontalReceiver(Node node, int port)
        {
            Vector3 direction = node.Rotation * Directions[port];
            Vector3 expected = node.Position + direction * 20;
            Node nearest = null; float highest = float.NegativeInfinity;
            foreach (Node candidate in nodes)
            {
                if (candidate == node || candidate.source || candidate.piece == node.piece) continue;
                Vector3 point = candidate.Position;
                if (Mathf.Abs(expected.x - point.x) > PortAlignmentTolerance || Mathf.Abs(expected.z - point.z) > PortAlignmentTolerance ||
                    point.y > expected.y + .45f || point.y < expected.y - 160 || point.y <= highest) continue;
                bool lower = point.y < node.Position.y - 3;
                // The falling outlet ends above the next cell's center. Only
                // Origen5 is there; a side-only elevating jet's intakes are
                // eight units away and cannot receive this vertical stream.
                bool accepts = lower && candidate.ports[4];
                if (!lower)
                    for (int incoming = 0; incoming < 4; incoming++)
                        if (candidate.ports[incoming] && Vector3.Dot(candidate.Rotation * Directions[incoming], direction) < -.9f) accepts = true;
                if (!accepts || SolidBlocksFall(node, candidate, expected)) continue;
                nearest = candidate; highest = point.y;
            }
            return nearest;
        }
        Node FindBottomReceiver(Node origin)
        {
            Node nearest = null;
            float highest = float.NegativeInfinity;
            foreach (Node candidate in nodes)
            {
                if (candidate.piece == origin.piece || candidate.source || !candidate.ports[4]) continue;
                Vector3 point = candidate.Position;
                if (Mathf.Abs(point.x - origin.Position.x) > .4f || Mathf.Abs(point.z - origin.Position.z) > .4f ||
                    point.y >= origin.Position.y - 3 || point.y <= highest || SolidBlocksFall(origin, candidate, origin.Position)) continue;
                nearest = candidate; highest = point.y;
            }
            return nearest;
        }
        Node FindUpwardReceiver(Node origin)
        {
            Node receiver = null;
            float lowest = float.PositiveInfinity;
            foreach (Node candidate in nodes)
            {
                if (candidate.piece == origin.piece || candidate.source || !candidate.ports[5]) continue;
                Vector3 point = candidate.Position;
                float rise = point.y - origin.Position.y;
                float reach = origin.builtInEmitter == null ? 8.1f : ReadFloat(origin.builtInEmitter, "distan_y") + .5f;
                if (Mathf.Abs(point.x - origin.Position.x) > .4f || Mathf.Abs(point.z - origin.Position.z) > .4f ||
                    rise < 3 || rise > reach || point.y >= lowest) continue;
                receiver = candidate; lowest = point.y;
            }
            return receiver;
        }
        Node FindJetReceiver(Node origin, RecoveredWaterJets.Route route)
        {
            Vector3 target = origin.Position + route.displacement;
            Node receiver = null; float highest = float.NegativeInfinity;
            foreach (Node candidate in nodes)
            {
                if (candidate.piece == origin.piece || candidate.source || !candidate.ports[4]) continue;
                Vector3 position = candidate.Position;
                if (Mathf.Abs(position.x - target.x) > .4f || Mathf.Abs(position.z - target.z) > .4f ||
                    position.y > origin.Position.y + 15.7f || position.y < origin.Position.y - 31 || position.y <= highest) continue;
                if (SolidBlocksFall(origin, candidate, target)) continue;
                receiver = candidate; highest = position.y;
            }
            return receiver;
        }
        bool SolidBlocksFall(Node origin, Node candidate, Vector3 expected)
        {
            if (collision != null) return collision.BlocksFall(origin.piece, origin.Position, candidate.piece, candidate.Position, expected);
            foreach (RecoveryPiece piece in game.Pieces)
            {
                if (piece == null || piece == candidate.piece || piece == origin.piece) continue;
                Vector3 point = piece.transform.position;
                if (Mathf.Abs(point.x - expected.x) < .4f && Mathf.Abs(point.z - expected.z) < .4f &&
                    point.y > candidate.Position.y + 3 && point.y <= origin.Position.y + .4f) return true;
            }
            return false;
        }
        static int IncomingPort(Node source, Node destination, int port)
        {
            if (port == 4 && source.pedestal) return 6;
            if (destination.Position.y < source.Position.y - 3 && destination.ports[4]) return 5;
            Vector3 direction = source.Rotation * Directions[Mathf.Clamp(port, 0, 5)];
            for (int incoming = 0; incoming < 6; incoming++)
                if (destination.ports[incoming] && Vector3.Dot(destination.Rotation * Directions[incoming], direction) < -.9f) return incoming + 1;
            return 5;
        }
        Node ReadPorts(RecoveryPiece piece, MonoBehaviour behaviour)
        {
            // PiedraFuente7 carries the original emitter directly, with no
            // ControlPieza or water-controller prefab. CascadaTest3.Start
            // explicitly enables its fountain; CascadaTest1 is water-controlled.
            MonoBehaviour emitter = behaviour is CascadaTest3 || behaviour is CascadaTest1 ? behaviour : null;
            if (emitter != null && emitter.gameObject.activeInHierarchy && (emitter is CascadaTest3 || ReadInt(emitter, "activa") == 1))
            {
                var source = new Node { piece = piece, controller = emitter, builtInEmitter = emitter,
                    source = true, pedestal = true, expectedColor = ReadInt(emitter, "color") };
                bool open; Dictionary<int, bool> sourcePorts;
                source.ports[4] = !portOverrides.TryGetValue(piece, out sourcePorts) ||
                    !sourcePorts.TryGetValue(5, out open) || open;
                return source;
            }
            if (behaviour == null || !behaviour.GetType().Name.StartsWith("ControlPieza", StringComparison.Ordinal)) return null;
            GameObject prefab = ReadObject<GameObject>(behaviour, "prefabAguaControl");
            if (prefab == null) return null;
            Type type = behaviour.GetType();
            var node = new Node { piece = piece, controller = behaviour, prefab = prefab };
            node.wallSource = type.Name == "ControlPiezaFuente";
            node.source = node.wallSource;
            node.basin = type.Name.Contains("Cubeta"); node.pedestal = type.Name == "ControlPiezaPfuente";
            int kind = ReadInt(behaviour, "tipoPieza");
            // ControlPiezaC.doTratarTipoPieza8 activates its authored rendija
            // color trigger only for configuration 2. Other jet blocks need water.
            bool internalSource = type.Name == "ControlPiezaC" && kind == 8 && ReadInt(behaviour, "configFuente") == 2;
            bool authoredSource = false;
            if (type.Name == "ControlPiezaC")
                foreach (activaEsfera activator in behaviour.GetComponentsInChildren<activaEsfera>(true))
                    if (activator != null && activator.gameObject.activeInHierarchy && activator.esfera1 != null) authoredSource = true;
            // activaEsfera.Start invokes activaEsfera1 after 0.5s. This authored
            // self-priming colored trigger occurs on CuFuenteED1..4 and groups.
            node.source |= internalSource || authoredSource;
            node.sourceDelay = authoredSource && !internalSource ? .5f : 0;
            node.target = (node.basin || (type.Name == "ControlPiezaC" && kind >= 2 && kind != 8)) &&
                ReadString(behaviour, "conResorte") != "9";
            node.expectedColor = ReadInt(behaviour, "color");
            if (internalSource)
            {
                int postColor = ReadInt(behaviour, "palo4");
                node.expectedColor = postColor >= 7 && postColor <= 9 ? postColor - 5 : 1;
                node.incoming = 5;
            }
            else if (authoredSource) node.incoming = 5;
            if (piece.ColorVariant > 0 && (node.source || node.basin || node.expectedColor > 0)) node.expectedColor = piece.ColorVariant;
            string prefix = node.wallSource ? "cascada" : (type.GetField("origen1") != null ? "origen" : "ori");
            bool any = false; Dictionary<int, bool> overrides;
            portOverrides.TryGetValue(piece, out overrides);
            bool[] nativePorts = null; int[] nativeDirections;
            ControlPiezaC pool = behaviour as ControlPiezaC;
            if (pool != null && RecoveredNativeGeometry.TryGetWaterPorts(pool, out nativePorts, out nativeDirections))
                node.centerDirections = nativeDirections;
            for (int port = 0; port < 6; port++)
            {
                bool enabled;
                bool authored = nativePorts != null ? nativePorts[port] : ReadBool(behaviour, prefix + (port + 1));
                node.ports[port] = authored &&
                    (overrides == null || !overrides.TryGetValue(port + 1, out enabled) || enabled);
                any |= node.ports[port];
            }
            if (internalSource) { node.ports[4] = true; any = true; }
            return any ? node : null;
        }
        void CreateOriginalWater(Node node)
        {
            if (node.builtInEmitter != null)
            {
                // Own only the new effect container; the authored source and
                // its geometry belong to the original piece and survive rebuilds.
                GameObject container = new GameObject("Original water · " + node.builtInEmitter.GetType().Name);
                container.transform.SetParent(node.controller.transform, false);
                container.transform.localPosition = Vector3.zero;
                container.transform.rotation = node.Rotation;
                node.water = container; visuals.Add(container);
                RestorePedestal(node, new MonoBehaviour[] { node.builtInEmitter });
                return;
            }
            Transform parent = node.previewRoot != null && !node.wallSource ? node.prefab.transform.parent : node.controller.transform;
            GameObject water = Instantiate(node.prefab, parent, false);
            water.name = "Original water · " + node.controller.GetType().Name;
            water.transform.position = node.Position; water.transform.rotation = node.Rotation;
            // ControlPieza/C/Pfuente.Awake overwrite the preview prefab scale.
            // Source Awake instead explicitly uses unit local scale.
            if (node.previewRoot != null && !node.wallSource) SetWorldScale(water.transform, node.prefab.transform.lossyScale);
            else water.transform.localScale = Vector3.one * (node.wallSource ? 1 : .1f);
            node.water = water; if (node.previewRoot == null) visuals.Add(water); OriginalWaterPrefabCount++;
            var components = water.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in components) if (behaviour != null) behaviour.enabled = false;
            foreach (Collider collider in water.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (Rigidbody body in water.GetComponentsInChildren<Rigidbody>(true)) { body.isKinematic = true; body.useGravity = false; }
            foreach (Renderer renderer in water.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            MonoBehaviour channelOrigin = FindOrigin(components, node.incoming, "FuenteTest");
            if (channelOrigin != null)
            {
                if (node.previewRoot != null) node.visualConfiguration = channelOrigin;
                bool configured = RestoreOriginalChannelConfiguration(node, channelOrigin);
                if (!configured && node.previewRoot == null && channelOrigin is FuenteTest3 && node.centerDirections != null)
                    for (int i = 1; i <= 4; i++) channelOrigin.GetType().GetField("dirPlano" + i).SetValue(channelOrigin, node.centerDirections[i - 1]);
                RestoreChannelSurfaces(node, channelOrigin);
                RecoveredNativeWaterAssets.RestoreFlow(channelOrigin, node.incoming, node.waterColor, SpawnEffect, visualAge);
            }
            if (node.basin) RestoreBasin(node, components);
            RestoreConfiguredFountains(node, components);
            deferredEffects = new List<GameObject>();
            if (node.pedestal) RestorePedestal(node, components);
            for (int port = 0; port < 4; port++)
            {
                if (!node.ports[port] || node.pedestal) continue;
                if (!node.wallSource && node.incoming == port + 1) continue;
                Node receiver = node.links[port];
                if (!node.wallSource && receiver != null && Mathf.Abs(receiver.Position.y - node.Position.y) < 1) continue;
                RestoreCascade(node, components, port, receiver);
            }
            if (node.ports[5] && !node.pedestal) RestoreBottomCascade(node, components);
            if (channelOrigin != null) RecoveredWaterJets.Restore(channelOrigin, node.waterColor, node.jets, SpawnEffect);
            float outputWait = OutletDelay(node, node.incoming) - visualAge;
            foreach (GameObject effect in deferredEffects)
                RecoveredWaterArrival.ActivateAfter(node.water.transform, effect, outputWait);
            deferredEffects = null;
        }
        bool RestoreOriginalChannelConfiguration(Node node, MonoBehaviour origin)
        {
            // Menu sequences modify their actual FuenteTest3 instances after
            // Start. Their serialized/runtime fields are already the final
            // configuration and must survive cloning for the preview.
            if (node.previewRoot != null) return false;
            var water = node.water.GetComponent<AguaControlC>();
            var target = origin as FuenteTest3;
            if (water == null || target == null) return false;
            var source = node.controller as ControlPiezaC;
            if (source != null)
            {
                // ControlPiezaC.Awake/Start -> AguaControlC -> FuenteTest3.
                water.activarFuente = source.activarFuente; water.tipoPieza = source.tipoPieza;
                water.colorCubeta = source.color; water.confColor = source.confColor;
                water.configFuente = source.configFuente; water.configFuenteB = source.configFuenteB;
                water.configFuenteCentro = source.configFuenteCentro;
                water.configPit1 = source.configPit1; water.configPit2 = source.configPit2;
                water.configPit3 = source.configPit3; water.configPit4 = source.configPit4;
                water.configRes1 = source.configRes1; water.configRes2 = source.configRes2;
                water.configRes3 = source.configRes3; water.configRes4 = source.configRes4;
                water.configAnim = source.configAnim; water.configLights = source.configLights;
                RecoveredNativeGeometry.ApplyWaterConfiguration(source, water);
            }
            water.ori1 = node.ports[0]; water.ori2 = node.ports[1]; water.ori3 = node.ports[2];
            water.ori4 = node.ports[3]; water.ori5 = node.ports[4]; water.ori6 = node.ports[5];
            target.esf1 = water.ori1 && node.incoming != 1; target.esf2 = water.ori2 && node.incoming != 2;
            target.esf3 = water.ori3 && node.incoming != 3; target.esf4 = water.ori4 && node.incoming != 4;
            target.esf6 = water.ori6;
            target.dirPlano1 = water.rebos1 ? 1 : water.dirCentro1;
            target.dirPlano2 = water.rebos2 ? 1 : water.dirCentro2;
            target.dirPlano3 = water.rebos3 ? 1 : water.dirCentro3;
            target.dirPlano4 = water.rebos4 ? 1 : water.dirCentro4;
            // FC9F70/FCA0A4/etc only replace the center direction when nonzero.
            int center = node.incoming == 1 ? water.dirCentro1 : node.incoming == 2 ? water.dirCentro2 :
                node.incoming == 3 ? water.dirCentro3 : node.incoming == 4 ? water.dirCentro4 : 0;
            if (center != 0) target.dirPlanoCentro = center;
            target.rebose1 = water.rebos1; target.rebose2 = water.rebos2;
            target.rebose3 = water.rebos3; target.rebose4 = water.rebos4;
            target.wall1 = water.wall1; target.wall2 = water.wall2; target.wall3 = water.wall3; target.wall4 = water.wall4;
            target.activarFuente = water.activarFuente; target.tipoPieza = water.tipoPieza;
            target.colorCubeta = water.colorCubeta; target.confColor = water.confColor;
            target.configFuente = water.configFuente; target.configFuenteB = water.configFuenteB;
            target.configFuenteCentro = water.configFuenteCentro;
            target.configPit1 = water.configPit1; target.configPit2 = water.configPit2;
            target.configPit3 = water.configPit3; target.configPit4 = water.configPit4;
            target.configRes1 = water.configRes1; target.configRes2 = water.configRes2;
            target.configRes3 = water.configRes3; target.configRes4 = water.configRes4;
            target.configAnim = water.configAnim; target.configLi = water.configLights;
            node.visualConfiguration = target;
            return true;
        }
        void RestoreChannelSurfaces(Node node, MonoBehaviour origin)
        {
            int from = node.incoming > 0 ? node.incoming : ReadInt(origin, "origen");
            Material edge = ReadObject<Material>(origin, "materialColor" + node.waterColor + "_1") ?? ReadObject<Material>(origin, "materialColor" + node.waterColor);
            Material center = ReadObject<Material>(origin, "materialColor" + node.waterColor + "_C") ?? edge;
            for (int port = 1; port <= 4; port++)
            {
                if (!node.ports[port - 1]) continue;
                GameObject plane = ReadObject<GameObject>(origin, "plano" + port); ShowMesh(plane, edge);
                scrollTest1 scroll = plane == null ? null : plane.GetComponent<scrollTest1>();
                if (scroll == null) continue;
                int direction = ReadInt(origin, "dirPlano" + port);
                if (from == port) direction = (direction + 3) % 8 + 1;
                SetFlowDirection(scroll, direction);
            }
            GameObject straightCenter = ReadObject<GameObject>(origin, "planoCentro");
            GameObject curvedCenter = ReadObject<GameObject>(origin, "planoCentro1");
            if (straightCenter != null) straightCenter.SetActive(false);
            if (curvedCenter != null) curvedCenter.SetActive(false);
            // FuenteTest1/3.doTratarDirecciones selects ONE center mesh by
            // dirPlanoCentro parity. The 500-scale curve is authored in XY and
            // must rotate -90 around X; enabling it by tipoPieza creates a sheet.
            if (from >= 1 && from <= 4)
            {
                int direction = ReadInt(origin, "dirPlanoCentro");
                int configured = node.centerDirections != null && node.centerDirections.Length >= from ? node.centerDirections[from - 1] : ReadInt(node.controller, "dirCentro" + from);
                if (configured >= 1) direction = configured;
                if (direction >= 1 && direction <= 8)
                {
                    if (RecoveredNativeWaterAssets.RestoreCenter(origin, node.water.transform, from, direction, center)) return;
                    bool curved = direction % 2 == 0;
                    GameObject centerPlane = curved ? curvedCenter : straightCenter;
                    if (centerPlane != null)
                    {
                        if (curved)
                            centerPlane.transform.rotation = node.Rotation * Quaternion.Euler(-90, 0, (direction - 2) * 45);
                        else centerPlane.transform.rotation = node.Rotation * Quaternion.Euler(0, (direction - 1) * 45, 0);
                        ShowMesh(centerPlane, center);
                    }
                }
            }
        }
        static void SetFlowDirection(scrollTest1 scroll, int direction)
        {
            Vector2[] compass = { Vector2.zero, Vector2.right, new Vector2(1,1).normalized, Vector2.up,
                new Vector2(-1,1).normalized, Vector2.left, new Vector2(-1,-1).normalized, Vector2.down, new Vector2(1,-1).normalized };
            if (direction < 1 || direction > 8) return;
            float speed = Mathf.Max(.5f, new Vector2(scroll.scrollSpeed_X, scroll.scrollSpeed_Y).magnitude);
            scroll.scrollSpeed_X = compass[direction].x * speed; scroll.scrollSpeed_Y = compass[direction].y * speed;
        }
        void RestoreBasin(Node node, MonoBehaviour[] components)
        {
            MonoBehaviour origin = FindOrigin(components, node.incoming, "FuenteCubeta");
            if (origin == null) return;
            GameObject waterPrefab = ReadObject<GameObject>(origin, "prefabAgua" + node.waterColor);
            int incoming = Mathf.Clamp(node.incoming, 1, 5);
            if (waterPrefab != null)
            {
                // FuenteCubetaB.doInstanciarAgua 0x109B0A0: native position,
                // rotation and scale constants, rather than the prefab preview position.
                GameObject model = SpawnEffect(waterPrefab, origin.transform);
                model.transform.localPosition = incoming <= 4 ? Directions[incoming - 1] * 14.32f + Vector3.down * 3.9f : new Vector3(0, -1.5f, 11.6f);
                model.transform.localRotation = Quaternion.Euler(0, incoming <= 4 ? (incoming - 2) * 90 : 180, 0);
                model.transform.localScale = incoming <= 4 ? new Vector3(2.9f, 4, 2.7f) : new Vector3(2.9f, 2, 2);
            }
            int type = ReadInt(node.controller, "tipoFuente");
            GameObject fountainPrefab = ReadObject<GameObject>(origin, "prefabFuente" + node.waterColor);
            if (type != 3 && fountainPrefab != null)
            {
                GameObject fountain = SpawnEffect(fountainPrefab, origin.transform);
                SetWorldScale(fountain.transform, fountainPrefab.transform.localScale);
                fountain.transform.localPosition = new Vector3(.11f, type == 2 ? 15.5f : 23.12f, -.16f);
                fountain.transform.localRotation = Quaternion.identity;
            }
            for (int port = 1; port <= 4; port++)
                if (node.ports[port-1]) ShowMesh(FindDescendant(node.water.transform, "plano" + port), ReadObject<Material>(origin, "materialColor" + node.waterColor));
        }
        void RestorePedestal(Node node, MonoBehaviour[] components)
        {
            MonoBehaviour origin = FindOrigin(components, node.incoming, "FuenteCubeta2");
            // FuenteCubeta2.doInstanciarAgua (0x109876C) only publishes origen;
            // it does not instantiate the legacy prefabAgua fields.
            if (origin != null)
            {
                GameObject trigger = ReadObject<GameObject>(origin, "esferaFuente1");
                if (trigger != null) { ActivateAncestors(trigger.transform); trigger.tag = "color" + node.waterColor; }
                AguasControl aguas = node.water.GetComponentInChildren<AguasControl>(true);
                if (aguas != null) aguas.hayOrigen = node.incoming;
            }
            foreach (MonoBehaviour behaviour in components)
            {
                if (!(behaviour is CascadaTest1) && !(behaviour is CascadaTest3)) continue;
                MonoBehaviour controller = behaviour;
                controller.GetType().GetField("activa").SetValue(controller, 1);
                controller.GetType().GetField("color").SetValue(controller, node.waterColor);
                // CascadaTest1.Update suppresses its fountain when a solid block
                // occupies the authored check point above the emitter.
                Vector3 check = controller.transform.position + Vector3.up * ReadFloat(controller, "distan_y");
                bool blocked = node.previewRoot == null ? node.upwardBlocked : false;
                if (!blocked)
                {
                    GameObject prefab = ReadObject<GameObject>(behaviour, "prefabFuente" + node.waterColor);
                    if (prefab != null)
                    {
                        GameObject effect = SpawnEffect(prefab, node.builtInEmitter == null ? controller.transform : node.water.transform);
                        SetWorldScale(effect.transform, prefab.transform.localScale);
                        // Test1 FD3BEC uses .7; standalone Test3 FD5134 uses 1.2.
                        effect.transform.localPosition = new Vector3(0, controller is CascadaTest3 ? 1.2f : .7f, 0);
                        effect.transform.localRotation = Quaternion.identity;
                    }
                }
                break;
            }
        }
        void SpawnAtPiece(GameObject prefab, Node node, float height)
        {
            if (prefab == null) return;
            GameObject effect = SpawnEffect(prefab, node.water.transform);
            effect.transform.position = node.Position + Vector3.up * height; effect.transform.rotation = node.Rotation;
            SetWorldScale(effect.transform, prefab.transform.localScale);
        }
        void RestoreCascade(Node node, MonoBehaviour[] components, int port, Node receiver)
        {
            MonoBehaviour cascade = null;
            foreach (MonoBehaviour behaviour in components)
                if (behaviour != null && behaviour.GetType().Name.StartsWith("CascadaTest", StringComparison.Ordinal) &&
                    ReadBool(behaviour, "cascada" + (port + 1))) { cascade = behaviour; break; }
            if (cascade == null) return;
            float ground = node.previewRoot == null ? game.MinimumHeight : node.previewGround;
            Vector3 target = node.Position + node.Offset(Directions[port] * 20);
            if (receiver != null) ground = receiver.LevelPosition.y;
            else if (node.previewRoot == null) foreach (RecoveryPiece piece in game.Pieces)
                if (piece != null && piece != node.piece && Mathf.Abs(piece.transform.position.x - target.x) < .4f &&
                    Mathf.Abs(piece.transform.position.z - target.z) < .4f && piece.transform.position.y < node.Position.y)
                    ground = Mathf.Max(ground, piece.transform.position.y);
            float drop = Mathf.Max(3.8f, node.LevelPosition.y - ground);
            int height = Mathf.Clamp(Mathf.RoundToInt(drop / 7.6f), 1, 5);
            bool source = cascade.GetType().Name == "CascadaTest2";
            int family = source ? (height == 1 ? 1 : height <= 3 ? 2 : 3) : (height <= 3 ? 1 : 2);
            GameObject prefab = ReadObject<GameObject>(cascade, "prefabCas" + node.waterColor + "_" + family) ?? ReadObject<GameObject>(cascade, "prefabCas" + node.waterColor + "_1");
            if (prefab == null) return;
            GameObject effect = SpawnEffect(prefab, cascade.transform);
            Vector3 position, scale;
            if (source)
            {
                // CascadaTest2.doAsignarValores 0xFD4A20, decoded native float table.
                float[] offsetY = { 0, -.289f, -.506f, -.726f, -.64f, -.712f };
                float[] scaleY = { 0, .2f, .42f, .7f, .57f, .65f };
                position = Directions[port] * (height == 1 ? 1.811f : 1.7f) + Vector3.up * offsetY[height];
                scale = new Vector3(height == 1 ? .42f : .5f, scaleY[height], height == 1 ? .5f : height <= 3 ? .45f : .38f);
            }
            else
            {
                // CascadaTest.doAsignarValores, normal channel branch checkPNormal=false.
                float[] offsetY = { 0, -.053f, -.279f, -.514f, -.397f, -.54f };
                float[] scaleY = { 0, .125f, .285f, .45f, .369f, .47f };
                float[] horizontal = { 0, 1.714f, 1.73f, 1.699f, 1.635f, 1.7f };
                position = Directions[port] * horizontal[height] + Vector3.up * offsetY[height];
                scale = new Vector3(height == 1 ? .48f : .5f, scaleY[height], height == 1 ? .405f : height == 2 ? .41f : height == 3 ? .38f : .4f);
            }
            effect.transform.localPosition = position;
            // Original Start/OnEnable constants: W=90, N=180, E=-90, S=0.
            effect.transform.localRotation = Quaternion.Euler(0, 90 + port * 90, 0);
            effect.transform.localScale = scale;
            // FD18A8..FD18CC: a falling outlet replaces the flat side plane
            // with its curved lip when doControlCascada starts.
            if (cascade is CascadaTest)
                RecoveredWaterArrival.SetActiveAfter(node.water.transform, ReadObject<GameObject>(cascade, "plano"),
                    false, OutletDelay(node, node.incoming) - visualAge);
            GameObject curved = ReadObject<GameObject>(cascade, "planoCurvo");
            ShowMesh(curved, ReadObject<Material>(cascade, "materialColor" + node.waterColor));
            if (curved != null && deferredEffects != null) deferredEffects.Add(curved);
            GameObject splashPrefab = ReadObject<GameObject>(cascade, "prefabSplash_1");
            if (splashPrefab == null) return;
            GameObject splash = SpawnEffect(splashPrefab, node.water.transform);
            if (node.previewRoot != null)
            {
                Vector3 point = node.previewRoot.InverseTransformPoint(target); point.y = ground + 1.4f;
                splash.transform.position = node.previewRoot.TransformPoint(point);
            }
            else splash.transform.position = new Vector3(target.x, ground + 1.4f, target.z);
            splash.transform.rotation = node.Rotation;
            SetWorldScale(splash.transform, splashPrefab.transform.localScale);
        }
        void RestoreBottomCascade(Node node, MonoBehaviour[] components)
        {
            MonoBehaviour cascade = null;
            foreach (MonoBehaviour behaviour in components)
                if (behaviour != null && behaviour.GetType().Name == "CascadaTest" && ReadBool(behaviour, "cascada6")) { cascade = behaviour; break; }
            if (cascade == null) return;
            Node target = node.links[5];
            float drop = node.LevelPosition.y - (target == null ? (node.previewRoot == null ? game.MinimumHeight : node.previewGround) : target.LevelPosition.y);
            int height = Mathf.Clamp(Mathf.RoundToInt(drop / 7.6f) + 1, 2, 5);
            GameObject prefab = ReadObject<GameObject>(cascade, "prefabCas" + node.waterColor + "_6");
            if (prefab == null) return;
            GameObject effect = SpawnEffect(prefab, cascade.transform);
            effect.transform.localPosition = new Vector3(0, height == 2 ? -.37f : -.4f, 0);
            effect.transform.localRotation = Quaternion.identity;
            float[] widths = { 0, 0, .7f, .7f, .8f, .8f };
            float[] heights = { 0, 0, .3f, .5f, .72f, .9f };
            effect.transform.localScale = new Vector3(widths[height], heights[height], .4f);
        }
        void RestoreConfiguredFountains(Node node, MonoBehaviour[] components)
        {
            if (node.wallSource || node.basin || node.pedestal) return;
            MonoBehaviour origin = FindOrigin(components, node.incoming, "FuenteTest");
            if (origin == null) return;
            object configuration = node.visualConfiguration ?? node.controller;
            int kind = ReadInt(configuration, "tipoPieza");
            if (kind == 2) RecoveredNativeWaterAssets.RestoreFountains(origin, configuration, node.waterColor, SpawnEffect);
            else if (kind == 6) SpawnAtPiece(ReadObject<GameObject>(origin, "prefabBig" + ColorNames[node.waterColor]), node, 25);
        }
        GameObject SpawnEffect(GameObject prefab, Transform parent)
        {
            GameObject effect = Instantiate(prefab, parent, false); effect.name = "Original " + prefab.name; effect.SetActive(true);
            ActivateAncestors(effect.transform);
            foreach (MonoBehaviour behaviour in effect.GetComponentsInChildren<MonoBehaviour>(true)) if (behaviour != null) behaviour.enabled = false;
            foreach (Collider collider in effect.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            if (deferredEffects != null) deferredEffects.Add(effect);
            // Native Instantiate preserves both child selection and renderer
            // flags. In particular hidden trigger spheres must stay invisible.
            OriginalWaterPrefabCount++; return effect;
        }
        static MonoBehaviour FindOrigin(MonoBehaviour[] components, int incoming, string prefix)
        {
            MonoBehaviour fallback = null;
            // Some original Pfuente origin components contain stale serialized
            // origen values (Origen2=0, Origen6=4); the hierarchy names are the
            // actual six authored intake objects.
            if (incoming >= 1 && incoming <= 6)
                foreach (MonoBehaviour behaviour in components)
                    if (behaviour != null && behaviour.GetType().Name.StartsWith(prefix, StringComparison.Ordinal) &&
                        behaviour.gameObject.name == "Origen" + incoming) return behaviour;
            foreach (MonoBehaviour behaviour in components)
            {
                if (behaviour == null || !behaviour.GetType().Name.StartsWith(prefix, StringComparison.Ordinal)) continue;
                if (fallback == null) fallback = behaviour;
                if (ReadInt(behaviour, "origen") == incoming) return behaviour;
            }
            return fallback;
        }
        static GameObject FindDescendant(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child.gameObject;
            return null;
        }
        static void ShowMesh(GameObject target, Material material)
        {
            if (target == null) return;
            ActivateAncestors(target.transform);
            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = true; if (material != null) renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
            }
        }
        static void ActivateAncestors(Transform target)
        {
            for (Transform parent = target; parent != null; parent = parent.parent)
            {
                parent.gameObject.SetActive(true);
                if (parent.GetComponent<RecoveryPiece>() != null || parent.GetComponent<RecoveredPreviewWater>() != null) break;
            }
        }
        static void SetWorldScale(Transform target, Vector3 scale)
        {
            Vector3 parent = target.parent == null ? Vector3.one : target.parent.lossyScale;
            target.localScale = new Vector3(scale.x / Mathf.Max(.0001f, Mathf.Abs(parent.x)),
                scale.y / Mathf.Max(.0001f, Mathf.Abs(parent.y)), scale.z / Mathf.Max(.0001f, Mathf.Abs(parent.z)));
        }
        static bool ReadBool(object value, string name)
        {
            FieldInfo field = value.GetType().GetField(name); return field != null && field.FieldType == typeof(bool) && (bool)field.GetValue(value);
        }
        static int ReadInt(object value, string name)
        {
            FieldInfo field = value.GetType().GetField(name); return field != null && field.FieldType == typeof(int) ? (int)field.GetValue(value) : 0;
        }
        static float ReadFloat(object value, string name)
        {
            FieldInfo field = value.GetType().GetField(name); return field != null && field.FieldType == typeof(float) ? (float)field.GetValue(value) : 0;
        }
        static string ReadString(object value, string name)
        {
            FieldInfo field = value.GetType().GetField(name); return field == null ? null : field.GetValue(value) as string;
        }
        static T ReadObject<T>(object value, string name) where T : UnityEngine.Object
        {
            FieldInfo field = value.GetType().GetField(name); return field == null ? null : field.GetValue(value) as T;
        }
        void ClearVisuals()
        {
            foreach (GameObject visual in visuals) if (visual != null) { visual.SetActive(false); Destroy(visual); }
            visuals.Clear();
        }
        void OnDestroy() { ClearVisuals(); if (Instance == this) Instance = null; }
    }
}



