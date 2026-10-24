using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Recovered.FlowWater
{
    /// <summary>Placement and UV tables evaluated from the original ARM64 methods.</summary>
    public static class RecoveredNativeWaterAssets
    {
        [Serializable] sealed class FountainCatalog { public FountainCase[] entries; }
        [Serializable] sealed class FountainCase
        {
            public string family;
            public int configuration, color, centerMode, pitMode, animation;
            public Recipe[] recipes;
        }
        [Serializable] sealed class Recipe
        {
            public string prefabField, nativeInstantiateVA;
            public float[] position, rotation, scale;
        }
        [Serializable] sealed class SurfaceCatalog { public SurfaceCase[] cases; }
        [Serializable] sealed class SurfaceCase { public int origen, dirPlanoCentro; public Mutation[] mutations; }
        [Serializable] sealed class Mutation
        {
            public string existingField;
            public float[] rotation;
            public float scrollSpeed_X, scrollSpeed_Y, offsetX;
            public bool active;
        }
        [Serializable] sealed class FlowCatalog { public FlowCase[] cases; }
        [Serializable] sealed class FlowCase { public int phase, origen; public FlowRecipe[] recipes; }
        [Serializable] sealed class FlowRecipe
        {
            public string prefabField;
            public float[] localPosition, quaternion, localScale;
        }
        static FountainCase[] fountains;
        static SurfaceCase[] surfaces;
        static FlowCase[] flows;
        static readonly string[] ColorNames = { "", "Azul", "Verde", "Z", "Ro" };
        static T Load<T>(string path) where T : class
        {
            TextAsset asset = Resources.Load<TextAsset>(path);
            return asset == null ? null : JsonUtility.FromJson<T>(asset.text);
        }
        public static void RestoreFountains(MonoBehaviour origin, object control, int color, Func<GameObject, Transform, GameObject> spawn)
        {
            if (fountains == null)
            {
                FountainCatalog catalog = Load<FountainCatalog>("RecoveredNativeWaterRecipes");
                fountains = catalog == null ? new FountainCase[0] : catalog.entries;
            }
            int center = Number(control, "configFuenteCentro");
            int mode = center == 8 || center == 9 ? center : 0;
            string pit = Text(control, "configPit1");
            int pitMode = !String.IsNullOrEmpty(pit) && (pit[0] == '5' || pit[0] == '6') ? pit[0] - '0' : 0;
            int animation = String.IsNullOrEmpty(Text(control, "configAnim")) ? 0 : 1;
            string animationCode = Text(control, "configAnim");
            ApplyFamily(origin, "primary", Number(control, "configFuente"), color, mode, pitMode, 0, spawn, animationCode);
            ApplyFamily(origin, "border", Number(control, "configFuenteB"), color, center == 7 ? 7 : mode, 0, 0, spawn, animationCode);
            ApplyFamily(origin, "center", center, color, 0, 0, animation, spawn, animationCode);
            RecoveredNativeDecorativePits.Restore(origin, control, color, spawn);
        }
        static void ApplyFamily(MonoBehaviour origin, string family, int configuration, int color, int center, int pit, int animation, Func<GameObject, Transform, GameObject> spawn, string animationCode)
        {
            if (configuration <= 0) return;
            foreach (FountainCase entry in fountains)
            {
                if (entry.family != family || entry.configuration != configuration || entry.color != color || entry.centerMode != center || entry.pitMode != pit || entry.animation != animation) continue;
                foreach (Recipe recipe in entry.recipes)
                {
                    GameObject prefab = Reference<GameObject>(origin, recipe.prefabField);
                    if (prefab == null) continue;
                    GameObject instance = spawn(prefab, origin.transform);
                    RecoveredNativeFountainAnimation.Bind(instance, recipe.nativeInstantiateVA, animationCode);
                    instance.transform.localPosition = Vector(recipe.position);
                    instance.transform.rotation = origin.transform.rotation * Rotation(recipe.rotation);
                    if (recipe.scale != null && recipe.scale.Length == 3) instance.transform.localScale = Vector(recipe.scale);
                    else WorldScale(instance.transform, prefab.transform.localScale);
                }
                break;
            }
        }
        public static bool RestoreCenter(MonoBehaviour origin, Transform basis, int incoming, int direction, Material material)
        {
            if (surfaces == null)
            {
                SurfaceCatalog catalog = Load<SurfaceCatalog>("RecoveredNativeWaterSurfaces");
                surfaces = catalog == null ? new SurfaceCase[0] : catalog.cases;
            }
            foreach (SurfaceCase entry in surfaces)
            {
                if (entry.origen != incoming || entry.dirPlanoCentro != direction) continue;
                foreach (Mutation mutation in entry.mutations)
                {
                    if (!mutation.existingField.StartsWith("planoCentro", StringComparison.Ordinal)) continue;
                    GameObject surface = Reference<GameObject>(origin, mutation.existingField);
                    if (surface == null) continue;
                    surface.SetActive(mutation.active);
                    surface.transform.rotation = basis.rotation * Rotation(mutation.rotation);
                    Renderer renderer = surface.GetComponent<Renderer>();
                    if (renderer != null) { renderer.enabled = true; if (material != null) renderer.sharedMaterial = material; }
                    scrollTest1 scroll = surface.GetComponent<scrollTest1>();
                    if (scroll != null) { scroll.scrollSpeed_X = mutation.scrollSpeed_X; scroll.scrollSpeed_Y = mutation.scrollSpeed_Y; scroll.offsetInicialX = mutation.offsetX; }
                }
                return true;
            }
            return false;
        }
        public static void RestoreFlow(MonoBehaviour origin, int incoming, int color, Func<GameObject, Transform, GameObject> spawn, float age = 0)
        {
            if (origin is FuenteTest1 && incoming >= 5 && incoming <= 6)
            {
                RestoreNormalVerticalFlow(origin, incoming, color, spawn);
                return;
            }
            if (!(origin is FuenteTest3) || incoming < 1 || incoming > 6) return;
            if (flows == null)
            {
                FlowCatalog catalog = Load<FlowCatalog>("RecoveredNativeWaterFlows");
                flows = catalog == null ? new FlowCase[0] : catalog.cases;
            }
            float delay = Mathf.Max(0, Scalar(origin, incoming <= 4 ? "espera1" : "espera2"));
            foreach (FlowCase entry in flows)
            {
                if (entry.origen != incoming) continue;
                if (entry.phase == 0 && incoming <= 4 && age >= delay + 2) continue;
                foreach (FlowRecipe recipe in entry.recipes)
                {
                    // Native side travel branches exist only where the static
                    // side plane has dirPlano=0 (FuenteTest3.doInstanciarAgua).
                    if (recipe.prefabField == "prefabAguaC" && Number(origin, "dirPlano" + (incoming % 4 + 1)) != 0) continue;
                    if (recipe.prefabField == "prefabAguaD" && Number(origin, "dirPlano" + ((incoming + 2) % 4 + 1)) != 0) continue;
                    string field = recipe.prefabField == "prefabAgua" ? "prefabAgua" + color :
                        recipe.prefabField == "prefabAguaB" ? "prefabAguaB" + color : recipe.prefabField + ColorNames[color];
                    GameObject prefab = Reference<GameObject>(origin, field);
                    if (prefab == null) continue;
                    GameObject instance = spawn(prefab, origin.transform);
                    instance.transform.localPosition = Vector(recipe.localPosition);
                    instance.transform.rotation = origin.transform.rotation * Rotation(recipe.quaternion);
                    if (recipe.localScale != null && recipe.localScale.Length == 3) instance.transform.localScale = Vector(recipe.localScale);
                    else WorldScale(instance.transform, prefab.transform.localScale);
                    if (entry.phase == 1) RecoveredWaterArrival.ActivateAfter(origin.transform, instance, delay - age);
                    // doInstanciarAgua1 invokes doDestroyAgua after two seconds
                    // only for side entries. Vertical Agua1 remains authored.
                    if (entry.phase == 0 && incoming <= 4) UnityEngine.Object.Destroy(instance, delay + 2 - age);
                }
            }
        }
        static void RestoreNormalVerticalFlow(MonoBehaviour origin, int incoming, int color, Func<GameObject, Transform, GameObject> spawn)
        {
            // FuenteTest1.doInstanciarAgua 0x10A06C8..0x10A0968 creates
            // the original animated inlet for both vertical directions.
            // doTratarDirecciones deliberately skips the static center plane
            // for these inputs, so the inlet must supply that missing water.
            GameObject prefab = Reference<GameObject>(origin, "prefabAgua" + color);
            if (prefab == null) return;
            GameObject instance = spawn(prefab, origin.transform);
            instance.transform.localPosition = new Vector3(8.64f, -1.35f, 0);
            float yaw = 0;
            if (incoming == 5)
            {
                // FT1 quantizes its world-facing intake marker to rotay;
                // vertical6 instead uses a literal world -90-degree pose.
                GameObject marker = Reference<GameObject>(origin, "esfera1");
                if (marker != null)
                {
                    Vector3 facing = marker.transform.position - origin.transform.position;
                    if (facing.x > -.1f)
                    {
                        yaw = facing.z < .1f ? -90 : 90;
                        if (facing.x > .1f) yaw = 180;
                    }
                }
            }
            instance.transform.rotation = Quaternion.Euler(0, yaw - 90, 0);
            instance.transform.localScale = new Vector3(2.31f, 2.1f, 1.5f);
        }
        static Vector3 Vector(float[] values) { return values == null || values.Length < 3 ? Vector3.zero : new Vector3(values[0], values[1], values[2]); }
        static Quaternion Rotation(float[] values) { return values == null || values.Length < 4 ? Quaternion.identity : new Quaternion(values[0], values[1], values[2], values[3]); }
        static void WorldScale(Transform target, Vector3 desired)
        {
            Vector3 scale = target.parent == null ? Vector3.one : target.parent.lossyScale;
            target.localScale = new Vector3(desired.x / Mathf.Max(.0001f, Mathf.Abs(scale.x)), desired.y / Mathf.Max(.0001f, Mathf.Abs(scale.y)), desired.z / Mathf.Max(.0001f, Mathf.Abs(scale.z)));
        }
        static object Value(object target, string name) { FieldInfo field = target.GetType().GetField(name); return field == null ? null : field.GetValue(target); }
        static int Number(object target, string name) { object value = Value(target, name); return value is int ? (int)value : 0; }
        static float Scalar(object target, string name) { object value = Value(target, name); return value is float ? (float)value : 0; }
        static string Text(object target, string name) { return Value(target, name) as string; }
        static T Reference<T>(object target, string name) where T : UnityEngine.Object { return Value(target, name) as T; }
    }
}
