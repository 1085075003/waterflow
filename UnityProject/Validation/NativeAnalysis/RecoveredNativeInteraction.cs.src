// Original selection: MoveBloque.ReturnClickedObject10306B4..1030928.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Recovered.FlowWater
{
    public static class RecoveredNativeInteraction
    {
        // Shared by native movement probes; the returned scope restores all
        // original/generic collider enabled states on disposal.
        public static IDisposable QueryOriginalColliders(RecoveredFlowGame game)
        { return new OriginalColliderQuery(game); }
        // ReturnClickedObject sets only the exact hit GameObject to IgnoreRaycast;
        // doEnd102ECFC restores that same object to the authored bloque layer.
        public static bool BeginSelection(Collider collider)
        {
            if (collider == null || collider.gameObject.layer != LayerMask.NameToLayer("bloque")) return false;
            collider.gameObject.layer = 2;
            Physics.SyncTransforms();
            return true;
        }

        public static void EndSelection(Collider collider)
        {
            if (collider == null) return;
            collider.gameObject.layer = LayerMask.NameToLayer("bloque");
            Physics.SyncTransforms();
        }

        // Stable landing guide from doProcessMove102FDB8..10301C4,
        // doCheckTarget1/2 and doEnd102ED60. The original moving up/over/down
        // route and temporary +.5/+1.7 visual guide lifts are separate animation.
        public static bool TryGetDropPoint(Camera camera, Vector3 screenPosition, RecoveredFlowGame game,
            out Vector3 destination, out RaycastHit hit)
        {
            destination = Vector3.zero; hit = new RaycastHit();
            if (camera == null || game == null || !game.IsLevelActive || game.IsSolutionPreview ||
                !camera.pixelRect.Contains(new Vector2(screenPosition.x, screenPosition.y))) return false;
            MoveBloque movement = OriginalMovement(camera);
            if (movement == null) return false;
            using (var query = new OriginalColliderQuery(game))
            {
                if (!Physics.Raycast(camera.ScreenPointToRay(screenPosition), out hit, Mathf.Infinity,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal)) return false;
                string tag = hit.collider.tag, name = hit.collider.name;
                if (!(tag.IndexOf("Map", StringComparison.Ordinal) >= 0 && tag != "Map1") &&
                    name.IndexOf("Origen", StringComparison.Ordinal) < 0) return false;
                Vector3 guide = hit.collider.transform.position;
                bool outside = guide.x < movement.minX2 || guide.x > movement.maxX2 ||
                    guide.z < movement.minZ2 || guide.z > movement.maxZ2;
                if (guide.x < movement.minX2) guide.x = movement.minX2;
                if (guide.z < movement.minZ2) guide.z = movement.minZ2;
                // Native uses literal 80 for both high-end corrections.
                if (guide.x > movement.maxX2) guide.x = 80;
                if (guide.z > movement.maxZ2) guide.z = 80;
                bool configured = false;
                foreach (RecoveryPiece actor in game.Pieces)
                    if (actor != null && actor.BasePrefab != null && actor.BasePrefab.IndexOf("ConfigLevel", StringComparison.Ordinal) >= 0)
                    { configured = true; break; }
                if (configured && outside) ResolveConfiguredColumn(ref guide, movement);
                else ResolveOrdinaryColumn(ref guide, movement);
                // A guide at/above 53 is only shown as the secondary guide by
                // the ordinary branch; it is not a new stable landing surface.
                if (hit.collider.gameObject.layer != LayerMask.NameToLayer("Suelo1") && guide.y >= 53) return false;
                destination = guide + Vector3.up * 4.199999809265137f;
                return true;
            }
        }

        static void ResolveOrdinaryColumn(ref Vector3 guide, MoveBloque movement)
        {
            // Seven queries and at most seven rises in the original unrolled
            // method. CuboResorte adds 1.2 for each matching overlap collider.
            for (int attempt = 0; attempt < 7; attempt++)
            {
                bool occupied = false, spring = false;
                foreach (Collider collider in Physics.OverlapSphere(guide + Vector3.up * movement.subeRayo,
                    movement.sphereRadius, movement.mascaraSuelo.value, QueryTriggerInteraction.UseGlobal))
                {
                    string name = collider.name;
                    if (name.IndexOf("OrigenCentro", StringComparison.Ordinal) < 0 && name.IndexOf("Origen6", StringComparison.Ordinal) < 0) occupied = true;
                    if (name.IndexOf("CuboResorte", StringComparison.Ordinal) >= 0)
                    { spring = true; guide.y += 1.2000000476837158f; }
                }
                if (spring || !occupied) return;
                guide.y += movement.arribay;
            }
        }

        static void ResolveConfiguredColumn(ref Vector3 guide, MoveBloque movement)
        {
            guide.y = -30.649999618530273f; bool encountered = false;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                guide.y += movement.arribay;
                bool occupied = false, spring = false;
                foreach (Collider collider in Physics.OverlapSphere(guide + Vector3.up * movement.subeRayo,
                    movement.sphereRadius, movement.mascaraSuelo2.value, QueryTriggerInteraction.UseGlobal))
                {
                    if (collider.gameObject.layer == LayerMask.NameToLayer("Suelo"))
                    { if (collider.CompareTag("Map1")) occupied = true; }
                    else if (collider.name.IndexOf("Origen6", StringComparison.Ordinal) < 0) occupied = true;
                    if (collider.name.IndexOf("CuboResorte", StringComparison.Ordinal) >= 0) spring = true;
                }
                if (occupied) encountered = true;
                if (encountered && spring) { guide.y += 1.2000000476837158f; return; }
                if (encountered && !occupied) return;
            }
        }

        static MoveBloque OriginalMovement(Camera camera)
        {
            foreach (MoveBloque movement in UnityEngine.Object.FindObjectsOfType<MoveBloque>(true))
                if (movement != null && movement.gameObject.scene == camera.gameObject.scene) return movement;
            return null;
        }

        // This query preserves every collider's layer and restores all enabled
        // states before returning. The first physical hit determines selection.
        public static bool TryPick(Camera camera, Vector3 screenPosition, RecoveredFlowGame game,
            out RecoveryPiece piece, out RaycastHit hit)
        {
            piece = null; hit = new RaycastHit();
            if (camera == null || game == null || !game.IsLevelActive || game.IsSolutionPreview ||
                !camera.pixelRect.Contains(new Vector2(screenPosition.x, screenPosition.y))) return false;
            float distance = 2, radius = 1; int mask = 4096;
            // The original component remains serialized in each authored scene
            // although its missing exported Update method is disabled.
            MoveBloque movement = OriginalMovement(camera);
            if (movement != null) { distance = movement.distancia; radius = movement.sphereRadius; mask = movement.mascara.value; }
            using (var query = new OriginalColliderQuery(game))
            {
                Ray ray = camera.ScreenPointToRay(screenPosition);
                // Native uses unmasked Physics.Raycast and subsequently tests
                // only the nearest collider's layer. Fixed geometry can occlude
                // a movable block behind it.
                if (!Physics.Raycast(ray, out hit, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal)) return false;
                if (hit.collider.gameObject.layer != LayerMask.NameToLayer("bloque")) return false;
                Vector3 overhead = hit.collider.transform.position + Vector3.up * distance;
                if (Physics.CheckSphere(overhead, radius, mask, QueryTriggerInteraction.UseGlobal)) return false;
                RecoveryPiece candidate = hit.collider.GetComponentInParent<RecoveryPiece>();
                if (candidate == null || !candidate.CanMove || !candidate.gameObject.activeInHierarchy) return false;
                piece = candidate; return true;
            }
        }

        sealed class OriginalColliderQuery : IDisposable
        {
            struct Saved { public Collider collider; public bool enabled; }
            readonly List<Saved> changed = new List<Saved>();
            public OriginalColliderQuery(RecoveredFlowGame game)
            {
                foreach (RecoveryPiece piece in game.Pieces)
                {
                    if (piece == null) continue;
                    var original = new HashSet<Collider>();
                    if (piece.OriginalCaptureColliders != null)
                        foreach (Collider collider in piece.OriginalCaptureColliders)
                            if (collider != null) original.Add(collider);
                    foreach (Collider collider in piece.GetComponentsInChildren<Collider>(true))
                    {
                        if (collider == null) continue;
                        bool desired = collider.enabled;
                        if (collider.gameObject.name == "Recovery Pick Collider") desired = false;
                        else if (original.Contains(collider))
                        {
                            desired = true;
                            if (RecoveredNativeGeometry.TryGetOriginalColliderEnabled(collider, out bool cEnabled)) desired = cEnabled;
                            if (RecoveredNativeFixedGeometry.TryGetOriginalColliderEnabled(collider, out bool fixedEnabled)) desired = fixedEnabled;
                        }
                        if (desired == collider.enabled) continue;
                        changed.Add(new Saved { collider = collider, enabled = collider.enabled });
                        collider.enabled = desired;
                    }
                }
                Physics.SyncTransforms();
            }
            public void Dispose()
            {
                foreach (Saved saved in changed) if (saved.collider != null) saved.collider.enabled = saved.enabled;
                Physics.SyncTransforms();
            }
        }
    }
}
