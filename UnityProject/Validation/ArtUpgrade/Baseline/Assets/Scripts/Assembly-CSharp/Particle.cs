using UnityEngine;

public class Particle : MonoBehaviour
{
	public float lifeTime;

	public float speed;

	public float inertia;

	public float velX;

	public float velY;

	private float[] vFieldX;

	private float[] vFieldY;

	private int width;

	private int height;

	public void Init(float[] velFieldX, float[] velFieldY, int w, int h)
	{
	}

	private void Update()
	{
	}

	private float Bilin(float[] field, float xPos, float yPos)
	{
		return 0f;
	}
}
