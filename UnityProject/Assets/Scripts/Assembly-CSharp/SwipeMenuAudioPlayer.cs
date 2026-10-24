using UnityEngine;

public class SwipeMenuAudioPlayer : MonoBehaviour
{
	public ExampleMenuAudioPlayer audioPlayer;

	public SpriteRenderer spriteRenderer;

	public Sprite playingSrpite;

	public Sprite notPlayingSprite;

	private bool _isPlaying;

	public void Activate(AudioClip clip)
	{
	}

	public void Deactivate()
	{
	}
}
