using UnityEngine;

public class GenericThreePlanesCuttingController : MonoBehaviour
{
	public GameObject plane1;

	public GameObject plane2;

	public GameObject plane3;

	private Material mat;

	public Vector3 normal1;

	public Vector3 position1;

	public Vector3 normal2;

	public Vector3 position2;

	public Vector3 normal3;

	public Vector3 position3;

	public Renderer rend;

	private void Start()
	{
	}

	private void Update()
	{
	}

	private void UpdateShaderProperties()
	{
	}
}
