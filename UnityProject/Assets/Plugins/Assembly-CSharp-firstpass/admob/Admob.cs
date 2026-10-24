using System.Runtime.CompilerServices;
using UnityEngine;

namespace admob
{
	public class Admob
	{
		public delegate void AdmobEventHandler(string eventName, string msg);

		private class InnerAdmobListener : IAdmobListener
		{
			internal Admob admobInstance;

			public void onAdmobEvent(string adtype, string eventName, string paramString)
			{
			}
		}

		[CompilerGenerated]
		private AdmobEventHandler m_bannerEventHandler;

		[CompilerGenerated]
		private AdmobEventHandler m_interstitialEventHandler;

		[CompilerGenerated]
		private AdmobEventHandler m_rewardedVideoEventHandler;

		[CompilerGenerated]
		private AdmobEventHandler m_nativeBannerEventHandler;

		private static Admob _instance;

		private AndroidJavaObject jadmob;

		public event AdmobEventHandler bannerEventHandler
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		public event AdmobEventHandler interstitialEventHandler
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		public event AdmobEventHandler rewardedVideoEventHandler
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		public event AdmobEventHandler nativeBannerEventHandler
		{
			[CompilerGenerated]
			add
			{
			}
			[CompilerGenerated]
			remove
			{
			}
		}

		public static Admob Instance()
		{
			return null;
		}

		private void preInitAdmob()
		{
		}

		public void removeAllBanner()
		{
		}

		public void initSDK(string appid, AdProperties value)
		{
		}

		public void showBannerRelative(string admobBannerID, AdSize size, int position, int marginY = 0, string instanceName = "defaultBanner")
		{
		}

		public void showBannerAbsolute(string admobBannerID, AdSize size, int x, int y, string instanceName = "defaultBanner")
		{
		}

		public void removeBanner(string instanceName = "defaultBanner")
		{
		}

		public void loadInterstitial(string interstitialID)
		{
		}

		public bool isInterstitialReady()
		{
			return false;
		}

		public void showInterstitial()
		{
		}

		public void loadRewardedVideo(string rewardedVideoID)
		{
		}

		public bool isRewardedVideoReady()
		{
			return false;
		}

		public void showRewardedVideo()
		{
		}

		public void showNativeBannerRelative(string nativeBannerID, AdSize size, int position, int marginY = 0, string instanceName = "defaultNativeBanner")
		{
		}

		public void showNativeBannerAbsolute(string nativeBannerID, AdSize size, int x, int y, string instanceName = "defaultNativeBanner")
		{
		}

		public void removeNativeBanner(string instanceName = "defaultNativeBanner")
		{
		}

		public void reloadNativeBanner(string instanceName = "defaultNativeBanner")
		{
		}
	}
}
