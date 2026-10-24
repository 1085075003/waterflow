#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Recovered.FlowWater.Editor
{
    [InitializeOnLoad]
    public static class FlowWaterDecorativePitValidation
    {
        const string Key = "FlowWater.DecorativePitValidation.";
        [Serializable] sealed class Pref { public string key, text; public bool existed, isString; public int value; }
        [Serializable] sealed class Prefs { public List<Pref> values = new List<Pref>(); }
        [Serializable] sealed class SavedScene { public string path; public bool loaded, active; }
        [Serializable] sealed class Scenes { public SavedScene[] values; }
        [Serializable] sealed class Check { public string name, detail; public bool passed; }
        [Serializable] sealed class Report
        {
            public string unityVersion, startedUtc, finishedUtc;
            public bool passed, preferencesRestored;
            public List<Check> checks = new List<Check>();
            public List<string> errors = new List<string>();
            public List<ColorSample> colors = new List<ColorSample>();
        }
        [Serializable] sealed class ColorSample { public int color; public bool night; public string gradient; public string[] nativePrefabs; }
        static readonly List<ColorSample> colorSamples = new List<ColorSample>();
        static Report report;
        static IEnumerator routine;
        static bool busy, locked;
        static int nextFrame;
        static double deadline;
        static string Output { get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation", "DecorativePits"); } }
        static FlowWaterDecorativePitValidation()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode) Unlock();
                if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Key + "active", false)) return;
                Restore(); SessionState.EraseBool(Key + "active");
                int exit = SessionState.GetInt(Key + "exit", -1);
                if (Application.isBatchMode && exit >= 0) EditorApplication.Exit(exit);
            };
            Application.logMessageReceived += (text, stack, type) =>
            {
                if (report != null && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) report.errors.Add(text + "\n" + stack);
            };
        }
        [MenuItem("Tools/Flow Water/Validate Original Decorative Pit Providers")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start decorative-pit validation in Edit mode.");
            SceneSetup[] scenes = EditorSceneManager.GetSceneManagerSetup();
            foreach (SceneSetup scene in scenes)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scene.path).isDirty) throw new InvalidOperationException("Save edited scenes first.");
            var prefs = new Prefs();
            foreach (string key in new[] { "helpRotation", "Recovery.LastLevelId", "Recovery.Complete.1075", "Recovery.Completed.gm1075", "Recovery.Complete.1701", "Recovery.Completed.gm1701", "Recovery.Complete.1705", "Recovery.Completed.gm1705", "Recovery.Complete.1758", "Recovery.Completed.gm1758", "Recovery.Complete.1949", "Recovery.Completed.gm1949", "Recovery.Complete.2001", "Recovery.Completed.gm2001", "esTablet", "OptionSelLev", "primeraVez", "MusicaON", "Soundlevel", "language" })
                prefs.values.Add(new Pref { key = key, existed = PlayerPrefs.HasKey(key), isString = key == "language", value = PlayerPrefs.GetInt(key), text = PlayerPrefs.GetString(key) });
            SessionState.SetString(Key + "prefs", JsonUtility.ToJson(prefs));
            SessionState.SetString(Key + "scenes", JsonUtility.ToJson(new Scenes { values = scenes.Select(s => new SavedScene { path = s.path, loaded = s.isLoaded, active = s.isActive }).ToArray() }));
            SessionState.SetFloat(Key + "scale", Time.timeScale); SessionState.SetBool(Key + "active", true);
            RecoveryCatalogBuilder.Build(); EditorSceneManager.OpenScene("Assets/Scenes/Portada.unity", OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }
        static void Update()
        {
            if (busy || !SessionState.GetBool(Key + "active", false) || !EditorApplication.isPlaying || EditorApplication.isCompiling || Time.frameCount < nextFrame) return;
            busy = true;
            try
            {
                if (report == null)
                {
                    AssetDatabase.DisallowAutoRefresh(); EditorApplication.LockReloadAssemblies(); locked = true;
                    report = new Report { unityVersion = Application.unityVersion, startedUtc = DateTime.UtcNow.ToString("o") };
                    Time.timeScale = 4; deadline = EditorApplication.timeSinceStartup + 300; routine = Exercise();
                }
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Decorative-pit validation exceeded300 seconds.");
                if (!routine.MoveNext()) { Finish(); return; }
                nextFrame = Time.frameCount + 1;
            }
            catch (Exception ex) { report.errors.Add(ex.ToString()); Finish(); }
            finally { busy = false; }
        }
        static IEnumerator Exercise()
        {
            Require("unity-2022.3.28f1", Application.unityVersion == "2022.3.28f1", Application.unityVersion);
            while (FlowWaterRecoveryUI.Instance == null) yield return null;
            foreach (int level in new[] { 1075, 1701, 1705, 1758, 1949, 2001 })
            {
                FlowWaterRecoveryUI.Instance.StartGame(level);
                while (RecoveredFlowGame.Instance == null || RecoveredFlowGame.Instance.CurrentLevelId != level || RecoveredFlowGame.Instance.PieceRoot == null) yield return null;
                RecoveredFlowGame game = RecoveredFlowGame.Instance;
                foreach (object step in DismissTutorial()) yield return step;
                game.Solve(); RecoveredWaterNetwork water = RecoveredWaterNetwork.Instance; water.Rebuild();
                float until = Time.time + 180;
                while (water.HasPendingPropagation && Time.time < until) yield return null;
                Require("gm" + level + "-actual-full-colored-flow", !water.HasPendingPropagation && water.RequiredFountainCount > 0 && water.ActiveFountainCount >= water.RequiredFountainCount,
                    water.ActiveFountainCount + "/" + water.RequiredFountainCount + " actual correct-color fountains; decorative provider does not change graph or counts");
                RecoveredNativeDecorativePits[] bindings = game.PieceRoot.GetComponentsInChildren<RecoveredNativeDecorativePits>(true).Where(b => b.gameObject.activeInHierarchy).ToArray();
                Require("gm" + level + "-actual-wet-provider-instances", bindings.Length > 0, "new wet original FT3 provider owners=" + bindings.Length);
                int[] codes = level == 1075 ? new[] { 2 } : level == 1758 ? new[] { 5 } : level == 1949 ? new[] { 7 } : level == 2001 ? new[] { 1, 6 } : new int[0];
                foreach (int code in codes)
                {
                    RecoveredNativeDecorativePits[] animated = bindings.Where(b => b.NativeAnimatorCode == code).ToArray();
                    Require("gm" + level + "-actual-native-animcode" + code + "-owner", animated.Length > 0, "actual configured native provider, no injected Animator parameter");
                    bool parameter = false, reachedState = false, moving = false;
                    var samples = new Dictionary<RecoveredNativeDecorativePits, string>();
                    until = Time.time + 12;
                    while (Time.time < until && (!parameter || !reachedState || !moving))
                    {
                        foreach (RecoveredNativeDecorativePits binding in animated)
                        {
                            if (binding == null) continue;
                            Animator animator = binding.GetComponent<Animator>();
                            if (animator == null || !animator.enabled) continue;
                            parameter |= animator.GetInteger("animcode") == code;
                            var controller = animator.runtimeAnimatorController as AnimatorController;
                            if (controller != null)
                            {
                                bool hasCondition = false;
                                foreach (ChildAnimatorState state in controller.layers[0].stateMachine.states)
                                    foreach (AnimatorStateTransition transition in state.state.transitions)
                                        if (transition.destinationState != null && transition.conditions.Any(c => c.parameter == "animcode" && c.mode == AnimatorConditionMode.Equals && c.threshold == code))
                                        { hasCondition = true; reachedState |= animator.GetCurrentAnimatorStateInfo(0).shortNameHash == Animator.StringToHash(transition.destinationState.name); }
                                foreach (AnimatorStateTransition transition in controller.layers[0].stateMachine.anyStateTransitions)
                                    if (transition.destinationState != null && transition.conditions.Any(c => c.parameter == "animcode" && c.mode == AnimatorConditionMode.Equals && c.threshold == code))
                                    { hasCondition = true; reachedState |= animator.GetCurrentAnimatorStateInfo(0).shortNameHash == Animator.StringToHash(transition.destinationState.name); }
                                if (!hasCondition && controller.layers[0].stateMachine.defaultState != null)
                                    reachedState |= animator.GetCurrentAnimatorStateInfo(0).shortNameHash == Animator.StringToHash(controller.layers[0].stateMachine.defaultState.name);
                            }
                            string sample = MotionSignature(binding.transform), old;
                            if (samples.TryGetValue(binding, out old)) moving |= old != sample;
                            else samples[binding] = sample;
                        }
                        yield return null;
                    }
                    Require("gm" + level + "-native-animcode" + code + "-parameter", parameter, "original integer survived production Rebind");
                    Require("gm" + level + "-native-animcode" + code + "-state", reachedState, "actual original conditional state, or original default when no condition consumes that integer");
                    Require("gm" + level + "-native-animcode" + code + "-motion", moving, "actual transforms/original velocity or force modules changed over real Unity frames");
                }
                if (level == 1701)
                {
                    RecoveredNativeDecorativePits velocity = bindings.FirstOrDefault(b => b.NativeInstantiateVA == "0x10ae784" && b.HasVelocityMutation && b.NativeAnimatorCode == 0);
                    Require("gm1701-original-child0-y10-owner", velocity != null, "native configAnim4 performs particle mutation without inventing animcode4");
                    ParticleSystem child = velocity.transform.GetChild(0).GetComponent<ParticleSystem>();
                    Require("gm1701-original-child0-y10", child != null && Mathf.Approximately(child.velocityOverLifetime.y.constant, 10),
                        "actual original Instantiate10AE784 child[0] velocity.y=10, native mutation10AEE04");
                }
                if (level == 1705)
                {
                    RecoveredNativeDecorativePits velocity = bindings.FirstOrDefault(b => b.NativeInstantiateVA == "0x10af59c" && b.HasVelocityMutation && b.NativeAnimatorCode == 0);
                    Require("gm1705-original-child0-velocity-owner", velocity != null, "exact original Instantiate10AF59C, particle override10AF688/10AF6B4");
                    ParticleSystem child = velocity.transform.GetChild(0).GetComponent<ParticleSystem>();
                    Require("gm1705-original-child0-y16-z-minus2", child != null && Mathf.Approximately(child.velocityOverLifetime.y.constant, 16) && Mathf.Approximately(child.velocityOverLifetime.z.constant, -2),
                        "actual original provider child[0] ParticleSystem has native y=16,z=-2 constants");
                    until = Time.time + 3;
                    while (child.particleCount == 0 && Time.time < until) yield return null;
                    Require("gm1705-original-child0-real-particles", child.particleCount > 0, "actual original particle system emits after native mutations");
                }
                if (level == 1075) { CheckFourColorContexts(game); report.colors = colorSamples; }
                if (level == 2001)
                {
                    RecoveredNativeDecorativePits red = bindings.FirstOrDefault(b => b.GetComponentInParent<RecoveryPiece>().SourceRecordIndex == 81 && b.NativeConfiguration.EndsWith("|4|1"));
                    Require("gm2001-original-night-config", red != null, "actual record81 original tagcolor4, configLights2D and FT3 native night branch");
                    bool sub13 = bindings.Where(b => b.GetComponentInParent<RecoveryPiece>().SourceRecordIndex == 81).Any(b =>
                        b.GetComponentsInChildren<changeColorP>(true).Any(c => c.subcolor == 4) || b.GetComponentsInChildren<changeColorU>(true).Any(c => c.subcolor == 4));
                    Require("gm2001-original-night-light-D-remapped4", sub13, "original FT3.doConfigColor remaps night lig2 D to4 before10AECA0 writes exact subcolor; raw D13 is not the native final value");
                }
                int[] ids = bindings.Select(b => b.GetInstanceID()).ToArray(); water.Rebuild(); yield return null;
                Require("gm" + level + "-unrelated-rebuild-retains-native-instances", bindings.All(b => b != null) && ids.SequenceEqual(bindings.Select(b => b.GetInstanceID())), "original provider instances survive unrelated topology rebuild");
                Directory.CreateDirectory(Output);
                FlowWaterRecoveryUI.Instance.CaptureBoardFrame(() => FlowWaterValidation.SaveVisibleFrame(Path.Combine(Output, "gm" + level + "-native-pits.png")));
                yield return null;
            }
            report.colors = colorSamples;
        }
        static IEnumerable DismissTutorial()
        {
            HelpRotate help = FindHelp();
            if (help == null || !help.gameObject.activeInHierarchy) yield break;
            SwipeHandler2 swipe = RecoveredNativeCamera.CurrentSwipe;
            if (swipe == null) throw new InvalidOperationException("Actual visible tutorial has no native camera controller.");
            float yaw = swipe.EsferaCamera.eulerAngles.y;
            swipe.GirarDerecha();
            while (Mathf.Abs(Mathf.DeltaAngle(swipe.EsferaCamera.eulerAngles.y, yaw + 90)) > .25f) yield return null;
            swipe.GirarIzquierda();
            while (Mathf.Abs(Mathf.DeltaAngle(swipe.EsferaCamera.eulerAngles.y, yaw)) > .25f) yield return null;
            float until = Time.time + 1; while (Time.time < until) yield return null;
        }
        static void CheckFourColorContexts(RecoveredFlowGame game)
        {
            RecoveryPiece piece = game.Pieces.First(p => p.SourceRecordIndex == 32);
            ControlPiezaC control = piece.GetComponentInChildren<ControlPiezaC>(true);
            GameObject prefab = control.prefabAguaControl;
            int originalPart = UIManagerSelScript0.parte, originalSubpart = UIManagerSelScript0.subparte;
            var colors = new[] { "", "Azul", "Verde", "Z", "Ro" };
            try
            {
                foreach (bool night in new[] { false, true })
                    for (int color = 1; color <= 4; color++)
                    {
                        UIManagerSelScript0.parte = night ? 36 : 1; UIManagerSelScript0.subparte = 0;
                        GameObject bench = UnityEngine.Object.Instantiate(prefab, control.transform, false);
                        bench.name = "Isolated original provider color component fixture"; bench.transform.localPosition = Vector3.zero; bench.transform.localScale = Vector3.one * .1f;
                        try
                        {
                            foreach (Collider c in bench.GetComponentsInChildren<Collider>(true)) c.enabled = false;
                            foreach (MonoBehaviour b in bench.GetComponentsInChildren<MonoBehaviour>(true)) if (b != null) b.enabled = false;
                            FuenteTest3 origin = bench.GetComponentsInChildren<FuenteTest3>(true).First(f => f.origen == 1);
                            RecoveredNativeDecorativePits.Restore(origin, control, color, (p, parent) =>
                            {
                                GameObject effect = UnityEngine.Object.Instantiate(p, parent, false); effect.SetActive(true);
                                foreach (Collider c in effect.GetComponentsInChildren<Collider>(true)) c.enabled = false;
                                foreach (MonoBehaviour b in effect.GetComponentsInChildren<MonoBehaviour>(true)) if (b != null) b.enabled = false;
                                return effect;
                            });
                            RecoveredNativeDecorativePits[] instances = bench.GetComponentsInChildren<RecoveredNativeDecorativePits>(true);
                            bench.AddComponent<RecoveredWaterMotion>().RestoreAuthoredMotion(1);
                            ParticleSystem particles = instances.SelectMany(i => i.GetComponentsInChildren<ParticleSystem>(true)).FirstOrDefault(p => p.colorOverLifetime.enabled);
                            Require("isolated-original-config-color" + color + (night ? "-night" : "-day"), instances.Any(i => i.NativePrefabField.Contains(colors[color])) && particles != null,
                                "actual1075 record32 native configured component; original prefab selection and actual gradient module, not whole-level success");
                            Gradient gradient = particles.colorOverLifetime.color.gradient;
                            colorSamples.Add(new ColorSample { color = color, night = night, nativePrefabs = instances.Select(i => i.NativePrefabField).ToArray(),
                                gradient = gradient == null ? particles.colorOverLifetime.color.color.ToString("F5") : gradient.Evaluate(0).ToString("F5") + ";" + gradient.Evaluate(.5f).ToString("F5") + ";" + gradient.Evaluate(1).ToString("F5") });
                        }
                        finally { UnityEngine.Object.DestroyImmediate(bench); }
                    }
            }
            finally { UIManagerSelScript0.parte = originalPart; UIManagerSelScript0.subparte = originalSubpart; }
        }
        static string MotionSignature(Transform root)
        {
            var values = new System.Text.StringBuilder();
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                values.Append(child.localPosition.ToString("F4")).Append(child.localRotation.ToString("F4")).Append(child.localScale.ToString("F4"));
            foreach (ParticleSystem particles in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                var velocity = particles.velocityOverLifetime;
                values.Append(velocity.x.constant.ToString("F4")).Append(velocity.y.constant.ToString("F4")).Append(velocity.z.constant.ToString("F4"));
                var force = particles.forceOverLifetime;
                values.Append(force.x.constant.ToString("F4")).Append(force.y.constant.ToString("F4")).Append(force.z.constant.ToString("F4"));
            }
            return values.ToString();
        }
        static HelpRotate FindHelp()
        {
            foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            { HelpRotate result = root.GetComponentInChildren<HelpRotate>(true); if (result != null) return result; }
            return null;
        }
        static void Require(string name, bool passed, string detail)
        {
            report.checks.Add(new Check { name = name, passed = passed, detail = detail });
            if (!passed) throw new InvalidOperationException(name + ": " + detail);
        }
        static void Finish()
        {
            report.finishedUtc = DateTime.UtcNow.ToString("o");
            report.passed = report.errors.Count == 0 && report.checks.All(c => c.passed);
            RestorePreferences(); report.preferencesRestored = true;
            Directory.CreateDirectory(Output); File.WriteAllText(Path.Combine(Output, "decorative-pit-validation.json"), JsonUtility.ToJson(report, true));
            SessionState.SetInt(Key + "exit", report.passed ? 0 : 2); Unlock(); EditorApplication.ExitPlaymode();
        }
        static void RestorePreferences()
        {
            Prefs prefs = JsonUtility.FromJson<Prefs>(SessionState.GetString(Key + "prefs", ""));
            if (prefs == null) return;
            foreach (Pref p in prefs.values)
                if (!p.existed) PlayerPrefs.DeleteKey(p.key); else if (p.isString) PlayerPrefs.SetString(p.key, p.text); else PlayerPrefs.SetInt(p.key, p.value);
            PlayerPrefs.Save(); Time.timeScale = SessionState.GetFloat(Key + "scale", 1);
        }
        static void Restore()
        {
            RestorePreferences();
            Scenes scenes = JsonUtility.FromJson<Scenes>(SessionState.GetString(Key + "scenes", ""));
            if (scenes != null && scenes.values != null && scenes.values.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(scenes.values.Select(s => new SceneSetup { path = s.path, isLoaded = s.loaded, isActive = s.active }).ToArray());
        }
        static void Unlock()
        {
            if (!locked) return; locked = false; EditorApplication.UnlockReloadAssemblies(); AssetDatabase.AllowAutoRefresh();
        }
    }
}
#endif
