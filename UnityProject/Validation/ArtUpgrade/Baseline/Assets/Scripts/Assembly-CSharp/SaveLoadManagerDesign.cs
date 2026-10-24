using UnityEngine;

public class SaveLoadManagerDesign : MonoBehaviour
{
	public bool checkSalvar;

	public bool checkCargar;

	public GameObject[] prefabPiezas;

	private GameObject prefabPieza1;

	private string nombre;

	public GameObject[] respawns;

	public int contadorFuentes;

	private void Start()
	{
	}

	public void salvarEscena()
	{
	}

	public void salvarEscenaSolucion()
	{
	}

	public void doCargarEscena()
	{
	}

	public void doCargarEscenaSolucion()
	{
	}

	private Save CreateSaveGameObject()
	{
		return null;
	}
}
