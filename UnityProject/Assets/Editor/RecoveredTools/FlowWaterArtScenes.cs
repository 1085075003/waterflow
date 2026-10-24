#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

namespace Recovered.FlowWater.Editor
{
    /// <summary>Serialized presentation changes only. All existing transforms and gameplay components are checked before saving.</summary>
    public static class FlowWaterArtScenes
    {
        const string ProfilePath = "Assets/ArtUpgrade/Lighting/WaterGarden-PC.asset";
        const string ResourcesPath = "Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset";
        static readonly string[] ScenePaths = {
            "Assets/Scenes/Portada.unity", "Assets/Scenes/SelLevel0.unity", "Assets/Scenes/SelLevel1.unity",
            "Assets/Scenes/Levely5z/Levely5.unity", "Assets/Scenes/Levely5_basic.unity",
            "Assets/Scenes/Levely5_6x6.unity", "Assets/Scenes/Levely5_7x7.unity", "Assets/Settings.unity", "Assets/Store.unity"
        };

        [Serializable] public sealed class SceneCheck
        {
            public string scene, protectedBefore, protectedAfter, cameraProjectionBefore, cameraProjectionAfter;
            public bool hierarchyAndGameplayUnchanged, cameraProjectionUnchanged, flatAmbientForNativeNight;
            public int cameras, postProcessCameras, directionalLights;
            public float ambientRed, ambientGreen, ambientBlue;
        }
        [Serializable] public sealed class Report
        {
            public string generatedUtc, unityVersion, profile;
            public string[] allowedChanges;
            public bool passed;
            public int activeQuality, antiAliasing, textureMipmapLimit;
            public float shadowDistance;
            public List<SceneCheck> scenes = new List<SceneCheck>();
        }

