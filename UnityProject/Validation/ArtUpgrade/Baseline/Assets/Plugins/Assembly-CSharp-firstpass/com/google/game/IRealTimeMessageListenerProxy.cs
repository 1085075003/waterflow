using UnityEngine;

namespace com.google.game
{
	public class IRealTimeMessageListenerProxy : AndroidJavaProxy
	{
		private IRealTimeMessageListener listener;

		internal IRealTimeMessageListenerProxy(IRealTimeMessageListener listener)
			: base((string)null)
		{
		}

		private void onGameEvent(string eventName, string eventData)
		{
		}
	}
}
