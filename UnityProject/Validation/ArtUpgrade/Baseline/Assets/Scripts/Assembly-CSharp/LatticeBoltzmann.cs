using UnityEngine;

public class LatticeBoltzmann : MonoBehaviour
{
	public Texture2D obstacles;

	public bool showVel;

	public bool showDens;

	public Particle part;

	private Texture2D tex;

	private int[] ex;

	private int[] ey;

	private int Q;

	private float[] rt;

	private float cs2;

	private bool[] obst;

	private float[] f;

	private float[] ftemp;

	private float[] density;

	private float[] ux;

	private float[] uy;

	private float density0;

	private float ux0;

	private float omegaf;

	private int t;

	private int lx;

	private int ly;

	private void Start()
	{
	}

	private void Update()
	{
	}

	private void Initialization()
	{
	}

	private void Propagation(float[] f, float[] ftemp)
	{
	}

	private void Boundary(float ux0, float[] f, float[] ftemp, bool[] obst)
	{
	}

	private void CalcMacroQuantities(float[] density, float[] ux, float[] uy, float[] ftemp, bool[] obst)
	{
	}

	private void CollisionSRT(float[] density, float[] ux, float[] uy, float[] f, float[] ftemp, bool[] obst, float omegaf)
	{
	}

	private void UserInput()
	{
	}

	private void Draw()
	{
	}
}
