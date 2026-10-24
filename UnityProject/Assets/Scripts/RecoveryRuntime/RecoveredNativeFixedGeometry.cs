// Original ControlFijo1.Start107ADB0..107DBD8 construction and SetActive pass.
// All 243 gm/so contexts were statically interpreted with zero extraction failures.
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Recovered.FlowWater
{
    public static class RecoveredNativeFixedGeometry
    {
        const string Marker = "__Recovered Native Fixed Geometry";
        const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        [Serializable] sealed class Catalog { public Entry[] entries; public RecipeSet[] recipeSets; }
        [Serializable] sealed class Entry { public string basePrefab, configuration, path; public int recipeSet; }
        [Serializable] sealed class RecipeSet { public Recipe[] recipes; public Change[] changes; }
        [Serializable] sealed class ChildMaterial { public int childIndex; public string materialField; }
        [Serializable] sealed class Recipe
        {
            public string prefabField, instanceField, materialField, tag;
            public float[] position, rotation, scale;
            public bool hasActive, active, rotationIsLocal;
            public ChildMaterial[] childMaterials;
        }
        [Serializable] sealed class Change { public string target, materialField, tag; public bool hasActive, active; }
        static Dictionary<string, RecipeSet> recipes;
        static readonly Dictionary<string, FieldInfo> fields = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        static readonly Dictionary<int, Collider[]> authoredColliders = new Dictionary<int, Collider[]>();
        static readonly Dictionary<Collider, bool> originalColliderEnabled = new Dictionary<Collider, bool>();
        static Transform levelRoot;

        public static void Apply(RecoveryPiece piece)
        {
            if (piece == null) return;
            if (levelRoot != piece.transform.parent) { levelRoot = piece.transform.parent; authoredColliders.Clear(); originalColliderEnabled.Clear(); }
            EnsureCatalog();
            foreach (ControlFijo1 source in piece.GetComponentsInChildren<ControlFijo1>(true))
            {
                if (source.transform.Find(Marker) != null) continue;
                RecipeSet set = Find(piece, source);
                if (set == null) { Debug.LogError("Missing original fixed geometry recipe: " + piece.BasePrefab + " / " + piece.OriginalConfiguration); continue; }
                if (source.transform == piece.transform) SynchronizeWallFields(piece, source);
                new GameObject(Marker).transform.SetParent(source.transform, false);
                var colliders = new List<Collider>();
                if (set.recipes != null) foreach (Recipe recipe in set.recipes) Spawn(source, recipe, colliders);
                if (set.changes != null) foreach (Change change in set.changes) ApplyChange(source, change);
                authoredColliders[source.GetInstanceID()] = colliders.ToArray();
            }
        }

        // These are the colliders enabled in the original spawned prefabs.
        // The recovery renderer disables simulation, but native geometry
        // queries can temporarily enable these exact components and layers.
        public static IReadOnlyList<Collider> OriginalCollidersFor(ControlFijo1 source)
        {
            if (source != null && authoredColliders.TryGetValue(source.GetInstanceID(), out Collider[] colliders)) return colliders;
            return new Collider[0];
        }
        public static bool TryGetOriginalColliderEnabled(Collider collider, out bool enabled)
        { if (collider != null) return originalColliderEnabled.TryGetValue(collider, out enabled); enabled = false; return false; }

        static void SynchronizeWallFields(RecoveryPiece piece, ControlFijo1 source)
        {
            // doProcesoPiezas101A62C..101A640 packs four int assignments.
            string code = piece.OriginalConfiguration ?? "";
            int start = code.IndexOf("sr", StringComparison.Ordinal);
            if (start < 0) return;
            start += 2; int end = code.IndexOf("er", start, StringComparison.Ordinal);
            if (end < start + 4) return;
            for (int i = 0; i < 4; i++)
            {
                int side = code[start+i] - '0';
                if (side >= 0 && side <= 9) Set(source, "lado" + (i+1), side);
            }
        }
        static void EnsureCatalog()
        {
            if (recipes != null) return;
            recipes = new Dictionary<string, RecipeSet>(StringComparer.Ordinal);
            TextAsset asset = Resources.Load<TextAsset>("RecoveredNativeFixedGeometry");
            if (asset == null) { Debug.LogError("Missing original ControlFijo1 geometry recipes."); return; }
            Catalog catalog = JsonUtility.FromJson<Catalog>(asset.text);
            if (catalog == null || catalog.entries == null || catalog.recipeSets == null) return;
            foreach (Entry entry in catalog.entries)
                if (entry.recipeSet >= 0 && entry.recipeSet < catalog.recipeSets.Length)
                    recipes[Key(entry.basePrefab, entry.configuration, entry.path)] = catalog.recipeSets[entry.recipeSet];
        }
        static RecipeSet Find(RecoveryPiece piece, ControlFijo1 source)
        {
            string path = ""; Transform current = source.transform;
            while (current != piece.transform)
            {
                if (current == null) return null;
                path = path.Length == 0 ? current.name : current.name + "/" + path;
                current = current.parent;
            }
            recipes.TryGetValue(Key(piece.BasePrefab, piece.OriginalConfiguration, path), out RecipeSet result);
            return result;
        }
        static string Key(string prefab, string configuration, string path) { return (prefab ?? "") + "\n" + (configuration ?? "") + "\n" + (path ?? ""); }
        static void Spawn(ControlFijo1 source, Recipe recipe, List<Collider> colliders)
        {
            GameObject prefab = Get(source, recipe.prefabField) as GameObject;
            if (prefab == null) return;
            Quaternion rotation = recipe.rotation != null && recipe.rotation.Length == 4 ?
                new Quaternion(recipe.rotation[0], recipe.rotation[1], recipe.rotation[2], recipe.rotation[3]) : Quaternion.identity;
            GameObject instance = UnityEngine.Object.Instantiate(prefab, Vector3.zero, rotation);
            foreach (MonoBehaviour script in instance.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
            { originalColliderEnabled[collider] = collider.enabled; if (collider.enabled) colliders.Add(collider); collider.enabled = false; }
            foreach (Rigidbody body in instance.GetComponentsInChildren<Rigidbody>(true)) { body.isKinematic = true; body.detectCollisions = false; }
            foreach (AudioSource audio in instance.GetComponentsInChildren<AudioSource>(true)) { audio.Stop(); audio.enabled = false; }
            instance.transform.SetParent(source.transform, true);
            if (recipe.rotationIsLocal) instance.transform.localRotation = rotation;
            if (recipe.position != null && recipe.position.Length == 3) instance.transform.localPosition = Vector(recipe.position);
            if (recipe.scale != null && recipe.scale.Length == 3) instance.transform.localScale = Vector(recipe.scale);
            Set(source, recipe.instanceField, instance);
            AssignMaterial(instance, Get(source, recipe.materialField) as Material);
            if (recipe.childMaterials != null)
                foreach (ChildMaterial child in recipe.childMaterials)
                    if (child.childIndex >= 0 && child.childIndex < instance.transform.childCount)
                        AssignMaterial(instance.transform.GetChild(child.childIndex).gameObject, Get(source, child.materialField) as Material);
            if (!String.IsNullOrEmpty(recipe.tag)) instance.tag = recipe.tag;
            if (recipe.hasActive) instance.SetActive(recipe.active);
        }
        static void ApplyChange(ControlFijo1 source, Change change)
        {
            if (String.IsNullOrEmpty(change.target)) return;
            string[] path = change.target.Split('/'); GameObject target = Get(source, path[0]) as GameObject;
            if (target == null) return;
            for (int i = 1; i < path.Length; i++)
            {
                if (!Int32.TryParse(path[i], out int index) || index < 0 || index >= target.transform.childCount) return;
                target = target.transform.GetChild(index).gameObject;
            }
            if (change.hasActive) target.SetActive(change.active);
            if (!String.IsNullOrEmpty(change.tag)) target.tag = change.tag;
            AssignMaterial(target, Get(source, change.materialField) as Material);
        }
        static FieldInfo Field(string name)
        {
            if (String.IsNullOrEmpty(name)) return null;
            if (!fields.TryGetValue(name, out FieldInfo field)) { field = typeof(ControlFijo1).GetField(name, Flags); fields[name] = field; }
            return field;
        }
        static object Get(ControlFijo1 source, string name) { FieldInfo field = Field(name); return field == null ? null : field.GetValue(source); }
        static void Set(ControlFijo1 source, string name, object value) { FieldInfo field = Field(name); if (field != null) field.SetValue(source, value); }
        static Vector3 Vector(float[] value) { return new Vector3(value[0], value[1], value[2]); }
        static void AssignMaterial(GameObject target, Material material) { Renderer renderer = target == null ? null : target.GetComponent<Renderer>(); if (renderer != null && material != null) renderer.sharedMaterial = material; }
    }
}
