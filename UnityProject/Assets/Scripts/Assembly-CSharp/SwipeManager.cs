using UnityEngine;

public class SwipeManager : MonoBehaviour
{
	private static SwipeManager instance;

	private Vector3 touchPosition;

	private float swipeResistanceX;

	private float swipeResistanceY;

	public float alturaMinima;

	public static SwipeManager Instance => null;

	public SwipeDirection Direction { get; set; }

	private void Start()
	{
	}

	private void Update()
	{
	}

	public bool IsSwiping(SwipeDirection dir)
	{
		return false;
	}
}
