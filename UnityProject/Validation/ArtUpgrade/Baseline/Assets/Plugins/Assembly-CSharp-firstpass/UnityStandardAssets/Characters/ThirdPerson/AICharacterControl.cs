using UnityEngine;
using UnityEngine.AI;

namespace UnityStandardAssets.Characters.ThirdPerson
{
	[RequireComponent(typeof(ThirdPersonCharacter))]
	[RequireComponent(typeof(NavMeshAgent))]
	public class AICharacterControl : MonoBehaviour
	{
		public Transform target;

		public NavMeshAgent agent { get; private set; }

		public ThirdPersonCharacter character { get; private set; }

		private void Start()
		{
		}

		private void Update()
		{
		}

		public void SetTarget(Transform target)
		{
		}
	}
}
