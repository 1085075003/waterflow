using UnityEngine;

public class UIManagerScript1 : MonoBehaviour
{
	public TextAsset asset;

	private AudioManager audioManager;

	public static bool giroDerecha;

	public static bool giroIzda;

	public GameObject mainCamera;

	public bool checkSalvar;

	public bool checkCargar;

	public GameObject[] prefabPiezas;

	private GameObject prefabPieza1;

	private string nombre;

	public GameObject[] respawns;

	public GameObject scr;

	public static string streamingAssetsPath => null;

	private void Start()
	{
	}

	private void Update()
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

	public void salvarEscena()
	{
	}

	public void cargarEscena()
	{
	}

	public void doCargarEscena()
	{
	}

	private Save CreateSaveGameObject()
	{
		return null;
	}

	private string GetStreamingAssetsPath()
	{
		return null;
	}

	public void doTest1()
	{
	}
}
