using System.Collections.Generic;
using GoogleMobileAds.Api;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BannerHandle : MonoBehaviour
{
	public enum Sizes
	{
		Banner = 0,
		IABBanner = 1,
		Leaderboard = 2,
		MediumRectangle = 3,
		SmartBanner = 4
	}

	public string adUnitID;

	public string adUnitIDAndroid;

	public string adUnitIDIOS;

	public Sizes adSize;

	public AdPosition adPosition;

	public bool customSize;

	public int customWidth;

	public int customHeight;

	public List<string> testDevices;

	public bool useEmulatorAsATestDevice;

	public string keywords;

	public Gender gender;

	public BannerView bannerView;

	public static BannerHandle instance;

	public static int mostrar;

	private string texto1;

	private List<string> deviceIds;

	private void Awake()
	{
	}

	private void OnEnable()
	{
	}

	public void OnDisable()
	{
	}

	private void OnDestroy()
	{
	}

	public void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
	{
	}

	private void destroyAd()
	{
	}

	private AdSize getAdSize()
	{
		return null;
	}

	private void ListenToAdEvents()
	{
	}
}
