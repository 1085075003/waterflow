using UnityEngine;

public class MoveBloqueDesign : MonoBehaviour
{
	public static bool objetoCambiado;

	public bool cambiado;

	private GameObject getTarget;

	public GameObject target1;

	public GameObject esferaGuia;

	public GameObject planoGuia;

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

	public bool cogeObjeto;

	public bool test1;

	public bool test2;

	public LayerMask mask;

	private int giroOld;

	public float varx;

	public float vary;

	public float distancia;

	public LayerMask mascara;

	public LayerMask mascaraSubsuelo;

	public float sphereRadius;

	private Vector3 vector;

	public bool checkCollision;

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

	public float variax;

	public float variay;

	public float variaz;

	public float posiX;

	public float posiY;

	public float posiZ;

	public float arribay;

	public bool salida;

	private void Start()
	{
	}

	private void Update()
	{
	}

	private GameObject ReturnClickedObject(out RaycastHit hit)
	{
		hit = default;
		return null;
	}

	public void doCheckSubsuelo()
	{
	}

	public void doCheckTarget1()
	{
	}
}
