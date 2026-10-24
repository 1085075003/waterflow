using UnityEngine;
namespace Recovered.FlowWater
{
    public sealed class RecoveredNativeJetHandle : MonoBehaviour
    {
        internal RecoveredNativeJetCache owner;
        internal RecoveredNativeJetCache.Entry entry;
        public bool Born { get { return entry != null && entry.born; } }
        public int Slot { get { return entry == null ? 0 : entry.slot; } }
        public string Origin { get { return entry == null ? null : entry.origin; } }
        public GameObject Model { get { return entry == null ? null : entry.model; } }
    }
}
