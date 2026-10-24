using UnityEngine;

namespace UnityStandardAssets.Cameras
{
	public class FreeLookCam : PivotBasedCameraRig
	{
		[SerializeField]
		private float m_MoveSpeed;

		[Range(0f, 10f)]
		[SerializeField]
		private float m_TurnSpeed;

		[SerializeField]
		private float m_TurnSmoothing;

		[SerializeField]
		private float m_TiltMax;

		[SerializeField]
		private float m_TiltMin;

		[SerializeField]
		private bool m_LockCursor;

		[SerializeField]
		private bool m_VerticalAutoReturn;

		private float m_LookAngle;

		private float m_TiltAngle;

		private const float k_LookDistance = 100f;

		private Vector3 m_PivotEulers;

		private Quaternion m_PivotTargetRot;

		private Quaternion m_TransformTargetRot;

		protected override void Awake()
		{
		}

		protected void Update()
		{
		}

		private void OnDisable()
		{
		}

		protected override void FollowTarget(float deltaTime)
		{
		}

		private void HandleRotationMovement()
		{
		}
	}
}
