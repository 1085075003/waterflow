#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;

namespace Recovered.FlowWater.Editor
{
    public static class FlowWaterNativeJetRotationValidation
    {
        [MenuItem("Tools/Flow Water/Validate Native Rotating Jet Routes")]
        public static void Run()
        {
            // Reuse the actual production screen-ray pickup, hover, release,
            // original Save1 replay, spring/busy waits, and all-target checks.
            // The normal mode0 board is required; final proxy Solve is unused.
            MethodInfo begin = typeof(FlowWaterSave1GameplayValidation).GetMethod("Begin", BindingFlags.Static | BindingFlags.NonPublic);
            if (begin == null) throw new MissingMethodException("Original gameplay validation Begin");
            begin.Invoke(null, new object[] { new[] {1696,1656,1660}, true });
        }
    }
}
#endif
