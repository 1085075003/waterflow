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
    public static class FlowWaterFountainDispatchValidation
    {
        const string Key = "FlowWater.FountainDispatchValidation.";
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
        static string Output { get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation", "FountainDispatch"); } }
        static FlowWaterFountainDispatchValidation()
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
        [MenuItem("Tools/Flow Water/Validate Original Fountain Dispatch")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start camera validation in Edit mode.");
            SceneSetup[] scenes = EditorSceneManager.GetSceneManagerSetup();
            foreach (SceneSetup scene in scenes)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scene.path).isDirty) throw new InvalidOperationException("Save edited scenes first.");
            var prefs = new Prefs();
            foreach (string key in new[] { "helpRotation", "Recovery.LastLevelId", "Recovery.Complete.456", "Recovery.Completed.gm456", "Recovery.Complete.518", "Recovery.Completed.gm518", "Recovery.Complete.549", "Recovery.Completed.gm549", "Recovery.Complete.1058", "Recovery.Completed.gm1058", "Recovery.Complete.2001", "Recovery.Completed.gm2001", "esTablet", "OptionSelLev", "primeraVez", "MusicaON", "Soundlevel", "language" })
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
                    Time.timeScale = 4; deadline = EditorApplication.timeSinceStartup + 180; routine = Exercise();
                }
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Camera/tutorial validation exceeded180 seconds.");
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
            foreach (int level in new[] { 456, 518, 549, 1058, 2001 })
            {
                FlowWaterRecoveryUI.Instance.StartGame(level);
                while (RecoveredFlowGame.Instance == null || RecoveredFlowGame.Instance.CurrentLevelId != level || RecoveredFlowGame.Instance.PieceRoot == null) yield return null;
                RecoveredFlowGame game = RecoveredFlowGame.Instance;
                foreach (object step in DismissTutorial()) yield return step;
                game.Solve(); RecoveredWaterNetwork water = RecoveredWaterNetwork.Instance; water.Rebuild();
                float until = Time.time + 180;
                while (water.HasPendingPropagation && Time.time < until) yield return null;
                Require("gm" + level + "-actual-full-colored-flow", !water.HasPendingPropagation && water.RequiredFountainCount > 0 && water.ActiveFountainCount >= water.RequiredFountainCount,
                    water.ActiveFountainCount + "/" + water.RequiredFountainCount + " actual correct-color targets, unchanged graph/count semantics");
                RecoveredNativeDecorativePits[] bindings = game.PieceRoot.GetComponentsInChildren<RecoveredNativeDecorativePits>(true).Where(b => b.gameObject.activeInHierarchy).ToArray();
                if (level == 456 || level == 549)
                {
                    foreach (int kind in new[] { 3, 4 })
                    {
                        RecoveredNativeDecorativePits[] small = bindings.Where(b => b.NativeConfiguration.StartsWith("small|" + kind + "|")).ToArray();
                        Require("gm" + level + "-actual-type" + kind + "-native-provider", small.Length > 0, "actual original HA/HB/FA configured fountain owner");
                        Require("gm" + level + "-type" + kind + "-original-default-jet-pose", small.Any(b => b.NativeInstantiateVA == "0x10a8ee4" &&
                            Mathf.Abs(Mathf.Abs(b.transform.localPosition.x) - 4) < .001f && Mathf.Abs(Mathf.Abs(b.transform.localPosition.z) - 4) < .001f && Mathf.Abs(b.transform.localPosition.y - 1.06f) < .001f),
                            "native empty-pit branch uses original dirPlano1/2, localX/Z±4, localY1.06");
                        float particleUntil = Time.time + 3;
                        while (!small.Any(b => b.GetComponentsInChildren<ParticleSystem>(true).Any(ps => ps.particleCount > 0)) && Time.time < particleUntil) yield return null;
                        Require("gm" + level + "-type" + kind + "-actual-original-particles", small.Any(b => b.GetComponentsInChildren<ParticleSystem>(true).Any(ps => ps.particleCount > 0)),
                            "actual original particle systems emit after production wet activation");
                    }
                    Require("gm" + level + "-type4-extra-original-font", bindings.Any(b => b.NativeConfiguration.StartsWith("small|4|") && b.NativeInstantiateVA == "0x10a961c" &&
                        Vector3.Distance(b.transform.localPosition, new Vector3(10, 24, -10)) < .001f), "native type4 additionally doFuente3, original prefabFont/color selection and local(10,24,-10)");
                }
                if (level == 518)
                {
                    RecoveryPiece piece = game.Pieces.First(p => p.SourceRecordIndex == 8);
                    ControlPiezaC control = piece.GetComponentInChildren<ControlPiezaC>(true);
                    Require("actual518-original-type6-wet", control != null && control.tipoPieza == 6 && water.IsWet(piece), "original CubFuenteFA6Col4 remains a separate Big provider");
                    GameObject prototype = control.prefabAguaControl.GetComponentInChildren<FuenteTest3>(true).prefabBigRo;
                    Transform big = piece.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Original " + prototype.name && Vector3.Distance(t.position, control.transform.position + Vector3.up * 25) < .001f);
                    Require("actual518-original-type6-big-pose", big != null, "original prefabBigRo, original localY25 and preserved native Big scale");
                    until = Time.time + 3;
                    while (!big.GetComponentsInChildren<ParticleSystem>(true).Any(ps => ps.particleCount > 0) && Time.time < until) yield return null;
                    Require("actual518-original-type6-big-particles", big.GetComponentsInChildren<ParticleSystem>(true).Any(ps => ps.particleCount > 0), "actual original Big particles emit");
                }
                if (level == 1058)
                {
                    RecoveryPiece target = game.Pieces.First(p => p.SourceRecordIndex == 30);
                    ControlPiezaC control = target.GetComponentInChildren<ControlPiezaC>(true);
                    Require("actual1058-type2-B9-wet-case", control != null && control.tipoPieza == 2 && control.configFuenteB == 9 && water.IsWet(target), "actual original Jsr1010104300ersk9eksd2ed");
                    Require("actual1058-B9-no-incorrect-fountain-family", target.GetComponentsInChildren<RecoveredNativeFountainAnimation>(true).Length == 0 && target.GetComponentsInChildren<RecoveredNativeDecorativePits>(true).Length == 0,
                        "native10A4E60..70 skips doInstanciarFuente entirely; actual routing jet retained separately");
                    Require("actual1058-B9-original-routing-jet-retained", target.GetComponentsInChildren<CascadaTestE>(true).Length > 0, "original E wrapper remains on the wet B9 piece");
                }
                if (level == 2001)
                {
                    RecoveredNativeDecorativePits lamp = bindings.FirstOrDefault(b => b.NativeConfiguration == "wetlight|71" && b.GetComponentInParent<RecoveryPiece>().SourceRecordIndex == 82);
                    Require("actual2001-record82-native-wet-light", lamp != null && lamp.NativeInstantiateVA == "0x10a6bec", "separate original FT3.doLights, distinct from static wall or decorative-pit lamps");
                    Light light = lamp.GetComponent<Light>();
                    Require("actual2001-original-light7-height-intensity", light != null && Mathf.Approximately(lamp.transform.localPosition.y, 6.5f) && Mathf.Approximately(light.intensity, 6),
                        "original first-digit7 branch10A6CB8..CE8 sets localY6.5 and intensity6");
                    RecoveredNativeDecorativePits red = bindings.First(b => b.GetComponentInParent<RecoveryPiece>().SourceRecordIndex == 81 && b.NativePrefabField == "prefabChorroRoE");
                    changeColorP original = red.GetComponent<changeColorP>();
                    Color sampled = original.ps.colorOverLifetime.color.gradient.Evaluate(0);
                    Require("actual2001-native-letter-gradient-colx1", original.subcolor == 4 && original.colx == 1 &&
                        Mathf.Abs(sampled.r - .13333334028720856f) < .00001f && Mathf.Abs(sampled.g - 1) < .00001f && Mathf.Abs(sampled.b - .0470588244497776f) < .00001f,
                        "original doLuzColor colx1/D4 green gradient; numeric4/colx0 is a separate original gold palette");
                    RecoveryPiece[] holes = game.Pieces.Where(p => p.BasePrefab != null && p.BasePrefab.Contains("FuenteEB6") && water.IsWet(p)).ToArray();
                    Require("actual2001-original-hole-piece-light-skip", holes.All(p => !p.GetComponentsInChildren<RecoveredNativeDecorativePits>(true).Any(b => b.NativeConfiguration.StartsWith("wetlight|"))),
                        "native doProcessEnter excludes FuenteEB6 from doLights; other authored decorative objects retained");
                }
                Directory.CreateDirectory(Output);
                FlowWaterRecoveryUI.Instance.CaptureBoardFrame(() => FlowWaterValidation.SaveVisibleFrame(Path.Combine(Output, "gm" + level + "-native-dispatch.png")));
                yield return null;
                if (level == 456)
                {
                    foreach (RecoveryPiece piece in game.Pieces) water.SetSourceEnabled(piece, false);
                    water.Rebuild(); yield return null; yield return null;
                    Require("actual456-native-provider-disconnect-disposes", water.ActiveFountainCount == 0 && bindings.All(b => b == null),
                        "production source-disable/disconnect exit removes exact old wet generated instances, matching native immediate Destroy list");
                }
            }
        }
        static IEnumerable DismissTutorial()
        {
            HelpRotate help = FindHelp(); if (help == null || !help.gameObject.activeInHierarchy) yield break;
            SwipeHandler2 swipe = RecoveredNativeCamera.CurrentSwipe;
            if (swipe == null) throw new InvalidOperationException("Visible native tutorial has no actual swipe controller.");
            float yaw = swipe.EsferaCamera.eulerAngles.y; swipe.GirarDerecha();
            while (Mathf.Abs(Mathf.DeltaAngle(swipe.EsferaCamera.eulerAngles.y, yaw + 90)) > .25f) yield return null;
            swipe.GirarIzquierda(); while (Mathf.Abs(Mathf.DeltaAngle(swipe.EsferaCamera.eulerAngles.y, yaw)) > .25f) yield return null;
            float until = Time.time + 1; while (Time.time < until) yield return null;
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
            Directory.CreateDirectory(Output); File.WriteAllText(Path.Combine(Output, "fountain-dispatch-validation.json"), JsonUtility.ToJson(report, true));
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
