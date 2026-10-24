using System;
using System.Collections.Generic;
using UnityEngine;

namespace Recovered.FlowWater
{
    // CascadaTest.Start FCFC4C and doControlAltura FD0354: original
    // floor probes use layer 9, radius 2, endpoint distance 20. The
    // same-height arch marker is open when the layer 15 probe at 14 is clear.
    public sealed class RecoveredWaterCollision : IDisposable
    {
        readonly Dictionary<Collider, RecoveryPiece> owners = new Dictionary<Collider, RecoveryPiece>();
        readonly List<Collider> enabled = new List<Collider>();
        readonly List<GameObject> waterMarkers = new List<GameObject>();
        public static readonly List<string> BlockedDiagnostics = new List<string>();
        static readonly float[] Depths = { 0, 7.92f, 16, 24, 32, 38 };

        public RecoveredWaterCollision(IEnumerable<RecoveryPiece> pieces)
        {
            BlockedDiagnostics.Clear();
            foreach (RecoveryPiece piece in pieces)
            {
                if (piece == null || piece.OriginalCaptureColliders == null) continue;
                foreach (Collider collider in piece.OriginalCaptureColliders)
                {
                    if (collider == null || !collider.gameObject.activeInHierarchy) continue;
                    bool originalEnabled;
                    if ((RecoveredNativeGeometry.TryGetOriginalColliderEnabled(collider, out originalEnabled) ||
                        RecoveredNativeFixedGeometry.TryGetOriginalColliderEnabled(collider, out originalEnabled)) && !originalEnabled) continue;
                    owners[collider] = piece;
                    if (!collider.enabled) { collider.enabled = true; enabled.Add(collider); }
                }
            }
            Physics.SyncTransforms();
        }

        public void AddWaterPrefabColliders(RecoveryPiece owner, Transform controller, GameObject prefab,
            Quaternion rotation, float localScale)
        {
            if (owner == null || controller == null || prefab == null) return;
            GameObject markerRoot = null;
            foreach (Collider original in prefab.GetComponentsInChildren<Collider>(true))
            {
                if (!original.enabled || (original.gameObject.layer != 9 && original.gameObject.layer != 15)) continue;
                bool active = true;
                for (Transform parent = original.transform; parent != null; parent = parent.parent)
                {
                    if (!parent.gameObject.activeSelf) active = false;
                    if (parent == prefab.transform) break;
                }
                if (!active) continue;
                if (markerRoot == null)
                {
                    markerRoot = new GameObject("Original water collision markers");
                    markerRoot.transform.SetParent(controller, false);
                    markerRoot.transform.position = controller.position; markerRoot.transform.rotation = rotation;
                    markerRoot.transform.localScale = Vector3.one * localScale;
                    waterMarkers.Add(markerRoot);
                }
                GameObject clone = UnityEngine.Object.Instantiate(original.gameObject, markerRoot.transform, false);
                clone.name = original.name;
                clone.transform.localPosition = prefab.transform.InverseTransformPoint(original.transform.position);
                clone.transform.localRotation = Quaternion.Inverse(prefab.transform.rotation) * original.transform.rotation;
                Vector3 scale = original.transform.lossyScale, basis = prefab.transform.lossyScale;
                clone.transform.localScale = new Vector3(scale.x / basis.x, scale.y / basis.y, scale.z / basis.z);
                foreach (Renderer renderer in clone.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
                foreach (MonoBehaviour script in clone.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
                foreach (Rigidbody body in clone.GetComponentsInChildren<Rigidbody>(true)) { body.isKinematic = true; body.useGravity = false; }
                foreach (Collider collider in clone.GetComponentsInChildren<Collider>(true)) owners[collider] = owner;
            }
        }
        public void SyncWaterPrefabColliders() { Physics.SyncTransforms(); }

        public bool BlocksFall(RecoveryPiece source, Vector3 sourcePoint,
            RecoveryPiece target, Vector3 targetPoint, Vector3 endpoint)
        {
            float drop = sourcePoint.y - targetPoint.y;
            Vector3 direction = endpoint - sourcePoint; direction.y = 0;
            Vector3 entryProbe = sourcePoint + direction.normalized * 14;
            for (int i = 0; i < Depths.Length; i++)
            {
                if (Depths[i] > drop + 2) break;
                Vector3 probe = new Vector3(endpoint.x, sourcePoint.y - Depths[i], endpoint.z);
                Collider[] contacts = Physics.OverlapSphere(probe, 2, 1 << 9, QueryTriggerInteraction.UseGlobal);
                bool hit = false, arch = false;
                var blockers = new List<string>();
                foreach (Collider collider in contacts)
                {
                    RecoveryPiece owner;
                    if (!owners.TryGetValue(collider, out owner) || owner == source || owner == target ||
                        owner.transform.position.y <= targetPoint.y + 3 || owner.transform.position.y > sourcePoint.y + .4f) continue;
                    hit = true;
                    blockers.Add(owner.Index + ":" + owner.BasePrefab + "/" + collider.name + "@" + collider.transform.position);
                    arch |= collider.name.Contains("esferacentroArco");
                }
                if (!hit) continue;
                if (i == 0 && arch)
                {
                    bool wall = false;
                    foreach (Collider collider in Physics.OverlapSphere(entryProbe, 2, 1 << 15, QueryTriggerInteraction.UseGlobal))
                    {
                        RecoveryPiece owner;
                        if (owners.TryGetValue(collider, out owner) && owner != source && owner != target) { wall = true; break; }
                    }
                    if (!wall) continue;
                }
                if (BlockedDiagnostics.Count < 200) BlockedDiagnostics.Add(source.Index + "->" + target.Index +
                    " at " + probe + " depth=" + Depths[i] + " blocked by " + String.Join(",", blockers.ToArray()));
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            foreach (GameObject marker in waterMarkers)
                if (marker != null) { marker.SetActive(false); UnityEngine.Object.Destroy(marker); }
            waterMarkers.Clear();
            foreach (Collider collider in enabled) if (collider != null) collider.enabled = false;
            enabled.Clear(); owners.Clear();
        }

        public bool BlocksUpward(RecoveryPiece source, RecoveryPiece target, Vector3 probe)
        {
            // CascadaTest1.CheckCollision FD3C94 and .ctor FD3DB0.
            foreach (Collider collider in Physics.OverlapSphere(probe, 2, 1 << 9, QueryTriggerInteraction.UseGlobal))
            {
                RecoveryPiece owner;
                if (owners.TryGetValue(collider, out owner) && owner != source && owner != target)
                {
                    if (target != null && owner.BasePrefab == target.BasePrefab &&
                        (owner.transform.position - target.transform.position).sqrMagnitude < .0001f &&
                        Quaternion.Angle(owner.transform.rotation, target.transform.rotation) < .1f) continue;
                    return true;
                }
            }
            return false;
        }
    }
}
