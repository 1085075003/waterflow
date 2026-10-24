using UnityEngine;

public class CascadaTestB : MonoBehaviour
{
	public GameObject esferaSalida;

	public GameObject prefabCas1_1;

	public GameObject prefabCas2_1;

	public GameObject prefabCas3_1;

	public GameObject prefabCas1_2;

	public GameObject prefabCas2_2;

	public GameObject prefabCas3_2;

	public GameObject prefabEsfera;

	public int color;

	public bool cascada1;

	public bool cascada2;

	public bool cascada3;

	public bool cascada4;

	public GameObject planoFin;

	public Material materialColor1;

	public Material materialColor2;

	public Material materialColor3;

	public int altura;

	public int altura_old;

	public int color_old;

	private GameObject cascada;

	private bool instanciar;

	public bool checkSuelo;

	public float distan_y;

	private float var_x;

	private float var_y;

	private float var_z;

	private float var_rota;

	private float varScalex;

	private float varScaley;

	private float varScalez;

	private float distancia_x;

	private float distancia_z;

	private Vector3 vector2;

	private float sphereRadius;

	public LayerMask mascaraSuelo;

	private float varx_esfera;

	private float vary_esfera;

	private float varz_esfera;

	public int control;

	public int contadorUpdate;

	public float tanteoY;

	private void Start()
	{
	}

	private void Update()
	{
	}

	private void doControlAltura()
	{
	}

	private void doControlCascada()
	{
	}

	private void doAsignarValores()
	{
	}

	public bool CheckCollision(float distan_y)
	{
		return false;
	}
}
