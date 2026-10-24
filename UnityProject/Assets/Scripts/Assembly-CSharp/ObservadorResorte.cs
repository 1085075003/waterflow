using UnityEngine;

public class ObservadorResorte : MonoBehaviour
{
	public string conResorte;

	public string tipoResorte;

	public string valEncima;

	public string valMove;

	public string valMove2;

	public string valDebajo;

	public GameObject prefabBloqueado;

	private GameObject objeto;

	private float diferenciax;

	private float diferenciay;

	private float diferenciaz;

	public float posx;

	public float posy;

	public float posz;

	private float inix;

	private float iniy;

	private float iniz;

	private float inix0;

	private float iniy0;

	private float iniz0;

	private float angle0;

	private float inix1;

	private float iniy1;

	private float iniz1;

	private float angle1;

	private float inix2;

	private float iniy2;

	private float iniz2;

	private float angle2;

	private float inix3;

	private float iniy3;

	private float iniz3;

	private float angle3;

	private float parx;

	private float pary;

	private float parz;

	private float parangle;

	private float parnextx;

	private float parnexty;

	private float parnextz;

	private float parnextangle;

	private float inixreturn;

	private float iniyreturn;

	private float inizreturn;

	private bool ocupado;

	private int numPiezas;

	private int posiMaxTotal;

	private int posiMaxTotal1;

	private int posiMaxTotal2;

	public Material materialTextura1;

	public Material materialTextura2;

	public Material materialTexturaPieza;

	public Material materialTexturaSuelo;

	public float rotationSpeed;

	public float moveSpeed;

	private Vector3 puntoFinal;

	private Vector3 puntoFinal1;

	private Vector3 puntoFinal2;

	public Vector3 puntoInicial1;

	public Vector3 puntoInicial2;

	public Vector3 puntoIni1_2;

	public Vector3 puntoIni2_2;

	public Vector3 puntoIni1_3;

	public Vector3 puntoIni2_3;

	private bool activadoResorte;

	private bool terminadoResorte;

	public bool sw_obj1;

	public bool sw_obj2;

	private float angle;

	private bool checkCollision;

	private bool checkCollision1;

	private bool checkCollision2;

	private bool checkCollision3;

	private bool checkCollision4;

	private bool checkCollisionFijo;

	private bool checkOrigen;

	private bool checkDestino;

	private Vector3 vector;

	private float sphereRadius;

	public LayerMask mascaraBloque;

	public LayerMask mascaraBloque2;

	public LayerMask mascaraBloque3;

	public LayerMask mascaraSuelo;

	public LayerMask mascaraPlano;

	private bool sitioLibre;

	private float valorx;

	private float valory;

	private float valorz;

	private float variax;

	private float variay;

	private float variaz;

	private float varangle;

	private int comprueba;

	private GameObject aguas;

	private int chorro;

	private GameObject otherObject;

	public GameObject objeto1;

	public GameObject objeto2;

	public GameObject objeto3;

	public GameObject objeto4;

	public GameObject objetoSuelo0;

	public GameObject objetoSuelo1;

	public GameObject objetoSuelo2;

	public GameObject objetoSuelo3;

	public GameObject objetoSuelo4;

	private GameObject objeto1_1;

	private string tipoResorte1;

	private string tipoResorte2;

	private string tipoResorte3;

	public string tipoResor;

	public string tipoResor1;

	public string tipoResor2;

	public string tipoResor3;

	private string conResorte1;

	private string conResorte2;

	private string conResor;

	private bool swResorte2;

	private int proceso;

	public bool terminadoResorte1;

	public bool terminadoResorte2;

	public bool inicioRes2;

	public bool terminado1;

	private float currentAngle;

	private float anglereturn;

	private float anglediferenciay;

	private bool finRotacion;

	public int position;

	public int position_old;

	private bool sw_hayConResorte2;

	public string tipo1;

	public string tipo2;

	public string tipoResorte1_1;

	public string tipoResorte1_2;

	public string tipoResorte1_3;

	public string tipoResorte2_1;

	public string tipoResorte2_2;

	public string tipoResorte2_3;

	public bool sw_hayresorte2;

	public bool sw_hayresorte3;

	public bool sw_hayresorte1_2;

	public bool sw_hayresorte1_3;

	public bool sw_hayresorte2_2;

	public bool sw_hayresorte2_3;

	private bool sigueMovimiento;

	private bool hacerMovimiento;

	public int managerResorte;

	public int managerResorte_old;

	public int positionRes;

	public int positionRes_old;

	private int managerR1;

	private int managerR1_old;

	private int managerR2;

	private int managerR2_old;

	private int managerR3;

	private int managerR3_old;

	private int managerR4;

	private int managerR4_old;

	private int managerR5;

	private int managerR5_old;

	private int managerR6;

	private int managerR6_old;

	private int managerR7;

	private int managerR7_old;

	private int managerR8;

	private int managerR8_old;

	private bool validaOk;

	private int posiMax;

	private int posiMin;

	private bool validarmove;

	public int numPie;

	private bool primeraVezAguas;

	private bool prineraVezResorte2;

	public string testsound;

	private int contadorUpdate;

	private int bloqueoSuelo;

	private string SoundPiedra;

	public AudioManager audioManager;

	public int check1;

	public int check2;

	public int check3;

	public int check4;

	public int check5;

	public int check6;

	public int check7;

	public int check8;

	public int check9;

	public int check10;

	public int check11;

	public bool sueloBloqueado;

	public bool activadoResorteok;

	public int cont1;

	private void Start()
	{
	}

	private void Update()
	{
	}

	public void doCambioResorte()
	{
	}

	public void doPrevio()
	{
	}

	public void doResorte()
	{
	}

	public void doValidarPosActual()
	{
	}

	public void doValidarRotacion()
	{
	}

	public void doValidarMovimientos0()
	{
	}

	public void doValidarMovimiento1()
	{
	}

	public void doValidarMovimiento2()
	{
	}

	public void doValidarMovimientos1()
	{
	}

	public void doValidarMovimientos2()
	{
	}

	public void doValidarPos1()
	{
	}

	public void doValidarPos2()
	{
	}

	public void doValidarPos3()
	{
	}

	public void doRotation()
	{
	}

	public void doMove()
	{
	}

	public void doPuntoFinal1()
	{
	}

	public void doCalPuntoNext()
	{
	}

	public void doValidarNext()
	{
	}

	public void doNumPiezasEncima()
	{
	}

	public void docheckPlanoguia()
	{
	}

	public void doBloquearSuelo()
	{
	}

	public void doCheckCollision(float valorx, float valory, float valorz)
	{
	}

	private void doCheckCollision1()
	{
	}

	public void doCheckCollision2(float valorx, float valory, float valorz)
	{
	}

	public void doCheckCollision3(float valorx, float valory, float valorz)
	{
	}

	private void doCheckCollision4()
	{
	}

	public void doInactivarAguas()
	{
	}

	public void doActivarAguas()
	{
	}

	public void doChangeManager()
	{
	}

	public void doInactivarPieza()
	{
	}

	public void doActivarPieza()
	{
	}

	public void doActivarSuelo()
	{
	}

	public void doInicializarContPiezas()
	{
	}

	public void doSumaContPiezas()
	{
	}

	public void doProcesoContPiezas()
	{
	}

	public void doActivaRes()
	{
	}

	public void doInactivaRes()
	{
	}
}
