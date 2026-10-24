using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using GoogleMobileAds.Api;
using UnityEngine;
using UnityEngine.UI;

public class GoogleMobileAdsDemoScript1 : MonoBehaviour
{
	[CompilerGenerated]
	private sealed class _003CRetryRewardedLoadCoroutine_003Ed__28 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public GoogleMobileAdsDemoScript1 _003C_003E4__this;

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
		public _003CRetryRewardedLoadCoroutine_003Ed__28(int _003C_003E1__state)
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

	public static GoogleMobileAdsDemoScript1 instance1;

	public RewardedAd rewardedAd;

	public static bool admobLoaded0;

	public static bool cambiadoEstadoAdmob;

	public static int estadoAdmob;

	private string SonidoBuy;

	private string SonidoFuente;

	private string Chill;

	private string MusicaLevel;

	private AudioManager audioManager;

	public Text textRewAdmob;

	private string texto1;

	private List<string> deviceIds;

	private bool isLoadingRewarded;

	private bool retryScheduledRewarded;

	private float retryDelayRewarded;

	private const float maxRetryDelayRewarded = 60f;

	private int retryCountRewarded;

	private const int maxRetryCountRewarded = 3;

	private int vez;

	private void Awake()
	{
	}

	private void Start()
	{
	}

	private void LoadRewardedAd()
	{
	}

	public void ShowAdAdmob()
	{
	}

	public void doSonido()
	{
	}

	private void RegisterEventHandlers(RewardedAd ad)
	{
	}

	private void ConfigureAdMob()
	{
	}

	private void ScheduleRewardedRetry()
	{
	}

	[IteratorStateMachine(typeof(_003CRetryRewardedLoadCoroutine_003Ed__28))]
	private IEnumerator RetryRewardedLoadCoroutine()
	{
		return null;
	}
}
