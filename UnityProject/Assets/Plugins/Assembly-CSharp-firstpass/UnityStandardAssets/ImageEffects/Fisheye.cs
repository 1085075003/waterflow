using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
	[ExecuteInEditMode]
	[AddComponentMenu("Image Effects/Displacement/Fisheye")]
	[RequireComponent(typeof(Camera))]
	public class Fisheye : PostEffectsBase
	{
		[Range(0f, 1.5f)]
		public float strengthX;

		[Range(0f, 1.5f)]
		public float strengthY;

		public Shader fishEyeShader;

		private Material fisheyeMaterial;

		public override bool CheckResources()
		{
			return false;
		}

		private void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
		}
	}
}
