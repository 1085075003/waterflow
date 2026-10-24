using UnityEngine;

public class SwipeHandler4 : MonoBehaviour
{
	public enum FlickType
	{
		Inertia = 0,
		MoveOne = 1
	}

	public Transform paneles;

	public Transform panel1;

	public Transform panel2;

	public Vector3 posicionInicial;

	public Vector3 posicionFinal;

	public bool hayClick;

	public float smoothSpeed;

	public float distPaneles;

	public float ventanita;

	public bool handleSwipes;

	public bool handleFlicks;

	public float requiredForceForFlick;

	private bool botonPress;

	private float diferenciax;

	private int ventana;

	public FlickType flickType;

	public bool lockToClosest;

	public float maxForce;

	private Vector3 finalPosition;

	private Vector3 startpos;

	private Vector3 endpos;

	private Vector3 oldpos;

	public float length;

	public float startTime;

	public float mouseMove;

	public float force;

	private bool SW;

	private int panel;

	public bool isSwiping => false;

	private void Start()
	{
	}

	private void Update()
	{
	}

	private void HandleMobileSwipe()
	{
	}

	public void HandleButton()
	{
	}

	private void HandleMouseSwipe()
	{
	}

	public void MoverDerecha()
	{
	}

	public void MoverIzquierda()
	{
	}

	public void InicioPaneles()
	{
	}

	public void botonFlechaDerecha()
	{
	}

	public void botonFlechaIzquierda()
	{
	}
}
