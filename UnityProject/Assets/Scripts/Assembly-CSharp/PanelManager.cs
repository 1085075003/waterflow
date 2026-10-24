using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PanelManager : MonoBehaviour
{
	[CompilerGenerated]
	private sealed class _003CDisablePanelDeleyed_003Ed__10 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public Animator anim;

		public PanelManager _003C_003E4__this;

		private bool _003CclosedStateReached_003E5__2;

		private bool _003CwantToClose_003E5__3;

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
		public _003CDisablePanelDeleyed_003Ed__10(int _003C_003E1__state)
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

	public Animator initiallyOpen;

	private int m_OpenParameterId;

	private Animator m_Open;

	private GameObject m_PreviouslySelected;

	private const string k_OpenTransitionName = "Open";

	private const string k_ClosedStateName = "Closed";

	public void OnEnable()
	{
	}

	public void OpenPanel(Animator anim)
	{
	}

	private static GameObject FindFirstEnabledSelectable(GameObject gameObject)
	{
		return null;
	}

	public void CloseCurrent()
	{
	}

	[IteratorStateMachine(typeof(_003CDisablePanelDeleyed_003Ed__10))]
	private IEnumerator DisablePanelDeleyed(Animator anim)
	{
		return null;
	}

	private void SetSelected(GameObject go)
	{
	}
}
