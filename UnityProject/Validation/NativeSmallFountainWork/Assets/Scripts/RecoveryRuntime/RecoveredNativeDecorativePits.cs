using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Recovered.FlowWater
{
    /// <summary>Original wet FT3 decorative-pit instances and exact child mutations.</summary>
    public sealed class RecoveredNativeDecorativePits : MonoBehaviour
    {
        [Serializable] sealed class Catalog { public Entry[] entries; public RecipeSet[] recipeSets; }
        [Serializable] sealed class Entry { public string key; public int set; }
        [Serializable] sealed class RecipeSet { public Recipe[] recipes; }
        [Serializable] sealed class Recipe
        {
            public string prefabField, nativeInstantiateVA;
            public float[] position, rotation, scale;
            public Operation[] operations;
        }
        [Serializable] sealed class Operation
        {
            public int[] childIndices;
            public string kind, component, field, parameter, nativeVA;
            public int integer;
            public bool enabled;
            public float number;
            public float[] rgba;
        }
        sealed class AnimatorState
        {
            public Animator animator;
            public bool enabled;
            public readonly List<Operation> integers = new List<Operation>();
        }
        static Catalog catalog;
        static Dictionary<string, RecipeSet> recipes;
        public static readonly HashSet<string> MissingConfigurations = new HashSet<string>();
        readonly List<AnimatorState> animators = new List<AnimatorState>();
        Recipe nativeRecipe;
        public string NativeInstantiateVA { get; private set; }
        public string NativePrefabField { get; private set; }
        public string NativeConfiguration { get; private set; }
        public int NativeAnimatorCode { get; private set; }
        public bool HasVelocityMutation { get; private set; }

        static void Load()
        {
            if (recipes != null) return;
            recipes = new Dictionary<string, RecipeSet>(StringComparer.Ordinal);
            TextAsset data = Resources.Load<TextAsset>("RecoveredNativeDecorativePits");
            if (data == null) return;
            catalog = JsonUtility.FromJson<Catalog>(data.text);
            if (catalog == null || catalog.entries == null || catalog.recipeSets == null) return;
            foreach (Entry entry in catalog.entries)
                if (entry.set >= 0 && entry.set < catalog.recipeSets.Length) recipes[entry.key] = catalog.recipeSets[entry.set];
        }
        public static void Restore(MonoBehaviour origin, object control, int color, Func<GameObject, Transform, GameObject> spawn)
        {
            // FT3.doInstanciarFuente invokes this provider after the type2
            // primary/border/center providers. E routing jets are separate.
            string pit = Text(control, "configPit1");
            if (String.IsNullOrEmpty(pit) || pit == "5000" || pit == "6000") return;
            string lights = Text(control, "configLights");
            if (String.IsNullOrEmpty(lights)) lights = Text(control, "configLi");
            // FT3.Start's exact native night gate is parte36..39. P/U
            // gradient scripts have their own, different menu-night gate.
            bool night = UIManagerSelScript0.parte >= 36 && UIManagerSelScript0.parte <= 39;
            string key = String.Join("|", pit, Text(control, "configPit2"), Text(control, "configPit3"),
                Text(control, "configPit4"), Text(control, "configAnim"), lights, color.ToString(), night ? "1" : "0");
            Load(); RecipeSet set;
            if (!recipes.TryGetValue(key, out set)) { MissingConfigurations.Add(key); return; }
            Spawn(set, origin, key, spawn);
        }
        public static void RestoreSmall(MonoBehaviour origin, object control, int color, Func<GameObject, Transform, GameObject> spawn)
        {
            int kind = Number(control, "tipoPieza");
            if (kind != 3 && kind != 4) return;
            // Every actual type3/4 configuration has these original defaults.
            // Do not silently apply the empty-pit branch to a different config.
            if (!String.IsNullOrEmpty(Text(control, "configPit1")) || Number(control, "configFuente") != 0 ||
                Number(control, "configFuenteCentro") != 0) return;
            string key = "small|" + kind + "|" + (Number(origin, "dirPlano1") == 1 ? "1" : "0") +
                "|" + (Number(origin, "dirPlano2") == 1 ? "1" : "0") + "|" + color;
            Load(); RecipeSet set;
            if (!recipes.TryGetValue(key, out set)) { MissingConfigurations.Add(key); return; }
            Spawn(set, origin, key, spawn);
        }
        public static void RestoreWetLight(MonoBehaviour origin, object control, Func<GameObject, Transform, GameObject> spawn)
        {
            string code = Text(control, "configLights");
            if (String.IsNullOrEmpty(code)) code = Text(control, "configLi");
            if (String.IsNullOrEmpty(code)) return;
            string key = "wetlight|" + code;
            Load(); RecipeSet set;
            if (!recipes.TryGetValue(key, out set)) { MissingConfigurations.Add(key); return; }
            Spawn(set, origin, key, spawn);
        }
        static void Spawn(RecipeSet set, MonoBehaviour origin, string key, Func<GameObject, Transform, GameObject> spawn)
        {
            foreach (Recipe recipe in set.recipes)
            {
                GameObject prefab = Reference<GameObject>(origin, recipe.prefabField);
                if (prefab == null) continue;
                GameObject instance = spawn(prefab, origin.transform);
                instance.transform.localPosition = Vector(recipe.position);
                instance.transform.rotation = origin.transform.rotation * Rotation(recipe.rotation);
                if (recipe.scale != null && recipe.scale.Length == 3) instance.transform.localScale = Vector(recipe.scale);
                else WorldScale(instance.transform, prefab.transform.localScale);
                RecoveredNativeDecorativePits binding = instance.AddComponent<RecoveredNativeDecorativePits>();
                binding.Initialize(recipe, key);
            }
        }
        void Initialize(Recipe recipe, string configuration)
        {
            nativeRecipe = recipe; NativeInstantiateVA = recipe.nativeInstantiateVA; NativePrefabField = recipe.prefabField; NativeConfiguration = configuration;
            foreach (Animator animator in GetComponentsInChildren<Animator>(true))
                animators.Add(new AnimatorState { animator = animator, enabled = animator.enabled });
            if (recipe.operations == null) return;
            foreach (Operation operation in recipe.operations)
            {
                Transform target = Target(operation.childIndices);
                if (target == null) continue;
                if (operation.kind == "animatorInteger" || operation.kind == "componentEnabled" && operation.component == "Animator")
                {
                    Animator animator = target.GetComponent<Animator>();
                    foreach (AnimatorState state in animators)
                        if (state.animator == animator)
                        {
                            if (operation.kind == "componentEnabled") state.enabled = operation.enabled;
                            else { state.integers.Add(operation); NativeAnimatorCode = operation.integer; }
                        }
                }
                else Apply(operation, target);
            }
        }
        Transform Target(int[] indices)
        {
            Transform target = transform;
            if (indices != null)
                foreach (int index in indices)
                { if (index < 0 || index >= target.childCount) return null; target = target.GetChild(index); }
            return target;
        }
        void Apply(Operation operation, Transform target)
        {
            if (operation.kind == "velocityConstant")
            {
                ParticleSystem particles = target.GetComponent<ParticleSystem>();
                if (particles == null) return;
                var velocity = particles.velocityOverLifetime;
                if (operation.field == "x") velocity.x = operation.number;
                else if (operation.field == "y") velocity.y = operation.number;
                else if (operation.field == "z") velocity.z = operation.number;
                HasVelocityMutation = true;
            }
            else if (operation.kind == "componentField")
            {
                Component component = operation.component == "changeColorP" ? (Component)target.GetComponent<changeColorP>() :
                    operation.component == "changeColorU" ? target.GetComponent<changeColorU>() : null;
                if (component == null) return;
                FieldInfo field = component.GetType().GetField(operation.field);
                if (field != null && field.FieldType == typeof(int)) field.SetValue(component, operation.integer);
            }
            else if (operation.kind == "lightColor")
            {
                Light light = target.GetComponent<Light>(); float[] c = operation.rgba;
                if (light != null && c != null && c.Length == 4) light.color = new Color(c[0], c[1], c[2], c[3]);
            }
            else if (operation.kind == "lightFloat")
            {
                Light light = target.GetComponent<Light>();
                if (light == null) return;
                if (operation.field == "intensity") light.intensity = operation.number;
                else if (operation.field == "range") light.range = operation.number;
                else if (operation.field == "spotAngle") light.spotAngle = operation.number;
            }
            else if (operation.kind == "active") target.gameObject.SetActive(operation.enabled);
        }
        public static bool TryRestore(Animator animator, float age)
        {
            RecoveredNativeDecorativePits binding = animator.GetComponentInParent<RecoveredNativeDecorativePits>(true);
            if (binding == null) return false;
            foreach (AnimatorState state in binding.animators)
            {
                if (state.animator != animator) continue;
                animator.enabled = state.enabled; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                if (!state.enabled || !animator.gameObject.activeInHierarchy || animator.runtimeAnimatorController == null) return true;
                animator.Rebind();
                foreach (Operation integer in state.integers) animator.SetInteger(integer.parameter, integer.integer);
                // Preserve the original script-configured initial velocity
                // before the original animation evaluates its actual age.
                if (binding.nativeRecipe.operations != null)
                    foreach (Operation operation in binding.nativeRecipe.operations)
                        if (operation.kind == "velocityConstant")
                        { Transform target = binding.Target(operation.childIndices); if (target != null) binding.Apply(operation, target); }
                animator.Update(Mathf.Max(0, age)); return true;
            }
            return false;
        }
        static Vector3 Vector(float[] v) { return v == null || v.Length < 3 ? Vector3.zero : new Vector3(v[0], v[1], v[2]); }
        static Quaternion Rotation(float[] v) { return v == null || v.Length < 4 ? Quaternion.identity : new Quaternion(v[0], v[1], v[2], v[3]); }
        static void WorldScale(Transform target, Vector3 desired)
        {
            Vector3 scale = target.parent == null ? Vector3.one : target.parent.lossyScale;
            target.localScale = new Vector3(desired.x / Mathf.Max(.0001f, Mathf.Abs(scale.x)), desired.y / Mathf.Max(.0001f, Mathf.Abs(scale.y)), desired.z / Mathf.Max(.0001f, Mathf.Abs(scale.z)));
        }
        static object Value(object target, string name) { if (target == null) return null; FieldInfo field = target.GetType().GetField(name); return field == null ? null : field.GetValue(target); }
        static string Text(object target, string name) { return Value(target, name) as string ?? ""; }
        static int Number(object target, string name) { object value = Value(target, name); return value is int ? (int)value : 0; }
        static T Reference<T>(object target, string name) where T : UnityEngine.Object { return Value(target, name) as T; }
    }
}
