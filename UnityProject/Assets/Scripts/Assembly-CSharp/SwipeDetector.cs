using UnityEngine;

public class SwipeDetector : MonoBehaviour
{
	public enum SwipeDirection
	{
		None = 0,
		Up = 1,
		Down = 2
	}

	public float comfortZone;

	public float minSwipeDist;

	public float maxSwipeTime;

	private float startTime;

	private Vector2 startPos;

	private bool couldBeSwipe;

	public SwipeDirection lastSwipe;

	public float lastSwipeTime;

	private void Update()
	{
	}
}
