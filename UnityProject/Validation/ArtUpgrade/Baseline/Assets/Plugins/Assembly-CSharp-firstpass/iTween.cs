using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

public class iTween : MonoBehaviour
{
	private delegate float EasingFunction(float start, float end, float Value);

	private delegate void ApplyTween();

	public enum EaseType
	{
		easeInQuad = 0,
		easeOutQuad = 1,
		easeInOutQuad = 2,
		easeInCubic = 3,
		easeOutCubic = 4,
		easeInOutCubic = 5,
		easeInQuart = 6,
		easeOutQuart = 7,
		easeInOutQuart = 8,
		easeInQuint = 9,
		easeOutQuint = 10,
		easeInOutQuint = 11,
		easeInSine = 12,
		easeOutSine = 13,
		easeInOutSine = 14,
		easeInExpo = 15,
		easeOutExpo = 16,
		easeInOutExpo = 17,
		easeInCirc = 18,
		easeOutCirc = 19,
		easeInOutCirc = 20,
		linear = 21,
		spring = 22,
		easeInBounce = 23,
		easeOutBounce = 24,
		easeInOutBounce = 25,
		easeInBack = 26,
		easeOutBack = 27,
		easeInOutBack = 28,
		easeInElastic = 29,
		easeOutElastic = 30,
		easeInOutElastic = 31,
		punch = 32
	}

	public enum LoopType
	{
		none = 0,
		loop = 1,
		pingPong = 2
	}

	public enum NamedValueColor
	{
		_Color = 0,
		_SpecColor = 1,
		_Emission = 2,
		_ReflectColor = 3
	}

	public static class Defaults
	{
		public static float time;

		public static float delay;

		public static NamedValueColor namedColorValue;

		public static LoopType loopType;

		public static EaseType easeType;

		public static float lookSpeed;

		public static bool isLocal;

		public static Space space;

		public static bool orientToPath;

		public static Color color;

		public static float updateTimePercentage;

		public static float updateTime;

		public static float lookAhead;

		public static bool useRealTime;

		public static Vector3 up;
	}

	private class CRSpline
	{
		public Vector3[] pts;

		public CRSpline(params Vector3[] pts)
		{
		}

		public Vector3 Interp(float t)
		{
			return default;
		}
	}

	[CompilerGenerated]
	private sealed class _003CStart_003Ed__229 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public iTween _003C_003E4__this;

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
		public _003CStart_003Ed__229(int _003C_003E1__state)
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

	[CompilerGenerated]
	private sealed class _003CTweenDelay_003Ed__145 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public iTween _003C_003E4__this;

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
		public _003CTweenDelay_003Ed__145(int _003C_003E1__state)
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

	[CompilerGenerated]
	private sealed class _003CTweenRestart_003Ed__147 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public iTween _003C_003E4__this;

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
		public _003CTweenRestart_003Ed__147(int _003C_003E1__state)
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

	public static List<Hashtable> tweens;

	public string id;

	public string type;

	public string method;

	public EaseType easeType;

	public float time;

	public float delay;

	public LoopType loopType;

	public bool isRunning;

	public bool isPaused;

	public string _name;

	private float runningTime;

	private float percentage;

	private float delayStarted;

	private bool kinematic;

	private bool isLocal;

	private bool loop;

	private bool reverse;

	private bool wasPaused;

	private bool physics;

	private Hashtable tweenArguments;

	private Space space;

	private EasingFunction ease;

	private ApplyTween apply;

	private AudioSource audioSource;

	private Vector3[] vector3s;

	private Vector2[] vector2s;

	private Color[,] colors;

	private float[] floats;

	private Rect[] rects;

	private CRSpline path;

	private Vector3 preUpdate;

	private Vector3 postUpdate;

	private NamedValueColor namedcolorvalue;

	private float lastRealTime;

	private bool useRealTime;

	private Transform thisTransform;

	public static void Init(GameObject target)
	{
	}

	public static void ValueTo(GameObject target, Hashtable args)
	{
	}

	public static void FadeFrom(GameObject target, float alpha, float time)
	{
	}

	public static void FadeFrom(GameObject target, Hashtable args)
	{
	}

	public static void FadeTo(GameObject target, float alpha, float time)
	{
	}

	public static void FadeTo(GameObject target, Hashtable args)
	{
	}

