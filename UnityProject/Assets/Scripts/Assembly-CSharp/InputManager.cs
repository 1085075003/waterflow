using System.Runtime.CompilerServices;
using UnityEngine;

public class InputManager : MonoBehaviour
{
	private float _xMovement;

	private float _yMovement;

	[CompilerGenerated]
	private static MouseMoved m_MouseMoved;

	public static event MouseMoved MouseMoved
	{
		[CompilerGenerated]
		add
		{
		}
		[CompilerGenerated]
		remove
		{
		}
	}

	private static void OnMouseMoved(float xmovement, float ymovement)
	{
	}

	private void InvokeActionOnInput()
	{
	}

	private void Update()
	{
	}
}
