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
    public static class FlowWaterFountainAnimationValidation
    {
        const string Key = "FlowWater.FountainAnimationValidation.";
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
        }
        static Report report;
        static IEnumerator routine;
        static bool busy, locked;
        static int nextFrame;
        static double deadline;
        static string Output { get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation", "FountainAnimation"); } }
        static FlowWaterFountainAnimationValidation()
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
        [MenuItem("Tools/Flow Water/Validate Original Fountain Animator Recipes")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start camera validation in Edit mode.");
            SceneSetup[] scenes = EditorSceneManager.GetSceneManagerSetup();
            foreach (SceneSetup scene in scenes)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scene.path).isDirty) throw new InvalidOperationException("Save edited scenes first.");
            var prefs = new Prefs();
            foreach (string key in new[] { "helpRotation", "Recovery.LastLevelId", "Recovery.Complete.1101", "Recovery.Completed.gm1101", "Recovery.Complete.1193", "Recovery.Completed.gm1193", "esTablet", "OptionSelLev", "primeraVez", "MusicaON", "Soundlevel", "language" })
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
                    Time.timeScale = 4; deadline = EditorApplication.timeSinceStartup + 90; routine = Exercise();
                }
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Camera/tutorial validation exceeded90 seconds.");
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
            foreach (int level in new[] { 1101, 1193 })
            {
                FlowWaterRecoveryUI.Instance.StartGame(level);
                while (RecoveredFlowGame.Instance == null || RecoveredFlowGame.Instance.CurrentLevelId != level || RecoveredFlowGame.Instance.PieceRoot == null) yield return null;
                RecoveredFlowGame game = RecoveredFlowGame.Instance;
                game.Solve(); RecoveredWaterNetwork.Instance.Rebuild();
                float until = Time.time + 180;
                while (RecoveredWaterNetwork.Instance.HasPendingPropagation && Time.time < until) yield return null;
                Require("gm" + level + "-actual-flow-settled", !RecoveredWaterNetwork.Instance.HasPendingPropagation &&
                    RecoveredWaterNetwork.Instance.ActiveFountainCount >= RecoveredWaterNetwork.Instance.RequiredFountainCount && RecoveredWaterNetwork.Instance.RequiredFountainCount > 0,
                    "actual original source to all targets before inspecting generated fountain");
                string code = level == 1101 ? "3" : "2";
                RecoveredNativeFountainAnimation[] bindings = game.PieceRoot.GetComponentsInChildren<RecoveredNativeFountainAnimation>(true)
                    .Where(b => b.NativeConfiguration == code && b.gameObject.activeInHierarchy).ToArray();
                Require("gm" + level + "-native-generated-animation-owner", bindings.Length > 0, "actual production recipe owners with configAnim=" + code + "; count=" + bindings.Length);
                bool correctParameter = false, reachedState = false, moving = false;
                var samples = new Dictionary<RecoveredNativeFountainAnimation, string>();
                until = Time.time + 8;
                while (Time.time < until && (!correctParameter || !reachedState || !moving))
                {
                    foreach (RecoveredNativeFountainAnimation binding in bindings)
                    {
                        if (binding == null) continue;
                        Animator animator = binding.GetComponent<Animator>();
                        if (animator == null || !animator.enabled) continue;
                        correctParameter |= animator.GetInteger("animcode") == Int32.Parse(code);
                        var controller = animator.runtimeAnimatorController as AnimatorController;
                        if (controller != null)
                        {
                            foreach (ChildAnimatorState state in controller.layers[0].stateMachine.states)
                                foreach (AnimatorStateTransition transition in state.state.transitions)
                                    if (transition.destinationState != null && transition.conditions.Any(c => c.parameter == "animcode" && c.mode == AnimatorConditionMode.Equals && c.threshold == Int32.Parse(code)))
                                        reachedState |= animator.GetCurrentAnimatorStateInfo(0).shortNameHash == Animator.StringToHash(transition.destinationState.name);
                        }
                        string sample = MotionSignature(binding.transform);
                        string old;
                        if (samples.TryGetValue(binding, out old)) moving |= old != sample;
                        else samples[binding] = sample;
                    }
                    yield return null;
                }
                Require("gm" + level + "-native-integer-survives-rebind", correctParameter, "original animcode=" + code);
                Require("gm" + level + "-actual-controller-transition", reachedState, "actual current state matches original animcode transition destination");
                Require("gm" + level + "-actual-fountain-motion", moving, "actual native instance transforms/particle velocity changed over real Unity frames; no injected Animator.Update");
                RecoveredWaterNetwork.Instance.Rebuild(); yield return null;
                Require("gm" + level + "-unrelated-rebuild-keeps-animation-code", bindings.Any(b => b != null && b.GetComponent<Animator>().GetInteger("animcode") == Int32.Parse(code)), "native parameter remains after production unrelated graph rebuild");
            }
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
            Directory.CreateDirectory(Output); File.WriteAllText(Path.Combine(Output, "fountain-animation-validation.json"), JsonUtility.ToJson(report, true));
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
