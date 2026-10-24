using UnityEngine;
using UnityEngine.UI;

public class exitLang : MonoBehaviour
{
	public Text mensajeTexto;

	private RectTransform rectTransform;

	private string idioma;

	public void OnEnable()
	{
		RecoveredNativeLocalization.Apply(this);
	}

	public void AjustarPopup()
	{
		RecoveredNativeLocalization.ResizeExit(this);
	}

	public bool EsTablet()
	{
		return RecoveredNativeLocalization.IsTablet();
	}

	private string ObtenerTextoTraducido(string idioma)
	{
		return RecoveredNativeLocalization.Text("exitLang$$ObtenerTextoTraducido", idioma, "");
	}
}
