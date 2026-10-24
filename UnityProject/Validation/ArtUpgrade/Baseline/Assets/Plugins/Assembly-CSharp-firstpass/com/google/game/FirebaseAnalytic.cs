using UnityEngine;

namespace com.google.game
{
	public class FirebaseAnalytic
	{
		private static FirebaseAnalytic _instance;

		private AndroidJavaObject jobject;

		public static FirebaseAnalytic Instance()
		{
			return null;
		}

		private void preInit()
		{
		}

		public void logEvent(string name, string jsonObjectString)
		{
		}

		public void setUserProperty(string name, string value)
		{
		}

		public void setUserId(string userID)
		{
		}

		public void setSessionTimeoutDuration(long milliseconds)
		{
		}

		public void setMinimumSessionDuration(long milliseconds)
		{
		}

		public void setAnalyticsCollectionEnabled(bool enable)
		{
		}
	}
}
