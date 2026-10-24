#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Recovered.FlowWater.Editor
{
    // Editor-only portrait QA. Capture uses the REAL Game View size: changing a
    // RenderTexture alone does not reflow native CanvasScaler or Screen APIs.
    [InitializeOnLoad]
    public static class FlowWaterPortraitValidation
    {
        const string Key = "FlowWater.PortraitValidation.Active";
        static readonly string[] Scenes = {
            "Assets/Scenes/Portada.unity", "Assets/Scenes/SelLevel0.unity", "Assets/Scenes/SelLevel1.unity",
            "Assets/Settings.unity", "Assets/Store.unity", "Assets/Scenes/Levely5_basic.unity",
            "Assets/Scenes/Levely5z/Levely5.unity", "Assets/Scenes/Levely5_6x6.unity", "Assets/Scenes/Levely5_7x7.unity"
        };
        static readonly string[] Menus = { "home", "settings", "store", "categories", "levels", "dialog" };
        static readonly int[] Levels = { 1, 407, 518, 626, 1058, 1095, 2001 };
        static string Root { get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation/Portrait"); } }
        static string RunDirectory { get { return Path.Combine(Root, "Runtime-" + (report == null ? Screen.width : report.width) + "x" + (report == null ? Screen.height : report.height)); } }
        [Serializable] sealed class StaticControl { public string path, type; public bool activeSelf; public Vector2 anchoredPosition, sizeDelta, anchorMin, anchorMax; public string[] events; }
        [Serializable] sealed class StaticAnimator { public string path, controller; public bool enabled; public List<string> layoutBindings = new List<string>(); }
        [Serializable] sealed class StaticCanvas { public string path, mode, scaleMode; public Vector2 referenceResolution; public float match; }
        [Serializable] sealed class SceneAudit { public string path; public List<StaticControl> controls = new List<StaticControl>(); public List<StaticAnimator> animators = new List<StaticAnimator>(); public List<StaticCanvas> canvases = new List<StaticCanvas>(); }
        [Serializable] sealed class StaticReport { public string utc, unity, scope; public List<SceneAudit> scenes = new List<SceneAudit>(); }
        [Serializable] sealed class ControlResult { public string path, type, hit, inputHandler, expectedAction, actualAction, skippedReason; public Rect screenRect; public Vector2 referenceSize; public bool centerReachable, inSafeArea, minimumTarget, masked, decorativeAlias; }
        [Serializable] sealed class MeshResult { public string path; public int vertices, outside, beforeNear, beyondFar; public Rect viewportBounds; }
        [Serializable] sealed class FrameResult {
            public string name, scene, screenshot; public int width, height, level, moves; public float aspect; public Rect safeArea, playfieldViewport;
            public bool isSolution, portrait, passed; public List<ControlResult> controls = new List<ControlResult>(); public List<MeshResult> meshes = new List<MeshResult>();
            public List<string> issues = new List<string>(), warnings = new List<string>(); public int verticesChecked, controlsClippedByPaging, controlsBehindModal;
        }
        [Serializable] sealed class RuntimeReport { public string utc, finishedUtc, unity, scope; public int width, height; public bool passed; public List<FrameResult> frames = new List<FrameResult>(); public List<string> errors = new List<string>(); }
        static RuntimeReport report;
        static int index, phase;
        static double next, deadline;
        static bool busy;
        static int candidateWidth, candidateHeight, stableFrames;
        static double stableSince;

        static FlowWaterPortraitValidation()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += PlayModeChanged;
            Application.logMessageReceived += Log;
        }

        [MenuItem("Tools/Flow Water/Portrait/Export Scene Layout Audit")]
        public static void Export()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Export requires Edit mode.");
            for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save scene changes before the read-only scene audit.");
            Directory.CreateDirectory(Root);
            var result = new StaticReport { utc = DateTime.UtcNow.ToString("o"), unity = Application.unityVersion,
                scope = "Read-only authored layout inventory of all five menu and four gameplay scenes. Inactive dialogs and offscreen paging children are inventoried, not asserted visible. Animator position/scale/size curves are listed because they can overwrite edited RectTransforms." };
            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string path in Scenes)
                {
                    Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var item = new SceneAudit { path = path };
                    foreach (GameObject root in scene.GetRootGameObjects())
                    {
                        foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
                        {
                            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                            item.canvases.Add(new StaticCanvas { path = PathOf(canvas.transform), mode = canvas.renderMode.ToString(), scaleMode = scaler == null ? "none" : scaler.uiScaleMode.ToString(), referenceResolution = scaler == null ? Vector2.zero : scaler.referenceResolution, match = scaler == null ? 0 : scaler.matchWidthOrHeight });
                        }
                        foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
                        {
                            var button = rect.GetComponent<Button>(); var trigger = rect.GetComponent<EventTrigger>(); var slider = rect.GetComponent<Slider>();
                            if (button == null && trigger == null && slider == null) continue;
                            item.controls.Add(new StaticControl { path = PathOf(rect), type = button != null ? "Button" : slider != null ? "Slider" : "EventTrigger", activeSelf = rect.gameObject.activeSelf,
                                anchoredPosition = rect.anchoredPosition, sizeDelta = rect.sizeDelta, anchorMin = rect.anchorMin, anchorMax = rect.anchorMax,
                                events = button == null ? new string[0] : Enumerable.Range(0, button.onClick.GetPersistentEventCount()).Select(n => button.onClick.GetPersistentMethodName(n)).ToArray() });
                        }
                        foreach (Animator animator in root.GetComponentsInChildren<Animator>(true))
                        {
                            var animation = new StaticAnimator { path = PathOf(animator.transform), enabled = animator.enabled, controller = animator.runtimeAnimatorController == null ? "" : AssetDatabase.GetAssetPath(animator.runtimeAnimatorController) };
                            if (animator.runtimeAnimatorController != null) foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips.Distinct())
                                foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
                                    if (binding.propertyName.Contains("Position") || binding.propertyName.Contains("Scale") || binding.propertyName.Contains("SizeDelta") || binding.propertyName.Contains("Anchor"))
                                        animation.layoutBindings.Add(clip.name + ": " + binding.path + " " + binding.propertyName);
                            item.animators.Add(animation);
                        }
                    }
                    result.scenes.Add(item);
                }
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
            File.WriteAllText(Path.Combine(Root, "scene-layout-audit.json"), JsonUtility.ToJson(result, true));
        }

        [MenuItem("Tools/Flow Water/Portrait/Capture Current Game View Suite")]
        public static void Capture()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start the suite in Edit mode after selecting the portrait Game View size.");
            for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save scene changes before portrait QA.");
            ResetState();
            SessionState.SetBool(Key, true);
            EditorSceneManager.OpenScene("Assets/Scenes/Portada.unity");
            EditorApplication.EnterPlaymode();
        }

        // Useful for a final live screenshot without navigating or changing play state.
        [MenuItem("Tools/Flow Water/Portrait/Audit Current Live Frame")]
        public static void Audit()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("A live Play mode frame is required.");
            Directory.CreateDirectory(RunDirectory);
            FrameResult frame = Inspect("current", false);
            File.WriteAllText(Path.Combine(RunDirectory, "current.json"), JsonUtility.ToJson(frame, true));
        }

        static void Update()
        {
            if (busy || !SessionState.GetBool(Key, false) || EditorApplication.isCompiling || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup < next) return;
            var ui = FlowWaterRecoveryUI.Instance; if (ui == null) return;
            busy = true;
            try
            {
                if (report == null)
                {
                    // EnterPlaymode can expose the previous docked Game View
                    // dimensions for its first updates. Bind only after the real
                    // portrait surface has stayed unchanged for at least .5 s.
                    if (!StablePortraitSize()) return;
                    index = phase = 0;
                    report = new RuntimeReport { utc = DateTime.UtcNow.ToString("o"), unity = Application.unityVersion, width = Screen.width, height = Screen.height,
                        scope = "Actual current Game View, not a landscape UI rendered into a portrait texture. Six menu/modal states and seven native levels across all four gameplay scenes in initial/saved-solution states. Tests active unmasked control rectangles, 44-unit targets at a 720-wide reference, EventSystem center reachability, and every visible structural mesh vertex inside the HUD-free playfield. Native gameplay outcomes are covered separately by FullValidation." };
                    Directory.CreateDirectory(RunDirectory);
                }
                if (Screen.width != report.width || Screen.height != report.height) throw new InvalidOperationException("Game View size changed during the suite; rerun at one fixed resolution.");
                if (index >= Menus.Length + Levels.Length * 2) { Finish(); return; }
                bool gameplay = index >= Menus.Length;
                int levelIndex = gameplay ? (index - Menus.Length) / 2 : 0;
                bool solution = gameplay && (index - Menus.Length) % 2 != 0;
                string label = gameplay ? "gm" + Levels[levelIndex] + (solution ? "-solution" : "-initial") : Menus[index];
                if (phase == 0)
                {
                    if (gameplay)
                    {
                        if (!solution) ui.StartGame(Levels[levelIndex]);
                        else if (RecoveredFlowGame.Instance != null) RecoveredFlowGame.Instance.Solve();
                    }
                    else switch (label)
                    {
                        case "home": ui.GoHome(); break;
                        case "settings": Invoke(ui, "OpenSettings"); break;
                        case "store": Invoke(ui, "OpenStore"); break;
                        case "categories": SceneManager.LoadScene("SelLevel0"); break;
                        case "levels": ui.OpenPack(1); break;
                        case "dialog": ui.Notice("A quiet garden, one stream at a time."); break;
                    }
                    phase = 1; next = EditorApplication.timeSinceStartup + 3; deadline = EditorApplication.timeSinceStartup + 90; return;
                }
                if (gameplay)
                {
                    var game = RecoveredFlowGame.Instance;
                    if (game == null || game.CurrentLevelId != Levels[levelIndex] || !game.IsLevelActive) { if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException(label + " did not load."); return; }
                    var water = RecoveredWaterNetwork.Instance;
                    if (water != null && water.HasPendingPropagation && EditorApplication.timeSinceStartup < deadline) { next = EditorApplication.timeSinceStartup + .1; return; }
                    FrameResult frame = null;
                    // Completion is intentionally hidden for the board/normal HUD check.
                    // It remains available for independent modal review.
                    ui.CaptureBoardFrame(() => frame = Inspect(label, solution));
                    report.frames.Add(frame);
                }
                else report.frames.Add(Inspect(label, false));
                Save(); index++; phase = 0; next = EditorApplication.timeSinceStartup + .2;
            }
            catch (Exception exception) { if (report != null) report.errors.Add(exception.ToString()); else Debug.LogException(exception); Finish(); }
            finally { busy = false; }
        }

        static FrameResult Inspect(string name, bool solution)
        {
            Canvas.ForceUpdateCanvases();
            var game = RecoveredFlowGame.Instance;
            var result = new FrameResult { name = name, scene = SceneManager.GetActiveScene().name, width = Screen.width, height = Screen.height, aspect = Screen.width / (float)Mathf.Max(1, Screen.height), safeArea = Screen.safeArea,
                level = game == null ? 0 : game.CurrentLevelId, moves = game == null ? 0 : game.MoveCount, isSolution = solution, portrait = Screen.width < Screen.height,
                screenshot = Path.Combine(RunDirectory, name + ".png") };
            if (!result.portrait) result.issues.Add("Actual Game View is not portrait.");
            if (result.safeArea.width <= 0 || result.safeArea.height <= 0) result.safeArea = new Rect(0, 0, Screen.width, Screen.height);
            var objects = new HashSet<GameObject>();
            var modalRoots = new List<GameObject>();
            var ui = FlowWaterRecoveryUI.Instance;
            if (ui != null)
            {
                var method = ui.GetType().GetMethod("SceneDialogObjects", BindingFlags.Instance | BindingFlags.NonPublic);
                var dialogs = method == null ? null : method.Invoke(ui, null) as IEnumerable<GameObject>;
                if (dialogs != null) modalRoots.AddRange(dialogs.Where(obj => obj != null && obj.activeInHierarchy));
            }
            foreach (Selectable selectable in UnityEngine.Object.FindObjectsOfType<Selectable>()) if (selectable.isActiveAndEnabled && selectable.IsInteractable()) objects.Add(selectable.gameObject);
            foreach (EventTrigger trigger in UnityEngine.Object.FindObjectsOfType<EventTrigger>())
                if (trigger.isActiveAndEnabled && trigger.triggers != null && trigger.triggers.Any(entry => entry.eventID == EventTriggerType.PointerClick || entry.eventID == EventTriggerType.PointerDown || entry.eventID == EventTriggerType.BeginDrag)) objects.Add(trigger.gameObject);
            foreach (GameObject obj in objects)
            {
                RectTransform rect = obj.GetComponent<RectTransform>(); Canvas canvas = obj.GetComponentInParent<Canvas>();
                if (rect == null || canvas == null || canvas.renderMode == RenderMode.WorldSpace || !Visible(obj.transform)) continue;
                // Background controls are intentionally blocked by a modal. Test
                // its own buttons, rather than falsely reporting the whole page.
                if (modalRoots.Count > 0 && !modalRoots.Any(modal => obj == modal || obj.transform.IsChildOf(modal.transform))) { result.controlsBehindModal++; continue; }
                Rect screen = ScreenRect(rect, canvas);
                bool masked = obj.GetComponentsInParent<RectMask2D>().Any(mask => mask.isActiveAndEnabled) || obj.GetComponentsInParent<Mask>().Any(mask => mask.isActiveAndEnabled) || obj.GetComponentInParent<ScrollRect>() != null;
                Rect viewport = new Rect(0, 0, Screen.width, Screen.height);
                if (masked && !viewport.Overlaps(screen)) { result.controlsClippedByPaging++; continue; }
                if (screen.width < .5f || screen.height < .5f) continue;
                var control = new ControlResult { path = PathOf(rect), type = obj.GetComponent<Selectable>() == null ? "EventTrigger" : obj.GetComponent<Selectable>().GetType().Name,
                    screenRect = screen, referenceSize = new Vector2(screen.width, screen.height) * (720f / Mathf.Max(1, Screen.width)), inSafeArea = Contains(result.safeArea, screen, 1), masked = masked };
                control.minimumTarget = control.referenceSize.x >= 43 && control.referenceSize.y >= 43;
                Button visualOwner;
                if (DecorativeAlias(obj, canvas, screen, out visualOwner))
                {
                    control.decorativeAlias = true;
                    control.skippedReason = "Nested duplicate Button has no active raycast-target Graphic on itself or descendants; its matching ancestor Button retains the interactive surface.";
                    control.inputHandler = PathOf(visualOwner.transform);
                    control.expectedAction = ControlActions(obj, EventTriggerType.PointerClick);
                    control.actualAction = ControlActions(visualOwner.gameObject, EventTriggerType.PointerClick);
                    result.controls.Add(control);
                    continue;
                }
                if (!control.inSafeArea && !masked) result.issues.Add("Control outside safe area: " + control.path);
                if (!control.minimumTarget) result.issues.Add("Control target below 44 reference units: " + control.path + " " + control.referenceSize);
                if (EventSystem.current != null && viewport.Contains(screen.center))
                {
                    var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screen.center }, hits);
                    GameObject hit = hits.Count == 0 ? null : hits[0].gameObject;
                    control.hit = hit == null ? "none" : PathOf(hit.transform);
                    control.centerReachable = ReachesControl(obj, hit, out control.inputHandler, out control.expectedAction, out control.actualAction);
                    if (!control.centerReachable && !masked) result.issues.Add("Control center blocked: " + control.path + " by " + control.hit);
                }
                result.controls.Add(control);
            }
            if (name == "categories") for (int category = 1; category <= 6; category++)
            {
                string suffix = "/PanelTab1/part" + category + "/boton" + category;
                string action = "botonTabDown(" + category + ")";
                var parents = result.controls.Where(control => !control.decorativeAlias && control.path.EndsWith(suffix, StringComparison.Ordinal)).ToArray();
                if (parents.Length == 0) result.issues.Add("Missing interactive category parent: " + suffix);
                foreach (ControlResult parent in parents)
                    if (!parent.centerReachable || parent.expectedAction != action || parent.actualAction != action)
                        result.issues.Add("Category parent must route the expected action " + action + ": " + parent.path + " actual=" + parent.actualAction);
            }
            for (int a = 0; a < result.controls.Count; a++) for (int b = a + 1; b < result.controls.Count; b++)
            {
                var first = result.controls[a]; var second = result.controls[b];
                if (first.decorativeAlias || second.decorativeAlias || first.masked || second.masked || first.path.StartsWith(second.path + "/", StringComparison.Ordinal) || second.path.StartsWith(first.path + "/", StringComparison.Ordinal)) continue;
                float overlapX = Mathf.Min(first.screenRect.xMax, second.screenRect.xMax) - Mathf.Max(first.screenRect.xMin, second.screenRect.xMin);
                float overlapY = Mathf.Min(first.screenRect.yMax, second.screenRect.yMax) - Mathf.Max(first.screenRect.yMin, second.screenRect.yMin);
                if (overlapX > 1 && overlapY > 1) result.warnings.Add("Interaction rectangles overlap; inspect decorative/slider intent: " + first.path + " / " + second.path);
            }
            if (game != null && game.IsLevelActive && game.GameCamera != null)
            {
                Camera camera = game.GameCamera; var framing = camera.GetComponent<RecoveredBoardFraming>();
                if (framing != null) framing.FitNow();
                PropertyInfo viewportProperty = framing == null ? null : framing.GetType().GetProperty("PlayfieldViewport");
                result.playfieldViewport = viewportProperty == null ? new Rect(0, 0, 1, 1) : (Rect)viewportProperty.GetValue(framing, null);
                IEnumerable<Renderer> geometry = game.PieceRoot.GetComponentsInChildren<MeshRenderer>();
                foreach (ConfigSuelo floor in OriginalScenePresentation.Components<ConfigSuelo>(SceneManager.GetActiveScene()))
                {
                    var renderer = floor.GetComponent<Renderer>(); if (renderer != null) geometry = geometry.Concat(new[] { renderer });
                    if (floor.tierra != null) geometry = geometry.Concat(floor.tierra.GetComponentsInChildren<Renderer>(true));
                }
                foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects()) if (root.name == "Recovered original underground border") geometry = geometry.Concat(root.GetComponentsInChildren<Renderer>(true));
                foreach (Renderer renderer in geometry.Where(RecoveredBoardFraming.IsStructuralRenderer).Distinct())
                {
                    MeshFilter filter = renderer.GetComponent<MeshFilter>(); var mesh = new MeshResult { path = PathOf(renderer.transform) };
                    Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
                    foreach (Vector3 vertex in filter.sharedMesh.vertices)
                    {
                        Vector3 p = camera.WorldToViewportPoint(filter.transform.TransformPoint(vertex)); mesh.vertices++; result.verticesChecked++;
                        min = Vector2.Min(min, p); max = Vector2.Max(max, p);
                        if (p.z < camera.nearClipPlane) mesh.beforeNear++; if (p.z > camera.farClipPlane) mesh.beyondFar++;
                        Rect allowed = result.playfieldViewport;
                        if (p.x < allowed.xMin - .0002f || p.x > allowed.xMax + .0002f || p.y < allowed.yMin - .0002f || p.y > allowed.yMax + .0002f) mesh.outside++;
                    }
                    mesh.viewportBounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                    if (mesh.outside + mesh.beforeNear + mesh.beyondFar > 0) result.issues.Add("Structural mesh crosses HUD-free viewport: " + mesh.path + " outside=" + mesh.outside + " near=" + mesh.beforeNear + " far=" + mesh.beyondFar);
                    result.meshes.Add(mesh);
                }
                Rect playfield = Rect.MinMaxRect(result.playfieldViewport.xMin * Screen.width, result.playfieldViewport.yMin * Screen.height, result.playfieldViewport.xMax * Screen.width, result.playfieldViewport.yMax * Screen.height);
                foreach (ControlResult control in result.controls) if (!control.decorativeAlias && control.screenRect.Overlaps(playfield)) result.warnings.Add("Control intersects reserved playfield; inspect actual board clearance: " + control.path);
            }
            foreach (Text label in UnityEngine.Object.FindObjectsOfType<Text>()) if (label.isActiveAndEnabled && Visible(label.transform) && !string.IsNullOrWhiteSpace(label.text) && label.font == null) result.issues.Add("Missing text font: " + PathOf(label.transform));
            FlowWaterValidation.SaveVisibleFrame(result.screenshot, Screen.width, Screen.height);
            result.passed = result.issues.Count == 0;
            return result;
        }

        static Rect ScreenRect(RectTransform rect, Canvas canvas)
        {
            Vector3[] corners = new Vector3[4]; rect.GetWorldCorners(corners); Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
            foreach (Vector3 corner in corners) { Vector2 p = RectTransformUtility.WorldToScreenPoint(camera, corner); min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
        static bool Visible(Transform transform)
        {
            float alpha = 1;
            for (Transform node = transform; node != null; node = node.parent)
            {
                if (!node.gameObject.activeInHierarchy) return false;
                CanvasGroup group = node.GetComponent<CanvasGroup>(); if (group != null) { alpha *= group.alpha; if (group.ignoreParentGroups) break; }
            }
            return alpha > .01f;
        }
        static bool Contains(Rect outer, Rect inner, float tolerance) { return inner.xMin >= outer.xMin - tolerance && inner.xMax <= outer.xMax + tolerance && inner.yMin >= outer.yMin - tolerance && inner.yMax <= outer.yMax + tolerance; }
        static bool DecorativeAlias(GameObject source, Canvas canvas, Rect screen, out Button owner)
        {
            owner = null;
            if (source.GetComponent<Button>() == null || source.GetComponentsInChildren<Graphic>(true).Any(graphic => graphic.isActiveAndEnabled && graphic.raycastTarget)) return false;
            for (Transform parent = source.transform.parent; parent != null; parent = parent.parent)
            {
                Button button = parent.GetComponent<Button>();
                if (button == null) continue;
                if (!button.isActiveAndEnabled || !button.IsInteractable() || !parent.GetComponentsInChildren<Graphic>(true).Any(graphic => graphic.isActiveAndEnabled && graphic.raycastTarget)) return false;
                RectTransform rect = parent as RectTransform; if (rect == null) return false;
                Rect parentScreen = ScreenRect(rect, canvas);
                // Only a redundant nested surface matching the real button is
                // decorative. A standalone control with no hit surface still
                // fails reachability rather than disappearing from coverage.
                if ((parentScreen.center - screen.center).sqrMagnitude > 4 || Mathf.Abs(parentScreen.width - screen.width) > 2 || Mathf.Abs(parentScreen.height - screen.height) > 2) return false;
                owner = button; return true;
            }
            return false;
        }
        static bool ReachesControl(GameObject expected, GameObject hit, out string handler, out string expectedAction, out string actualAction)
        {
            handler = expectedAction = actualAction = "";
            if (hit == null) return false;
            var button = expected.GetComponent<Button>();
            var trigger = expected.GetComponent<EventTrigger>();
            if (button != null)
            {
                // GetEventHandler follows the same nearest-handler ancestor
                // walk as ExecuteHierarchy, without dispatching a real click.
                GameObject clicked = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit);
                GameObject pressed = ExecuteEvents.GetEventHandler<IPointerDownHandler>(hit);
                handler = "down=" + (pressed == null ? "none" : PathOf(pressed.transform)) + ";click=" + (clicked == null ? "none" : PathOf(clicked.transform));
                expectedAction = ControlActions(expected, EventTriggerType.PointerClick);
                actualAction = ControlActions(clicked, EventTriggerType.PointerClick);
                // The installed UGUI module stores pointerClick separately from
                // pointerPress, then compares the click owner on release.
                // Therefore a distinct pointer-down handler alone is not a
                // reason to reject an otherwise reachable click handler.
                if (clicked == null) return false;
                return SameControlOrEquivalentAncestor(expected, clicked, expectedAction, actualAction);
            }
            if (trigger != null && trigger.triggers != null)
            {
                bool found = false;
                foreach (EventTrigger.Entry entry in trigger.triggers)
                {
                    GameObject actual;
                    if (entry.eventID == EventTriggerType.PointerClick) actual = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit);
                    else if (entry.eventID == EventTriggerType.PointerDown) actual = ExecuteEvents.GetEventHandler<IPointerDownHandler>(hit);
                    else if (entry.eventID == EventTriggerType.BeginDrag) actual = ExecuteEvents.GetEventHandler<IDragHandler>(hit);
                    else continue;
                    found = true;
                    handler = actual == null ? "none" : PathOf(actual.transform);
                    expectedAction = ControlActions(expected, entry.eventID); actualAction = ControlActions(actual, entry.eventID);
                    if (!SameControlOrEquivalentAncestor(expected, actual, expectedAction, actualAction)) return false;
                }
                return found;
            }
            // Sliders, toggles, dropdowns and input fields normally own the
            // nearest pointer-down handler. Nested decorative Graphics bubble
            // to it; a competing Selectable or EventTrigger does not.
            GameObject owner = ExecuteEvents.GetEventHandler<IPointerDownHandler>(hit);
            if (owner == null) owner = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit);
            handler = owner == null ? "none" : PathOf(owner.transform);
            return owner == expected;
        }
        static bool SameControlOrEquivalentAncestor(GameObject expected, GameObject actual, string expectedAction, string actualAction)
        {
            if (actual == expected) return true;
            // Do not accept a different overlay button merely because it has
            // a similar action. Only a nested duplicate under its actual event
            // owner can be an alias, and its argument must agree as well.
            return actual != null && expected.transform.IsChildOf(actual.transform) && !string.IsNullOrEmpty(expectedAction) && expectedAction == actualAction;
        }
        static string ControlActions(GameObject source, EventTriggerType eventType)
        {
            var ui = FlowWaterRecoveryUI.Instance;
            if (source == null || ui == null) return "";
            return string.Join(";", ui.OriginalControls.Where(control => control.source == source &&
                ((eventType == EventTriggerType.PointerClick && control.button != null) || (control.trigger != null && control.eventType == eventType)))
                .Select(control => control.method + "(" + control.argument + ")").Distinct().OrderBy(value => value).ToArray());
        }
        static string PathOf(Transform transform) { string path = transform.name; for (Transform node = transform.parent; node != null; node = node.parent) path = node.name + "/" + path; return path; }
        static void Invoke(object instance, string method) { instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance, null); }
        static bool StablePortraitSize()
        {
            int width = Screen.width, height = Screen.height;
            if (width <= 0 || height <= width)
            {
                candidateWidth = candidateHeight = stableFrames = 0;
                stableSince = EditorApplication.timeSinceStartup;
                return false;
            }
            if (width != candidateWidth || height != candidateHeight)
            {
                candidateWidth = width; candidateHeight = height; stableFrames = 1;
                stableSince = EditorApplication.timeSinceStartup;
                return false;
            }
            stableFrames++;
            return stableFrames >= 4 && EditorApplication.timeSinceStartup - stableSince >= .5;
        }
        static void Save()
        {
            if (report == null) return;
            // The report's fixed dimensions own all run artifacts, even if a
            // later Game View resize causes the run to fail.
            Directory.CreateDirectory(RunDirectory);
            File.WriteAllText(Path.Combine(RunDirectory, "portrait-runtime-validation.json"), JsonUtility.ToJson(report, true));
        }
        static void Finish()
        {
            // Clear the trigger first; an I/O exception must never turn Finish
            // into a per-update retry loop or keep the suite alive next Play.
            SessionState.EraseBool(Key);
            try
            {
                if (report != null) { report.finishedUtc = DateTime.UtcNow.ToString("o"); report.passed = report.frames.Count == Menus.Length + Levels.Length * 2 && report.errors.Count == 0 && report.frames.All(frame => frame.passed); Save(); }
            }
            catch (Exception exception) { Debug.LogException(exception); }
            finally { ResetState(); EditorApplication.isPlaying = false; }
        }
        static void ResetState()
        {
            SessionState.EraseBool(Key); report = null; index = phase = 0; next = deadline = 0;
            candidateWidth = candidateHeight = stableFrames = 0; stableSince = 0; busy = false;
        }
        static void PlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode) ResetState();
        }
        static void Log(string message, string stack, LogType type) { if (report != null && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) report.errors.Add(message); }
    }

    // Uses the existing narrow, on-demand Editor automation allow-list.
    public static class FlowWaterArtPortraitValidation
    {
        public static void Export() { FlowWaterPortraitValidation.Export(); }
        public static void Capture() { FlowWaterPortraitValidation.Capture(); }
        public static void Audit() { FlowWaterPortraitValidation.Audit(); }
    }
}
#endif
