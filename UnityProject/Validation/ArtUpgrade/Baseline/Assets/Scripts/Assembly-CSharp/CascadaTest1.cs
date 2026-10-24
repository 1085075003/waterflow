using UnityEngine;

public class CascadaTest1 : MonoBehaviour
{
	public GameObject esferaSalida;

	public GameObject prefabFuente1;

	public GameObject prefabFuente2;

	public GameObject prefabFuente3;

	public GameObject prefabFuente4;

	public GameObject prefabEsfera;

	public GameObject fuente;

	public GameObject esferita;

	public int color;

	public int color_old;

	private bool instanciar;

	public bool ObjetoArriba;

	public bool checkObjetoArriba;

	public float distan_y;

	private float var_rota;

	private Vector3 vector2;

	private float sphereRadius;

	public LayerMask mascaraSuelo;

	public int activa;

	public int activa_old;

	public bool creada;

	private void Start()
	{
	}

	private void Update()
	{
	}

	private void doControlObjetoArriba()
	{
	}

	private void doCrearFuente()
	{
	}

	public bool CheckCollision(float distan_y)
	{
		return false;
	}
}
