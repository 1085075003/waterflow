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
    // Fixed fountains reject the production screen-ray, drag and instant-move
    // entry points; an ordinary pipe remains draggable. No moves are committed.
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
            if (game == null || !game.IsLevelActive) throw new InvalidOperationException("Load the user's playable level before validating fixed fountains.");
            var report = new Report { level = game.CurrentLevelId };
            string output = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation", "UserReportedIssues", "fountain-fixed-validation.json");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            object before = typeof(RecoveredFlowGame).GetMethod("CaptureSnapshot", Private).Invoke(game, null);
            int selectedBefore = game.SelectedPieceIndex;
            string statusBefore = game.Status;
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
                    typeof(RecoveredFlowGame).GetProperty("SelectedPieceIndex").GetSetMethod(true).Invoke(game, new object[] { selectedBefore });
                    typeof(RecoveredFlowGame).GetProperty("Status").GetSetMethod(true).Invoke(game, new object[] { statusBefore });
                    Physics.SyncTransforms();
                }
                File.WriteAllText(output, JsonUtility.ToJson(report, true));
            }
            if (completed != null) completed(report.passed, output);
        }

        static IEnumerator Exercise(RecoveredFlowGame game, Report report)
        {
            RecoveryPiece[] fountains = game.Pieces.Where(p => p != null && p.GetComponent<ControlPiezaCubetaB>() != null).ToArray();
            Require(report, "two-basin-fountains-present", fountains.Length == 2, "count=" + fountains.Length);
            foreach (RecoveryPiece piece in fountains)
            {
                string name = "record" + piece.SourceRecordIndex;
                var control = piece.GetComponent<ControlPiezaCubetaB>();
                Collider body = control.objeto == null ? null : control.objeto.GetComponent<Collider>();
                int originalLayer = body == null ? -1 : body.gameObject.layer;
                Require(report, name + "-fixed-configuration", !piece.CanMove, piece.BasePrefab + ": CanMove=" + piece.CanMove);
                Require(report, name + "-authored-body-present", body != null, piece.BasePrefab);
                Vector3 pointer;
                Require(report, name + "-nearest-original-body-ray", FindBodyPointer(game, body, out pointer), piece.BasePrefab + ": physical ray hits the authored basin collider.");
                Vector3 start = piece.transform.position;
                Quaternion rotation = piece.transform.rotation;
                int moves = game.MoveCount;
                RecoveryPiece picked; RaycastHit hit;
                bool rejected = !RecoveredNativeInteraction.TryPick(game.GameCamera, pointer, game, out picked, out hit);
                Require(report, name + "-try-pick-rejected", rejected && picked == null && hit.collider == body, "The nearest physical fountain is fixed and cannot be picked.");
                Input(game, "BeginDrag", pointer);
                var motion = game.GetComponent<RecoveredNativeDragMotion>();
                Require(report, name + "-begin-drag-rejected", (motion == null || !motion.IsActive) && typeof(RecoveredFlowGame).GetField("dragging", Private).GetValue(game) == null && body.gameObject.layer == originalLayer, "Production BeginDrag creates no active fountain drag.");

                bool selected = false;
                try
                {
                    selected = RecoveredNativeInteraction.BeginSelection(body);
                    Require(report, name + "-begin-selection-rejected", !selected && body.gameObject.layer == originalLayer, "Direct selection preserves the original collision layer.");
                }
                finally { if (selected) RecoveredNativeInteraction.EndSelection(body); }
                bool moved = game.TryMove(piece.Index, start + new Vector3(-20, 0, 0));
                Require(report, name + "-public-move-rejected", !moved, "TryMove refuses the fixed fountain.");
                Require(report, name + "-unchanged-pose-and-move-count", Vector3.Distance(piece.transform.position, start) < .001f && Quaternion.Angle(piece.transform.rotation, rotation) < .001f && game.MoveCount == moves && !piece.CanMove && body.gameObject.layer == originalLayer, "position=" + piece.transform.position.ToString("F3") + ", moves=" + game.MoveCount);
            }

            RecoveryPiece pipe = null;
            Collider pipeBody = null;
            Vector3 pipePointer = Vector3.zero;
            foreach (RecoveryPiece candidate in game.Pieces.Where(p => p != null && p.CanMove && p.GetComponent<ControlPieza>() != null))
            {
                if (FindPipePointer(game, candidate, out pipePointer, out pipeBody)) { pipe = candidate; break; }
            }
            Require(report, "ordinary-movable-pipe-present", pipe != null && pipeBody != null, "A real screen ray can pick an ordinary movable pipe.");
            int pipeLayer = pipeBody.gameObject.layer;
            Vector3 pipeStart = pipe.transform.position;
            Quaternion pipeRotation = pipe.transform.rotation;
            int pipeMoves = game.MoveCount;
            Input(game, "BeginDrag", pipePointer);
            var pipeMotion = game.GetComponent<RecoveredNativeDragMotion>();
            Require(report, "ordinary-pipe-begin-drag", pipeMotion != null && pipeMotion.IsActive && game.SelectedPieceIndex == pipe.Index && pipeBody.gameObject.layer == 2, pipe.BasePrefab + ": production BeginDrag still selects an ordinary pipe.");
            Input(game, "CancelDrag");
            Require(report, "ordinary-pipe-cancel-restores-state", !pipeMotion.IsActive && typeof(RecoveredFlowGame).GetField("dragging", Private).GetValue(game) == null && pipeBody.gameObject.layer == pipeLayer && Vector3.Distance(pipe.transform.position, pipeStart) < .001f && Quaternion.Angle(pipe.transform.rotation, pipeRotation) < .001f && game.MoveCount == pipeMoves && pipe.CanMove, "Cancel restores the ordinary pipe without committing a move.");
            yield break;
        }

        static bool FindBodyPointer(RecoveredFlowGame game, Collider body, out Vector3 pointer)
        {
            pointer = Vector3.zero;
            if (body == null) return false;
            using (RecoveredNativeInteraction.QueryOriginalColliders(game))
                foreach (Vector3 screen in PointerSamples(game.GameCamera, body))
                {
                    RaycastHit hit;
                    if (Physics.Raycast(game.GameCamera.ScreenPointToRay(screen), out hit, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal) && hit.collider == body)
                    { pointer = screen; return true; }
                }
            return false;
        }
        static bool FindPipePointer(RecoveredFlowGame game, RecoveryPiece actor, out Vector3 pointer, out Collider body)
        {
            pointer = Vector3.zero; body = null;
            using (RecoveredNativeInteraction.QueryOriginalColliders(game))
                foreach (Collider candidate in actor.GetComponentsInChildren<Collider>(true))
                {
                    if (!candidate.enabled || candidate.gameObject.layer != LayerMask.NameToLayer("bloque")) continue;
                    foreach (Vector3 screen in PointerSamples(game.GameCamera, candidate))
                    {
                        RecoveryPiece picked; RaycastHit hit;
                        if (RecoveredNativeInteraction.TryPick(game.GameCamera, screen, game, out picked, out hit) && picked == actor)
                        { pointer = screen; body = hit.collider; return true; }
                    }
                }
            return false;
        }
        static IEnumerable<Vector3> PointerSamples(Camera camera, Collider body)
        {
            Bounds bounds = body.bounds;
            for (int x = -2; x <= 2; x++)
                for (int z = -2; z <= 2; z++)
                {
                    Vector3 screen = camera.WorldToScreenPoint(bounds.center + new Vector3(bounds.extents.x * x * .35f, 0, bounds.extents.z * z * .35f));
                    if (screen.z > 0 && camera.pixelRect.Contains(new Vector2(screen.x, screen.y))) yield return screen;
                }
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
