#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Recovered.FlowWater.Editor
{
    /// <summary>
    /// Reversible visual asset pass. It never touches prefabs, scene hierarchy,
    /// colliders, game data, animation, controllers or any runtime C# source.
    /// </summary>
    public static class FlowWaterArtMaterials
    {
        const string Output = "Validation/ArtUpgrade";
        const string NormalPath = "Assets/VisualAssets/Materials/FineWaterNormal.png";
        const string StonePath = "Assets/ArtUpgrade/Textures/ArtStoneMech11.png";
        const string GrassPath = "Assets/ArtUpgrade/Textures/ArtGrass.png";
        const string GrassAlternatePath = "Assets/ArtUpgrade/Textures/ArtGrassCesped.png";
        const string GrassZeroPath = "Assets/ArtUpgrade/Textures/ArtGrassCesped0.png";
        const string SprayPath = "Assets/ArtUpgrade/Textures/ArtWaterSpray.png";
        const string WaterShaderPath = "Assets/Shader/FlowWater_ClearWater.shader";

        [Serializable] public sealed class MaterialEntry
        {
            public string path, guid, family, shader, mainTexture, normalTexture;
            public bool supported;
            public float opacity, rippleStrength, normalStrength;
            public Color originalColor;
            public Vector2 mainTextureScale, mainTextureOffset;
        }
        [Serializable] public sealed class Report
        {
            public string unityVersion, utc;
            public int materials, water, stone, grass, particles, textureImports;
            public List<MaterialEntry> entries = new List<MaterialEntry>();
            public List<string> issues = new List<string>();
        }

        [MenuItem("Tools/Flow Water Art/Apply visual materials")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before applying visual assets.");
            Directory.CreateDirectory(Output);
            EnsureRippleNormal();
            Texture2D waterNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath);
            Shader clearWater = AssetDatabase.LoadAssetAtPath<Shader>(WaterShaderPath);
            if (clearWater == null || !clearWater.isSupported)
                throw new InvalidOperationException("Clear Water shader is missing or unsupported. Compile the shaders first.");

            var report = NewReport();
            var baseTextures = new HashSet<string>(StringComparer.Ordinal);
            var normalTextures = new HashSet<string>(StringComparer.Ordinal);
            string[] paths = MaterialPaths();
            foreach (string path in paths)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null) continue;
                string family = Family(material);
                if (family == "other") continue;
                Backup(path);
                if (family == "water")
                {
                    // Night materials retain their native shader/palette callback.
                    // Those same shader assets now use the matching water surface.
                    bool nativeNight = material.shader != null &&
                        material.shader.name == "Mobile/Diffuse Color Mig2";
                    Texture texture = material.mainTexture;
                    Vector2 scale = material.mainTextureScale;
                    Vector2 offset = material.mainTextureOffset;
                    if (!nativeNight) material.shader = clearWater;
                    material.mainTexture = texture;
                    material.mainTextureScale = scale;
                    material.mainTextureOffset = offset;
                    material.SetTexture("_DetailTex", waterNormal);
                    material.SetTextureScale("_DetailTex", scale);
                    material.SetTextureOffset("_DetailTex", offset);
                    Set(material, "_ArtWaterSurface", 1);
                    Set(material, "_ArtOpacity", material.name.Contains("chorro") ? .96f : .91f);
                    Set(material, "_ArtWaveStrength", material.name.Contains("chorro") ? .21f : .30f);
                    Set(material, "_ArtSpecular", .68f);
                    Set(material, "_ArtWrap", .12f);
                    Set(material, "_ArtNormalStrength", 0);
                    Set(material, "_ArtSaturation", 1);
                    Set(material, "_ArtContrast", 1);
                    SetColor(material, "_ArtTint", Color.white);
                    SetColor(material, "_ArtHighlight", new Color(.76f, .91f, 1f, 1));
                    report.water++;
                }
                else if (family == "stone")
                {
                    if (material.shader != null && material.shader.name == "Mobile/Bumped Diffuse")
                        material.shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shader/Mobile_Diffuse Color1.shader");
                    MaybeReplaceTexture(material, "TextureMech11", StonePath);
                    Set(material, "_ArtWaterSurface", 0);
                    Set(material, "_ArtSaturation", .82f);
                    Set(material, "_ArtContrast", .86f);
                    Set(material, "_ArtNormalStrength", .34f);
                    Set(material, "_ArtSpecular", .055f);
                    Set(material, "_ArtWrap", .12f);
                    SetColor(material, "_ArtTint", new Color(1f, .99f, .96f, 1));
                    SetColor(material, "_ArtHighlight", new Color(1f, .94f, .82f, 1));
                    report.stone++;
                }
                else if (family == "grass")
                {
                    // Legacy grass used a separate Mobile/Bumped Diffuse shader.
                    // Change it before configuring the new visual properties.
                    if (material.shader != null && material.shader.name == "Mobile/Bumped Diffuse")
                        material.shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shader/Mobile_Diffuse Color1.shader");
                    MaybeReplaceTexture(material, "Cesped", AssetDatabase.LoadAssetAtPath<Texture2D>(GrassAlternatePath) != null ? GrassAlternatePath : GrassPath);
                    MaybeReplaceTexture(material, "Cesped_0", GrassZeroPath);
                    Set(material, "_ArtWaterSurface", 0);
                    Set(material, "_ArtSaturation", .66f);
                    Set(material, "_ArtContrast", .80f);
                    Set(material, "_ArtNormalStrength", .20f);
                    Set(material, "_ArtSpecular", .025f);
                    Set(material, "_ArtWrap", .12f);
                    SetColor(material, "_ArtTint", new Color(.93f, .98f, .92f, 1));
                    SetColor(material, "_ArtHighlight", new Color(.78f, .91f, .82f, 1));
                    report.grass++;
                }
                else if (family == "particles")
                {
                    MaybeReplaceTexture(material, "foam", SprayPath);
                    if (material.name == "Water Splash II" || material.shader.name == "FlowWater/Art/Soft Spray")
                        material.shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shader/FlowWater_SoftSpray.shader");
                    Set(material, "_ArtSoftness", .2f);
                    // Original glow/tint are preserved: colored beams also use
                    // this shader, and their color identifies their destination.
                    report.particles++;
                }
                material.enableInstancing = true;
                EditorUtility.SetDirty(material);
                if (material.mainTexture != null)
                    baseTextures.Add(AssetDatabase.GetAssetPath(material.mainTexture));
                if (material.HasProperty("_BumpMap") && material.GetTexture("_BumpMap") != null)
                    normalTextures.Add(AssetDatabase.GetAssetPath(material.GetTexture("_BumpMap")));
                report.materials++;
                report.entries.Add(Entry(path, material, family));
            }
            foreach (string texture in baseTextures)
                if (ConfigureTexture(texture, false)) report.textureImports++;
            foreach (string texture in normalTextures)
                if (ConfigureTexture(texture, true)) report.textureImports++;
            AssetDatabase.SaveAssets();
            WriteReport("materials-apply.json", report);
            Audit();
            Debug.Log("Art materials applied: water=" + report.water + ", stone=" + report.stone +
                ", grass=" + report.grass + ", particles=" + report.particles +
                ". All original asset GUIDs, main UV transforms and gameplay data retained.");
        }

        [MenuItem("Tools/Flow Water Art/Audit visual materials")]
        public static void Audit()
        {
            var report = NewReport();
            foreach (string path in MaterialPaths())
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null) { report.issues.Add("Material failed to load: " + path); continue; }
                string family = Family(material);
                if (family == "other") continue;
                report.materials++;
                if (family == "water") report.water++;
                if (family == "stone") report.stone++;
                if (family == "grass") report.grass++;
                if (family == "particles") report.particles++;
                MaterialEntry entry = Entry(path, material, family);
                report.entries.Add(entry);
                if (!entry.supported) report.issues.Add("Unsupported shader: " + path + " / " + entry.shader);
                if (family == "water")
                {
                    if (!material.HasProperty("_DetailTex") || material.GetTexture("_DetailTex") == null)
                        report.issues.Add("Missing fine ripple normal: " + path);
                    if (!material.HasProperty("_ArtWaterSurface") || material.GetFloat("_ArtWaterSurface") < .5f)
                        report.issues.Add("Water surface was not configured: " + path);
                }
            }
            foreach (string shaderPath in AssetDatabase.FindAssets("t:Shader", new[] { "Assets/Shader" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
                if (shader == null) { report.issues.Add("Shader failed to load: " + shaderPath); continue; }
                foreach (var message in ShaderUtil.GetShaderMessages(shader))
                    if (message.severity.ToString() == "Error")
                        report.issues.Add(shaderPath + ": " + message.message);
            }
            WriteReport("materials-audit.json", report);
            if (report.issues.Count != 0)
                throw new InvalidOperationException("Art material audit failed: " + string.Join("; ", report.issues));
            Debug.Log("Art material audit passed: " + report.materials + " configured materials, zero shader errors.");
        }

        static string[] MaterialPaths()
        {
            return AssetDatabase.FindAssets("t:Material", new[] { "Assets/Material" })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p, StringComparer.Ordinal).ToArray();
        }

        static string Family(Material material)
        {
            string name = material.name;
            string shader = material.shader == null ? "" : material.shader.name;
            string texture = material.mainTexture == null ? "" : material.mainTexture.name;
            if (name.StartsWith("MatGr", StringComparison.Ordinal)) return "water";
            if (shader == "Particles/Additive Intensify" || shader == "FlowWater/Art/Soft Spray" || name == "Water Splash II") return "particles";
            if (texture == "Cesped" || texture == "Cesped_0" || texture == "baldosaHierba" ||
                texture.StartsWith("ArtGrass", StringComparison.Ordinal) || name.StartsWith("MaterialHierba", StringComparison.Ordinal))
                return "grass";
            if (name.StartsWith("matFondo", StringComparison.Ordinal)) return "other";
            if (shader == "Mobile/Diffuse Color1" ||
                (shader == "Mobile/Bumped Diffuse" && texture != "")) return "stone";
            return "other";
        }

        static void MaybeReplaceTexture(Material material, string oldName, string candidate)
        {
            if (material.mainTexture == null || material.mainTexture.name != oldName) return;
            Texture2D replacement = AssetDatabase.LoadAssetAtPath<Texture2D>(candidate);
            if (replacement != null) material.mainTexture = replacement;
        }

        static void EnsureRippleNormal()
        {
            if (!File.Exists(NormalPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(NormalPath));
                const int size = 512;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
                var pixels = new Color32[size * size];
                // Integer-frequency harmonics tile exactly. Analytical slopes
                // provide a smooth optical ripple, without adding noisy albedo.
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size * Mathf.PI * 2;
                    float v = y / (float)size * Mathf.PI * 2;
                    float slopeX = Mathf.Cos(2*u + 3*v)*.23f + Mathf.Cos(5*u-v+.7f)*.10f + Mathf.Cos(9*u+4*v)*.025f;
                    float slopeY = Mathf.Cos(2*u + 3*v)*.34f - Mathf.Cos(5*u-v+.7f)*.02f + Mathf.Cos(9*u+4*v)*.011f;
                    Vector3 n = new Vector3(-slopeX, -slopeY, 1).normalized;
                    pixels[y*size+x] = new Color(n.x*.5f+.5f, n.y*.5f+.5f, n.z*.5f+.5f, 1);
                }
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                File.WriteAllBytes(NormalPath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(NormalPath, ImportAssetOptions.ForceSynchronousImport);
            }
            ConfigureTexture(NormalPath, true);
        }

        static bool ConfigureTexture(string path, bool normal)
        {
            if (string.IsNullOrEmpty(path)) return false;
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return false;
            Backup(path + ".meta");
            bool changed = importer.filterMode != FilterMode.Trilinear || importer.anisoLevel < 4 ||
                !importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed ||
                normal && (importer.textureType != TextureImporterType.NormalMap || importer.sRGBTexture);
            if (!changed) return false;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 8;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            if (normal)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.sRGBTexture = false;
                importer.convertToNormalmap = false;
            }
            importer.SaveAndReimport();
            return true;
        }

        static MaterialEntry Entry(string path, Material material, string family)
        {
            return new MaterialEntry {
                path = path, guid = AssetDatabase.AssetPathToGUID(path), family = family,
                shader = material.shader == null ? "missing" : material.shader.name,
                supported = material.shader != null && material.shader.isSupported,
                mainTexture = AssetDatabase.GetAssetPath(material.mainTexture),
                normalTexture = material.HasProperty("_BumpMap") ? AssetDatabase.GetAssetPath(material.GetTexture("_BumpMap")) : "",
                opacity = Get(material, "_ArtOpacity"), rippleStrength = Get(material, "_ArtWaveStrength"),
                normalStrength = Get(material, "_ArtNormalStrength"),
                originalColor = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white,
                mainTextureScale = material.mainTextureScale, mainTextureOffset = material.mainTextureOffset
            };
        }
        static Report NewReport() { return new Report { unityVersion = Application.unityVersion, utc = DateTime.UtcNow.ToString("O") }; }
        static float Get(Material material, string property) { return material.HasProperty(property) ? material.GetFloat(property) : 0; }
        static void Set(Material material, string property, float value) { if (material.HasProperty(property)) material.SetFloat(property, value); }
        static void SetColor(Material material, string property, Color value) { if (material.HasProperty(property)) material.SetColor(property, value); }
        static void WriteReport(string name, Report report)
        {
            Directory.CreateDirectory(Output);
            File.WriteAllText(Path.Combine(Output, name), JsonUtility.ToJson(report, true));
        }
        static void Backup(string path)
        {
            if (!File.Exists(path)) return;
            string target = Path.Combine(Output + "/Before", path);
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            if (!File.Exists(target)) File.Copy(path, target);
            if (!path.EndsWith(".meta", StringComparison.Ordinal) && File.Exists(path + ".meta") && !File.Exists(target + ".meta"))
                File.Copy(path + ".meta", target + ".meta");
        }
    }
}
#endif
