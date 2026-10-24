#if UNITY_EDITOR
using System;
using System.Collections;
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
    /// <summary>Renders the authored camera/UI at requested target dimensions without changing FOV.</summary>
    [InitializeOnLoad]
    public static class FlowWaterAspectValidation
    {
        const string Key = "FlowWater.AspectValidation.Active";
        [Serializable] public sealed class ControlBounds
        { public string method, path; public float left, bottom, right, top; public bool fullyInside, intersectsScreen; }
        [Serializable] public sealed class Frame
        {
            public string scene, screenshot; public int level, width, height, pivotCount, pivotsInView;
            public bool supportedOrientation, boardBoundsInView;
            public float cameraFov, cameraAspect, cameraBackwardDistance;
            public List<ControlBounds> controls = new List<ControlBounds>();
        }
        [Serializable] public sealed class Report
        {
            public string unityVersion, startedUtc, finishedUtc;
            public bool passed, preferencesRestored;
            public List<Frame> frames = new List<Frame>();
            public List<string> errors = new List<string>();
            public List<string> observations = new List<string>();
        }
        sealed class CameraState { public Camera camera; public RenderTexture target; public float aspect; public int mask; }
        sealed class CanvasState { public Canvas canvas; public RenderMode mode; public Camera camera; public float distance; }
        static Report report;
        static IEnumerator routine;
        static int resume;
        static bool busy, locked;
        static string Output { get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation", "Aspect"); } }
        static FlowWaterAspectValidation()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode) Unlock();
                if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key, false))
                { RestorePrefs(); SessionState.EraseBool(Key); }
            };
        }
        [MenuItem("Tools/Flow Water/Validate Original Aspect Ratios")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Run the aspect validation from Edit mode.");
            BackupPrefs();
            SessionState.SetBool(Key, true);
            EditorSceneManager.OpenScene("Assets/Scenes/Portada.unity", OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }
        static void Update()
        {
            if (busy || !SessionState.GetBool(Key, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            busy = true;
            try
            {
                if (report == null)
                {
                    AssetDatabase.DisallowAutoRefresh(); EditorApplication.LockReloadAssemblies(); locked = true;
                    report = new Report { unityVersion = Application.unityVersion, startedUtc = DateTime.UtcNow.ToString("o") };
                    report.observations.Add("Original PlayerSettings permits landscape left/right and disables both portrait orientations. Portrait renders are recorded as unsupported observations.");
                    report.observations.Add("Original game controllers contain no Camera.set_fieldOfView calls. Authored FOV and rotation are retained; the reconstructed desktop framing guard moves the camera backward only when needed to contain structural board meshes. Overlay canvases are temporarily captured through a full-screen UI camera.");
                    routine = Exercise(); resume = Time.frameCount + 8;
                }
                if (Time.frameCount < resume) return;
                if (!routine.MoveNext()) { Finish(); return; }
                resume = Time.frameCount + (routine.Current is int ? (int)routine.Current : 1);
            }
            catch (Exception error) { if (report == null) report = new Report(); report.errors.Add(error.ToString()); Finish(); }
            finally { busy = false; }
        }
        static IEnumerator Exercise()
        {
            CaptureThree("home");
            SceneManager.LoadScene("Settings"); yield return 8; CaptureThree("settings");
            SceneManager.LoadScene("Store"); yield return 8; CaptureThree("store");
            SceneManager.LoadScene("SelLevel0"); yield return 8;
            for (int tab = 1; tab <= 6; tab++)
            { FlowWaterRecoveryUI.Instance.ShowCategory(tab); yield return 5; CaptureThree("category" + tab); }
            foreach (int level in new[] { 1, 401, 288, 2001 })
            { FlowWaterRecoveryUI.Instance.StartGame(level); yield return 12; CaptureThree("gm" + level); }
        }
        static void CaptureThree(string name)
        {
            foreach (Vector2Int size in new[] { new Vector2Int(1280,720), new Vector2Int(885,630), new Vector2Int(720,1280) })
                Capture(name, size.x, size.y);
        }
        static void Capture(string name, int width, int height)
        {
            Directory.CreateDirectory(Output);
            var frame = new Frame { scene = SceneManager.GetActiveScene().name, level = OriginalScenePresentation.IsGameplayScene(SceneManager.GetActiveScene()) ? RecoveredFlowGame.Instance.CurrentLevelId : 0,
                width = width, height = height, supportedOrientation = width >= height, screenshot = Path.Combine(Output, name + "-" + width + "x" + height + ".png") };
            var cameraStates = new List<CameraState>(); var canvasStates = new List<CanvasState>();
            var scales = UnityEngine.Object.FindObjectsOfType<Canvas>().Where(x => x.isActiveAndEnabled && x.isRootCanvas).ToDictionary(x => x,x => x.scaleFactor);
            var layers = new Dictionary<GameObject,int>(); RenderTexture previous = RenderTexture.active;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            GameObject uiObject = null; Texture2D pixels = null;
            try
            {
                target.Create();
                Camera[] cameras = UnityEngine.Object.FindObjectsOfType<Camera>().Where(x => x.isActiveAndEnabled && x.cameraType == CameraType.Game).OrderBy(x => x.depth).ToArray();
                foreach (Camera camera in cameras)
                {
                    cameraStates.Add(new CameraState { camera = camera, target = camera.targetTexture, aspect = camera.aspect, mask = camera.cullingMask });
                    camera.targetTexture = target; camera.aspect = width / (float)height;
                }
                var occupied = new HashSet<int>(UnityEngine.Object.FindObjectsOfType<Transform>().Select(x => x.gameObject.layer));
                int layer = Enumerable.Range(8,24).Reverse().First(x => !occupied.Contains(x));
                uiObject = new GameObject("Aspect validation UI camera", typeof(Camera)); uiObject.hideFlags = HideFlags.HideAndDontSave;
                Camera uiCamera = uiObject.GetComponent<Camera>(); uiCamera.enabled = false; uiCamera.targetTexture = target;
                uiCamera.clearFlags = CameraClearFlags.Depth; uiCamera.orthographic = true; uiCamera.orthographicSize = height * .5f;
                uiCamera.nearClipPlane = .1f; uiCamera.farClipPlane = 1000; uiCamera.cullingMask = 1 << layer;
                foreach (Canvas canvas in UnityEngine.Object.FindObjectsOfType<Canvas>())
                {
                    if (!canvas.isActiveAndEnabled || !canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                    canvasStates.Add(new CanvasState { canvas = canvas, mode = canvas.renderMode, camera = canvas.worldCamera, distance = canvas.planeDistance });
                    canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = uiCamera; canvas.planeDistance = 1;
                    foreach (Transform child in canvas.GetComponentsInChildren<Transform>(true))
                    { if (!layers.ContainsKey(child.gameObject)) layers.Add(child.gameObject, child.gameObject.layer); child.gameObject.layer = layer; }
                }
                Canvas.ForceUpdateCanvases();
                // CanvasScaler normally updates once per frame. Each immediate
                // RT capture needs the authored scaler to evaluate the new size.
                MethodInfo handle = typeof(CanvasScaler).GetMethod("Handle",BindingFlags.Instance | BindingFlags.NonPublic);
                foreach (CanvasScaler scaler in UnityEngine.Object.FindObjectsOfType<CanvasScaler>())
                    if (scaler.isActiveAndEnabled && handle != null) handle.Invoke(scaler,null);
                Canvas.ForceUpdateCanvases();
                RenderTexture.active = target; GL.Clear(true,true, cameras.Length > 0 ? cameras[0].backgroundColor : Color.black);
                foreach (Camera camera in cameras) { camera.cullingMask &= ~(1 << layer); camera.Render(); }
                uiCamera.Render();
                foreach (var group in FlowWaterRecoveryUI.Instance.OriginalControls.Where(x => x.source != null && x.source.activeInHierarchy).GroupBy(x => x.source))
                {
                    var control = group.First(); RectTransform rect = control.source.GetComponent<RectTransform>();
                    Canvas canvas = control.source.GetComponentInParent<Canvas>(); if (rect == null || canvas == null) continue;
                    Camera canvasCamera = canvas.rootCanvas.worldCamera;
                    var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                    Vector2[] positions = corners.Select(x => RectTransformUtility.WorldToScreenPoint(canvasCamera,x)).ToArray();
                    var bounds = new ControlBounds { method = control.method, path = Hierarchy(rect), left = positions.Min(x => x.x), right = positions.Max(x => x.x), bottom = positions.Min(x => x.y), top = positions.Max(x => x.y) };
                    bounds.fullyInside = bounds.left >= -1 && bounds.bottom >= -1 && bounds.right <= width+1 && bounds.top <= height+1;
                    bounds.intersectsScreen = bounds.right > 0 && bounds.left < width && bounds.top > 0 && bounds.bottom < height;
                    frame.controls.Add(bounds);
                }
                if (frame.level > 0)
                {
                    RecoveredFlowGame game = RecoveredFlowGame.Instance; Camera camera = game.GameCamera;
                    frame.cameraFov = camera.fieldOfView; frame.cameraAspect = camera.aspect;
                    RecoveredBoardFraming framing=camera.GetComponent<RecoveredBoardFraming>();
                    frame.cameraBackwardDistance=framing==null ? 0 : framing.LastBackwardDistance;
                    foreach (RecoveryPiece piece in game.Pieces)
                    { Vector3 p = camera.WorldToViewportPoint(piece.transform.position); frame.pivotCount++; if (Inside(p)) frame.pivotsInView++; }
                    IEnumerable<Renderer> board=game.PieceRoot.GetComponentsInChildren<MeshRenderer>();
                    foreach(ConfigSuelo floor in OriginalScenePresentation.Components<ConfigSuelo>(SceneManager.GetActiveScene()))
                    {
                        Renderer renderer=floor.GetComponent<Renderer>(); if(renderer!=null) board=board.Concat(new[] { renderer });
                        if(floor.tierra!=null) board=board.Concat(floor.tierra.GetComponentsInChildren<Renderer>(true));
                    }
                    foreach(GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                        if(root.name=="Recovered original underground border") board=board.Concat(root.GetComponentsInChildren<Renderer>(true));
                    Renderer[] renderers=board.Where(RecoveredBoardFraming.IsStructuralRenderer).ToArray();
                    if (renderers.Length > 0)
                    {
                        // Inspect actual mesh boxes. One aggregate AABB adds
                        // empty upper/lower diagonal corners outside the board.
                        frame.boardBoundsInView=renderers.All(renderer =>
                        {
                            MeshFilter mesh=renderer.GetComponent<MeshFilter>();
                            return Corners(mesh.sharedMesh.bounds).All(x => Inside(camera.WorldToViewportPoint(mesh.transform.TransformPoint(x))));
                        });
                    }
                    if (frame.level == 2001)
                        FlowWaterVisualDiagnostics.Write(Path.Combine(Output, name + "-" + width + "x" + height + "-renderers.json"), camera, width, height);
                }
                pixels = new Texture2D(width,height,TextureFormat.RGB24,false); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0,0,width,height),0,0,false); pixels.Apply(false,false); File.WriteAllBytes(frame.screenshot,pixels.EncodeToPNG());
                report.frames.Add(frame);
            }
            finally
            {
                foreach (var item in layers) if (item.Key != null) item.Key.layer = item.Value;
                foreach (var state in canvasStates) if (state.canvas != null) { state.canvas.renderMode = state.mode; state.canvas.worldCamera = state.camera; state.canvas.planeDistance = state.distance; }
                foreach (var state in cameraStates) if (state.camera != null) { state.camera.targetTexture = state.target; state.camera.aspect = state.aspect; state.camera.cullingMask = state.mask; }
                foreach (var state in scales) if (state.Key != null) state.Key.scaleFactor=state.Value;
                RenderTexture.active = previous;
                if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels); if (uiObject != null) UnityEngine.Object.DestroyImmediate(uiObject);
                target.Release(); UnityEngine.Object.DestroyImmediate(target); Canvas.ForceUpdateCanvases();
            }
        }
        static string Hierarchy(Transform transform)
        { string path = transform.name; while (transform.parent != null) { transform = transform.parent; path = transform.name + "/" + path; } return path; }
        static bool Inside(Vector3 p) { return p.z > 0 && p.x >= 0 && p.x <= 1 && p.y >= 0 && p.y <= 1; }
        static IEnumerable<Vector3> Corners(Bounds b)
        { for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2) yield return b.center + Vector3.Scale(b.extents,new Vector3(x,y,z)); }
        static bool RestorePrefs()
        {
            string json = SessionState.GetString(Key + ".Prefs", ""); if (String.IsNullOrEmpty(json)) return false;
            var backup = JsonUtility.FromJson<FlowWaterUIValidation.Preferences>(json);
            bool exact = true;
            foreach (var item in backup.entries)
            {
                if (!item.existed) PlayerPrefs.DeleteKey(item.key);
                else if (item.floating) PlayerPrefs.SetFloat(item.key,item.number);
                else PlayerPrefs.SetInt(item.key,item.integer);
                exact &= PlayerPrefs.HasKey(item.key) == item.existed && (!item.existed || (item.floating ? PlayerPrefs.GetFloat(item.key) == item.number : PlayerPrefs.GetInt(item.key) == item.integer));
            }
            PlayerPrefs.Save(); return exact;
        }
        static void BackupPrefs()
        {
            var backup = new FlowWaterUIValidation.Preferences();
            Action<string,bool> add = (key,floating) => backup.entries.Add(new FlowWaterUIValidation.Preference { key=key, floating=floating,
                existed=PlayerPrefs.HasKey(key), integer=PlayerPrefs.GetInt(key), number=PlayerPrefs.GetFloat(key) });
            foreach (string key in new[] { "Recovery.LastLevelId", "esTablet", "Soundlevel", "MusicaON", "primeraVez", "OptionSelLev" }) add(key,false);
            add("Conf_music",true); add("Conf_sound",true);
            foreach (RecoveryLevelEntry entry in RecoveryCatalog.Load().levels)
            { add("Recovery.Complete."+entry.assetId,false); add("Recovery.Completed.gm"+entry.assetId,false); }
            SessionState.SetString(Key+".Prefs",JsonUtility.ToJson(backup));
        }
        static void Unlock()
        { if (locked) { locked = false; EditorApplication.UnlockReloadAssemblies(); AssetDatabase.AllowAutoRefresh(); } }
        static void Finish()
        {
            try
            {
                report.finishedUtc = DateTime.UtcNow.ToString("o"); report.preferencesRestored = RestorePrefs();
                foreach (Frame frame in report.frames.Where(x => x.supportedOrientation))
                {
                    foreach (ControlBounds bounds in frame.controls.Where(x => !x.fullyInside)) report.observations.Add(Path.GetFileName(frame.screenshot) + " control crosses screen: " + bounds.path);
                    if (frame.level > 0 && frame.pivotsInView != frame.pivotCount) report.observations.Add(Path.GetFileName(frame.screenshot) + " visible piece pivots=" + frame.pivotsInView + "/" + frame.pivotCount);
                    if (frame.level > 0 && !frame.boardBoundsInView) report.observations.Add(Path.GetFileName(frame.screenshot) + " rendered board bounds cross the camera viewport.");
                }
                report.passed = report.preferencesRestored && report.errors.Count == 0 && report.frames.Count == 39 && report.frames.Where(x => x.supportedOrientation).All(x => x.controls.All(c => c.fullyInside) && (x.level == 0 || x.pivotsInView == x.pivotCount && x.boardBoundsInView));
                Directory.CreateDirectory(Output); File.WriteAllText(Path.Combine(Output,"aspect-validation.json"),JsonUtility.ToJson(report,true));
                Debug.Log("Original aspect validation: " + (report.passed ? "PASS" : "REVIEW") + "; frames=" + report.frames.Count);
            }
            finally { SessionState.EraseBool(Key); Unlock(); routine = null; }
            bool passed = report.passed; report = null;
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1); else EditorApplication.isPlaying = false;
        }
    }
}
#endif
