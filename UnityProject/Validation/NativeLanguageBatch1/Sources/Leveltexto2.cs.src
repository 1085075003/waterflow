using UnityEngine;
using UnityEngine.UI;

public class Leveltexto2 : MonoBehaviour
{
	public Text texto1;

	private string texto2;

	private string idiomaActual;

	private void Start()
	{
		doLanguage();
		int[] offsets={-50,-100,-150,-200,-250,-300,-350,-400,-450,-500,-550,-600,-650,-700,-750,-1000,0,0,-1050,-1100,-1150,-1200,0,0,0,0,0,0,0,0,-1650,-1700,-1750,-1800,-1850,-1900,-1950,-2000};
		int p=UIManagerSelScript0.parte;
		RecoveredNativeLocalization.Set(texto1,texto2+" "+(ControladorJuego.nivelActual+(p>=2&&p<=39?offsets[p-2]:0)));
	}

	public void doLanguage()
	{
		idiomaActual=PlayerPrefs.GetString("language","en");
		texto2=ObtenerTexto();
	}

	public string ObtenerTexto()
	{
		return RecoveredNativeLocalization.Text("Leveltexto2$$ObtenerTexto", idiomaActual, "");
	}
}
