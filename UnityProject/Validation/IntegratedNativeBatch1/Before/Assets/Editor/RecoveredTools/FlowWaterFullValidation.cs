#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Recovered.FlowWater.Editor
{
    [InitializeOnLoad]
    public static class FlowWaterFullValidation
    {
        const string SessionKey = "FlowWater.FullValidation.Mode";
        const string TimeKey = "FlowWater.FullValidation.OriginalTimeScale";
        static readonly int[] Representatives = { 401, 1, 251, 351, 1001, 1051, 1101, 1151, 1201, 1651, 1701, 1751, 1801, 1851, 1901, 1951, 2001,
            13, 82, 113, 158, 275, 355, 403, 514, 552, 605, 651, 707, 751, 1002, 1054, 1103, 1152, 1204 };
        [Serializable] public sealed class LevelResult
        {
            public int assetId, pieces, missingPrefabs, activeRenderers, emptyMaterials, emptyMeshes;
            public int wetAtStart, wetAtSolution, basins, filledBasins, requiredFountains, activeFountains, waterPrefabs, particleSystems;
            public string scene, expectedScene, status;
            public bool originalPresentation, geometricSolutionAvailable, geometricSolutionMatched, movedAndUndone, waterVictory;
            public bool previewHasNoCountedTargets, normalGameplayVerified;
            public string normalGameplayEvidence;
            public int pendingWaterEvents;
            public float propagationSeconds;
            public List<string> failures = new List<string>();
        }
        [Serializable] public sealed class Report
        {
            public string startedUtc, finishedUtc, unityVersion, scope;
            public bool all1350Instantiated, passed, originalEquivalenceEstablished;
            public int requested, completed, failureCount, unsupportedSolutions;
            public float simulationTimeScale, originalTimeScale;
            public List<LevelResult> levels = new List<LevelResult>();
            public List<string> runtimeErrors = new List<string>();
            public List<string> contactSheets = new List<string>();
        }
        [Serializable] sealed class WaterMeshDiagnostic
        {
            public string path, mesh, material, shader, texture;
            public Vector3 center, size, localPosition, localEuler, worldEuler, localScale, worldScale;
            public float materialAlpha;
        }
        [Serializable] sealed class WaterMeshReport { public List<WaterMeshDiagnostic> meshes = new List<WaterMeshDiagnostic>(); }
        [Serializable] sealed class WaterNodeDiagnostic
        {
            public int index, pieceIndex, sourceRecord, expectedColor, waterColor, incoming;
            public string prefab, configuration, type;
            public Vector3 position, rotation;
            public bool[] ports;
            public int[] links;
            public bool source, basin, pedestal, target;
            public List<WaterJetDiagnostic> jets;
        }
        [Serializable] sealed class WaterJetDiagnostic
        { public string configuration, type; public int mode, direction, receiverRecord; public Vector3 displacement; }
        [Serializable] sealed class WaterGraphReport
        { public List<WaterNodeDiagnostic> nodes = new List<WaterNodeDiagnostic>(); public string[] blockedFlow, solutionCaptures; }
        static Report report;
        static int[] ids;
        static int index, phase, solutionFrame, settledFrame;
        static double next, deadline;
        static float solutionStarted;
        static bool busy;
        static bool importsBlocked;
        static Texture2D sheet;
        static int sheetSlots, sheetIndex;
        static string mode;
        [Serializable] sealed class SavedInt { public string key; public bool existed; public int value; }
        static string Output { get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation", "FullContent"); } }
        static FlowWaterFullValidation()
        {
            EditorApplication.update += Update;
            Application.logMessageReceived += OnLog;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.ExitingPlayMode || report == null) return;
                Time.timeScale = SessionState.GetFloat(TimeKey, 4); RestorePreferences(); ReleaseImports(); SessionState.EraseString(SessionKey); report = null;
            };
        }

        [MenuItem("Tools/Flow Water/Validate All 1350 Runtime Layouts")]
        public static void RunAll() { Start("all"); }
        public static void RunRepresentative() { Start("representative"); }
        public static void RunRegression() { Start("regression"); }
        public static void RunFailures() { Start("failures"); }
        static void Start(string requestedMode)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start from Edit mode.");
            RecoveryCatalogBuilder.Build();
            Directory.CreateDirectory(Output);
            SessionState.SetString(SessionKey, requestedMode);
            SessionState.SetFloat(TimeKey, Time.timeScale);
            SavePreferences();
            EditorSceneManager.OpenScene("Assets/Scenes/Portada.unity", OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }
        static void Initialize()
        {
            mode = SessionState.GetString(SessionKey, "");
            if (string.IsNullOrEmpty(mode)) return;
            AssetDatabase.DisallowAutoRefresh(); EditorApplication.LockReloadAssemblies(); importsBlocked = true;
            RecoveryCatalog catalog = RecoveryCatalog.Load();
            if (mode == "all") ids = catalog.levels.Select(x => x.assetId).OrderBy(x => x).ToArray();
            else if (mode == "failures")
            {
                Report prior = JsonUtility.FromJson<Report>(File.ReadAllText(Path.Combine(Output, "all-runtime-validation.json")));
                ids = prior.levels.Where(x => x.failures.Count > 0).Select(x => x.assetId).ToArray();
            }
            else if (mode == "regression") ids = new[] { 514, 1701, 1751, 1801, 1951, 2001 };
            else ids = Representatives;
            report = new Report { startedUtc = DateTime.UtcNow.ToString("o"), unityVersion = Application.unityVersion,
                requested = ids.Length, simulationTimeScale = 10, originalTimeScale = SessionState.GetFloat(TimeKey, 4), scope = "Every requested layout is instantiated in its native-selected original scene, rendered after actual water-front propagation, inspected, moved/undone where possible, and its saved solution is applied. Simulation uses timeScale=10 and restores the original value; native delayed events and the 51-frame victory poll remain active. Preview boards without counted targets require separate successful normal gameplay evidence. Original binary equivalence is a separate assessment." };
            Time.timeScale = report.simulationTimeScale;
            index = phase = sheetIndex = sheetSlots = 0;
            deadline = EditorApplication.timeSinceStartup + 60;
            next = EditorApplication.timeSinceStartup + 1;
        }
        static void Update()
        {
            if (busy || EditorApplication.isCompiling || !EditorApplication.isPlaying) return;
            if (report == null) Initialize();
            if (report == null || EditorApplication.timeSinceStartup < next) return;
            busy = true;
            try
            {
                if (index >= ids.Length) { Finish(); return; }
                int id = ids[index];
                FlowWaterRecoveryUI ui = FlowWaterRecoveryUI.Instance;
                RecoveredFlowGame game = RecoveredFlowGame.Instance;
                if (phase == 0)
                {
                    if (ui == null) throw new InvalidOperationException("Original UI adapter did not initialize.");
                    ui.StartGame(id); phase = 1; deadline = EditorApplication.timeSinceStartup + 120; next = EditorApplication.timeSinceStartup + (mode == "all" ? .6 : 1.1);
                }
                else if (phase == 1)
                {
                    if (game == null || game.CurrentLevelId != id || !game.IsLevelActive)
                    {
                        if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Layout did not load: gm" + id);
                        return;
                    }
                    var initialWater = RecoveredWaterNetwork.Instance;
                    if (initialWater == null || initialWater.CurrentPieceRoot != game.PieceRoot || initialWater.HasPendingPropagation)
                    {
                        if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Initial water front did not settle: gm" + id);
                        next = EditorApplication.timeSinceStartup + .05; return;
                    }
                    var entry = new LevelResult { assetId = id, scene = SceneManager.GetActiveScene().name,
                        expectedScene = OriginalScenePresentation.SceneForLevel(id), pieces = game.PieceCount,
                        missingPrefabs = game.UnresolvedPrefabCount, originalPresentation = FlowWaterValidation.HasOriginalPresentation(),
                        geometricSolutionAvailable = game.HasSolution, status = game.Status };
                    if (entry.scene != entry.expectedScene) entry.failures.Add("Wrong native-selected scene.");
                    if (!entry.originalPresentation) entry.failures.Add("Original camera/ground/UI presentation is missing.");
                    if (entry.missingPrefabs != 0) entry.failures.Add("Unresolved original prefab.");
                    if (!entry.geometricSolutionAvailable) entry.failures.Add("Original solution could not be loaded.");
                    Inspect(entry, game);
                    var water = RecoveredWaterNetwork.Instance;
                    if (water != null) { entry.wetAtStart = water.WetPieceCount; entry.basins = water.BasinCount; entry.waterPrefabs = water.OriginalWaterPrefabCount; entry.particleSystems = water.OriginalParticleSystemCount; }
                    // Exercise the same placement path used by dragging, preserving the initial board.
                    foreach (RecoveryPiece piece in game.Pieces)
                    {
                        if (!piece.CanMove || !piece.HasGoal || (piece.InitialPosition - piece.GoalPosition).sqrMagnitude < 1) continue;
                        Vector3 before = piece.transform.position;
                        if (!game.TryMove(piece.Index, piece.GoalPosition)) continue;
                        game.Undo(); entry.movedAndUndone = (piece.transform.position - before).sqrMagnitude < .01f && game.MoveCount == 0;
                        if (!entry.movedAndUndone) entry.failures.Add("Move/undo did not restore the initial layout.");
                        break;
                    }
                    report.levels.Add(entry);
                    if (mode != "all") FlowWaterValidation.SaveVisibleFrame(Path.Combine(Output, "gm" + id + "-start.png"));
                    AddThumbnail(game.GameCamera);
                    if (game.HasSolution) game.Solve(); else report.unsupportedSolutions++;
                    entry.geometricSolutionMatched = game.MatchesSavedSolution;
                    solutionFrame = Time.frameCount;
                    settledFrame = -1;
                    solutionStarted = Time.time;
                    phase = 2; deadline = EditorApplication.timeSinceStartup + 120;
                    next = EditorApplication.timeSinceStartup + (mode == "all" ? .8 : 3.2);
                }
                else
                {
                    var pendingWater = RecoveredWaterNetwork.Instance;
                    if (pendingWater != null && pendingWater.HasPendingPropagation && EditorApplication.timeSinceStartup < deadline)
                    { settledFrame = -1; next = EditorApplication.timeSinceStartup + .05; return; }
                    if (settledFrame < 0) settledFrame = Time.frameCount;
                    if (Time.frameCount - settledFrame < 60 && EditorApplication.timeSinceStartup < deadline)
                    { next = EditorApplication.timeSinceStartup + .05; return; }
                    LevelResult entry = report.levels[report.levels.Count - 1];
                    entry.waterVictory = game.IsComplete;
                    if (game.HasSolution && !entry.geometricSolutionMatched) entry.failures.Add("Saved geometric solution did not match immediately after applying it.");
                    var water = RecoveredWaterNetwork.Instance;
                    if (water != null)
                    {
                        entry.pendingWaterEvents = water.PendingWaterEventCount;
                        entry.propagationSeconds = Time.time - solutionStarted;
                        if (water.HasPendingPropagation) entry.failures.Add("Native water propagation did not settle before the validation deadline.");
                        entry.wetAtSolution = water.WetPieceCount; entry.filledBasins = water.FilledBasinCount;
                        entry.requiredFountains = water.RequiredFountainCount; entry.activeFountains = water.ActiveFountainCount;
                        entry.waterPrefabs = water.OriginalWaterPrefabCount; entry.particleSystems = water.OriginalParticleSystemCount;
                        entry.normalGameplayVerified = FindNormalGameplayEvidence(id, out entry.normalGameplayEvidence);
                        if (entry.requiredFountains == 0)
                        {
                            entry.previewHasNoCountedTargets = game.IsShowingFinalSolution && game.SolutionStepCount > 0 &&
                                game.Pieces.Any(p => (p.OriginalConfiguration ?? "").Contains("st9et"));
                            // Original doSolucionLevel disables victory checking.
                            // Some mode1 boards contain only st9 target proxies.
                            // Their preview cannot prove a win: require a separate
                            // real mode0 replay with every original target supplied.
                            if (!entry.previewHasNoCountedTargets || !entry.normalGameplayVerified)
                                entry.failures.Add("No counted target in this preview; successful normal gameplay replay is required.");
                        }
                        if (entry.waterPrefabs == 0) entry.failures.Add("No original water prefab was activated.");
                        if (entry.requiredFountains > 0 && entry.activeFountains < entry.requiredFountains)
                            entry.failures.Add("Saved solution leaves original fountain targets dry or with the wrong water color: " + entry.activeFountains + "/" + entry.requiredFountains + ".");
                        else if (game.HasSolution && entry.requiredFountains > 0 && !entry.waterVictory)
                            entry.failures.Add("Fountains are supplied but the native 51-frame victory did not occur.");
                    }
                    else entry.failures.Add("Water simulation is missing.");
                    if (mode != "all" || entry.failures.Count > 0)
                    {
                        ui.CaptureBoardFrame(() => FlowWaterValidation.SaveVisibleFrame(Path.Combine(Output, "gm" + id + "-solution.png")));
                        WriteWaterDiagnostics(id, game);
                        WriteWaterGraph(id);
                    }
                    ui.CaptureBoardFrame(() => AddThumbnail(game.GameCamera));
                    report.completed++; report.failureCount += entry.failures.Count;
                    index++; phase = 0; next = EditorApplication.timeSinceStartup + .02;
                    if (index % 25 == 0 || index == ids.Length)
                    {
                        Save(false);
                        EditorUtility.UnloadUnusedAssetsImmediate();
                        GC.Collect();
                        Debug.Log("Full content validation: " + index + "/" + ids.Length + "; failures=" + report.failureCount);
                    }
                }
            }
            catch (Exception exception)
            {
                if (report != null)
                {
                    report.runtimeErrors.Add("gm" + (ids != null && index < ids.Length ? ids[index] : 0) + ": " + exception);
                    report.failureCount++;
                    index++; phase = 0; next = EditorApplication.timeSinceStartup + .1;
                    Save(false);
                }
            }
            finally { busy = false; }
        }
        static void Inspect(LevelResult entry, RecoveredFlowGame game)
        {
            foreach (Renderer renderer in game.PieceRoot.GetComponentsInChildren<Renderer>(false))
            {
                if (!renderer.enabled) continue;
                entry.activeRenderers++;
                foreach (Material material in renderer.sharedMaterials)
                    if (material == null || material.shader == null || ShaderUtil.ShaderHasError(material.shader)) entry.emptyMaterials++;
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh == null) entry.emptyMeshes++;
            }
            if (entry.emptyMaterials > 0) entry.failures.Add("Active original renderer has a missing/error material.");
            if (entry.emptyMeshes > 0) entry.failures.Add("Active original renderer has a missing mesh.");
            if (entry.activeRenderers == 0) entry.failures.Add("No visible original piece geometry.");
        }
        static void WriteWaterDiagnostics(int id, RecoveredFlowGame game)
        {
            var diagnostic = new WaterMeshReport();
            foreach (RecoveryPiece piece in game.Pieces)
                foreach (MeshRenderer renderer in piece.GetComponentsInChildren<MeshRenderer>(false))
                {
                    if (!renderer.enabled || diagnostic.meshes.Count >= 5000) continue;
                    string path = renderer.name;
                    Transform parent = renderer.transform.parent;
                    while (parent != null && parent != piece.transform) { path = parent.name + "/" + path; parent = parent.parent; }
                    if (!path.Contains("Original water")) continue;
                    Bounds bounds = renderer.bounds;
                    MeshFilter filter = renderer.GetComponent<MeshFilter>();
                    Material material = renderer.sharedMaterial;
                    diagnostic.meshes.Add(new WaterMeshDiagnostic { path = piece.name + "/" + path,
                        mesh = filter == null || filter.sharedMesh == null ? "" : filter.sharedMesh.name,
                        center = bounds.center, size = bounds.size, localPosition = renderer.transform.localPosition,
                        localEuler = renderer.transform.localEulerAngles, worldEuler = renderer.transform.eulerAngles,
                        localScale = renderer.transform.localScale, worldScale = renderer.transform.lossyScale,
                        material = material == null ? "" : material.name, shader = material == null || material.shader == null ? "" : material.shader.name,
                        texture = material == null || material.mainTexture == null ? "" : material.mainTexture.name,
                        materialAlpha = material == null || !material.HasProperty("_Color") ? 1 : material.color.a });
                }
            File.WriteAllText(Path.Combine(Output, "gm" + id + "-water-mesh-diagnostics.json"), JsonUtility.ToJson(diagnostic, true));
        }
        static void WriteWaterGraph(int id)
        {
            var water = RecoveredWaterNetwork.Instance;
            if (water == null) return;
            var nodes = (IList)typeof(RecoveredWaterNetwork).GetField("nodes", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(water);
            var diagnostic = new WaterGraphReport { blockedFlow = water.BlockedFlowDiagnostics,
                solutionCaptures = RecoveredFlowGame.Instance.SolutionCaptureTrace };
            foreach (object node in nodes)
            {
                RecoveryPiece piece = (RecoveryPiece)NodeField(node, "piece");
                MonoBehaviour controller = (MonoBehaviour)NodeField(node, "controller");
                var links = new List<int>();
                foreach (object link in (IEnumerable)NodeField(node, "links")) links.Add(link == null ? -1 : nodes.IndexOf(link));
                var jets = new List<WaterJetDiagnostic>();
                var routes = NodeField(node, "jets") as IEnumerable<RecoveredWaterJets.Route>;
                if (routes != null) foreach (RecoveredWaterJets.Route route in routes)
                {
                    RecoveryPiece receiver = route.receiver == null ? null : route.receiver.GetComponentInParent<RecoveryPiece>();
                    jets.Add(new WaterJetDiagnostic { configuration = route.configuration, type = route.type, mode = route.mode,
                        direction = route.direction, displacement = route.displacement, receiverRecord = receiver == null ? -1 : receiver.SourceRecordIndex });
                }
                diagnostic.nodes.Add(new WaterNodeDiagnostic { index = nodes.IndexOf(node), pieceIndex = piece.Index, sourceRecord = piece.SourceRecordIndex,
                    prefab = piece.BasePrefab, configuration = piece.OriginalConfiguration, type = controller.GetType().Name,
                    position = controller.transform.position, rotation = controller.transform.eulerAngles, ports = (bool[])NodeField(node, "ports"), links = links.ToArray(),
                    expectedColor = (int)NodeField(node, "expectedColor"), waterColor = (int)NodeField(node, "waterColor"), incoming = (int)NodeField(node, "incoming"),
                    source = (bool)NodeField(node, "source"), basin = (bool)NodeField(node, "basin"), pedestal = (bool)NodeField(node, "pedestal"), target = (bool)NodeField(node, "target"), jets = jets });
            }
            File.WriteAllText(Path.Combine(Output, "gm" + id + "-water-graph.json"), JsonUtility.ToJson(diagnostic, true));
        }
        static object NodeField(object node, string name) { return node.GetType().GetField(name).GetValue(node); }
        static bool FindNormalGameplayEvidence(int id, out string path)
        {
            string root = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation", "Save1Gameplay");
            foreach (string candidate in new[] { Path.Combine(root, "save1-gameplay-validation.json"),
                Path.Combine(root, "FocusedFront", "save1-gameplay-validation.json"),
                Path.Combine(root, "FirstFocused", "save1-gameplay-validation.json") })
            {
                if (!File.Exists(candidate)) continue;
                var gameplay = JsonUtility.FromJson<FlowWaterSave1GameplayValidation.Report>(File.ReadAllText(candidate));
                if (gameplay == null || gameplay.unityVersion != Application.unityVersion || String.IsNullOrEmpty(gameplay.finishedUtc)) continue;
                var level = gameplay.levels.FirstOrDefault(x => x.id == id);
                if (level != null && level.passed && level.normalGameplay && level.complete && level.nativeMissionComplete &&
                    level.finalRequired > 0 && level.finalActive >= level.finalRequired)
                { path = candidate; return true; }
            }
            path = ""; return false;
        }
        static void AddThumbnail(Camera camera)
        {
            if (camera == null) return;
            const int width = 320, height = 180, columns = 6, rows = 4;
            if (sheet == null) { sheet = new Texture2D(width * columns, height * rows, TextureFormat.RGB24, false); sheetSlots = 0; }
            RenderTexture prior = RenderTexture.active;
            RenderTexture oldTarget = camera.targetTexture;
            float oldAspect = camera.aspect;
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var thumb = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target; camera.aspect = width / (float)height;
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                thumb.ReadPixels(new Rect(0, 0, width, height), 0, 0); thumb.Apply();
                int column = sheetSlots % columns, row = sheetSlots / columns;
                sheet.SetPixels(column * width, (rows - 1 - row) * height, width, height, thumb.GetPixels());
                sheetSlots++;
            }
            finally
            {
                camera.targetTexture = oldTarget; camera.aspect = oldAspect; RenderTexture.active = prior;
                RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(thumb); Canvas.ForceUpdateCanvases();
            }
            if (sheetSlots == columns * rows) FlushSheet();
        }
        static void FlushSheet()
        {
            if (sheet == null) return;
            sheet.Apply();
            string path = Path.Combine(Output, mode + "-contact-" + (++sheetIndex).ToString("D3") + ".png");
            File.WriteAllBytes(path, sheet.EncodeToPNG()); report.contactSheets.Add(path);
            UnityEngine.Object.DestroyImmediate(sheet); sheet = null; sheetSlots = 0;
        }
        static void OnLog(string message, string stack, LogType type)
        {
            if (report == null || (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)) return;
            string value = "gm" + (ids != null && index < ids.Length ? ids[index] : 0) + ": " + message;
            if (!report.runtimeErrors.Contains(value)) report.runtimeErrors.Add(value);
        }
        static void Save(bool final)
        {
            report.all1350Instantiated = mode == "all" && report.completed == 1350;
            report.passed = final && report.completed == report.requested && report.failureCount == 0 && report.runtimeErrors.Count == 0;
            if (final) report.finishedUtc = DateTime.UtcNow.ToString("o");
            File.WriteAllText(Path.Combine(Output, mode + "-runtime-validation.json"), JsonUtility.ToJson(report, true));
        }
        static void Finish()
        {
            FlushSheet(); Save(true); SessionState.EraseString(SessionKey);
            Time.timeScale = report.originalTimeScale;
            RestorePreferences();
            ReleaseImports();
            Debug.Log("Full content validation " + mode + ": " + (report.passed ? "PASS" : "FAIL") + "; layouts=" + report.completed + "; errors=" + report.runtimeErrors.Count);
            bool passed = report.passed; report = null;
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1); else EditorApplication.isPlaying = false;
        }

        [Serializable] sealed class PreferenceBackup { public List<SavedInt> values = new List<SavedInt>(); }
        static void SavePreferences()
        {
            var backup = new PreferenceBackup();
            AddPreference(backup, "Recovery.LastLevelId");
            foreach (RecoveryLevelEntry level in RecoveryCatalog.Load().levels)
            {
                AddPreference(backup, "Recovery.Completed.gm" + level.assetId);
                AddPreference(backup, "Recovery.Complete." + level.assetId);
            }
            SessionState.SetString(SessionKey + ".Preferences", JsonUtility.ToJson(backup));
        }
        static void AddPreference(PreferenceBackup backup, string key)
        {
            backup.values.Add(new SavedInt { key = key, existed = PlayerPrefs.HasKey(key), value = PlayerPrefs.GetInt(key) });
        }
        static void RestorePreferences()
        {
            string json = SessionState.GetString(SessionKey + ".Preferences", "");
            if (string.IsNullOrEmpty(json)) return;
            foreach (SavedInt item in JsonUtility.FromJson<PreferenceBackup>(json).values)
                if (item.existed) PlayerPrefs.SetInt(item.key, item.value); else PlayerPrefs.DeleteKey(item.key);
            PlayerPrefs.Save();
            SessionState.EraseString(SessionKey + ".Preferences");
        }
        static void ReleaseImports()
        {
            if (!importsBlocked) return;
            importsBlocked = false; EditorApplication.UnlockReloadAssemblies(); AssetDatabase.AllowAutoRefresh();
        }
    }
}
#endif
