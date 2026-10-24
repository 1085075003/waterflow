using UnityEngine;

namespace UnityStandardAssets.Cameras
{
	public abstract class AbstractTargetFollower : MonoBehaviour
	{
		public enum UpdateType
		{
			FixedUpdate = 0,
			LateUpdate = 1,
			ManualUpdate = 2
		}

		[SerializeField]
		protected Transform m_Target;

		[SerializeField]
		private bool m_AutoTargetPlayer;

		[SerializeField]
		private UpdateType m_UpdateType;

		protected Rigidbody targetRigidbody;

		public Transform Target => null;

		protected virtual void Start()
		{
		}

		private void FixedUpdate()
		{
		}

		private void LateUpdate()
		{
		}

		public void ManualUpdate()
		{
		}

		protected abstract void FollowTarget(float deltaTime);

		public void FindAndTargetPlayer()
		{
		}

		public virtual void SetTarget(Transform newTransform)
		{
		}
	}
}
