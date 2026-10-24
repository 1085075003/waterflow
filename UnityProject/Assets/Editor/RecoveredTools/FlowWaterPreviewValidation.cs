#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Recovered.FlowWater.Editor
{
    /// <summary>Exercises the actual six category models across native demo delays and a complete return cycle.</summary>
    [InitializeOnLoad]
    public static class FlowWaterPreviewValidation
    {
        const string ActiveKey = "FlowWater.PreviewValidation.Active", PrefsKey = "FlowWater.PreviewValidation.Prefs";
        [Serializable] sealed class Preference { public string key; public bool existed, floating; public int integer; public float number; }
        [Serializable] sealed class Preferences { public List<Preference> entries = new List<Preference>(); }
        [Serializable] public sealed class Check { public string name, detail; public bool passed; }
        [Serializable] public sealed class Report
        {
            public string startedUtc, finishedUtc, unityVersion;
            public bool passed, preferencesRestored;
            public List<Check> checks = new List<Check>();
            public List<string> errors = new List<string>();
            public List<string> screenshots = new List<string>();
        }
        static Report report;
        static IEnumerator routine;
        static bool locked, busy;
        static float originalScale;
        static double deadline;
        static int resume;
        sealed class MotionSample { public string kind; public Vector4 value; }
        static string Output { get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation", "CategoryPreview"); } }
        static FlowWaterRecoveryUI UI { get { return FlowWaterRecoveryUI.Instance; } }
        static FlowWaterPreviewValidation()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode) Unlock();
                if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(ActiveKey, false))
                { Restore(); SessionState.EraseBool(ActiveKey); }
            };
            Application.logMessageReceived += (message, stack, type) =>
            {
                if (report != null && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
                    report.errors.Add(message + "\n" + stack);
            };
        }
        [MenuItem("Tools/Flow Water/Validate Original Category Animations")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start category validation from Edit mode.");
            Backup();
            SessionState.SetBool(ActiveKey, true);
            EditorSceneManager.OpenScene("Assets/Scenes/SelLevel0.unity", OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }
        static void Update()
        {
            if (busy || !SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            busy = true;
            try
            {
                if (report == null)
                {
                    AssetDatabase.DisallowAutoRefresh(); EditorApplication.LockReloadAssemblies(); locked = true;
                    originalScale = Time.timeScale; Time.timeScale = 8;
                    report = new Report { startedUtc = DateTime.UtcNow.ToString("o"), unityVersion = Application.unityVersion };
                    deadline = EditorApplication.timeSinceStartup + 300;
                    routine = Exercise(); resume = Time.frameCount + 8;
                }
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Category animation validation exceeded 300 seconds.");
                if (Time.frameCount < resume) return;
                if (!routine.MoveNext()) { Finish(); return; }
                resume = Time.frameCount + 1;
            }
            catch (Exception error) { if (report == null) report = new Report(); report.errors.Add(error.ToString()); Finish(); }
            finally { busy = false; }
        }
        static IEnumerator Exercise()
        {
            CheckState("unity-version", Application.unityVersion == "2022.3.28f1", Application.unityVersion);
            for (int category = 1; category <= 6; category++)
            {
                var control = UI.OriginalControls.FirstOrDefault(x => x.source != null && x.source.activeInHierarchy &&
                    x.argument == category && x.method == "botonTabDown");
                CheckState("category-" + category + "-control", control != null, "Original category Button/EventTrigger");
                if (control == null) continue;
                if (control.button != null) control.button.onClick.Invoke();
                else if (control.trigger != null)
                {
                    var item = control.trigger.triggers.FirstOrDefault(x => x.eventID == control.eventType);
                    if (item != null) item.callback.Invoke(new PointerEventData(EventSystem.current));
                }
                yield return null;
                var demo = UnityEngine.Object.FindObjectsOfType<RecoveredPreviewDemoSequence>().FirstOrDefault(x => x.gameObject.activeInHierarchy);
                CheckState("category-" + category + "-model", demo != null, "Only the selected original group is active");
                if (demo == null) continue;
                float start = Time.time;
                var initial = Positions(demo);
                while (Time.time - start < 3.5f) yield return null;
                if (category <= 2)
                {
                    CheckState("category-" + category + "-authored-motion", demo.ControllerCount == 0 && demo.GetComponent<RecoveredWaterMotion>() != null,
                        "Baked original water and authored UV/animation driver; no spring controller added");
                    var bakedBefore = WaterMotion(demo);
                    Capture("category" + category + "-baked-a.png");
                    float bakedNext = Time.unscaledTime + .35f;
                    while (Time.unscaledTime < bakedNext) yield return null;
                    var bakedAfter = WaterMotion(demo);
                    int bakedProgressed = bakedBefore.Count(pair => bakedAfter.ContainsKey(pair.Key) &&
                        pair.Value.kind == bakedAfter[pair.Key].kind && (pair.Value.value - bakedAfter[pair.Key].value).sqrMagnitude > .000000001f);
                    CheckState("category-" + category + "-water-actually-animates", bakedBefore.Count > 0 && bakedProgressed > 0,
                        "Active original baked water samples=" + bakedBefore.Count + "; progressed=" + bakedProgressed + "; wall interval>=.35s");
                    Capture("category" + category + "-baked-b.png");
                    continue;
                }
                CheckState("category-" + category + "-initial-delay", Same(initial, Positions(demo)), "Original movement starts after the native five-second delay");
                CheckState("category-" + category + "-controller", demo.ControllerCount == 1 && demo.MissingReferences == 0,
                    demo.CurrentPhase + "; missing=" + demo.MissingReferences);
                Capture("category" + category + "-initial.png");
                while (!demo.CurrentPhase.Contains(":2:") && Time.time - start < 100) yield return null;
                CheckState("category-" + category + "-forward", demo.CurrentPhase.Contains(":2:") && !Same(initial, Positions(demo)), demo.CurrentPhase);
                float settle = Time.unscaledTime + .5f;
                while (Time.unscaledTime < settle) yield return null;
                var water = demo.GetComponent<RecoveredPreviewWater>();
                CheckState("category-" + category + "-native-water", water != null && water.SourceCount > 0 && water.WetControllerCount > 0 && water.OriginalEffectCount > 0,
                    water == null ? "No preview water" : "source=" + water.SourceCount + "; wet=" + water.WetControllerCount + "; effects=" + water.OriginalEffectCount);
                var poseBeforeFlow = Positions(demo);
                string phaseBeforeFlow = demo.CurrentPhase;
                var motionBefore = WaterMotion(demo);
                Capture("category" + category + "-forward-a.png");
                float nextFrame = Time.unscaledTime + .35f;
                while (Time.unscaledTime < nextFrame) yield return null;
                var motionAfter = WaterMotion(demo);
                int progressed = motionBefore.Count(pair => motionAfter.ContainsKey(pair.Key) &&
                    pair.Value.kind == motionAfter[pair.Key].kind && (pair.Value.value - motionAfter[pair.Key].value).sqrMagnitude > .000000001f);
                CheckState("category-" + category + "-flow-isolated", phaseBeforeFlow == demo.CurrentPhase && Same(poseBeforeFlow, Positions(demo)),
                    "Same native hold stage and model positions across two captures; " + demo.CurrentPhase);
                CheckState("category-" + category + "-water-actually-animates", motionBefore.Count > 0 && progressed > 0,
                    "Active water UV/Animator/particle samples=" + motionBefore.Count + "; progressed=" + progressed + "; wall interval>=.35s");
                Capture("category" + category + "-forward-b.png");
                while (demo.CompletedCycles < 1 && Time.time - start < 180) yield return null;
                CheckState("category-" + category + "-return-cycle", demo.CompletedCycles >= 1, demo.CurrentPhase + "; transitions=" + demo.TransitionCount);
                // Resorte2b changes its palette only after its native five-second restart wait.
                if (category >= 4)
                {
                    while (demo.CurrentColorCycle < 2 && Time.time - start < 190) yield return null;
                    CheckState("category-" + category + "-next-palette", demo.CurrentColorCycle == 2, "Native three-palette cycle=" + demo.CurrentColorCycle);
                }
                Capture("category" + category + "-returned.png");
            }
        }
        static Vector3[] Positions(Component root)
        {
            var source = root.GetComponentsInChildren<MonoBehaviour>(true).FirstOrDefault(x => x != null &&
                new[] { "Resorte2", "Resorte2b", "Resorte2c", "Resorte2d" }.Contains(x.GetType().Name));
            if (source == null) return new Vector3[0];
            return source.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance).Where(x => x.FieldType == typeof(GameObject) &&
                x.Name.StartsWith("objeto", StringComparison.Ordinal)).OrderBy(x => x.Name).Select(x => x.GetValue(source) as GameObject)
                .Where(x => x != null).Select(x => x.transform.localPosition).ToArray();
        }
        static bool Same(Vector3[] first, Vector3[] second)
        { return first.Length == second.Length && first.Select((point, index) => (point - second[index]).sqrMagnitude < .0001f).All(x => x); }
        static Dictionary<int, MotionSample> WaterMotion(Component root)
        {
            var result = new Dictionary<int, MotionSample>();
            var properties = new MaterialPropertyBlock();
            int main = Shader.PropertyToID("_MainTex_ST"), detail = Shader.PropertyToID("_DetailTex_ST");
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy || !WaterObject(renderer.transform, root.transform)) continue;
                renderer.GetPropertyBlock(properties);
                var st = properties.GetVector(main);
                var second = properties.GetVector(detail);
                if (st == Vector4.zero && renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty(main)) st = renderer.sharedMaterial.GetVector(main);
                if (second == Vector4.zero && renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty(detail)) second = renderer.sharedMaterial.GetVector(detail);
                result[renderer.GetInstanceID()] = new MotionSample { kind = "water-UV", value = st + second * .371f };
                properties.Clear();
            }
            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
            {
                if (!animator.isActiveAndEnabled || animator.runtimeAnimatorController == null || !WaterObject(animator.transform, root.transform)) continue;
                var state = animator.GetCurrentAnimatorStateInfo(0);
                result[animator.GetInstanceID()] = new MotionSample { kind = "water-Animator", value = new Vector4(state.normalizedTime, state.shortNameHash, 0, 0) };
            }
            foreach (var particles in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (!particles.gameObject.activeInHierarchy || !particles.isPlaying || !WaterObject(particles.transform, root.transform)) continue;
                result[particles.GetInstanceID()] = new MotionSample { kind = "water-particles", value = new Vector4(particles.time, particles.particleCount, 0, 0) };
            }
            return result;
        }
        static bool WaterObject(Transform target, Transform boundary)
        {
            for (var node = target; node != null && node != boundary; node = node.parent)
            {
                string label = node.name.ToLowerInvariant();
                if (label.Contains("agua") || label.Contains("water") || label.Contains("chorro") || label.Contains("cascada")) return true;
            }
            return false;
        }
        static void CheckState(string name, bool passed, string detail)
        { report.checks.Add(new Check { name = name, passed = passed, detail = detail }); }
        static void Capture(string file)
        {
            Directory.CreateDirectory(Output);
            string path = Path.Combine(Output, file);
            FlowWaterValidation.SaveVisibleFrame(path); report.screenshots.Add(path);
            var demo = UnityEngine.Object.FindObjectsOfType<RecoveredPreviewDemoSequence>().FirstOrDefault(x => x.gameObject.activeInHierarchy);
            var camera = Camera.main;
            if (camera == null) camera = UnityEngine.Object.FindObjectsOfType<Camera>().FirstOrDefault(x => x.isActiveAndEnabled);
            if (demo != null) FlowWaterVisualDiagnostics.Write(Path.ChangeExtension(path, ".renderers.json"), camera, 1280, 720, demo.transform);
        }
        static void Backup()
        {
            var prefs = new Preferences();
            Action<string, bool> add = (key, floating) => prefs.entries.Add(new Preference { key = key, floating = floating,
                existed = PlayerPrefs.HasKey(key), integer = PlayerPrefs.GetInt(key), number = PlayerPrefs.GetFloat(key) });
            foreach (var key in new[] { "Soundlevel", "MusicaON", "Recovery.LastLevelId", "primeraVez", "esTablet", "OptionSelLev" }) add(key, false);
            add("Conf_music", true); add("Conf_sound", true);
            foreach (var level in RecoveryCatalog.Load().levels)
            { add("Recovery.Complete." + level.assetId, false); add("Recovery.Completed.gm" + level.assetId, false); }
            SessionState.SetString(PrefsKey, JsonUtility.ToJson(prefs));
        }
        static bool Restore()
        {
            string json = SessionState.GetString(PrefsKey, "");
            if (String.IsNullOrEmpty(json)) return false;
            var prefs = JsonUtility.FromJson<Preferences>(json);
            foreach (var value in prefs.entries)
            {
                if (!value.existed) PlayerPrefs.DeleteKey(value.key);
                else if (value.floating) PlayerPrefs.SetFloat(value.key, value.number);
                else PlayerPrefs.SetInt(value.key, value.integer);
            }
            PlayerPrefs.Save(); SessionState.EraseString(PrefsKey);
            return prefs.entries.All(x => PlayerPrefs.HasKey(x.key) == x.existed && (!x.existed || (x.floating ? Mathf.Approximately(PlayerPrefs.GetFloat(x.key), x.number) : PlayerPrefs.GetInt(x.key) == x.integer)));
        }
        static void Unlock()
        {
            if (!locked) return;
            locked = false; EditorApplication.UnlockReloadAssemblies(); AssetDatabase.AllowAutoRefresh();
        }
        static void Finish()
        {
            Time.timeScale = originalScale;
            if (report == null) report = new Report();
            report.preferencesRestored = Restore(); report.finishedUtc = DateTime.UtcNow.ToString("o");
            report.passed = report.preferencesRestored && report.errors.Count == 0 && report.checks.Count > 0 && report.checks.All(x => x.passed);
            Directory.CreateDirectory(Output); File.WriteAllText(Path.Combine(Output, "category-preview-validation.json"), JsonUtility.ToJson(report, true));
            bool passed = report.passed;
            SessionState.EraseBool(ActiveKey); Unlock(); routine = null; report = null;
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1); else EditorApplication.isPlaying = false;
        }
    }
}
#endif
