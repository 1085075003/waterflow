using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Recovered.FlowWater
{
    /// <summary>Original FuenteTest3.doActivarChorro and C/D/E particle recipes.</summary>
    public static class RecoveredWaterJets
    {
        public sealed class Route
        {
            public string configuration, spring, secondarySpring, type;
            public int slot, mode, direction;
            public Vector3 displacement;
            public float yaw;
            public Transform receiver;
        }
        [Serializable] sealed class Catalog { public Entry[] entries; }
        [Serializable] sealed class Entry { public string configuration; public int color; public Recipe[] recipes; }
        [Serializable] sealed class Recipe { public string prefabField; public float[] localPosition, quaternion, localScale; }
        [Serializable] sealed class ParticleCatalog { public ParticleCase[] cases; }
        [Serializable] sealed class ParticleCase { public string type; public int mode, height; public bool diagonal; public Property[] properties; }
        [Serializable] sealed class Property { public string module, field; public float value; }
        static Entry[] entries;
        static ParticleCase[] particleCases;
        static readonly Vector3[] Direction = { Vector3.zero, Vector3.left, new Vector3(-1,0,1), Vector3.forward,
            new Vector3(1,0,1), Vector3.right, new Vector3(1,0,-1), Vector3.back, new Vector3(-1,0,-1) };
        static readonly float[] Yaw = { 0,90,135,180,225,-90,-45,0,45 };
        public static List<Route> ReadRoutes(MonoBehaviour control)
        {
            var routes = new List<Route>();
            bool categoryDemo = control.GetComponentInParent<RecoveredPreviewWater>() != null && control.GetComponentInParent<RecoveryPiece>() == null;
            int kind = Number(control, "tipoPieza");
            if (kind != 8 && kind != 9 && Number(control, "configFuenteB") != 9) return routes;
            if (Number(control, "configFuente") == 0) return routes;
            for (int slot = 1; slot <= 4; slot++)
            {
                string code = Text(control, "configPit" + slot);
                if (String.IsNullOrEmpty(code) || code.Length < 2 || code[1] < '1' || code[1] > '8') continue;
                char initial = code[0];
                if (initial != '1' && initial != '2' && initial != '3' && initial != '4' && initial != '7') continue;
                var route = new Route { slot = slot, configuration = code,
                    type = initial == '1' ? "CascadaTestD" : initial == '2' ? "CascadaTestC" : "CascadaTestE",
                    mode = initial == '3' || initial == '1' ? 1 : initial == '7' ? 3 : 2, direction = code[1] - '0' };
                // FuenteTest3.doActivarChorro dispatches the first character
                // as a jet slot, then passes Substring(1,3) to CascadaTestE.
                // Slots2..4 also read configRes1, not configRes2..4.
                // The original slot4 branch compares literal"3" as does slot3.
                if (initial == '3' || initial == '4')
                {
                    route.spring = SpringForSlot(Text(control, "configRes1"), slot);
                    route.secondarySpring = slot == 1 ? SpringForSlot(Text(control, "configRes2"), 1) : "";
                }
                if (route.type == "CascadaTestE" && !String.IsNullOrEmpty(route.spring) && route.spring.Length >= 3)
                {
                    int channel = route.spring[0] - '0';
                    if ((categoryDemo ? UIManagerSelScript0.estadoResorte1 : channel >= 1 && channel <= 4 ? Global("conResorte" + channel) : 0) == 1)
                    {
                        int mode = route.spring[1] - '0', direction = route.spring[2] - '0';
                        if (mode >= 1 && mode <= 3 && direction >= 1 && direction <= 8) { route.mode = mode; route.direction = direction; }
                    }
                }
                // FT3.doActivarChorro instantiates its wrapper with the
                // original configured WORLD quaternion, then set_parent
                // preserves that quaternion. A spring turn rotates the intake,
                // while a newly emitted jet retains its configured world route.
                route.displacement = Direction[route.direction] * (route.mode == 1 ? 40 : 20);
                route.yaw = Yaw[route.direction]; routes.Add(route);
            }
            return routes;
        }
        public static string SpringForSlot(string configuration, int slot)
        {
            int nativeSlot = slot == 4 ? 3 : slot;
            return !String.IsNullOrEmpty(configuration) && configuration.Length >= 4 &&
                nativeSlot >= 1 && nativeSlot <= 3 && configuration[0] == (char)('0' + nativeSlot)
                ? configuration.Substring(1, 3) : "";
        }
        public static void Restore(MonoBehaviour origin, int color, IList<Route> routes, Func<GameObject, Transform, GameObject> spawn)
        {
            EnsureCatalogs();
            foreach (Route route in routes)
            {
                Entry found = null;
                foreach (Entry entry in entries) if (entry.color == color && entry.configuration == route.configuration) { found = entry; break; }
                if (found == null) continue;
                foreach (Recipe recipe in found.recipes)
                {
                    GameObject prefab = Reference<GameObject>(origin, recipe.prefabField);
                    if (prefab == null) continue;
                    GameObject wrapper = spawn(prefab, origin.transform);
                    wrapper.transform.localPosition = Vector(recipe.localPosition);
                    bool categoryDemo = origin.GetComponentInParent<RecoveredPreviewWater>() != null && origin.GetComponentInParent<RecoveryPiece>() == null;
                    // Native Instantiate(position, rotation), followed by
                    // Transform.set_parent, preserves the recipe world yaw.
                    // Menu previews use a separate transformed display frame.
                    wrapper.transform.rotation = (categoryDemo ? origin.transform.rotation : Quaternion.identity) * Rotation(recipe.quaternion);
                    if (recipe.localScale != null && recipe.localScale.Length == 3) wrapper.transform.localScale = Vector(recipe.localScale);
                    else WorldScale(wrapper.transform, prefab.transform.localScale);
                    MonoBehaviour jet = null;
                    foreach (MonoBehaviour component in wrapper.GetComponentsInChildren<MonoBehaviour>(true))
                        if (component != null && component.GetType().Name == route.type) { jet = component; break; }
                    if (jet == null) continue;
                    Set(jet, "pitValor", route.type == "CascadaTestE" ? route.configuration : route.configuration.Substring(1, 1));
                    Set(jet, "color", color); Set(jet, "conResorte1", route.spring ?? "");
                    if (route.type == "CascadaTestE") Set(jet, "conResorte2", route.secondarySpring ?? "");
                    string modelField = (route.type == "CascadaTestC" ? "ChorroCortoColor" : "ChorroLargoColor") + color;
                    GameObject modelPrefab = Reference<GameObject>(jet, modelField);
                    if (modelPrefab == null) continue;
                    GameObject model = spawn(modelPrefab, jet.transform);
                    model.transform.localPosition = Vector3.up * (route.type == "CascadaTestC" ? 3.5f : route.type == "CascadaTestD" ? 4 : 3.8f);
                    model.transform.localRotation = Quaternion.Euler(0, route.yaw, 0);
                    model.transform.localScale = route.type == "CascadaTestC" ? new Vector3(.49f,.01f,.01f) : Vector3.one;
                    int height = route.receiver == null ? -4 : Mathf.Clamp(Mathf.RoundToInt((route.receiver.position.y - origin.transform.position.y) / 7.6f), -4, 5);
                    foreach (ParticleSystem system in model.GetComponentsInChildren<ParticleSystem>(true)) ApplyParticleProperties(system, route, height);
                }
            }
        }
        static void ApplyParticleProperties(ParticleSystem system, Route route, int height)
        {
            bool diagonal = route.direction % 2 == 0;
            foreach (ParticleCase entry in particleCases)
            {
                if (entry.height != height || entry.diagonal != diagonal) continue;
                if (route.type == "CascadaTestE" ? entry.mode != route.mode || !String.IsNullOrEmpty(entry.type) : entry.type != route.type) continue;
                foreach (Property property in entry.properties)
                {
                    if (property.module == "VelocityOverLifetimeModule")
                    {
                        var velocity = system.velocityOverLifetime; velocity.enabled = true;
                        if (property.field == "x") velocity.x = property.value;
                        else if (property.field == "y") velocity.y = property.value;
                        else if (property.field == "z") velocity.z = property.value;
                    }
                    else if (property.module == "ForceOverLifetimeModule")
                    {
                        var force = system.forceOverLifetime; force.enabled = true;
                        if (property.field == "x") force.x = property.value;
                        else if (property.field == "y") force.y = property.value;
                        else if (property.field == "z") force.z = property.value;
                    }
                    else if (property.module == "MainModule" && property.field == "startLifetime") { var main = system.main; main.startLifetime = property.value; }
                }
                break;
            }
        }
        static void EnsureCatalogs()
        {
            if (entries != null) return;
            TextAsset recipes = Resources.Load<TextAsset>("RecoveredNativeJetRecipes");
            entries = recipes == null ? new Entry[0] : JsonUtility.FromJson<Catalog>(recipes.text).entries;
            TextAsset particles = Resources.Load<TextAsset>("RecoveredNativeJetParticles");
            particleCases = particles == null ? new ParticleCase[0] : JsonUtility.FromJson<ParticleCatalog>(particles.text).cases;
        }
        public static int Global(string name) { FieldInfo field = typeof(UIManagerScript).GetField(name, BindingFlags.Public | BindingFlags.Static); return field == null ? 0 : (int)field.GetValue(null); }
        static Vector3 Vector(float[] a) { return a == null || a.Length < 3 ? Vector3.zero : new Vector3(a[0],a[1],a[2]); }
        static Quaternion Rotation(float[] a) { return a == null || a.Length < 4 ? Quaternion.identity : new Quaternion(a[0],a[1],a[2],a[3]); }
        static object Value(object source, string name) { FieldInfo field = source.GetType().GetField(name); return field == null ? null : field.GetValue(source); }
        static int Number(object source, string name) { object value = Value(source,name); return value is int ? (int)value : 0; }
        static string Text(object source, string name) { return Value(source,name) as string; }
        static T Reference<T>(object source,string name) where T : UnityEngine.Object { return Value(source,name) as T; }
        static void Set(object source,string name,object value) { FieldInfo field = source.GetType().GetField(name); if (field != null) field.SetValue(source,value); }
        static void WorldScale(Transform target, Vector3 desired)
        {
            Vector3 parent = target.parent == null ? Vector3.one : target.parent.lossyScale;
            target.localScale = new Vector3(desired.x / Mathf.Max(.0001f,Mathf.Abs(parent.x)),desired.y / Mathf.Max(.0001f,Mathf.Abs(parent.y)),desired.z / Mathf.Max(.0001f,Mathf.Abs(parent.z)));
        }
    }
}

