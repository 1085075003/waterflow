// New reconstruction from serialized directional ports, not the original IL2CPP methods.
// Supports basic channels and downhill falls. Gates, springs and encoded mechanisms remain unverified.
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
            public readonly bool[] ports = new bool[4];
            public bool source, basin, topInlet;
            public int expectedColor, waterColor;
            public readonly Node[] links = new Node[4];
        }

        readonly List<Node> nodes = new List<Node>();
        readonly List<LineRenderer> lines = new List<LineRenderer>();
        readonly List<Material> materials = new List<Material>();
        RecoveredFlowGame game;
        Transform visuals;
        float nextCheck;
        int signature;
        public int WetPieceCount { get; private set; }
        public int FilledBasinCount { get; private set; }
        public int BasinCount { get; private set; }
        static readonly Vector3[] Directions = { Vector3.left, Vector3.forward, Vector3.right, Vector3.back };
        static readonly Color[] Colors = { Color.cyan, new Color(.15f,.65f,1), new Color(.25f,1,.45f), new Color(1,.8f,.1f), new Color(1,.25f,.25f) };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartNetwork()
        {
            new GameObject("Recovered basic water network").AddComponent<RecoveredWaterNetwork>();
        }

        void Awake() { DontDestroyOnLoad(gameObject); }

        void Update()
        {
            if (game == null) game = RecoveredFlowGame.Instance;
            if (game == null || Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + .15f;
            int hash = game.CurrentLevelId;
            if (game.PieceRoot != null)
            {
                unchecked { hash = hash * 31 + game.PieceRoot.GetInstanceID(); }
                foreach (RecoveryPiece piece in game.Pieces)
                    if (piece != null) unchecked { hash = hash * 31 + piece.transform.position.GetHashCode(); }
            }
            if (hash != signature) { signature = hash; Rebuild(); }
            foreach (Material material in materials)
                if (material != null && material.HasProperty("_MainTex")) material.mainTextureOffset = new Vector2(-Time.time * .3f, 0);
        }

        public void Rebuild()
        {
            ClearVisuals(); nodes.Clear(); WetPieceCount = FilledBasinCount = BasinCount = 0;
            if (game == null || game.PieceRoot == null) return;
            visuals = new GameObject("Reconstructed basic channel water").transform;
            visuals.SetParent(game.PieceRoot, false);
            foreach (RecoveryPiece piece in game.Pieces)
            {
                if (piece == null) continue;
                Node node = ReadPorts(piece);
                if (node == null) continue;
                nodes.Add(node);
                if (node.basin) BasinCount++;
            }
            foreach (Node node in nodes)
            {
                for (int port = 0; port < 4; port++)
                {
                    if (!node.ports[port]) continue;
                    Vector3 direction = node.piece.transform.rotation * Directions[port];
                    Vector3 expected = node.piece.transform.position + direction * 20;
                    Node nearest = null;
                    float highest = float.NegativeInfinity;
                    foreach (Node candidate in nodes)
                    {
                        if (candidate == node || candidate.source) continue;
                        Vector3 point = candidate.piece.transform.position;
                        if (Mathf.Abs(expected.x - point.x) > .3f || Mathf.Abs(expected.z - point.z) > .3f ||
                            point.y > expected.y + .4f || point.y < expected.y - 100 || point.y <= highest) continue;
                        // Direction 5 is the original open top. A downhill waterfall can
                        // land in that top even when the facing horizontal edge is closed.
                        bool accepts = candidate.topInlet && point.y < node.piece.transform.position.y - 3;
                        for (int incoming = 0; incoming < 4; incoming++)
                            if (candidate.ports[incoming] && Vector3.Dot(candidate.piece.transform.rotation * Directions[incoming], direction) < -.9f)
                                accepts = true;
                        if (!accepts) continue;
                        nearest = candidate; highest = point.y;
                    }
                    node.links[port] = nearest;
                }
            }
            var queue = new Queue<Node>();
            foreach (Node node in nodes)
                if (node.source) { node.waterColor = Mathf.Clamp(node.expectedColor, 1, 4); queue.Enqueue(node); }
            while (queue.Count > 0)
            {
                Node node = queue.Dequeue();
                for (int port = 0; port < 4; port++)
                {
                    Node destination = node.links[port];
                    if (destination == null || destination.waterColor != 0 ||
                        (destination.expectedColor != 0 && destination.expectedColor != node.waterColor)) continue;
                    destination.waterColor = node.waterColor;
                    queue.Enqueue(destination);
                }
            }
            foreach (Node node in nodes)
            {
                if (node.waterColor == 0) continue;
                WetPieceCount++;
                if (node.basin) FilledBasinCount++;
                Color color = Colors[Mathf.Clamp(node.waterColor, 0, 4)];
                Vector3 center = node.piece.transform.position + Vector3.up * 2.0f;
                for (int port = 0; port < 4; port++)
                {
                    if (!node.ports[port]) continue;
                    Vector3 edge = center + node.piece.transform.rotation * Directions[port] * 10;
                    DrawWater(center, edge, color);
                    Node destination = node.links[port];
                    if (destination != null && destination.waterColor == node.waterColor &&
                        destination.piece.transform.position.y < node.piece.transform.position.y - 1)
                    {
                        Vector3 landing = destination.piece.transform.position + Vector3.up * 2;
                        DrawWater(edge, landing, color);
                    }
                }
            }
        }

        static Node ReadPorts(RecoveryPiece piece)
        {
            foreach (MonoBehaviour behaviour in piece.GetComponents<MonoBehaviour>())
            {
                if (behaviour == null || !behaviour.GetType().Name.StartsWith("ControlPieza", StringComparison.Ordinal)) continue;
                Type type = behaviour.GetType();
                var node = new Node { piece = piece };
                node.source = type.Name == "ControlPiezaFuente";
                node.basin = type.Name.Contains("Cubeta");
                node.expectedColor = ReadInt(behaviour, "color");
                bool any = false;
                string prefix = node.source ? "cascada" : (type.GetField("origen1") != null ? "origen" : "ori");
                node.topInlet = !node.source && ReadBool(behaviour, prefix + "5");
                for (int port = 0; port < 4; port++)
                    any |= node.ports[port] = ReadBool(behaviour, prefix + (port + 1));
                if (any) return node;
            }
            return null;
        }

        static bool ReadBool(object value, string name)
        {
            FieldInfo field = value.GetType().GetField(name);
            return field != null && field.FieldType == typeof(bool) && (bool)field.GetValue(value);
        }

        static int ReadInt(object value, string name)
        {
            FieldInfo field = value.GetType().GetField(name);
            return field != null && field.FieldType == typeof(int) ? (int)field.GetValue(value) : 0;
        }

        void DrawWater(Vector3 start, Vector3 end, Color color)
        {
            var lineObject = new GameObject("Basic water");
            lineObject.transform.SetParent(visuals, false);
            var line = lineObject.AddComponent<LineRenderer>();
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            var material = new Material(shader);
            material.color = Color.white;
            materials.Add(material);
            line.sharedMaterial = material;
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.SetPosition(0, start); line.SetPosition(1, end);
            line.startWidth = line.endWidth = 2.1f;
            line.startColor = line.endColor = color;
            line.numCapVertices = 4;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            lines.Add(line);
        }

        void ClearVisuals()
        {
            if (visuals != null) Destroy(visuals.gameObject);
            foreach (Material material in materials) if (material != null) Destroy(material);
            materials.Clear(); lines.Clear(); visuals = null;
        }

        void OnDestroy() { ClearVisuals(); }
    }
}
