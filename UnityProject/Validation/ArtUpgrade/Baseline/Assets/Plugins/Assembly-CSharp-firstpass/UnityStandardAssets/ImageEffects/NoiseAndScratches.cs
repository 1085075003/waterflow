using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(Camera))]
	[AddComponentMenu("Image Effects/Noise/Noise and Scratches")]
	public class NoiseAndScratches : MonoBehaviour
	{
		public bool monochrome;

		private bool rgbFallback;

		[Range(0f, 5f)]
		public float grainIntensityMin;

		[Range(0f, 5f)]
		public float grainIntensityMax;

		[Range(0.1f, 50f)]
		public float grainSize;

		[Range(0f, 5f)]
		public float scratchIntensityMin;

		[Range(0f, 5f)]
		public float scratchIntensityMax;

		[Range(1f, 30f)]
		public float scratchFPS;

		[Range(0f, 1f)]
		public float scratchJitter;

		public Texture grainTexture;

		public Texture scratchTexture;

		public Shader shaderRGB;

		public Shader shaderYUV;

		private Material m_MaterialRGB;

		private Material m_MaterialYUV;

		private float scratchTimeLeft;

		private float scratchX;

		private float scratchY;

		protected Material material => null;

		protected void Start()
		{
		}

		protected void OnDisable()
		{
		}

		private void SanitizeParameters()
		{
		}

		private void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
		}
	}
}
