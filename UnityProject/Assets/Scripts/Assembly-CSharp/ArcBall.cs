using UnityEngine;

[ExecuteInEditMode]
public class ArcBall : MonoBehaviour
{
	public float radius;

	public float minRadius;

	public float maxRadius;

	public float scale;

	public GameObject target;

	private float targetRadius;

	private float mouseX;

	private float mouseZ;

	private Vector3 up;

	private Vector3 right;

	private Vector3 newPosition;

	private void Start()
	{
	}

	private void Update()
	{
	}
}
