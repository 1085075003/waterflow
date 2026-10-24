using UnityEngine;
using UnityEngine.UI;

public class rateUsLang : MonoBehaviour
{
	public Text texto1;

	public Text texto2;

	public Text texto3;

	private string idioma;

	private string tex1;

	private string tex2;

	private string tex3;

	private const string TIENDA = "Google Play";

	public void Start()
	{
		RecoveredNativeLocalization.Apply(this);
	}

	public string ObtenerTexto1()
	{
		return RecoveredNativeLocalization.Text("rateUsLang$$ObtenerTexto1", idioma, "");
	}

	public string ObtenerTexto2()
	{
		return RecoveredNativeLocalization.Text("rateUsLang$$ObtenerTexto2", idioma, "");
	}

	public string ObtenerTexto3()
	{
		return RecoveredNativeLocalization.Text("rateUsLang$$ObtenerTexto3", idioma, "");
	}

	public void Ajustar()
	{
		RecoveredNativeLocalization.FitRate(this);
	}
}