	public static void ColorFrom(GameObject target, Color color, float time)
	{
	}

	public static void ColorFrom(GameObject target, Hashtable args)
	{
	}

	public static void ColorTo(GameObject target, Color color, float time)
	{
	}

	public static void ColorTo(GameObject target, Hashtable args)
	{
	}

	public static void AudioFrom(GameObject target, float volume, float pitch, float time)
	{
	}

	public static void AudioFrom(GameObject target, Hashtable args)
	{
	}

	public static void AudioTo(GameObject target, float volume, float pitch, float time)
	{
	}

	public static void AudioTo(GameObject target, Hashtable args)
	{
	}

	public static void Stab(GameObject target, AudioClip audioclip, float delay)
	{
	}

	public static void Stab(GameObject target, Hashtable args)
	{
	}

	public static void LookFrom(GameObject target, Vector3 looktarget, float time)
	{
	}

	public static void LookFrom(GameObject target, Hashtable args)
	{
	}

	public static void LookTo(GameObject target, Vector3 looktarget, float time)
	{
	}

	public static void LookTo(GameObject target, Hashtable args)
	{
	}

	public static void MoveTo(GameObject target, Vector3 position, float time)
	{
	}

	public static void MoveTo(GameObject target, Hashtable args)
	{
	}

	public static void MoveFrom(GameObject target, Vector3 position, float time)
	{
	}

	public static void MoveFrom(GameObject target, Hashtable args)
	{
	}

	public static void MoveAdd(GameObject target, Vector3 amount, float time)
	{
	}

	public static void MoveAdd(GameObject target, Hashtable args)
	{
	}

	public static void MoveBy(GameObject target, Vector3 amount, float time)
	{
	}

	public static void MoveBy(GameObject target, Hashtable args)
	{
	}

	public static void ScaleTo(GameObject target, Vector3 scale, float time)
	{
	}

	public static void ScaleTo(GameObject target, Hashtable args)
	{
	}

	public static void ScaleFrom(GameObject target, Vector3 scale, float time)
	{
	}

	public static void ScaleFrom(GameObject target, Hashtable args)
	{
	}

	public static void ScaleAdd(GameObject target, Vector3 amount, float time)
	{
	}

	public static void ScaleAdd(GameObject target, Hashtable args)
	{
	}

	public static void ScaleBy(GameObject target, Vector3 amount, float time)
	{
	}

	public static void ScaleBy(GameObject target, Hashtable args)
	{
	}

	public static void RotateTo(GameObject target, Vector3 rotation, float time)
	{
	}

	public static void RotateTo(GameObject target, Hashtable args)
	{
	}

	public static void RotateFrom(GameObject target, Vector3 rotation, float time)
	{
	}

	public static void RotateFrom(GameObject target, Hashtable args)
	{
	}

	public static void RotateAdd(GameObject target, Vector3 amount, float time)
	{
	}

	public static void RotateAdd(GameObject target, Hashtable args)
	{
	}

	public static void RotateBy(GameObject target, Vector3 amount, float time)
	{
	}

	public static void RotateBy(GameObject target, Hashtable args)
	{
	}

	public static void ShakePosition(GameObject target, Vector3 amount, float time)
	{
	}

	public static void ShakePosition(GameObject target, Hashtable args)
	{
	}

	public static void ShakeScale(GameObject target, Vector3 amount, float time)
	{
	}

	public static void ShakeScale(GameObject target, Hashtable args)
	{
	}

	public static void ShakeRotation(GameObject target, Vector3 amount, float time)
	{
	}

	public static void ShakeRotation(GameObject target, Hashtable args)
	{
	}

	public static void PunchPosition(GameObject target, Vector3 amount, float time)
	{
	}

	public static void PunchPosition(GameObject target, Hashtable args)
	{
	}

	public static void PunchRotation(GameObject target, Vector3 amount, float time)
	{
	}

	public static void PunchRotation(GameObject target, Hashtable args)
	{
	}

	public static void PunchScale(GameObject target, Vector3 amount, float time)
	{
	}

	public static void PunchScale(GameObject target, Hashtable args)
	{
	}

	private void GenerateTargets()
	{
	}

	private void GenerateRectTargets()
	{
	}

	private void GenerateColorTargets()
	{
	}

