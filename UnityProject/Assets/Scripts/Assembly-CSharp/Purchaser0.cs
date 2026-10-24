using UnityEngine;
using UnityEngine.Purchasing;

public class Purchaser0 : MonoBehaviour, IStoreListener
{
	private int removeAds;

	private int allGame;

	public static IStoreController m_StoreController;

	public static IExtensionProvider m_StoreExtensionProvider;

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

	private string texto;

	private string errorText;

	private int errorOninit;

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

	public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
	{
	}

	public void doInformarVars()
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

	public void doTextos()
	{
	}
}
