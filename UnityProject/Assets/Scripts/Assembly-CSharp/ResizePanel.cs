using UnityEngine;
using UnityEngine.EventSystems;

public class ResizePanel : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IDragHandler
{
	public Vector2 minSize;

	public Vector2 maxSize;

	private RectTransform panelRectTransform;

	private Vector2 originalLocalPointerPosition;

	private Vector2 originalSizeDelta;

	private void Awake()
	{
	}

	public void OnPointerDown(PointerEventData data)
	{
	}

	public void OnDrag(PointerEventData data)
	{
	}
}
