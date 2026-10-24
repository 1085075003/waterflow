using System;
using System.Collections.Generic;
using UnityEngine;

namespace Recovered.FlowWater
{
    /// <summary>Original changeColorP/U particle gradients evaluated from ARM64.</summary>
    public static class RecoveredNativeParticleColors
    {
        [Serializable] sealed class Catalog { public Entry[] cases; }
        [Serializable] sealed class Entry
        {
            public string type, family;
            public int color, configuration, colx;
            public bool waterfall, enabled;
            public ColorKey[] colors;
            public AlphaKey[] alpha;
        }
        [Serializable] sealed class ColorKey { public float[] color; public float time; }
        [Serializable] sealed class AlphaKey { public float alpha, time; }
        static Dictionary<string, Entry> recipes;
        public static int LastAppliedCount { get; private set; }
        static string Key(string type, string family, int color, int configuration, bool waterfall, int colx)
        { return type + ":" + family + ":" + color + ":" + configuration + ":" + waterfall + ":" + (family == "light" && colx != 0 ? 1 : 0); }
        static void EnsureCatalog()
        {
            if (recipes != null) return;
            recipes = new Dictionary<string, Entry>(StringComparer.Ordinal);
            TextAsset data = Resources.Load<TextAsset>("RecoveredNativeParticleColors");
            if (data == null) { Debug.LogError("Missing original particle color recipes."); return; }
            Catalog catalog = JsonUtility.FromJson<Catalog>(data.text);
            if (catalog == null || catalog.cases == null) return;
            foreach (Entry entry in catalog.cases)
                recipes[Key(entry.type, entry.family, entry.color, entry.configuration, entry.waterfall, entry.colx)] = entry;
        }
        public static void Apply(GameObject root)
        {
            LastAppliedCount = 0;
            if (root == null) return;
            EnsureCatalog();
            foreach (changeColorP original in root.GetComponentsInChildren<changeColorP>(true))
            {
                bool night = UIManagerSelScript0.parte >= 36 && UIManagerSelScript0.parte <= 39 ||
                    UIManagerSelScript0.parte == 0 && UIManagerSelScript0.subparte == 1;
                ApplyOne(original, original.ps, original.color, original.subcolor, night, original.colx);
            }
            foreach (changeColorU original in root.GetComponentsInChildren<changeColorU>(true))
                ApplyOne(original, original.ps, original.color, original.subcolor,
                    UIManagerSelScript0.parte == 36 || UIManagerSelScript0.parte == 37, original.colx);
        }
        static void ApplyOne(MonoBehaviour original, ParticleSystem particles, int color, int subcolor, bool night, int colx)
        {
            if (particles == null) return;
            string family = subcolor != 0 ? "light" : night ? "night" : "day";
            int configuration = subcolor != 0 ? subcolor : color == 3 ? UIManagerScript.confColor1 : color == 4 ? UIManagerScript.confColor2 : 0;
            Entry entry;
            if (!recipes.TryGetValue(Key(original.GetType().Name, family, color, configuration,
                original.gameObject.name.Contains("WFall"), colx), out entry)) return;
            var colors = new GradientColorKey[entry.colors.Length];
            var alpha = new GradientAlphaKey[entry.alpha.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                float[] value = entry.colors[i].color;
                colors[i] = new GradientColorKey(new Color(value[0], value[1], value[2], value[3]), entry.colors[i].time);
            }
            for (int i = 0; i < alpha.Length; i++) alpha[i] = new GradientAlphaKey(entry.alpha[i].alpha, entry.alpha[i].time);
            var gradient = new Gradient(); gradient.SetKeys(colors, alpha);
            ParticleSystem.ColorOverLifetimeModule module = particles.colorOverLifetime;
            module.enabled = entry.enabled; module.color = new ParticleSystem.MinMaxGradient(gradient);
            original.enabled = false; LastAppliedCount++;
        }
    }
}
