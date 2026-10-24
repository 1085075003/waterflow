using UnityEngine;
using UnityEngine.UI;

public class settingsLang : MonoBehaviour
{
	public Text texto1;

	public Text texto2;

	public Text texto3;

	public Text texto4;

	public Text texto5;

	public Text texto6;

	private string idioma;

	private string tex1;

	private string tex2;

	private string tex3;

	private string tex4;

	private string tex5;

	private string tex6;

	private int lastFontSize4;

	private int lastFontSize5;

	private void Start()
	{
		RecoveredNativeLocalization.Apply(this);
	}

	public string ObtenerTexto1()
	{
		return RecoveredNativeLocalization.Text("settingsLang$$ObtenerTexto1", idioma, "");
	}

	public string ObtenerTexto2()
	{
		return RecoveredNativeLocalization.Text("settingsLang$$ObtenerTexto2", idioma, "");
	}

	public string ObtenerTexto3()
	{
		return RecoveredNativeLocalization.Text("settingsLang$$ObtenerTexto3", idioma, "");
	}

	public string ObtenerTexto4()
	{
		return RecoveredNativeLocalization.Text("settingsLang$$ObtenerTexto4", idioma, "");
	}

	public string ObtenerTexto5()
	{
		return RecoveredNativeLocalization.Text("settingsLang$$ObtenerTexto5", idioma, "");
	}

	public string ObtenerTexto6()
	{
		return RecoveredNativeLocalization.Text("settingsLang$$ObtenerTexto6", idioma, "");
	}
}
