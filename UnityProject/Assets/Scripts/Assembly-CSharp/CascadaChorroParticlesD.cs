using UnityEngine;

public class CascadaChorroParticlesD : MonoBehaviour
{
	public ParticleSystem part;

	public GameObject esferaSalida;

	public string objectName;

	public string objectParentName;

	public int objectLayer;

	public float object_x;

	public float object_y;

	public float object_z;

	public int altura;

	public float object_y_old;

	public GameObject objeto;

	public int bloqueo;

	public int contador;

	public int cont2;

	public int cont12;

	public int cont12_old;

	public int cont9;

	public int cont9_old;

	public int cont9_1;

	public int cont9_2;

	public int cont11;

	public int contotro;

	public int estado;

	public int subestado;

	public int layer_old;

	public CascadaTestD cascadaD;

	public int contUpdates;

	private Vector3 hitPosition;

	private void Start()
	{
	}

	private void OnEnable()
	{
	}

	public void Update()
	{
	}

	public void OnParticleCollision(GameObject other)
	{
	}
}
