using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Uses the authored play scenes for their board, perspective camera, lighting,
/// background and UI. The extracted scenes remain the source of presentation.
/// </summary>
public static class OriginalScenePresentation
{
    [Serializable] sealed class PresentationMap
    {
        public PartPresentation[] parts;
        public NightPresentation[] night;
        public float removeTierraIfLocalYLessThan;
    }
    [Serializable] sealed class PartPresentation
    {
        public int parte;
        public string backgroundField, floorMaterialField;
        public bool disableDirectionalLight;
    }
    [Serializable] sealed class NightPresentation
    {
        public int parte;
        public string backgroundField;
        public float backgroundRGB, ambientRGB;
        public NightOverride[] overrides;
    }
    [Serializable] sealed class NightOverride
    {
        public int[] assets;
        public float backgroundRGB, ambientRGB;
    }
    static PresentationMap presentation;
    static int ambientScene = -1;
    static Color sceneAmbient;

    public static bool IsGameplayScene(Scene scene)
    {
        return scene.IsValid() && (scene.name == "Levely5" || scene.name == "Levely5_basic" ||
            scene.name == "Levely5_6x6" || scene.name == "Levely5_7x7");
    }

    public static bool TryPrepareCurrentScene(out Camera camera)
    {
        camera = null;
        Scene scene = SceneManager.GetActiveScene();
        if (!IsGameplayScene(scene)) return false;
        foreach (var root in scene.GetRootGameObjects())
        {
            // These are saved editor/manipulation guides, not the game's ground.
            if (root.name.StartsWith("PlanoGuia", StringComparison.Ordinal) || root.name == "EsferaGuia")
            {
                root.SetActive(false);
                continue;
            }
            var cameras = root.GetComponentsInChildren<Camera>(true);
            if (cameras.Length > 0)
            {
                root.SetActive(true);
                foreach (Camera candidate in cameras)
                    if (camera == null || candidate.CompareTag("MainCamera")) camera = candidate;
            }
            if (root.name.StartsWith("Suelo", StringComparison.Ordinal)) root.SetActive(true);
        }
        if (camera == null) return false;
        camera.gameObject.SetActive(true);
        camera.enabled = true;
        foreach (var root in scene.GetRootGameObjects())
        {
            // isRootCanvas also becomes true for an inactive nested Canvas.
            // Only the authored scene-root Canvas is the gameplay UI root;
            // the nested solution/rating/completion Canvases keep their state.
            if (root.name != "Canvas") continue;
            Canvas canvas = root.GetComponent<Canvas>();
            if (canvas == null) continue;
            root.SetActive(true);
            canvas.enabled = true;
            canvas.transform.localScale = Vector3.one;
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera) canvas.worldCamera = camera;
        }
        return true;
    }

    public static void ApplyThemeForLevel(int assetId)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!IsGameplayScene(scene)) return;
        if (presentation == null)
        {
            TextAsset asset = Resources.Load<TextAsset>("NativePresentation");
            if (asset == null) { Debug.LogError("NativePresentation is missing from Resources."); return; }
            presentation = JsonUtility.FromJson<PresentationMap>(asset.text);
        }
        if (presentation == null || presentation.parts == null) return;
        if (ambientScene != scene.handle)
        {
            ambientScene = scene.handle;
            sceneAmbient = RenderSettings.ambientLight;
        }
        RenderSettings.ambientLight = sceneAmbient;
        int part = PartForLevel(assetId);
        PartPresentation selected = null;
        foreach (PartPresentation item in presentation.parts) if (item.parte == part) { selected = item; break; }
        if (selected == null) return;
        string background = selected.backgroundField;
        bool tintBackground = false;
        float backgroundValue = 1;
        if (presentation.night != null)
            foreach (NightPresentation night in presentation.night)
                if (night.parte == part)
                {
                    background = night.backgroundField;
                    backgroundValue = night.backgroundRGB;
                    float ambient = night.ambientRGB;
                    if (night.overrides != null)
                        foreach (NightOverride item in night.overrides)
                            if (item != null && item.assets != null && item.assets.Length >= 2 &&
                                assetId >= item.assets[0] && assetId <= item.assets[1])
                            { backgroundValue = item.backgroundRGB; ambient = item.ambientRGB; }
                    RenderSettings.ambientLight = new Color(ambient, ambient, ambient, 1);
                    tintBackground = true;
                    break;
                }
        foreach (UIManagerScript ui in Components<UIManagerScript>(scene))
        {
            foreach (FieldInfo field in typeof(UIManagerScript).GetFields(BindingFlags.Public | BindingFlags.Instance))
                if (field.FieldType == typeof(GameObject) && field.Name.StartsWith("fondo", StringComparison.Ordinal))
                {
                    GameObject target = field.GetValue(ui) as GameObject;
                    if (target == null) continue;
                    bool active = field.Name == background;
                    target.SetActive(active);
                    if (active && tintBackground)
                    {
                        Image image = target.GetComponent<Image>();
                        if (image != null) image.color = new Color(backgroundValue, backgroundValue, backgroundValue, 1);
                    }
                }
            if (ui.light != null) ui.light.SetActive(!selected.disableDirectionalLight);
        }
        foreach (ConfigSuelo floor in Components<ConfigSuelo>(scene))
        {
            FieldInfo materialField = typeof(ConfigSuelo).GetField(selected.floorMaterialField);
            Material material = materialField == null ? null : materialField.GetValue(floor) as Material;
            Renderer renderer = floor.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;
            // Original Awake destroyed this sub-object. Keep the extracted
            // hierarchy intact while reproducing the same visible result.
            if (floor.tierra != null && floor.transform.localPosition.y < presentation.removeTierraIfLocalYLessThan)
                floor.tierra.SetActive(false);
        }
    }

    public static string SceneForLevel(int assetId)
    {
        return SceneForPart(PartForLevel(assetId));
    }
    public static int PartForLevel(int assetId)
    {
        if (assetId >= 1 && assetId <= 800) return (assetId - 1) / 50 + 1;
        if (assetId >= 1001 && assetId <= 1050) return 17;
        if (assetId >= 1051 && assetId <= 2050) return (assetId - 1051) / 50 + 20;
        return 0;
    }

    public static int AssetForLevel(int part, int level)
    {
        level = Mathf.Clamp(level, 1, 50);
        if (part >= 1 && part <= 16) return (part - 1) * 50 + level;
        if (part == 17) return 1000 + level;
        if (part >= 20 && part <= 39) return 1050 + (part - 20) * 50 + level;
        return -1;
    }

    // Recovered from UIManagerSelScript.doCallGame in the shipped IL2CPP
    // binary. These are the original pack identifiers, not asset ID groups.
    public static string SceneForPart(int part)
    {
        switch (part)
        {
            case 6: case 7: case 11: case 14: case 15: case 17: case 21:
            case 22: case 25: case 27: case 29: case 30: case 31: case 33:
            case 34: case 37: case 38: return "Levely5_6x6";
            case 9: case 10: case 13: case 20: case 26: case 32: case 36:
                return "Levely5_basic";
            case 8: case 12: case 16: case 23: case 28: case 35: case 39:
                return "Levely5_7x7";
            default: return "Levely5";
        }
    }

    public static IEnumerable<T> Components<T>(Scene scene) where T : Component
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var component in root.GetComponentsInChildren<T>(true))
                yield return component;
    }
}
