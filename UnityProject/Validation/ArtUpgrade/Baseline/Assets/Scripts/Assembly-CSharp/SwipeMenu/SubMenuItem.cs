using UnityEngine;
using UnityEngine.UI;

namespace SwipeMenu
{
	public class SubMenuItem : MonoBehaviour
	{
		public MenuItem OwnerMenu;

		public Button.ButtonClickedEvent OnClick;

		private void Update()
		{
		}

		private void CheckTouch(Vector3 screenPoint)
		{
		}
	}
}
