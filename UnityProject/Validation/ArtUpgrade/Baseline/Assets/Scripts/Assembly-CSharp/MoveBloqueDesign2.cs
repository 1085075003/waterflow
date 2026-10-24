using UnityEngine;

public class MoveBloqueDesign2 : MonoBehaviour
{
	public static bool objetoCambiado;

	public bool cambiado;

	public GameObject getTarget;

	public GameObject esferaCentro;

	public GameObject target1;

	public GameObject esferaGuia;

	public GameObject planoGuia;

	public GameObject planoGuia1;

	private bool isMouseDragging;

	private Vector3 offsetValue;

	private Vector3 positionOfScreen;

	private Vector3 hitPosition;

	private Vector3 hitPosition1;

	private Vector3 hitPosition2;

	private Vector3 hitPosition3;

	private Vector3 hitPosition2Old;

	private Vector3 desiredPosition1;

	public float smoothSpeed;

	public float smoothSpeed1;

	public float smoothSpeed2;

	public bool cogeObjeto;

	public bool test1;

	public bool test2;

	public LayerMask mask;

	private int giroOld;

	public float varx;

	public float vary;

	public float distancia;

	public LayerMask mascara;

	public LayerMask mascaraSuelo;

	public LayerMask mascaraSuelo1;

	public LayerMask mascaraSuelo2;

	public LayerMask mascaraAlturaMax;

	public LayerMask mascaraSubsuelo;

	public LayerMask mascaraDefault;

	public float sphereRadius;

	private Vector3 vector;

	public bool checkCollision;

	public bool checkCollision3;

	public bool checkCollision4;

	public bool checkCollision5;

	public bool checkCollision7;

	public bool checkCollision8;

	public bool checkCollision9;

	public bool checkCollisionSubsuelo;

	public bool checkCollisionTarget1;

	public bool check1;

	public bool check2;

	public bool check3;

	public GameObject esferita;

	private bool faseA;

	private float orix;

	private float oriy;

	private float oriz;

	private float variablex;

	private float variablez;

	public float desfase;

	private float desfase_old;

	public float bajaRayo;

	public float subeRayo;

	public float ratonx;

	public float ratonY;

	public float posicionlimiteY;

	public float posicionlimitex1;

	public float posicionlimitex2;

	public float hitx;

	public float hity;

	public float hitz;

	public float hitx2;

	public float hity2;

	public float hitz2;

	public float variax;

	public float variay;

	public float variaz;

	public float posiX;

	public float posiY;

	public float posiZ;

	public float arribay;

	public bool salida;

	public int contadorDown;

	public int contadorUp;

	public int contadorDragging;

	public bool checka1;

	public bool checka2;

	public bool checka3;

	public bool checka4;

	public bool checka5;

	public bool checka6;

	public int capa;

	public bool checkSuelo1;

	public bool fijarPieza;

	private Vector3 puntoFinal;

	public int contadorFijar;

	public static bool parada;

	public static float minX;

	public static float minX2;

	public static float minZ;

	public static float minZ2;

	public Save1 save1;

	public int salvarMoves;

	private int move;

	private int oricx;

	private int oricy;

	private int oricz;

	private int descx;

	private int descy;

	private int descz;

	private string xcept;

	public bool inicioFijacion;

	public bool noDesbloqueado;

	public GameObject targetFin;

	public Vector3 vector7;

	public Vector3 vector9;

	public bool downProcess;

	public int fijacionPieza;

	private bool moviendoRuta1;

	private bool procesoCheck;

	private bool checkFija;

	private int buscarC;

	public int paso;

	public int numpasos;

	public Vector3 punto0;

	public Vector3 punto1;

	public Vector3 punto2;

	public Vector3 punto3;

	public Vector3 punto4;

	public bool MoviendoBloque;

	public float posx_ini;

	public float posy_ini;

	public float postotal_ini;

	public float posx;

	public float posy;

	public float postotal;

	public float cambio;

	public float margen;

