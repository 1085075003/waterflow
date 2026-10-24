#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Recovered.FlowWater.Editor
{
    // On-demand Editor QA. No component is added to the game or shipped.
    [InitializeOnLoad]
    public static class FlowWaterArtReview
    {
        const string Key = "FlowWater.ArtReview";
        static double next;
        static int phase, page;
        static string Output { get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation/ArtUpgrade/Review"); } }
        [Serializable] sealed class Page { public string name, screenshot; public int visibleTexts, visibleImages; public List<string> issues = new List<string>(); }
        [Serializable] sealed class Report { public string utc, unity; public bool passed; public List<Page> pages = new List<Page>(); public List<string> errors = new List<string>(); }
        [Serializable] sealed class Details { public int[] ids; }
        static Report report;
        static readonly string[] Pages = { "home", "settings", "store", "categories", "levels", "dialog" };
        static FlowWaterArtReview() { EditorApplication.update += Update; Application.logMessageReceived += Log; }

        public static void Apply()
        {
            Directory.CreateDirectory(Output);
            FlowWaterValidation.SaveVisibleFrame(Path.Combine(Output, "edit-preview.png"));
        }
        public static void Capture()
        {
            Directory.CreateDirectory(Output);
            SessionState.SetBool(Key, true);
            EditorSceneManager.OpenScene("Assets/Scenes/Portada.unity");
            EditorApplication.EnterPlaymode();
        }
        public static void Audit()
        {
            // Cover every recovered pack theme, the reported inlet, and large/complex boards.
            var available = new HashSet<int>(RecoveryCatalog.Load().levels.Select(level => level.assetId));
            int[] first = Enumerable.Range(1, 39).Select(part => OriginalScenePresentation.AssetForLevel(part, 1)).Where(available.Contains).ToArray();
            int[] extra = { 407, 518, 626, 1058, 1095, 2001, 13, 82, 113, 158, 275, 355, 403, 514, 552, 605, 651, 707, 751, 1002, 1054, 1103, 1152, 1204 };
            string review = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation/VisualReview");
            Directory.CreateDirectory(review);
            File.WriteAllText(Path.Combine(review, "detail-request.json"), JsonUtility.ToJson(new Details { ids = first.Concat(extra).Where(available.Contains).Distinct().ToArray() }, true));
            FlowWaterFullValidation.RunVisualDetails();
        }
        public static void Export() { FlowWaterFullValidation.RunRegression(); }
        static void Update()
        {
            if (!SessionState.GetBool(Key, false) || EditorApplication.isCompiling || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup < next) return;
            var ui = FlowWaterRecoveryUI.Instance;
            if (ui == null) return;
            try
            {
                if (report == null) { report = new Report { utc = DateTime.UtcNow.ToString("o"), unity = Application.unityVersion }; phase = page = 0; }
                if (page == Pages.Length)
                {
                    report.passed = report.errors.Count == 0 && report.pages.All(p => p.issues.Count == 0);
                    File.WriteAllText(Path.Combine(Output, "ui-review.json"), JsonUtility.ToJson(report, true));
                    SessionState.EraseBool(Key); report = null; EditorApplication.isPlaying = false; return;
                }
                if (phase == 0)
                {
                    switch (Pages[page])
                    {
                        case "home": ui.GoHome(); break;
                        case "settings": Invoke(ui, "OpenSettings"); break;
                        case "store": Invoke(ui, "OpenStore"); break;
                        case "categories": SceneManager.LoadScene("SelLevel0"); break;
                        case "levels": ui.OpenPack(1); break;
                        case "dialog": ui.Notice("A quiet garden, one stream at a time."); break;
                    }
                    phase = 1; next = EditorApplication.timeSinceStartup + 2; return;
                }
                Canvas.ForceUpdateCanvases();
                var result = new Page { name = Pages[page], screenshot = Path.Combine(Output, Pages[page] + ".png") };
                foreach (Text label in UnityEngine.Object.FindObjectsOfType<Text>())
                {
                    if (!label.isActiveAndEnabled || string.IsNullOrWhiteSpace(label.text) || label.color.a < .01f) continue;
                    result.visibleTexts++; if (label.font == null) result.issues.Add("Missing font: " + label.name);
                }
                foreach (Image image in UnityEngine.Object.FindObjectsOfType<Image>())
                {
                    if (!image.isActiveAndEnabled || image.color.a < .01f) continue;
                    result.visibleImages++;
                    if (image.material != null && !image.material.shader.isSupported) result.issues.Add("Unsupported UI shader: " + image.name);
                }
                FlowWaterValidation.SaveVisibleFrame(result.screenshot);
                FlowWaterValidation.SaveVisibleFrame(Path.Combine(Output, Pages[page] + "-885x630.png"), 885, 630);
                report.pages.Add(result);
                page++; phase = 0; next = EditorApplication.timeSinceStartup + .2;
            }
            catch (Exception exception)
            {
                if (report != null) { report.errors.Add(exception.ToString()); File.WriteAllText(Path.Combine(Output, "ui-review.json"), JsonUtility.ToJson(report, true)); }
                SessionState.EraseBool(Key); report = null; EditorApplication.isPlaying = false;
            }
        }
        static void Invoke(object instance, string name) { instance.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance, null); }
        static void Log(string message, string stack, LogType type) { if (report != null && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) report.errors.Add(message); }
    }
}
#endif
