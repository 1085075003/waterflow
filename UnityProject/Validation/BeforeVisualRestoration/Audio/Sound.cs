using System;
using UnityEngine;

[Serializable]
public class Sound
{
	public string name;

	public AudioClip clip;

	public float volume;

	public float pitch;

	public bool loop;

	private AudioSource source;

	public void SetSource(AudioSource _source)
	{
	}

	public void Play(float volume)
	{
	}

	public void Stop()
	{
	}

	public void Volume(float volume)
	{
	}
}
