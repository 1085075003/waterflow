using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

public class Purchaser : MonoBehaviour, IStoreListener
{
	public Text descriptionText1;

	public Text priceText1;

	public Text descriptionText2;

	public Text priceText2;

	public Text descriptionText3;

	public Text priceText3;

	public Text descriptionText4;

	public Text priceText4;

	public Text descriptionText5;

	public Text priceText5;

	public Text descriptionText6;

	public Text priceText6;

	public Text descriptionText7;

	public Text priceText7;

	public Text descriptionText11;

	public Text priceText11;

	public Text descriptionText12;

	public Text priceText12;

	public Text descriptionText13;

	public Text priceText13;

	public Text descriptionText14;

	public Text priceText14;

	public Text descriptionText15;

	public Text priceText15;

	public GameObject botonbuy1;

	public GameObject botonbuy2;

	public GameObject botonbuy3;

	public GameObject botonbuy4;

	public GameObject botonbuy5;

	public GameObject botonbuy6;

	public GameObject botonbuy7;

	public GameObject botonbuy11;

	public GameObject botonbuy12;

	public GameObject botonbuy13;

	public GameObject botonbuy14;

	public GameObject botonbuy15;

	public GameObject botonTestbuy1;

	public GameObject botonTestbuy2;

	public GameObject botonTestbuy3;

	public GameObject botonTestbuy4;

	public GameObject botonTestbuy5;

	public GameObject botonTestbuy6;

	public GameObject botonTestbuy7;

	public GameObject botonTestbuy11;

	public GameObject botonTestbuy12;

	public GameObject botonTestbuy13;

	public GameObject botonTestbuy14;

	public GameObject botonTestbuy15;

	public int removeAds;

	public int master;

	public int genius;

	public int maniac;

	public int hard;

	public int mix;

	public int pools6;

	public int todos;

	public int allGame;

	public int pools;

	public int mech;

	public int jets;

	public int jets1;

	public int jetsN;

	public int night;

	public int numSoluciones;

	public GameObject cajaErrorIAP;

	private string errorText;

	public Text errorText1;

	public int errorOninit;

	public int error;

	public GameObject Manager;

	public GameObject purch1;

	public GameObject purch2;

	public GameObject purch3;

	public GameObject purch4;

	public GameObject purch5;

	public GameObject purch6;

	public GameObject purch12;

	public GameObject purch14;

	private string texto;

	private string click;

	private AudioManager0 audioManager0;

	private AudioManager audioManager;

	private static IStoreController m_StoreController;

	private static IExtensionProvider m_StoreExtensionProvider;

	public static string kProductIDConsumable1;

	public static string kProductIDConsumable2;

	public static string kProductIDConsumable3;

	public static string kProductIDConsumable4;

	public static string kProductIDNonConsumable1;

	public static string kProductIDNonConsumable2;

	public static string kProductIDNonConsumable3;

	public static string kProductIDNonConsumable4;

	public static string kProductIDNonConsumable5;

	public static string kProductIDNonConsumable6;

	public static string kProductIDNonConsumable7;

	public static string kProductIDNonConsumable8;

	public static string kProductIDNonConsumable9;

	public static string kProductIDNonConsumable10;

	public static string kProductIDNonConsumable11;

	public static string kProductIDNonConsumable12;

	private bool processPurchaseCalled;

	private void Start()
	{
	}

	public void InitializePurchasing()
	{
	}

	private bool IsInitialized()
	{
		return false;
	}

	public void BuyConsumable1()
	{
	}

	public void BuyConsumable2()
	{
	}

	public void BuyConsumable3()
	{
	}

	public void BuyConsumable4()
	{
	}

	public void BuyNonConsumable1()
	{
	}

	public void BuyNonConsumable2()
	{
	}

	public void BuyNonConsumable6()
	{
	}

	public void BuyNonConsumable7()
	{
	}

	public void BuyNonConsumable8()
	{
	}

	public void BuyNonConsumable9()
	{
	}

	public void BuyNonConsumable11()
	{
	}

	public void BuyNonConsumable12()
	{
	}

	public void BuyNonConsumabletest()
	{
	}

	private void BuyProductID(string productId)
	{
	}

	public void doError()
	{
	}

	public void RestorePurchases()
	{
	}

	public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
	{
	}

	public void OnInitializeFailed(InitializationFailureReason error)
	{
	}

	public void OnInitializeFailed(InitializationFailureReason error, string errorx)
	{
	}

	public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
	{
		return PurchaseProcessingResult.Complete;
	}

	public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
	{
	}

	public void doInformarProductos()
	{
	}

	public void doReinstallYaComprado()
	{
	}

	public void testBuyConsumable1()
	{
	}

	public void testBuyConsumable2()
	{
	}

	public void testBuyConsumable3()
	{
	}

	public void testBuyConsumable4()
	{
	}

	public void testBuyNonConsumable1()
	{
	}

	public void testBuyNonConsumable2()
	{
	}

	public void testBuyNonConsumable6()
	{
	}

	public void testBuyNonConsumable7()
	{
	}

	public void testBuyNonConsumable8()
	{
	}

	public void testBuyNonConsumable9()
	{
	}

	public void testBuyNonConsumable11()
	{
	}

	public void testBuyNonConsumable12()
	{
	}

	public void doRemoveAds()
	{
	}

	public void doExtraSolutions()
	{
	}

	public void do30ExtraSolutions()
	{
	}

	public void do80ExtraSolutions()
	{
	}

	public void do200ExtraSolutions()
	{
	}

	public void doActiveAll()
	{
	}

	public void doActiveAllGame()
	{
	}

	public void doActivePools()
	{
	}

	public void doActiveMech()
	{
	}

	public void doActiveJets()
	{
	}

	public void doActiveJets1()
	{
	}

	public void doActiveJetsN()
	{
	}

	public void doActiveNight()
	{
	}

	public void doRecYaComprado0()
	{
	}

	public void doRecYaComprado1()
	{
	}

	public void doTextos()
	{
	}
}
