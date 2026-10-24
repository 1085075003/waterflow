using UnityEngine;

namespace com.google.game
{
	public class IGoogleServiceListenerProxy : AndroidJavaProxy
	{
		private IGoogleServiceListener listener;

		internal IGoogleServiceListenerProxy(IGoogleServiceListener listener)
			: base((string)null)
		{
		}

		private void onServiceEvent(string eventName, string eventData)
		{
		}
	}
}