	private void GenerateVector3Targets()
	{
	}

	private void GenerateVector2Targets()
	{
	}

	private void GenerateFloatTargets()
	{
	}

	private void GenerateColorToTargets()
	{
	}

	private void GenerateAudioToTargets()
	{
	}

	private void GenerateStabTargets()
	{
	}

	private void GenerateLookToTargets()
	{
	}

	private void GenerateMoveToPathTargets()
	{
	}

	private void GenerateMoveToTargets()
	{
	}

	private void GenerateMoveByTargets()
	{
	}

	private void GenerateScaleToTargets()
	{
	}

	private void GenerateScaleByTargets()
	{
	}

	private void GenerateScaleAddTargets()
	{
	}

	private void GenerateRotateToTargets()
	{
	}

	private void GenerateRotateAddTargets()
	{
	}

	private void GenerateRotateByTargets()
	{
	}

	private void GenerateShakePositionTargets()
	{
	}

	private void GenerateShakeScaleTargets()
	{
	}

	private void GenerateShakeRotationTargets()
	{
	}

	private void GeneratePunchPositionTargets()
	{
	}

	private void GeneratePunchRotationTargets()
	{
	}

	private void GeneratePunchScaleTargets()
	{
	}

	private void ApplyRectTargets()
	{
	}

	private void ApplyColorTargets()
	{
	}

	private void ApplyVector3Targets()
	{
	}

	private void ApplyVector2Targets()
	{
	}

	private void ApplyFloatTargets()
	{
	}

	private void ApplyColorToTargets()
	{
	}

	private void ApplyAudioToTargets()
	{
	}

	private void ApplyStabTargets()
	{
	}

	private void ApplyMoveToPathTargets()
	{
	}

	private void ApplyMoveToTargets()
	{
	}

	private void ApplyMoveByTargets()
	{
	}

	private void ApplyScaleToTargets()
	{
	}

	private void ApplyLookToTargets()
	{
	}

	private void ApplyRotateToTargets()
	{
	}

	private void ApplyRotateAddTargets()
	{
	}

	private void ApplyShakePositionTargets()
	{
	}

	private void ApplyShakeScaleTargets()
	{
	}

	private void ApplyShakeRotationTargets()
	{
	}

	private void ApplyPunchPositionTargets()
	{
	}

	private void ApplyPunchRotationTargets()
	{
	}

	private void ApplyPunchScaleTargets()
	{
	}

	[IteratorStateMachine(typeof(_003CTweenDelay_003Ed__145))]
	private IEnumerator TweenDelay()
	{
		return null;
	}

	private void TweenStart()
	{
	}

	[IteratorStateMachine(typeof(_003CTweenRestart_003Ed__147))]
	private IEnumerator TweenRestart()
	{
		return null;
	}

	private void TweenUpdate()
	{
	}

	private void TweenComplete()
	{
	}

	private void TweenLoop()
	{
	}

	public static Rect RectUpdate(Rect currentValue, Rect targetValue, float speed)
	{
		return default;
	}

	public static Vector3 Vector3Update(Vector3 currentValue, Vector3 targetValue, float speed)
	{
		return default;
	}

	public static Vector2 Vector2Update(Vector2 currentValue, Vector2 targetValue, float speed)
	{
		return default;
	}

	public static float FloatUpdate(float currentValue, float targetValue, float speed)
	{
		return 0f;
	}

	public static void FadeUpdate(GameObject target, Hashtable args)
	{
	}

	public static void FadeUpdate(GameObject target, float alpha, float time)
	{
	}

	public static void ColorUpdate(GameObject target, Hashtable args)
	{
	}

	public static void ColorUpdate(GameObject target, Color color, float time)
	{
	}

	public static void AudioUpdate(GameObject target, Hashtable args)
	{
	}

	public static void AudioUpdate(GameObject target, float volume, float pitch, float time)
	{
	}

	public static void RotateUpdate(GameObject target, Hashtable args)
	{
	}

	public static void RotateUpdate(GameObject target, Vector3 rotation, float time)
	{
	}

	public static void ScaleUpdate(GameObject target, Hashtable args)
	{
	}

	public static void ScaleUpdate(GameObject target, Vector3 scale, float time)
	{
	}

	public static void MoveUpdate(GameObject target, Hashtable args)
	{
	}

