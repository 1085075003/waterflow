#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Recovered.FlowWater.Editor
{
    [InitializeOnLoad]
    public static class FlowWaterCameraTutorialValidation
    {
        const string Key = "FlowWater.CameraTutorialValidation.";
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
        static string Output { get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation", "CameraTutorial"); } }
        static FlowWaterCameraTutorialValidation()
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
        [MenuItem("Tools/Flow Water/Validate Original Camera and Rotation Tutorial")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start camera validation in Edit mode.");
            SceneSetup[] scenes = EditorSceneManager.GetSceneManagerSetup();
            foreach (SceneSetup scene in scenes)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scene.path).isDirty) throw new InvalidOperationException("Save edited scenes first.");
            var prefs = new Prefs();
            foreach (string key in new[] { "helpRotation", "Recovery.LastLevelId", "Recovery.Complete.401", "Recovery.Completed.gm401", "esTablet", "OptionSelLev", "primeraVez", "MusicaON", "Soundlevel", "language" })
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
            PlayerPrefs.SetInt("helpRotation", 1);
            FlowWaterRecoveryUI.Instance.StartGame(401);
            while (RecoveredFlowGame.Instance == null || RecoveredFlowGame.Instance.CurrentLevelId != 401 || RecoveredFlowGame.Instance.GameCamera == null) yield return null;
            yield return null;
            RecoveredFlowGame game = RecoveredFlowGame.Instance;
            SwipeHandler2 swipe = RecoveredNativeCamera.ConfigureLevel(401);
            HelpRotate help = FindHelp();
            Require("real-original-camera-binding", swipe != null && swipe.EsferaCamera != null, swipe == null ? "missing SwipeHandler2" : swipe.EsferaCamera.name);
            Require("native-forced-level-tutorial", help != null && help.gameObject.activeInHierarchy && help.checkSwipe && !SwipeHandler2.girado && PlayerPrefs.GetInt("helpRotation") == 0, "level401 resets original helpRotation");
            Vector3 spherePosition = swipe.EsferaCamera.position; Quaternion sphereRotation = swipe.EsferaCamera.rotation;
            Vector3 sphereLocalPosition = swipe.EsferaCamera.localPosition; Quaternion sphereLocalRotation = swipe.EsferaCamera.localRotation;
            Vector3 cameraPosition = game.GameCamera.transform.position; Quaternion cameraRotation = game.GameCamera.transform.rotation;
            PlayerPrefs.SetString("language", "zh"); help.doLanguage();
            Require("native-chinese-tutorial-text", help.texto1a != null && help.texto1a.text == "旋转棋盘" && help.texto1b != null && help.texto1b.text == "在这里滑动", "exact native return literals");
            PlayerPrefs.SetString("language", "en"); help.doLanguage();
            Require("native-english-tutorial-text", help.texto1a.text == "rotate board" && help.texto1b.text == "swipe here", "exact native English fallback");
            float yaw = swipe.EsferaCamera.eulerAngles.y;
            swipe.GirarDerecha();
            float until = Time.time + 10;
            while (Mathf.Abs(Mathf.DeltaAngle(swipe.EsferaCamera.eulerAngles.y, swipe.rotationFinal.y)) > .25f && Time.time < until) yield return null;
            Require("production-quarter-turn", SwipeHandler2.girado && Mathf.Abs(Mathf.DeltaAngle(yaw + 90, swipe.EsferaCamera.eulerAngles.y)) < .3f, "GirarDerecha with original smoothSpeed=" + swipe.smoothSpeed);
            Require("actual-camera-follows-authored-sphere", Vector3.Distance(game.GameCamera.transform.position, cameraPosition) > .1f || Quaternion.Angle(game.GameCamera.transform.rotation, cameraRotation) > 1,
                "checks actual gameplay Camera, not only the serialized parent state");
            yield return null;
            Require("actual-rotation-dismisses-tutorial", !help.checkSwipe && PlayerPrefs.GetInt("helpRotation") == 1 && (help.Rotation == null || !help.Rotation.activeSelf) && help.fadeOut != null && help.fadeOut.enabled, "native Update enables original fadeOut and persists helpRotation=1");
            swipe.GirarIzquierda(); until = Time.time + 10;
            while (Mathf.Abs(Mathf.DeltaAngle(swipe.EsferaCamera.eulerAngles.y, yaw)) > .25f && Time.time < until) yield return null;
            Require("production-return-quarter-turn", Mathf.Abs(Mathf.DeltaAngle(swipe.EsferaCamera.eulerAngles.y, yaw)) < .3f, "GirarIzquierda returns original camera");
            swipe.EsferaCamera.eulerAngles = new Vector3(0, yaw + 31, 0);
            swipe.SynchronizeExternalRotation(true); float external = swipe.EsferaCamera.eulerAngles.y;
            for (int i = 0; i < 5; i++) yield return null;
            Require("desktop-orbit-target-sync", Mathf.Abs(Mathf.DeltaAngle(swipe.rotationFinal.y, external)) < .001f && Mathf.Abs(Mathf.DeltaAngle(swipe.EsferaCamera.eulerAngles.y, external)) < .001f, "external orbit remains stable without rebound");
            bool oldMoving = MoveBloque.MoviendoBloque; int oldMode = MoveBloque.modoSolucion;
            MoveBloque.MoviendoBloque = true; MoveBloque.modoSolucion = 0; float before = swipe.rotationFinal.y;
            swipe.GirarDerecha();
            Require("selected-piece-blocks-rotation", swipe.rotationFinal.y == before && !swipe.BeginRotation(new Vector2(Screen.width * .5f, Screen.height * .5f)), "native MoviendoBloque&&modoSolucion0 gate");
            MoveBloque.MoviendoBloque = oldMoving; MoveBloque.modoSolucion = oldMode;
            swipe.EsferaCamera.SetPositionAndRotation(spherePosition, sphereRotation);
            game.GameCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation); swipe.SynchronizeExternalRotation();
            Physics.SyncTransforms();
            bool began = false; Vector2 entrance = Vector2.zero;
            for (int y = 2; y <= 7 && !began; y++) for (int x = 1; x <= 11 && !began; x++)
            {
                Vector2 candidate = new Vector2(Screen.width * x / 12f, Screen.height * y / 9f);
                if (swipe.BeginRotation(candidate, true, game.GameCamera)) { began = true; entrance = candidate; }
            }
            Require("production-begin-uses-original-entrada", began && swipe.getAreaRotation != null && swipe.getAreaRotation.layer == LayerMask.NameToLayer("entrada"), began ? swipe.getAreaRotation.name : "no allowed original entrada hit");
            yield return null;
            float continuousBefore = swipe.EsferaCamera.eulerAngles.y;
            swipe.ContinueRotation(entrance + new Vector2(48, 0));
            Require("production-mobile-drag-moves-camera", Mathf.Abs(Mathf.DeltaAngle(continuousBefore, swipe.EsferaCamera.eulerAngles.y)) > 0, "actual Begin/Continue with original magnitude*dt*.2*-.4*multiplicador");
            swipe.EndRotation(entrance + new Vector2(48, 0), true);
            Require("production-cancel-releases-gesture", !swipe.IsPointerRotating, "no synthetic swipe flag injection");
            swipe.EsferaCamera.SetPositionAndRotation(spherePosition, sphereRotation);
            game.GameCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation); swipe.SynchronizeExternalRotation();
            Require("camera-pose-restored", Quaternion.Angle(swipe.EsferaCamera.rotation, sphereRotation) < .001f && Vector3.Distance(game.GameCamera.transform.position, cameraPosition) < .001f, "both authored sphere and actual gameplay camera restored");
            swipe.GirarDerecha(); until = Time.time + 10;
            while (Mathf.Abs(Mathf.DeltaAngle(swipe.EsferaCamera.eulerAngles.y, yaw + 90)) > .25f && Time.time < until) yield return null;
            game.LoadLevel(402);
            while (game.CurrentLevelId != 402 || game.PieceRoot == null) yield return null;
            yield return null;
            SwipeHandler2 nextSwipe = RecoveredNativeCamera.CurrentSwipe;
            Require("same-scene-level-load-resets-authored-camera", nextSwipe == swipe &&
                Quaternion.Angle(nextSwipe.EsferaCamera.localRotation, sphereLocalRotation) < .001f &&
                Vector3.Distance(nextSwipe.EsferaCamera.localPosition, sphereLocalPosition) < .001f,
                "actual LoadLevel402 reuses scene and source Swipe but resets authored parent pose");
            Require("next-forced-level-restarts-native-tutorial", !SwipeHandler2.girado && PlayerPrefs.GetInt("helpRotation") == 0 && help.checkSwipe && help.gameObject.activeInHierarchy,
                "native level402 reset is independent of external-orbit target synchronization");
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
            Directory.CreateDirectory(Output); File.WriteAllText(Path.Combine(Output, "camera-tutorial-validation.json"), JsonUtility.ToJson(report, true));
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