        [MenuItem("Tools/Flow Water/Art Upgrade/Apply Scene Lighting")]
        public static void Apply()
        {
            RequireEditMode();
            string project = Directory.GetParent(Application.dataPath).FullName;
            string output = Path.Combine(project, "Validation", "ArtUpgrade");
            string backup = Path.Combine(output, "Before", "Lighting");
            Directory.CreateDirectory(backup);
            Backup(project, backup, "ProjectSettings/QualitySettings.asset");
            PostProcessResources resources = AssetDatabase.LoadAssetAtPath<PostProcessResources>(ResourcesPath);
            if (resources == null) throw new InvalidOperationException("Unity Post Processing 3.4 resources must be imported before scene lighting is applied.");
            PostProcessProfile profile = ConfigureProfile();
            ConfigurePCQuality();
            var report = NewReport();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string path in ScenePaths)
                {
                    Backup(project, backup, path);
                    Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var item = new SceneCheck { scene = path, protectedBefore = ProtectedFingerprint(scene), cameraProjectionBefore = CameraFingerprint(scene) };
                    bool gameplay = OriginalScenePresentation.IsGameplayScene(scene);
                    // Both ApplyThemeForLevel and ShowCategory write ambientLight for
                    // the original night themes. Flat mode keeps those overrides effective.
                    RenderSettings.ambientMode = AmbientMode.Flat;
                    RenderSettings.ambientLight = new Color(.63f, .66f, .67f, 1f);
                    RenderSettings.ambientIntensity = 1f;
                    RenderSettings.reflectionIntensity = .45f;
                    RenderSettings.fog = false;
                    foreach (Light light in Components<Light>(scene))
                    {
                        if (light.type != LightType.Directional) continue;
                        item.directionalLights++;
                        // Keep the authored light GameObject, transform, active/enabled state and
                        // UIManagerScript.light reference: native night themes turn it off.
                        light.color = new Color(1f, .965f, .89f, 1f);
                        light.useColorTemperature = false;
                        light.intensity = .96f;
                        light.bounceIntensity = 1f;
                        light.shadows = LightShadows.Soft;
                        light.shadowStrength = .30f;
                        light.shadowBias = .035f;
                        light.shadowNormalBias = .15f;
                        light.shadowResolution = LightShadowResolution.VeryHigh;
                    }
                    foreach (Camera camera in Components<Camera>(scene))
                    {
                        item.cameras++;
                        camera.allowMSAA = true;
                        if (!gameplay || !camera.CompareTag("MainCamera")) continue;
                        camera.allowHDR = true;
                        camera.depthTextureMode |= DepthTextureMode.Depth | DepthTextureMode.DepthNormals;
                        PostProcessLayer layer = camera.GetComponent<PostProcessLayer>();
                        if (layer == null) layer = camera.gameObject.AddComponent<PostProcessLayer>();
                        layer.Init(resources);
                        // No new hierarchy nodes or layers are needed for one global profile.
                        layer.volumeLayer = 1 << camera.gameObject.layer;
                        layer.volumeTrigger = camera.transform;
                        layer.stopNaNPropagation = true;
                        layer.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
                        layer.subpixelMorphologicalAntialiasing.quality = SubpixelMorphologicalAntialiasing.Quality.High;
                        layer.fog.enabled = false;
                        PostProcessVolume volume = camera.GetComponent<PostProcessVolume>();
                        if (volume == null) volume = camera.gameObject.AddComponent<PostProcessVolume>();
                        volume.isGlobal = true;
                        volume.weight = 1f;
                        volume.priority = 0f;
                        volume.sharedProfile = profile;
                        EditorUtility.SetDirty(layer);
                        EditorUtility.SetDirty(volume);
                        item.postProcessCameras++;
                    }
                    CompleteCheck(scene, item);
                    if (!item.hierarchyAndGameplayUnchanged || !item.cameraProjectionUnchanged)
                        throw new InvalidOperationException("A protected scene property changed: " + path);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    report.scenes.Add(item);
                }
                FillQuality(report);
                report.passed = report.scenes.Count == ScenePaths.Length && report.scenes.All(x => x.hierarchyAndGameplayUnchanged && x.cameraProjectionUnchanged);
                AssetDatabase.SaveAssets();
                File.WriteAllText(Path.Combine(output, "lighting-apply-report.json"), JsonUtility.ToJson(report, true));
                Debug.Log("Water Garden lighting applied: " + report.scenes.Count + " scenes; original transforms, cameras, colliders, UI and gameplay component data verified unchanged.");
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        [MenuItem("Tools/Flow Water/Art Upgrade/Audit Scene Lighting")]
        public static void Audit()
        {
            RequireEditMode();
            string output = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation", "ArtUpgrade");
            string apply = Path.Combine(output, "lighting-apply-report.json");
            if (!File.Exists(apply)) throw new InvalidOperationException("Apply scene lighting before auditing it.");
            Report original = JsonUtility.FromJson<Report>(File.ReadAllText(apply));
            var report = NewReport();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (SceneCheck prior in original.scenes)
                {
                    Scene scene = EditorSceneManager.OpenScene(prior.scene, OpenSceneMode.Single);
                    var item = new SceneCheck { scene = prior.scene, protectedBefore = prior.protectedBefore, cameraProjectionBefore = prior.cameraProjectionBefore };
                    item.cameras = Components<Camera>(scene).Count();
                    item.postProcessCameras = Components<PostProcessLayer>(scene).Count();
                    item.directionalLights = Components<Light>(scene).Count(x => x.type == LightType.Directional);
                    CompleteCheck(scene, item);
                    report.scenes.Add(item);
                }
                FillQuality(report);
                report.passed = report.scenes.Count == ScenePaths.Length && report.scenes.All(x => x.hierarchyAndGameplayUnchanged && x.cameraProjectionUnchanged && (!x.scene.Contains("Levely5") || x.flatAmbientForNativeNight && x.postProcessCameras > 0)) && report.antiAliasing == 4 && report.textureMipmapLimit == 0;
                File.WriteAllText(Path.Combine(output, "lighting-audit.json"), JsonUtility.ToJson(report, true));
                if (!report.passed) throw new InvalidOperationException("Lighting audit failed; inspect Validation/ArtUpgrade/lighting-audit.json.");
                Debug.Log("Water Garden lighting audit passed in all nine scenes.");
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        static PostProcessProfile ConfigureProfile()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(Directory.GetParent(Application.dataPath).FullName, ProfilePath)));
            AssetDatabase.Refresh();
            var profile = AssetDatabase.LoadAssetAtPath<PostProcessProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<PostProcessProfile>();
                profile.name = "Water Garden - Clear Stone (PC)";
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            AmbientOcclusion ao = Settings<AmbientOcclusion>(profile);
            ao.enabled.Override(true);
            ao.mode.Override(AmbientOcclusionMode.ScalableAmbientObscurance);
            ao.intensity.Override(.30f);
            ao.radius.Override(2f);
            ao.quality.Override(AmbientOcclusionQuality.High);
            ao.color.Override(new Color(.12f, .15f, .14f));
            ao.ambientOnly.Override(false);
            UnityEngine.Rendering.PostProcessing.Bloom bloom = Settings<UnityEngine.Rendering.PostProcessing.Bloom>(profile);
            bloom.enabled.Override(true);
            bloom.intensity.Override(.065f);
            bloom.threshold.Override(1.15f);
            bloom.softKnee.Override(.45f);
            bloom.diffusion.Override(3f);
            bloom.clamp.Override(2.5f);
            bloom.dirtIntensity.Override(0f);
            // The authored HUD uses a camera-space canvas. Keep its whites and text
            // legible: no tonemapping, exposure lift, vignette, grain or depth of field.
            ColorGrading grading = Settings<ColorGrading>(profile);
            grading.enabled.Override(true);
            grading.gradingMode.Override(GradingMode.LowDefinitionRange);
            grading.tonemapper.Override(Tonemapper.None);
            grading.saturation.Override(-2f);
            grading.contrast.Override(2f);
            grading.temperature.Override(0f);
            grading.tint.Override(0f);
            grading.colorFilter.Override(Color.white);
            EditorUtility.SetDirty(ao);
            EditorUtility.SetDirty(bloom);
            EditorUtility.SetDirty(grading);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        static T Settings<T>(PostProcessProfile profile) where T : PostProcessEffectSettings
        {
            T setting = profile.GetSetting<T>();
            if (setting != null) return setting;
            setting = profile.AddSettings<T>();
            AssetDatabase.AddObjectToAsset(setting, profile);
            return setting;
        }

