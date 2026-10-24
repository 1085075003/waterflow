using UnityEngine;
using UnityEngine.UI;

namespace SwipeMenu
{
	public class MenuItem : MonoBehaviour
	{
		public Button.ButtonClickedEvent OnClick;

		public Button.ButtonClickedEvent OnOtherMenuClick;
	}
}
