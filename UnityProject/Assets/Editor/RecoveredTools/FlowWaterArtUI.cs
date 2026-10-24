#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Recovered.FlowWater.Editor
{
    /// <summary>Replaces only presentation assets on the original authored UI.</summary>
    public static class FlowWaterArtUI
    {
        const string AssetRoot = "Assets/Resources/ArtUpgrade/UI/";
        static readonly Color Ink = new Color32(36, 63, 68, 255);
        static readonly string[] ScenePaths = {
            "Assets/Scenes/Portada.unity", "Assets/Scenes/SelLevel0.unity", "Assets/Scenes/SelLevel1.unity",
            "Assets/Scenes/Levely5z/Levely5.unity", "Assets/Scenes/Levely5_basic.unity",
            "Assets/Scenes/Levely5_6x6.unity", "Assets/Scenes/Levely5_7x7.unity", "Assets/Settings.unity", "Assets/Store.unity"
        };
        static readonly Dictionary<string, string> Mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "water994d", "wordmark" }, { "3dpuzzle2b", "subtitle" },
            { "SF Button5", "card" }, { "SF Button3", "card" }, { "SF Button5b", "inset" },
            { "SF Button5c1", "card" }, { "SF Button5c4", "tab" }, { "SF Button5c6b", "ring" },
            { "cajaNivel9b", "button" }, { "cajaNivel8b1", "highlight" }, { "cajaNivel12x", "button" },
            { "boton6dx", "hud" }, { "boton6d2", "round" }, { "boton6d3", "ring" }, { "Boton6", "badge" },
            { "Boton9b", "close" }, { "Box-9", "inset" }, { "btn_9slice_normal", "primary" },
            { "back1", "undo" }, { "image3x1", "arrow-left" }, { "flecha1", "arrow-right" }, { "flecha3", "arrow-right" },
            { "flechalanz", "play" }, { "Icon_11c", "check" }, { "check6", "check" }, { "Icon_11b", "close" }, { "cruz1", "close" },
            { "menu2", "menu" }, { "Cart", "cart" }, { "Icon_04", "restart" }, { "Icon_05", "music" }, { "Icon_23", "sound" },
            { "lightBulb1", "bulb" }, { "candado3", "lock" }, { "RewardedV", "video" }, { "turn2c", "rotate" },
            { "huellas2", "footsteps" }, { "starx3", "star" }, { "starx7", "star" }, { "starx8", "star" },
            { "cinta11", "transparent" }, { "Circle", "round" }, { "SinAds", "no-ads" }, { "SFbarra", "inset" },
            { "Store1", "garden-1" }, { "Store2c", "garden-2" }, { "Store3c", "garden-3" },
            { "Store4c", "garden-4" }, { "Store5c", "garden-5" }, { "Store6c", "garden-6" },
            { "image2", "garden-4" }, { "test2", "garden-4" },
            { "fondo1", "background-day" }, { "fondo2", "background-day" }, { "fondo3", "background-day" },
            { "fondo4", "background-soft" }, { "fondo6", "background-night" }, { "fondo7b", "background-day" },
            { "fondo7c2", "background-night" }, { "fondo8b", "background-soft" }, { "fondo9", "background-night" },
            { "fondo11", "background-soft" }, { "fondo15", "background-day" }, { "fondo20", "background-soft" },
            { "fondo23b", "background-soft" }, { "fondo24b", "background-soft" }, { "fondo25b", "background-soft" },
            { "fondo27", "background-soft" }, { "fondo30", "background-soft" }
        };
        static readonly HashSet<string> Sliced = new HashSet<string> { "card", "button", "primary", "inset", "gold", "hud", "tab", "badge", "highlight", "tab-overlay" };
        static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        [Serializable] public sealed class SceneChange { public string scene; public int images, texts, effects, serializedSprites, hudTitleOutlines, hudTitleShadows; public bool interactionInvariant; }
        [Serializable] public sealed class Report { public string generatedUtc, unityVersion; public bool passed; public List<SceneChange> scenes = new List<SceneChange>(); }

        [MenuItem("Tools/Flow Water/Art Upgrade/Apply UI Theme")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Apply the UI theme from Edit mode.");
            string project = Directory.GetParent(Application.dataPath).FullName;
            string backup = Path.Combine(project, "Validation", "ArtUpgrade", "Before", "UI");
            Directory.CreateDirectory(backup);
            ConfigureImports();
            Font regular = AssetDatabase.LoadAssetAtPath<Font>(AssetRoot + "Fonts/Lato-Regular.ttf");
            Font bold = AssetDatabase.LoadAssetAtPath<Font>(AssetRoot + "Fonts/Lato-Bold.ttf");
            if (regular == null || bold == null) throw new InvalidOperationException("The Water Garden fonts were not imported.");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            var report = new Report { generatedUtc = DateTime.UtcNow.ToString("o"), unityVersion = Application.unityVersion, passed = true };
            try
            {
                foreach (string path in ScenePaths)
                {
                    string original = Path.Combine(project, path);
                    if (!File.Exists(original)) continue;
                    string copy = Path.Combine(backup, path.Replace('/', Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(Path.GetDirectoryName(copy));
                    if (!File.Exists(copy)) File.Copy(original, copy);
                    if (File.Exists(original + ".meta") && !File.Exists(copy + ".meta")) File.Copy(original + ".meta", copy + ".meta");
                    Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    string invariant = InteractionInvariant(scene);
                    SceneChange changed = StyleScene(scene, regular, bold);
                    changed.scene = path;
                    changed.interactionInvariant = invariant == InteractionInvariant(scene);
                    if (!changed.interactionInvariant) throw new InvalidOperationException("UI interaction invariant changed: " + path);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    report.scenes.Add(changed);
                }
                AssetDatabase.SaveAssets();
                File.WriteAllText(Path.Combine(project, "Validation", "ArtUpgrade", "UI-apply-report.json"), JsonUtility.ToJson(report, true));
                Debug.Log("Water Garden UI applied to " + report.scenes.Count + " scenes. Original hierarchy, order, input rectangles, events and raycasts verified unchanged.");
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        static void ConfigureImports()
        {
            Sprites.Clear();
            foreach (string file in Directory.GetFiles(AssetRoot + "Sprites", "*.png"))
            {
                string path = file.Replace('\\', '/');
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Missing importer: " + path);
                string name = Path.GetFileNameWithoutExtension(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.spriteBorder = Sliced.Contains(name) ? new Vector4(28, 28, 28, 28) : Vector4.zero;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 2048;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
                Sprites[name] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            string fallbackPath = AssetRoot + "Fonts/NotoSansSC-Regular.otf";
            AssetDatabase.ImportAsset(fallbackPath, ImportAssetOptions.ForceSynchronousImport);
            Font fallback = AssetDatabase.LoadAssetAtPath<Font>(fallbackPath);
            foreach (string name in new[] { "Lato-Regular", "Lato-Bold" })
            {
                string path = AssetRoot + "Fonts/" + name + ".ttf";
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = AssetImporter.GetAtPath(path) as TrueTypeFontImporter;
                if (importer == null) continue;
                importer.includeFontData = true;
                var serialized = new SerializedObject(importer);
                var property = serialized.FindProperty("fallbackFontReferences") ?? serialized.FindProperty("m_FallbackFontReferences");
                if (property != null && fallback != null)
                {
                    property.arraySize = 1;
                    property.GetArrayElementAtIndex(0).objectReferenceValue = fallback;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                importer.SaveAndReimport();
            }
        }

        static SceneChange StyleScene(Scene scene, Font regular, Font bold)
        {
            var change = new SceneChange();
            Dictionary<ulong, Sprite> originalSprites = OriginalSceneSprites(scene.path);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Image image in root.GetComponentsInChildren<Image>(true))
                {
                    string art;
                    string path = HierarchyPath(image.transform);
                    bool levelBackplate = scene.name.StartsWith("Levely5", StringComparison.Ordinal) && image.transform.parent != null && image.transform.parent.name == "Level" && image.name.StartsWith("Label", StringComparison.Ordinal);
                    Sprite original;
                    Sprite source = originalSprites.TryGetValue(GlobalObjectId.GetGlobalObjectIdSlow(image).targetObjectId, out original) ? original : image.sprite;
                    if (levelBackplate) art = "card";
                    else if (source == null || !TryArtName(source, out art)) continue;
                    if (source != null && image.transform.childCount == 0 && (source.name == "SF Button3" || source.name == "SF Button5" || source.name == "SF Button5c1")) art = "highlight";
                    if (source != null && image.transform.childCount == 0 && source.name == "SF Button5c4") art = "tab-overlay";
                    if (path.EndsWith("/Background (1)", StringComparison.Ordinal) && art == "primary") art = "highlight";
                    if (scene.name.StartsWith("Levely5", StringComparison.Ordinal) && art == "background-night") art = "background-night-tinted";
                    if (path.EndsWith("/Play/Background", StringComparison.Ordinal)) art = "primary";
                    if (path.Contains("/Slider") && path.EndsWith("/Handle", StringComparison.Ordinal)) art = "round";
                    image.sprite = Sprites[art];
                    image.type = Sliced.Contains(art) ? Image.Type.Sliced : Image.Type.Simple;
                    image.preserveAspect = !Sliced.Contains(art) && !art.StartsWith("background", StringComparison.Ordinal);
                    image.color = new Color(1, 1, 1, levelBackplate ? .96f : image.color.a);
                    // Original rounded buttons contain authored skew in their leaf art. Keep the hit target unchanged.
                    if (!image.raycastTarget && image.transform.childCount == 0 && image.GetComponent<Selectable>() == null && image.GetComponent<EventTrigger>() == null &&
                        (Sliced.Contains(art) || art == "wordmark" || art == "subtitle"))
                        image.rectTransform.localRotation = Quaternion.identity;
                    EditorUtility.SetDirty(image); change.images++;
                }
                foreach (Text text in root.GetComponentsInChildren<Text>(true))
                {
                    bool numeric = text.text.Trim().All(c => Char.IsDigit(c) || c == '+' || c == '-' || Char.IsWhiteSpace(c));
                    bool label = text.fontSize >= 25 || numeric;
                    text.font = label ? bold : regular;
                    text.fontStyle = FontStyle.Normal;
                    text.color = new Color(Ink.r, Ink.g, Ink.b, text.color.a);
                    text.lineSpacing = 1.03f;
                    int levelNumber;
                    if (scene.name == "SelLevel1" && Int32.TryParse(text.text.Trim(), out levelNumber))
                    { text.font = regular; text.fontSize = 44; text.resizeTextMaxSize = 44; }
                    // Preserve authored sizing and localization-driven best-fit limits.
                    foreach (var effect in text.GetComponents<Shadow>()) { effect.enabled = false; EditorUtility.SetDirty(effect); change.effects++; }
                    if (scene.name.StartsWith("Levely5", StringComparison.Ordinal) && HierarchyPath(text.transform).Contains("/PanelRotation/"))
                    {
                        text.color = new Color32(240, 240, 230, (byte)Mathf.RoundToInt(text.color.a * 255));
                        foreach (var effect in text.GetComponents<Shadow>())
                        { effect.enabled = true; effect.effectColor = new Color32(36, 63, 68, 220); effect.effectDistance = new Vector2(1, -1); }
                    }
                    if (scene.name.StartsWith("Levely5", StringComparison.Ordinal) && HierarchyPath(text.transform).Contains("/Level/"))
                    {
                        foreach (var effect in text.GetComponents<Shadow>())
                        {
                            effect.enabled = true;
                            bool outline = effect is Outline;
                            effect.effectColor = outline ? new Color32(240, 240, 230, 255) : new Color32(20, 39, 43, 100);
                            effect.effectDistance = outline ? new Vector2(1.25f, -1.25f) : new Vector2(1.5f, -1.5f);
                            EditorUtility.SetDirty(effect);
                            if (outline) change.hudTitleOutlines++; else change.hudTitleShadows++;
                        }
                    }
                    if (text.name.IndexOf("Sombra", StringComparison.OrdinalIgnoreCase) >= 0)
                        text.color = new Color(Ink.r, Ink.g, Ink.b, 0);
                    EditorUtility.SetDirty(text); change.texts++;
                }
                foreach (Selectable selectable in root.GetComponentsInChildren<Selectable>(true))
                {
                    ColorBlock colors = selectable.colors;
                    colors.normalColor = Color.white;
                    colors.highlightedColor = new Color32(218, 237, 227, 255);
                    colors.pressedColor = new Color32(182, 212, 200, 255);
                    colors.selectedColor = Color.white;
                    colors.disabledColor = new Color32(180, 193, 185, 180);
                    colors.colorMultiplier = 1;
                    selectable.colors = colors;
                    EditorUtility.SetDirty(selectable);
                }
                foreach (MonoBehaviour script in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (script == null || script is Image || script is Text) continue;
                    SerializedObject serialized = new SerializedObject(script);
                    SerializedProperty property = serialized.GetIterator();
                    bool dirty = false;
                    while (property.NextVisible(true))
                    {
                        if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                        Sprite old = property.objectReferenceValue as Sprite;
                        string art;
                        if (old == null || !TryArtName(old, out art)) continue;
                        property.objectReferenceValue = Sprites[art]; dirty = true; change.serializedSprites++;
                    }
                    if (dirty) serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            if (scene.name.StartsWith("Levely5", StringComparison.Ordinal) && change.hudTitleOutlines == 0)
                throw new InvalidOperationException("No existing HUD Level Outline was themed: " + scene.path);
            return change;
        }

        static bool TryArtName(Sprite sprite, out string art)
        {
            string path = AssetDatabase.GetAssetPath(sprite);
            if (path.StartsWith(AssetRoot + "Sprites/", StringComparison.Ordinal))
            { art = Path.GetFileNameWithoutExtension(path); return Sprites.ContainsKey(art); }
            return Mapping.TryGetValue(sprite.name, out art);
        }

        // Scene component fileIDs survive SaveScene. Always remap from the
        // immutable pre-theme Sprite reference, including after a previous pass.
        static Dictionary<ulong, Sprite> OriginalSceneSprites(string scenePath)
        {
            var originals = new Dictionary<ulong, Sprite>();
            string project = Directory.GetParent(Application.dataPath).FullName;
            string backup = Path.Combine(project, "Validation", "ArtUpgrade", "Before", "UI", scenePath);
            if (!File.Exists(backup)) return originals;
            string yaml = File.ReadAllText(backup);
            foreach (Match component in Regex.Matches(yaml, @"--- !u!114 &(\d+)\r?\n(.*?)(?=--- !u!|\z)", RegexOptions.Singleline))
            {
                Match reference = Regex.Match(component.Groups[2].Value, @"m_Sprite: \{fileID: \d+, guid: ([a-fA-F0-9]+)");
                if (!reference.Success) continue;
                string path = AssetDatabase.GUIDToAssetPath(reference.Groups[1].Value);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null) originals[UInt64.Parse(component.Groups[1].Value)] = sprite;
            }
            return originals;
        }

        static string HierarchyPath(Transform item)
        { return item.parent == null ? item.name : HierarchyPath(item.parent) + "/" + item.name; }

        static string InteractionInvariant(Scene scene)
        {
            var result = new StringBuilder();
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    GameObject go = transform.gameObject;
                    result.Append(HierarchyPath(transform)).Append('|').Append(transform.GetSiblingIndex()).Append('|').Append(go.activeSelf).Append('|').Append(go.layer).Append('|').Append(go.tag);
                    foreach (Component component in go.GetComponents<Component>()) result.Append('|').Append(component == null ? "missing" : component.GetType().FullName);
                    Graphic graphic = go.GetComponent<Graphic>();
                    if (graphic != null) result.Append("|ray=").Append(graphic.raycastTarget);
                    Selectable selectable = go.GetComponent<Selectable>();
                    EventTrigger trigger = go.GetComponent<EventTrigger>();
                    if (selectable != null || trigger != null)
                    {
                        RectTransform rect = transform as RectTransform;
                        if (rect != null) result.Append("|rect=").Append(JsonUtility.ToJson(new RectSnapshot(rect)));
                    }
                    Button button = go.GetComponent<Button>();
                    if (button != null)
                    {
                        result.Append("|interactable=").Append(button.interactable);
                        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                            result.Append("|click=").Append(button.onClick.GetPersistentTarget(i)).Append(':').Append(button.onClick.GetPersistentMethodName(i)).Append(':').Append(button.onClick.GetPersistentListenerState(i));
                    }
                    if (trigger != null) result.Append("|trigger=").Append(EditorJsonUtility.ToJson(trigger));
                    result.AppendLine();
                }
            return result.ToString();
        }

        [Serializable] sealed class RectSnapshot
        {
            public Vector2 min, max, pivot, position, size; public Vector3 scale; public Quaternion rotation; public Vector3[] worldCorners;
            public RectSnapshot(RectTransform rect) { min = rect.anchorMin; max = rect.anchorMax; pivot = rect.pivot; position = rect.anchoredPosition; size = rect.sizeDelta; scale = rect.localScale; rotation = rect.localRotation; worldCorners = new Vector3[4]; rect.GetWorldCorners(worldCorners); }
        }
    }
}
#endif
