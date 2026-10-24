// Recovery implementation created for this export. This is not recovered original source.
using UnityEngine;

namespace Recovered.FlowWater
{
    /// <summary>Keeps decoded identity/configuration verbatim; fijo is not treated as a Boolean.</summary>
    public sealed class RecoveryPiece : MonoBehaviour
    {
        public int Index;
        public int SourceRecordIndex;
        public string OriginalName;
        public string BasePrefab;
        [TextArea] public string OriginalConfiguration;
        public int ColorVariant;
        public bool CanMove;
        public Vector3 InitialPosition;
        public Quaternion InitialRotation;
        public Vector3 GoalPosition;
        public bool HasGoal;
        [System.NonSerialized] public Collider[] OriginalCaptureColliders;
    }
}
