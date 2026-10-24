#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Recovered.FlowWater.Editor
{
    [InitializeOnLoad]
    public static class FlowWaterBottomMarkerValidation
    {
        const string Key = "FlowWater.BottomMarkerValidation.";
        [Serializable] sealed class Pref { public string key, text; public bool existed, isString; public int value; }
        [Serializable] sealed class Prefs { public List<Pref> values = new List<Pref>(); }
        [Serializable] sealed class SavedScene { public string path; public bool loaded, active; }
        [Serializable] sealed class Scenes { public SavedScene[] values; }
        [Serializable] sealed class Check { public string name, detail; public bool passed; }
        [Serializable] sealed class Report
        {
            public string unityVersion, startedUtc, finishedUtc;
            public bool passed, preferencesRestored;
            public List<Check> checks = new List<Check>();
            public List<string> errors = new List<string>();
        }
        static Report report;
        static IEnumerator routine;
        static bool busy, locked;
        static int nextFrame;
        static double deadline;
        static string Output { get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation", "BottomMarker"); } }
        static FlowWaterBottomMarkerValidation()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode) Unlock();
                if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Key + "active", false)) return;
                Restore(); SessionState.EraseBool(Key + "active");
                int exit = SessionState.GetInt(Key + "exit", -1);
                if (Application.isBatchMode && exit >= 0) EditorApplication.Exit(exit);
            };
            Application.logMessageReceived += (text, stack, type) =>
            {
                if (report != null && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) report.errors.Add(text + "\n" + stack);
            };
        }
        [MenuItem("Tools/Flow Water/Validate Original Bottom Marker Contact")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start bottom marker validation in Edit mode.");
            SceneSetup[] scenes = EditorSceneManager.GetSceneManagerSetup();
            foreach (SceneSetup scene in scenes)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scene.path).isDirty) throw new InvalidOperationException("Save edited scenes first.");
            var prefs = new Prefs();
            foreach (string key in new[] { "helpRotation", "Recovery.LastLevelId", "Recovery.Complete.1236", "Recovery.Completed.gm1236", "Recovery.Complete.1901", "Recovery.Completed.gm1901", "esTablet", "OptionSelLev", "primeraVez", "MusicaON", "Soundlevel", "language" })
                prefs.values.Add(new Pref { key = key, existed = PlayerPrefs.HasKey(key), isString = key == "language", value = PlayerPrefs.GetInt(key), text = PlayerPrefs.GetString(key) });
            SessionState.SetString(Key + "prefs", JsonUtility.ToJson(prefs));
            SessionState.SetString(Key + "scenes", JsonUtility.ToJson(new Scenes { values = scenes.Select(s => new SavedScene { path = s.path, loaded = s.isLoaded, active = s.isActive }).ToArray() }));
            SessionState.SetFloat(Key + "scale", Time.timeScale); SessionState.SetBool(Key + "active", true);
            RecoveryCatalogBuilder.Build(); EditorSceneManager.OpenScene("Assets/Scenes/Portada.unity", OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }
        static void Update()
        {
            if (busy || !SessionState.GetBool(Key + "active", false) || !EditorApplication.isPlaying || EditorApplication.isCompiling || Time.frameCount < nextFrame) return;
            busy = true;
            try
            {
                if (report == null)
                {
                    AssetDatabase.DisallowAutoRefresh(); EditorApplication.LockReloadAssemblies(); locked = true;
                    report = new Report { unityVersion = Application.unityVersion, startedUtc = DateTime.UtcNow.ToString("o") };
                    Time.timeScale = 4; deadline = EditorApplication.timeSinceStartup + 120; routine = Exercise();
                }
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Bottom marker validation exceeded120 seconds.");
                if (!routine.MoveNext()) { Finish(); return; }
                nextFrame = Time.frameCount + 1;
            }
            catch (Exception ex) { report.errors.Add(ex.ToString()); Finish(); }
            finally { busy = false; }
        }
        static readonly System.Reflection.BindingFlags PrivateStatic = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
        static readonly System.Reflection.BindingFlags AnyField = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
        static T Field<T>(object value, string name) { return (T)value.GetType().GetField(name, AnyField).GetValue(value); }
        static object NodeFor(RecoveredWaterNetwork water, int record)
        {
            foreach (object node in Field<IEnumerable>(water, "nodes"))
                if (Field<RecoveryPiece>(node, "piece").SourceRecordIndex == record) return node;
            return null;
        }
        static MonoBehaviour Origin(object node, int incoming)
        { return (MonoBehaviour)typeof(RecoveredWaterNetwork).GetMethod("PrefabOrigin", PrivateStatic).Invoke(null, new[] { node, (object)incoming }); }
        static bool Contact(object source, object target)
        { return (bool)typeof(RecoveredWaterNetwork).GetMethod("BottomMarkerContact", PrivateStatic).Invoke(null, new[] { source, target }); }
        static GameObject CloneWater(object node)
        {
            MonoBehaviour control = Field<MonoBehaviour>(node, "controller");
            GameObject clone = UnityEngine.Object.Instantiate(Field<GameObject>(node, "prefab"), control.transform, false);
            clone.name = "Original bottom marker geometry fixture";
            clone.transform.position = control.transform.position;
            clone.transform.rotation = (Quaternion)node.GetType().GetProperty("Rotation").GetValue(node);
            clone.transform.localScale = Vector3.one * .1f;
            foreach (MonoBehaviour script in clone.GetComponentsInChildren<MonoBehaviour>(true)) if (script != null) script.enabled = false;
            foreach (Collider collider in clone.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (Renderer renderer in clone.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            foreach (Rigidbody body in clone.GetComponentsInChildren<Rigidbody>(true)) { body.useGravity = false; body.isKinematic = true; }
            return clone;
        }
        static SphereCollider MatchingSphere(object node, GameObject clone, SphereCollider original)
        {
            Transform root = Field<GameObject>(node, "prefab").transform;
            var indices = new Stack<int>();
            for (Transform cursor = original.transform; cursor != root; cursor = cursor.parent) indices.Push(cursor.GetSiblingIndex());
            Transform target = clone.transform;
            while (indices.Count > 0) target = target.GetChild(indices.Pop());
            SphereCollider result = target.GetComponent<SphereCollider>();
            for (Transform cursor = target; cursor != null && cursor != clone.transform.parent; cursor = cursor.parent) cursor.gameObject.SetActive(true);
            result.enabled = true; return result;
        }
        static bool PhysicsContact(SphereCollider source, SphereCollider target)
        {
            Physics.SyncTransforms(); Vector3 direction; float distance;
            return Physics.ComputePenetration(source, source.transform.position, source.transform.rotation,
                target, target.transform.position, target.transform.rotation, out direction, out distance);
        }
        static IEnumerator Exercise()
        {
            Require("unity-2022.3.28f1", Application.unityVersion == "2022.3.28f1", Application.unityVersion);
            while (FlowWaterRecoveryUI.Instance == null) yield return null;
            FlowWaterRecoveryUI.Instance.StartGame(1236);
            while (RecoveredFlowGame.Instance == null || RecoveredFlowGame.Instance.CurrentLevelId != 1236 || RecoveredFlowGame.Instance.PieceRoot == null) yield return null;
            RecoveredFlowGame game = RecoveredFlowGame.Instance; game.Solve();
            RecoveredWaterNetwork water = RecoveredWaterNetwork.Instance; water.Rebuild();
            object source = NodeFor(water, 69), target = NodeFor(water, 33);
            Require("actual-1236-native-C-and-pipe", source != null && target != null && Field<MonoBehaviour>(source, "controller") is ControlPiezaC,
                "original record69 C pool and record33 ordinary top-intake pipe");
            MonoBehaviour sourceControl = Field<MonoBehaviour>(source, "controller"), targetControl = Field<MonoBehaviour>(target, "controller");
            Vector3 authoredPosition = sourceControl.transform.position;
            Require("actual-1236-cross-empty-layer", Mathf.Abs(authoredPosition.y - targetControl.transform.position.y - 15.2f) < .01f,
                "native two-level separation, not an invented fixture geometry");
            SphereCollider sourceShape = Field<GameObject>(Origin(source, 1), "esfera6").GetComponent<SphereCollider>();
            SphereCollider targetShape = Origin(target, 5).GetComponent<SphereCollider>();
            Require("original-marker-shapes", sourceShape != null && targetShape != null && Mathf.Approximately(sourceShape.radius, .1f) && Mathf.Approximately(targetShape.radius, .4f),
                "original esfera6 radius.1 at child scale2; actual Origin5 radius.4");
            GameObject sourceClone = null, targetClone = null;
            try
            {
                sourceClone = CloneWater(source); targetClone = CloneWater(target);
                SphereCollider sourceSphere = MatchingSphere(source, sourceClone, sourceShape), targetSphere = MatchingSphere(target, targetClone, targetShape);
                bool nativeFar = PhysicsContact(sourceSphere, targetSphere), adapterFar = Contact(source, target);
                Require("C6-cross-layer-real-physics-rejects", !nativeFar, "actual original cloned collider geometry does not intersect across15.2");
                Require("C6-cross-layer-adapter-matches", adapterFar == nativeFar, "production marker predicate matches actual Unity collider contact");
                sourceControl.transform.position = authoredPosition - Vector3.up * 7.6f;
                bool nativeNear = PhysicsContact(sourceSphere, targetSphere), adapterNear = Contact(source, target);
                Require("C6-adjacent-real-physics-connects", nativeNear, "same original shapes at next7.6 level: centers.4 apart, radii sum.6");
                Require("C6-adjacent-adapter-matches", adapterNear == nativeNear, "production predicate retains real adjacent-level flow");
            }
            finally
            {
                sourceControl.transform.position = authoredPosition;
                if (sourceClone != null) UnityEngine.Object.DestroyImmediate(sourceClone);
                if (targetClone != null) UnityEngine.Object.DestroyImmediate(targetClone);
                Physics.SyncTransforms();
            }
            FlowWaterRecoveryUI.Instance.StartGame(1901);
            while (game.CurrentLevelId != 1901 || game.PieceRoot == null) yield return null;
            game.Solve(); water.Rebuild();
            object longSource = NodeFor(water, 11), longTarget = NodeFor(water, 17);
            Require("actual-1901-ordinary-bottom-cascade", longSource != null && longTarget != null && Field<MonoBehaviour>(longSource, "controller") is ControlPieza,
                "original BloqueP9_2 record11 and Recta6j_4 record17");
            Array links = Field<Array>(longSource, "links");
            float drop = Field<MonoBehaviour>(longSource, "controller").transform.position.y - Field<MonoBehaviour>(longTarget, "controller").transform.position.y;
            Require("ordinary-CP-long-fall-retained", drop > 15 && System.Object.ReferenceEquals(links.GetValue(5), longTarget),
                "production CP6 graph still crosses15.2 to the real next lower intake");
            GameObject nativePrefab = Field<GameObject>(longSource, "prefab");
            Require("ordinary-CP-authored-Cascada6-exists", nativePrefab.GetComponentsInChildren<CascadaTest>(true).Any(c => c.cascada6),
                "positive long-drop case has an original moving/extended bottom-waterfall controller");
            float until = Time.time + 180;
            while (water.HasPendingPropagation && Time.time < until) yield return null;
            Require("actual-1901-full-colored-flow", !water.HasPendingPropagation && water.RequiredFountainCount > 0 && water.ActiveFountainCount >= water.RequiredFountainCount,
                water.ActiveFountainCount + "/" + water.RequiredFountainCount + " correct-color fountains after original propagation");
        }
        static HelpRotate FindHelp()
        {
            foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            { HelpRotate result = root.GetComponentInChildren<HelpRotate>(true); if (result != null) return result; }
            return null;
        }
        static void Require(string name, bool passed, string detail)
        {
            report.checks.Add(new Check { name = name, passed = passed, detail = detail });
            if (!passed) throw new InvalidOperationException(name + ": " + detail);
        }
        static void Finish()
        {
            report.finishedUtc = DateTime.UtcNow.ToString("o");
            report.passed = report.errors.Count == 0 && report.checks.All(c => c.passed);
            RestorePreferences(); report.preferencesRestored = true;
            Directory.CreateDirectory(Output); File.WriteAllText(Path.Combine(Output, "bottom-marker-validation.json"), JsonUtility.ToJson(report, true));
            SessionState.SetInt(Key + "exit", report.passed ? 0 : 2); Unlock(); EditorApplication.ExitPlaymode();
        }
        static void RestorePreferences()
        {
            Prefs prefs = JsonUtility.FromJson<Prefs>(SessionState.GetString(Key + "prefs", ""));
            if (prefs == null) return;
            foreach (Pref p in prefs.values)
                if (!p.existed) PlayerPrefs.DeleteKey(p.key); else if (p.isString) PlayerPrefs.SetString(p.key, p.text); else PlayerPrefs.SetInt(p.key, p.value);
            PlayerPrefs.Save(); Time.timeScale = SessionState.GetFloat(Key + "scale", 1);
        }
        static void Restore()
        {
            RestorePreferences();
            Scenes scenes = JsonUtility.FromJson<Scenes>(SessionState.GetString(Key + "scenes", ""));
            if (scenes != null && scenes.values != null && scenes.values.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(scenes.values.Select(s => new SceneSetup { path = s.path, isLoaded = s.loaded, isActive = s.active }).ToArray());
        }
        static void Unlock()
        {
            if (!locked) return; locked = false; EditorApplication.UnlockReloadAssemblies(); AssetDatabase.AllowAutoRefresh();
        }
    }
}
#endif
