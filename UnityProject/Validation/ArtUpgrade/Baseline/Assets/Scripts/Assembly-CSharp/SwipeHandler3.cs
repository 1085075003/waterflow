using UnityEngine;

public class SwipeHandler3 : MonoBehaviour
{
	public enum FlickType
	{
		Inertia = 0,
		MoveOne = 1
	}

	public Transform EsferaCamera;

	public bool hayClick;

	public float smoothSpeed;

	public float degree;

	public float multiplicador;

	public Vector3 rotationInicial;

	public Vector3 rotationFinal;

	public GameObject getAreaRotation;

	public bool areaCorrecta;

	private int layer_mask;

	private bool inicio;

	private float diferenciaInicial;

	private float diferencia;

	public float porcentajeDiferencia;

	private bool permitirClick;

	private float nextFire;

	public bool handleSwipes;

	public bool handleFlicks;

	public float requiredForceForFlick;

	public static bool girado;

	public FlickType flickType;

	public bool lockToClosest;

	public float maxForce;

	public Vector3 finalPosition;

	public Vector3 startpos;

	public Vector3 endpos;

	public Vector3 oldpos;

	public float length;

	public float startTime;

	public float mouseMove1;

	public float mouseMove2;

	public float force;

	private bool SW;

	public float widthCenter;

	public float heightCenter;

	private bool finished;

	public bool permitidoGiro;

	public bool permitidoGiro1;

	public bool isSwiping1 => false;

	private void Start()
	{
	}

	private void Update()
	{
	}

	private void HandleMobileSwipe()
	{
	}

	private void HandleMouseSwipe()
	{
	}

	public void doComprobarGiro()
	{
	}

	public void doComprobarGiro1()
	{
	}

	public void GirarDerecha()
	{
	}

	public void GirarIzquierda()
	{
	}

	private GameObject ReturnClickedObject1(out RaycastHit hit)
	{
		hit = default;
		return null;
	}

	private void HandleMobileSwipe2()
	{
	}
}
