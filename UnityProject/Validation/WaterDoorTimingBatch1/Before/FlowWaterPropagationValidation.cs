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

namespace Recovered.FlowWater.Editor
{
    // Exercises real authored controllers and the production snapshot boundary.
    // The deterministic scheduler checks in Validation/WaterPropagationChecks.cs
    // cover individual callback order; this checks actual Unity render objects.
    [InitializeOnLoad]
    public static class FlowWaterPropagationValidation
    {
        const string ActiveKey = "FlowWater.PropagationValidation.Active", BackupKey = "FlowWater.PropagationValidation.Backup";
        const string ScenesKey = "FlowWater.PropagationValidation.Scenes", ScaleKey = "FlowWater.PropagationValidation.TimeScale";
        const string ExitKey = "FlowWater.PropagationValidation.Exit";
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [Serializable] sealed class Preference { public string key; public bool existed; public int value; }
        [Serializable] sealed class Backup { public List<Preference> values = new List<Preference>(); }
        [Serializable] sealed class SavedScene { public string path; public bool loaded, active; }
        [Serializable] sealed class SavedScenes { public SavedScene[] scenes; }
        [Serializable] public sealed class Check { public string name, detail; public bool passed; }
        [Serializable] public sealed class Sample { public string stage; public float time; public int wet, active, required, pending, waterPrefabs; }
        [Serializable] public sealed class Report
        {
            public string unityVersion, startedUtc, finishedUtc;
            public bool passed, preferencesRestored;
            public List<Check> checks = new List<Check>();
            public List<Sample> samples = new List<Sample>();
            public List<string> errors = new List<string>();
        }
        static Report report;
        static IEnumerator routine;
        static bool busy, locked;
        static float originalScale;
        static double deadline;
        static int nextFrame;
        static RecoveredFlowGame Game { get { return RecoveredFlowGame.Instance; } }
        static RecoveredWaterNetwork Water { get { return RecoveredWaterNetwork.Instance; } }
        static string Output { get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation", "WaterPropagation"); } }
        static FlowWaterPropagationValidation()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode) Unlock();
                if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(ActiveKey, false))
                {
                    RestorePreferences(); RestoreScenes(); SessionState.EraseBool(ActiveKey);
                    int exit = SessionState.GetInt(ExitKey, -1); SessionState.EraseInt(ExitKey);
                    if (Application.isBatchMode && exit >= 0) EditorApplication.Exit(exit);
                }
            };
            Application.logMessageReceived += (message, stack, type) =>
            { if (report != null && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) report.errors.Add(message + "\n" + stack); };
        }
        [MenuItem("Tools/Flow Water/Validate Real Water Propagation")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start water propagation validation from Edit mode.");
            foreach (SceneSetup setup in EditorSceneManager.GetSceneManagerSetup())
                if (UnityEngine.SceneManagement.SceneManager.GetSceneByPath(setup.path).isDirty)
                    throw new InvalidOperationException("Save edited scenes before running water propagation validation.");
            var backup = new Backup();
            foreach (string key in new[] { "Recovery.LastLevelId", "Recovery.Completed.gm401", "Recovery.Complete.401", "Recovery.Completed.gm1052", "Recovery.Complete.1052", "Recovery.Completed.gm605", "Recovery.Complete.605", "esTablet", "OptionSelLev", "primeraVez", "MusicaON", "Soundlevel" })
                backup.values.Add(new Preference { key = key, existed = PlayerPrefs.HasKey(key), value = PlayerPrefs.GetInt(key) });
            SessionState.SetString(BackupKey, JsonUtility.ToJson(backup)); SessionState.SetBool(ActiveKey, true);
            SessionState.SetFloat(ScaleKey, Time.timeScale);
            SessionState.SetString(ScenesKey, JsonUtility.ToJson(new SavedScenes { scenes = EditorSceneManager.GetSceneManagerSetup()
                .Select(scene => new SavedScene { path = scene.path, loaded = scene.isLoaded, active = scene.isActive }).ToArray() }));
            RecoveryCatalogBuilder.Build(); EditorSceneManager.OpenScene("Assets/Scenes/Portada.unity", OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }
        static void Update()
        {
            if (busy || !SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling || Time.frameCount < nextFrame) return;
            busy = true;
            try
            {
                if (report == null)
                {
                    AssetDatabase.DisallowAutoRefresh(); EditorApplication.LockReloadAssemblies(); locked = true;
                    originalScale = SessionState.GetFloat(ScaleKey, 1); Time.timeScale = 4;
                    report = new Report { unityVersion = Application.unityVersion, startedUtc = DateTime.UtcNow.ToString("o") };
                    deadline = EditorApplication.timeSinceStartup + 180; routine = Exercise();
                }
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Water propagation validation exceeded 180 seconds.");
                if (!routine.MoveNext()) { Finish(); return; }
                nextFrame = Time.frameCount + 1;
            }
            catch (Exception error) { report.errors.Add(error.ToString()); Finish(); }
            finally { busy = false; }
        }
        static IEnumerator Exercise()
        {
            CheckState("unity-version", Application.unityVersion == "2022.3.28f1", Application.unityVersion);
            while (FlowWaterRecoveryUI.Instance == null) yield return null;
            FlowWaterRecoveryUI.Instance.StartGame(401);
            while (Game == null || Water == null || Game.CurrentLevelId != 401 || Game.PieceRoot == null) yield return null;
            Game.Solve(); Water.Rebuild(); SampleState("solution-start");
            CheckState("front-does-not-fill-immediately", Water.RequiredFountainCount > 0 && Water.ActiveFountainCount == 0 && Water.HasPendingPropagation,
                Counts());
            float until = Time.time + 30;
            while (Water.WetPieceCount == 0 && Time.time < until) yield return null;
            CheckState("authored-source-enters", Water.WetPieceCount > 0 && Water.HasPendingPropagation && Water.ActiveFountainCount == 0, Counts());
            until = Time.time + .25f;
            while (Time.time < until) yield return null;
            SampleState("before-source-callback");
            CheckState("source-native-one-second-callback", Water.ActiveFountainCount == 0, Counts());
            var originalVisuals = Visuals(); int previousWet = Water.WetPieceCount;
            Water.Rebuild(); var rebuiltVisuals = Visuals();
            CheckState("unrelated-rebuild-preserves-arrived-water", originalVisuals.Count > 0 &&
                originalVisuals.All(pair => rebuiltVisuals.ContainsKey(pair.Key) && rebuiltVisuals[pair.Key] == pair.Value) && Water.WetPieceCount == previousWet,
                "Retained water roots=" + originalVisuals.Count + "; " + Counts());
            MethodInfo capture = typeof(RecoveredFlowGame).GetMethod("CaptureSnapshot", Private);
            MethodInfo restore = typeof(RecoveredFlowGame).GetMethod("RestoreSnapshot", Private);
            object snapshot = capture.Invoke(Game, null);
            int savedActive = Water.ActiveFountainCount; bool savedPending = Water.HasPendingPropagation;
            until = Time.time + 120;
            while (Water.HasPendingPropagation && Time.time < until) { SampleWhenChanged("front"); yield return null; }
            SampleState("front-settled");
            CheckState("all-authored-targets-entered", !Water.HasPendingPropagation && Water.ActiveFountainCount >= Water.RequiredFountainCount && Water.RequiredFountainCount > 0, Counts());
            int frameLimit = Time.frameCount + 110;
            while (!Game.IsComplete && Time.frameCount < frameLimit) yield return null;
            CheckState("native-victory-after-arrivals", Game.IsComplete, Counts());
            restore.Invoke(Game, new[] { snapshot }); SampleState("snapshot-restored");
            CheckState("production-snapshot-restores-front", savedPending && Water.HasPendingPropagation && Water.ActiveFountainCount == savedActive,
                Counts());
            var restoredVisuals = Visuals(); Water.Rebuild();
            CheckState("next-rebuild-keeps-restored-water", restoredVisuals.All(pair => Visuals().ContainsKey(pair.Key) && Visuals()[pair.Key] == pair.Value),
                "Retained restored roots=" + restoredVisuals.Count);
            until = Time.time + 120;
            while (Water.HasPendingPropagation && Time.time < until) yield return null;
            SampleState("restored-front-settled");
            CheckState("restored-front-reaches-targets", !Water.HasPendingPropagation && Water.ActiveFountainCount >= Water.RequiredFountainCount, Counts());
            IEnumerator door = DoorSnapshotFixture(capture, restore);
            while (door.MoveNext()) yield return door.Current;
            IEnumerator rotary = RotatingWaterSnapshotFixture(capture, restore);
            while (rotary.MoveNext()) yield return rotary.Current;
        }
        static IEnumerator DoorSnapshotFixture(MethodInfo capture, MethodInfo restore)
        {
            FlowWaterRecoveryUI.Instance.StartGame(1052);
            while (Game == null || Game.CurrentLevelId != 1052 || Game.PieceRoot == null) yield return null;
            Game.RestartLevel();
            object immediate = capture.Invoke(Game, null);
            bool currentAtCapture = Water.LevelId == Game.CurrentLevelId && Water.CurrentPieceRoot == Game.PieceRoot;
            float immediateRemaining = Water.LastScheduledWaterEventTime - Time.time;
            Water.Rebuild(); restore.Invoke(Game, new[] { immediate });
            CheckState("same-frame-load-capture-matches-board", currentAtCapture && Water.LevelId == Game.CurrentLevelId && Water.CurrentPieceRoot == Game.PieceRoot,
                "Same-frame RestartLevel -> production CaptureSnapshot; " + Counts());
            CheckState("same-frame-load-capture-keeps-source-timers", currentAtCapture &&
                Mathf.Abs((Water.LastScheduledWaterEventTime - Time.time) - immediateRemaining) < .02f, Counts());
            Game.Solve(); Water.Rebuild();
            RuntimeWaterMechanisms mechanisms = Water.GetComponent<RuntimeWaterMechanisms>();
            while ((Transform)typeof(RuntimeWaterMechanisms).GetField("levelRoot", Private).GetValue(mechanisms) != Game.PieceRoot || mechanisms.DoorCount == 0) yield return null;
            // A real original N/8<g1g> door is closed before the source front
            // reaches its original yellow input hole. No switch is fabricated.
            Water.Rebuild();
            RecoveryPiece door = Game.Pieces.FirstOrDefault(piece => piece.SourceRecordIndex == 11);
            CheckState("gm1052-authored-door-closed", door != null && PortIsClosed(door.Index, 2) && RecoveredWaterJets.Global("conDoor1") == 2,
                "record11 port2; " + Counts());
            object saved = capture.Invoke(Game, null);
            bool pending = Water.HasPendingPropagation;
            float remaining = Water.LastScheduledWaterEventTime - Time.time;
            Transform savedRoot = Game.PieceRoot;
            Game.RestartLevel(); restore.Invoke(Game, new[] { saved });
            door = Game.Pieces.FirstOrDefault(piece => piece.SourceRecordIndex == 11);
            CheckState("closed-door-restores-before-next-frame", door != null && Game.PieceRoot != savedRoot && Water.CurrentPieceRoot == Game.PieceRoot &&
                PortIsClosed(door.Index, 2) && RecoveredWaterJets.Global("conDoor1") == 2, Counts());
            CheckState("door-board-front-duration-restored", pending && Water.HasPendingPropagation &&
                Mathf.Abs((Water.LastScheduledWaterEventTime - Time.time) - remaining) < .02f,
                "Before=" + remaining + ", after=" + (Water.LastScheduledWaterEventTime - Time.time) + "; " + Counts());
            var ids = Visuals(); Water.Rebuild();
            CheckState("closed-door-restored-water-survives-rebuild", ids.Count > 0 && ids.All(pair => Visuals().ContainsKey(pair.Key) && Visuals()[pair.Key] == pair.Value),
                "Retained=" + ids.Count + "; " + Counts());
            SampleState("closed-door-snapshot-restored");
        }
        static IEnumerator RotatingWaterSnapshotFixture(MethodInfo capture, MethodInfo restore)
        {
            FlowWaterRecoveryUI.Instance.StartGame(605);
            while (Game == null || Game.CurrentLevelId != 605 || Game.PieceRoot == null) yield return null;
            Water.Rebuild();
            var springs = RecoveredNativeSprings.Attach(Game);
            RecoveryPiece rotary = Game.Pieces.FirstOrDefault(piece => piece.SourceRecordIndex == 13);
            RecoveryPiece player = Game.Pieces.FirstOrDefault(piece => piece.CanMove &&
                (piece.transform.position - new Vector3(80, 3.95f, 40)).sqrMagnitude < .01f);
            CheckState("gm605-original-rotating-c-fixture", rotary != null && rotary.GetComponent<ControlPiezaC>() != null && player != null,
                "Original CuFuenteED1 record13, BloqueResorte record12 and first Save1 placement.");
            if (rotary == null || player == null) yield break;
            bool placed = Game.TryMove(player.Index, new Vector3(60, 3.95f, 20));
            CheckState("gm605-first-native-pressure-placement", placed, Game.Status);
            if (!placed) yield break;
            float until = Time.time + 15;
            while (!springs.IsBusy && Time.time < until) yield return null;
            CheckState("gm605-real-c-rotation-started", springs.IsBusy && MovingWaterIsGated(rotary),
                "No synthetic switch: original first Save1 placement started the native plate and observer.");
            if (!springs.IsBusy) yield break;
            object moving = capture.Invoke(Game, null);
            Quaternion captured = rotary.transform.rotation;
            until = Time.time + 15;
            while (springs.IsBusy && Time.time < until) yield return null;
            Transform oldRoot = Game.PieceRoot;
            Game.RestartLevel(); restore.Invoke(Game, new[] { moving });
            rotary = Game.Pieces.FirstOrDefault(piece => piece.SourceRecordIndex == 13);
            springs = RecoveredNativeSprings.Attach(Game);
            CheckState("rotating-c-new-root-snapshot-keeps-water-gate", rotary != null && Game.PieceRoot != oldRoot &&
                springs.IsBusy && Quaternion.Angle(rotary.transform.rotation, captured) < .1f && MovingWaterIsGated(rotary),
                "Springs.Restore creates the new actor gate before Water.Restore rebuilds the new root.");
            Water.Rebuild();
            CheckState("rotating-c-rebuild-keeps-water-gate", rotary != null && MovingWaterIsGated(rotary) && !HasWaterNode(rotary),
                "Original rotating AguaControl5C remains inactive until the observer completes.");
            until = Time.time + 15;
            while (springs.IsBusy && Time.time < until) yield return null;
            Water.Rebuild();
            CheckState("rotating-c-completion-reactivates-water", rotary != null && !springs.IsBusy && !MovingWaterIsGated(rotary) && HasWaterNode(rotary),
                "The original observer end callback releases its C-only water gate.");
            SampleState("rotating-c-snapshot-completed");
        }
        static bool MovingWaterIsGated(RecoveryPiece piece)
        {
            foreach (object actor in (IEnumerable)typeof(RecoveredWaterNetwork).GetField("movingPieces", Private).GetValue(Water))
                if (actor == (object)piece) return true;
            return false;
        }
        static bool HasWaterNode(RecoveryPiece piece)
        {
            foreach (object node in (IEnumerable)typeof(RecoveredWaterNetwork).GetField("nodes", Private).GetValue(Water))
                if (node.GetType().GetField("piece").GetValue(node) == (object)piece) return true;
            return false;
        }
        static bool PortIsClosed(int pieceIndex, int port)
        {
            foreach (object node in (IEnumerable)typeof(RecoveredWaterNetwork).GetField("nodes", Private).GetValue(Water))
            {
                Type type = node.GetType(); var piece = type.GetField("piece").GetValue(node) as RecoveryPiece;
                if (piece != null && piece.Index == pieceIndex) return !((bool[])type.GetField("ports").GetValue(node))[port - 1];
            }
            return false;
        }
        static Dictionary<string, int> Visuals()
        {
            var result = new Dictionary<string, int>();
            foreach (object node in (IEnumerable)typeof(RecoveredWaterNetwork).GetField("nodes", Private).GetValue(Water))
            {
                Type type = node.GetType(); var water = type.GetField("water").GetValue(node) as GameObject;
                if (water != null) result[(string)type.GetField("key").GetValue(node)] = water.GetInstanceID();
            }
            return result;
        }
        static string Counts() { return "wet=" + Water.WetPieceCount + ", fountains=" + Water.ActiveFountainCount + "/" + Water.RequiredFountainCount + ", pending=" + Water.PendingWaterEventCount; }
        static void CheckState(string name, bool passed, string detail) { report.checks.Add(new Check { name = name, passed = passed, detail = detail }); }
        static void SampleWhenChanged(string stage)
        {
            Sample last = report.samples.Count == 0 ? null : report.samples[report.samples.Count - 1];
            if (last == null || last.wet != Water.WetPieceCount || last.active != Water.ActiveFountainCount) SampleState(stage);
        }
        static void SampleState(string stage) { report.samples.Add(new Sample { stage = stage, time = Time.time, wet = Water.WetPieceCount,
            active = Water.ActiveFountainCount, required = Water.RequiredFountainCount, pending = Water.PendingWaterEventCount, waterPrefabs = Water.OriginalWaterPrefabCount }); }
        static void Unlock()
        {
            Time.timeScale = SessionState.GetFloat(ScaleKey, originalScale > 0 ? originalScale : 1);
            if (!locked) return; EditorApplication.UnlockReloadAssemblies(); AssetDatabase.AllowAutoRefresh(); locked = false;
        }
        static void Finish()
        {
            RestorePreferences();
            report.preferencesRestored = true; report.finishedUtc = DateTime.UtcNow.ToString("o");
            report.passed = report.errors.Count == 0 && report.checks.Count > 0 && report.checks.All(check => check.passed);
            Directory.CreateDirectory(Output); File.WriteAllText(Path.Combine(Output, "water-propagation-validation.json"), JsonUtility.ToJson(report, true));
            SessionState.SetInt(ExitKey, report.passed ? 0 : 1); Unlock(); routine = null; report = null;
            EditorApplication.isPlaying = false;
        }
        static void RestorePreferences()
        {
            Backup backup = JsonUtility.FromJson<Backup>(SessionState.GetString(BackupKey, "{}"));
            if (backup == null || backup.values == null) return;
            foreach (Preference value in backup.values)
                if (value.existed) PlayerPrefs.SetInt(value.key, value.value); else PlayerPrefs.DeleteKey(value.key);
            PlayerPrefs.Save(); SessionState.EraseString(BackupKey);
        }
        static void RestoreScenes()
        {
            SavedScenes saved = JsonUtility.FromJson<SavedScenes>(SessionState.GetString(ScenesKey, "{}"));
            if (saved != null && saved.scenes != null && saved.scenes.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(saved.scenes.Select(scene => new SceneSetup { path = scene.path, isLoaded = scene.loaded, isActive = scene.active }).ToArray());
            SessionState.EraseString(ScenesKey); SessionState.EraseFloat(ScaleKey);
        }
    }
}
#endif
