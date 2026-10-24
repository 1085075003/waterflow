#if UNITY_EDITOR
using System;
using System.Reflection;
namespace Recovered.FlowWater.Editor
{
    public static class FlowWaterNativePreviewEvidenceValidation
    {
        // These original saved previews contain no counted goals. Their
        // real mode0 pointer gameplay must independently reach positive goals.
        public static void Run()
        {
            MethodInfo begin = typeof(FlowWaterSave1GameplayValidation).GetMethod("Begin", BindingFlags.Static | BindingFlags.NonPublic);
            if (begin == null) throw new MissingMethodException("Original gameplay validation Begin");
            ParameterInfo[] parameters = begin.GetParameters();
            object[] arguments = new object[parameters.Length];
            for (int i=0; i<parameters.Length; i++)
                arguments[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : Type.Missing;
            arguments[0] = new[] {609,622}; arguments[1] = true;
            begin.Invoke(null, arguments);
        }
    }
}
#endif
