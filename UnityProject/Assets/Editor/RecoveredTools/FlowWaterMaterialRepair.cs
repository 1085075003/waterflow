#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Recovered.FlowWater.Editor
{
    /// <summary>Evidence-based repairs for the missing slots in the import audit.</summary>
    public static class FlowWaterMaterialRepair
    {
        static readonly string[] Targets = {
            "Assets/Scenes/Portada.unity",
            "Assets/GameObject/Font1.prefab", "Assets/GameObject/Font2.prefab",
            "Assets/GameObject/Font2_0.prefab", "Assets/GameObject/FontBigZ.prefab",
            "Assets/GameObject/FontCircular2.prefab", "Assets/GameObject/grupo1Prefab.prefab",
            "Assets/GameObject/grupo2Prefab.prefab", "Assets/GameObject/Grupo4Prefab.prefab",
            "Assets/GameObject/grupo5Prefab.prefab", "Assets/GameObject/grupo6Prefab.prefab",
            "Assets/GameObject/GrupoMech21.prefab"
        };

        [Serializable] public sealed class Repair
        {
            public string asset, hierarchy, operation, material, evidence;
            public int slot;
        }
        [Serializable] public sealed class RepairReport
        {
            public string unityVersion, finishedUtc;
            public int repairedSlots;
            public List<Repair> repairs = new List<Repair>();
            public List<string> unresolved = new List<string>();
        }

        [MenuItem("Tools/Flow Water Recovery/Repair imported material slots")]
        public static void RepairMissingMaterials()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before repairing source assets.");
            var report = new RepairReport { unityVersion = Application.unityVersion };
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string path in Targets)
                {
                    Backup(path);
                    if (path.EndsWith(".prefab", StringComparison.Ordinal))
                    {
                        GameObject root = PrefabUtility.LoadPrefabContents(path);
                        try
                        {
                            bool changed = RepairRoots(new[] { root }, path, report);
                            if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
                        }
                        finally { PrefabUtility.UnloadPrefabContents(root); }
                    }
                    else
                    {
                        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                        if (RepairRoots(scene.GetRootGameObjects(), path, report))
                            EditorSceneManager.SaveScene(scene, path);
                    }
                }
                AssetDatabase.SaveAssets();
            }
            finally
            {
                report.finishedUtc = DateTime.UtcNow.ToString("O");
                Directory.CreateDirectory("Validation");
                File.WriteAllText("Validation/material-repair.json", JsonUtility.ToJson(report, true));
                if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
            if (report.unresolved.Count > 0)
                throw new InvalidOperationException("Material repair has unresolved evidence: " + String.Join("; ", report.unresolved));
            Debug.Log("Recovered material repair completed: " + report.repairedSlots + " slots. Evidence: Validation/material-repair.json");
        }

        public static void RepairAndAudit()
        {
            RepairMissingMaterials();
            FlowWaterValidation.RunAudit();
        }

        static void Backup(string path)
        {
            string backup = Path.Combine("Validation/BeforeRuntimeRepair", path);
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            if (!File.Exists(backup)) File.Copy(path, backup);
            if (File.Exists(path + ".meta") && !File.Exists(backup + ".meta")) File.Copy(path + ".meta", backup + ".meta");
        }

        static bool RepairRoots(GameObject[] roots, string asset, RepairReport report)
        {
            bool changed = false;
            foreach (GameObject root in roots)
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                if (!materials.Any(material => material == null)) continue;
                var particle = renderer as ParticleSystemRenderer;
                var system = renderer.GetComponent<ParticleSystem>();
                // These five old water-particle prefabs have [originalMaterial,
                // null]. Their TrailModule is disabled, and Font1new retains only
                // the first slot. Removing the unused trail slot preserves output.
                if (particle != null && system != null && !system.trails.enabled &&
                    materials.Length > 1 && materials[0] != null && materials.Skip(1).All(m => m == null))
                {
                    renderer.sharedMaterials = new[] { materials[0] };
                    AddRepair(report, asset, renderer, 1, materials[0], "Remove unused particle trail slot",
                        "Original TrailModule.enabled=0; original Font1new/Font2new use the identical first material with a single slot.");
                    changed = true;
                    continue;
                }
                for (int slot = 0; slot < materials.Length; slot++)
                {
                    if (materials[slot] != null) continue;
                    string evidence;
                    Material replacement = ResolveOriginalMaterial(renderer, asset, slot, out evidence);
                    if (replacement == null)
                    {
                        report.unresolved.Add(asset + ": " + Hierarchy(renderer.transform) + " slot " + slot);
                        continue;
                    }
                    materials[slot] = replacement;
                    AddRepair(report, asset, renderer, slot, replacement, "Reuse original counterpart material", evidence);
                    changed = true;
                }
                renderer.sharedMaterials = materials;
            }
            return changed;
        }

        static void AddRepair(RepairReport report, string asset, Renderer renderer, int slot,
            Material material, string operation, string evidence)
        {
            report.repairs.Add(new Repair {
                asset = asset, hierarchy = Hierarchy(renderer.transform), slot = slot,
                material = AssetDatabase.GetAssetPath(material), operation = operation, evidence = evidence
            });
            report.repairedSlots++;
        }

        static Material ResolveOriginalMaterial(Renderer target, string targetAsset, int slot, out string evidence)
        {
            evidence = null;
            // Prefer the same named original prefab from an ancestor of a flattened
            // scene copy, with the same renderer name and exactly the same Mesh.
            for (Transform ancestor = target.transform.parent; ancestor != null; ancestor = ancestor.parent)
            {
                string name = Normalize(ancestor.name);
                string sourcePath = "Assets/GameObject/" + name + ".prefab";
                if (sourcePath == targetAsset) continue;
                Material material = MaterialFromCounterpart(target, sourcePath, slot, out evidence);
                if (material != null) return material;
            }
            string fallback = null;
            if (target.name.StartsWith("plano", StringComparison.Ordinal))
                fallback = "Assets/GameObject/AguaControl5.prefab";
            else if (targetAsset.EndsWith("grupo2Prefab.prefab", StringComparison.Ordinal))
                fallback = "Assets/GameObject/Cascada1Azul.prefab";
            else if (target.name == "Sphere" && targetAsset.EndsWith("grupo6Prefab.prefab", StringComparison.Ordinal))
                fallback = "Assets/GameObject/EsferaFiltrante3Z.prefab";
            if (fallback != null)
            {
                Material material = MaterialFromCounterpart(target, fallback, slot, out evidence);
                if (material != null)
                {
                    evidence += " Chosen original water-effect family; when original per-instance color assignment was absent, use the original blue/default water variant. Renderer enabled state is preserved.";
                    return material;
                }
            }
            return null;
        }

        static Material MaterialFromCounterpart(Renderer target, string sourcePath, int slot, out string evidence)
        {
            evidence = null;
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source == null) return null;
            Mesh targetMesh = SharedMesh(target);
            if (targetMesh == null) return null;
            var matches = source.GetComponentsInChildren<Renderer>(true)
                .Where(candidate => Normalize(candidate.name) == Normalize(target.name) && SharedMesh(candidate) == targetMesh)
                .Select(candidate => new { renderer = candidate, materials = candidate.sharedMaterials })
                .Where(candidate => candidate.materials.Length > slot && candidate.materials[slot] != null)
                .ToArray();
            Material[] unique = matches.Select(candidate => candidate.materials[slot]).Distinct().ToArray();
            if (unique.Length != 1) return null;
            evidence = sourcePath + " / " + Hierarchy(matches[0].renderer.transform) +
                "; identical renderer name and Mesh asset " + AssetDatabase.GetAssetPath(targetMesh) +
                "; source material " + AssetDatabase.GetAssetPath(unique[0]);
            return unique[0];
        }

        static Mesh SharedMesh(Renderer renderer)
        {
            var filter = renderer.GetComponent<MeshFilter>();
            if (filter != null) return filter.sharedMesh;
            var skinned = renderer as SkinnedMeshRenderer;
            return skinned != null ? skinned.sharedMesh : null;
        }
        static string Normalize(string name)
        {
            return Regex.Replace(name.Replace("(Clone)", ""), @"\s*\(\d+\)$", "").Trim();
        }
        static string Hierarchy(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null) { transform = transform.parent; path = transform.name + "/" + path; }
            return path;
        }
    }
}
#endif
