using UnityEngine;
using UnityEngine.UI;

public class ControladorJuego : MonoBehaviour
{
	public GameObject objComplete1;

	private bool primeravez;

	[SerializeField]
	public static int nivelActual;

	public static bool stopLevel;

	public static bool misionComplete;

	public static bool misionCompleteAd;

	public static int numFuentes;

	public static int numFuentesSon;

	public static int requiredFuentes;

	public int numFuentes1;

	public int numFuentes1Son;

	public int requiredFuentes1;

	public GameObject menubutton;

	public Image menuIcon;

	public GameObject musicbutton;

	public Image musicIcon;

	public GameObject soundbutton;

	public Image soundIcon;

	public GameObject restartbutton;

	public Image restartIcon;

	public GameObject botonSolutions;

	public Image bulb;

	public Image aro;

	public Image botonNumSol;

	public Text numSol;

	public GameObject cajaComplete;

	public GameObject helpRotate;

	public Text barra;

	public Text barra1;

	private int contadorUpdate;

	public static bool primeraVez;

	public bool primeraVez1;

	private string SonidoCompleted;

	private string MusicaLevel;

	public static bool chequearFuentes;

	public static bool piezasMovibles;

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

	private void doLevelComplete()
	{
	}

	private void doFinaliza()
	{
	}

	private void doQuitaBotones()
	{
	}

	private void mostrarComplete()
	{
	}

	public void doStandardEvents()
	{
	}

	public void doTestComplete()
	{
	}
}
