using UnityEngine;
using UnityEngine.SceneManagement;

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
	private readonly System.Collections.Generic.List<GameObject> sources = new System.Collections.Generic.List<GameObject>();
	private bool initialized;

	private void Awake()
	{
		if (instance != null && instance != this) { Destroy(gameObject); return; }
		instance = this;
		DontDestroyOnLoad(gameObject);
		SceneManager.sceneLoaded += OnSceneLoaded;
		AudioManager0.EnsurePreferences();
		RefreshVolumes();
	}

	private void Start()
	{
		if (instance != this) return;
		if (sounds != null) for (int i = 0; i < sounds.Length; i++)
		{
			Sound2 item = sounds[i];
			if (item == null) continue;
			var sourceObject = new GameObject("Sound_" + i + "_" + item.name2);
			var source = sourceObject.AddComponent<AudioSource>();
			source.playOnAwake = false;
			item.SetSource(source);
			DontDestroyOnLoad(sourceObject);
			sources.Add(sourceObject);
		}
		initialized = true;
		RefreshVolumes();
		if (PlayerPrefs.GetInt("Soundlevel", 1) == 1) PlaySound("Chill");
	}

	public void PlaySound(string _name)
	{
		if (!initialized || sounds == null) return;
		foreach (Sound2 item in sounds)
			if (item != null && item.name2 == _name) { item.Play(item.volume2); return; }
	}

	public void StopSound(string _name)
	{
		if (sounds == null) return;
		foreach (Sound2 item in sounds)
			if (item != null && item.name2 == _name) { item.Stop(); return; }
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (scene.name == "SelLevel1") Destroy(gameObject);
	}

	// Native Awake: sound preference, music override, +0.2 for fountains/completion.
	public void RefreshVolumes()
	{
		volumenMusic = PlayerPrefs.GetFloat("Conf_music", .3f);
		volumenSound = PlayerPrefs.GetFloat("Conf_sound", .3f);
		if (sounds == null) return;
		foreach (Sound2 item in sounds)
		{
			if (item == null) continue;
			item.volume2 = item.name2 == "Chill" || item.name2 == "MusicaLevel" ? volumenMusic : volumenSound;
			if (item.name2 == "SonidoFuente" || item.name2 == "SonidoCompleted") item.volume2 += .2f;
			item.Volume(item.volume2);
		}
	}

	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
		foreach (GameObject sourceObject in sources) if (sourceObject != null) Destroy(sourceObject);
		if (instance == this) instance = null;
	}
}
