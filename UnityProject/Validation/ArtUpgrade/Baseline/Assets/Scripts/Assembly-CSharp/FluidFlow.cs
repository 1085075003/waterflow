using UnityEngine;

public class FluidFlow : MonoBehaviour
{
	private struct Particle
	{
		public float posX;

		public float posY;

		public float velX;

		public float velY;

		private const float inertia = 0.9f;

		public void AddForce(float forceX, float forceY)
		{
		}
	}

	public Texture2D relief;

	public bool showNormalmap;

	public float heightScale;

	public float diffStrength;

	public int iterations;

	public float influenceRelief;

	public float influenceDiff;

	public float influenceLiquid;

	public float influenceSpread;

	public float turbulence;

	public int userRadius;

	public float userStrength;

	public float source;

	private Texture2D tex;

	private int width;

	private int height;

	private float[,] liquid;

	private float[,] ground;

	private float[,] riverbed;

	private float[,] users;

	private float[,] velX;

	private float[,] velY;

	private int numParticles;

	private Particle[] particles;

	private int partInd;

	private float[,] partGradX;

	private float[,] partGradY;

	private int userPosX;

	private int userPosY;

	private void Start()
	{
	}

	private void Update()
	{
	}

	private void Source()
	{
	}

	private void Fluid()
	{
	}

	private void VelocityField()
	{
	}

	private void UserInput()
	{
	}

	private void UserMap(int x, int y)
	{
	}

	private void Draw()
	{
	}

	private void ParticleReset(int i)
	{
	}

	private void MoveParticles()
	{
	}
}
