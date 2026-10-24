using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Recovered.FlowWater
{
    /// <summary>Native water switch bus. Evidence: FuenteTest1.doProcessEnter,
    /// doActivarMech/doActivarDoor and ObservadorDoor.getEstadoDoor/doCheckDoor.</summary>
    public sealed class RuntimeWaterMechanisms : MonoBehaviour
    {
        sealed class Control
        {
            public RecoveryPiece piece;
            public MonoBehaviour component;
            public int index;
        }
        sealed class Gate
        {
            public RecoveryPiece piece;
            public int port, group;
            public Animator animator;
            public int previous;
        }
        sealed class State
        {
            public readonly Dictionary<string, bool> previousWet = new Dictionary<string, bool>();
            public readonly Dictionary<string, bool> toggles = new Dictionary<string, bool>();
            public bool[] physical, doors, published;
            public int[] springGlobals;
        }
        readonly List<Control> controls = new List<Control>();
        readonly List<Gate> gates = new List<Gate>();
        readonly Dictionary<string, bool> previousWet = new Dictionary<string, bool>();
        readonly Dictionary<string, bool> toggleStates = new Dictionary<string, bool>();
        readonly List<GameObject> decorations = new List<GameObject>();
        readonly bool[] switches = new bool[9];
        readonly bool[] physicalSwitches = new bool[9];
        readonly bool[] publishedSwitches = new bool[9];
        readonly bool[] doors = new bool[6];
        Transform levelRoot;
        float nextUpdate;
        public int WaterSwitchCount { get; private set; }
        public int DoorCount { get { return gates.Count; } }
        static readonly BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;

        public object CaptureState()
        {
            RecoveredFlowGame game = RecoveredFlowGame.Instance;
            if (game != null && levelRoot != game.PieceRoot) Initialize(game);
            var state = new State();
            foreach (var pair in previousWet) state.previousWet[pair.Key] = pair.Value;
            foreach (var pair in toggleStates) state.toggles[pair.Key] = pair.Value;
            state.physical = (bool[])physicalSwitches.Clone();
            state.doors = (bool[])doors.Clone(); state.published = (bool[])publishedSwitches.Clone();
            state.springGlobals = new int[9];
            for (int group = 1; group <= 8; group++) state.springGlobals[group] = RecoveredWaterJets.Global("conResorte" + group);
            return state;
        }
        public void RestoreState(object snapshot)
        {
            State state = snapshot as State;
            if (state == null) return;
            RecoveredFlowGame game = RecoveredFlowGame.Instance;
            if (game != null && levelRoot != game.PieceRoot) Initialize(game);
            previousWet.Clear(); toggleStates.Clear();
            foreach (var pair in state.previousWet) previousWet[pair.Key] = pair.Value;
            foreach (var pair in state.toggles) toggleStates[pair.Key] = pair.Value;
            Array.Clear(physicalSwitches, 0, physicalSwitches.Length);
            if (state.physical != null) Array.Copy(state.physical, physicalSwitches, Math.Min(state.physical.Length, physicalSwitches.Length));
            Array.Clear(publishedSwitches, 0, publishedSwitches.Length);
            if (state.published != null) Array.Copy(state.published, publishedSwitches, Math.Min(state.published.Length, publishedSwitches.Length));
            if (state.springGlobals != null)
                for (int group = 1; group <= 8; group++) SetGlobal("conResorte" + group, state.springGlobals[group]);
            Array.Clear(doors, 0, doors.Length);
            if (state.doors != null) Array.Copy(state.doors, doors, Math.Min(state.doors.Length, doors.Length));
            for (int group = 1; group <= 5; group++) SetGlobal("conDoor" + group, doors[group] ? 1 : 2);
            RecoveredWaterNetwork water = RecoveredWaterNetwork.Instance;
            foreach (Gate gate in gates) ApplyGate(gate, doors[gate.group] ? 1 : 2, water);
            nextUpdate = 0;
        }
        public void SetPhysicalSwitch(int channel, bool active)
        {
            if (channel >= 1 && channel <= 8) physicalSwitches[channel] = active;
        }

        void LateUpdate()
        {
            RecoveredFlowGame game = RecoveredFlowGame.Instance;
            RecoveredWaterNetwork water = RecoveredWaterNetwork.Instance;
            if (game == null || water == null || Time.unscaledTime < nextUpdate) return;
            nextUpdate = Time.unscaledTime + .1f;
            if (levelRoot != game.PieceRoot) Initialize(game);
            if (levelRoot == null) return;
            Array.Clear(switches, 0, switches.Length);
            Array.Clear(doors, 0, doors.Length);
            WaterSwitchCount = 0;
            foreach (Control control in controls)
            {
                if (control.component == null || control.piece == null) continue;
                for (int port = 1; port <= 6; port++)
                {
                    string spring = Text(control.component, "conOrigen" + port);
                    string hole = Text(control.component, "conHole" + port);
                    if (String.IsNullOrEmpty(spring) && String.IsNullOrEmpty(hole)) continue;
                    bool wet = water.IsControllerPortWet(control.component, port);
                    string key = control.piece.Index + ":" + control.index + ":" + port;
                    bool wasWet;
                    previousWet.TryGetValue(key, out wasWet);
                    previousWet[key] = wet;
                    if (!String.IsNullOrEmpty(spring))
                    {
                        WaterSwitchCount++;
                        int group;
                        if (spring == "A" || spring == "B")
                        {
                            // Native Start sets faseMech=1. Each fresh wet entry
                            // alternates doActivarMech and doDesactivarMech.
                            group = spring == "A" ? 1 : 2;
                            bool state;
                            toggleStates.TryGetValue(key, out state);
                            if (wet && !wasWet) { state = !state; toggleStates[key] = state; }
                            switches[group] |= state;
                        }
                        else if (Int32.TryParse(spring, out group) && group >= 1 && group <= 5)
                            switches[group] |= wet;
                    }
                    int door;
                    if (wet && Int32.TryParse(hole, out door))
                    {
                        // Trigger '6' addresses the fifth global door; observer
                        // conDoor uses group '5'. This asymmetry is in native code.
                        if (door == 6) door = 5;
                        if (door >= 1 && door <= 5) doors[door] = true;
                    }
                }
            }
            for (int group = 1; group <= 8; group++)
            {
                bool active = switches[group] || physicalSwitches[group];
                if (active == publishedSwitches[group]) continue;
                publishedSwitches[group] = active;
                // Native trigger callbacks publish on edges. Inactive is 4;
                // unchanged inputs must preserve observer cleanup states3/5.
                SetGlobal("conResorte" + group, active ? 1 : 4);
            }
            for (int group = 1; group <= 5; group++) SetGlobal("conDoor" + group, doors[group] ? 1 : 2);
            foreach (Gate gate in gates)
            {
                if (gate.piece == null) continue;
                int state = doors[gate.group] ? 1 : 2;
                if (state == gate.previous) continue;
                ApplyGate(gate, state, water);
            }
        }

        static void ApplyGate(Gate gate, int state, RecoveredWaterNetwork water)
        {
            if (gate.piece == null) return;
            gate.previous = state; bool open = state == 1;
            if (water != null) water.SetPortEnabled(gate.piece, gate.port, open);
            if (gate.animator != null) { gate.animator.enabled = true; gate.animator.SetBool("openDoor", open); }
            foreach (FuenteTest1 origin in gate.piece.GetComponentsInChildren<FuenteTest1>(true))
            {
                origin.barrera = !open; origin.barreraOpen = open;
                FieldInfo closed = typeof(FuenteTest1).GetField("cerrada" + gate.port);
                if (closed != null) closed.SetValue(origin, !open);
            }
        }

        void Initialize(RecoveredFlowGame game)
        {
            foreach (GameObject decoration in decorations) if (decoration != null) Destroy(decoration);
            decorations.Clear(); controls.Clear(); gates.Clear(); previousWet.Clear(); toggleStates.Clear();
            Array.Clear(physicalSwitches, 0, physicalSwitches.Length);
            Array.Clear(publishedSwitches, 0, publishedSwitches.Length);
            Array.Clear(doors, 0, doors.Length);
            levelRoot = game.PieceRoot;
            for (int group = 1; group <= 8; group++) SetGlobal("conResorte" + group, 0);
            for (int group = 1; group <= 5; group++) SetGlobal("conDoor" + group, 2);
            if (levelRoot == null) return;
            foreach (RecoveryPiece piece in game.Pieces)
            {
                if (piece == null) continue;
                int index = 0;
                foreach (MonoBehaviour component in piece.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (component == null || !component.GetType().Name.StartsWith("ControlPieza", StringComparison.Ordinal)) continue;
                    controls.Add(new Control { piece = piece, component = component, index = index++ });
                    for (int port = 1; port <= 6; port++)
                    {
                        string hole = Text(component, "conHole" + port);
                        if (!String.IsNullOrEmpty(hole) && port <= 4)
                            SpawnDecoration(component, "prefabHole", port, false);
                        int group;
                        if (!Int32.TryParse(Text(component, "conDoor" + port), out group) || group < 1 || group > 5) continue;
                        GameObject decoration = port <= 4 ? SpawnDecoration(component, "prefabDoor", port, true) : null;
                        gates.Add(new Gate { piece = piece, port = port, group = group,
                            animator = decoration == null ? null : decoration.GetComponentInChildren<Animator>(true) });
                    }
                }
            }
        }
        GameObject SpawnDecoration(MonoBehaviour control, string field, int port, bool gate)
        {
            FieldInfo reference = control.GetType().GetField(field, PublicInstance);
            GameObject prefab = reference == null ? null : reference.GetValue(control) as GameObject;
            if (prefab == null) return null;
            GameObject instance = Instantiate(prefab, control.transform, false);
            instance.name = "Original " + (gate ? "door" : "water switch hole") + " " + port;
            // ControlPieza.Start native: origin zero, authored 0.1 scale,
            // rotations 0/90/180/270 for ports 1/2/3/4.
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.Euler(0, (port - 1) * 90, 0);
            instance.transform.localScale = Vector3.one * .1f;
            foreach (MonoBehaviour behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true)) if (behaviour != null) behaviour.enabled = false;
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (Rigidbody body in instance.GetComponentsInChildren<Rigidbody>(true)) { body.isKinematic = true; body.useGravity = false; }
            decorations.Add(instance); return instance;
        }
        static string Text(object control, string name)
        {
            FieldInfo field = control.GetType().GetField(name, PublicInstance);
            return field == null ? null : field.GetValue(control) as string;
        }
        static void SetGlobal(string name, int value)
        {
            FieldInfo field = typeof(UIManagerScript).GetField(name, BindingFlags.Static | BindingFlags.Public);
            if (field != null && field.FieldType == typeof(int)) field.SetValue(null, value);
        }
    }
}
