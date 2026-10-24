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
    public static class FlowWaterPitAnimationDiagnostic
    {
        const string Key = "FlowWater.PitAnimationDiagnostic.";
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
            public List<Observation> observations = new List<Observation>();
        }
        [Serializable] sealed class Observation
        {
            public float scaled, normalized, stateSpeed, clipLength, constant, min, max, multiplier, expected;
            public int state, particleCount;
            public string clip, target, mode, particle;
        }
        static readonly List<Observation> observations = new List<Observation>();
        static Report report;
        static IEnumerator routine;
        static bool busy, locked;
        static int nextFrame;
        static double deadline;
        static string Output { get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation", "DecorativePitDiagnostics"); } }
        static FlowWaterPitAnimationDiagnostic()
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
        [MenuItem("Tools/Flow Water/Diagnose Original Pit Particle Animation")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start camera validation in Edit mode.");
            SceneSetup[] scenes = EditorSceneManager.GetSceneManagerSetup();
            foreach (SceneSetup scene in scenes)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scene.path).isDirty) throw new InvalidOperationException("Save edited scenes first.");
            var prefs = new Prefs();
            foreach (string key in new[] { "helpRotation", "Recovery.LastLevelId", "Recovery.Complete.2001", "Recovery.Completed.gm2001", "esTablet", "OptionSelLev", "primeraVez", "MusicaON", "Soundlevel", "language" })
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
            FlowWaterRecoveryUI.Instance.StartGame(2001);
            while (RecoveredFlowGame.Instance == null || RecoveredFlowGame.Instance.CurrentLevelId != 2001 || RecoveredFlowGame.Instance.PieceRoot == null) yield return null;
            RecoveredFlowGame game = RecoveredFlowGame.Instance; game.Solve(); RecoveredWaterNetwork water = RecoveredWaterNetwork.Instance; water.Rebuild();
            float until = Time.time + 180;
            while (water.HasPendingPropagation && Time.time < until) yield return null;
            Require("actual2001-full-flow", !water.HasPendingPropagation && water.ActiveFountainCount >= water.RequiredFountainCount && water.RequiredFountainCount > 0, water.ActiveFountainCount + "/" + water.RequiredFountainCount);
            RecoveredNativeDecorativePits owner = game.PieceRoot.GetComponentsInChildren<RecoveredNativeDecorativePits>(true).FirstOrDefault(b =>
                b.NativeAnimatorCode == 1 && b.NativeInstantiateVA == "0x10ae784" && b.GetComponentInParent<RecoveryPiece>().SourceRecordIndex == 81);
            Require("actual-code1-record81-owner", owner != null, "actual native redE original FT3 instance, no test parameter assignment");
            Animator animator = owner.GetComponent<Animator>();
            ParticleSystem particles = owner.transform.GetChild(0).GetComponent<ParticleSystem>();
            Require("original-child0-particles", particles != null && particles.name == "FontTest9 (1)", "real curve target path");
            until = Time.time + 25;
            while (Time.time < until)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(0);
                AnimationClip clip = clips.Length > 0 ? clips[0].clip : null;
                EditorCurveBinding binding = clip == null ? new EditorCurveBinding() : AnimationUtility.GetCurveBindings(clip).FirstOrDefault(c => c.path == "FontTest9 (1)" && c.propertyName == "VelocityModule.y.scalar");
                AnimationCurve curve = clip == null ? null : AnimationUtility.GetEditorCurve(clip, binding);
                UnityEngine.Object target = clip == null ? null : AnimationUtility.GetAnimatedObject(owner.gameObject, binding);
                var velocity = particles.velocityOverLifetime.y;
                var sample = new ParticleSystem.Particle[Mathf.Min(8, Mathf.Max(1, particles.particleCount))]; int count = particles.GetParticles(sample);
                observations.Add(new Observation { scaled = Time.time, state = state.shortNameHash, normalized = state.normalizedTime, stateSpeed = state.speed,
                    clip = clip == null ? "none" : clip.name, clipLength = clip == null ? 0 : clip.length, target = target == null ? "missing" : target.name + ":" + target.GetType().Name,
                    constant = velocity.constant, min = velocity.constantMin, max = velocity.constantMax, multiplier = velocity.curveMultiplier, mode = velocity.mode.ToString(),
                    expected = curve == null || clip == null ? -1 : curve.Evaluate(Mathf.Repeat(state.normalizedTime, 1) * clip.length), particleCount = particles.particleCount,
                    particle = count == 0 ? "none" : sample[0].randomSeed + ":" + sample[0].position.ToString("F4") + ":" + sample[0].remainingLifetime.ToString("F4") });
                yield return null;
            }
            report.observations = observations;
            Require("actual-state-time-advances", observations.Max(o => o.normalized) > observations.Min(o => o.normalized) + .1f, "actual Animator clock over real frames");
            Require("actual-animation-binding-target-resolves", observations.Any(o => o.target == "FontTest9 (1):ParticleSystem"), "actual original AnimationUtility target resolution");
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
            Directory.CreateDirectory(Output); File.WriteAllText(Path.Combine(Output, "decorative-pit-diagnostic.json"), JsonUtility.ToJson(report, true));
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
