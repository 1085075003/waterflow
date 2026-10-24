using UnityEngine;

public class CameraMotor : MonoBehaviour
{
	public Transform lookAt;

	public Transform objeto;

	private Vector3 desiredPosition0;

	private Vector3 desiredPosition1;

	private Vector3 offset;

	private Vector3 offset1;

	public float smoothSpeed;

	public float distance;

	public float yOffset;

	public float xOffset;

	public float zOffset;

	public float dist;

	public bool primeraVez;

	private void Start()
	{
	}

	private void FixedUpdate()
	{
	}

	protected void Update()
	{
	}

	public void SlideCamera(Vector3 rotation)
	{
	}
}
