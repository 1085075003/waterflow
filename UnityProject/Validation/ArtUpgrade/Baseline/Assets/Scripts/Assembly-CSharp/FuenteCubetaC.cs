using UnityEngine;

public class FuenteCubetaC : MonoBehaviour
{
	public string nombreObjeto;

	public string proceso;

	public int origen;

	public int colorCubeta;

	private int colorCubeta1;

	public GameObject Cascada1;

	public GameObject Cascada2;

	public GameObject Cascada3;

	public GameObject Cascada4;

	public GameObject esfera1;

	public GameObject esfera2;

	public GameObject esfera3;

	public GameObject esfera4;

	public GameObject esferaCascada1;

	public GameObject esferaCascada2;

	public GameObject esferaCascada3;

	public GameObject esferaCascada4;

	public float espera;

	private GameObject Agua1;

	private GameObject Agua2;

	public GameObject Aguas;

	public GameObject prefabAgua1;

	public GameObject prefabAgua2;

	public GameObject prefabAgua3;

	public GameObject prefabFuente1;

	public GameObject prefabFuente2;

	public GameObject prefabFuente3;

	public int tipoFuente;

	private GameObject prefabAgua;

	private GameObject prefabFuente;

	public GameObject planoNoEntra;

	public bool ontrigger;

	public bool esf1;

	public bool esf2;

	public bool esf3;

	public bool esf4;

	public bool activar;

	public bool activarFuente;

	public bool esf1Activada;

	public bool esf2Activada;

	public bool esf3Activada;

	public bool esf4Activada;

	public string tagName;

	private int color;

	public Material materialAmarillo;

	public Material materialRojo;

	public Material materialAzul;

	public Material materialColor1;

	public Material materialColor2;

	public Material materialColor3;

	private AguasControlB agscr;

	public bool triggered;

	public GameObject otherObject;

	public LayerMask mascaraObjeto;

	public string tagReceived;

	public string tagcolor;

	private int contador;

	private Vector3 vector2;

	private string colorAChequear;

	private bool checkCollision5;

	private float sphereRadius;

	public int numOrigen5;

	private string SonidoFuente;

	private AudioManager audioManager;

	private void Awake()
	{
	}

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

	public void doReceivefromAguas()
	{
	}

	public void doProcessEnter()
	{
	}

	private void doExit()
	{
	}

	private void doActivarEsfera()
	{
	}

	private void doDesactivarEsfera()
	{
	}

	private void doInstanciarAgua()
	{
	}

	private void doActivarCascada()
	{
	}

	private void doCheckCollision5()
	{
	}
}
