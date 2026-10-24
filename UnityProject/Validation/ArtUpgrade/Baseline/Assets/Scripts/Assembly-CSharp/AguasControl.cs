using UnityEngine;

public class AguasControl : MonoBehaviour
{
	public int hayOrigen;

	public string hayColor;

	public bool llenadoTerminado;

	public bool origen1;

	public bool origen2;

	public bool origen3;

	public bool origen4;

	public bool origen5;

	public bool origen6;

	public bool checkCollision;

	public bool checkCollision1;

	public bool checkCollision2;

	public bool checkCollision3;

	public bool checkCollision4;

	public bool checkCollision5;

	public GameObject orig1;

	public GameObject orig2;

	public GameObject orig3;

	public GameObject orig4;

	public GameObject orig5;

	public GameObject orig6;

	public LayerMask mascaraNoObjeto;

	public LayerMask mascaraObjeto;

	private Vector3 vector2;

	public float sphereRadius;

	private float distan_x;

	private float distan_z;

	private bool encontrado;

	private string tag1;

	private GameObject otherObject;

	private void Awake()
	{
	}

	private void Start()
	{
	}

	public void doBuscarOrigen()
	{
	}

	private void doCheckCollider1()
	{
	}

	private void doCheckCollider2()
	{
	}

	private void doCheckCollider3()
	{
	}

	private void doCheckCollider4()
	{
	}

	private void doCheckCollider5()
	{
	}

	private void doCheckCollision1(float distan_x, float distan_z)
	{
	}

	public bool CheckCollision2(float distan_x, float distan_z)
	{
		return false;
	}
}
