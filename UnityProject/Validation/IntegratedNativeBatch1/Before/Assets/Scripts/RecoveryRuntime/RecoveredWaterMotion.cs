using System.Collections.Generic;
using UnityEngine;

namespace Recovered.FlowWater
{
    /// <summary>Replays authored UV speeds and exported water animation/particle assets.</summary>
    public sealed class RecoveredWaterMotion : MonoBehaviour
    {
        struct Surface
        {
            public Renderer renderer;
            public Vector2 speed, offset, scale;
        }
        readonly List<Surface> surfaces = new List<Surface>();
        MaterialPropertyBlock properties;
        static readonly int MainTextureTransform = Shader.PropertyToID("_MainTex_ST");
        static readonly int DetailTextureTransform = Shader.PropertyToID("_DetailTex_ST");
        public void RestoreAuthoredMotion(float age = 0)
        {
            if (properties == null) properties = new MaterialPropertyBlock();
            RecoveredNativeParticleColors.Apply(gameObject);
            surfaces.Clear();
            foreach (scrollTest1 original in GetComponentsInChildren<scrollTest1>(true))
            {
                Renderer renderer = original.rend != null ? original.rend : original.GetComponent<Renderer>();
                if (renderer == null || renderer.sharedMaterial == null) continue;
                Material material = renderer.sharedMaterial;
                surfaces.Add(new Surface { renderer = renderer, speed = new Vector2(original.scrollSpeed_X, original.scrollSpeed_Y),
                    offset = material.mainTextureOffset + new Vector2(original.offsetInicialX, original.offsetInicialY), scale = material.mainTextureScale });
                original.enabled = false;
            }
            foreach (Animator animator in GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = true; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                if (animator.gameObject.activeInHierarchy && animator.runtimeAnimatorController != null) { animator.Rebind(); animator.Update(Mathf.Max(0, age)); }
            }
            foreach (Animation animation in GetComponentsInChildren<Animation>(true)) { animation.enabled = true; animation.Play(); }
            foreach (ParticleSystem particles in GetComponentsInChildren<ParticleSystem>(true))
            {
                if (!particles.gameObject.activeInHierarchy) continue;
                particles.Simulate(Mathf.Max(0, age), false, true, true); particles.Play(false);
            }
        }
        void LateUpdate()
        {
            if (properties == null) return;
            foreach (Surface surface in surfaces)
            {
                if (surface.renderer == null || !surface.renderer.enabled) continue;
                Vector2 offset = surface.offset + surface.speed * Time.time;
                surface.renderer.GetPropertyBlock(properties);
                Vector4 st = new Vector4(surface.scale.x, surface.scale.y, offset.x, offset.y);
                properties.SetVector(MainTextureTransform, st); properties.SetVector(DetailTextureTransform, st);
                surface.renderer.SetPropertyBlock(properties); properties.Clear();
            }
        }
    }
}
