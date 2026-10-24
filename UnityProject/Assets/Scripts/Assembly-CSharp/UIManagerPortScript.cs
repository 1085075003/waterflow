using System.Collections.Generic;
using GoogleMobileAds.Ump.Api;
using UnityEngine;
using UnityEngine.UI;

public class UIManagerPortScript : MonoBehaviour
{
	public GameObject panelExit;

	public GameObject PlayButton;

	public GameObject SettingsButton;

	public GameObject ExitButton;

	public static bool sonidoPortada;

	private bool botonesActivos;

	private int contadorUpdate;

	private string click;

	private AudioManager0 audioManager;

	public int modoBasic;

	public int modoEasy;

	public int modoHard;

	public int modoMix;

	public int modoMaster;

	public int modoGenius;

	public int modoManiac;

	public int modoPools;

	public int modoPools6;

	public int modoPools3;

	public int modoPools4;

	public int modoMech1;

	public int modoMech2;

	public int modoMech3;

	public int modoMech4;

	public int modoJets1;

	public int modoJets2;

	public int modoJets3;

	public int modoJets4;

	public int modoJets5;

	public static int[] nums;

	public static int displayData;

	public GameObject iniInterstAdmob;

	public GameObject iniRewardedAdmob;

	private int indicadorAds;

	private int intervalInt0;

	private int intervalInt1;

	public static int callingScene;

	private string idioma;

	public Text texto1;

	public Text texto2;

	public Text texto3;

	private int lang;

	public Text TxtLang;

	public Text TxtbuyOff;

	public Text TxtAds;

	public int esTablet;

	private int botonAds;

	private List<string> deviceIds;

	private void Awake()
	{
	}

	private void Start()
	{
		// Native first Portada entry reads system locale. Return routes set callingScene=1.
		if(callingScene==0) doLanguage();
	}

	public void botonPlayDown()
	{
	}

	public void botonOptionsDown()
	{
	}

	public void botonCheckOkDown()
	{
	}

	public void botonCancelDown()
	{
	}

	public void botonExitDown()
	{
	}

	public void doFirebase()
	{
	}

	public void RequestConsent()
	{
	}

	public void OnConsentInfoUpdated(FormError consentError)
	{
	}

	public void OnConsentFormCompleted(FormError formError)
	{
	}

	public void doAds()
	{
	}

	public void doValoresDefecto()
	{
	}

	public void doLanguage()
	{
		idioma=ObtenerIdiomaDelSistema();
		PlayerPrefs.SetString("language",idioma); PlayerPrefs.Save();
	}

	public string ObtenerIdiomaDelSistema()
	{
		return RecoveredNativeLocalization.SystemLanguageCode(Application.systemLanguage);
	}

	public void botonLangDown()
	{
		RecoveredNativeLocalization.Cycle(this);
	}

	public void doDevice()
	{
	}

	public void botonBuyOff()
	{
	}

	public void botonAds1()
	{
	}
}