        static void ConfigurePCQuality()
        {
            UnityEngine.Object asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset").First();
            var serialized = new SerializedObject(asset);
            var presets = serialized.FindProperty("m_QualitySettings");
            int pc = presets.arraySize - 1;
            SerializedProperty quality = presets.GetArrayElementAtIndex(pc);
            Set(quality, "pixelLightCount", 4);
            Set(quality, "shadows", (int)ShadowQuality.All);
            Set(quality, "shadowResolution", (int)ShadowResolution.VeryHigh);
            Set(quality, "shadowCascades", 4);
            Set(quality, "shadowDistance", 850f);
            Set(quality, "antiAliasing", 4);
            Set(quality, "globalTextureMipmapLimit", 0);
            Set(quality, "anisotropicTextures", (int)AnisotropicFiltering.ForceEnable);
            Set(quality, "softParticles", true);
            Set(quality, "realtimeReflectionProbes", true);
            Set(quality, "lodBias", 2f);
            Set(quality, "vSyncCount", 1);
            serialized.FindProperty("m_CurrentQuality").intValue = pc;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            QualitySettings.SetQualityLevel(pc, true);
        }

        static void Set(SerializedProperty parent, string field, int value) { parent.FindPropertyRelative(field).intValue = value; }
        static void Set(SerializedProperty parent, string field, float value) { parent.FindPropertyRelative(field).floatValue = value; }
        static void Set(SerializedProperty parent, string field, bool value) { parent.FindPropertyRelative(field).boolValue = value; }

        static Report NewReport()
        {
            return new Report {
                generatedUtc = DateTime.UtcNow.ToString("o"), unityVersion = Application.unityVersion, profile = ProfilePath,
                allowedChanges = new[] { "RenderSettings ambient mode/color/intensity, reflections, fog", "Directional Light color/intensity/bounce/shadow properties", "Camera HDR/MSAA/depth-texture presentation flags", "PostProcessLayer and global PostProcessVolume components on existing main camera", "Highest PC quality preset and active Editor quality" }
            };
        }
        static void FillQuality(Report report)
        {
            report.activeQuality = QualitySettings.GetQualityLevel();
            report.antiAliasing = QualitySettings.antiAliasing;
            report.textureMipmapLimit = QualitySettings.globalTextureMipmapLimit;
            report.shadowDistance = QualitySettings.shadowDistance;
        }
        static void CompleteCheck(Scene scene, SceneCheck item)
        {
            item.protectedAfter = ProtectedFingerprint(scene);
            item.cameraProjectionAfter = CameraFingerprint(scene);
            item.hierarchyAndGameplayUnchanged = item.protectedBefore == item.protectedAfter;
            item.cameraProjectionUnchanged = item.cameraProjectionBefore == item.cameraProjectionAfter;
            item.flatAmbientForNativeNight = RenderSettings.ambientMode == AmbientMode.Flat;
            item.ambientRed = RenderSettings.ambientLight.r;
            item.ambientGreen = RenderSettings.ambientLight.g;
            item.ambientBlue = RenderSettings.ambientLight.b;
        }

