using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class UIManagerScriptDesign : MonoBehaviour
{
	[CompilerGenerated]
	private sealed class _003CRestartCoroutine_003Ed__358 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public UIManagerScriptDesign _003C_003E4__this;

		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			get
			{
				return null;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return null;
			}
		}

		[DebuggerHidden]
		public _003CRestartCoroutine_003Ed__358(int _003C_003E1__state)
		{
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
		}

		private bool MoveNext()
		{
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
		}
	}

	public TextAsset asset;

	public Button botonPFija;

	public Button botonPAzul;

	public Button botonPVerde;

	public Button botonPZ;

	public Button botonPRojo;

	public Button botonPAmarillo;

	public Button botonPNaranja;

	public Button botonPRosa;

	public Button botonPMarino;

	public Button botonPNoColor;

	public Button botonPTextura1;

	public Button botonPTextura2;

	public Button botonPTextura3;

	public Button botonPTextura4;

	public Button botonPTextura5;

	public Button botonPTextura6;

	public Button botonPTextura7;

	public Button botonPTextura8;

	public Button botonPTextura9;

	public Button botonPTextura10;

	public Button botonPTextura11;

	public Button botonPTextura12;

	public Button botonPTextS1;

	public Button botonPTextS2;

	public Button botonPTextS3;

	public Button botonPTextS4;

	public Button botonPTextS5;

	public Button botonPTextS6;

	public Button botonPTextS7;

	public Button botonPTextS8;

	public Button botonPlantas1;

	public Button botonPlantas2;

	public Button botonPlantas3;

	public Button botonPlantas4;

	public Button botonPlantas5;

	public Button botonPlantas6;

	public Button botonPlantas7;

	public Button botonPlantas8;

	public Button botonPlantas9;

	public Button botonPlantas10;

	public Button botonPlantas11;

	public Button botonPlantas12;

	public Button botonPlantas13;

	public Button botonPlantas14;

	public Button botonPlantas15;

	public Button botonPlantas16;

	public Button botonPlantas17;

	public Button botonPlantas18;

	public Button botonPlantas19;

	public Button botonLongPlantas1;

	public Button botonLongPlantas2;

	public Button botonLongPlantas3;

	public Button botonTextAll;

	public Button botonTextPL;

	public Button botonTextSem;

	public Button botonTextPLAll;

	[SerializeField]
	private Button[] botonF1;

	[SerializeField]
	private Button[] botonF2;

	[SerializeField]
	private Button[] botonF3;

	[SerializeField]
	private Button[] botonF4;

	[SerializeField]
	private GameObject[] icons;

	private bool[] cubF1;

	private bool[] cubF2;

	private bool[] cubF3;

	private bool[] cubF4;

	public Button botonPResorte0;

	public Button botonPResorte1;

	public Button botonPResorte2;

	public Button botonPResorte3;

	public Button botonPResorte4;

	public Button botonPResorte5;

	public Button botonPResorte6;

	public Button botonPResorte7;

	public Button botonPResorte8;

	public Button botonPResorte9;

	public Button botonPResorteB0;

	public Button botonPResorteB1;

	public Button botonPResorteB2;

	public Button botonPResorteB3;

	public Button botonPResorteB4;

	public Button botonPResorteB5;

	public Button botonPResorteB6;

	public Button botonPResorteB7;

	public Button botonPResorteB8;

	public Button botonPResorteB9;

	public Button botonConResorte0;

	public Button botonConResorte1;

	public Button botonConResorte2;

	public Button botonConResorte3;

	public Button botonConResorte4;

	public Button botonConResorte5;

	public Button botonConResorte6;

	public Button botonConResorte7;

	public Button botonConResorteB0;

	public Button botonConResorteB1;

	public Button botonConResorteB2;

	public Button botonConResorteB3;

	public Button botonConResorteB4;

	public Button botonConResorteB5;

	public Button botonConResorteB6;

	public Button botonConResorteB7;

	public GameObject PanelLaterales;

	public GameObject PanelLateralesFuente;

	public GameObject PanelNormales;

	public GameObject PanelFuentesH;

	public GameObject PanelFuentesP;

	public GameObject PanelFuentesT;

	public GameObject PanelHierbas;

	public GameObject PanelPiedras;

	public GameObject PanelArcos;

	public GameObject PanelTierras;

	public GameObject PanelPFuentes;

	public GameObject PanelPEleva;

	public GameObject PanelPEleva1;

	public GameObject PanelCubetasA;

	public GameObject PanelCubetasB;

	public GameObject PanelCubetasB1;

	public GameObject PanelCubetasB2;

	public GameObject PanelCubetasB3;

	public GameObject PanelSuelo;

	public GameObject PanelCubetasC;

	public GameObject PanelCubetasD;

	public GameObject PanelCubetasE;

	public GameObject PanelCubetasF;

	public GameObject PanelCubetasG;

	public GameObject PanelCubetasH;

	public GameObject PanelConfCub;

	public int colorPieza;

	public string nombrePieza1;

	public Text texto;

	public bool piezaFija;

	public bool textura1;

	public bool textura2;

	public bool textura3;

	public bool textura4;

	public bool textura5;

	public bool textura6;

	public bool textura7;

	public bool textura8;

	public bool textura9;

	public bool textura10;

	public bool textura11;

	public bool textura12;

	public bool textS1;

	public bool textS2;

	public bool textS3;

	public bool textS4;

	public bool textS5;

	public bool textS6;

	public bool textS7;

	public bool textS8;

	public bool plantas1;

	public bool plantas2;

	public bool plantas3;

	public bool plantas4;

	public bool plantas5;

	public bool plantas6;

	public bool plantas7;

	public bool plantas8;

	public bool plantas9;

	public bool plantas10;

	public bool plantas11;

	public bool plantas12;

	public bool plantas13;

	public bool plantas14;

	public bool plantas15;

	public bool plantas16;

	public bool plantas17;

	public bool plantas18;

	public bool plantas19;

	public bool longplant1;

	public bool longplant2;

	public bool longplant3;

	public bool hayResorte;

	public bool hayResorteB;

	public bool hayConResorte;

	public string resorte;

	public string conResorte;

	public string conex;

	public string conex1;

	public string nombrePieza;

	public GameObject[] prefabPiezas;

	private GameObject prefabPieza1;

	public GameObject[] respawns;

	public GameObject[] respawns1;

	private string click;

	private AudioManager audioManager;

	public static bool giroDerecha;

	public static bool giroIzda;

	public bool checkSalvar;

	public bool checkCargar;

	public GameObject suelo5x5;

	public GameObject suelo6x6;

	public GameObject suelo7x7;

	public GameObject suelo8x8;

	public int board;

	public string nombreCol;

	public int numCol;

	public int numColorx;

	private bool encontrado;

	private string rebo;

	private string rebo1;

	private string letraSuelo;

	private string numSuelo;

	private string nivelSuelo;

	public string combi;

	private Vector3 vector;

	private float sphereRadius;

	public LayerMask mascaraPieza;

	public LayerMask mascaraSuelo;

	public float altura1;

	private bool codigo;

	private bool registroMech;

	private int modoSave;

	public Save save;

	public Save save1;

	public Save save2;

	public float arribay;

	public float subeRayo;

	public LayerMask mascaraDefault;

	public float valorx;

	public float valory;

	public float valorz;

	public bool checkFija;

	public bool checkCollision3;

	public int piezasArriba;

	private int numEncontrado;

	private bool segundoJuegoNiveles;

	public GameObject mainCamera;

	public GameObject light;

	private int modoNight;

	private int numArco;

	private bool buttonAll;

	private bool botonPLAll;

	public Save1 saveX;

	private void Awake()
	{
	}

	private void Start()
	{
	}

	public void RotacionDerecha()
	{
	}

	public void RotacionIzquierda()
	{
	}

	public void botonMenuDown()
	{
	}

	public void botonNextDown()
	{
	}

	public void botonHomeDown()
	{
	}

	public void Laterales()
	{
	}

	public void LateralesFuente()
	{
	}

	public void Normales()
	{
	}

	public void FuentesH()
	{
	}

	public void FuentesP()
	{
	}

	public void FuentesT()
	{
	}

	public void Hierbas()
	{
	}

	public void Piedras()
	{
	}

	public void Confcub()
	{
	}

	public void Arcos()
	{
	}

	public void Tierras()
	{
	}

	public void PFuentes()
	{
	}

	public void PEleva()
	{
	}

	public void PEleva1()
	{
	}

	public void Night()
	{
	}

	public void CubetasA()
	{
	}

	public void CubetasB()
	{
	}

	public void CubetasB1()
	{
	}

	public void CubetasB2()
	{
	}

	public void CubetasB3()
	{
	}

	public void Suelo()
	{
	}

	public void CubetasC()
	{
	}

	public void CubetasD()
	{
	}

	public void CubetasE()
	{
	}

	public void CubetasF()
	{
	}

	public void CubetasG()
	{
	}

	public void CubetasH()
	{
	}

	public void PiezaFija()
	{
	}

	public void doTexturas(GameObject objeto)
	{
	}

	public void doPlantas(GameObject objeto)
	{
	}

	public void doPlantas(int num)
	{
	}

	public void doLongPlantas(int num)
	{
	}

	public void doTexturaSFalse()
	{
	}

	public void doPlantasFalse()
	{
	}

	public void doTextS1()
	{
	}

	public void doTextS2()
	{
	}

	public void doTextS3()
	{
	}

	public void doTextS4()
	{
	}

	public void doTextS5()
	{
	}

	public void doTextS6()
	{
	}

	public void doTextS7()
	{
	}

	public void doTextS8()
	{
	}

	public void doTextura1()
	{
	}

	public void doTextura2()
	{
	}

	public void doTextura3()
	{
	}

	public void doTextura4()
	{
	}

	public void doTextura5()
	{
	}

	public void doTextura6()
	{
	}

	public void doTextura7()
	{
	}

	public void doTextura8()
	{
	}

	public void doTextura9()
	{
	}

	public void doTextura10()
	{
	}

	public void doTextura11()
	{
	}

	public void doTextura12()
	{
	}

	public void doResorte0(int resor)
	{
	}

	public void doResorteB0(int resor)
	{
	}

	public void doConResorte0(int conexion)
	{
	}

	public void doConResorteB0(int conexion1)
	{
	}

	public void PiezaColor(int color)
	{
	}

	public void IncluirPiezaArco(string nombrePieza)
	{
	}

	public void IncluirPiezaColor(string nombrePieza)
	{
	}

	public void IncluirPiezaColor2(string nombrePieza)
	{
	}

	public void IncluirPieza(string pieza)
	{
	}

	public void configPieza(int entero)
	{
	}

	public void configSuelo(int entero)
	{
	}

	public void configPieza1(int entero)
	{
	}

	public void doCodificar()
	{
	}

	public void BorrarPieza()
	{
	}

	public void BorrarPieza6x6()
	{
	}

	public void BorrarPieza7x7()
	{
	}

	public void doDesactivaPaneles()
	{
	}

	public void salvarEscena()
	{
	}

	public void salvarSolucion()
	{
	}

	private Save CreateSaveGameObject()
	{
		return null;
	}

	public void doCodigo(string nombre)
	{
	}

	public void t2All()
	{
	}

	public void t3All()
	{
	}

	public void t4All()
	{
	}

	public void t5All()
	{
	}

	public void t6All()
	{
	}

	public void t7All()
	{
	}

	public void t8All()
	{
	}

	public void t11All()
	{
	}

	public void t12All()
	{
	}

	public void tS1All(string textura)
	{
	}

	public void doPlant2(string tipoPlanta)
	{
	}

	public void doPlantCubeta1()
	{
	}

	public void doPlantCubeta2(string tipoPlanta)
	{
	}

	public void doPiezasArriba()
	{
	}

	public void doCheckColl3()
	{
	}

	public void plant1All()
	{
	}

	public void plant2All()
	{
	}

	public void t11CubetasAll()
	{
	}

	public void doPlant(string tipoPlantas)
	{
	}

	public void board5x5()
	{
	}

	public void board6x6()
	{
	}

	public void board7x7()
	{
	}

	public void board8x8()
	{
	}

	public void salvarSolucionMech()
	{
	}

	public void cargarSave1()
	{
	}

	public void cargarSave2()
	{
	}

	public void mergeFiles()
	{
	}

	public void doCargarEscenaDesign()
	{
	}

	public void doCargarFichss()
	{
	}

	public void salvarPiezasMech()
	{
	}

	public void piezasSave()
	{
	}

	public void piezasSave1()
	{
	}

	public void doCargarPiezasMech()
	{
	}

	public void doSalvarFile()
	{
	}

	public void doButtonAll()
	{
	}

	public void doPlantsDesign()
	{
	}

	public void doPlantsAll()
	{
	}

	public void doSemicirculo()
	{
	}

	public void doFila1(int num)
	{
	}

	public void doRestart()
	{
	}

	[IteratorStateMachine(typeof(_003CRestartCoroutine_003Ed__358))]
	private IEnumerator RestartCoroutine()
	{
		return null;
	}

	public void doPlantas1()
	{
	}

	public void doColor(GameObject objeto)
	{
	}

	public void doColorDesign(int num)
	{
	}

	public void doF1(int num)
	{
	}

	public void doF2(int num)
	{
	}

	public void doF3(int num)
	{
	}

	public void doF4(int num)
	{
	}

	public void doF1False()
	{
	}

	public void doF2False()
	{
	}

	public void doF3False()
	{
	}

	public void doF4False()
	{
	}

	public void cargarSaveX()
	{
	}

	public void cargarSave1Resources()
	{
	}
}
