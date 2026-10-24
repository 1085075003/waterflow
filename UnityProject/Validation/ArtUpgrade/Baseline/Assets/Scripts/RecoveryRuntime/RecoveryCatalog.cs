// Recovery implementation created for this export. This is not recovered original source.
using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
#endif

namespace Recovered.FlowWater
{
    [Serializable]
    public sealed class RecoveryLevelEntry
    {
        public int assetId;
        public TextAsset layout;
        public TextAsset solution;
    }

    [Serializable]
    public sealed class RecoveryPrefabEntry
    {
        public string name;
        public GameObject prefab;
    }

    [Serializable]
    public sealed class RecoveryAlias
    {
        public string originalName;
        public string basePrefab;
        public string mappingConfidence;
        public int colorSuffix;
        public bool scene292exception;
    }

    [Serializable]
    public sealed class RecoveryAliasFile { public List<RecoveryAlias> aliases; }

    /// <summary>Build-safe references to the original exported JSON and prefab assets.</summary>
    public sealed class RecoveryCatalog : ScriptableObject
    {
        public RecoveryLevelEntry[] levels = new RecoveryLevelEntry[0];
        public RecoveryPrefabEntry[] prefabs = new RecoveryPrefabEntry[0];
        public TextAsset aliases;

        public static RecoveryCatalog Load()
        {
            return Resources.Load<RecoveryCatalog>("RecoveryCatalog");
        }
    }

    [Serializable]
    public sealed class RecoveryEnvelope
    {
        public string source_asset;
        public RecoverySave data;
    }

    /// <summary>Preserves both decoded Save and Save1 schemas without BinaryFormatter execution.</summary>
    [Serializable]
    public sealed class RecoverySave
    {
        public string __type__;
        public int level;
        public List<string> pieza;
        public List<float> positionx, positiony, positionz;
        public List<string> fijo;
        public List<int> move, orix, oriy, oriz, desx, desy, desz;
        public List<string> excep;

        public Vector3 Position(int index)
        {
            return new Vector3(positionx[index], positiony[index], positionz[index]);
        }

        public bool IsValidLayout()
        {
            int count = pieza == null ? 0 : pieza.Count;
            return __type__ == "Save" && count > 0 && positionx != null && positiony != null &&
                positionz != null && fijo != null && positionx.Count == count && positiony.Count == count &&
                positionz.Count == count && fijo.Count == count;
        }

        public bool IsValidSteps()
        {
            int count = move == null ? 0 : move.Count;
            return __type__ == "Save1" && count > 0 && orix != null && oriy != null && oriz != null &&
                desx != null && desy != null && desz != null && orix.Count == count && oriy.Count == count &&
                oriz.Count == count && desx.Count == count && desy.Count == count && desz.Count == count;
        }
    }

#if UNITY_EDITOR
    public static class RecoveryCatalogBuilder
    {
        public const string CatalogPath = "Assets/Resources/RecoveryCatalog.asset";

        [MenuItem("Tools/Flow Water/Build Runtime Recovery Catalog")]
        public static void Build()
        {
            const string root = "Assets/RecoveredData/LevelData";
            if (!Directory.Exists(root + "/01_Layouts"))
                throw new DirectoryNotFoundException("Recovered JSON is missing: " + root);
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            RecoveryCatalog catalog = AssetDatabase.LoadAssetAtPath<RecoveryCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<RecoveryCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            var levels = new List<RecoveryLevelEntry>();
            foreach (string path in Directory.GetFiles(root + "/01_Layouts", "gm*.json"))
            {
                int id;
                if (!int.TryParse(Path.GetFileNameWithoutExtension(path).Substring(2), out id)) continue;
                levels.Add(new RecoveryLevelEntry
                {
                    assetId = id,
                    layout = AssetDatabase.LoadAssetAtPath<TextAsset>(path.Replace('\\', '/')),
                    solution = AssetDatabase.LoadAssetAtPath<TextAsset>(root + "/02_Solutions/so" + id + ".json")
                });
            }
            catalog.levels = levels.OrderBy(x => x.assetId).ToArray();
            catalog.prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/GameObject" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(x => AssetDatabase.LoadAssetAtPath<GameObject>(x))
                .Where(x => x != null)
                .Select(x => new RecoveryPrefabEntry { name = OriginalPrefabName(x), prefab = x })
                .OrderBy(x => x.name, StringComparer.Ordinal).ToArray();
            catalog.aliases = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/RecoveredData/preview_aliases.json");
            if (catalog.levels.Length != 1350 || catalog.levels.Any(x => x.layout == null || x.solution == null) || catalog.aliases == null)
                throw new InvalidDataException("Runtime catalog must contain all 1350 paired layouts and solutions plus aliases.");
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("Recovery runtime catalog built: " + catalog.levels.Length + " paired levels; " + catalog.prefabs.Length + " original prefab references.");
        }

        static string OriginalPrefabName(GameObject prefab)
        {
            // Unity imports this exported root using its suffixed filename.
            // Original SaveLoadManager.prefabPiezas[79] references this GUID
            // as BloqueFijo in all four gameplay scenes. The lowercase
            // bloqueFijo GUID is an unrelated decorative mesh.
            return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab)) == "f527ee718f0ab1c4389bced3619d375c"
                ? "BloqueFijo" : prefab.name;
        }
    }
#endif
}
