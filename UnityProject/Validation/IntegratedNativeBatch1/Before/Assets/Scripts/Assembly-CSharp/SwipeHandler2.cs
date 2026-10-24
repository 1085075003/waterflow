using UnityEngine;

public class SwipeHandler2 : MonoBehaviour
{
	public enum FlickType
	{
		Inertia = 0,
		MoveOne = 1
	}

	public Transform EsferaCamera;

	public Vector3 posicionInicial;

	public Vector3 posicionFinal;

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

	private Vector3 finalPosition;

	private Vector3 startpos;

	private Vector3 endpos;

	private Vector3 oldpos;

	private float length;

	private float startTime;

	private float mouseMove1;

	private float force;

	private bool SW;

	private bool finished;

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
