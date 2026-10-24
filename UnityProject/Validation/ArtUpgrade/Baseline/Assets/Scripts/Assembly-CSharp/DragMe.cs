using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class DragMe : MonoBehaviour, IBeginDragHandler, IEventSystemHandler, IDragHandler, IEndDragHandler
{
	public bool dragOnSurfaces;

	private Dictionary<int, GameObject> m_DraggingIcons;

	private Dictionary<int, RectTransform> m_DraggingPlanes;

	public void OnBeginDrag(PointerEventData eventData)
	{
	}

	public void OnDrag(PointerEventData eventData)
	{
	}

	private void SetDraggedPosition(PointerEventData eventData)
	{
	}

	public void OnEndDrag(PointerEventData eventData)
	{
	}

	public static T FindInParents<T>(GameObject go) where T : Component
	{
		return null;
	}
}
