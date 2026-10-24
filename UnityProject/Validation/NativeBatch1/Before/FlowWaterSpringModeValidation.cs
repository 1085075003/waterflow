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
    public static class FlowWaterSpringModeValidation
    {
        const string ActiveKey = "FlowWater.SpringModeValidation.Active";
        const string PrefsKey = "FlowWater.SpringModeValidation.Preferences";
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
        static string Output { get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation", "SpringModes"); } }
        static RecoveredFlowGame Game { get { return RecoveredFlowGame.Instance; } }
        static RecoveredNativeSprings Springs { get { return Game == null ? null : Game.GetComponent<RecoveredNativeSprings>(); } }
        static RuntimeWaterMechanisms Bus { get { return RecoveredWaterNetwork.Instance == null ? null : RecoveredWaterNetwork.Instance.GetComponent<RuntimeWaterMechanisms>(); } }

        static FlowWaterSpringModeValidation()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += OnPlayMode;
            Application.logMessageReceived += OnLog;
        }
        [MenuItem("Tools/Flow Water/Validate Native Spring Plate Modes")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start the spring validation from Edit mode.");
            RecoveryCatalogBuilder.Build();
            Directory.CreateDirectory(Output);
            var backup = new Preferences();
            var keys = new List<string> { "Recovery.LastLevelId" };
            foreach (int id in new[] { 1130, 1132 })
            { keys.Add("Recovery.Complete." + id); keys.Add("Recovery.Completed.gm" + id); }
            foreach (string key in keys) backup.values.Add(new SavedPreference { key = key, existed = PlayerPrefs.HasKey(key), value = PlayerPrefs.GetInt(key) });
            SessionState.SetString(PrefsKey, JsonUtility.ToJson(backup));
            SessionState.SetBool(ActiveKey, true);
            PlayerPrefs.SetInt("Recovery.LastLevelId", 1130);
            string sceneName=OriginalScenePresentation.SceneForLevel(1130);
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
                    report.notes.Add("Controlled mode tests use original gm1130/gm1132 plates and real public TryMove pickup/placement. Only this helper's automatic LateUpdate is paused while its production UpdatePlates is explicitly called, so injected native activadoRes bus flags remain under test control. This is a focused gate/material/snapshot unit test, not recorded whole-level gameplay proof.");
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
            foreach(object wait in Load(1130)) yield return wait;
            RecoveryPiece platePiece=Record(43),press=Game.Pieces.FirstOrDefault(p=>p.CanMove && p.GetComponent<ControlPieza>()!=null);
            Require("mode3-original-fixture",platePiece!=null && press!=null,"gm1130 record43 <r1r><t3t>, original movable pipe.");
            Resorte3 model=platePiece.GetComponentInChildren<Resorte3>(true);
            Require("mode3-original-model",model!=null && model.modoRes3=="3","The restored original pressure model retains native mode3.");
            object board=typeof(RecoveredFlowGame).GetMethod("CaptureSnapshot",Private).Invoke(Game,null);
            bool previousEnabled=Springs.enabled; Springs.enabled=false;
            try
            {
                Vector3 start=press.transform.position,cell=model.transform.position+Vector3.up*model.altura;
                object plate=InternalPlate(model);
                Require("mode3-production-plate-binding",plate!=null && Phase(plate)==1,"Native first phase.");
                for(int channel=1;channel<=8;channel++)
                {
                    Busy(channel);
                    Require("mode3-real-placement-busy-"+channel,Game.TryMove(press.Index,cell),Game.Status); Tick();
                    CheckState("mode3-all-eight-busy-gates-"+channel,Phase(plate)==1 && !Output1(plate),"activadoRes"+channel+"=true rejects an original plate whose own channel is1.");
                    Require("mode3-real-pickup-busy-"+channel,Game.TryMove(press.Index,start),Game.Status); Tick();
                }
                Busy(); Require("mode3-real-placement-all-idle",Game.TryMove(press.Index,cell),Game.Status); Tick();
                yield return new Delay(.3f); Tick();
                CheckState("mode3-idle-accepts-and-paints-pressed",Phase(plate)==2 && Output1(plate) && MaterialIs(model,2),"All channels idle accepts phase1 and uses original materialTextura2.");
                Require("mode3-real-pickup-holds-toggle",Game.TryMove(press.Index,start),Game.Status); Tick();
                CheckState("mode3-release-holds-toggle",Output1(plate),"Mode3 has latched release semantics.");
                Busy(8); Require("mode3-second-press-busy8",Game.TryMove(press.Index,cell),Game.Status); Tick();
                CheckState("mode3-active-toggle-still-obeys-global-gate",Phase(plate)==2 && Output1(plate),"A busy unrelated eighth channel also blocks the reverse press.");
                Require("mode3-pickup-after-blocked-reverse",Game.TryMove(press.Index,start),Game.Status); Tick(); Busy();
                Require("mode3-idle-reverse-placement",Game.TryMove(press.Index,cell),Game.Status); Tick(); yield return new Delay(.3f); Tick();
                CheckState("mode3-idle-reverse-and-texture",Phase(plate)==1 && !Output1(plate) && MaterialIs(model,1),"Original doSubirPieza/doTextura1 state.");
            }
            finally { Busy(); typeof(RecoveredFlowGame).GetMethod("RestoreSnapshot",Private).Invoke(Game,new[] { board }); Springs.enabled=previousEnabled; }

            foreach(object wait in Load(1132)) yield return wait;
            platePiece=Record(1); press=Game.Pieces.FirstOrDefault(p=>p.CanMove && p.GetComponent<ControlPieza>()!=null);
            model=platePiece==null?null:platePiece.GetComponentInChildren<Resorte3>(true);
            Require("mode2-original-fixture",model!=null && model.modoRes3=="2" && model.conResorte=="34" && press!=null,"gm1132 record1 <r34r><t2t>.");
            board=typeof(RecoveredFlowGame).GetMethod("CaptureSnapshot",Private).Invoke(Game,null); previousEnabled=Springs.enabled; Springs.enabled=false;
            try
            {
                Vector3 start=press.transform.position,cell=model.transform.position+Vector3.up*model.altura; object plate=InternalPlate(model);
                Busy(4); Require("mode2-phase1-actual-press-other-busy",Game.TryMove(press.Index,cell),Game.Status); Tick();
                CheckState("mode2-phase1-waits-only-for-second-channel",Phase(plate)==1 && !Output1(plate),"Channel4 busy blocks first activation.");
                Require("mode2-phase1-pickup",Game.TryMove(press.Index,start),Game.Status); Tick();
                Busy(3); Require("mode2-phase1-actual-press-own-busy",Game.TryMove(press.Index,cell),Game.Status); Tick(); yield return new Delay(.3f); Tick();
                CheckState("mode2-phase1-ignores-first-channel-busy",Phase(plate)==2 && Output1(plate) && MaterialIs(model,2),"Channel3 busy does not block phase1 when channel4 is idle.");
                Require("mode2-phase2-pickup",Game.TryMove(press.Index,start),Game.Status); Tick();
                Busy(3,4); Require("mode2-phase2-release-press-both-busy",Game.TryMove(press.Index,cell),Game.Status); Tick(); yield return new Delay(.3f); Tick();
                CheckState("mode2-phase2-releases-without-wait-and-texture3",Phase(plate)==3 && !Output1(plate) && !Output2(plate) && MaterialIs(model,3),"Both channels busy: first release still executes and displays native materialTextura3.");
                Require("mode2-phase3-pickup",Game.TryMove(press.Index,start),Game.Status); Tick(); Busy();
                object phase3=typeof(RecoveredFlowGame).GetMethod("CaptureSnapshot",Private).Invoke(Game,null);
                Busy(3); Require("mode2-phase3-press-first-busy",Game.TryMove(press.Index,cell),Game.Status); Tick();
                CheckState("mode2-phase3-waits-only-for-first-channel",Phase(plate)==3 && !Output2(plate),"Channel3 busy blocks second activation.");
                Require("mode2-phase3-pickup-after-block",Game.TryMove(press.Index,start),Game.Status); Tick();
                Busy(4); Require("mode2-phase3-press-second-busy",Game.TryMove(press.Index,cell),Game.Status); Tick(); yield return new Delay(.3f); Tick();
                CheckState("mode2-phase3-ignores-second-busy-and-texture2",Phase(plate)==4 && Output2(plate) && MaterialIs(model,2),"Second activation waits for channel3 only and displays texture2.");
                Require("mode2-phase4-pickup",Game.TryMove(press.Index,start),Game.Status); Tick();
                Busy(3,4); Require("mode2-phase4-release-press-both-busy",Game.TryMove(press.Index,cell),Game.Status); Tick(); yield return new Delay(.3f); Tick();
                CheckState("mode2-phase4-releases-without-wait-and-texture1",Phase(plate)==1 && !Output1(plate) && !Output2(plate) && MaterialIs(model,1),"Second release executes while both channels are busy, returning to native texture1.");
                Busy(); typeof(RecoveredFlowGame).GetMethod("RestoreSnapshot",Private).Invoke(Game,new[] { phase3 });
                CheckState("mode2-snapshot-restores-phase3-texture3",Phase(plate)==3 && MaterialIs(model,3),"Snapshot restores the exact four-phase display, not a boolean-color approximation.");
            }
            finally { Busy(); typeof(RecoveredFlowGame).GetMethod("RestoreSnapshot",Private).Invoke(Game,new[] { board }); Springs.enabled=previousEnabled; }
        }
        static object Read(object source,string name) { var f=source.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic); return f==null?null:f.GetValue(source); }
        static object InternalPlate(Resorte3 model) { foreach(object p in (IEnumerable)Read(Springs,"plates")) if(ReferenceEquals(Read(p,"source"),model)) return p; return null; }
        static int Phase(object plate) { return (int)Read(plate,"phase"); }
        static bool Output1(object plate) { return (bool)Read(plate,"output1"); }
        static bool Output2(object plate) { return (bool)Read(plate,"output2"); }
        static void Tick() { typeof(RecoveredNativeSprings).GetMethod("UpdatePlates",Private).Invoke(Springs,null); }
        static void Busy(params int[] channels) { for(int i=1;i<=8;i++) typeof(UIManagerScript).GetField("activadoRes"+i).SetValue(null,Array.IndexOf(channels,i)>=0); }
        static bool MaterialIs(Resorte3 model,int index)
        { Material material=(Material)typeof(Resorte3).GetField("materialTextura"+index).GetValue(model); return model.cilinder1.GetComponent<Renderer>().sharedMaterial==material && model.cilinder2.GetComponent<Renderer>().sharedMaterial==material; }
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
            File.WriteAllText(Path.Combine(Output, "spring-mode-validation.json"), JsonUtility.ToJson(report, true));
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
            Directory.CreateDirectory(Output); File.WriteAllText(Path.Combine(Output, "spring-mode-validation.json"), JsonUtility.ToJson(report,true));
            bool passed = report.passed; Debug.Log("Native spring validation " + (passed ? "PASS" : "FAIL") + "; checks=" + report.checks.Count + "; errors=" + report.runtimeErrors.Count);
            SessionState.EraseBool(ActiveKey); report = null; routine = null; ReleaseImports();
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1); else EditorApplication.isPlaying = false;
        }
    }
}
#endif
