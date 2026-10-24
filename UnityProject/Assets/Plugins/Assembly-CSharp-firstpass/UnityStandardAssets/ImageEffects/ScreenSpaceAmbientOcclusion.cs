using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(Camera))]
	[AddComponentMenu("Image Effects/Rendering/Screen Space Ambient Occlusion")]
	public class ScreenSpaceAmbientOcclusion : MonoBehaviour
	{
		public enum SSAOSamples
		{
			Low = 0,
			Medium = 1,
			High = 2
		}

		[Range(0.05f, 1f)]
		public float m_Radius;

		public SSAOSamples m_SampleCount;

		[Range(0.5f, 4f)]
		public float m_OcclusionIntensity;

		[Range(0f, 4f)]
		public int m_Blur;

		[Range(1f, 6f)]
		public int m_Downsampling;

		[Range(0.2f, 2f)]
		public float m_OcclusionAttenuation;

		[Range(1E-05f, 0.5f)]
		public float m_MinZ;

		public Shader m_SSAOShader;

		private Material m_SSAOMaterial;

		public Texture2D m_RandomTexture;

		private bool m_Supported;

		private static Material CreateMaterial(Shader shader)
		{
			return null;
		}

		private static void DestroyMaterial(Material mat)
		{
		}

		private void OnDisable()
		{
		}

		private void Start()
		{
		}

		private void OnEnable()
		{
		}

		private void CreateMaterials()
		{
		}

		[ImageEffectOpaque]
		private void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
		}
	}
}
