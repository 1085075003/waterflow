using UnityEngine;

namespace UnityStandardAssets.Characters.ThirdPerson
{
	[RequireComponent(typeof(ThirdPersonCharacter))]
	public class ThirdPersonUserControl : MonoBehaviour
	{
		private ThirdPersonCharacter m_Character;

		private Transform m_Cam;

		private Vector3 m_CamForward;

		private Vector3 m_Move;

		private bool m_Jump;

		private void Start()
		{
		}

		private void Update()
		{
		}

		private void FixedUpdate()
		{
		}
	}
}
