using UnityEngine;
using UnityEngine.UI;

public class UIManagerOptScript : MonoBehaviour
{
	private string click;

	private AudioManager0 audioManager;

	public Slider volumen1;

	public Slider volumen2;

	public float valor1;

	public float valor2;

	private int indicadorAds;

	public GameObject BannerManager;

	public GameObject cajaReset;

	public GameObject cajaProgress;

	public GameObject resetButton;

	public GameObject storeButton;

	public GameObject rateButton;

	public GameObject backButton;

	public Text texto2;

	public Text texto3;

	public Text texto4;

	public Text texto5;

	public Text texto6;

	public Text texto7;

	private string idioma;

	private string tex1;

	private string tex2;

	private string tex3;

	private string tex4;

	private string tex5;

	private string tex6;

	private string tex7;

	private int lastFontSize4;

	private int lastFontSize5;

	private void Start()
	{
		doLanguages();
	}

	public void botonBackDown()
	{
	}

	public void volumenMusicChanged()
	{
	}

	public void volumenSoundChanged()
	{
	}

	public void botonStoreDown()
	{
	}

	public void botonResetDown()
	{
	}

	public void botonResetCancel()
	{
	}

	public void botonResetOk()
	{
	}

	public void botonProgressClose()
	{
	}

	public void botonRateDown()
	{
	}

	public void doLanguages()
	{
		RecoveredNativeLocalization.Apply(this);
	}

	public string ObtenerTexto2()
	{
		return RecoveredNativeLocalization.Text("UIManagerOptScript$$ObtenerTexto2", idioma, "");
	}

	public string ObtenerTexto3()
	{
		return RecoveredNativeLocalization.Text("UIManagerOptScript$$ObtenerTexto3", idioma, "");
	}

	public string ObtenerTexto6()
	{
		return RecoveredNativeLocalization.Text("UIManagerOptScript$$ObtenerTexto6", idioma, "");
	}

	public string ObtenerTexto7()
	{
		return RecoveredNativeLocalization.Text("UIManagerOptScript$$ObtenerTexto7", idioma, "");
	}
}
