using System.Runtime.CompilerServices;
using UnityEngine;

namespace com.google.game
{
	public class GoogleServce
	{
		public delegate void GoogleServiceEventHandler(string eventName, string msg);

		private class GoogleServiceListener : IGoogleServiceListener
		{
			internal GoogleServce googleInstance;

			public void onServiceEvent(string eventName, string paramString)
			{
			}
		}

		[CompilerGenerated]
		private GoogleServiceEventHandler m_googleEventHandler;

		private static GoogleServce _instance;

		private AndroidJavaObject jgame;

		public event GoogleServiceEventHandler googleEventHandler
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

		public static GoogleServce Instance()
		{
			return null;
		}

		private void preInitGame()
		{
		}

		public void sign(string[] scopes)
		{
		}

		public void signOut()
		{
		}

		public bool isSignedIn()
		{
			return false;
		}

		public bool checkServiceAvailable()
		{
			return false;
		}
	}
}
