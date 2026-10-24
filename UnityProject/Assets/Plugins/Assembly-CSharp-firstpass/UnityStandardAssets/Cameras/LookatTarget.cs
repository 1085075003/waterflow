using UnityEngine;

namespace UnityStandardAssets.Cameras
{
	public class LookatTarget : AbstractTargetFollower
	{
		[SerializeField]
		private Vector2 m_RotationRange;

		[SerializeField]
		private float m_FollowSpeed;

		private Vector3 m_FollowAngles;

		private Quaternion m_OriginalRotation;

		protected Vector3 m_FollowVelocity;

		protected override void Start()
		{
		}

		protected override void FollowTarget(float deltaTime)
		{
		}
	}
}
