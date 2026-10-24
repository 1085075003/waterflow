using UnityEngine;
using UnityEngine.UI;

public class watchBox : MonoBehaviour
{
	public Text texto1;

	private string idioma;

	private string tex1;

	public void Start()
	{
		RecoveredNativeLocalization.Apply(this);
	}

	public string ObtenerTexto1()
	{
		return RecoveredNativeLocalization.Text("watchBox$$ObtenerTexto1", idioma, "");
	}
}
