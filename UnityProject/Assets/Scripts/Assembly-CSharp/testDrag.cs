using UnityEngine;

public class testDrag : MonoBehaviour
{
	private GameObject getTarget;

	private bool isMouseDragging;

	private Vector3 offsetValue;

	private Vector3 positionOfScreen;

	private void Start()
	{
	}

	private void Update()
	{
	}

	private GameObject ReturnClickedObject(out RaycastHit hit)
	{
		hit = default;
		return null;
	}
}