        // Path/index and original object states include every scene object, even inactive
        // nested UI. Serialized colliders and runtime behaviours protect links and logic.
        // Renderers/MeshFilters/UI artwork are omitted so other art tools can work safely.
        static string ProtectedFingerprint(Scene scene)
        {
            var result = new StringBuilder();
            foreach (Transform transform in Components<Transform>(scene))
            {
                GameObject go = transform.gameObject;
                result.Append(PathOf(transform)).Append('|').Append(go.name).Append('|').Append(go.activeSelf).Append('|').Append(go.layer).Append('|').Append(go.tag).Append('|');
                result.Append(StableSerialization(transform)).AppendLine();
                foreach (Component component in go.GetComponents<Component>())
                {
                    if (component == null) { result.AppendLine("missing-component"); continue; }
                    Type type = component.GetType();
                    bool runtimeScript = component is MonoBehaviour && type.Namespace != "UnityEngine.Rendering.PostProcessing" && type.Assembly.GetName().Name.StartsWith("Assembly-CSharp", StringComparison.Ordinal);
                    if (!(component is Collider) && !(component is Collider2D) && !(component is Rigidbody) && !(component is Rigidbody2D) && !runtimeScript) continue;
                    result.Append(type.FullName).Append('|').Append(StableSerialization(component)).AppendLine();
                }
            }
            return Hash(result.ToString());
        }
        static string CameraFingerprint(Scene scene)
        {
            var result = new StringBuilder();
            foreach (Camera camera in Components<Camera>(scene))
            {
                result.Append(PathOf(camera.transform)).Append('|').Append(camera.enabled).Append('|').Append(camera.orthographic).Append('|');
                result.Append(camera.fieldOfView.ToString("R", CultureInfo.InvariantCulture)).Append('|').Append(camera.orthographicSize.ToString("R", CultureInfo.InvariantCulture)).Append('|');
                result.Append(camera.nearClipPlane.ToString("R", CultureInfo.InvariantCulture)).Append('|').Append(camera.farClipPlane.ToString("R", CultureInfo.InvariantCulture)).Append('|');
                result.Append(camera.rect).Append('|').Append(camera.cullingMask).Append('|').Append(camera.depth).Append('|').Append(camera.targetDisplay).AppendLine();
            }
            return Hash(result.ToString());
        }
        static string PathOf(Transform transform)
        {
            string path = transform.GetSiblingIndex().ToString();
            for (Transform parent = transform.parent; parent != null; parent = parent.parent) path = parent.GetSiblingIndex() + "/" + path;
            return path;
        }
        static string Hash(string value)
        {
            using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "").ToLowerInvariant();
        }
        static string StableSerialization(UnityEngine.Object value)
        {
            // Editor JSON uses transient instance IDs. Resolve them to persistent
            // scene/asset identities so Audit also works after closing/reopening scenes.
            return Regex.Replace(EditorJsonUtility.ToJson(value), "\\\"instanceID\\\"\\s*:\\s*(-?[0-9]+)", match => {
                int id = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                UnityEngine.Object reference = id == 0 ? null : EditorUtility.InstanceIDToObject(id);
                if (reference == null) return "\"instanceID\":\"null\"";
                return "\"instanceID\":\"" + GlobalObjectId.GetGlobalObjectIdSlow(reference) + "\"";
            });
        }
        static IEnumerable<T> Components<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects()) foreach (T component in root.GetComponentsInChildren<T>(true)) yield return component;
        }
        static void Backup(string project, string backup, string path)
        {
            string original = Path.Combine(project, path);
            string copy = Path.Combine(backup, path);
            Directory.CreateDirectory(Path.GetDirectoryName(copy));
            if (!File.Exists(copy)) File.Copy(original, copy);
            if (File.Exists(original + ".meta") && !File.Exists(copy + ".meta")) File.Copy(original + ".meta", copy + ".meta");
        }
        static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Scene art tools require Edit mode.");
        }
    }
}
#endif
