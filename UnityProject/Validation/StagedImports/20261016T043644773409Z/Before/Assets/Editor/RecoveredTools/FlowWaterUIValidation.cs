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
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Recovered.FlowWater.Editor
{
    /// <summary>Exercises the actual rebound original Button and EventTrigger controls.</summary>
    [InitializeOnLoad]
    public static class FlowWaterUIValidation
    {
        const string ActiveKey = "FlowWater.UIValidation.Active";
        const string PrefsKey = "FlowWater.UIValidation.Preferences";
        [Serializable] public sealed class Check { public string name, detail; public bool passed; }
        [Serializable] public sealed class Preference { public string key; public bool existed, floating; public int integer; public float number; }
        [Serializable] public sealed class Preferences { public List<Preference> entries = new List<Preference>(); }
        [Serializable] public sealed class NativeScenes { public NativePart[] parts; }
        [Serializable] public sealed class NativePart { public int parte, firstAsset, lastAsset; public string scene; }
        [Serializable] public sealed class Report
        {
            public string startedUtc, finishedUtc, unityVersion;
            public bool passed, preferencesRestored;
            public int originalButtonInvocations, originalEventTriggerInvocations, nativePartsChecked, recoveredPacksEntered;
            public List<string> scenesVisited = new List<string>();
            public List<Check> checks = new List<Check>();
            public List<string> runtimeErrors = new List<string>();
            public List<string> screenshots = new List<string>();
            public List<string> notes = new List<string>();
        }
        sealed class WaitFrames { public int count; public WaitFrames(int count) { this.count = count; } }
        static Report report;
        static IEnumerator routine;
        static int resumeFrame;
        static double deadline;
        static bool busy;
        static bool importsLocked;
        static string Output { get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation", "UIFlows"); } }
        static FlowWaterRecoveryUI UI { get { return FlowWaterRecoveryUI.Instance; } }
        static RecoveredFlowGame Game { get { return RecoveredFlowGame.Instance; } }

        static FlowWaterUIValidation()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            Application.logMessageReceived += OnLog;
        }

        [MenuItem("Tools/Flow Water/Validate Original UI Flows")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start the UI validation from Edit mode.");
            Directory.CreateDirectory(Output);
            BackupPreferences();
            SessionState.SetBool(ActiveKey, true);
            EditorSceneManager.OpenScene("Assets/Scenes/Portada.unity", OpenSceneMode.Single);
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
                    AssetDatabase.DisallowAutoRefresh();
                    EditorApplication.LockReloadAssemblies();
                    importsLocked = true;
                    report = new Report { startedUtc = DateTime.UtcNow.ToString("o"), unityVersion = Application.unityVersion };
                    report.notes.Add("Checks invoke the rebound original Button.onClick or EventTrigger callback. Direct runtime calls are used only to arrange saved-solution/water victory states.");
                    report.notes.Add("All 39 native part mappings are inspected. Only the 27 packs whose level assets exist can be entered; missing source packs are not fabricated.");
                    report.notes.Add("The original game menu leaves the active board for level selection. This is checked as the original pause/menu behavior; no replacement pause screen is added.");
                    routine = ExerciseFlows();
                    resumeFrame = Time.frameCount + 8;
                    deadline = EditorApplication.timeSinceStartup + 600;
                }
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("UI validation exceeded 600 seconds.");
                if (Time.frameCount < resumeFrame) return;
                if (!routine.MoveNext()) { Finish(); return; }
                WaitFrames wait = routine.Current as WaitFrames;
                resumeFrame = Time.frameCount + (wait == null ? 1 : wait.count);
            }
            catch (Exception exception)
            {
                if (report != null) report.runtimeErrors.Add(exception.ToString());
                Finish();
            }
            finally { busy = false; }
        }

        static IEnumerator ExerciseFlows()
        {
            CheckState("unity-version", Application.unityVersion == "2022.3.28f1", Application.unityVersion);
            CheckScene("home", "Portada");
            CheckState("home-no-modal", UI != null && UI.ActiveModalCount == 0, ModalDescription());
            Capture("01-home.png");
            VerifyNativeParts();
            InvokeOriginal("botonExitDown");
            yield return new WaitFrames(3);
            var home = SceneComponents<UIManagerPortScript>().FirstOrDefault();
            CheckState("original-exit-confirmation", home != null && home.panelExit != null && home.panelExit.activeInHierarchy && UI.ActiveModalCount == 1, ModalDescription());
            Capture("01b-original-exit.png");
            InvokeOriginal("botonCancelDown");
            yield return new WaitFrames(3);
            CheckState("original-exit-cancel", UI.ActiveModalCount == 0, ModalDescription());

            InvokeOriginal("botonOptionsDown");
            yield return new WaitFrames(8);
            CheckScene("settings", "Settings");
            var options = SceneComponents<UIManagerOptScript>().FirstOrDefault();
            CheckState("settings-sliders", options != null && options.volumen1 != null && options.volumen2 != null, "Original music and sound sliders");
            if (options != null && options.volumen1 != null && options.volumen2 != null)
            {
                options.volumen1.value = .17f;
                options.volumen2.value = .24f;
            }
            yield return new WaitFrames(3);
            CheckState("settings-native-preferences", Near(PlayerPrefs.GetFloat("Conf_music", -1), .17f) && Near(PlayerPrefs.GetFloat("Conf_sound", -1), .24f), "Conf_music=.17, Conf_sound=.24");
            VerifyAudioVolumes();
            Capture("02-settings.png");
            string progressBeforeReset = ProgressState();
            InvokeOriginal("botonResetDown");
            yield return new WaitFrames(3);
            CheckState("original-reset-confirmation", options != null && options.cajaReset != null && options.cajaReset.activeInHierarchy && UI.ActiveModalCount == 1, ModalDescription());
            Capture("02b-original-reset.png");
            InvokeOriginal("botonResetCancel");
            yield return new WaitFrames(3);
            CheckState("original-reset-cancel-preserves-progress", UI.ActiveModalCount == 0 && ProgressState() == progressBeforeReset, "Both recovered completion key families unchanged");
            InvokeOriginal("botonStoreDown");
            yield return new WaitFrames(8);
            CheckScene("store", "Store");
            CheckState("store-no-modal", UI.ActiveModalCount == 0, ModalDescription());
            Capture("03-store.png");
            InvokeOriginal("botonBackDown");
            yield return new WaitFrames(8);
            CheckScene("store-back-to-settings", "Settings");
            InvokeOriginal("botonBackDown");
            yield return new WaitFrames(8);
            CheckScene("settings-back-to-home", "Portada");
            InvokeOriginal("botonPlayDown");
            yield return new WaitFrames(8);
            CheckScene("pack-selection", "SelLevel0");

            for (int tab = 1; tab <= 6; tab++)
            {
                InvokeOriginal("botonTabDown", tab);
                yield return new WaitFrames(3);
                CheckState("category-" + tab, UI.SelectedTab == tab && ActiveCategory(tab), "Original category groups and preview");
            }
            Capture("04-night-category.png");

            int[] parts = Enumerable.Range(1, 39).Where(x => UI.HasRecoveredPack(x)).ToArray();
            CheckState("all-source-packs-reachable", parts.Length == 27, parts.Length + " recovered 50-level packs");
            foreach (int part in parts)
            {
                int tab = PartTab(part);
                InvokeOriginal("botonTabDown", tab);
                yield return new WaitFrames(2);
                InvokeOriginal("botonPartDown", part);
                yield return new WaitFrames(6);
                CheckScene("part-" + part + "-grid", "SelLevel1");
                CheckState("part-" + part + "-selected", UI.SelectedPart == part, UI.SelectedPart.ToString());
                int[] cells = UI.OriginalControls.Where(x => x.method == "Level").Select(x => x.argument).Distinct().OrderBy(x => x).ToArray();
                CheckState("part-" + part + "-50-original-cells", cells.SequenceEqual(Enumerable.Range(1, 50)), cells.Length + " serialized original level controls");
                InvokeOriginal("Level", 1);
                yield return new WaitFrames(8);
                int expected = OriginalScenePresentation.AssetForLevel(part, 1);
                CheckScene("part-" + part + "-game-scene", OriginalScenePresentation.SceneForPart(part));
                CheckState("part-" + part + "-original-level-entry", UI.IsInGame && Game != null && Game.CurrentLevelId == expected && Game.IsLevelActive, "gm" + expected);
                CheckState("part-" + part + "-no-residual-dialog", UI.ActiveModalCount == 0, ModalDescription());
                CheckState("part-" + part + "-no-replacement-hud", GameObject.Find("RecoveryGameBar") == null && GameObject.Find("RecoveryGameFooter") == null, "Original gameplay UI");
                report.recoveredPacksEntered++;
                if (part == 9)
                {
                    Capture("05-basic-original-game.png");
                    bool soundWasEnabled = UI.SoundEnabled, musicWasEnabled = UI.MusicEnabled;
                    float soundBefore = UI.SoundVolume, musicBefore = UI.MusicVolume;
                    InvokeOriginal("botonSoundDown"); yield return new WaitFrames(3);
                    CheckState("original-sound-switch-independent-of-volume", UI.SoundEnabled != soundWasEnabled && Near(UI.SoundVolume, soundBefore) && SoundSlashMatches(), "Soundlevel toggled; Conf_sound preserved");
                    InvokeOriginal("botonSoundDown"); yield return new WaitFrames(3);
                    CheckState("original-sound-switch-restored", UI.SoundEnabled == soundWasEnabled && SoundSlashMatches(), "Original sound switch and slash agree");
                    InvokeOriginal("botonMusicDown"); yield return new WaitFrames(3);
                    CheckState("original-music-switch-independent-of-volume", UI.MusicEnabled != musicWasEnabled && Near(UI.MusicVolume, musicBefore) && MusicSlashMatches(), "MusicaON toggled; Conf_music preserved");
                    InvokeOriginal("botonMusicDown"); yield return new WaitFrames(3);
                    CheckState("original-music-switch-restored", UI.MusicEnabled == musicWasEnabled && MusicSlashMatches(), "Original music switch and slash agree");
                }
                VerifyAudioVolumes();
                InvokeOriginal("botonMenuDown");
                yield return new WaitFrames(6);
                CheckScene("part-" + part + "-game-menu", "SelLevel1");
                CheckState("part-" + part + "-board-stopped-in-menu", !UI.IsInGame && Game != null && !Game.IsLevelActive, "Original menu suspends/leaves board");
                InvokeOriginal("botonMenuDown");
                yield return new WaitFrames(6);
                CheckScene("part-" + part + "-grid-back", "SelLevel0");
            }

            // gm288's original so contains a different inventory. The preview
            // must build that inventory and return to the exact original gm.
            InvokeOriginal("botonTabDown", 1);
            yield return new WaitFrames(2);
            InvokeOriginal("botonPartDown", 6);
            yield return new WaitFrames(6);
            for (int page = 0; page < 3; page++) { InvokeOriginal("botonFlechaDerecha"); yield return new WaitFrames(2); }
            InvokeOriginal("Level", 38);
            yield return new WaitFrames(8);
            CheckState("paged-level-38-is-gm288", Game != null && Game.CurrentLevelId == 288, Game == null ? "No game" : Game.CurrentLevelId.ToString());
            int gm288Count = Game.PieceCount;
            Vector3[] gm288Positions = Positions();
            InvokeOriginal("showSolution");
            yield return new WaitFrames(3);
            CheckState("solution-dialog-alone", UI.ActiveModalCount == 1 && UI.OriginalSolutionPanel.activeInHierarchy, ModalDescription());
            InvokeOriginal("cancelSolution");
            yield return new WaitFrames(3);
            CheckState("solution-dialog-cancel", UI.ActiveModalCount == 0 && !Game.IsSolutionPreview, ModalDescription());
            InvokeOriginal("showSolution");
            yield return new WaitFrames(2);
            InvokeOriginal("okSolution");
            yield return new WaitFrames(8);
            CheckState("gm288-different-inventory-preview", Game.IsSolutionPreview && Game.PieceCount == SolutionPieceCount(288), Game.PieceCount + " original so pieces");
            CheckState("preview-does-not-open-completion", UI.OriginalCompletePanel == null || !UI.OriginalCompletePanel.activeInHierarchy, ModalDescription());
            Capture("06-gm288-solution-preview.png");
            InvokeOriginal("doSolucionLevel");
            yield return new WaitFrames(8);
            CheckState("gm288-preview-restores-gm-inventory", !Game.IsSolutionPreview && Game.PieceCount == gm288Count && SamePositions(gm288Positions), "Original gm positions and piece count restored");
            InvokeOriginal("doRestartLevel");
            yield return new WaitFrames(5);
            CheckState("original-restart", Game.MoveCount == 0 && SamePositions(gm288Positions) && UI.ActiveModalCount == 0, "Original restart button restores board");
            InvokeOriginal("botonMenuDown"); yield return new WaitFrames(6);
            InvokeOriginal("botonMenuDown"); yield return new WaitFrames(6);

            InvokeOriginal("botonTabDown", 3); yield return new WaitFrames(2);
            InvokeOriginal("botonPartDown", 13); yield return new WaitFrames(6);
            InvokeOriginal("Level", 1); yield return new WaitFrames(8);
            Vector3[] stepPositions = Positions();
            InvokeOriginal("showSolution"); yield return new WaitFrames(2);
            InvokeOriginal("okSolution"); yield return new WaitFrames(3);
            CheckState("save1-preview-has-original-steps-entry", Game.IsSolutionPreview && Game.SolutionStepCount > 0 && HasVisibleControl("okSteps"), "Original PanelW3b steps control is reachable");
            InvokeOriginal("okSteps"); yield return new WaitFrames(3);
            int previousStep = Game.AppliedSolutionSteps;
            InvokeOriginal("okSteps"); yield return new WaitFrames(3);
            CheckState("original-step-button-advances", Game.IsSolutionPreview && Game.AppliedSolutionSteps > previousStep, Game.AppliedSolutionSteps + "/" + Game.SolutionStepCount);
            Capture("07-original-steps-preview.png");
            InvokeOriginal("doSolucionLevel"); yield return new WaitFrames(8);
            CheckState("steps-exit-restores-board", !Game.IsSolutionPreview && SamePositions(stepPositions) && UI.ActiveModalCount == 0, "Pre-preview board restored");
            InvokeOriginal("botonMenuDown"); yield return new WaitFrames(6);
            InvokeOriginal("botonMenuDown"); yield return new WaitFrames(6);

            InvokeOriginal("botonTabDown", 1); yield return new WaitFrames(2);
            InvokeOriginal("botonPartDown", 9); yield return new WaitFrames(6);
            InvokeOriginal("Level", 1); yield return new WaitFrames(8);
            Game.Solve();
            double waterDeadline = EditorApplication.timeSinceStartup + 5;
            while (!WaterReadyForVictory() && EditorApplication.timeSinceStartup < waterDeadline)
                yield return new WaitFrames(1);
            CheckState("completion-water-network-ready", WaterReadyForVictory(), WaterState());
            // Network rebuilds are throttled by .12 seconds. Extremely fast
            // batch-mode frames can otherwise exhaust sixty frames first.
            double settledAfter = EditorApplication.timeSinceStartup + .25;
            while (EditorApplication.timeSinceStartup < settledAfter) yield return new WaitFrames(1);
            yield return new WaitFrames(60);
            CheckState("water-victory-opens-original-completion", Game.IsComplete && UI.OriginalCompletePanel != null && UI.OriginalCompletePanel.activeInHierarchy, "60 rendered frames after ready water network plus .25-second settle; " + WaterState());
            var completedLoader = SceneComponents<SaveLoadManager>().FirstOrDefault();
            CheckState("completion-next-hidden-until-dismissed", completedLoader != null && !completedLoader.NextButton.activeInHierarchy, "Toolbar Next appears after closing completion; modal Next remains available");
            Capture("08-original-completion.png");
            InvokeOriginal("botonCompleteOff"); yield return new WaitFrames(3);
            CheckState("completion-close", UI.ActiveModalCount == 0, ModalDescription());
            CheckState("completion-next-replaces-solution-group", HasVisibleControl("botonNextDown") && completedLoader != null && !completedLoader.solutionButton.activeInHierarchy && !completedLoader.botonNumSol.activeInHierarchy, "Next and solution count do not overlap");
            InvokeOriginal("botonNextDown"); yield return new WaitFrames(8);
            CheckState("original-next-level", Game.CurrentLevelId == 402 && UI.ActiveModalCount == 0, "gm" + Game.CurrentLevelId);
            InvokeOriginal("botonMenuDown"); yield return new WaitFrames(6);
            InvokeOriginal("botonMenuDown"); yield return new WaitFrames(6);
            InvokeOriginal("botonMenuDown"); yield return new WaitFrames(8);
            CheckScene("final-home", "Portada");
            CheckState("final-no-residual-dialog", UI.ActiveModalCount == 0, ModalDescription());
            string[] expectedScenes = { "Portada", "Settings", "Store", "SelLevel0", "SelLevel1", "Levely5", "Levely5_basic", "Levely5_6x6", "Levely5_7x7" };
            CheckState("all-nine-original-scenes-visited", expectedScenes.All(x => report.scenesVisited.Contains(x)), String.Join(", ", report.scenesVisited.ToArray()));
        }

        static bool InvokeOriginal(string method, int? argument = null)
        {
            FlowWaterRecoveryUI.OriginalControl control = UI.OriginalControls.FirstOrDefault(x => x.method == method &&
                (!argument.HasValue || x.argument == argument.Value) && x.source != null && x.source.activeInHierarchy && (x.button == null || x.button.interactable));
            CheckState("invoke-" + method + (argument.HasValue ? "-" + argument.Value : ""), control != null, "Active original control");
            if (control == null) return false;
            if (control.button != null)
            { report.originalButtonInvocations++; control.button.onClick.Invoke(); }
            else
            {
                EventTrigger.Entry entry = control.trigger.triggers.FirstOrDefault(x => x.eventID == control.eventType);
                if (entry == null) throw new MissingMemberException("Original EventTrigger entry disappeared: " + method);
                report.originalEventTriggerInvocations++;
                entry.callback.Invoke(new PointerEventData(EventSystem.current));
            }
            return true;
        }

        static bool HasVisibleControl(string method)
        { return UI.OriginalControls.Any(x => x.method == method && x.source != null && x.source.activeInHierarchy); }
        static void CheckState(string name, bool passed, string detail)
        { report.checks.Add(new Check { name = name, passed = passed, detail = detail }); }
        static void CheckScene(string check, string expected)
        {
            string actual = SceneManager.GetActiveScene().name;
            if (!report.scenesVisited.Contains(actual)) report.scenesVisited.Add(actual);
            CheckState(check, actual == expected, actual + " / " + expected);
        }
        static bool Near(float a, float b) { return Mathf.Abs(a - b) < .0001f; }
        static string ModalDescription() { return UI == null ? "UI unavailable" : String.Join(", ", UI.VisibleModalNames); }
        static IEnumerable<T> SceneComponents<T>() where T : Component
        { return OriginalScenePresentation.Components<T>(SceneManager.GetActiveScene()); }
        static int PartTab(int part)
        { return part >= 36 ? 6 : part >= 24 ? 5 : part >= 20 ? 4 : part >= 13 && part <= 16 ? 3 : part >= 10 && part <= 12 || part == 17 ? 2 : 1; }
        static bool ActiveCategory(int tab)
        {
            var selector = SceneComponents<UIManagerSelScript0>().FirstOrDefault();
            if (selector == null) return false;
            GameObject[] groups = { selector.grupoTab1, selector.grupoTab2, selector.grupoTab3, selector.grupoTab4, selector.grupoTab5, selector.grupoTab6 };
            for (int i = 0; i < groups.Length; i++) if (groups[i] != null && groups[i].activeSelf != (i == tab - 1)) return false;
            GameObject[] previews = { selector.grupo1, selector.grupo2, selector.grupo3, selector.grupo4, selector.grupo5, selector.grupo6 };
            if (previews[tab-1] == null || !previews[tab-1].activeInHierarchy || previews.Where(x => x != null && x.activeInHierarchy).Count() != 1) return false;
            if (PlayerPrefs.GetInt("OptionSelLev",-1) != tab) return false;
            for (int part=1;part<=6;part++) for(int layer=1;layer<=2;layer++)
            {
                GameObject target=typeof(UIManagerSelScript0).GetField("part"+part+"_"+layer).GetValue(selector) as GameObject;
                Image image=target == null ? null : target.GetComponent<Image>();
                if(image != null && !Near(image.color.a,part==tab ? 1 : .7058823704719543f)) return false;
            }
            return true;
        }
        static bool SoundSlashMatches()
        {
            UIManagerScript ui = SceneComponents<UIManagerScript>().FirstOrDefault();
            return ui != null && ui.soundOff != null && ui.soundOff.activeSelf == !UI.SoundEnabled;
        }
        static bool MusicSlashMatches()
        {
            UIManagerScript ui = SceneComponents<UIManagerScript>().FirstOrDefault();
            return ui != null && ui.musicFuera != null && ui.musicFuera.activeSelf == !UI.MusicEnabled;
        }
        static Vector3[] Positions() { return Game.Pieces.Select(x => x.transform.position).ToArray(); }
        static bool SamePositions(Vector3[] positions)
        { return Game.PieceCount == positions.Length && Game.Pieces.Select((x, i) => (x.transform.position - positions[i]).sqrMagnitude < .001f).All(x => x); }
        static int SolutionPieceCount(int id)
        {
            RecoveryLevelEntry entry = RecoveryCatalog.Load().levels.First(x => x.assetId == id);
            return JsonUtility.FromJson<RecoveryEnvelope>(entry.solution.text).data.pieza.Count;
        }
        static bool WaterReadyForVictory()
        {
            RecoveredWaterNetwork water = RecoveredWaterNetwork.Instance;
            return Game != null && water != null && water.CurrentPieceRoot == Game.PieceRoot &&
                water.RequiredFountainCount > 0 && water.ActiveFountainCount >= water.RequiredFountainCount;
        }
        static string WaterState()
        {
            RecoveredWaterNetwork water = RecoveredWaterNetwork.Instance;
            return water == null ? "No water network" : "root=" + (Game != null && water.CurrentPieceRoot == Game.PieceRoot) +
                "; active=" + water.ActiveFountainCount + "; required=" + water.RequiredFountainCount + "; level=" + water.LevelId;
        }
        static void VerifyNativeParts()
        {
            string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation", "NativeAnalysis", "native-scene-map.json");
            NativeScenes native = JsonUtility.FromJson<NativeScenes>(File.ReadAllText(path));
            foreach (NativePart part in native.parts)
            {
                bool scene = OriginalScenePresentation.SceneForPart(part.parte) == part.scene;
                bool range = part.firstAsset > 0 ? OriginalScenePresentation.AssetForLevel(part.parte, 1) == part.firstAsset && OriginalScenePresentation.AssetForLevel(part.parte, 50) == part.lastAsset : OriginalScenePresentation.AssetForLevel(part.parte, 1) == -1;
                CheckState("native-part-" + part.parte, scene && range, part.scene + "; " + part.firstAsset + ".." + part.lastAsset);
                report.nativePartsChecked++;
            }
        }
        static void VerifyAudioVolumes()
        {
            float music = PlayerPrefs.GetFloat("Conf_music", .3f), sound = PlayerPrefs.GetFloat("Conf_sound", .3f);
            foreach (AudioManager0 manager in UnityEngine.Object.FindObjectsOfType<AudioManager0>())
                foreach (Sound item in manager.sounds)
                {
                    AudioSource source = typeof(Sound).GetField("source", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(item) as AudioSource;
                    float expected = item.name == "Chill" || item.name == "Musica" ? music : sound;
                    CheckState("menu-audio-volume-" + item.name, source != null && source.clip == item.clip && Near(source.volume, expected), source == null ? "No source" : source.volume.ToString());
                }
            foreach (AudioManager manager in UnityEngine.Object.FindObjectsOfType<AudioManager>())
                foreach (Sound2 item in manager.sounds)
                {
                    AudioSource source = typeof(Sound2).GetField("source2", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(item) as AudioSource;
                    float expected = item.name2 == "Chill" || item.name2 == "MusicaLevel" ? music : sound;
                    if (item.name2 == "SonidoFuente" || item.name2 == "SonidoCompleted") expected += .2f;
                    CheckState("game-audio-volume-" + item.name2, source != null && source.clip == item.clip2 && Near(source.volume, expected), source == null ? "No source" : source.volume.ToString());
                }
        }
        static void Capture(string file)
        {
            string path = Path.Combine(Output, file);
            FlowWaterValidation.SaveVisibleFrame(path);
            report.screenshots.Add(path);
        }
        static void BackupPreferences()
        {
            var backup = new Preferences();
            Action<string, bool> add = (key, floating) => backup.entries.Add(new Preference { key = key, floating = floating,
                existed = PlayerPrefs.HasKey(key), integer = PlayerPrefs.GetInt(key, 0), number = PlayerPrefs.GetFloat(key, 0) });
            add("Conf_music", true); add("Conf_sound", true); add("primeraVez", false);
            add("Soundlevel", false); add("MusicaON", false); add("Recovery.LastLevelId", false); add("esTablet", false); add("OptionSelLev",false);
            foreach (RecoveryLevelEntry entry in RecoveryCatalog.Load().levels)
            { add("Recovery.Complete." + entry.assetId, false); add("Recovery.Completed.gm" + entry.assetId, false); }
            SessionState.SetString(PrefsKey, JsonUtility.ToJson(backup));
        }
        static bool RestorePreferences()
        {
            string json = SessionState.GetString(PrefsKey, "");
            if (String.IsNullOrEmpty(json)) return false;
            Preferences backup = JsonUtility.FromJson<Preferences>(json);
            foreach (Preference item in backup.entries)
            {
                if (!item.existed) PlayerPrefs.DeleteKey(item.key);
                else if (item.floating) PlayerPrefs.SetFloat(item.key, item.number);
                else PlayerPrefs.SetInt(item.key, item.integer);
            }
            PlayerPrefs.Save();
            bool exact = backup.entries.All(x => PlayerPrefs.HasKey(x.key) == x.existed && (!x.existed || (x.floating ? Near(PlayerPrefs.GetFloat(x.key), x.number) : PlayerPrefs.GetInt(x.key) == x.integer)));
            SessionState.EraseString(PrefsKey);
            return exact;
        }
        static void OnLog(string message, string stack, LogType type)
        { if (report != null && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) report.runtimeErrors.Add(message + "\n" + stack); }
        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                ReleaseImportLock();
                return;
            }
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(ActiveKey, false)) return;
            RestorePreferences(); SessionState.EraseBool(ActiveKey);
        }
        static void ReleaseImportLock()
        {
            if (!importsLocked) return;
            importsLocked = false;
            EditorApplication.UnlockReloadAssemblies();
            AssetDatabase.AllowAutoRefresh();
        }
        static string ProgressState()
        {
            return String.Join(";", RecoveryCatalog.Load().levels.Select(x =>
                PlayerPrefs.GetInt("Recovery.Complete." + x.assetId, -1) + "," + PlayerPrefs.GetInt("Recovery.Completed.gm" + x.assetId, -1)).ToArray());
        }
        static void Finish()
        {
            bool passed = false;
            try
            {
                if (Game != null) Game.ReturnToOriginalScene();
                bool restored = RestorePreferences();
                if (report == null) report = new Report();
                report.preferencesRestored = restored;
                report.finishedUtc = DateTime.UtcNow.ToString("o");
                report.passed = restored && report.checks.Count > 0 && report.checks.All(x => x.passed) && report.runtimeErrors.Count == 0;
                Directory.CreateDirectory(Output);
                File.WriteAllText(Path.Combine(Output, "ui-flow-validation.json"), JsonUtility.ToJson(report, true));
                passed = report.passed;
                Debug.Log("Original UI flow validation: " + (passed ? "PASS" : "FAIL") + "; checks=" + report.checks.Count + "; restored preferences=" + restored);
            }
            finally
            {
                SessionState.EraseBool(ActiveKey);
                report = null; routine = null;
                ReleaseImportLock();
            }
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1); else EditorApplication.isPlaying = false;
        }
    }
}
#endif
