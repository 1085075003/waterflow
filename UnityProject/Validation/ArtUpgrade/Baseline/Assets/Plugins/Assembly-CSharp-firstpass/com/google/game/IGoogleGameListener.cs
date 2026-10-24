using UnityEngine;

namespace com.google.game
{
	internal interface IGoogleGameListener
	{
		void onGameEvent(string eventName, string eventData);

		void onGetSnapshotData(string snapshotMeta, AndroidJavaObject snapshotData);
	}
}
