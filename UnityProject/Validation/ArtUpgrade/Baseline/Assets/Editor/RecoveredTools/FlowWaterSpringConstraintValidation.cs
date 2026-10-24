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

namespace Recovered.FlowWater.Editor
{
    // Runs the real LateUpdate simulation, not a substitute mechanism solver.
    [InitializeOnLoad]
    public static class FlowWaterSpringConstraintValidation
    {
        const string ActiveKey = "FlowWater.SpringConstraintValidation.Active";
        const string PrefsKey = "FlowWater.SpringConstraintValidation.Preferences";
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [Serializable] public sealed class Check { public string name, detail; public bool passed; }
        [Serializable] public sealed class SavedPreference { public string key; public bool existed; public int value; }
        [Serializable] public sealed class Preferences { public List<SavedPreference> values = new List<SavedPreference>(); }
        [Serializable] public sealed class Report
        {
            public string startedUtc, finishedUtc, unityVersion;
            public bool passed, preferencesRestored;
            public List<Check> checks = new List<Check>();
            public List<string> runtimeErrors = new List<string>();
            public List<string> screenshots = new List<string>();
            public List<string> notes = new List<string>();
        }
        sealed class Delay { public float seconds; public Delay(float seconds) { this.seconds = seconds; } }
        static Report report;
        static IEnumerator routine;
        static float resumeAt;
        static double deadline;
        static bool busy, importsLocked;
        static string Output { get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation", "SpringConstraints"); } }
        static RecoveredFlowGame Game { get { return RecoveredFlowGame.Instance; } }
        static RecoveredNativeSprings Springs { get { return Game == null ? null : Game.GetComponent<RecoveredNativeSprings>(); } }
        static RuntimeWaterMechanisms Bus { get { return RecoveredWaterNetwork.Instance == null ? null : RecoveredWaterNetwork.Instance.GetComponent<RuntimeWaterMechanisms>(); } }

        static FlowWaterSpringConstraintValidation()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += OnPlayMode;
            Application.logMessageReceived += OnLog;
        }
        [MenuItem("Tools/Flow Water/Validate Native Spring Collision Constraints")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start the spring validation from Edit mode.");
            RecoveryCatalogBuilder.Build();
            Directory.CreateDirectory(Output);
            var backup = new Preferences();
            var keys = new List<string> { "Recovery.LastLevelId" };
            foreach (int id in new[] { 1053, 1132 })
            { keys.Add("Recovery.Complete." + id); keys.Add("Recovery.Completed.gm" + id); }
            foreach (string key in keys) backup.values.Add(new SavedPreference { key = key, existed = PlayerPrefs.HasKey(key), value = PlayerPrefs.GetInt(key) });
            SessionState.SetString(PrefsKey, JsonUtility.ToJson(backup));
            SessionState.SetBool(ActiveKey, true);
            PlayerPrefs.SetInt("Recovery.LastLevelId", 1053);
            string sceneName=OriginalScenePresentation.SceneForLevel(1053);
            string scenePath=AssetDatabase.FindAssets(sceneName+" t:Scene",new[] { "Assets/Scenes" }).Select(AssetDatabase.GUIDToAssetPath).First(x=>Path.GetFileNameWithoutExtension(x)==sceneName);
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }
        static void Update()
        {
            if (busy || !SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            busy = true;
            try
            {
                if (report == null)
                {
                    AssetDatabase.DisallowAutoRefresh(); EditorApplication.LockReloadAssemblies(); importsLocked = true;
                    report = new Report { startedUtc = DateTime.UtcNow.ToString("o"), unityVersion = Application.unityVersion };
                    report.notes.Add("Controlled negative-input tests use original gm1053/gm1132 actors, serialized masks and production Prepare/OverlapSphere. Other authored actors are temporarily inactive and explicit temporary blockers or native busy flags isolate one rule at a time. Every active state, native snapshot and injected object is restored in finally. These tests do not claim normal recorded level completion.");
                    routine = Exercise(); deadline = EditorApplication.timeSinceStartup + 300; resumeAt = Time.time + .3f;
                }
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Spring validation exceeded 300 seconds.");
                if (Time.time < resumeAt) return;
                if (!routine.MoveNext()) { Finish(); return; }
                Delay delay = routine.Current as Delay; resumeAt = Time.time + (delay == null ? 0 : delay.seconds);
            }
            catch (Exception exception) { if (report != null) report.runtimeErrors.Add(exception.ToString()); Finish(); }
            finally { busy = false; }
        }
        static IEnumerator Exercise()
        {
            CheckState("unity-version",Application.unityVersion=="2022.3.28f1",Application.unityVersion);
            foreach(object wait in Load(1053)) yield return wait;
            RecoveryPiece rotor=Record(6);
            Require("original-sw1-rotator",rotor!=null && rotor.OriginalConfiguration.Contains("sz3ezsw1ew"),"gm1053 record6, original rotation action3 and valMove1.");
            object snapshot=CaptureBoard(); bool oldEnabled=Springs.enabled; Springs.enabled=false;
            var active=Game.Pieces.Where(p=>p!=null).ToDictionary(p=>p,p=>p.gameObject.activeSelf);
            GameObject obstacle=null;
            try
            {
                Isolate(rotor); Busy(); object binding=BindingFor(rotor);
                Require("original-rotation-binding",binding!=null,"The original restored observer and action sequence.");
                CheckState("rotation-all-four-neighbors-clear",Prepare(binding),"Native rotation can begin when all four20-unit probes are clear."); RestoreBoard(snapshot);
                obstacle=new GameObject("Native constraint validation temporary blocker"); obstacle.layer=0;
                var solid=new GameObject("solid"); solid.transform.SetParent(obstacle.transform,false); var collider=solid.AddComponent<BoxCollider>(); collider.size=Vector3.one*2;
                foreach(Vector3 side in new[] {Vector3.right,Vector3.left,Vector3.forward,Vector3.back})
                {
                    obstacle.transform.position=rotor.transform.position+side*20; Physics.SyncTransforms();
                    CheckState("rotation-original-cardinal-probe-"+side.ToString("F0"),!Prepare(binding),"Only the exact original native probe at"+obstacle.transform.position.ToString("F3")+" is obstructed.");
                    RestoreBoard(snapshot);
                }
                obstacle.SetActive(false); Physics.SyncTransforms();
                CheckState("rotation-removing-actual-collider-allows-motion",Prepare(binding),"The same original actor is accepted after the physical blocker is disabled."); RestoreBoard(snapshot);
                // gm1053 record9 is an original horizontal valMove1 actor.
                RecoveryPiece slide=Record(9); Isolate(slide); binding=BindingFor(slide); Busy(8);
                Require("original-sw1-slide-binding",binding!=null && slide.OriginalConfiguration.Contains("sw1ew"),"Original valMove1 configuration.");
                CheckState("horizontal-valMove1-does-not-wait-for-other-bus",Prepare(binding),"Unrelated channel8 busy is not a valMove1 gate."); RestoreBoard(snapshot);
            }
            finally
            {
                if(obstacle!=null) UnityEngine.Object.DestroyImmediate(obstacle); Busy(); RestoreBoard(snapshot);
                foreach(var pair in active) if(pair.Key!=null) pair.Key.gameObject.SetActive(pair.Value);
                Physics.SyncTransforms(); Springs.enabled=oldEnabled;
            }
            foreach(object wait in Load(1132)) yield return wait;
            RecoveryPiece mover=Record(19);
            Require("original-valMove2-slide",mover!=null && mover.OriginalConfiguration.Contains("st3etsz6ezsw2ew"),"gm1132 record19, channel3 horizontal action6 with native valMove2.");
            snapshot=CaptureBoard(); oldEnabled=Springs.enabled; Springs.enabled=false; active=Game.Pieces.Where(p=>p!=null).ToDictionary(p=>p,p=>p.gameObject.activeSelf);
            try
            {
                Isolate(mover); object binding=BindingFor(mover,3);
                Require("original-valMove2-binding",binding!=null && (int)Read(binding,"channel")==3 && (string)Read(binding,"actions")=="6","Original main st3/action6 observer, explicitly distinct from the first-registered <v47v> extra channel4/action7 observer."); Busy();
                CheckState("valMove2-all-channels-idle-allows",Prepare(binding),"No native busy channel: horizontal action accepted."); RestoreBoard(snapshot);
                Busy(3); CheckState("valMove2-own-channel-busy-exemption",Prepare(binding),"The original check explicitly exempts its own selected conResor channel3."); RestoreBoard(snapshot);
                for(int channel=1;channel<=8;channel++) if(channel!=3)
                {
                    Busy(channel); CheckState("valMove2-other-channel-busy-"+channel,!Prepare(binding),"Original doValidarMovimientos0 rejects unrelated activadoRes"+channel+"."); RestoreBoard(snapshot);
                }
                Busy(); CheckState("valMove2-cleared-bus-retry-allows",Prepare(binding),"Clearing the injected busy state re-enables the exact original move."); RestoreBoard(snapshot);
            }
            finally
            {
                Busy(); RestoreBoard(snapshot); foreach(var pair in active) if(pair.Key!=null) pair.Key.gameObject.SetActive(pair.Value);
                Physics.SyncTransforms(); Springs.enabled=oldEnabled;
            }
            yield return null;
        }
        static object Read(object source,string name) { var f=source.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic); return f==null?null:f.GetValue(source); }
        static object BindingFor(RecoveryPiece piece,int channel=0) { foreach(object b in (IEnumerable)Read(Springs,"bindings")) if(ReferenceEquals(Read(Read(b,"actor"),"piece"),piece) && (channel==0 || (int)Read(b,"channel")==channel)) return b; return null; }
        static bool Prepare(object binding) { return (bool)typeof(RecoveredNativeSprings).GetMethod("Prepare",Private).Invoke(Springs,new[] {binding,(object)0,(object)1}); }
        static void Busy(params int[] channels) { for(int i=1;i<=8;i++) typeof(UIManagerScript).GetField("activadoRes"+i).SetValue(null,Array.IndexOf(channels,i)>=0); }
        static void Isolate(RecoveryPiece actor) { foreach(RecoveryPiece p in Game.Pieces) if(p!=null) p.gameObject.SetActive(p==actor); Physics.SyncTransforms(); }
        static object CaptureBoard() { return typeof(RecoveredFlowGame).GetMethod("CaptureSnapshot",Private).Invoke(Game,null); }
        static void RestoreBoard(object snapshot) { typeof(RecoveredFlowGame).GetMethod("RestoreSnapshot",Private).Invoke(Game,new[] {snapshot}); }
        static IEnumerable Load(int id)
        {
            Require("ui-ready-gm" + id, FlowWaterRecoveryUI.Instance != null, "Original UI adapter starts the native scene.");
            FlowWaterRecoveryUI.Instance.StartGame(id);
            double until = EditorApplication.timeSinceStartup + 20;
            while (Game == null || Game.CurrentLevelId != id || !Game.IsLevelActive)
            { if (EditorApplication.timeSinceStartup > until) throw new TimeoutException("gm" + id + " did not load."); yield return null; }
            yield return new Delay(.6f);
            CheckState("gm" + id + "-original-mechanism-models", Springs != null && Springs.ObserverCount > 0, "Observers=" + (Springs == null ? 0 : Springs.ObserverCount) + ", plates=" + (Springs == null ? 0 : Springs.PhysicalSwitchCount));
        }
        static RecoveryPiece Record(int index) { return Game.Pieces.FirstOrDefault(p => p != null && p.SourceRecordIndex == index); }
        static RecoveryPiece At(Vector3 position) { return Game.Pieces.Where(p => p != null && p.CanMove && Near(p.transform.position, position)).OrderBy(p => (p.transform.position-position).sqrMagnitude).FirstOrDefault(); }
        static RecoverySave Solution(int id) { return JsonUtility.FromJson<RecoveryEnvelope>(RecoveryCatalog.Load().levels.First(e => e.assetId == id).solution.text).data; }
        static bool Near(Vector3 a, Vector3 b) { return (a-b).sqrMagnitude < .04f; }
        static string Positions(params RecoveryPiece[] pieces) { return String.Join("; ", pieces.Select(p => p == null ? "missing" : "record" + p.SourceRecordIndex + "=" + p.transform.position.ToString("F3")).ToArray()); }
        static void Require(string name, bool passed, string detail) { CheckState(name, passed, detail); if (!passed) throw new InvalidOperationException(name + ": " + detail); }
        static void CheckState(string name, bool passed, string detail)
        {
            report.checks.Add(new Check { name = name, passed = passed, detail = detail });
            File.WriteAllText(Path.Combine(Output, "spring-constraint-validation.json"), JsonUtility.ToJson(report, true));
        }
        static void Capture(string file)
        {
            Camera camera = Game.GameCamera; if (camera == null) return;
            string path = Path.Combine(Output, file);
            RenderTexture target = RenderTexture.GetTemporary(1280, 800, 24);
            RenderTexture old = camera.targetTexture, active = RenderTexture.active; float aspect = camera.aspect;
            Texture2D texture = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            try { camera.targetTexture = target; camera.aspect = 1.6f; Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target; texture.ReadPixels(new Rect(0,0,1280,800),0,0); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); report.screenshots.Add(path); }
            finally { camera.targetTexture = old; camera.aspect = aspect; RenderTexture.active = active; RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(texture); }
        }
        static bool RestorePreferences()
        {
            string json = SessionState.GetString(PrefsKey, ""); if (String.IsNullOrEmpty(json)) return false;
            Preferences backup = JsonUtility.FromJson<Preferences>(json);
            foreach (SavedPreference saved in backup.values) if (saved.existed) PlayerPrefs.SetInt(saved.key, saved.value); else PlayerPrefs.DeleteKey(saved.key);
            PlayerPrefs.Save(); SessionState.EraseString(PrefsKey);
            return backup.values.All(s => PlayerPrefs.HasKey(s.key) == s.existed && (!s.existed || PlayerPrefs.GetInt(s.key) == s.value));
        }
        static void OnLog(string message, string stack, LogType type) { if (report != null && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) report.runtimeErrors.Add(message + "\n" + stack); }
        static void OnPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode) ReleaseImports();
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(ActiveKey,false)) { RestorePreferences(); SessionState.EraseBool(ActiveKey); report = null; routine = null; }
        }
        static void ReleaseImports() { if (!importsLocked) return; importsLocked = false; EditorApplication.UnlockReloadAssemblies(); AssetDatabase.AllowAutoRefresh(); }
        static void Finish()
        {
            if (report == null) report = new Report();
            report.preferencesRestored = RestorePreferences(); report.finishedUtc = DateTime.UtcNow.ToString("o");
            report.passed = report.preferencesRestored && report.checks.Count > 0 && report.checks.All(c => c.passed) && report.runtimeErrors.Count == 0;
            Directory.CreateDirectory(Output); File.WriteAllText(Path.Combine(Output, "spring-constraint-validation.json"), JsonUtility.ToJson(report,true));
            bool passed = report.passed; Debug.Log("Native spring validation " + (passed ? "PASS" : "FAIL") + "; checks=" + report.checks.Count + "; errors=" + report.runtimeErrors.Count);
            SessionState.EraseBool(ActiveKey); report = null; routine = null; ReleaseImports();
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1); else EditorApplication.isPlaying = false;
        }
    }
}
#endif
