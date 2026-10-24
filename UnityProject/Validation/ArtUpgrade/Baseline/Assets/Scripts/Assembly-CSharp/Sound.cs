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
		source = _source;
		source.clip = clip;
		source.loop = loop;
	}

	public void Play(float volume)
	{
		if (source == null) return;
		source.volume = volume;
		source.pitch = pitch;
		source.Play();
	}

	public void Stop()
	{
		if (source != null) source.Stop();
	}

	public void Volume(float volume)
	{
		if (source != null) source.volume = volume;
	}
}
