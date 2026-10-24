using System;

namespace admob
{
	[Serializable]
	public class AdProperties
	{
		public bool isForChildDirectedTreatment;

		public bool isUnderAgeOfConsent;

		public bool isAppMuted;

		public bool isTesting;

		public bool nonPersonalizedAdsOnly;

		public int appVolume;

		public string maxAdContentRating;

		public string[] keywords;

		public string toString()
		{
			return null;
		}
	}
}
