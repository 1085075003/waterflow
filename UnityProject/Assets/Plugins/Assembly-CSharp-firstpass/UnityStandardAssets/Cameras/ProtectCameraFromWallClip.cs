using System.Collections;
using UnityEngine;

namespace UnityStandardAssets.Cameras
{
	public class ProtectCameraFromWallClip : MonoBehaviour
	{
		public class RayHitComparer : IComparer
		{
			public int Compare(object x, object y)
			{
				return 0;
			}
		}

		public float clipMoveTime;

		public float returnTime;

		public float sphereCastRadius;

		public bool visualiseInEditor;

		public float closestDistance;

		public string dontClipTag;

		private Transform m_Cam;

		private Transform m_Pivot;

		private float m_OriginalDist;

		private float m_MoveVelocity;

		private float m_CurrentDist;

		private Ray m_Ray;

		private RaycastHit[] m_Hits;

		private RayHitComparer m_RayHitComparer;

		public bool protecting { get; private set; }

		private void Start()
		{
		}

		private void LateUpdate()
		{
		}
	}
}
