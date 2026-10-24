using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Game.EnableObjectsWithDelay
{
	public class EnableObjectsWithDelay : MonoBehaviour
	{
		[Serializable]
		public class ObjectDelay
		{
			public GameObject targetObject;

			public float startDelay;
		}

		[CompilerGenerated]
		private sealed class _003CEnableObjectAfterDelay_003Ed__4 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public float delay;

			public GameObject targetObject;

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
			public _003CEnableObjectAfterDelay_003Ed__4(int _003C_003E1__state)
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

		[SerializeField]
		private List<ObjectDelay> objectsWithDelays;

		private void Start()
		{
		}

		private void Update()
		{
		}

		[IteratorStateMachine(typeof(_003CEnableObjectAfterDelay_003Ed__4))]
		private IEnumerator EnableObjectAfterDelay(GameObject targetObject, float delay)
		{
			return null;
		}

		private void PlayStarEndAnimationForAll()
		{
		}
	}
}