	public static void MoveUpdate(GameObject target, Vector3 position, float time)
	{
	}

	public static void LookUpdate(GameObject target, Hashtable args)
	{
	}

	public static void LookUpdate(GameObject target, Vector3 looktarget, float time)
	{
	}

	public static float PathLength(Transform[] path)
	{
		return 0f;
	}

	public static float PathLength(Vector3[] path)
	{
		return 0f;
	}

	public static void PutOnPath(GameObject target, Vector3[] path, float percent)
	{
	}

	public static void PutOnPath(Transform target, Vector3[] path, float percent)
	{
	}

	public static void PutOnPath(GameObject target, Transform[] path, float percent)
	{
	}

	public static void PutOnPath(Transform target, Transform[] path, float percent)
	{
	}

	public static Vector3 PointOnPath(Transform[] path, float percent)
	{
		return default;
	}

	public static void DrawLine(Vector3[] line)
	{
	}

	public static void DrawLine(Vector3[] line, Color color)
	{
	}

	public static void DrawLine(Transform[] line)
	{
	}

	public static void DrawLine(Transform[] line, Color color)
	{
	}

	public static void DrawLineGizmos(Vector3[] line)
	{
	}

	public static void DrawLineGizmos(Vector3[] line, Color color)
	{
	}

	public static void DrawLineGizmos(Transform[] line)
	{
	}

	public static void DrawLineGizmos(Transform[] line, Color color)
	{
	}

	public static void DrawLineHandles(Vector3[] line)
	{
	}

	public static void DrawLineHandles(Vector3[] line, Color color)
	{
	}

	public static void DrawLineHandles(Transform[] line)
	{
	}

	public static void DrawLineHandles(Transform[] line, Color color)
	{
	}

	public static Vector3 PointOnPath(Vector3[] path, float percent)
	{
		return default;
	}

	public static void DrawPath(Vector3[] path)
	{
	}

	public static void DrawPath(Vector3[] path, Color color)
	{
	}

	public static void DrawPath(Transform[] path)
	{
	}

	public static void DrawPath(Transform[] path, Color color)
	{
	}

	public static void DrawPathGizmos(Vector3[] path)
	{
	}

	public static void DrawPathGizmos(Vector3[] path, Color color)
	{
	}

	public static void DrawPathGizmos(Transform[] path)
	{
	}

	public static void DrawPathGizmos(Transform[] path, Color color)
	{
	}

	public static void DrawPathHandles(Vector3[] path)
	{
	}

	public static void DrawPathHandles(Vector3[] path, Color color)
	{
	}

	public static void DrawPathHandles(Transform[] path)
	{
	}

	public static void DrawPathHandles(Transform[] path, Color color)
	{
	}

	public static void Resume(GameObject target)
	{
	}

	public static void Resume(GameObject target, bool includechildren)
	{
	}

	public static void Resume(GameObject target, string type)
	{
	}

	public static void Resume(GameObject target, string type, bool includechildren)
	{
	}

	public static void Resume()
	{
	}

	public static void Resume(string type)
	{
	}

	public static void Pause(GameObject target)
	{
	}

	public static void Pause(GameObject target, bool includechildren)
	{
	}

	public static void Pause(GameObject target, string type)
	{
	}

	public static void Pause(GameObject target, string type, bool includechildren)
	{
	}

	public static void Pause()
	{
	}

	public static void Pause(string type)
	{
	}

	public static int Count()
	{
		return 0;
	}

	public static int Count(string type)
	{
		return 0;
	}

	public static int Count(GameObject target)
	{
		return 0;
	}

	public static int Count(GameObject target, string type)
	{
		return 0;
	}

	public static void Stop()
	{
	}

	public static void Stop(string type)
	{
	}

	public static void StopByName(string name)
	{
	}

	public static void Stop(GameObject target)
	{
	}

	public static void Stop(GameObject target, bool includechildren)
	{
	}

	public static void Stop(GameObject target, string type)
	{
	}

	public static void StopByName(GameObject target, string name)
	{
	}

	public static void Stop(GameObject target, string type, bool includechildren)
	{
	}

	public static void StopByName(GameObject target, string name, bool includechildren)
	{
	}

	public static Hashtable Hash(params object[] args)
	{
		return null;
	}

