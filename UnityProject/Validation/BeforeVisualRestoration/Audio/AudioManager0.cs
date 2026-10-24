using UnityEngine;

public class AudioManager0 : MonoBehaviour
{
	public static AudioManager0 instance;

	[SerializeField]
	public Sound[] sounds;

	private int music;

	private int sound;

	private GameObject gameObject1;

	private GameObject gameObject2;

	private GameObject gameObject3;

	public static bool changedVolume;

	private float volumenMusic;

	private float volumenSound;

	private int primeraVez;

	private void Awake()
	{
	}

	private void Start()
	{
	}

	private void Update()
	{
	}

	public void PlaySound(string _name)
	{
	}

	public void StopSound(string _name)
	{
	}

	public void ChangeVolume()
	{
	}

	private void OnLevelWasLoaded(int lvlNum)
	{
	}
}
