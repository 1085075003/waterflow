using UnityEngine;

public class AudioManager : MonoBehaviour
{
	public static AudioManager instance;

	[SerializeField]
	public Sound2[] sounds;

	private int music;

	private int sound;

	private GameObject gameObject1;

	private GameObject gameObject2;

	private GameObject gameObject3;

	private GameObject gameObject4;

	private GameObject gameObject5;

	private GameObject gameObject6;

	private GameObject gameObject7;

	private GameObject gameObject8;

	private float volumenMusic;

	private float volumenSound;

	private void Awake()
	{
	}

	private void Start()
	{
	}

	public void PlaySound(string _name)
	{
	}

	public void StopSound(string _name)
	{
	}

	private void OnLevelWasLoaded(int lvlNum)
	{
	}
}
