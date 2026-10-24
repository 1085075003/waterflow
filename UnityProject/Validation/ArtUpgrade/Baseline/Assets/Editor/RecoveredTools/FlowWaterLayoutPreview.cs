// New recovery utility, written for this export. This is not original game code.
// Does not execute at import/startup. Use Tools > Flow Water > Layout Preview.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Recovered.FlowWater.Editor
{
    public sealed class FlowWaterLayoutPreview : EditorWindow
    {
        const string Root = "Assets/RecoveredData/LevelData";
        const string AliasPath = "Assets/RecoveredData/preview_aliases.json";
        int assetId = 1;
        bool finalLayout;
        bool originalMainScene292;
        string message = "Generate an independent geometry preview; original gameplay methods are stubs.";
        TextAsset selectedJson;

        [Serializable] sealed class Envelope { public string source_asset; public Layout data; }
        [Serializable] sealed class Layout
        {
            public string __type__;
            public int level;
            public List<string> pieza;
            public List<float> positionx, positiony, positionz;
            public List<string> fijo;
        }
        [Serializable] sealed class AliasFile { public List<Alias> aliases; }
        [Serializable] sealed class Alias
        {
            public string originalName, basePrefab, mappingConfidence;
            public int colorSuffix;
            public bool scene292exception;
        }

        [MenuItem("Tools/Flow Water/Layout Preview")]
        public static void ShowWindow()
        {
            GetWindow<FlowWaterLayoutPreview>("Flow Water Recovery");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Recovered level geometry", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This new Editor utility places recovered prefabs at original saved coordinates. " +
                "It does not restore water simulation, interaction, runtime color variants or game progression.", MessageType.Info);
            EditorGUILayout.HelpBox("Use an empty scene or close other scene tabs first: this preview opens additively, so other loaded scenes can overlap it.", MessageType.None);
            assetId = EditorGUILayout.IntField("Asset ID (gm / so suffix)", assetId);
            EditorGUILayout.HelpBox("IDs are not continuous. Select by gmNNN / soNNN; saved data.level is preserved and may differ.", MessageType.None);
            finalLayout = EditorGUILayout.Toggle("Use final-layout solution", finalLayout);
            originalMainScene292 = EditorGUILayout.Toggle("Use main-scene index 292", originalMainScene292);
            EditorGUILayout.HelpBox("Index 292 differs in the original scenes: main = BloqueChorro2; 6x6/7x7/basic = Curva4y. " +
                "The default preview uses Curva4y. ColN variants use base geometry with original prefab materials.", MessageType.None);
            if (GUILayout.Button("Create selected asset preview in new additive scene"))
            {
                string kind = finalLayout ? "so" : "gm";
                string folder = finalLayout ? "02_Solutions" : "01_Layouts";
                CreatePreview(Root + "/" + folder + "/" + kind + assetId + ".json");
            }
            EditorGUILayout.Space();
            selectedJson = (TextAsset)EditorGUILayout.ObjectField("Or decoded JSON", selectedJson, typeof(TextAsset), false);
            using (new EditorGUI.DisabledScope(selectedJson == null))
            {
                if (GUILayout.Button("Preview selected JSON"))
                    CreatePreview(AssetDatabase.GetAssetPath(selectedJson));
            }
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(message, MessageType.Info);
        }

        void CreatePreview(string path)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                message = "Exit Play mode before creating a preview.";
                return;
            }
            Scene scene = default;
            try
            {
                if (!File.Exists(path)) throw new FileNotFoundException("No decoded asset at " + path);
                Envelope envelope = JsonUtility.FromJson<Envelope>(File.ReadAllText(path));
                Layout data = envelope == null ? null : envelope.data;
                if (data == null) throw new InvalidDataException("The JSON has no data object.");
                if (data.__type__ != "Save")
                    throw new InvalidDataException("This is " + data.__type__ + "; Save1 stores move steps, not a final piece layout. Use its matching gm asset.");
                int count = data.pieza == null ? 0 : data.pieza.Count;
                if (count == 0 || data.positionx == null || data.positiony == null || data.positionz == null ||
                    data.positionx.Count != count || data.positiony.Count != count || data.positionz.Count != count ||
                    data.fijo == null || data.fijo.Count != count)
                    throw new InvalidDataException("Piece, position and fijo list lengths do not match.");
                AliasFile aliasFile = JsonUtility.FromJson<AliasFile>(File.ReadAllText(AliasPath));
                var aliases = aliasFile.aliases.ToDictionary(a => a.originalName, StringComparer.Ordinal);
                var prefabs = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/GameObject" }))
                {
                    string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                    string key = Path.GetFileNameWithoutExtension(prefabPath);
                    if (!prefabs.ContainsKey(key)) prefabs[key] = new List<string>();
                    prefabs[key].Add(prefabPath);
                }
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(scene);
                GameObject root = new GameObject("RECOVERED PREVIEW - " + envelope.source_asset + " (saved level " + data.level + ")");
                SceneManager.MoveGameObjectToScene(root, scene);
                int missing = 0, variants = 0, inferred = 0;
                var missingNames = new List<string>();
                Bounds bounds = new Bounds();
                bool hasBounds = false;
                for (int i = 0; i < count; i++)
                {
                    EditorUtility.DisplayProgressBar("Recovered geometry", data.pieza[i], (float)i / count);
                    Alias alias;
                    aliases.TryGetValue(data.pieza[i], out alias);
                    string prefabName = alias == null ? data.pieza[i].Replace("(Clone)", "").Trim() : alias.basePrefab;
                    if (alias != null && alias.scene292exception && originalMainScene292) prefabName = "BloqueChorro2";
                    List<string> matches;
                    GameObject instance = null;
                    if (prefabs.TryGetValue(prefabName, out matches) && matches.Count == 1)
                    {
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(matches[0]);
                        instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    }
                    if (instance == null)
                    {
                        instance = new GameObject("UNRESOLVED - " + data.pieza[i]);
                        SceneManager.MoveGameObjectToScene(instance, scene);
                        missing++;
                        missingNames.Add(data.pieza[i] + " -> " + prefabName);
                    }
                    else instance.name = "[" + i.ToString("D4") + "] " + data.pieza[i];
                    instance.transform.SetParent(root.transform, false);
                    instance.transform.localPosition = new Vector3(data.positionx[i], data.positiony[i], data.positionz[i]);
                    // Keep each recovered prefab's authored rotation and scale. Saved JSON has only positions.
                    foreach (MonoBehaviour behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
                        if (behaviour != null) behaviour.enabled = false;
                    foreach (Animator animator in instance.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                    foreach (Rigidbody body in instance.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
                    foreach (ParticleSystem particles in instance.GetComponentsInChildren<ParticleSystem>(true))
                        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(false))
                    {
                        if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                        else bounds.Encapsulate(renderer.bounds);
                    }
                    if (alias != null && alias.colorSuffix > 0) variants++;
                    if (alias != null && alias.mappingConfidence != null && alias.mappingConfidence.StartsWith("inferred", StringComparison.Ordinal)) inferred++;
                }
                if (!hasBounds) bounds = new Bounds(root.transform.position, Vector3.one * 100);
                GameObject cameraObject = new GameObject("Recovery Preview Camera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                float radius = Mathf.Max(20, bounds.extents.magnitude);
                cameraObject.transform.position = bounds.center + new Vector3(1, 1.1f, -1.2f).normalized * radius * 2.3f;
                cameraObject.transform.LookAt(bounds.center);
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.nearClipPlane = Mathf.Max(0.1f, radius / 1000);
                camera.farClipPlane = radius * 20;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.12f, 0.15f, 0.18f);
                GameObject lightObject = new GameObject("Recovery Preview Light", typeof(Light));
                SceneManager.MoveGameObjectToScene(lightObject, scene);
                lightObject.transform.rotation = Quaternion.Euler(50, -30, 0);
                Light light = lightObject.GetComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.1f;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.65f, 0.65f, 0.65f);
                Selection.activeGameObject = root;
                if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.Frame(bounds, false);
                EditorSceneManager.MarkSceneDirty(scene);
                message = envelope.source_asset + ": " + count + " pieces, " + missing + " unresolved, " + variants +
                    " color variants using base materials; " + inferred + " inferred mappings. This scene is unsaved.";
                Debug.Log(message + "\nSource JSON: " + path + "\nAll original fijo strings remain in that JSON; they are not interpreted as booleans." +
                    (missingNames.Count == 0 ? "" : "\nUnresolved: " + string.Join("; ", missingNames)));
            }
            catch (Exception exception)
            {
                message = exception.Message;
                Debug.LogError("Recovery preview: " + exception);
                // An incomplete additive scene stays available for inspection; no existing scene is overwritten.
            }
            finally { EditorUtility.ClearProgressBar(); }
        }
    }
}
#endif
