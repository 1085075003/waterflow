using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using GoogleMobileAds.Api;
using UnityEngine;
using UnityEngine.UI;

public class InicializarInterstitial : MonoBehaviour
{
	[CompilerGenerated]
	private sealed class _003CRetryInterstitialLoadCoroutine_003Ed__27 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public InicializarInterstitial _003C_003E4__this;

		private float _003CcurrentDelay_003E5__2;

		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			get
			{
				return null;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return null;
			}
		}

		[DebuggerHidden]
		public _003CRetryInterstitialLoadCoroutine_003Ed__27(int _003C_003E1__state)
		{
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
		}

		private bool MoveNext()
		{
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
		}
	}

	public static InicializarInterstitial instance3;

	private InterstitialAd interstitial;

	[CompilerGenerated]
	private Action m_OnInterstitialFailedToLoad;

	[CompilerGenerated]
	private Action m_OnInterstitialClosed;

	public static bool cambiadoEstadoIntAdmob;

	public static int estadoInterstitialAdmob;

	public static bool InterstitialLoaded;

	public Text textInterAdmob;

	private string texto1;

	private List<string> deviceIds;

	private bool isLoadingInterstitial;

	private bool retryScheduled;

	private float retryDelay;

	private const float maxRetryDelay = 60f;

	private int retryCount;

	private const int maxRetryCount = 3;

	public event Action OnInterstitialFailedToLoad
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

	public event Action OnInterstitialClosed
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

	private void Awake()
	{
	}

	private void Start()
	{
	}

	private void RequestInterstitial()
	{
	}

	public void showIntersAdmob()
	{
	}

	private void RegisterEventHandlers(InterstitialAd ad)
	{
	}

	private void LogLoadAdError(string tag, LoadAdError error)
	{
	}

	private void ScheduleInterstitialRetry()
	{
	}

	[IteratorStateMachine(typeof(_003CRetryInterstitialLoadCoroutine_003Ed__27))]
	private IEnumerator RetryInterstitialLoadCoroutine()
	{
		return null;
	}

	private void ConfigureAdMob()
	{
	}
}
