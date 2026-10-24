using UnityEngine;

public class VelocityField : MonoBehaviour
{
	private struct Particle
	{
		public float x;

		public float y;
	}

	public float visc;

	public int iterations;

	public float particleSpeed;

	private Texture2D tex;

	private int width;

	private int height;

	private float[,] u;

	private float[,] v;

	private float[,] u_prev;

	private float[,] v_prev;

	private int numParticles;

	private Particle[] particles;

	private void Start()
	{
	}

	private void Update()
	{
	}

	private void SetBounds(int b, float[,] x)
	{
	}

	private void Diffuse(int b, float[,] x, float[,] x0, float a, float c)
	{
	}

	private void Project(float[,] u, float[,] v, float[,] p, float[,] div)
	{
	}

	private void VelStep()
	{
	}

	private void UserInput()
	{
	}

	private void Draw()
	{
	}

	private void MoveParticles()
	{
	}

	private float Bilin(float[,] field, float xPos, float yPos)
	{
		return 0f;
	}
}
