using UnityEngine;

public class MoveBloque : MonoBehaviour
{
	public static bool objetoCambiado;

	public static int modoSolucion;

	private int modoSolucion_old;

	[SerializeField]
	public static bool MoviendoBloque;

	public static bool moveActivo;

	public static bool inicializar;

	public bool moveactivo_old;

	public int fijacionPieza;

	public bool cambiado;

	public GameObject getTarget;

	public GameObject esferaCentro;

	public GameObject aguas;

	public GameObject target1;

	public GameObject target1_old;

	public GameObject target2;

	public GameObject targetFin;

	public GameObject targetDebajo;

	public GameObject targetpadre;

	public GameObject targetpadre1;

	public GameObject targetpadretest;

	public GameObject esferaGuia;

	public GameObject planoGuia;

	public GameObject planoGuia1;

	public GameObject planoGuia2;

	public bool isMouseDragging;

	public bool isMouseDragging1;

	private Vector3 hitPosition;

	public Vector3 hitPosition1;

	public float smoothSpeed;

	public float smoothSpeed1;

	public float smoothSpeed2;

	public float smoothSpeed3;

	public Vector2 mousePosition2;

	public LayerMask mask;

	public float varx;

	public float vary;

	public float distancia;

	public LayerMask mascara;

	public LayerMask mascaraSuelo;

	public LayerMask mascaraSuelo1;

	public LayerMask mascaraSuelo2;

	public LayerMask mascaraAlturaMax;

	public LayerMask mascaraBloque;

	public LayerMask mascaraDefault;

	public float sphereRadius;

	private Vector3 vector;

	public Vector3 vector1;

	public Vector3 vector4;

	public bool checkCollision;

	public bool checkCollisionSubsuelo;

	public bool checkCollisionTarget1;

	public bool checkCollisionTarget2;

	public bool checkCollisionTarget3;

	public bool checkCollisionTarget4;

	public bool checkCollisionTarget5;

	public bool checkCollision3;

	public bool checkCollision4;

	public bool checkCollision5;

	public bool checkCollision7;

	public bool checkCollision8;

	public bool checkCollision9;

	private bool caminoLibre;

	private bool caminoLibreB;

	private bool noPunto3;

	private bool procesoCheck;

	private bool checkFija;

	public GameObject esferita;

	public GameObject esferita3;

	private bool faseA;

	public float orix;

	public float oriy;

	public float oriz;

	public float bajaRayo;

	public float subeRayo;

	public float hitx;

	public float hity;

	public float hitz;

	public float variax;

	public float variay;

	public float variaz;

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

	public float arribay;

	public bool checka7;

	public int capa;

	public bool checkSuelo1;

	public bool checkPieza;

	public static bool fijarPieza;

	public Vector3 puntoFinal;

	public Vector3 puntoFinal1;

	public Vector3 punto0;

	public Vector3 punto1;

	public Vector3 punto2;

	public Vector3 punto3;

	public Vector3 punto4;

	private GameObject MCamera;

	private GameObject padre;

	public Vector3 puntoComienzoFijacion;

	public bool inicioFijacion;

	public bool noDesbloqueado;

	public float diferencia;

	public float porcentajeDiferencia;

	public float porcentajeDiferencia1;

	public float factorDiferencia;

	public float factorDiferencia1;

	public static bool parada;

	private float arribaCheck;

	public float minX;

	public float maxX;

	public float minZ;

	public float maxZ;

	public float minX2;

	public float minZ2;

	public float maxX2;

	public float maxZ2;

	private bool planoG;

	public bool checkResorte;

	public bool encontrado;

	public int altura;

	public bool configLevel;

	public static bool config;

	private string SoundBlock;

	public int paso;

	public int numpasos;

	public Material material1;

	public Material material2;

	public Renderer rend0;

	public int primeraAltura;

	public int ultimaAltura;

	public float valor5y_1;

	public float valor5y_2;

	public int altura3;

	private bool moviendoRuta1;

	public int camino;

	public int casocamino;

	private float rotation;

	public float posx_ini;

	public float posy_ini;

	public float postotal_ini;

	public float posx;

	public float posy;

	public float postotal;

	public float cambio;

	public float margen;

	public bool moverGuia;

	public bool moveArriba;

	public bool moviendoArriba;

	public bool downProcess;

	private float variax_old;

	private float variay_old;

	private float variaz_old;

	private float parx;

	private float pary;

	private float parz;

	public int alturaReal;

	public int alturaMax7;

	public int alturaMax8;

	public int altura7;

	public int altura8;

	public float alturaMin7;

	public float alturaMin8;

	public Vector3 vector7;

	public Vector3 vector9;

	public float incremento;

	public int diferenciaAltura;

	public int alturaGuia;

	private int buscarC;

	private float paramy;

	public bool checkGuia;

	public int contadorMax;

	public float aumento;

	public float difVariay;

	public static bool conRes3;

	public bool conRes3_8;

	public float coordx_old;

	public float coordx;

	public float coordz_old;

	public float coordz;

	public Save1 save1;

	private int faseSol;

	private bool checkCollisionSolu;

	private int numMove;

	private int numMoveMax;

	private float inix;

	private float iniy;

	private float iniz;

	private float finx;

	private float finy;

	private float finz;

	private string xcept;

	private GameObject piezaCapturada;

	private Vector3 vectorSolu;

	private bool cambiofase;

	public int activadoMech;

	public bool activadaEspera;

	public bool mismoObjeto;

	public bool activoMech;

	public float alturaSolution;

	private string checkname;

	private string checktag;

	private Transform parentTransform;

	private Transform childTransform;

	private GameObject objdel;

	public Animator animBack;

	private AudioManager audioManager;

	private void Start()
	{
	}

	private void Update()
	{
	}

	public void doInicializar()
	{
	}

	private void HandleMobileSwipe()
	{
	}

	private void HandleMouseSwipe()
	{
	}

	public void doProcess1()
	{
	}

	public void doEnd()
	{
	}

	public void doFijarPieza()
	{
	}

	public void doProcessMove()
	{
	}

	public void docheckMoveGuia()
	{
	}

	public void doCheckMech()
	{
	}

	public void doCheckTarget1()
	{
	}

	public void doCheckCollResorte()
	{
	}

	public void doCheckTarget2()
	{
	}

	public void doCheckColl2()
	{
	}

	public void doCheckTarget3()
	{
	}

	public void doCheckTarget4()
	{
	}

	public void doCheckCollisionPieza()
	{
	}

	public void doCheckColl(float vax, float vaz)
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

	public void doCheckFija()
	{
	}

	public void doCheckFija1()
	{
	}

	public void doCheckArribaFija()
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

	public void doCheckColl3()
	{
	}

	public void doCheckColl4()
	{
	}

	public void doCheckArriba()
	{
	}

	public void doCheckColl5()
	{
	}

	public void DoMoverArriba()
	{
	}

	public void DoMoverAbajo()
	{
	}

	public void doMovimiento(Vector3 puntoFinal)
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

	public void doCorregirPuntoFinal()
	{
	}

	public void doCheckDebajo()
	{
	}

	public void HandleSolution()
	{
	}

	public void doCargarSolucionDesign()
	{
	}

	public void doCargarSolucionResources()
	{
	}

	private void HandleSolutionTotal()
	{
	}

	private void HandleStepsSolution()
	{
	}

	public void doProcesoEspera()
	{
	}

	public void doProcess1Solution()
	{
	}

	public void doCapturarPieza()
	{
	}

	public void doProcess2Solution()
	{
	}

	public void doProcess3Solution()
	{
	}

	public void doProcessMoveSolu()
	{
	}
}
