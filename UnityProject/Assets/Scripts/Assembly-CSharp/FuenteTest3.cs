using UnityEngine;

public class FuenteTest3 : MonoBehaviour
{
	public string nombreObjeto;

	public string proceso;

	public bool activeObject;

	public int colorCubeta;

	private int colorCubeta1;

	public int origen;

	public int direccion;

	public GameObject Cascada1;

	public GameObject Cascada2;

	public GameObject Cascada3;

	public GameObject Cascada4;

	public GameObject esfera1;

	public GameObject esfera2;

	public GameObject esfera3;

	public GameObject esfera4;

	public GameObject esfera6;

	public GameObject esferaCascada1;

	public GameObject esferaCascada2;

	public GameObject esferaCascada3;

	public GameObject esferaCascada4;

	public float espera0;

	public float espera;

	public float espera1;

	public float espera2;

	public GameObject plano1;

	public GameObject plano2;

	public GameObject plano3;

	public GameObject plano4;

	public GameObject planoCentro;

	public GameObject planoCentro1;

	public int dirPlano1;

	public int dirPlano2;

	public int dirPlano3;

	public int dirPlano4;

	public int dirPlanoCentro;

	public GameObject planoCas1;

	public GameObject planoCas2;

	public GameObject planoCas3;

	public GameObject planoCas4;

	private GameObject Agua1;

	private GameObject Agua2;

	private GameObject Agua3;

	private GameObject Agua4;

	private GameObject Agua5;

	private GameObject Agua6;

	private GameObject Agua7;

	private GameObject Agua71;

	private GameObject Agua72;

	private GameObject Agua73;

	private GameObject Agua74;

	public GameObject Agua8;

	public GameObject Agua9;

	public GameObject Agua91;

	public GameObject Agua92;

	public GameObject Agua10;

	public GameObject Agua11;

	private GameObject Agua12;

	private GameObject Agua121;

	private GameObject Agua122;

	private GameObject Agua123;

	public GameObject Agua13;

	public GameObject Agua14;

	public GameObject Agua15;

	public GameObject Agua16;

	public GameObject Lights1;

	public GameObject Lights2;

	public GameObject lit11;

	public GameObject lit12;

	public GameObject lit13;

	public GameObject lit14;

	public GameObject Aguas;

	public GameObject prefabAgua1;

	public GameObject prefabAgua2;

	public GameObject prefabAgua3;

	public GameObject prefabAgua4;

	public GameObject prefabAguaB1;

	public GameObject prefabAguaB2;

	public GameObject prefabAguaB3;

	public GameObject prefabAguaB4;

	public GameObject prefabAguaCAzul;

	public GameObject prefabAguaCVerde;

	public GameObject prefabAguaCZ;

	public GameObject prefabAguaCRo;

	public GameObject prefabAguaDAzul;

	public GameObject prefabAguaDVerde;

	public GameObject prefabAguaDZ;

	public GameObject prefabAguaDRo;

	public GameObject prefabChorroAzul;

	public GameObject prefabChorroVerde;

	public GameObject prefabChorroZ;

	public GameObject prefabChorroRo;

	public GameObject prefabChorroAzulB;

	public GameObject prefabChorroVerdeB;

	public GameObject prefabChorroZB;

	public GameObject prefabChorroRoB;

	public GameObject prefabChorroAzulC;

	public GameObject prefabChorroVerdeC;

	public GameObject prefabChorroZC;

	public GameObject prefabChorroRoC;

	public GameObject prefabChorroAzulD;

	public GameObject prefabChorroVerdeD;

	public GameObject prefabChorroZD;

	public GameObject prefabChorroRoD;

	public GameObject prefabChorroAzulE;

	public GameObject prefabChorroVerdeE;

	public GameObject prefabChorroZE;

	public GameObject prefabChorroRoE;

	public GameObject prefabBigAzul;

	public GameObject prefabBigVerde;

	public GameObject prefabBigZ;

	public GameObject prefabBigRo;

	public GameObject prefabBig1Azul;

	public GameObject prefabBig1Verde;

	public GameObject prefabBig1Z;

	public GameObject prefabBig1Ro;

	public GameObject prefabBig2Azul;

	public GameObject prefabBig2Verde;

	public GameObject prefabBig2Z;

	public GameObject prefabBig2Ro;

	public GameObject prefabBig3Azul;

	public GameObject prefabBig3Verde;

	public GameObject prefabBig3Z;

	public GameObject prefabBig3Ro;

	public GameObject prefabBig11Azul;

	public GameObject prefabBig11Verde;

	public GameObject prefabBig11Z;

	public GameObject prefabBig11Ro;

	public GameObject prefabEstr1Azul;

	public GameObject prefabEstr1Verde;

	public GameObject prefabEstr1Z;

	public GameObject prefabEstr1Ro;

	public GameObject prefabEstr2Azul;

	public GameObject prefabEstr2Verde;

	public GameObject prefabEstr2Z;

	public GameObject prefabEstr2Ro;

	public GameObject prefabFont1;

	public GameObject prefabFont2;

	public GameObject prefabFont3;

	public GameObject prefabFont4;

	public GameObject prefabFont1new1;

	public GameObject prefabFont2new1;

	public GameObject prefabFont3new1;

	public GameObject prefabFont4new1;

	private GameObject prefabEsfera;

	public GameObject prefabEsferaAzul;

	public GameObject prefabEsferaVerde;

	public GameObject prefabEsferaZ;

