using UnityEngine;

public class CascadaTest4 : MonoBehaviour
{
	public GameObject esferaSalida;

	public GameObject prefabCascada;

	public GameObject prefabEsfera;

	public GameObject esferita;

	public bool cascada1;

	public bool cascada2;

	public bool cascada3;

	public bool cascada4;

	public int altura;

	public int altura_old;

	private GameObject cascada;

	private bool instanciar;

	public bool checkSuelo;

	public float distan_y;

	private float var_x;

	private float var_z;

	private float var_rota;

	private Vector3 vector2;

	private float sphereRadius;

	public LayerMask mascaraSuelo;

	private float varx_esfera;

	private float vary_esfera;

	private float varz_esfera;

	public int control;

	private void Start()
	{
	}

	private void Update()
	{
	}

	private void OnTriggerEnter(Collider other)
	{
	}

	private void OnTriggerExit(Collider other)
	{
	}

	private void doControlAltura()
	{
	}

	private void doControlCascada()
	{
	}

	public bool CheckCollision(float distan_y)
	{
		return false;
	}
}
