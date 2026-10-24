using UnityEngine;

namespace UnityStandardAssets.Cameras
{
	public class TargetFieldOfView : AbstractTargetFollower
	{
		[SerializeField]
		private float m_FovAdjustTime;

		[SerializeField]
		private float m_ZoomAmountMultiplier;

		[SerializeField]
		private bool m_IncludeEffectsInSize;

		private float m_BoundSize;

		private float m_FovAdjustVelocity;

		private Camera m_Cam;

		private Transform m_LastTarget;

		protected override void Start()
		{
		}

		protected override void FollowTarget(float deltaTime)
		{
		}

		public override void SetTarget(Transform newTransform)
		{
		}

		public static float MaxBoundsExtent(Transform obj, bool includeEffects)
		{
			return 0f;
		}
	}
}
