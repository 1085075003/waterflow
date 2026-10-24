using UnityEngine;
using UnityEngine.UI;

public class SolutionLang : MonoBehaviour
{
	public Text texto1a;

	private string idioma;

	private string texto1;

	private void Start()
	{
		RecoveredNativeLocalization.Apply(this);
	}

	public string ObtenerTexto()
	{
		return RecoveredNativeLocalization.Text("SolutionLang$$ObtenerTexto", idioma, "");
	}
}