	public bool moverGuia;

	public bool checkGuia;

	public int contadorMax;

	public float aumento;

	public float difVariay;

	public float incremento;

	public GameObject targetDebajo;

	public GameObject targetpadre;

	public bool moveArriba;

	private bool noPunto3;

	public int modoSolucion;

	private int faseSol;

	private bool cambiofase;

	public float valor1x;

	public float valor1y;

	public float valor1z;

	public float valor3x;

	public float valor3y;

	public float valor3z;

	public float valor4x;

	public float valor4y;

	public float valor4z;

	public float valor5x;

	public float valor5y;

	public float valor5z;

	public int altura3;

	private GameObject MCamera;

	private GameObject padre;

	public int camino;

	public int casocamino;

	private float rotation;

	private bool caminoLibre;

	private bool caminoLibreB;

	public int primeraAltura;

	public int ultimaAltura;

	private int numPiezas;

	public float valor5y_1;

	public float valor5y_2;

	private float parx;

	private float pary;

	private float parz;

	public int alturaMax7;

	public int alturaMax8;

	public int altura7;

	public int altura8;

	public float alturaMin7;

	public float alturaMin8;

	public int diferenciaAltura;

	public int alturaGuia;

	public bool conRes3;

	public bool espera;

	private bool segundoJuegoNiveles;

	public static bool botonPL;

	public static bool botonLP;

	public static string tipoPL;

	public static string tipoLP;

	public static bool botonSem;

	public static int numColor;

	private GameObject obj1;

	public static int lado1;

	public static int lado2;

	public static int lado3;

	public static int lado4;

	private void Start()
	{
	}

	private void Update()
	{
	}

	private void HandleMouseSwipe()
	{
	}

	public void doEnd()
	{
	}

	public void doProcess1()
	{
	}

	public void doProcessMove()
	{
	}

	public void doFijarPieza()
	{
	}

	public void doCorregirPuntoFinal()
	{
	}

	public void doCheckSubsuelo()
	{
	}

	public void doCheckTarget1()
	{
	}

	public void doCheckTarget3()
	{
	}

	private GameObject ReturnClickedObject(out RaycastHit hit)
	{
		hit = default;
		return null;
	}

	public void doBloquearCascadasTarget()
	{
	}

	public void doDesbloquearCascadasTarget()
	{
	}

	public void doCheckAlturaMax(float fx, float fy, float fz)
	{
	}

	public void checkMaxAltura()
	{
	}

	public void doCheckColl7()
	{
	}

	public void salvarSolStart()
	{
	}

	public void salvarSolEnd()
	{
	}

	public void salvarSolMech(int param)
	{
	}

	public void salvarDeletePieza()
	{
	}

	public void doSalvarOrigen(float valx, float valy, float valz)
	{
	}

	public void doSalvarDestino(float valx, float valy, float valz)
	{
	}

	public void doEspera()
	{
	}

	public void cargarSave1()
	{
	}

	public void doCargarSolucionResources()
	{
	}

	public void DoMoverAbajo()
	{
	}

	public void doMovimiento(Vector3 puntoFinal)
	{
	}

	public void doCheckFija()
	{
	}

	public void doCheckFija1()
	{
	}

	public void doCheckArribaFija()
	{
	}

	public void doCheckArriba()
	{
	}

	public void doCheckColl5()
	{
	}

	public void doBuscarCamino()
	{
	}

	public void checkCamino1()
	{
	}

	public void checkCamino2()
	{
	}

	public void checkCamino3()
	{
	}

	public void checkCamino4()
	{
	}

	public void doCheckCamino()
	{
	}

	public void doCheckDebajo()
	{
	}

	public void doCheckColl3()
	{
	}

	public void doPlantas()
	{
	}

	public void doSemicirculo()
	{
	}

	public void doLongPlantas()
	{
	}

	public void doCubeta()
	{
	}

	public void doColor()
	{
	}

	public void doReemplazar(GameObject obj1)
	{
	}
}
