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
                CheckState(id + ":inactive-alternate-star-delay-not-run", driver != null && driver.StarTargetCount == 0 && driver.StarsEnabledCount == 0,
                    "Original StarCollection_B1 is inactive; native Start does not run there");
                var visibleStars = panel.GetComponentsInChildren<Animator>(true).Where(x => x.gameObject.name.StartsWith("sta3_", StringComparison.Ordinal)).ToArray();
                var visibleTimes = visibleStars.Select(x => x.GetCurrentAnimatorStateInfo(0).normalizedTime).ToArray();
                var visibleScales = visibleStars.Select(x => x.transform.localScale).ToArray();
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
                var alternate = panel.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "StarCollection_B1");
                CheckState(id + ":authored-alternate-b-remains-inactive", alternate != null && !alternate.gameObject.activeSelf && starB != null && !starB.gameObject.activeInHierarchy,
                    "No hidden original branch is activated as a fabricated victory effect");
                CheckState(id + ":three-visible-star-controllers-advance", visibleStars.Length == 3 && visibleStars.Select((x, i) =>
                    x.enabled && x.gameObject.activeInHierarchy && x.GetCurrentAnimatorStateInfo(0).normalizedTime > visibleTimes[i]).All(x => x),
                    "Actual stars/sta3_1..3 original star3.controller time advances");
                CheckState(id + ":visible-star-scale-curves-render", visibleStars.Select((x, i) => (x.transform.localScale - visibleScales[i]).sqrMagnitude > .000001f).Any(x => x),
                    "At least one actual visible star transform changes across captured scaled time");
                var unused = stars.Where(x => x.gameObject.name == "Star_D").ToArray();
                CheckState(id + ":unused-d-keeps-authored-disabled", unused.Length == 3 && unused.All(x => !x.enabled), "No invented targets or orphan curve remapping");
                Capture(id + "-complete-stars.png");
                if (driver != null) driver.PlayConfiguredStarEnd();
                CheckState(id + ":baseline-e-keeps-inactive-b-hidden", alternate != null && !alternate.gameObject.activeSelf,
                    "Original inactive Delay.Update does not route E to the alternate B effect");

                // Explicit test-only activation: validate the original assigned
                // B branch independently, without calling it the gameplay star.
                if (id == 1 && alternate != null)
                {
                    bool oldActive = alternate.gameObject.activeSelf;
                    alternate.gameObject.SetActive(true);
                    driver = RecoveredOriginalUIAnimation.ShowComplete(panel);
                    CheckState("isolated-b:configured-delay-start", driver.StarTargetCount == 1 && starB != null && !starB.gameObject.activeSelf,
                        "Controlled original branch activation only; target disabled before native .2s delay");
                    settle = Time.time + .3f;
                    while (Time.time < settle) yield return null;
                    CheckState("isolated-b:delay-enables-authored-target", driver.StarsEnabledCount == 1 && starB != null && starB.enabled && starB.gameObject.activeInHierarchy,
                        "Original .2scaled-second activation completed");
                    float bTime = starB == null ? 0 : starB.GetCurrentAnimatorStateInfo(0).normalizedTime;
                    settle = Time.time + .2f;
                    while (Time.time < settle) yield return null;
                    CheckState("isolated-b:controller-time-advances", starB != null && starB.GetCurrentAnimatorStateInfo(0).normalizedTime > bTime,
                        "Original B default controller advances; orphan curves remain untouched");
                    driver.PlayConfiguredStarEnd();
                    CheckState("isolated-b:original-e-star-end-state", starB != null && starB.GetCurrentAnimatorStateInfo(0).IsName("Star_End"),
                        "Same assigned target list as native E-key PlayStarEndAnimationForAll106834C");
                    Capture("isolated-original-b-effect.png");
                    alternate.gameObject.SetActive(oldActive);
                }
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
