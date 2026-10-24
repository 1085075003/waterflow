using UnityEngine;

public class LiquidRipples : MonoBehaviour
{
	private struct WaterParticle
	{
		public float height;

		private float speed;

		public void AddForce(float force)
		{
		}

		public void Update()
		{
		}
	}

	private Texture2D tex;

	private int width;

	private int height;

	private float level;

	private const float viscosity = 0.07f;

	private WaterParticle[,] particles;

	private void Start()
	{
	}

	private void Update()
	{
	}

	private void Fluid()
	{
	}

	private void UserInput()
	{
	}

	private void Draw()
	{
	}
}
