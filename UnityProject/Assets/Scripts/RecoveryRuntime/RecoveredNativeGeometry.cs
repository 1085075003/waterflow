// Static geometry transcribed from the original ARM64 ControlPiezaC.Start and
// its construction helpers. Recipes cover every C configuration in gm/so data.
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Recovered.FlowWater
{
    public static class RecoveredNativeGeometry
    {
        const string Marker = "__Recovered Native Geometry";
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        [Serializable] sealed class Catalog { public Entry[] entries; public RecipeSet[] recipeSets; }
        [Serializable] sealed class Entry { public string basePrefab, configuration, path; public int recipeSet; }
        [Serializable] sealed class RecipeSet { public Recipe[] recipes; public Change[] changes; public bool[] waterPorts; public int[] waterDirections; public bool[] waterRebose, waterWalls, controllerWalls; }
        [Serializable] sealed class ChildMaterial { public int childIndex; public string materialField; }
        [Serializable] sealed class Recipe
        {
            public string prefabField, instanceField, materialField, tag;
            public float[] position, rotation, scale;
            public bool hasActive, active, hasCylinderColor, rotationIsLocal;
            public int colorCilindro;
            public ChildMaterial[] childMaterials;
        }
        [Serializable] sealed class Change { public string target, materialField, tag; public bool hasActive, active; }
        static Catalog catalog;
        static Dictionary<string, RecipeSet> recipes;
        static readonly Dictionary<Type, Dictionary<string, FieldInfo>> fields = new Dictionary<Type, Dictionary<string, FieldInfo>>();
        static readonly Dictionary<Collider, bool> originalColliderEnabled = new Dictionary<Collider, bool>();
        static Transform levelRoot;

        public static bool TryGetOriginalColliderEnabled(Collider collider, out bool enabled)
        { if (collider != null) return originalColliderEnabled.TryGetValue(collider, out enabled); enabled = false; return false; }

        public static void Apply(RecoveryPiece piece)
        {
            if (piece == null || piece.transform.Find(Marker) != null) return;
            if (levelRoot != piece.transform.parent) { levelRoot = piece.transform.parent; originalColliderEnabled.Clear(); }
            EnsureCatalog();
            if (recipes == null) return;
            ControlPiezaC[] controllers = piece.GetComponentsInChildren<ControlPiezaC>(true);
            if (controllers.Length == 0) return;
            var marker = new GameObject(Marker);
            marker.transform.SetParent(piece.transform, false);
            foreach (ControlPiezaC source in controllers)
            {
                // doProcesoPiezas resolves the original Col1..4 alias before
                // running Start. Prefab C.color is commonly serialized as0.
                if (source.transform == piece.transform && piece.ColorVariant >= 1 && piece.ColorVariant <= 4)
                    source.color = piece.ColorVariant;
                RecipeSet set = Find(piece, source);
                if (set == null) continue;
                if (set.controllerWalls != null)
                    for (int i = 0; i < Math.Min(4, set.controllerWalls.Length); i++) Set(source, "wall" + (i + 1), set.controllerWalls[i]);
                if (set.recipes != null)
                    foreach (Recipe recipe in set.recipes) Spawn(source, recipe);
                if (set.changes != null)
                    foreach (Change change in set.changes) ApplyChange(source, change);
                if (source.color >= 1 && source.color <= 4)
                {
                    AssignMaterial(source.circulo, Get(source, "material" + source.color) as Material);
                    GameObject center = Get(source, "obj9") as GameObject;
                    if (source.esTextura10 && center != null) center.tag = "color" + source.color;
                }
            }
        }

        // Original Awake copies authored origen flags into aguaC, after which
        // Start's wall/type helpers alter its ports and center directions.
        public static bool TryGetWaterPorts(ControlPiezaC source, out bool[] ports, out int[] directions)
        {
            ports = null; directions = null;
            if (source == null) return false;
            RecoveryPiece piece = source.GetComponentInParent<RecoveryPiece>();
            if (piece == null) return false;
            EnsureCatalog();
            RecipeSet set = Find(piece, source);
            if (set == null || set.waterPorts == null || set.waterPorts.Length != 6) return false;
            ports = set.waterPorts;
            directions = set.waterDirections;
            return true;
        }

        // Original C.Start stores these fields into the AguaControlC instance
        // at 1084C5C..C78 and 1088CF0..D0C, including authored-default false.
        public static bool ApplyWaterConfiguration(ControlPiezaC source, AguaControlC water)
        {
            if (source == null || water == null) return false;
            RecoveryPiece piece = source.GetComponentInParent<RecoveryPiece>();
            if (piece == null) return false;
            EnsureCatalog(); RecipeSet set = Find(piece, source);
            if (set == null) return false;
            for (int i = 0; i < 6; i++)
                if (set.waterPorts != null && i < set.waterPorts.Length) Set(water, "ori" + (i + 1), set.waterPorts[i]);
            for (int i = 0; i < 4; i++)
            {
                string suffix = (i + 1).ToString();
                if (set.waterDirections != null && i < set.waterDirections.Length) Set(water, "dirCentro" + suffix, set.waterDirections[i]);
                if (set.waterRebose != null && i < set.waterRebose.Length) Set(water, "rebos" + suffix, set.waterRebose[i]);
                if (set.waterWalls != null && i < set.waterWalls.Length) Set(water, "wall" + suffix, set.waterWalls[i]);
                if (set.controllerWalls != null && i < set.controllerWalls.Length) Set(source, "wall" + suffix, set.controllerWalls[i]);
            }
            return true;
        }
        public static bool TryGetWaterRebose(ControlPiezaC source, out bool[] rebose)
        {
            rebose = null; if (source == null) return false;
            RecoveryPiece piece = source.GetComponentInParent<RecoveryPiece>();
            if (piece == null) return false;
            EnsureCatalog(); RecipeSet set = Find(piece, source);
            if (set == null || set.waterRebose == null || set.waterRebose.Length != 4) return false;
            rebose = set.waterRebose; return true;
        }

        static void EnsureCatalog()
        {
            if (recipes != null) return;
            recipes = new Dictionary<string, RecipeSet>(StringComparer.Ordinal);
            TextAsset asset = Resources.Load<TextAsset>("RecoveredNativeGeometry");
            if (asset == null) { Debug.LogError("Missing original C geometry recipes."); return; }
            catalog = JsonUtility.FromJson<Catalog>(asset.text);
            if (catalog == null || catalog.entries == null || catalog.recipeSets == null) return;
            foreach (Entry entry in catalog.entries)
                if (entry.recipeSet >= 0 && entry.recipeSet < catalog.recipeSets.Length)
                    recipes[Key(entry.basePrefab, entry.configuration, entry.path)] = catalog.recipeSets[entry.recipeSet];
        }

        static RecipeSet Find(RecoveryPiece piece, ControlPiezaC source)
        {
            if (recipes == null || source == null) return null;
            string path = "";
            Transform current = source.transform;
            while (current != piece.transform)
            {
                if (current == null) return null;
                path = path.Length == 0 ? current.name : current.name + "/" + path;
                current = current.parent;
            }
            recipes.TryGetValue(Key(piece.BasePrefab, piece.OriginalConfiguration, path), out RecipeSet set);
            return set;
        }

        static string Key(string prefab, string configuration, string path)
        {
            return (prefab ?? "") + "\n" + (configuration ?? "") + "\n" + (path ?? "");
        }

        static void Spawn(ControlPiezaC source, Recipe recipe)
        {
            GameObject prefab = Get(source, recipe.prefabField) as GameObject;
            if (prefab == null) return;
            Quaternion rotation = recipe.rotation != null && recipe.rotation.Length == 4
                ? new Quaternion(recipe.rotation[0], recipe.rotation[1], recipe.rotation[2], recipe.rotation[3]) : Quaternion.identity;
            // Native Instantiate uses an absolute rotation, then sets parent
            // with worldPositionStays=true and finally writes local position.
            GameObject instance = UnityEngine.Object.Instantiate(prefab, Vector3.zero, rotation);
            DisableOriginalSimulation(instance);
            instance.transform.SetParent(source.transform, true);
            if (recipe.rotationIsLocal) instance.transform.localRotation = rotation;
            if (recipe.position != null && recipe.position.Length == 3)
                instance.transform.localPosition = Vector(recipe.position);
            if (recipe.scale != null && recipe.scale.Length == 3)
                instance.transform.localScale = Vector(recipe.scale);
            Set(source, recipe.instanceField, instance);
            Material material = Get(source, recipe.materialField) as Material;
            AssignMaterial(instance, material);
            if (recipe.childMaterials != null)
                foreach (ChildMaterial child in recipe.childMaterials)
                    if (child.childIndex >= 0 && child.childIndex < instance.transform.childCount)
                        AssignMaterial(instance.transform.GetChild(child.childIndex).gameObject, Get(source, child.materialField) as Material);
            if (!String.IsNullOrEmpty(recipe.tag)) instance.tag = recipe.tag;
            if (recipe.hasCylinderColor)
            {
                colorCilindro cylinder = instance.GetComponent<colorCilindro>();
                if (cylinder != null) { cylinder.color = source.color; ApplyCylinderMaterial(cylinder); }
            }
            // C.doConfigPit can assign color on a child component instead.
            foreach (colorCilindro cylinder in instance.GetComponentsInChildren<colorCilindro>(true))
                if (cylinder != null) ApplyCylinderMaterial(cylinder);
            if (recipe.hasActive) instance.SetActive(recipe.active);
        }

        static void ApplyChange(ControlPiezaC source, Change change)
        {
            if (String.IsNullOrEmpty(change.target)) return;
            string[] path = change.target.Split('/');
            GameObject target = Get(source, path[0]) as GameObject;
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

        static void DisableOriginalSimulation(GameObject instance)
        {
            foreach (MonoBehaviour behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true)) { originalColliderEnabled[collider] = collider.enabled; collider.enabled = false; }
            foreach (Rigidbody body in instance.GetComponentsInChildren<Rigidbody>(true)) { body.isKinematic = true; body.detectCollisions = false; }
            foreach (AudioSource audio in instance.GetComponentsInChildren<AudioSource>(true)) { audio.Stop(); audio.enabled = false; }
        }

        // Original colorCilindro.Start: Rendija uses material0 and returns;
        // otherwise five named renderers choose the corresponding color group.
        static void ApplyCylinderMaterial(colorCilindro cylinder)
        {
            if (cylinder.cilindro != null && cylinder.cilindro.name.Contains("Rendija"))
            {
                AssignMaterial(cylinder.cilindro, cylinder.material0);
                return;
            }
            if (cylinder.color >= 1 && cylinder.color <= 4)
            {
                string number = cylinder.color.ToString(System.Globalization.CultureInfo.InvariantCulture);
                Material main = Get(cylinder, "material" + number) as Material;
                AssignMaterial(cylinder.cilindro, main);
                AssignMaterial(cylinder.cilindroB, main);
                AssignMaterial(cylinder.cilindroC, Get(cylinder, "material" + number + "c") as Material);
                Material secondary = Get(cylinder, "material" + number + "b") as Material;
                AssignMaterial(cylinder.cilindro1, secondary);
                AssignMaterial(cylinder.cilindro2, secondary);
            }
            if (Int32.TryParse(cylinder.textura, out int texture) && texture >= 1 && texture <= 4)
            {
                Material trim = Get(cylinder, "materialFuente" + texture) as Material;
                AssignMaterial(cylinder.parte1, trim);
                AssignMaterial(cylinder.parte2, trim);
            }
        }

        static void AssignMaterial(GameObject target, Material material)
        {
            if (target == null || material == null) return;
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
        }
        static Vector3 Vector(float[] value) { return new Vector3(value[0], value[1], value[2]); }
        static FieldInfo Field(object source, string name)
        {
            if (source == null || String.IsNullOrEmpty(name)) return null;
            Type type = source.GetType();
            if (!fields.TryGetValue(type, out Dictionary<string, FieldInfo> map))
            {
                map = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
                foreach (FieldInfo field in type.GetFields(Flags)) map[field.Name] = field;
                fields[type] = map;
            }
            map.TryGetValue(name, out FieldInfo result);
            return result;
        }
        static object Get(object source, string name) { FieldInfo field = Field(source, name); return field == null ? null : field.GetValue(source); }
        static void Set(object source, string name, object value) { FieldInfo field = Field(source, name); if (field != null) field.SetValue(source, value); }
    }
}
