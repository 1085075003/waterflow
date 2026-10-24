using System;
using System.Reflection;
using UnityEngine;

namespace Recovered.FlowWater
{
    // Unity scaled seconds read from actual authored input controllers. Shared
    // by the board and the original menu models, without a per-cell guess.
    public static class RecoveredWaterTiming
    {
        public static MonoBehaviour Origin(GameObject prototype, int incoming, string prefix = "FuenteTest")
        {
            if (prototype == null) return null;
            MonoBehaviour fallback = null;
            foreach (MonoBehaviour component in prototype.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || !component.GetType().Name.StartsWith(prefix, StringComparison.Ordinal)) continue;
                if (component.name == "Origen" + incoming) return component;
                if (fallback == null || Integer(component, "origen") == incoming) fallback = component;
            }
            return fallback;
        }
        public static float Outlet(MonoBehaviour controller, GameObject prototype, int incoming, bool pedestal, bool basin = false)
        {
            if (controller is ControlPiezaFuente || controller is CascadaTest3 || controller is CascadaTest1) return 0;
            MonoBehaviour origin = Origin(prototype, incoming, pedestal ? "FuenteCubeta2" : basin ? "FuenteCubeta" : "FuenteTest");
            object value = Value(origin, "espera");
            return value is float ? Mathf.Max(0, (float)value) : 0;
        }
        public static float Cascade(float drop, bool bottom)
        {
            int height = Mathf.Clamp(Mathf.RoundToInt(drop / 7.6f) + (bottom ? 1 : 0), 1, 5);
            return height <= 3 ? 1 : height == 4 ? 1.1f : 1.2f;
        }
        public static float Secondary(MonoBehaviour controller, int incoming, int outgoing)
        {
            if (!(controller is ControlPiezaC) && !(controller is AguaControlC) && !(controller is AguasControl3)) return 0;
            bool firstAxis = incoming == 1 || incoming == 3 || incoming >= 5;
            return outgoing < 4 && (outgoing % 2 == 0) != firstAxis ? .1f : 0;
        }
        static object Value(object target, string name)
        { FieldInfo field = target == null ? null : target.GetType().GetField(name); return field == null ? null : field.GetValue(target); }
        static int Integer(object target, string name) { object value = Value(target, name); return value is int ? (int)value : 0; }
    }
}
