using UnityEngine;

public class RotateCamera : MonoBehaviour
{
	public static int giro;

	public Transform superficie;

	public Transform from;

	public Transform to;

	public float speed;

	public GameObject centro;

	public Transform center;

	public Vector3 axis;

	public Vector3 desiredPosition;

	public float radius;

	public float radiusSpeed;

	public float rotationSpeed;

	public float valory;

	public float angulo;

	public float anguloOld;

	public bool primeraVez;

	public bool check1;

	public float ang;

	public float targetAngle;

	public float targetAngle1;

	public float rotationinicial;

	public float rotationAmount;

	public float descenso;

	private void Start()
	{
	}

	private void FixedUpdate()
	{
	}

	protected void Update()
	{
	}
}
