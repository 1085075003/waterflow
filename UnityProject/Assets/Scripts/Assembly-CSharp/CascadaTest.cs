using UnityEngine;

public class CascadaTest : MonoBehaviour
{
	public bool activateDebug;

	public GameObject esferaSalida;

	public GameObject prefabCas1_1;

	public GameObject prefabCas2_1;

	public GameObject prefabCas3_1;

	public GameObject prefabCas4_1;

	public GameObject prefabCas1_2;

	public GameObject prefabCas2_2;

	public GameObject prefabCas3_2;

	public GameObject prefabCas4_2;

	public GameObject prefabCas1_6;

	public GameObject prefabCas2_6;

	public GameObject prefabCas3_6;

	public GameObject prefabCas4_6;

	public GameObject prefabFall1_1;

	public GameObject prefabFall1_2;

	public GameObject prefabFall1_3;

	public GameObject prefabFall1_4;

	public GameObject prefabSplash_1;

	public GameObject esferaFiltrante;

	public GameObject capsulaFiltrante;

	private GameObject prefabUnion;

	public GameObject prefabUnion1;

	public GameObject prefabUnion2;

	public GameObject prefabUnion3;

	public GameObject prefabUnion4;

	public GameObject prefabEsfera;

	public int color;

	public string confColor;

	public bool cascada1;

	public bool cascada2;

	public bool cascada3;

	public bool cascada4;

	public bool cascada6;

	private bool cas1;

	private bool cas2;

	private bool cas3;

	private bool cas4;

	public GameObject plano;

	public GameObject planoCurvo;

	public GameObject planoFin;

	public Material materialColor1;

	public Material materialColor2;

	public Material materialColor3;

	public Material materialColor4;

	public int altura;

	public int altura_old;

	public int color_old;

	private GameObject cascada;

	private GameObject splash;

	private bool instanciar;

	public bool checkSuelo;

	public bool checkCamino;

	public bool checkCollision1;

	public bool checkPared;

	public float distan_y;

	private float var_x;

	private float var_y;

	private float var_z;

	private float var_rota;

	private float var_x1;

	private float var_y1;

	private float var_z1;

	private float var_rota1;

	private float var_x2;

	private float var_y2;

	private float var_z2;

	private float var_x3;

	private float var_y3;

	private float var_z3;

	private float varScalex;

	private float varScaley;

	private float varScalez;

	private float varScalex2;

	private float distancia_x;

	private float distancia_z;

	private float distancia_x1;

	private float distancia_z1;

	private float distancia_x2;

	private float distancia_z2;

	private Vector3 vector2;

	private Vector3 vector2b;

	private Vector3 vector3;

	private float sphereRadius;

	private float sphereRadius1;

	public LayerMask mascaraSuelo;

	public LayerMask mascaraDefault;

	public LayerMask mascaraArco;

	private float varx_esfera;

	private float vary_esfera;

	private float varz_esfera;

	public int control;

	public float tanteoY;

	public bool tipoChorro;

	private GameObject Agua2;

	private bool desactivar;

	public bool swhayrebose;

	public bool rebose;

	public bool wall1;

	public bool wall2;

	public bool wall3;

	public bool wall4;

	public float esperaEsfera;

	public bool checkCollisionArco;

	private int contUpdates;

	public bool checkPNormal;

	public bool checkfijo;

	public bool hayfijoalt1;

	private void Start()
	{
	}

	private void OnEnable()
	{
	}

	private void Update()
	{
	}

	private void doProcess()
	{
	}

	private void doDestroyCascada()
	{
	}

	private void doDesactivarPlanoFin()
	{
	}

	private void doControlAltura()
	{
	}

	private void doControlCascada()
	{
	}

	private void doControlCascada6()
	{
	}

	public void doActivarEsferaSalida()
	{
	}

	public void doAsignarValores()
	{
	}

	public void doAsignarValores6()
	{
	}

	public void doAsignarValoresFall()
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

	public void doActivarCapsulaFiltrante()
	{
	}

	private void doCaminoBloqueado()
	{
	}

	private void doAsignarValores1()
	{
	}

	public bool CheckCollision(float distan_y)
	{
		return false;
	}

	public void CheckCollisionPNormal()
	{
	}

	public bool CheckCollisionArco(float distan_y)
	{
		return false;
	}

	public bool CheckCollisionPared()
	{
		return false;
	}

	public void CheckCollision1(float distancia_x1, float distancia_z1)
	{
	}
}
