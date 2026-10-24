using System;
using UnityEngine;

namespace Recovered.FlowWater
{
    /// <summary>Native Animator writes bound to the exact original instantiated fountain.</summary>
    public sealed class RecoveredNativeFountainAnimation : MonoBehaviour
    {
        [Serializable] sealed class Catalog { public string[] nativeOwners; public Entry[] entries; }
        [Serializable] sealed class Entry { public string nativeInstantiateVA, configuration; public Operation[] operations; }
        [Serializable] sealed class Operation { public string kind, parameter; public int value; }
        static Catalog catalog;
        bool authoredEnabled;
        Entry configured;
        public string NativeInstantiateVA { get; private set; }
        public string NativeConfiguration { get; private set; }
        public static void Bind(GameObject instance, string nativeInstantiateVA, string configuration)
        {
            if (instance == null || String.IsNullOrEmpty(nativeInstantiateVA)) return;
            if (catalog == null)
            {
                TextAsset asset = Resources.Load<TextAsset>("RecoveredNativeFountainAnimators");
                if (asset == null) return;
                catalog = JsonUtility.FromJson<Catalog>(asset.text);
            }
            if (catalog == null || catalog.nativeOwners == null || Array.IndexOf(catalog.nativeOwners, nativeInstantiateVA) < 0) return;
            // Native GetComponent<Animator> is on this exact instance root;
            // searching descendants would assign unrelated child controllers.
            Animator animator = instance.GetComponent<Animator>();
            if (animator == null) return;
            RecoveredNativeFountainAnimation binding = instance.AddComponent<RecoveredNativeFountainAnimation>();
            binding.authoredEnabled = animator.enabled;
            binding.NativeInstantiateVA = nativeInstantiateVA;
            binding.NativeConfiguration = configuration ?? "";
            if (catalog.entries != null)
                foreach (Entry entry in catalog.entries)
                    if (entry.nativeInstantiateVA == nativeInstantiateVA && entry.configuration == binding.NativeConfiguration)
                    { binding.configured = entry; break; }
        }
        public static bool TryRestore(Animator animator, float age)
        {
            RecoveredNativeFountainAnimation binding = animator.GetComponent<RecoveredNativeFountainAnimation>();
            if (binding == null) return false;
            bool enabled = binding.authoredEnabled;
            if (binding.configured != null && binding.configured.operations != null)
                foreach (Operation operation in binding.configured.operations)
                    if (operation.kind == "enabled") enabled = operation.value != 0;
            animator.enabled = enabled;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (!enabled || !animator.gameObject.activeInHierarchy || animator.runtimeAnimatorController == null) return true;
            animator.Rebind();
            // Rebind resets controller parameters. Reapply native writes before
            // the first animation evaluation instead of losing them to defaults.
            if (binding.configured != null && binding.configured.operations != null)
                foreach (Operation operation in binding.configured.operations)
                    if (operation.kind == "integer") animator.SetInteger(operation.parameter, operation.value);
            animator.Update(Mathf.Max(0, age));
            return true;
        }
    }
}
