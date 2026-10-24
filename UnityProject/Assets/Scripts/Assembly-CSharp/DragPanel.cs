using UnityEngine;
using UnityEngine.EventSystems;

public class DragPanel : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IDragHandler
{
	private Vector2 originalLocalPointerPosition;

	private Vector3 originalPanelLocalPosition;

	private RectTransform panelRectTransform;

	private RectTransform parentRectTransform;

	private void Awake()
	{
	}

	public void OnPointerDown(PointerEventData data)
	{
	}

	public void OnDrag(PointerEventData data)
	{
	}

	private void ClampToWindow()
	{
	}
}
