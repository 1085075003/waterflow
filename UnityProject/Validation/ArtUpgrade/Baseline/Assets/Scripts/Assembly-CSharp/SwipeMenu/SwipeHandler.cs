using UnityEngine;

namespace SwipeMenu
{
	public class SwipeHandler : MonoBehaviour
	{
		public enum FlickType
		{
			Inertia = 0,
			MoveOne = 1
		}

		public Transform paneles;

		public bool handleSwipes;

		public bool handleFlicks;

		public float requiredForceForFlick;

		public FlickType flickType;

		public bool lockToClosest;

		public float maxForce;

		private Vector3 finalPosition;

		private Vector3 startpos;

		private Vector3 endpos;

		private Vector3 oldpos;

		private float length;

		private float startTime;

		private float mouseMove;

		private float force;

		private bool SW;

		public bool isSwiping => false;

		private void Update()
		{
		}

		private void HandleMobileSwipe()
		{
		}

		private void HandleMouseSwipe()
		{
		}
	}
}
