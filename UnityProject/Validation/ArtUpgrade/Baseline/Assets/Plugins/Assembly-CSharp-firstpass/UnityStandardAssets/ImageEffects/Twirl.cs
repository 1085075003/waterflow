using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
	[AddComponentMenu("Image Effects/Displacement/Twirl")]
	[ExecuteInEditMode]
	public class Twirl : ImageEffectBase
	{
		public Vector2 radius;

		[Range(0f, 360f)]
		public float angle;

		public Vector2 center;

		private void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
		}
	}
}
