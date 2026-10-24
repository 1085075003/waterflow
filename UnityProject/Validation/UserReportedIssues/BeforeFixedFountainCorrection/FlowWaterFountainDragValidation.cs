#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Recovered.FlowWater.Editor
{
    // Invoked by a focused editor probe after loading the user's level.
    // Every move goes through the production screen-ray and drag entry points.
    public static class FlowWaterFountainDragValidation
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [Serializable] public sealed class Check { public string name, detail; public bool passed; }
        [Serializable] public sealed class Report
        {
            public int level;
            public bool passed;
            public string error;
            public List<Check> checks = new List<Check>();
        }

        public static IEnumerator Run(RecoveredFlowGame game, Action<bool, string> completed = null)
        {
            if (game == null || !game.IsLevelActive) throw new InvalidOperationException("Load a playable level before validating fountain dragging.");
            var report = new Report { level = game.CurrentLevelId };
            string output = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation", "UserReportedIssues", "fountain-drag.json");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            object before = typeof(RecoveredFlowGame).GetMethod("CaptureSnapshot", Private).Invoke(game, null);
            Transform originalRoot = game.PieceRoot;
            IEnumerator exercise = Exercise(game, report);
            try
            {
                while (true)
                {
                    bool pending = false;
                    try { pending = exercise.MoveNext(); }
                    catch (Exception exception) { report.error = exception.ToString(); }
                    if (!pending || report.error != null) break;
                    yield return null;
                }
                report.passed = report.error == null && report.checks.Count > 0 && report.checks.All(c => c.passed);
            }
            finally
            {
                (exercise as IDisposable)?.Dispose();
                if (game != null && game.PieceRoot == originalRoot)
                {
                    Input(game, "CancelDrag");
                    typeof(RecoveredFlowGame).GetMethod("RestoreSnapshot", Private).Invoke(game, new[] { before });
                    Physics.SyncTransforms();
                }
                File.WriteAllText(output, JsonUtility.ToJson(report, true));
            }
            if (completed != null) completed(report.passed, output);
        }

        static IEnumerator Exercise(RecoveredFlowGame game, Report report)
        {
            RecoveryPiece[] fountains = game.Pieces.Where(p => p != null && p.CanMove && p.GetComponent<ControlPiezaCubetaB>() != null).ToArray();
            Require(report, "movable-basin-fountains-present", fountains.Length > 0, "count=" + fountains.Length);
            foreach (RecoveryPiece piece in fountains)
            {
                string name = "record" + piece.SourceRecordIndex;
                Collider body = piece.GetComponent<ControlPiezaCubetaB>().objeto.GetComponent<Collider>();
                int originalLayer = body == null ? -1 : body.gameObject.layer;
                Require(report, name + "-authored-body-layer", body != null && (originalLayer == LayerMask.NameToLayer("Suelo1") || originalLayer == LayerMask.NameToLayer("bloque")), piece.BasePrefab + ": " + LayerMask.LayerToName(originalLayer));
                Vector3 pointer;
                Require(report, name + "-nearest-original-body-pick", FindPointer(game, piece, body, out pointer), piece.BasePrefab + ": original collider, original camera");
                bool movable = piece.CanMove;
                try
                {
                    piece.CanMove = false;
                    RecoveryPiece picked; RaycastHit hit;
                    bool rejected = !RecoveredNativeInteraction.TryPick(game.GameCamera, pointer, game, out picked, out hit);
                    Input(game, "BeginDrag", pointer);
                    var activeMotion = game.GetComponent<RecoveredNativeDragMotion>();
                    Require(report, name + "-fixed-flag-blocks-selection", rejected && (activeMotion == null || !activeMotion.IsActive) && typeof(RecoveredFlowGame).GetField("dragging", Private).GetValue(game) == null && body.gameObject.layer == originalLayer, "Production BeginDrag rejects the same physical fountain when CanMove=false.");
                }
                finally { piece.CanMove = movable; }

                Vector3 start = piece.transform.position;
                int moves = game.MoveCount;
                Input(game, "BeginDrag", pointer);
                var motion = game.GetComponent<RecoveredNativeDragMotion>();
                Require(report, name + "-production-begin", motion != null && motion.IsActive && game.SelectedPieceIndex == piece.Index && body.gameObject.layer == 2, "Production BeginDrag selects the exact basin collider and temporarily ignores it.");
                Input(game, "CancelDrag");
                Require(report, name + "-cancel-restores-layer-and-pose", !motion.IsActive && body.gameObject.layer == originalLayer && Vector3.Distance(piece.transform.position, start) < .01f && game.MoveCount == moves, piece.transform.position.ToString("F3"));

                Input(game, "BeginDrag", pointer);
                Vector3 expected = Vector3.positiveInfinity;
                bool found = false;
                for (int x = 0; x <= 80 && !found; x += 20)
                    for (int z = 0; z <= 80 && !found; z += 20)
                    {
                        if (game.Pieces.Any(p => p != null && Mathf.Abs(p.transform.position.x - x) < .1f && Mathf.Abs(p.transform.position.z - z) < .1f)) continue;
                        Vector3 screen = game.GameCamera.WorldToScreenPoint(new Vector3(x, -.25f, z));
                        Vector3 landing; RaycastHit hit;
                        if (!RecoveredNativeInteraction.TryGetDropPoint(game.GameCamera, screen, game, out landing, out hit) || Vector3.Distance(landing, new Vector3(x, 3.95f, z)) > .04f) continue;
                        Input(game, "ContinueDrag", screen);
                        if (Vector3.Distance(motion.Target, landing) > .04f) continue;
                        expected = landing; found = true;
                    }
                Require(report, name + "-production-floor-target", found, "An empty original Map cell supplies the stable landing.");
                Input(game, "EndDrag");
                Require(report, name + "-release-preserves-original-layer", body.gameObject.layer == originalLayer, "Repeated EndSelection calls preserve the original " + LayerMask.LayerToName(originalLayer) + " layer.");
                float timeout = Time.realtimeSinceStartup + 15;
                while (motion.IsActive && Time.realtimeSinceStartup < timeout) yield return null;
                Require(report, name + "-actual-drag-commits", !motion.IsActive && Vector3.Distance(piece.transform.position, expected) < .04f && game.MoveCount == moves + 1, "actual=" + piece.transform.position.ToString("F3") + ", expected=" + expected.ToString("F3") + ", moves=" + game.MoveCount);
                Require(report, name + "-settled-original-layer", body.gameObject.layer == originalLayer, "Fountain preserves its original collision role after motion completes.");
                game.Undo();
                Require(report, name + "-undo-restores-fountain", Vector3.Distance(piece.transform.position, start) < .04f && game.MoveCount == moves && body.gameObject.layer == originalLayer, piece.transform.position.ToString("F3"));
                yield return null;
            }
        }

        static bool FindPointer(RecoveredFlowGame game, RecoveryPiece actor, Collider body, out Vector3 pointer)
        {
            pointer = Vector3.zero;
            BoxCollider box = body as BoxCollider;
            Vector3 center = box == null ? body.transform.position : body.transform.TransformPoint(box.center);
            for (int x = -6; x <= 6; x += 3)
                for (int z = -6; z <= 6; z += 3)
                {
                    Vector3 screen = game.GameCamera.WorldToScreenPoint(center + new Vector3(x, 0, z));
                    RecoveryPiece picked; RaycastHit hit;
                    if (RecoveredNativeInteraction.TryPick(game.GameCamera, screen, game, out picked, out hit) && picked == actor && hit.collider == body)
                    { pointer = screen; return true; }
                }
            return false;
        }
        static void Input(RecoveredFlowGame game, string method, Vector3 screen)
        { typeof(RecoveredFlowGame).GetMethod(method, Private).Invoke(game, new object[] { new Vector2(screen.x, screen.y) }); }
        static void Input(RecoveredFlowGame game, string method)
        { typeof(RecoveredFlowGame).GetMethod(method, Private).Invoke(game, null); }
        static void Require(Report report, string name, bool passed, string detail)
        {
            report.checks.Add(new Check { name = name, passed = passed, detail = detail });
            if (!passed) throw new InvalidOperationException(name + ": " + detail);
        }
    }
}
#endif
