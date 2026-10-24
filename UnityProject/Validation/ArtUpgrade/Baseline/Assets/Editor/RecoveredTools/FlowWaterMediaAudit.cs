#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Recovered.FlowWater.Editor
{
    public static class FlowWaterMediaAudit
    {
        [Serializable] sealed class Item { public string path, type, detail; public bool passed; }
        [Serializable] sealed class Report
        {
            public string unityVersion, finishedUtc;
            public int textures, meshes, audioClips, animations, controllers, materials, shaders, failures;
            public bool passed;
            public List<Item> items = new List<Item>();
        }
        [MenuItem("Tools/Flow Water/Validate All Media and Animation Assets")]
        public static void Run()
        {
            var report = new Report { unityVersion = Application.unityVersion };
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid); Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(path); report.textures++;
                Add(report, path, texture == null ? "Texture" : texture.GetType().Name, texture != null && texture.width > 0 && texture.height > 0,
                    texture == null ? "Missing" : texture.width + "x" + texture.height + " " + texture.dimension);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid); Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path); report.meshes++;
                Add(report, path, "Mesh", mesh != null && mesh.vertexCount > 0 && mesh.subMeshCount > 0,
                    mesh == null ? "Missing" : mesh.vertexCount + " vertices, " + mesh.subMeshCount + " submeshes");
            }
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid); AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path); report.audioClips++;
                bool valid = clip != null && clip.samples > 0 && clip.frequency > 0 && clip.channels > 0 && clip.LoadAudioData();
                Add(report, path, "AudioClip", valid, clip == null ? "Missing" : clip.length + " seconds, " + clip.frequency + " Hz, " + clip.channels + " channels");
            }
            foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid); AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path); report.animations++;
                Add(report, path, "AnimationClip", clip != null, clip == null ? "Missing" : clip.length + " seconds, " + AnimationUtility.GetCurveBindings(clip).Length + " float curves");
            }
            foreach (string guid in AssetDatabase.FindAssets("t:AnimatorController", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid); AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path); report.controllers++;
                Add(report, path, "AnimatorController", controller != null && controller.layers.Length > 0,
                    controller == null ? "Missing" : controller.layers.Length + " layers, " + controller.parameters.Length + " parameters");
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid); Material material = AssetDatabase.LoadAssetAtPath<Material>(path); report.materials++;
                Add(report, path, "Material", material != null && material.shader != null && !ShaderUtil.ShaderHasError(material.shader), material == null || material.shader == null ? "Missing" : material.shader.name);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Shader", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid); Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(path); report.shaders++;
                Add(report, path, "Shader", shader != null && !ShaderUtil.ShaderHasError(shader), shader == null ? "Missing" : shader.name);
            }
            report.finishedUtc = DateTime.UtcNow.ToString("o"); report.passed = report.failures == 0;
            string output = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation", "media-validation.json");
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            Debug.Log("All media validation " + (report.passed ? "PASS" : "FAIL") + "; " + report.items.Count + " assets; failures=" + report.failures);
            if (Application.isBatchMode && !FlowWaterValidation.SuppressBatchExit) EditorApplication.Exit(report.passed ? 0 : 1);
        }
        static void Add(Report report, string path, string type, bool passed, string detail)
        {
            report.items.Add(new Item { path = path, type = type, passed = passed, detail = detail });
            if (!passed) report.failures++;
        }
    }
}
#endif
