// Original methods: C.Start1082F3C..94, C.doConfigureColors1088ED4,
// Fuente.Start10901A4..C0, Fuente.doConfigureColors1091F3C and doColor1/2.
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Recovered.FlowWater
{
    public sealed class RecoveredNativePalette : MonoBehaviour
    {
        [Serializable] sealed class Catalog { public Recipe[] cases; }
        [Serializable] sealed class Recipe
        {
            public string type, configuration;
            public bool night;
            public int color, confC;
            public float[] color1, color2, color3;
            public MaterialChange[] materials;
        }
        [Serializable] sealed class MaterialChange { public string field; public float[] color; }
        const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static Dictionary<string, Recipe> recipes;
        readonly Dictionary<Material, Material> instances = new Dictionary<Material, Material>();
        readonly Dictionary<MonoBehaviour,string> applied = new Dictionary<MonoBehaviour,string>();

        // Calling before geometry makes its material lookups use local palette
        // instances. Calling again after geometry binds newly constructed meshes.
        public static void Apply(RecoveryPiece piece)
        {
            if (piece == null) return;
            Transform scope=piece.transform.parent==null?piece.transform:piece.transform.parent;
            RecoveredNativePalette owner = scope.GetComponent<RecoveredNativePalette>();
            if (owner == null) owner = scope.gameObject.AddComponent<RecoveredNativePalette>();
            owner.ApplyControllers(piece.transform, piece, null);
            // Native set_color mutates a shared material across all pieces.
            // Keep that aliasing within this board using one shared local copy.
            owner.RebindMaterials(scope.gameObject);
        }
        public static void ApplyHierarchy(Transform root)
        { ApplyHierarchy(root,null); }
        public static void ApplyHierarchy(Transform root,bool? nightOverride)
        {
            if (root == null) return;
            RecoveredNativePalette owner = root.GetComponent<RecoveredNativePalette>();
            if (owner == null) owner = root.gameObject.AddComponent<RecoveredNativePalette>();
            owner.ApplyControllers(root, null, nightOverride);
            owner.RebindMaterials(root.gameObject);
        }
        // Water meshes can be instantiated after the board geometry pass.
        public static void ApplyMaterials(RecoveryPiece piece, GameObject instance)
        {
            if (piece == null || instance == null) return;
            RecoveredNativePalette owner = piece.GetComponentInParent<RecoveredNativePalette>();
            if (owner != null) owner.RebindMaterials(instance);
        }

        void ApplyControllers(Transform root, RecoveryPiece piece, bool? nightOverride)
        {
            EnsureCatalog();
            foreach (MonoBehaviour source in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (source == null || (!(source is ControlPiezaC) && !(source is ControlPiezaFuente))) continue;
                if (piece != null && source.transform == piece.transform && piece.ColorVariant >= 1 && piece.ColorVariant <= 4)
                    Set(source, "color", piece.ColorVariant);
                int color = Integer(source, "color");
                // Both original Starts call the palette only for color3/4.
                if (color != 3 && color != 4) continue;
                if (source is ControlPiezaC)
                {
                    GameObject body = Get(source, "objeto") as GameObject;
                    string nativeName = body == null || body.transform.parent == null ? "" : body.transform.parent.name;
                    if (piece != null && body != null && body.transform.parent == piece.transform) nativeName = piece.BasePrefab ?? nativeName;
                    // C.Start additionally restricts the call to FuenteED.
                    if (nativeName.IndexOf("FuenteED", StringComparison.Ordinal) < 0) continue;
                }
                bool night = source is ControlPiezaFuente && (nightOverride ?? ((UIManagerSelScript0.parte & ~3) == 36));
                string type = source.GetType().Name, code = Get(source, "confColor") as string ?? "";
                string key=Key(type,code,color,night);
                if(applied.TryGetValue(source,out string previous) && previous==key) continue;
                Recipe recipe;
                if (!recipes.TryGetValue(Key(type, code, color, night), out recipe) &&
                    !recipes.TryGetValue(Key(type, "", color, night), out recipe)) continue;
                applied[source]=key;
                if (night)
                {
                    Shader shader = Get(source, "shader1") as Shader;
                    foreach (string field in new[] { "material1", "material2", "material3", "material4", "material5", "material7" })
                    {
                        Material original = Get(source, field) as Material;
                        if (original == null) continue;
                        Material local = Local(original); Set(source, field, local);
                        if (shader != null) local.shader = shader;
                    }
                }
                Set(source, "confC", recipe.confC);
                Set(source, "color1", ColorValue(recipe.color1));
                Set(source, "color2", ColorValue(recipe.color2));
                Set(source, "color3", ColorValue(recipe.color3));
                if (recipe.materials != null) foreach (MaterialChange change in recipe.materials)
                {
                    Material original = Get(source, change.field) as Material;
                    if (original == null) continue;
                    Material local = Local(original); Set(source, change.field, local);
                    local.color = ColorValue(change.color);
                }
                if (color == 3) UIManagerScript.confColor1 = recipe.confC;
                else UIManagerScript.confColor2 = recipe.confC;
            }
        }
        Material Local(Material original)
        {
            if (original == null) return null;
            foreach (Material local in instances.Values) if (local == original) return original;
            if (!instances.TryGetValue(original, out Material result))
            {
                result = new Material(original) { name = original.name + " (native palette instance)" };
                instances.Add(original, result);
            }
            return result;
        }
        void RebindMaterials(GameObject root)
        {
            if (instances.Count == 0 || root == null) return;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] list = renderer.sharedMaterials; bool changed = false;
                for (int i = 0; i < list.Length; i++) if (list[i] != null && instances.TryGetValue(list[i], out Material local)) { list[i] = local; changed = true; }
                if (changed) renderer.sharedMaterials = list;
            }
            // Original shared material aliases also occur on the child
            // colorCilindro/water scripts created by the native geometry recipes.
            foreach (MonoBehaviour source in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (source == null || source == this) continue;
                foreach (FieldInfo field in source.GetType().GetFields(Fields))
                {
                    if (field.IsInitOnly) continue;
                    if (field.FieldType == typeof(Material))
                    {
                        Material original = field.GetValue(source) as Material;
                        if (original != null && instances.TryGetValue(original, out Material local)) field.SetValue(source, local);
                    }
                    else if (field.FieldType == typeof(Material[]))
                    {
                        Material[] list = field.GetValue(source) as Material[]; if (list == null) continue;
                        Material[] copy = (Material[])list.Clone(); bool changed = false;
                        for (int i = 0; i < copy.Length; i++) if (copy[i] != null && instances.TryGetValue(copy[i], out Material local)) { copy[i] = local; changed = true; }
                        if (changed) field.SetValue(source, copy);
                    }
                }
            }
        }
        static void EnsureCatalog()
        {
            if (recipes != null) return;
            recipes = new Dictionary<string, Recipe>(StringComparer.Ordinal);
            TextAsset text = Resources.Load<TextAsset>("RecoveredNativePalette");
            if (text == null) { Debug.LogError("Missing original palette recipes."); return; }
            Catalog catalog = JsonUtility.FromJson<Catalog>(text.text);
            if (catalog == null || catalog.cases == null) return;
            foreach (Recipe recipe in catalog.cases) recipes[Key(recipe.type, recipe.configuration, recipe.color, recipe.night)] = recipe;
        }
        static string Key(string type, string configuration, int color, bool night) { return type + "\n" + configuration + "\n" + color + "\n" + night; }
        static Color ColorValue(float[] value) { return value == null || value.Length != 4 ? Color.clear : new Color(value[0], value[1], value[2], value[3]); }
        static object Get(object source, string name) { FieldInfo field = source == null ? null : source.GetType().GetField(name, Fields); return field == null ? null : field.GetValue(source); }
        static int Integer(object source, string name) { object value = Get(source, name); return value is int ? (int)value : 0; }
        static void Set(object source, string name, object value) { FieldInfo field = source == null ? null : source.GetType().GetField(name, Fields); if (field != null) field.SetValue(source, value); }
        void OnDestroy() { foreach (Material material in instances.Values) if (material != null) { if(Application.isPlaying) Destroy(material); else DestroyImmediate(material); } instances.Clear(); }
    }
}
