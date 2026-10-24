#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Recovered.FlowWater.Editor
{
    // On-demand diagnostics for the two player-reported gm407 issues. Idle unless requested.
    [InitializeOnLoad]
    public static class FlowWaterUserIssueProbe
    {
        [Serializable] sealed class Request { public string id, action, label; public int level = 407; }
        [Serializable] sealed class ColliderInfo { public string name, layer, tag; public bool enabled; public Vector3 position, screen; }
        [Serializable] sealed class PieceInfo
        {
            public int index; public string prefab; public bool movable;
            public Vector3 position, screen;
            public List<ColliderInfo> colliders = new List<ColliderInfo>();
        }
        [Serializable] sealed class Report
        {
            public string id, capturedUtc, error, screenshot;
            public int level, moves, screenWidth, screenHeight;
            public bool playing, preview;
            public string expectedTitle;
            public string[] visibleTitles;
            public bool titleVerified;
            public List<PieceInfo> pieces = new List<PieceInfo>();
        }
        static string DirectoryPath { get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation", "UserReportedIssues"); } }
        static string loadingId;
        static double next, loadStarted;
        static FlowWaterUserIssueProbe() { EditorApplication.update += Update; }
        static void Update()
        {
            if (EditorApplication.isCompiling || EditorApplication.timeSinceStartup < next) return;
            next = EditorApplication.timeSinceStartup + .5;
            string path = Path.Combine(DirectoryPath, "request.json");
            if (!File.Exists(path)) return;
            Request request = null;
            try
            {
                request = JsonUtility.FromJson<Request>(File.ReadAllText(path));
                if (request == null || string.IsNullOrEmpty(request.id)) return;
                if (!EditorApplication.isPlaying)
                {
                    if (request.action == "load") EditorApplication.isPlaying = true;
                    return;
                }
                var game = RecoveredFlowGame.Instance;
                var ui = FlowWaterRecoveryUI.Instance;
                if (ui == null) return;
                if (request.action == "load")
                {
                    if (loadingId != request.id)
                    {
                        loadingId = request.id; loadStarted = EditorApplication.timeSinceStartup;
                        ui.StartGame(request.level); return;
                    }
                    if (EditorApplication.timeSinceStartup - loadStarted > 60) throw new TimeoutException("Reported level did not settle.");
                    var water = RecoveredWaterNetwork.Instance;
                    if (game == null || game.CurrentLevelId != request.level || !game.IsLevelActive ||
                        water == null || water.CurrentPieceRoot != game.PieceRoot || water.HasPendingPropagation ||
                        EditorApplication.timeSinceStartup - loadStarted < 2) return;
                }
                else if (request.action == "validate-fixed-fountains")
                {
                    if (game == null || !game.IsLevelActive) return;
                    game.StartCoroutine(FlowWaterFountainDragValidation.Run(game, (passed, reportPath) => Capture(request, game)));
                    File.Delete(path); return;
                }
                else if (request.action == "validate-inlet")
                {
                    if (game == null || !game.IsLevelActive) return;
                    game.StartCoroutine(FlowWaterVerticalInletValidation.Run(game, (passed, reportPath) => Capture(request, game)));
                    File.Delete(path); return;
                }
                else if (request.action != "capture") throw new ArgumentException("Unknown probe action.");
                if (game == null) return;
                Capture(request, game);
                File.Delete(path);
            }
            catch (Exception exception)
            {
                Directory.CreateDirectory(DirectoryPath);
                File.WriteAllText(Path.Combine(DirectoryPath, "response.json"), JsonUtility.ToJson(new Report {
                    id = request == null ? "" : request.id, capturedUtc = DateTime.UtcNow.ToString("o"), error = exception.ToString()
                }, true));
                File.Delete(path);
            }
        }
        static void Capture(Request request, RecoveredFlowGame game)
        {
            string label = string.IsNullOrEmpty(request.label) ? request.id : request.label;
            string output = Path.Combine(DirectoryPath, label);
            Directory.CreateDirectory(output);
            var camera = game.GameCamera;
            var report = new Report { id = request.id, capturedUtc = DateTime.UtcNow.ToString("o"),
                level = game.CurrentLevelId, moves = game.MoveCount, playing = EditorApplication.isPlaying,
                preview = game.IsSolutionPreview, screenWidth = Screen.width, screenHeight = Screen.height,
                screenshot = Path.Combine(output, "game.png") };
            report.expectedTitle = RecoveredNativeLocalization.Text("Leveltexto2$$ObtenerTexto", RecoveredNativeLocalization.Language) + " " + ((game.CurrentLevelId - 1) % 50 + 1);
            report.visibleTitles = UnityEngine.Object.FindObjectsOfType<Text>().Where(label => label.isActiveAndEnabled && label.color.a > .01f &&
                label.GetComponentsInParent<Transform>(true).Any(parent => parent.name == "Level")).Select(label => label.text).ToArray();
            report.titleVerified = report.visibleTitles.Length > 0 && report.visibleTitles.All(title => title == report.expectedTitle);
            foreach (var piece in game.Pieces)
            {
                var item = new PieceInfo { index = piece.Index, prefab = piece.BasePrefab, movable = piece.CanMove,
                    position = piece.transform.position, screen = camera.WorldToScreenPoint(piece.transform.position) };
                foreach (var collider in piece.GetComponentsInChildren<Collider>(true))
                    item.colliders.Add(new ColliderInfo { name = collider.name, layer = LayerMask.LayerToName(collider.gameObject.layer),
                        tag = collider.tag, enabled = collider.enabled, position = collider.transform.position,
                        screen = camera.WorldToScreenPoint(collider.bounds.center) });
                report.pieces.Add(item);
            }
            if (Screen.height > Screen.width) FlowWaterValidation.SaveVisibleFrame(report.screenshot, Screen.width, Screen.height);
            else FlowWaterValidation.SaveVisibleFrame(report.screenshot);
            FlowWaterVisualDiagnostics.Write(Path.Combine(output, "renderers.json"), camera, Screen.width, Screen.height);
            string json = JsonUtility.ToJson(report, true);
            File.WriteAllText(Path.Combine(output, "state.json"), json);
            File.WriteAllText(Path.Combine(DirectoryPath, "response.json"), json);
        }
    }
}
#endif
