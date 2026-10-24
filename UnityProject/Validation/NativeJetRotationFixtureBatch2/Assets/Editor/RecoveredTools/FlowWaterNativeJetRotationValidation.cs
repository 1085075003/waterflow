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
            ParameterInfo[] parameters = begin.GetParameters();
            object[] arguments = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
                arguments[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : Type.Missing;
            arguments[0] = new[] {1696,1656,1660}; arguments[1] = true;
            begin.Invoke(null, arguments);
        }
    }
}
#endif
