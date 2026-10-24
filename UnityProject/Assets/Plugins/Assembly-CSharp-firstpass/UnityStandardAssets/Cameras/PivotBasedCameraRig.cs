using UnityEngine;

namespace UnityStandardAssets.Cameras
{
	public abstract class PivotBasedCameraRig : AbstractTargetFollower
	{
		protected Transform m_Cam;

		protected Transform m_Pivot;

		protected Vector3 m_LastTargetPosition;

		protected virtual void Awake()
		{
		}
	}
}
