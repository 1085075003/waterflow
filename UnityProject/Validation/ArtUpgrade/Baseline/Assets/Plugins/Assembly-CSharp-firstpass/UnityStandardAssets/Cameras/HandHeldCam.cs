using UnityEngine;

namespace UnityStandardAssets.Cameras
{
	public class HandHeldCam : LookatTarget
	{
		[SerializeField]
		private float m_SwaySpeed;

		[SerializeField]
		private float m_BaseSwayAmount;

		[SerializeField]
		private float m_TrackingSwayAmount;

		[Range(-1f, 1f)]
		[SerializeField]
		private float m_TrackingBias;

		protected override void FollowTarget(float deltaTime)
		{
		}
	}
}
