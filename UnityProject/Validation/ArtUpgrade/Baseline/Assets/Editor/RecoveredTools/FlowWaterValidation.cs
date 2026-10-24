// Validation written for the recovery project; this is not original game code.
#if UNITY_EDITOR
using System;
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
    [InitializeOnLoad]
    public static class FlowWaterValidation
    {
        const string SessionKey = "Recovered.FlowWater.Validation.PlaySession";
        const string RuntimeTypeName = "Recovered.FlowWater.RecoveredFlowGame";
        static readonly string[] ExpectedScenes = {
            "Assets/Scenes/Portada.unity", "Assets/Scenes/Levely5z/Levely5.unity",
            "Assets/Scenes/SelLevel0.unity", "Assets/Scenes/SelLevel1.unity",
            "Assets/Settings.unity", "Assets/Store.unity", "Assets/Scenes/Levely5_6x6.unity",
            "Assets/Scenes/Levely5_7x7.unity", "Assets/Scenes/Levely5_basic.unity"
        };
        static readonly int[] PlayLevelIds = { 1, 1001, 1651 };
        static bool ticking;
        public static bool SuppressBatchExit { get; private set; }

        [Serializable] public sealed class AssetResult
        {
            public string path;
            public int gameObjects, missingScripts, renderers, missingMaterialSlots, missingMeshes;
        }
        [Serializable] public sealed class Check
        {
            public string name, detail;
            public bool passed;
        }
        [Serializable] public sealed class WaterSnapshot
        {
            public int level, wetPieceCount, basinCount, filledBasinCount;
            public string stage;
        }
        [Serializable] public sealed class DataReplayEntry
        {
            public int assetId, pieceCount, stepCount, complexConfigurationCount;
            public string classification, reason, solutionKind;
            public bool originalMechanismsVerified;
        }
        [Serializable] public sealed class DataReplayReport
        {
            public int total, success, unsupported, failure;
            public string scope;
            public List<DataReplayEntry> entries = new List<DataReplayEntry>();
        }
        [Serializable] public sealed class ReplayKindCounts
        {
            public string solutionKind;
            public int total, success, unsupported, failure;
        }
        [Serializable] public sealed class ConfigurationCounts
        {
            public int totalOccurrences, distinctValues, movableN, fixedY, numericConfiguration, complexConfiguration;
            public int levelsWithComplexConfiguration;
            public string scope = "Configuration strings are retained verbatim. Categories describe syntax, not recovered original mechanism semantics.";
        }
        [Serializable] public sealed class Report
        {
            public string kind, startedUtc, finishedUtc, unityVersion;
            public bool passed, originalGameplayFullyRestored;
            public string scope = "Checks imported original assets and newly reconstructed gameplay. Passing does not establish original IL2CPP behavior or visual equivalence.";
            public int sceneCount, prefabCount, materialCount, shaderCount;
            public List<AssetResult> scenes = new List<AssetResult>();
            public List<AssetResult> prefabs = new List<AssetResult>();
            public List<Check> checks = new List<Check>();
            public List<string> warnings = new List<string>();
            public List<string> failures = new List<string>();
            public List<string> screenshots = new List<string>();
            public List<string> runtimeErrors = new List<string>();
            public List<WaterSnapshot> waterSnapshots = new List<WaterSnapshot>();
            public int layoutCount, solutionCount, aliasCount;
            public DataReplayReport dataReplay;
            public List<ReplayKindCounts> replayBySolutionKind = new List<ReplayKindCounts>();
            public ConfigurationCounts configurations;
        }
        [Serializable] sealed class PlaySession
        {
            public bool active;
            public int phase, levelIndex, hintMoves, initialWetPieces;
            public double nextUpdate;
            public long startedTicks;
            public string pendingScreenshot;
            public double screenshotDeadline;
            public Report report;
        }
        [Serializable] sealed class LayoutEnvelope { public Layout data; }
        [Serializable] sealed class Layout { public string[] pieza; }
        sealed class CameraCaptureState
        {
            public Camera camera;
            public RenderTexture target;
            public float aspect;
            public int cullingMask;
        }
        sealed class CanvasCaptureState
        {
            public Canvas canvas;
            public Camera camera;
            public RenderMode mode;
            public float planeDistance;
        }

        static FlowWaterValidation()
        {
            EditorApplication.update += UpdatePlayValidation;
            Application.logMessageReceived += OnRuntimeLog;
        }

        static string ProjectRoot { get { return Directory.GetParent(Application.dataPath).FullName; } }
        static string OutputRoot { get { return Path.Combine(ProjectRoot, "Validation"); } }

        public static void RunAssetSuite()
        {
            bool passed = false;
            SuppressBatchExit = true;
            try
            {
                RunAudit(); RunDataValidation(); FlowWaterMediaAudit.Run();
                passed = new[] { "audit.json", "data-validation.json", "media-validation.json" }
                    .All(file => JsonUtility.FromJson<SuiteResult>(File.ReadAllText(Path.Combine(OutputRoot, file))).passed);
            }
            finally { SuppressBatchExit = false; }
            Debug.Log("Complete asset/data/media validation suite: " + (passed ? "PASS" : "FAIL"));
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
        }
        [Serializable] sealed class SuiteResult { public bool passed; }

        [MenuItem("Tools/Flow Water/Validate Imported Project")]
        public static void RunAudit()
        {
            Report report = NewReport("asset-import-audit");
            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                AddCheck(report, "requested-editor-version", Application.unityVersion == "2022.3.28f1", Application.unityVersion);
                var metaGuids = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                int malformedMeta = 0, duplicateMeta = 0;
                foreach (string metaPath in Directory.GetFiles(Application.dataPath, "*.meta", SearchOption.AllDirectories))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(File.ReadAllText(metaPath), @"(?m)^guid:\s*(\S+)");
                    Guid parsed;
                    if (!match.Success || !Guid.TryParseExact(match.Groups[1].Value, "N", out parsed))
                    { malformedMeta++; report.failures.Add("Malformed asset GUID: " + metaPath); continue; }
                    string prior;
                    if (metaGuids.TryGetValue(match.Groups[1].Value, out prior))
                    { duplicateMeta++; report.failures.Add("Duplicate asset GUID: " + metaPath + " and " + prior); }
                    else metaGuids.Add(match.Groups[1].Value, metaPath);
                }
                AddCheck(report, "all-asset-meta-guids-valid", malformedMeta == 0, metaGuids.Count + " metadata files; malformed=" + malformedMeta);
                AddCheck(report, "all-asset-meta-guids-unique", duplicateMeta == 0, "duplicates=" + duplicateMeta);
                EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
                report.sceneCount = buildScenes.Length;
                AddCheck(report, "build-scene-count", buildScenes.Length == ExpectedScenes.Length,
                    buildScenes.Length + " scenes; expected " + ExpectedScenes.Length);
                for (int i = 0; i < ExpectedScenes.Length; i++)
                {
                    string path = ExpectedScenes[i];
                    bool exists = AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null;
                    AddCheck(report, "scene-exists:" + path, exists, path);
                    bool configured = i < buildScenes.Length && buildScenes[i].path == path && buildScenes[i].enabled;
                    AddCheck(report, "build-scene-order:" + i, configured, path);
                    if (!exists) continue;
                    if (i < buildScenes.Length)
                        AddCheck(report, "build-scene-guid:" + i,
                            buildScenes[i].guid.ToString() == AssetDatabase.AssetPathToGUID(path), path);
                    Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    AssetResult entry = InspectHierarchy(path, scene.GetRootGameObjects());
                    report.scenes.Add(entry);
                    AddHierarchyChecks(report, entry);
                }
                foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null) { report.failures.Add("Prefab could not load: " + path); continue; }
                    AssetResult entry = InspectHierarchy(path, new[] { prefab });
                    report.prefabs.Add(entry);
                    AddHierarchyChecks(report, entry);
                }
                report.prefabCount = report.prefabs.Count;
                AddCheck(report, "recovered-prefab-count", report.prefabCount >= 529,
                    report.prefabCount + " loaded; original export contains 529");
                foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    report.materialCount++;
                    if (material == null || material.shader == null || material.shader.name == "Hidden/InternalErrorShader")
                        report.failures.Add("Material has an unavailable shader: " + path);
                    else if (ShaderUtil.ShaderHasError(material.shader))
                        report.failures.Add("Material shader has compiler errors: " + path + " -> " + material.shader.name);
                }
                foreach (string guid in AssetDatabase.FindAssets("t:Shader", new[] { "Assets" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                    report.shaderCount++;
                    AddCheck(report, "shader-compiles:" + path, shader != null && !ShaderUtil.ShaderHasError(shader),
                        shader == null ? "Could not load shader" : shader.name);
                    if (File.Exists(path) && File.ReadAllText(path).Contains("DummyShaderTextExporter"))
                        report.warnings.Add("Exported placeholder shader remains: " + path);
                }
                report.warnings.Add("Missing mesh counts are informational: some original MeshFilters are assigned at runtime.");
                report.warnings.Add("Advertising, purchasing, analytics, original water simulation, and all advanced mechanism encodings require separate fidelity tests.");
            }
            catch (Exception exception) { report.failures.Add(exception.ToString()); }
            finally
            {
                try { EditorSceneManager.RestoreSceneManagerSetup(setup); }
                catch (Exception exception) { report.warnings.Add("Scene setup restore: " + exception.Message); }
                FinishReport(report, "audit.json");
            }
            if (Application.isBatchMode && !SuppressBatchExit) EditorApplication.Exit(report.passed ? 0 : 1);
        }

        static AssetResult InspectHierarchy(string path, GameObject[] roots)
        {
            AssetResult result = new AssetResult { path = path };
            foreach (GameObject root in roots)
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    result.gameObjects++;
                    result.missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
                    foreach (Renderer renderer in child.GetComponents<Renderer>())
                    {
                        result.renderers++;
                        foreach (Material material in renderer.sharedMaterials) if (material == null) result.missingMaterialSlots++;
                    }
                    foreach (MeshFilter filter in child.GetComponents<MeshFilter>()) if (filter.sharedMesh == null) result.missingMeshes++;
                    foreach (SkinnedMeshRenderer renderer in child.GetComponents<SkinnedMeshRenderer>()) if (renderer.sharedMesh == null) result.missingMeshes++;
                }
            }
            return result;
        }

        static void AddHierarchyChecks(Report report, AssetResult entry)
        {
            AddCheck(report, "no-missing-scripts:" + entry.path, entry.missingScripts == 0, entry.missingScripts.ToString());
            AddCheck(report, "no-missing-materials:" + entry.path, entry.missingMaterialSlots == 0, entry.missingMaterialSlots.ToString());
            if (entry.missingMeshes > 0) report.warnings.Add(entry.path + ": " + entry.missingMeshes + " empty mesh references");
        }

        [MenuItem("Tools/Flow Water/Validate All Recovered Level Data")]
        public static void RunDataValidation()
        {
            Report report = NewReport("all-level-data-replay-validation");
            report.scope = "Validates all recovered JSON, alias/prefab references, and the runtime's stored-layout/stored-step replay. " +
                "Replay success means geometric replay only; original water behavior, fixed-state semantics, rotation and mechanisms are not established.";
            try
            {
                RecoveryCatalogBuilder.Build();
                RecoveryCatalog catalog = AssetDatabase.LoadAssetAtPath<RecoveryCatalog>(RecoveryCatalogBuilder.CatalogPath);
                if (catalog == null) throw new InvalidDataException("The runtime recovery catalog could not load.");
                report.layoutCount = catalog.levels.Length;
                report.solutionCount = catalog.levels.Count(level => level.solution != null);
                report.prefabCount = catalog.prefabs.Length;
                AddCheck(report, "all-1350-layouts", report.layoutCount == 1350, report.layoutCount.ToString());
                AddCheck(report, "all-1350-solutions", report.solutionCount == 1350, report.solutionCount.ToString());
                AddCheck(report, "all-529-prefabs", report.prefabCount == 529, report.prefabCount.ToString());
                RecoveryAliasFile aliasFile = JsonUtility.FromJson<RecoveryAliasFile>(catalog.aliases.text);
                if (aliasFile == null || aliasFile.aliases == null) throw new InvalidDataException("Alias JSON did not parse.");
                report.aliasCount = aliasFile.aliases.Count;
                AddCheck(report, "all-498-piece-aliases", report.aliasCount == 498, report.aliasCount.ToString());
                var aliases = new Dictionary<string, RecoveryAlias>(StringComparer.Ordinal);
                var prefabs = catalog.prefabs.GroupBy(item => item.name, StringComparer.Ordinal)
                    .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
                RecoveryPrefabEntry[] originalBlock;
                bool blockResolved = prefabs.TryGetValue("BloqueFijo", out originalBlock) && originalBlock.Length == 1 &&
                    originalBlock[0].prefab.GetComponent<ControlFijo>() != null &&
                    AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(originalBlock[0].prefab)) == "f527ee718f0ab1c4389bced3619d375c";
                AddCheck(report, "original-playable-BloqueFijo-reference", blockResolved,
                    "SaveLoadManager.prefabPiezas[79], distinct from lowercase decorative bloqueFijo.");
                foreach (RecoveryAlias alias in aliasFile.aliases)
                {
                    if (aliases.ContainsKey(alias.originalName)) report.failures.Add("Duplicate original alias: " + alias.originalName);
                    else aliases.Add(alias.originalName, alias);
                    RecoveryPrefabEntry[] matches;
                    bool resolved = prefabs.TryGetValue(alias.basePrefab, out matches) && matches.Length == 1 && matches[0].prefab != null;
                    AddCheck(report, "alias-prefab:" + alias.originalName, resolved, alias.basePrefab);
                }
                report.configurations = new ConfigurationCounts();
                var configurations = new HashSet<string>(StringComparer.Ordinal);
                var complexCounts = new Dictionary<int, int>();
                foreach (RecoveryLevelEntry entry in catalog.levels)
                {
                    try
                    {
                        if (entry.layout == null || entry.solution == null) throw new InvalidDataException("Missing gm/so reference.");
                        RecoveryEnvelope layoutEnvelope = JsonUtility.FromJson<RecoveryEnvelope>(entry.layout.text);
                        RecoveryEnvelope solutionEnvelope = JsonUtility.FromJson<RecoveryEnvelope>(entry.solution.text);
                        RecoverySave layout = layoutEnvelope == null ? null : layoutEnvelope.data;
                        RecoverySave solution = solutionEnvelope == null ? null : solutionEnvelope.data;
                        if (layout == null || !layout.IsValidLayout()) throw new InvalidDataException("Invalid Save layout schema or list lengths.");
                        if (solution == null || (!solution.IsValidLayout() && !solution.IsValidSteps()))
                            throw new InvalidDataException("Invalid Save/Save1 solution schema or list lengths.");
                        AddCheck(report, "gm" + entry.assetId + ":source-asset-pair",
                            layoutEnvelope.source_asset == "gm" + entry.assetId && solutionEnvelope.source_asset == "so" + entry.assetId,
                            layoutEnvelope.source_asset + " / " + solutionEnvelope.source_asset);
                        int complex = 0;
                        for (int i = 0; i < layout.pieza.Count; i++)
                        {
                            string piece = layout.pieza[i];
                            if (!aliases.ContainsKey(piece)) throw new InvalidDataException("Unmapped original piece: " + piece);
                            Vector3 position = layout.Position(i);
                            if (!Finite(position.x) || !Finite(position.y) || !Finite(position.z))
                                throw new InvalidDataException("Non-finite saved coordinate at piece " + i);
                            string value = layout.fijo[i] ?? "";
                            configurations.Add(value);
                            report.configurations.totalOccurrences++;
                            int number;
                            if (value == "N") report.configurations.movableN++;
                            else if (value == "Y") report.configurations.fixedY++;
                            else if (int.TryParse(value, out number)) report.configurations.numericConfiguration++;
                            else { report.configurations.complexConfiguration++; complex++; }
                        }
                        if (solution.IsValidLayout())
                            for (int i = 0; i < solution.pieza.Count; i++)
                            {
                                if (!aliases.ContainsKey(solution.pieza[i]))
                                    throw new InvalidDataException("Unmapped solution piece: " + solution.pieza[i]);
                                Vector3 position = solution.Position(i);
                                if (!Finite(position.x) || !Finite(position.y) || !Finite(position.z))
                                    throw new InvalidDataException("Non-finite solution coordinate at piece " + i);
                            }
                        else if (solution.excep == null || solution.excep.Count != solution.move.Count)
                            throw new InvalidDataException("Save1 exception marker list does not match step count.");
                        complexCounts[entry.assetId] = complex;
                        if (complex > 0) report.configurations.levelsWithComplexConfiguration++;
                        AddCheck(report, "gm" + entry.assetId + ":json-and-prefab-references", true,
                            layout.pieza.Count + " pieces; " + solution.__type__ + "; " + complex + " complex configuration strings");
                    }
                    catch (Exception exception) { report.failures.Add("gm" + entry.assetId + ": " + exception.Message); }
                }
                report.configurations.distinctValues = configurations.Count;
                Type runtime = FindType(RuntimeTypeName);
                MethodInfo validate = runtime == null ? null : runtime.GetMethod("ValidateAllData", BindingFlags.Public | BindingFlags.Static);
                if (validate == null) throw new MissingMethodException(RuntimeTypeName, "ValidateAllData");
                object replayResult = validate.Invoke(null, null);
                report.dataReplay = JsonUtility.FromJson<DataReplayReport>(JsonUtility.ToJson(replayResult));
                if (report.dataReplay == null) throw new InvalidDataException("Runtime all-data replay report was empty.");
                foreach (DataReplayEntry entry in report.dataReplay.entries)
                {
                    int count;
                    if (complexCounts.TryGetValue(entry.assetId, out count)) entry.complexConfigurationCount = count;
                    entry.originalMechanismsVerified = false;
                    if (entry.classification == "failure") report.failures.Add("Replay gm" + entry.assetId + ": " + entry.reason);
                    if (entry.classification == "unsupported") report.warnings.Add("Replay gm" + entry.assetId + " unsupported: " + entry.reason);
                }
                foreach (IGrouping<string, DataReplayEntry> group in report.dataReplay.entries.GroupBy(entry => entry.solutionKind))
                    report.replayBySolutionKind.Add(new ReplayKindCounts { solutionKind = group.Key, total = group.Count(),
                        success = group.Count(entry => entry.classification == "success"), unsupported = group.Count(entry => entry.classification == "unsupported"),
                        failure = group.Count(entry => entry.classification == "failure") });
                AddCheck(report, "all-layouts-replayed-or-explicitly-classified", report.dataReplay.total == 1350 &&
                    report.dataReplay.entries.Count == 1350 && report.dataReplay.success + report.dataReplay.unsupported + report.dataReplay.failure == 1350,
                    "success=" + report.dataReplay.success + "; unsupported=" + report.dataReplay.unsupported + "; failure=" + report.dataReplay.failure);
                AddCheck(report, "no-unclassified-replay-failures", report.dataReplay.failure == 0, report.dataReplay.failure.ToString());
                if (report.dataReplay.unsupported > 0)
                    report.warnings.Add("Unsupported source records are listed separately and are not counted as successful geometric replay.");
                report.warnings.Add("This static report does not execute mechanisms. Actual water, spring, input and whole-content results are recorded by their separate Play-mode validation suites.");
            }
            catch (Exception exception) { report.failures.Add(Unwrap(exception).ToString()); }
            FinishReport(report, "data-validation.json");
            if (Application.isBatchMode && !SuppressBatchExit) EditorApplication.Exit(report.passed ? 0 : 1);
        }

        static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }

        [MenuItem("Tools/Flow Water/Validate Reconstructed Play Mode")]
        public static void RunPlayValidation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Start validation from Edit Mode.");
            Directory.CreateDirectory(Path.Combine(OutputRoot, "screenshots"));
            EditorSceneManager.OpenScene(ExpectedScenes[0], OpenSceneMode.Single);
            PlaySession session = new PlaySession {
                active = true, phase = 0, startedTicks = DateTime.UtcNow.Ticks,
                report = NewReport("reconstructed-play-mode-validation")
            };
            session.report.warnings.Add("This exercises newly reconstructed runtime APIs and original scene UI wiring, not recovered original C# method bodies.");
            SaveSession(session);
            EditorApplication.EnterPlaymode();
        }

        static void UpdatePlayValidation()
        {
            if (ticking) return;
            PlaySession session = ReadSession();
            if (session == null || !session.active || EditorApplication.isCompiling) return;
            ticking = true;
            try
            {
                if ((DateTime.UtcNow.Ticks - session.startedTicks) / (double)TimeSpan.TicksPerSecond > 240)
                    throw new TimeoutException("Play validation exceeded 240 seconds.");
                if (!EditorApplication.isPlaying) return;
                if (EditorApplication.timeSinceStartup < session.nextUpdate) return;
                if (!string.IsNullOrEmpty(session.pendingScreenshot))
                {
                    if (!File.Exists(session.pendingScreenshot))
                    {
                        if (EditorApplication.timeSinceStartup > session.screenshotDeadline)
                            throw new IOException("Screenshot was not written: " + session.pendingScreenshot);
                        return;
                    }
                    session.report.screenshots.Add(session.pendingScreenshot);
                    session.pendingScreenshot = null;
                }
                Tick(session);
                if (session.active) SaveSession(session);
            }
            catch (Exception exception)
            {
                session.report.failures.Add(Unwrap(exception).ToString());
                CompletePlayValidation(session);
            }
            finally { ticking = false; }
        }

        static void Tick(PlaySession session)
        {
            if (session.phase == 0)
            {
                AddCheck(session.report, "home-scene", SceneManager.GetActiveScene().path == ExpectedScenes[0], SceneManager.GetActiveScene().path);
                AddCheck(session.report, "requested-editor-version", Application.unityVersion == "2022.3.28f1", Application.unityVersion);
                Capture(session, "00-home.png");
                session.phase = 1;
                Delay(session, 1);
                return;
            }
            if (session.phase == 1)
            {
                InvokeOriginalTrigger(session.report, "home-play-trigger", "Play");
                session.phase = 2;
                Delay(session, 1.5);
                return;
            }
            if (session.phase == 2)
            {
                string scene = SceneManager.GetActiveScene().name;
                AddCheck(session.report, "home-to-level-selection", scene == "SelLevel0" || scene == "SelLevel1", scene);
                Capture(session, "01-level-selection.png");
                session.phase = 3;
                Delay(session, 1);
                return;
            }
            if (session.phase == 3)
            {
                InvokeOriginalTrigger(session.report, "basic-pack-trigger", "Basic1 (1)");
                session.phase = 12;
                Delay(session, 1.5);
                return;
            }
            if (session.phase == 12)
            {
                AddCheck(session.report, "pack-to-level-grid", SceneManager.GetActiveScene().name == "SelLevel1", SceneManager.GetActiveScene().name);
                Capture(session, "02-original-level-grid.png");
                session.phase = 13;
                Delay(session, 1);
                return;
            }
            if (session.phase == 13)
            {
                InvokeOriginalTrigger(session.report, "first-level-trigger", "cuadro1", "/Panel1/Panel1a/cuadro1");
                session.phase = 14;
                Delay(session, 1.5);
                return;
            }
            if (session.phase == 14)
            {
                object enteredGame = RuntimeGame();
                AddCheck(session.report, "original-level-selection-loads-layout", Get<int>(enteredGame, "CurrentLevelId") == 401,
                    "Basic pack 9 / level 1 maps to gm" + Get<int>(enteredGame, "CurrentLevelId") + " in the reconstruction");
                AddCheck(session.report, "selected-layout-has-geometry", Get<int>(enteredGame, "PieceCount") == ExpectedPieces(401),
                    Get<int>(enteredGame, "PieceCount").ToString());
                AddCheck(session.report, "original-level-selection-shows-game-hud", HasOriginalPresentation(),
                    "Original gameplay scene, perspective camera, ground and controls are active");
                Capture(session, "03-original-ui-enters-gm401.png");
                session.phase = 4;
                Delay(session, 1);
                return;
            }
            int level = PlayLevelIds[session.levelIndex];
            object game = RuntimeGame();
            switch (session.phase)
            {
                case 4:
                    Invoke(RuntimeUI(), "StartGame", level);
                    session.phase = 5;
                    Delay(session, 1.3);
                    break;
                case 5:
                    int expected = ExpectedPieces(level);
                    AddCheck(session.report, "gm" + level + ":level-id", Get<int>(game, "CurrentLevelId") == level, Get<int>(game, "CurrentLevelId").ToString());
                    AddCheck(session.report, "gm" + level + ":piece-count", Get<int>(game, "PieceCount") == expected, Get<int>(game, "PieceCount") + " / " + expected);
                    AddCheck(session.report, "gm" + level + ":resolved-prefabs", Get<int>(game, "UnresolvedPrefabCount") == 0, Get<int>(game, "UnresolvedPrefabCount").ToString());
                    AddCheck(session.report, "gm" + level + ":solution-available", Get<bool>(game, "HasSolution"), "Original matching so data is available");
                    AddCheck(session.report, "gm" + level + ":restart-move-count", Get<int>(game, "MoveCount") == 0, Get<int>(game, "MoveCount").ToString());
                    AddCheck(session.report, "gm" + level + ":game-hud-visible", HasOriginalPresentation(), "Original scene presentation and controls");
                    if (level == 1) ValidatePlayerMovement(session.report, game);
                    WaterSnapshot startWater = ReadWater(level, "start");
                    session.report.waterSnapshots.Add(startWater);
                    if (level == 1)
                    {
                        session.initialWetPieces = startWater.wetPieceCount;
                        AddCheck(session.report, "gm1:initial-partial-water", startWater.wetPieceCount > 0 && startWater.basinCount == 1 && startWater.filledBasinCount == 0,
                            "Wet pieces=" + startWater.wetPieceCount + "; basins=" + startWater.basinCount + "; filled=" + startWater.filledBasinCount);
                    }
                    Capture(session, "gm" + level + "-start.png");
                    session.phase = 6;
                    Delay(session, 1);
                    break;
                case 6:
                    Invoke(game, "ApplyHint");
                    session.phase = 7;
                    Delay(session, 0.8);
                    break;
                case 7:
                    session.hintMoves = Get<int>(game, "MoveCount");
                    AddCheck(session.report, "gm" + level + ":hint", session.hintMoves > 0, "Moves=" + session.hintMoves + "; " + Get<string>(game, "Status"));
                    Invoke(game, "Undo");
                    session.phase = 8;
                    Delay(session, 0.5);
                    break;
                case 8:
                    AddCheck(session.report, "gm" + level + ":undo", Get<int>(game, "MoveCount") < session.hintMoves,
                        "Moves after undo=" + Get<int>(game, "MoveCount"));
                    Invoke(game, "ApplyHint");
                    Invoke(game, "RestartLevel");
                    session.phase = 9;
                    Delay(session, 0.8);
                    break;
                case 9:
                    AddCheck(session.report, "gm" + level + ":restart", Get<int>(game, "MoveCount") == 0 && Get<int>(game, "PieceCount") == ExpectedPieces(level),
                        "Moves=" + Get<int>(game, "MoveCount") + "; pieces=" + Get<int>(game, "PieceCount"));
                    Invoke(game, "Solve");
                    session.phase = 10;
                    Delay(session, 1);
                    break;
                case 10:
                    AddCheck(session.report, "gm" + level + ":assisted-solution-completion", Get<bool>(game, "IsComplete"), Get<string>(game, "Status"));
                    WaterSnapshot solvedWater = ReadWater(level, "assisted-solved");
                    session.report.waterSnapshots.Add(solvedWater);
                    if (level == 1)
                        AddCheck(session.report, "gm1:solved-water-connects-basin", solvedWater.basinCount == 1 && solvedWater.filledBasinCount == 1 && solvedWater.wetPieceCount > session.initialWetPieces,
                            "Wet pieces=" + solvedWater.wetPieceCount + "; initial=" + session.initialWetPieces + "; basins=" + solvedWater.basinCount + "; filled=" + solvedWater.filledBasinCount);
                    Capture(session, "gm" + level + "-assisted-complete.png");
                    session.phase = 11;
                    Delay(session, 1);
                    break;
                case 11:
                    session.levelIndex++;
                    if (session.levelIndex == PlayLevelIds.Length) CompletePlayValidation(session);
                    else { session.phase = 4; Delay(session, 0.4); }
                    break;
                default: throw new InvalidOperationException("Unknown validation phase " + session.phase);
            }
        }

        public static bool HasOriginalPresentation()
        {
            RecoveredFlowGame game = RecoveredFlowGame.Instance;
            if (!OriginalScenePresentation.IsGameplayScene(SceneManager.GetActiveScene()) || game == null ||
                game.GameCamera == null || game.GameCamera.orthographic || GameObject.Find("RecoveryGameBar") != null ||
                GameObject.Find("RecoveryGameFooter") != null || GameObject.Find("Recovery Grid 0,0") != null) return false;
            SaveLoadManager loader = UnityEngine.Object.FindObjectOfType<SaveLoadManager>();
            return loader != null && loader.MenuButton != null && loader.MenuButton.activeInHierarchy &&
                loader.GetComponentInParent<Canvas>() == null;
        }

        public static void SaveVisibleFrame(string absolutePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            var session = new PlaySession { report = NewReport("frame-capture") };
            Capture(session, absolutePath);
        }

        static object RuntimeGame()
        {
            Type type = FindType(RuntimeTypeName);
            if (type == null) throw new MissingMemberException("Reconstructed runtime type is unavailable: " + RuntimeTypeName);
            MethodInfo create = type.GetMethod("EnsureCreated", BindingFlags.Public | BindingFlags.Static);
            if (create == null) throw new MissingMethodException(RuntimeTypeName, "EnsureCreated");
            return create.Invoke(null, null);
        }
        static object RuntimeUI()
        {
            Type type = FindType("FlowWaterRecoveryUI");
            if (type == null) throw new MissingMemberException("Recovered UI type is unavailable.");
            PropertyInfo instance = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            object ui = instance == null ? null : instance.GetValue(null, null);
            if (ui == null) ui = UnityEngine.Object.FindObjectsOfType(type).FirstOrDefault();
            if (ui == null) throw new MissingMemberException("No active reconstructed UI instance.");
            return ui;
        }
        static void ValidatePlayerMovement(Report report, object game)
        {
            Transform root = Get<Transform>(game, "PieceRoot");
            RecoveryPiece[] pieces = root.GetComponentsInChildren<RecoveryPiece>(true);
            RecoveryPiece moving = pieces.First(piece => piece.Index == 3);
            RecoveryPiece fixedPiece = pieces.First(piece => piece.Index == 0);
            RecoveryPiece occupant = pieces.First(piece => piece.Index == 4);
            Vector3 original = moving.transform.position;
            Vector3 fixedOriginal = fixedPiece.transform.position;
            Vector3 destination = new Vector3(20, original.y, 40);
            bool moved = (bool)InvokeResult(game, "TryMove", 3, destination);
            AddCheck(report, "gm1:player-legal-empty-cell-move", moved && Get<int>(game, "MoveCount") == 1 &&
                (moving.transform.position - destination).sqrMagnitude < .01f,
                "Accepted=" + moved + "; moves=" + Get<int>(game, "MoveCount") + "; position=" + moving.transform.position);
            Invoke(game, "Undo");
            AddCheck(report, "gm1:player-move-undo", Get<int>(game, "MoveCount") == 0 &&
                (moving.transform.position - original).sqrMagnitude < .01f,
                "Moves=" + Get<int>(game, "MoveCount") + "; restored position=" + moving.transform.position);
            bool movedFixed = (bool)InvokeResult(game, "TryMove", 0, destination);
            AddCheck(report, "gm1:fixed-piece-move-rejected", !movedFixed && Get<int>(game, "MoveCount") == 0 &&
                (fixedPiece.transform.position - fixedOriginal).sqrMagnitude < .01f, "Accepted=" + movedFixed);
            bool movedOccupied = (bool)InvokeResult(game, "TryMove", 3, occupant.transform.position);
            AddCheck(report, "gm1:occupied-cell-move-rejected", !movedOccupied && Get<int>(game, "MoveCount") == 0 &&
                (moving.transform.position - original).sqrMagnitude < .01f, "Accepted=" + movedOccupied);
            bool movedNaN = (bool)InvokeResult(game, "TryMove", 3, new Vector3(float.NaN, original.y, 40));
            AddCheck(report, "gm1:nonfinite-move-rejected", !movedNaN && Get<int>(game, "MoveCount") == 0 &&
                (moving.transform.position - original).sqrMagnitude < .01f, "Accepted=" + movedNaN);
        }
        static WaterSnapshot ReadWater(int level, string stage)
        {
            Type type = FindType("Recovered.FlowWater.RecoveredWaterNetwork");
            if (type == null) throw new MissingMemberException("The reconstructed basic water network is unavailable.");
            UnityEngine.Object network = UnityEngine.Object.FindObjectsOfType(type).FirstOrDefault();
            if (network == null) throw new MissingMemberException("No active reconstructed basic water network instance.");
            return new WaterSnapshot { level = level, stage = stage,
                wetPieceCount = Get<int>(network, "WetPieceCount"), basinCount = Get<int>(network, "BasinCount"),
                filledBasinCount = Get<int>(network, "FilledBasinCount") };
        }
        static Type FindType(string name)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(name, false);
                if (type != null) return type;
            }
            return null;
        }
        static T Get<T>(object target, string name)
        {
            Type type = target.GetType();
            PropertyInfo property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (property != null) return (T)property.GetValue(target, null);
            FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field != null) return (T)field.GetValue(target);
            throw new MissingMemberException(type.FullName, name);
        }
        static void Invoke(object target, string name, params object[] args)
        {
            InvokeResult(target, name, args);
        }
        static object InvokeResult(object target, string name, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
            if (method == null) throw new MissingMethodException(target.GetType().FullName, name);
            return method.Invoke(target, args);
        }
        static void InvokeOriginalTrigger(Report report, string check, string objectName, string hierarchySuffix = null)
        {
            Button button = UnityEngine.Object.FindObjectsOfType<Button>(true)
                .FirstOrDefault(t => t.name == objectName && t.isActiveAndEnabled && t.interactable &&
                    (hierarchySuffix == null || HierarchyPath(t.transform).EndsWith(hierarchySuffix, StringComparison.Ordinal)));
            if (button != null)
            {
                AddCheck(report, check, true, "Button: " + HierarchyPath(button.transform));
                button.onClick.Invoke();
                return;
            }
            EventTrigger trigger = UnityEngine.Object.FindObjectsOfType<EventTrigger>(true)
                .FirstOrDefault(t => t.name == objectName && t.isActiveAndEnabled &&
                    (hierarchySuffix == null || HierarchyPath(t.transform).EndsWith(hierarchySuffix, StringComparison.Ordinal)));
            AddCheck(report, check, trigger != null, trigger == null ? "Missing active original EventTrigger: " + objectName : HierarchyPath(trigger.transform));
            if (trigger == null) throw new MissingMemberException("Missing active original EventTrigger: " + objectName);
            EventTrigger.Entry entry = trigger.triggers.FirstOrDefault(t => t.eventID == EventTriggerType.PointerDown);
            if (entry == null) entry = trigger.triggers.FirstOrDefault(t => t.eventID == EventTriggerType.PointerClick);
            if (entry == null) throw new MissingMemberException("No PointerDown/PointerClick entry on " + objectName);
            entry.callback.Invoke(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });
        }
        static string HierarchyPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null) { transform = transform.parent; path = transform.name + "/" + path; }
            return path;
        }
        static int ExpectedPieces(int level)
        {
            string path = "Assets/RecoveredData/LevelData/01_Layouts/gm" + level + ".json";
            TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (json == null) throw new FileNotFoundException(path);
            LayoutEnvelope data = JsonUtility.FromJson<LayoutEnvelope>(json.text);
            if (data == null || data.data == null || data.data.pieza == null) throw new InvalidDataException(path);
            return data.data.pieza.Length;
        }
        static void Capture(PlaySession session, string file)
        {
            string path = Path.Combine(OutputRoot, "screenshots", file);
            const int width = 1280, height = 720;
            var cameraStates = new List<CameraCaptureState>();
            var canvasStates = new List<CanvasCaptureState>();
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture target = null;
            Texture2D image = null;
            GameObject temporaryCamera = null;
            GameObject uiCameraObject = null;
            var originalLayers = new Dictionary<GameObject, int>();
            try
            {
                Camera[] cameras = UnityEngine.Object.FindObjectsOfType<Camera>()
                    .Where(camera => camera.isActiveAndEnabled && camera.cameraType == CameraType.Game)
                    .OrderBy(camera => camera.depth).ToArray();
                if (cameras.Length == 0)
                {
                    temporaryCamera = new GameObject("Validation capture camera", typeof(Camera));
                    temporaryCamera.hideFlags = HideFlags.HideAndDontSave;
                    Camera camera = temporaryCamera.GetComponent<Camera>();
                    camera.clearFlags = CameraClearFlags.SolidColor;
                    camera.backgroundColor = new Color(.12f, .15f, .18f);
                    camera.orthographic = true;
                    cameras = new[] { camera };
                    session.report.warnings.Add("No active scene camera during " + file + "; a temporary camera captured visible UI.");
                }
                target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                target.Create();
                RenderTexture.active = target;
                GL.Clear(true, true, cameras[0].backgroundColor);
                foreach (Camera camera in cameras)
                {
                    cameraStates.Add(new CameraCaptureState { camera = camera, target = camera.targetTexture,
                        aspect = camera.aspect, cullingMask = camera.cullingMask });
                    camera.targetTexture = target;
                    camera.aspect = width / (float)height;
                }
                // The original game cameras may use a partial viewport. Overlay UI must retain
                // its full-screen coordinate system instead of shrinking into that viewport.
                var occupiedLayers = new HashSet<int>(UnityEngine.Object.FindObjectsOfType<Transform>()
                    .Select(item => item.gameObject.layer));
                int captureLayer = Enumerable.Range(8, 24).Reverse().First(layer => !occupiedLayers.Contains(layer));
                uiCameraObject = new GameObject("Validation full-screen UI capture camera", typeof(Camera));
                uiCameraObject.hideFlags = HideFlags.HideAndDontSave;
                Camera uiCamera = uiCameraObject.GetComponent<Camera>();
                uiCamera.enabled = false;
                uiCamera.targetTexture = target;
                uiCamera.rect = new Rect(0, 0, 1, 1);
                uiCamera.aspect = width / (float)height;
                uiCamera.clearFlags = CameraClearFlags.Depth;
                uiCamera.orthographic = true;
                uiCamera.orthographicSize = height * .5f;
                uiCamera.nearClipPlane = .1f;
                uiCamera.farClipPlane = 1000;
                uiCamera.cullingMask = 1 << captureLayer;
                foreach (Canvas canvas in UnityEngine.Object.FindObjectsOfType<Canvas>())
                {
                    if (!canvas.isActiveAndEnabled || !canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                    canvasStates.Add(new CanvasCaptureState { canvas = canvas, camera = canvas.worldCamera,
                        mode = canvas.renderMode, planeDistance = canvas.planeDistance });
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = uiCamera;
                    canvas.planeDistance = 1;
                    foreach (Transform child in canvas.GetComponentsInChildren<Transform>(true))
                    {
                        if (!originalLayers.ContainsKey(child.gameObject)) originalLayers.Add(child.gameObject, child.gameObject.layer);
                        child.gameObject.layer = captureLayer;
                    }
                }
                Canvas.ForceUpdateCanvases();
                foreach (Camera camera in cameras)
                {
                    camera.cullingMask &= ~(1 << captureLayer);
                    camera.Render();
                }
                uiCamera.Render();
                RenderTexture.active = target;
                image = new Texture2D(width, height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                image.Apply(false, false);
                File.WriteAllBytes(path, image.EncodeToPNG());
                if (!File.Exists(path) || new FileInfo(path).Length < 100)
                    throw new IOException("Rendered validation screenshot is empty: " + path);
                session.report.screenshots.Add(path);
                session.pendingScreenshot = null;
            }
            finally
            {
                foreach (KeyValuePair<GameObject, int> state in originalLayers)
                    if (state.Key != null) state.Key.layer = state.Value;
                foreach (CanvasCaptureState state in canvasStates)
                    if (state.canvas != null)
                    {
                        state.canvas.renderMode = state.mode;
                        state.canvas.worldCamera = state.camera;
                        state.canvas.planeDistance = state.planeDistance;
                    }
                foreach (CameraCaptureState state in cameraStates)
                    if (state.camera != null)
                    {
                        state.camera.targetTexture = state.target;
                        state.camera.aspect = state.aspect;
                        state.camera.cullingMask = state.cullingMask;
                    }
                RenderTexture.active = previousActive;
                Canvas.ForceUpdateCanvases();
                if (image != null) UnityEngine.Object.DestroyImmediate(image);
                if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                if (temporaryCamera != null) UnityEngine.Object.DestroyImmediate(temporaryCamera);
                if (uiCameraObject != null) UnityEngine.Object.DestroyImmediate(uiCameraObject);
            }
        }
        static void Delay(PlaySession session, double seconds) { session.nextUpdate = EditorApplication.timeSinceStartup + seconds; }
        static Report NewReport(string kind)
        {
            return new Report { kind = kind, startedUtc = DateTime.UtcNow.ToString("o"), unityVersion = Application.unityVersion,
                originalGameplayFullyRestored = false };
        }
        static void AddCheck(Report report, string name, bool passed, string detail)
        {
            report.checks.Add(new Check { name = name, passed = passed, detail = detail });
            if (!passed) report.failures.Add(name + ": " + detail);
        }
        static void FinishReport(Report report, string file)
        {
            Directory.CreateDirectory(OutputRoot);
            report.finishedUtc = DateTime.UtcNow.ToString("o");
            report.passed = report.failures.Count == 0 && report.runtimeErrors.Count == 0;
            File.WriteAllText(Path.Combine(OutputRoot, file), JsonUtility.ToJson(report, true));
            Debug.Log("Flow Water " + report.kind + ": " + (report.passed ? "PASS" : "FAIL") + "; " +
                report.checks.Count + " checks, " + report.failures.Count + " failures, " + report.runtimeErrors.Count +
                " runtime errors. Report: " + Path.Combine(OutputRoot, file));
        }
        static PlaySession ReadSession()
        {
            string json = SessionState.GetString(SessionKey, "");
            return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<PlaySession>(json);
        }
        static void SaveSession(PlaySession session)
        {
            PlaySession previous = ReadSession();
            if (previous != null && previous.startedTicks == session.startedTicks && previous.report != null && session.report != null)
                foreach (string error in previous.report.runtimeErrors)
                    if (!session.report.runtimeErrors.Contains(error)) session.report.runtimeErrors.Add(error);
            SessionState.SetString(SessionKey, JsonUtility.ToJson(session));
        }
        static void OnRuntimeLog(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            PlaySession session = ReadSession();
            if (session == null || !session.active) return;
            string entry = type + ": " + message + "\n" + stack;
            if (!session.report.runtimeErrors.Contains(entry)) session.report.runtimeErrors.Add(entry);
            SaveSession(session);
        }
        static Exception Unwrap(Exception exception)
        {
            while (exception is TargetInvocationException && exception.InnerException != null) exception = exception.InnerException;
            return exception;
        }
        static void CompletePlayValidation(PlaySession session)
        {
            PlaySession latest = ReadSession();
            if (latest != null)
                foreach (string error in latest.report.runtimeErrors)
                    if (!session.report.runtimeErrors.Contains(error)) session.report.runtimeErrors.Add(error);
            session.active = false;
            SaveSession(session);
            FinishReport(session.report, "play-validation.json");
            if (Application.isBatchMode) EditorApplication.Exit(session.report.passed ? 0 : 1);
            else EditorApplication.ExitPlaymode();
        }
    }
}
#endif
