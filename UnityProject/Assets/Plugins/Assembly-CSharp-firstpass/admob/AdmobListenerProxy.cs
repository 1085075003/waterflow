using UnityEngine;

namespace admob
{
	public class AdmobListenerProxy : AndroidJavaProxy
	{
		private IAdmobListener listener;

		internal AdmobListenerProxy(IAdmobListener listener)
			: base((string)null)
		{
		}

		private void onAdmobEvent(string adtype, string eventName, string paramString)
		{
		}

		public override string toString()
		{
			return null;
		}
	}
}
