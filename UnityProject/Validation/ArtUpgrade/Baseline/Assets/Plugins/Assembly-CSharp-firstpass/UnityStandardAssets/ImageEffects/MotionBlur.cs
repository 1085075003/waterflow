using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
	[ExecuteInEditMode]
	[AddComponentMenu("Image Effects/Blur/Motion Blur (Color Accumulation)")]
	[RequireComponent(typeof(Camera))]
	public class MotionBlur : ImageEffectBase
	{
		[Range(0f, 0.92f)]
		public float blurAmount;

		public bool extraBlur;

		private RenderTexture accumTexture;

		protected override void Start()
		{
		}

		protected override void OnDisable()
		{
		}

		private void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
		}
	}
}
