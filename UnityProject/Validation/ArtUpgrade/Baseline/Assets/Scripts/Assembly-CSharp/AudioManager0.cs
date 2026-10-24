using UnityEngine;
using UnityEngine.SceneManagement;

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
	private readonly System.Collections.Generic.List<GameObject> sources = new System.Collections.Generic.List<GameObject>();
	private bool initialized;

	private void Awake()
	{
		if (instance != null && instance != this) { Destroy(gameObject); return; }
		instance = this;
		DontDestroyOnLoad(gameObject);
		SceneManager.sceneLoaded += OnSceneLoaded;
		EnsurePreferences();
		RefreshVolumes();
	}

	private void Start()
	{
		if (instance != this) return;
		if (sounds != null) for (int i = 0; i < sounds.Length; i++)
		{
			Sound item = sounds[i];
			if (item == null) continue;
			var sourceObject = new GameObject("Sound_" + i + "_" + item.name);
			var source = sourceObject.AddComponent<AudioSource>();
			source.playOnAwake = false;
			item.SetSource(source);
			DontDestroyOnLoad(sourceObject);
			sources.Add(sourceObject);
		}
		initialized = true;
		PlaySound("Chill");
		PlaySound("Musica");
	}

	private void Update()
	{
		if (!changedVolume) return;
		changedVolume = false;
		ChangeVolume();
	}

	public void PlaySound(string _name)
	{
		EnsurePreferences();
		RefreshVolumes();
		if (!initialized || sounds == null) return;
		foreach (Sound item in sounds)
			if (item != null && item.name == _name) { item.Play(_name == "Click" ? volumenSound : volumenMusic); return; }
	}

	public void StopSound(string _name)
	{
		if (sounds == null) return;
		foreach (Sound item in sounds)
			if (item != null && item.name == _name) { item.Stop(); return; }
	}

	public void ChangeVolume()
	{
		RefreshVolumes();
	}

	private void OnSceneLoaded(Scene loadedScene, LoadSceneMode mode)
	{
		string scene = loadedScene.name;
		if (scene == "Levely5" || scene == "Levely5_basic" || scene == "Levely5_6x6" || scene == "Levely5_7x7")
		{
			StopSound("Chill");
			Destroy(gameObject);
		}
	}

	public static void EnsurePreferences()
	{
		if (PlayerPrefs.GetInt("primeraVez", 0) != 0) return;
		PlayerPrefs.SetInt("primeraVez", 1);
		if (!PlayerPrefs.HasKey("Conf_sound")) PlayerPrefs.SetFloat("Conf_sound", .3f);
		if (!PlayerPrefs.HasKey("Conf_music")) PlayerPrefs.SetFloat("Conf_music", .3f);
	}

	public void RefreshVolumes()
	{
		volumenMusic = PlayerPrefs.GetFloat("Conf_music", .3f);
		volumenSound = PlayerPrefs.GetFloat("Conf_sound", .3f);
		if (sounds == null) return;
		foreach (Sound item in sounds)
		{
			if (item == null) continue;
			item.volume = item.name == "Chill" || item.name == "Musica" ? volumenMusic : volumenSound;
			item.Volume(item.volume);
		}
	}

	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
		foreach (GameObject sourceObject in sources) if (sourceObject != null) Destroy(sourceObject);
		if (instance == this) instance = null;
	}
}
