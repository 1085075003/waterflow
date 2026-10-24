using UnityEngine;

namespace SwipeMenu
{
	public class Menu : MonoBehaviour
	{
		public int startingMenuItem;

		public float menuItemAngle;

		public float distanceBetweenMenus;

		public float zOffsetForCentredItem;

		public MenuItem[] menuItems;

		private float _centreOffset;

		private float _currentMenuPosition;

		private float _maxMenuPosition;

		private SwipeHandler _swipeHandler;

		private static Menu _instance;

		public static Menu instance => null;

		private void Awake()
		{
		}

		private void Update()
		{
		}

		public void MoveLeftRightByAmount(int amount)
		{
		}

		public void AnimateToTargetItem(MenuItem item)
		{
		}

		public void Constant(float amount)
		{
		}

		public void Inertia(float amount)
		{
		}

		public void LockToClosest()
		{
		}

		public bool MenuCentred(MenuItem item)
		{
			return false;
		}

		public void HideMenus()
		{
		}

		public void ShowMenus()
		{
		}

		public void ActivateSelectedMenuItem(MenuItem item)
		{
		}

		private void ParentMenuItems()
		{
		}

		private MenuItem GetClosestMenuItem()
		{
			return null;
		}

		private int GetClosestMenuItemIndex()
		{
			return 0;
		}

		private float CalcPosXInverse(float realPosx)
		{
			return 0f;
		}

		private float CalculateMenuItemRotation(float offsetx)
		{
			return 0f;
		}

		private float CalculateMenuItemXPosition(float offsetx)
		{
			return 0f;
		}

		private float CalculateMenuItemZPosition(float offsetx)
		{
			return 0f;
		}

		private void UpdateMenuItemsPositionInWorldSpace()
		{
		}

		private void UpdateCurrentMenuPosition(float pos)
		{
		}

		private void AnimationComplete()
		{
		}

		private float IsDivisble(float x, float n)
		{
			return 0f;
		}

		private float CalculateOffsetFromX(float start, float x)
		{
			return 0f;
		}
	}
}
