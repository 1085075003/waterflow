using System;
using UnityEngine;

[Serializable]
public class Sound2
{
	public string name2;

	public AudioClip clip2;

	public float volume2;

	public float pitch2;

	public bool loop2;

	private AudioSource source2;

	public void SetSource(AudioSource _source2)
	{
		source2 = _source2;
		source2.clip = clip2;
		source2.loop = loop2;
	}

	public void Play(float volume)
	{
		if (source2 == null) return;
		source2.volume = volume2;
		source2.pitch = pitch2;
		source2.Play();
	}

	public void Stop()
	{
		if (source2 != null && source2.isPlaying) source2.Stop();
	}

	public void Volume(float volume)
	{
		if (source2 != null) source2.volume = volume;
	}
}
