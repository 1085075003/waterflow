using UnityEngine;
using UnityEngine.UI;

public class CajaComplete : MonoBehaviour
{
	private string texto;

	public GameObject buttonNext;

	public GameObject buttonHome;

	public GameObject buttonHome1;

	private int nivelPack;

	private int indicadorAds;

	private int marc1;

	private bool modoRewarded;

	private string texto1;

	private string texto2;

	private string idioma;

	public Text text2;

	private void Start()
	{
		doLanguage();
	}

	public void ConfigureRewarded()
	{
	}

	public void doLanguage()
	{
		RecoveredNativeLocalization.Apply(this);
	}

	public string ObtenerTexto1()
	{
		return RecoveredNativeLocalization.Text("CajaComplete$$ObtenerTexto1", idioma, "");
	}

	public string ObtenerTexto2()
	{
		return RecoveredNativeLocalization.Text("CajaComplete$$ObtenerTexto2", idioma, "");
	}
}
