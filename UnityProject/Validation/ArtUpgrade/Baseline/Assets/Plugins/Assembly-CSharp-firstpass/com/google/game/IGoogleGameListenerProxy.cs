using UnityEngine;

namespace com.google.game
{
	public class IGoogleGameListenerProxy : AndroidJavaProxy
	{
		private IGoogleGameListener listener;

		internal IGoogleGameListenerProxy(IGoogleGameListener listener)
			: base((string)null)
		{
		}

		private void onGameEvent(string eventName, string eventData)
		{
		}

		private void onGetSnapshotData(string snapshotMeta, AndroidJavaObject snapshotData)
		{
		}
	}
}