	private iTween(Hashtable h)
	{
	}

	private void Awake()
	{
	}

	[IteratorStateMachine(typeof(_003CStart_003Ed__229))]
	private IEnumerator Start()
	{
		return null;
	}

	private void Update()
	{
	}

	private void FixedUpdate()
	{
	}

	private void LateUpdate()
	{
	}

	private void OnEnable()
	{
	}

	private void OnDisable()
	{
	}

	private static void DrawLineHelper(Vector3[] line, Color color, string method)
	{
	}

	private static void DrawPathHelper(Vector3[] path, Color color, string method)
	{
	}

	private static Vector3[] PathControlPointGenerator(Vector3[] path)
	{
		return null;
	}

	private static Vector3 Interp(Vector3[] pts, float t)
	{
		return default;
	}

	private static void Launch(GameObject target, Hashtable args)
	{
	}

	private static Hashtable CleanArgs(Hashtable args)
	{
		return null;
	}

	private static string GenerateID()
	{
		return null;
	}

	private void RetrieveArgs()
	{
	}

	private void GetEasingFunction()
	{
	}

	private void UpdatePercentage()
	{
	}

	private void CallBack(string callbackType)
	{
	}

	private void Dispose()
	{
	}

	private void ConflictCheck()
	{
	}

	private void EnableKinematic()
	{
	}

	private void DisableKinematic()
	{
	}

	private void ResumeDelay()
	{
	}

	private float linear(float start, float end, float value)
	{
		return 0f;
	}

	private float clerp(float start, float end, float value)
	{
		return 0f;
	}

	private float spring(float start, float end, float value)
	{
		return 0f;
	}

	private float easeInQuad(float start, float end, float value)
	{
		return 0f;
	}

	private float easeOutQuad(float start, float end, float value)
	{
		return 0f;
	}

	private float easeInOutQuad(float start, float end, float value)
	{
		return 0f;
	}

	private float easeInCubic(float start, float end, float value)
	{
		return 0f;
	}

	private float easeOutCubic(float start, float end, float value)
	{
		return 0f;
	}

	private float easeInOutCubic(float start, float end, float value)
	{
		return 0f;
	}

	private float easeInQuart(float start, float end, float value)
	{
		return 0f;
	}

	private float easeOutQuart(float start, float end, float value)
	{
		return 0f;
	}

	private float easeInOutQuart(float start, float end, float value)
	{
		return 0f;
	}

	private float easeInQuint(float start, float end, float value)
	{
		return 0f;
	}

	private float easeOutQuint(float start, float end, float value)
	{
		return 0f;
	}

	private float easeInOutQuint(float start, float end, float value)
	{
		return 0f;
	}

	private float easeInSine(float start, float end, float value)
	{
		return 0f;
	}

	private float easeOutSine(float start, float end, float value)
	{
		return 0f;
	}

	private float easeInOutSine(float start, float end, float value)
	{
		return 0f;
	}

	private float easeInExpo(float start, float end, float value)
	{
		return 0f;
	}

	private float easeOutExpo(float start, float end, float value)
	{
		return 0f;
	}

	private float easeInOutExpo(float start, float end, float value)
	{
		return 0f;
	}

	private float easeInCirc(float start, float end, float value)
	{
		return 0f;
	}

	private float easeOutCirc(float start, float end, float value)
	{
		return 0f;
	}

	private float easeInOutCirc(float start, float end, float value)
	{
		return 0f;
	}

	private float easeInBounce(float start, float end, float value)
	{
		return 0f;
	}

	private float easeOutBounce(float start, float end, float value)
	{
		return 0f;
	}

	private float easeInOutBounce(float start, float end, float value)
	{
		return 0f;
	}

	private float easeInBack(float start, float end, float value)
	{
		return 0f;
	}

	private float easeOutBack(float start, float end, float value)
	{
		return 0f;
	}

	private float easeInOutBack(float start, float end, float value)
	{
		return 0f;
	}

	private float punch(float amplitude, float value)
	{
		return 0f;
	}

	private float easeInElastic(float start, float end, float value)
	{
		return 0f;
	}

	private float easeOutElastic(float start, float end, float value)
	{
		return 0f;
	}

	private float easeInOutElastic(float start, float end, float value)
	{
		return 0f;
	}
}
