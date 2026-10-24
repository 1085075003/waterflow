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
    public static class FlowWaterStarAnimationValidation
    {
        const string Key = "FlowWater.NativeStarValidation";
        [Serializable] public sealed class Check { public string name, detail; public bool passed; }
        [Serializable] public sealed class Report
        {
            public string startedUtc, finishedUtc, unityVersion;
            public bool passed, preferencesRestored;
            public List<Check> checks = new List<Check>();
            public List<string> errors = new List<string>();
        }
        static Report report;
        static IEnumerator routine;
        static bool busy, locked;
        static double timeout;
        static string Output { get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation", "UIAnimations"); } }
        static FlowWaterStarAnimationValidation()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode) Unlock();
                if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key, false))
                { Restore(); SessionState.EraseBool(Key); }
            };
            Application.logMessageReceived += (message, stack, type) =>
            {
                if (report != null && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
                    report.errors.Add(message + "\n" + stack);
            };
        }
        [MenuItem("Tools/Flow Water/Validate Native Completion And Stars")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start star validation from Edit mode.");
            Backup(); SessionState.SetBool(Key, true);
            EditorSceneManager.OpenScene("Assets/Scenes/Portada.unity", OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }
        static void Update()
        {
            if (busy || !SessionState.GetBool(Key, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            busy = true;
            try
            {
                if (report == null)
                {
                    AssetDatabase.DisallowAutoRefresh(); EditorApplication.LockReloadAssemblies(); locked = true;
                    report = new Report { startedUtc = DateTime.UtcNow.ToString("o"), unityVersion = Application.unityVersion };
                    timeout = EditorApplication.timeSinceStartup + 240; routine = Exercise();
                }
                if (EditorApplication.timeSinceStartup > timeout) throw new TimeoutException("Native UI animation fixture exceeded240seconds.");
                if (!routine.MoveNext()) Finish();
            }
            catch (Exception error) { if (report == null) report = new Report(); report.errors.Add(error.ToString()); Finish(); }
            finally { busy = false; }
        }
        static IEnumerator Exercise()
        {
            for (int frame = 0; frame < 8; frame++) yield return null;
            CheckState("unity", Application.unityVersion == "2022.3.28f1", Application.unityVersion);
            foreach (int id in new[] { 1, 401, 251, 351 })
            {
                FlowWaterRecoveryUI.Instance.StartGame(id);
                for (int frame = 0; frame < 10; frame++) yield return null;
                var game = RecoveredFlowGame.Instance;
                CheckState(id + ":source-scene", game != null && game.IsLevelActive && game.CurrentLevelId == id, "Original board loaded through UI");
                PlayerPrefs.SetInt("esTablet", id == 351 ? 1 : 0);
                game.Solve();
                float deadline = Time.realtimeSinceStartup + 45;
                while ((!game.IsComplete || FlowWaterRecoveryUI.Instance.OriginalCompletePanel == null || !FlowWaterRecoveryUI.Instance.OriginalCompletePanel.activeInHierarchy) && Time.realtimeSinceStartup < deadline)
                    yield return null;
                GameObject panel = FlowWaterRecoveryUI.Instance.OriginalCompletePanel;
                bool shown = game.IsComplete && panel != null && panel.activeInHierarchy;
                CheckState(id + ":natural-water-victory-opens-complete", shown, "Original water arrival and51-frame completion poll; no direct complete flag writes");
                if (!shown) continue;
                var root = panel.GetComponent<Animator>();
                string state = id == 351 ? "an_complete2d" : "an_complete";
                CheckState(id + ":native-root-state", root != null && root.enabled && root.GetCurrentAnimatorStateInfo(0).IsName(state), state);
                var driver = panel.GetComponent<RecoveredOriginalUIAnimation>();
                CheckState(id + ":serialized-star-delay-target", driver != null && driver.StarTargetCount == 1, "Original list contains one assigned Star_B(.2s), two null slots");
                Vector3 initialScale = panel.transform.localScale;
                float rootTime = root == null ? 0 : root.GetCurrentAnimatorStateInfo(0).normalizedTime;
                Capture(id + "-complete-opening.png");
                float settle = Time.time + .45f;
                while (Time.time < settle) yield return null;
                CheckState(id + ":root-animation-advances", root != null && root.GetCurrentAnimatorStateInfo(0).normalizedTime > rootTime,
                    "Root time=" + rootTime + "->" + (root == null ? -1 : root.GetCurrentAnimatorStateInfo(0).normalizedTime));
                CheckState(id + ":root-scale-curve-renders", (panel.transform.localScale - initialScale).sqrMagnitude > .000001f,
                    "Scale=" + initialScale + "->" + panel.transform.localScale);
                var stars = panel.GetComponentsInChildren<Animator>(true);
                var starB = stars.FirstOrDefault(x => x.gameObject.name == "Star_B");
                CheckState(id + ":original-star-b-enabled-after-delay", driver != null && driver.StarsEnabledCount == 1 && starB != null && starB.gameObject.activeInHierarchy && starB.enabled,
                    "Only the original configured target is activated");
                var unused = stars.Where(x => x.gameObject.name == "Star_D").ToArray();
                CheckState(id + ":unused-d-keeps-authored-disabled", unused.Length == 3 && unused.All(x => !x.enabled), "No invented targets or orphan curve remapping");
                float bTime = starB == null ? 0 : starB.GetCurrentAnimatorStateInfo(0).normalizedTime;
                settle = Time.time + .2f;
                while (Time.time < settle) yield return null;
                CheckState(id + ":star-controller-time-advances", starB != null && starB.GetCurrentAnimatorStateInfo(0).normalizedTime > bTime,
                    "Original Star_B_Controoller is playing its existing valid/default curves");
                Capture(id + "-complete-stars.png");
                if (driver != null) driver.PlayConfiguredStarEnd();
                CheckState(id + ":original-e-star-end-state", starB != null && starB.GetCurrentAnimatorStateInfo(0).IsName("Star_End"),
                    "Same assigned target list as native E-key PlayStarEndAnimationForAll106834C");
            }
        }
        static void CheckState(string name, bool passed, string detail)
        { report.checks.Add(new Check { name = name, passed = passed, detail = detail }); }
        static void Capture(string file)
        { Directory.CreateDirectory(Output); FlowWaterValidation.SaveVisibleFrame(Path.Combine(Output, file)); }
        static void Backup()
        {
            var prefs = new FlowWaterUIValidation.Preferences();
            Action<string, bool> add = (key, floating) => prefs.entries.Add(new FlowWaterUIValidation.Preference { key = key, floating = floating,
                existed = PlayerPrefs.HasKey(key), integer = PlayerPrefs.GetInt(key), number = PlayerPrefs.GetFloat(key) });
            foreach (string name in new[] { "Recovery.LastLevelId", "esTablet", "OptionSelLev", "Soundlevel", "MusicaON", "primeraVez", "helpRotation" }) add(name, false);
            add("Conf_music", true); add("Conf_sound", true);
            foreach (var level in RecoveryCatalog.Load().levels)
            { add("Recovery.Complete." + level.assetId, false); add("Recovery.Completed.gm" + level.assetId, false); }
            SessionState.SetString(Key + ".Prefs", JsonUtility.ToJson(prefs));
        }
        static bool Restore()
        {
            string json = SessionState.GetString(Key + ".Prefs", ""); if (String.IsNullOrEmpty(json)) return false;
            var prefs = JsonUtility.FromJson<FlowWaterUIValidation.Preferences>(json);
            foreach (var value in prefs.entries)
            {
                if (!value.existed) PlayerPrefs.DeleteKey(value.key);
                else if (value.floating) PlayerPrefs.SetFloat(value.key, value.number);
                else PlayerPrefs.SetInt(value.key, value.integer);
            }
            PlayerPrefs.Save(); SessionState.EraseString(Key + ".Prefs");
            return prefs.entries.All(x => PlayerPrefs.HasKey(x.key) == x.existed && (!x.existed || (x.floating ? Mathf.Approximately(PlayerPrefs.GetFloat(x.key), x.number) : PlayerPrefs.GetInt(x.key) == x.integer)));
        }
        static void Unlock()
        { if (locked) { locked = false; EditorApplication.UnlockReloadAssemblies(); AssetDatabase.AllowAutoRefresh(); } }
        static void Finish()
        {
            if (report == null) report = new Report(); report.preferencesRestored = Restore(); report.finishedUtc = DateTime.UtcNow.ToString("o");
            report.passed = report.preferencesRestored && report.errors.Count == 0 && report.checks.Count > 0 && report.checks.All(x => x.passed);
            Directory.CreateDirectory(Output); File.WriteAllText(Path.Combine(Output, "native-ui-animation-validation.json"), JsonUtility.ToJson(report, true));
            bool passed = report.passed; SessionState.EraseBool(Key); Unlock(); report = null; routine = null;
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1); else EditorApplication.isPlaying = false;
        }
    }
}
#endif
