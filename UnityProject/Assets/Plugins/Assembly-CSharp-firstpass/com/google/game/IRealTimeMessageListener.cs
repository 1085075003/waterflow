namespace com.google.game
{
	internal interface IRealTimeMessageListener
	{
		void onGameEvent(string eventName, string eventData);
	}
}
