using UnityEngine;
using UnityEngine.UI;

public class SaveLoadManager : MonoBehaviour
{
	public bool checkSalvar;

	public bool checkCargar;

	public GameObject[] prefabPiezas;

	private GameObject prefabPieza1;

	private string nombre;

	public GameObject objeto;

	public GameObject[] respawns;

	private GameObject[] respawns1;

	private GameObject[] respawns2;

	private GameObject objetoBorde;

	public GameObject prefabBorde;

	private float minY;

	private int sizeTablero;

	private float minimoY;

	public int contadorFuentes;

	public Save save;

	public Transform planoGuia;

	public static int numSoluciones;

	public static int accesoStore;

	public Text textNumS;

	private int solution;

	public GameObject solutionButton;

	public GameObject backButton;

	public GameObject backButton1;

	public GameObject stepsButton;

	public GameObject flecha1;

	public GameObject back1;

	public GameObject steps1;

	public Animator completeOff;

	public Animator solutionOff;

	public GameObject cajaSolution;

	public Animator noMoreOff;

	public GameObject cajaNoMore;

	private int solucionadoAntes;

	private string rebo;

	private string rebo1;

	public GameObject MenuButton;

	public GameObject SoundButton;

	public GameObject MusicButton;

	public GameObject RestartButton;

	public GameObject NextButton;

	public GameObject botonNumSol;

	public Text barra;

	public Text barra1;

	public GameObject lightBulb;

	public Text numSol;

	public GameObject cajaComplete;

	public GameObject cajaRate;

	private string SonidoFuente;

	private string Chill;

	private string click;

	private string MusicaLevel;

	private AudioManager audioManager;

	private bool encontrado;

	private bool modoRewarded;

	private int marc1;

	private int nivelPack;

	private string fijo;

	private string fijo1;

	private int posicionChar;

	private int posicionChar1;

	private int longChar;

	private string resorte;

	private string resortedep;

	private string tipoResorte;

	private string str1;

	private string str2;

	private string str3;

	private string str4;

	private string str5;

	private string str6;

	private bool swstr1;

	private bool swstr2;

	private bool swstr3;

	private bool swstr4;

	private bool swstr5;

	private bool swstr6;

	private bool swresorte;

	private bool swresortedep;

	private string valEncima;

	private string valDebajo;

	private string valMove;

	private string semiCil;

	private string tipoPlant;

	private string longPlant;

	private string confColor;

	private string modoRes3;

	private string conTipoRes3;

	private string configCho1;

	private string configCho2;

	private string hole1;

	private string hole2;

	private string hole3;

	private string hole4;

	private string hole6;

	private string door1;

	private string door2;

	private string door3;

	private string door4;

	private string door6;

	private string conResorte3;

	private bool swhaywalls;

	private int wall1;

	private int wall2;

	private int wall3;

	private int wall4;

	private int configFuente;

	private int configFuenteCentro;

	private int configFB;

	private int palo1;

	private int palo2;

	private int palo3;

	private int palo4;

	private string configPit1;

	private string configPit2;

	private string configPit3;

	private string configPit4;

	private string configRes1;

	private string configRes2;

	private string configRes3;

	private string configRes4;

	private string configAnim;

	private string configTexture;

	private string configLights;

	private string walls;

	private string conF;

	public GameObject customPlane;

	private float nivelPlano;

	private float masbajo;

	private string nombrePieza;

	private bool codigoNew;

	private bool recuperarSuelo;

	private bool noRestart;

	public GameObject panel2;

	public GameObject panel3;

	private int modoSolucion;

	private bool parteMech;

	private bool parteSteps;

	public int contadorTest9;

	public int contadorTestno9;

	private int modoProceso;

	private bool procesarPieza;

	private bool carlos;

	private bool carlos1;

	private bool segundoJuegoNiveles;

	private int alturamin;

	private int altura2;

	private float altura3;

	private float desviacion;

	private string cadenaBase;

	public int contadorTest;

	private bool dosteps;

	private string piezaCarg;

	private bool doSubt;

	public Animator animBack;

	public int esTablet;

	public Image menuIcon;

	public Image backIcon;

	public Image stepsIcon;

	public Image restartIcon;

	public Image musicIcon;

	public Image soundIcon;

	public Image lightbulbIcon;

	public Image aro;

	public Image numsolIcon;

	private void Start()
	{
	}

	public void salvarEscena()
	{
	}

	public void salvarEscenaSolucion()
	{
	}

	public void doCargarEscena()
	{
	}

	public void doCargarEscenaResources()
	{
	}

	public void doCargarEscenaDesign()
	{
	}

	public void doProcessRate()
	{
	}

	public void showRateUs()
	{
	}

	public void doNotNow()
	{
	}

	public void doRate()
	{
	}

	public void doCargarSolucion()
	{
	}

	public void doCargarSolucionResources()
	{
	}

	public void doCargarSolucionDesign()
	{
	}

	public void doProcesoPiezas()
	{
	}

	public void doSizeTablero()
	{
	}

	public void doSubterraneo()
	{
	}

	public void doCodigo(string code1)
	{
	}

	public void doRescatarFijos()
	{
	}

	public void doRescatarConfig()
	{
	}

	private Save CreateSaveGameObject()
	{
		return null;
	}

	public void doRestartLevel()
	{
	}

	public void doRecuperarSoluciones()
	{
	}

	public void showSolution()
	{
	}

	public void cancelSolution()
	{
	}

	public void okSolution()
	{
	}

	public void okSteps()
	{
	}

	public void okBuy()
	{
	}

	public void cancelBuy()
	{
	}

	public void doSolucionLevel()
	{
	}

	public void backbutton()
	{
	}

	public void backbutton1()
	{
	}

	public void backbutton2()
	{
	}

	public void doSolucionLevel1()
	{
	}

	public void doCheckMech()
	{
	}

	public void doTestSolution()
	{
	}

	public void botonCompleteOff()
	{
	}

	public void doCheckComplete()
	{
	}

	public void ConfigureRewarded()
	{
	}

	public void doSalvarTablero()
	{
	}

	public void doCargarTablero()
	{
	}

	public void doBorradoPiezas()
	{
	}

	public void doActivarSuelo()
	{
	}

	public void botonStoreDown()
	{
	}

	public void doActivarBotones()
	{
	}

	public void doDesactivarBotones()
	{
	}

	public void doCambioPanel()
	{
	}

	public void doCambioPanel1()
	{
	}

	public void doMoveBloque1()
	{
	}

	public void doMoveBloque2()
	{
	}

	public void doAnalytics()
	{
	}

	public void doControlPieza()
	{
	}

	public void doPiezaNormal()
	{
	}

	public void doRecuperarMech()
	{
	}

	public void doInicializar()
	{
	}
}
