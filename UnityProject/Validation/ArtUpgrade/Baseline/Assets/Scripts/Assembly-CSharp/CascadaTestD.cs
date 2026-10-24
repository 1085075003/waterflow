using UnityEngine;

public class CascadaTestD : MonoBehaviour
{
	public string pitValor;

	public GameObject Chorro;

	private GameObject esferaSalida;

	public GameObject esferaSalida1;

	public GameObject esferaSalida2;

	private int esfera;

	public GameObject ChorroLargo;

	public GameObject ChorroLargoColor1;

	public GameObject ChorroLargoColor2;

	public GameObject ChorroLargoColor3;

	public GameObject ChorroLargoColor4;

	public GameObject prefabSplash2;

	public GameObject esferaFiltrante;

	public int color;

	public int altura;

	public int altura1;

	public int altura_old;

	public int color_old;

	private GameObject splash;

	private bool instanciar;

	private bool noHayChorro;

	public bool checkSuelo;

	public float distan_y;

	public float distancia_x;

	public float distancia_x1;

	public float distancia_x2;

	public float distancia_z;

	public float distancia_z1;

	public float distancia_z2;

	public float rotacion;

	public float distan_x;

	public float distan_x1;

	public float distan_z;

	private Vector3 vector2;

	public float sphereRadius;

	public LayerMask mascaraSuelo;

	public LayerMask mascaraWater;

	public float vary_esfera;

	public float esperaEsfera;

	private int contUpdates;

	public ParticleSystem ps;

	public bool altura1_ok;

	private bool diagonal;

	public string nombre;

	public int estado;

	public int subestado;

	public int alturaChorro;

	public float objx;

	public float objy;

	public float objz;

	public float collx;

	public float colly;

	public float collz;

	public bool checkWater;

	public bool checkWater_old;

	private void Start()
	{
	}

	private void OnEnable()
	{
	}

	public void doAsignarValoresStart()
	{
	}

	public void Update()
	{
	}

	public void doProcess()
	{
	}

	public void doControlAltura()
	{
	}

	public void doControlAltura1()
	{
	}

	public void doControlAltura2()
	{
	}

	public void doControlChorro()
	{
	}

	public void doControlNormal()
	{
	}

	public void doControlDiagonal()
	{
	}

	public void doActivarEsferaSalida()
	{
	}

	public void doInstanciarSplash()
	{
	}

	public void doCheckSuelo()
	{
	}

	public void doActivarEsferaFiltrante()
	{
	}

	public bool CheckCollision(float distan_y)
	{
		return false;
	}

	public void doDesactivarSalidas()
	{
	}

	public void doCrearChorro()
	{
	}
}