	public GameObject prefabEsferaRo;

	public GameObject prefabUnion1;

	public GameObject prefabUnion2;

	public GameObject prefabUnion3;

	public GameObject prefabUnion4;

	public GameObject prefabUnionEsfera;

	private GameObject prefabAgua;

	private GameObject prefabAguaB;

	private GameObject prefabAguaC;

	private GameObject prefabAguaD;

	private GameObject prefabFuente;

	private GameObject prefabFuente2;

	private GameObject prefabFuenteBig;

	private GameObject prefabFuenteEstr;

	private GameObject prefabFuenteCentro;

	private GameObject prefabUnion;

	public GameObject prefabFin;

	public GameObject prefabLight1;

	public bool ontrigger;

	public bool esf1;

	public bool esf2;

	public bool esf3;

	public bool esf4;

	public bool esf6;

	public bool activar;

	public bool activarFuente;

	public int tipoPieza;

	private int pit;

	public bool esf1Activada;

	public bool esf2Activada;

	public bool esf3Activada;

	public bool esf4Activada;

	public bool esf6Activada;

	public string tagName;

	private int color;

	public string confColor;

	public Material materialAmarillo;

	public Material materialRojo;

	public Material materialAzul;

	public Material materialColor1_1;

	public Material materialColor1_C;

	public Material materialColor2_1;

	public Material materialColor2_C;

	public Material materialColor3_1;

	public Material materialColor3_C;

	public Material materialColor4_1;

	public Material materialColor4_C;

	private AguasControl3 agscr;

	public bool triggered;

	public GameObject otherObject;

	public GameObject otherObject1;

	public LayerMask mascaraObjeto;

	public string tagReceived;

	public string tagcolor;

	private Vector3 vector2;

	private string colorAChequear;

	private bool checkCollision5;

	private float sphereRadius;

	public int numOrigen5;

	private bool unionAguas5;

	private int contador;

	public bool creaAgua;

	private bool dejarpasar;

	public bool rebose1;

	public bool rebose2;

	public bool rebose3;

	public bool rebose4;

	public bool wall1;

	public bool wall2;

	public bool wall3;

	public bool wall4;

	public int configFuente;

	public int configFuenteB;

	public int configFuenteCentro;

	public string configPit1;

	public string configPit2;

	public string configPit3;

	public string configPit4;

	public string configRes1;

	public string configRes2;

	public string configRes3;

	public string configRes4;

	public string configAnim;

	public string configLi;

	private float altura;

	private float altura1;

	private float altura2;

	private float rotacion;

	private float rotacion1;

	private float rotacion2;

	private float valorx;

	private float valorx1;

	private float valorx2;

	private float valorz;

	private float valorz1;

	private float valorz2;

	private float rot2;

	private float valx2;

	private float valy2;

	private float valz2;

	private float valx3;

	private float valy3;

	private float valz3;

	private float valx4;

	private float valz4;

	private string pit1;

	private string pit2;

	private string pit3;

	private string pit4;

	private string lig1;

	private string lig2;

	private string lig1a;

	private string lig2a;

	private int modofuente3;

	private int modofuente;

	private bool pieza2confuentes;

	public GameObject obj13;

	public GameObject obj14;

	public GameObject obj15;

	public GameObject obj16;

	private float rotay;

	private float distx;

	private float distz;

	public Animator anim;

	private int scriptP;

	private int withLights;

	private bool night;

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

	public void doExit()
	{
	}

	private void doActivarEsfera()
	{
	}

	private void doActivarEsfera0()
	{
	}

	private void doInstanciarAgua()
	{
	}

	private void doInstanciarFuente()
	{
	}

	public void doLights()
	{
	}

	public void doConfigColor1(string lig2)
	{
	}

	public void doConfigColor(string lig2)
	{
	}

	public void doContarFuentes()
	{
	}

	public void doConfigFuentes()
	{
	}

	public void doConfigFuentesB()
	{
	}

	public void activaLight1()
	{
	}

	private void doConfigFuentesCentro()
	{
	}

	public void doFuente1()
	{
	}

	public void doFuente1bis()
	{
	}

	public void doFuente1bis1()
	{
	}

	public void doFuente2()
	{
	}

	public void doFuente3()
	{
	}

	public void doFuente3bis()
	{
	}

	public void doFuente4()
	{
	}

	public void doFuente5()
	{
	}

	public void doFuente6()
	{
	}

	public void doFuente1Centro()
	{
	}

	public void doFuente2Centro()
	{
	}

	public void doFuente3Centro()
	{
	}

	public void doFuente1B()
	{
	}

	public void doFuente2B()
	{
	}

	public void doFuente1A()
	{
	}

	public void doFuente2A()
	{
	}

	public void doFuente5A()
	{
	}

	private void doInstanciarChorros()
	{
	}

	public void doConfigPit(string configP)
	{
	}

	public void doPitA(string configP)
	{
	}

	public void doPitB(string configP)
	{
	}

	private void doTratarColores()
	{
	}

	private void doTratarDirecciones()
	{
	}

	public void doInstanciarAgua1()
	{
	}

	private void doDestroyAgua()
	{
	}

	private void doActivarCascada()
	{
	}

	public void doActivarChorro()
	{
	}

	private void doProcessUnionAguas()
	{
	}

	private void doProcessUnionAguas5()
	{
	}

	private void doProcessPlanosFin()
	{
	}

	private void doCheckCollision5()
	{
	}
}
