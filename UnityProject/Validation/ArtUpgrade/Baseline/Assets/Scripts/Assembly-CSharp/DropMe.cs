using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropMe : MonoBehaviour, IDropHandler, IEventSystemHandler, IPointerEnterHandler, IPointerExitHandler
{
	public Image containerImage;

	public Image receivingImage;

	private Color normalColor;

	public Color highlightColor;

	public void OnEnable()
	{
	}

	public void OnDrop(PointerEventData data)
	{
	}

	public void OnPointerEnter(PointerEventData data)
	{
	}

	public void OnPointerExit(PointerEventData data)
	{
	}

	private Sprite GetDropSprite(PointerEventData data)
	{
		return null;
	}
}
