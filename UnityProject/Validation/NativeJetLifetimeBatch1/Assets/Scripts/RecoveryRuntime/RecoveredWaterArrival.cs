using System.Collections.Generic;
using UnityEngine;

namespace Recovered.FlowWater
{
    // FuenteTest3 invokes doInstanciarAgua1 after espera1 (side) or
    // espera2 (vertical), then removes the side arrival two seconds later.
    public sealed class RecoveredWaterArrival : MonoBehaviour
    {
        struct Pending { public GameObject target; public float time; public bool active; }
        readonly List<Pending> pending = new List<Pending>();
        public static void ActivateAfter(Transform owner, GameObject target, float seconds)
        { SetActiveAfter(owner, target, true, seconds); }
        public static void SetActiveAfter(Transform owner, GameObject target, bool active, float seconds)
        {
            if (owner == null || target == null) return;
            if (seconds <= 0) { target.SetActive(active); if (active) RecoveredNativeJetCache.NotifyActivation(target); return; }
            var timer = owner.GetComponent<RecoveredWaterArrival>();
            if (timer == null) timer = owner.gameObject.AddComponent<RecoveredWaterArrival>();
            if (active) target.SetActive(false);
            timer.pending.Add(new Pending { target = target, time = Time.time + seconds, active = active });
        }
        void Update()
        {
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                Pending item = pending[i];
                if (item.target == null) { pending.RemoveAt(i); continue; }
                if (Time.time < item.time) continue;
                item.target.SetActive(item.active);
                if (!item.active) { pending.RemoveAt(i); continue; }
                RecoveredNativeJetCache.NotifyActivation(item.target);
                if (RecoveredNativeJetCache.Owns(item.target.transform)) { pending.RemoveAt(i); continue; }
                foreach (Animator animator in item.target.GetComponentsInChildren<Animator>(true))
                    if (animator.gameObject.activeInHierarchy && animator.runtimeAnimatorController != null)
                    { animator.enabled = true; animator.Rebind(); animator.Update(0); }
                foreach (ParticleSystem particles in item.target.GetComponentsInChildren<ParticleSystem>(true))
                    if (particles.gameObject.activeInHierarchy) particles.Play(false);
                pending.RemoveAt(i);
            }
            if (pending.Count == 0) Destroy(this);
        }
    }
}
