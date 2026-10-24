using System;
using System.Collections.Generic;
using System.Reflection;
using Recovered.FlowWater;
using UnityEngine;

/// <summary>
/// Executes the original Resorte2/b/c/d category models' recovered native
/// OnEnable and movement sequences. Uses the existing referenced objects only.
/// NativePreviewDemo contains the ARM64-derived poses, targets and color writes.
/// </summary>
public sealed class RecoveredPreviewDemoSequence : MonoBehaviour
{
    [Serializable] sealed class Database { public Entry[] entries; }
    [Serializable] sealed class Entry
    {
        public string type, evidence;
        public Pose[] positions, rotations;
        public Action[] active;
        public Target[] vectors;
        public Motion[] motions;
        public Cycle[] cycles;
    }
    [Serializable] sealed class Pose { public string target; public float[] value; }
    [Serializable] sealed class Target { public string field; public float[] value; }
    [Serializable] sealed class Motion
    {
        public string method, evidence, rotationObject;
        public string[] targets;
        public bool rotation, rotationLocal;
        public float rotationAngle, rotationEpsilon;
    }
    [Serializable] sealed class Cycle { public int cycle; public Write[] values; public Action[] actions; }
    [Serializable] sealed class Write
    {
        public string target, field, type, text;
        public int number;
        public float[] rgba;
    }
    [Serializable] sealed class Action
    {
        public string kind, target;
        public bool enabled;
        public float[] color;
    }

    public int ControllerCount { get { return runners.Count; } }
    public int TransitionCount { get; private set; }
    public int CompletedCycles { get; private set; }
    public string CurrentPhase { get { return runners.Count == 0 ? "none" : runners[0].Description; } }
    public int CurrentColorCycle { get { return runners.Count == 0 ? 0 : runners[0].Cycle; } }
    public int MissingReferences { get; private set; }
    readonly List<Runner> runners = new List<Runner>();
    readonly Dictionary<Material, Material> localMaterials = new Dictionary<Material, Material>();
    static Database database;
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    const float Epsilon = .009999999776482582f;

    public static RecoveredPreviewDemoSequence Attach(GameObject preview)
    {
        if (preview == null) return null;
        var existing = preview.GetComponent<RecoveredPreviewDemoSequence>();
        if (existing != null) return existing;
        if (database == null)
        {
            var asset = Resources.Load<TextAsset>("NativePreviewDemo");
            if (asset == null) return null;
            database = JsonUtility.FromJson<Database>(asset.text);
        }
        var sequence = preview.AddComponent<RecoveredPreviewDemoSequence>();
        foreach (var source in preview.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (source == null || !source.enabled) continue;
            Entry entry = null;
            foreach (var candidate in database.entries)
                if (candidate.type == source.GetType().Name) { entry = candidate; break; }
            if (entry == null) continue;
            source.enabled = false; // Its exported methods are stubs; this driver owns the sequence.
            var runner = new Runner(sequence, source, entry);
            sequence.runners.Add(runner);
            runner.Initialize();
        }
        return sequence;
    }

    void Update()
    {
        foreach (var runner in runners)
            if (runner.Source != null && runner.Source.gameObject.activeInHierarchy)
                runner.Tick(Time.time, Time.deltaTime);
    }

    void OnEnable()
    {
        // Native OnEnable restarts its five-second opening delay whenever the
        // existing preview is enabled again.
        foreach (var runner in runners) runner.Initialize();
    }

    void OnDisable()
    {
        foreach (var runner in runners) runner.Cancel();
    }

    void OnDestroy()
    {
        foreach (var material in localMaterials.Values) if (material != null) Destroy(material);
    }

    static object Read(object target, string name)
    {
        if (target == null) return null;
        var field = target.GetType().GetField(name, Fields);
        return field == null ? null : field.GetValue(target);
    }
    static void Set(object target, string name, object value)
    {
        if (target == null) return;
        var field = target.GetType().GetField(name, Fields);
        if (field != null) field.SetValue(target, value);
    }
    static Vector3 Vector(float[] value)
    {
        return value == null || value.Length < 3 ? Vector3.zero : new Vector3(value[0], value[1], value[2]);
    }
    static Color Rgba(float[] value)
    {
        return value == null || value.Length < 4 ? Color.white : new Color(value[0], value[1], value[2], value[3]);
    }

