using UnityEngine;

public class CascadaTestC : MonoBehaviour
{
	public string pitValor;

	public GameObject Chorro;

	private GameObject esferaSalida;

	public GameObject esferaSalida1;

	public GameObject esferaSalida2;

	private int esfera;

	public GameObject ChorroCorto;

	public GameObject ChorroCortoColor1;

	public GameObject ChorroCortoColor2;

	public GameObject ChorroCortoColor3;

	public GameObject ChorroCortoColor4;

	public GameObject prefabSplash2;

	public GameObject esferaFiltrante;

	public int color;

	public int altura;

	public int altura_old;

	public int color_old;

	private GameObject splash;

	private bool instanciar;

	private bool noHayChorro;

	public bool checkSuelo;

	public float distan_y;

	public float distancia_x;

	public float distancia_z;

	public float rotacion;

	public float distan_x;

	public float distan_z;

	private Vector3 vector2;

	private float sphereRadius;

	public LayerMask mascaraSuelo;

	public float vary_esfera;

	public float esperaEsfera;

	private int contUpdates;

	public ParticleSystem ps;

	private bool diagonal;

	public string nombre;

	public int estado;

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
