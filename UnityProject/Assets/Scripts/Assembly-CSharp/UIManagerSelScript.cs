using UnityEngine;

public class UIManagerSelScript : MonoBehaviour
{
	public static int var_design;

	public static int var_solucion;

	private string click;

	private AudioManager0 audioManager;

	public GameObject test1;

	public GameObject botonDesign2;

	public GameObject botonSolucion;

	public static int ventana;

	public int numlevel1;

	public int contadorTest;

	public int contadorTest1;

	public int optionTest;

	public int level;

	private bool accesoPermitido;

	private int levelMax;

	public GameObject BannerManager;

	private int indicadorAds;

	private void Start()
	{
	}

	public void botonMenuDown()
	{
	}

	public void botonTestDown()
	{
	}

	public void botonTest1Down()
	{
	}

	public void botonDesignDown()
	{
		var_design=1;
	}

	public void botonDesignDown2()
	{
		var_design=2;
	}

	public void botonDesignDown3()
	{
		var_design=3;
	}

	public void botonDesignSolucion()
	{
		var_solucion=1;
	}

	public void botonDesignSolucion2()
	{
		var_solucion=2;
	}

	public void Level(int numlevel)
	{
	}

	public void doControlAcceso()
	{
	}

	public void doCallGame()
	{
	}

	public void botonRestartDown()
	{
	}
}