    Material Localize(Material authored)
    {
        if (authored == null) return null;
        Material local;
        if (localMaterials.TryGetValue(authored, out local)) return local;
        local = new Material(authored) { name = authored.name + " (category instance)" };
        localMaterials.Add(authored, local);
        foreach (var renderer in GetComponentsInChildren<Renderer>(true))
        {
            var materials = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < materials.Length; i++)
                if (materials[i] == authored) { materials[i] = local; changed = true; }
            if (changed) renderer.sharedMaterials = materials;
        }
        return local;
    }

    sealed class Runner
    {
        public readonly MonoBehaviour Source;
        public int Cycle { get; private set; }
        public string Description { get { return entry.type + ":" + mode + ":" + stage; } }
        readonly RecoveredPreviewDemoSequence owner;
        readonly Entry entry;
        readonly Dictionary<string, Motion> motions = new Dictionary<string, Motion>();
        readonly Dictionary<string, Vector3> vectors = new Dictionary<string, Vector3>();
        readonly Dictionary<string, GameObject> objects = new Dictionary<string, GameObject>();
        // 0=start delay, 1=forward, 2=hold, 3=reverse, 4=restart delay.
        int mode, stage;
        float deadline, enableGateAt = float.PositiveInfinity, inactivateAt = float.PositiveInfinity;
        bool entered;

        public Runner(RecoveredPreviewDemoSequence owner, MonoBehaviour source, Entry entry)
        { this.owner = owner; Source = source; this.entry = entry; }

        GameObject Object(string name)
        {
            GameObject result;
            if (objects.TryGetValue(name, out result)) return result;
            result = Read(Source, name) as GameObject;
            objects[name] = result;
            return result;
        }
        void Active(string name, bool enabled)
        {
            var target = Object(name);
            if (target != null) target.SetActive(enabled);
        }
        void SetReferences(string prefix, int first, int last)
        {
            for (int i = first; i <= last; i++)
            {
                var field = Source.GetType().GetField(prefix + i, Fields);
                var obj = Object("objeto" + i);
                if (field == null || obj == null) continue;
                var component = obj.GetComponent(field.FieldType);
                field.SetValue(Source, component);
                if (component == null) owner.MissingReferences++;
            }
        }

        public void Initialize()
        {
            foreach (var motion in entry.motions) motions[motion.method] = motion;
            foreach (var vector in entry.vectors)
            {
                vectors[vector.field] = Vector(vector.value);
                Set(Source, vector.field, Vector(vector.value));
            }
            foreach (var pose in entry.positions)
            {
                var target = Object(pose.target);
                if (target != null) target.transform.localPosition = Vector(pose.value);
                else owner.MissingReferences++;
            }
            foreach (var pose in entry.rotations)
            {
                var target = Object(pose.target);
                if (target != null) target.transform.localRotation = Quaternion.Euler(Vector(pose.value) * Mathf.Rad2Deg);
            }
            SetReferences("ctrlF", 7, 8);
            SetReferences("ft", 11, 16);
            foreach (var action in entry.active) Active(action.target, action.enabled);
            Cycle = 1;
            Set(Source, "vez", Cycle);
            Set(Source, "empezar", 0);
            Set(Source, "disabled", 0);
            if (entry.type == "Resorte2")
            {
                InactivateWaters(1); InactivateWaters(2);
                Active("aguas1", true); Active("aguas2", true);
            }
            else
            {
                ConfigureColors();
                ClearOriginalJets();
                if (entry.type == "Resorte2b") inactivateAt = Time.time + .2f;
                if (entry.type == "Resorte2c" || entry.type == "Resorte2d") UIManagerSelScript0.estadoResorte1 = 0;
            }
            ChangeMode(0, Time.time + 5f);
        }

        public void Cancel()
        { deadline = enableGateAt = inactivateAt = float.PositiveInfinity; }

        void ChangeMode(int value, float time)
        {
            mode = value; deadline = time; stage = 0; entered = false;
            owner.TransitionCount++;
            if (mode == 1) Set(Source, "empezar", 1);
            if (mode == 3) Set(Source, "empezar", 4);
        }
        void NextStage() { stage++; entered = false; owner.TransitionCount++; }
        void InactivateWaters(int index)
        {
            Active(index == 1 ? "esferaCas" : "esfera4", false);
            Active(index == 1 ? "aguas1" : "aguas2", false);
        }

        public void Tick(float now, float dt)
        {
            if (now >= inactivateAt)
            {
                Active("objeto12", false); Active("objeto13", false);
                inactivateAt = float.PositiveInfinity;
            }
            if (now >= enableGateAt)
            {
                UIManagerSelScript0.estadoResorte1 = 1;
                Set(Source, "swfin3_1", true);
                enableGateAt = float.PositiveInfinity;
            }
            if (mode == 0 || mode == 4)
            {
                if (now >= deadline)
                {
                    if (mode == 4 && entry.type == "Resorte2b") AdvanceColors();
                    ChangeMode(1, 0);
                }
                else return;
            }
            if (mode == 2)
            {
                if (now < deadline) return;
                if (entry.type == "Resorte2c" || entry.type == "Resorte2d")
                    UIManagerSelScript0.estadoResorte1 = 0;
                if (entry.type == "Resorte2c") Active("esfera4", false);
                ChangeMode(3, 0);
            }
            if (mode == 1 || mode == 3)
            {
                bool reverse = mode == 3;
                if (!entered)
                {
                    entered = true;
                    if (entry.type == "Resorte2" && ((!reverse && stage > 0) || (reverse && stage == 1)))
                        InactivateWaters(reverse ? 1 : stage);
                    if (entry.type == "Resorte2" && reverse && stage == 0) InactivateWaters(2);
                }
                bool complete = RunStage(reverse, dt);
                if (!complete) return;
                if (entry.type == "Resorte2")
                {
                    if (!reverse && stage > 0) Active("aguas" + stage, true);
                    if (reverse && stage < 2) Active("aguas" + (2 - stage), true);
                }
                NextStage();
                int count = entry.type == "Resorte2" ? 3 : entry.type == "Resorte2b" ? 4 : 2;
                if (stage < count) return;
                if (!reverse)
                {
                    if (entry.type == "Resorte2c") Active("esfera4", true);
                    if (entry.type == "Resorte2c" || entry.type == "Resorte2d") enableGateAt = now + 2f;
                    ChangeMode(2, now + (entry.type == "Resorte2d" ? 30f : 25f));
                }
                else
                {
                    owner.CompletedCycles++;
                    if (entry.type == "Resorte2c" || entry.type == "Resorte2d") AdvanceColors();
                    ChangeMode(4, now + (entry.type == "Resorte2" ? 10f : 5f));
                }
            }
        }

        void AdvanceColors()
        {
            Cycle = Cycle % 3 + 1;
            Set(Source, "vez", Cycle);
            ConfigureColors(); ClearOriginalJets();
        }

        bool RunStage(bool reverse, float dt)
        {
            string suffix = reverse ? "b" : "";
            if (entry.type == "Resorte2")
            {
                if (!reverse)
                {
                    if (stage == 0) return Move("doMover1_1", dt) & Move("doMover1_2", dt);
                    if (stage == 1) return Move("doMover2_2", dt) & Move("doRota2_1", dt);
                    return Move("doRota3_1", dt) & Move("doMover3_1", dt);
                }
                if (stage == 0) return Move("doRota3_1b", dt) & Move("doMover3_1b", dt);
                if (stage == 1) return Move("doMover2_1b", dt) & Move("doMover2_2b", dt) & Move("doRota2_1b", dt);
                return Move("doMover1_1b", dt) & Move("doMover1_2b", dt);
            }
            int number = reverse ? (entry.type == "Resorte2b" ? 4 : 2) - stage : stage + 1;
            return Move("doMover" + number + "_1" + suffix, dt);
        }

        bool Move(string method, float dt)
        {
            Motion motion;
            if (!motions.TryGetValue(method, out motion)) return false;
            bool complete = true;
            if (motion.rotation)
            {
                var obj = Object(motion.rotationObject);
                if (obj == null) return false;
                var tr = obj.transform;
                var goal = Quaternion.Euler(0, motion.rotationAngle, 0);
                float speed = Convert.ToSingle(Read(Source, "rotationSpeed"));
                var current = motion.rotationLocal ? tr.localRotation : tr.rotation;
                var result = Quaternion.RotateTowards(current, goal, speed * dt);
                if (motion.rotationLocal) tr.localRotation = result; else tr.rotation = result;
                Set(Source, "angle", motion.rotationAngle);
                Set(Source, "currentAngle", result.eulerAngles.y);
                var difference = Mathf.Abs(motion.rotationAngle - result.eulerAngles.y);
                Set(Source, "anglediferenciay", difference);
                return difference < motion.rotationEpsilon;
            }
            float distance = Convert.ToSingle(Read(Source, "moveSpeed")) * dt;
            foreach (var target in motion.targets)
            {
                int separator = target.IndexOf('_');
                var obj = Object("objeto" + target.Substring(3, separator - 3));
                Vector3 goal;
                if (obj == null || !vectors.TryGetValue(target, out goal)) return false;
                var result = Vector3.MoveTowards(obj.transform.localPosition, goal, distance);
                obj.transform.localPosition = result;
                var delta = goal - result;
                complete &= Mathf.Abs(delta.x) < Epsilon && Mathf.Abs(delta.y) < Epsilon && Mathf.Abs(delta.z) < Epsilon;
            }
            var flag = method.Replace("doMover", "swfin");
            if (flag.EndsWith("b", StringComparison.Ordinal)) flag = flag.Substring(0, flag.Length - 1);
            Set(Source, flag, complete);
            return complete;
        }

        void ConfigureColors()
        {
            Cycle selected = null;
            foreach (var cycle in entry.cycles) if (cycle.cycle == Cycle) { selected = cycle; break; }
            if (selected == null) return;
            Active("objeto7", false); Active("objeto8", false);
            foreach (var value in selected.values)
            {
                object target = string.IsNullOrEmpty(value.target) ? Source : Read(Source, value.target);
                if (value.target == "$static.UIManagerScript")
                {
                    if (value.field == "confColor1") UIManagerScript.confColor1 = value.number;
                    if (value.field == "confColor2") UIManagerScript.confColor2 = value.number;
                    continue;
                }
                if (target == null) { owner.MissingReferences++; continue; }
                object data = value.type == "Color" ? (object)Rgba(value.rgba) : value.type == "int" ? (object)value.number : value.text;
                Set(target, value.field, data);
            }
            foreach (var action in selected.actions)
            {
                if (action.kind == "active") { Active(action.target, action.enabled); continue; }
                if (action.kind != "material") continue;
                var authored = Read(Source, action.target) as Material;
                if (authored == null) continue;
                // Native modifies its referenced materials; instance copies keep
                // those changes local to this category, leaving source assets intact.
                var material = owner.Localize(authored);
                material.color = Rgba(action.color);
            }
            RecoveredNativePalette.ApplyHierarchy(owner.transform, entry.type == "Resorte2d");
        }

        void ClearOriginalJets()
        {
            // Native removes these existing baked child jets, never the origin
            // components. Preserve the hierarchy so future graph rebuilds can
            // still read their authored geometry/configuration.
            if (entry.type == "Resorte2b")
            {
                HideChild("objeto9", 1); HideChild("objeto10", 0); HideChild("objeto14", 0);
                var door = Read(Source, "anim_door") as Animator;
                if (door != null && door.runtimeAnimatorController != null) door.SetBool("close", true);
            }
            else if (entry.type == "Resorte2c")
            {
                HideChild("objeto9", 0); HideChild("objeto9", 1); HideChild("objeto9", 2);
                HideChild("objeto10", 0); HideChild("objeto10", 1);
                HideChild("objeto11", 0); HideChild("objeto11", 1);
                HideChild("objeto12", 0); HideChild("objeto12", 1);
            }
        }
        void HideChild(string name, int index)
        {
            var target = Object(name);
            if (target == null || target.transform.childCount <= index) return;
            var child = target.transform.GetChild(index);
            // Exported prefabs can omit the transient jet and leave a different
            // child at that index. Only suppress an existing water visual.
            string label = child.name.ToLowerInvariant();
            bool waterName = label.Contains("agua") || label.Contains("chorro") || label.Contains("cascada") || label.Contains("water");
            if (waterName && child.GetComponent<Renderer>() != null && child.GetComponent<FuenteTest3>() == null &&
                child.GetComponent<FuenteTest1>() == null && child.GetComponent<AguaControl>() == null &&
                child.GetComponent<AguaControlC>() == null) child.gameObject.SetActive(false);
        }
    }
}
