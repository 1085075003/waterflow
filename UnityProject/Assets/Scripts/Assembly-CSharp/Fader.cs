using UnityEngine;
using UnityEngine.SceneManagement;

public class Fader : MonoBehaviour
{
	[HideInInspector]
	public bool start;

	[HideInInspector]
	public float fadeDamp;

	[HideInInspector]
	public string fadeScene;

	[HideInInspector]
	public float alpha;

	[HideInInspector]
	public Color fadeColor;

	[HideInInspector]
	public bool isFadeIn;

	private void OnEnable()
	{
	}

	private void OnDisable()
	{
	}

	private void OnGUI()
	{
	}

	private void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
	{
	}
}
