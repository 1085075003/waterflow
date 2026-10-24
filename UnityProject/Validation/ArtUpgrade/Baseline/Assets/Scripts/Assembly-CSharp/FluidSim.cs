using UnityEngine;

public class FluidSim : MonoBehaviour
{
	public float dt;

	public float visc;

	public float volatilize;

	public int iterations;

	public Texture2D border;

	public Texture2D flow;

	private Texture2D tex;

	private int width;

	private int height;

	private float[] u;

	private float[] v;

	private float[] u_prev;

	private float[] v_prev;

	private float[] dens;

	private float[] dens_prev;

	private float[] bndX;

	private float[] bndY;

	private float[] velX;

	private float[] velY;

	private int rowSize;

	private int size;

	private void Start()
	{
	}

	private void Update()
	{
	}

	private void addFields(float[] x, float[] s, float dt)
	{
	}

	private void set_bnd(int b, float[] x)
	{
	}

	private void lin_solve(float[] x, float[] x0, float a, float c)
	{
	}

	private void diffuse(float[] x, float[] x0)
	{
	}

	private void lin_solve2(float[] x, float[] x0, float[] y, float[] y0, float a, float c)
	{
	}

	private void diffuse2(float[] x, float[] x0, float[] y, float[] y0)
	{
	}

	private void advect(int b, float[] d, float[] d0, float[] u, float[] v, float dt)
	{
	}

	private void project(float[] u, float[] v, float[] p, float[] div)
	{
	}

	private void dens_step(float[] x, float[] x0, float[] u, float[] v, float dt)
	{
	}

	private void vel_step(float[] u, float[] v, float[] u0, float[] v0, float dt)
	{
	}

	private void UserInput()
	{
	}

	private void Draw()
	{
	}
}
