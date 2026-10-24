using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityStandardAssets.Utility;

namespace UnityStandardAssets.Effects
{
	public class Explosive : MonoBehaviour
	{
		[CompilerGenerated]
		private sealed class _003COnCollisionEnter_003Ed__8 : IEnumerator<object>, IEnumerator, IDisposable
		{
			private int _003C_003E1__state;

			private object _003C_003E2__current;

			public Explosive _003C_003E4__this;

			public Collision col;

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
			public _003COnCollisionEnter_003Ed__8(int _003C_003E1__state)
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

		public Transform explosionPrefab;

		public float detonationImpactVelocity;

		public float sizeMultiplier;

		public bool reset;

		public float resetTimeDelay;

		private bool m_Exploded;

		private ObjectResetter m_ObjectResetter;

		private void Start()
		{
		}

		[IteratorStateMachine(typeof(_003COnCollisionEnter_003Ed__8))]
		private IEnumerator OnCollisionEnter(Collision col)
		{
			return null;
		}

		public void Reset()
		{
		}
	}
}
