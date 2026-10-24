using UnityEngine;
using UnityEngine.UI;

public class noMoreLang : MonoBehaviour
{
	public Text texto1;

	private string idioma;

	private string tex1;

	private void Start()
	{
		RecoveredNativeLocalization.Apply(this);
	}

	public string ObtenerTexto()
	{
		return RecoveredNativeLocalization.Text("noMoreLang$$ObtenerTexto", idioma, "");
	}
}
