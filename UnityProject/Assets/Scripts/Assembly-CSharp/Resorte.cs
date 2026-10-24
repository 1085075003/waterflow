using UnityEngine;

public class Resorte : MonoBehaviour
{
	public GameObject objeto;

	public Animator animResorte;

	public bool depura1;

	public float tiempo1;

	public float tiempo2;

	private float contUpdate;

	public bool checkUpdate;

	private Vector3 vector;

	private float sphereRadius;

	public LayerMask mascaraSuelo;

	public string conResorte;

	public float valorx;

	public float valory;

	public float valorz;

	public bool checkCollision;

	public bool checkCollision_old;

	private int contador;

	public float altura;

	public float variacion;

	public bool esPiezaFija;

	public bool esTextura2;

	public bool esTextura3;

	public bool esTextura4;

	public bool esTextura5;

	public bool esTextura6;

	public bool esTextura7;

	public bool esTextura8;

	public Material materialFijo;

	public Material materialTextura2;

	public Material materialTextura3;

	public Material materialTextura4;

	public Material materialTextura5;

	public Material materialTextura6;

	public Material materialTextura7;

	private GameObject otherObject;

	private string conResorte1;

	private string conResorte2;

	private bool hayDependencias;

	private bool activar;

	public bool chequear;

	private string SoundResorte;

	private AudioManager audioManager;

	private void Start()
	{
	}

	private void Update()
	{
	}

	public void doDependencias()
	{
	}

	public void doBajarPieza()
	{
	}

	public void doCheckCollision()
	{
	}

	public void doActivarMech()
	{
	}

	public void doDesactivarMech()
	{
	}
}
