using UnityEngine;

public class CascadaChorroParticles : MonoBehaviour
{
	public ParticleSystem part;

	public string objectName;

	public string objectParentName;

	public int objectLayer;

	public float object_y;

	public float object_y_old;

	public GameObject objeto;

	public int bloqueo;

	public int contador;

	public int cont2;

	public int cont12;

	public int cont9_1;

	public int cont9_2;

	public int cont11;

	public int contotro;

	public int estado;

	public int layer_old;

	public CascadaTestC cascadaC;

	private Vector3 hitPosition;

	private void Start()
	{
	}

	private void OnEnable()
	{
	}

	public void OnParticleCollision(GameObject other)
	{
	}
}
