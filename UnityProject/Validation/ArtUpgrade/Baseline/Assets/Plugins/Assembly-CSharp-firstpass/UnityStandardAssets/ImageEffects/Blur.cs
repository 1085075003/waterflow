using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
	[ExecuteInEditMode]
	[AddComponentMenu("Image Effects/Blur/Blur")]
	public class Blur : MonoBehaviour
	{
		[Range(0f, 10f)]
		public int iterations;

		[Range(0f, 1f)]
		public float blurSpread;

		public Shader blurShader;

		private static Material m_Material;

		protected Material material => null;

		protected void OnDisable()
		{
		}

		protected void Start()
		{
		}

		public void FourTapCone(RenderTexture source, RenderTexture dest, int iteration)
		{
		}

		private void DownSample4x(RenderTexture source, RenderTexture dest)
		{
		}

		private void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
		}
	}
}
