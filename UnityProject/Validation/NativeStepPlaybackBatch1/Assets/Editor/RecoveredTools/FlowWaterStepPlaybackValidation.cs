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
using UnityEngine.EventSystems;

namespace Recovered.FlowWater.Editor
{
    [InitializeOnLoad]
    public static class FlowWaterStepPlaybackValidation
    {
        const string Key="FlowWater.NativeStepPlayback.Validation";
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        [Serializable] public sealed class Check { public string name,detail; public bool passed; }
        [Serializable] public sealed class Pref { public string key; public bool existed; public int value; }
        [Serializable] public sealed class Backup { public List<Pref> prefs=new List<Pref>(); public float scale; public SceneSetup[] scenes; public int[] cases; }
        [Serializable] public sealed class Report
        {
            public string startedUtc,finishedUtc,unityVersion;
            public bool passed,preferencesRestored;
            public float originalTimeScale;
            public List<Check> checks=new List<Check>();
            public List<string> runtimeErrors=new List<string>();
            public List<string> notes=new List<string>();
        }
        static Report report;
        static IEnumerator routine;
        static bool busy,locked;
        static int frame;
        static double deadline;
        static string Output { get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Validation","NativeStepPlayback"); } }
        static RecoveredFlowGame Game { get { return RecoveredFlowGame.Instance; } }
        static FlowWaterRecoveryUI UI { get { return FlowWaterRecoveryUI.Instance; } }
        static FlowWaterStepPlaybackValidation()
        {
            EditorApplication.update+=Update;
            EditorApplication.playModeStateChanged+=OnPlayMode;
            Application.logMessageReceived+=OnLog;
        }
        [MenuItem("Tools/Flow Water/Validate Original Animated Step Preview")]
        public static void Run() { Start(new[]{601,1851,1129}); }
        public static void RunMechanisms() { Start(new[]{1656}); }
        public static void RunLifts() { Start(new[]{1129,1754,1961}); }
        static void Start(int[] cases)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start animated preview validation in Edit mode.");
            Directory.CreateDirectory(Output); RecoveryCatalogBuilder.Build();
            var saved=new Backup {scale=Time.timeScale,scenes=EditorSceneManager.GetSceneManagerSetup(),cases=cases};
            var keys=new List<string>{"Recovery.LastLevelId","helpRotation"};
            foreach(RecoveryLevelEntry entry in RecoveryCatalog.Load().levels)
            { keys.Add("Recovery.Complete."+entry.assetId); keys.Add("Recovery.Completed.gm"+entry.assetId); }
            foreach(string key in keys) saved.prefs.Add(new Pref{key=key,existed=PlayerPrefs.HasKey(key),value=PlayerPrefs.GetInt(key)});
            string json=JsonUtility.ToJson(saved,true); File.WriteAllText(Path.Combine(Output,"preferences-before.json"),json);
            SessionState.SetString(Key+".backup",json); SessionState.SetBool(Key,true);
            try
            {
                PlayerPrefs.SetInt("Recovery.LastLevelId",cases[0]);
                string name=OriginalScenePresentation.SceneForLevel(cases[0]);
                string scene=AssetDatabase.FindAssets(name+" t:Scene",new[]{"Assets/Scenes"}).Select(AssetDatabase.GUIDToAssetPath).First(x=>Path.GetFileNameWithoutExtension(x)==name);
                EditorSceneManager.OpenScene(scene,OpenSceneMode.Single); EditorApplication.EnterPlaymode();
            }
            catch { Restore(); SessionState.EraseBool(Key); throw; }
        }
        static void Update()
        {
            if(busy || !SessionState.GetBool(Key,false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            busy=true;
            try
            {
                if(report==null)
                {
                    AssetDatabase.DisallowAutoRefresh(); EditorApplication.LockReloadAssemblies(); locked=true;
                    Backup backup=JsonUtility.FromJson<Backup>(SessionState.GetString(Key+".backup", ""));
                    report=new Report {startedUtc=DateTime.UtcNow.ToString("o"),unityVersion=Application.unityVersion,originalTimeScale=backup.scale};
                    report.notes.Add("Actual original solution/steps/back Buttons or EventTrigger callbacks start the production automatic preview. No direct actor, guide, water or switch assignments by this validator. Preview success is animation/recorded-cursor proof, not normal gameplay completion or zero-target victory.");
                    report.notes.Add("Original imported timeScale is retained. Native 2-second startup, 3-phase Lerp2.2, fixed-roof MoveTowards28/Lerp2.6 and strict automatic-w busy-to-idle state remain production behavior. Optional lifts/mechanism entrypoints broaden cases without weakening deadlines or injecting bus states.");
                    routine=Exercise(backup.cases); frame=Time.frameCount+8; deadline=EditorApplication.timeSinceStartup+1200;
                }
                if(EditorApplication.timeSinceStartup>deadline) throw new TimeoutException("Animated preview validation exceeded1200 real seconds.");
                if(Time.frameCount<frame) return;
                frame=Time.frameCount+1;
                if(!routine.MoveNext()) Finish();
            }
            catch(Exception exception) { if(report!=null) report.runtimeErrors.Add(exception.ToString()); Finish(); }
            finally { busy=false; }
        }
        static IEnumerator Exercise(int[] cases)
        {
            Require("unity-version",Application.unityVersion=="2022.3.28f1",Application.unityVersion);
            foreach(int id in cases)
            {
                UI.StartGame(id);
                double until=EditorApplication.timeSinceStartup+30;
                while((Game==null || Game.CurrentLevelId!=id || !Game.IsLevelActive) && EditorApplication.timeSinceStartup<until) yield return null;
                Require("gm"+id+"-actual-level",Game!=null && Game.CurrentLevelId==id && Game.IsLevelActive,"Original scene and mode0 board loaded through production UI.");
                for(int f=0;f<8;f++) yield return null;
                Vector3[] before=Positions();
                Click("showSolution"); for(int f=0;f<2;f++) yield return null;
                Click("okSolution"); for(int f=0;f<3;f++) yield return null;
                Require("gm"+id+"-final-preview",Game.IsSolutionPreview && Game.IsShowingFinalSolution,"Original final-solution stage remains separate from animated mode0.");
                Click("okSteps"); float entered=Time.time;
                Require("gm"+id+"-mode0-rebuild",Game.IsSolutionPreview && !Game.IsShowingFinalSolution && Game.AppliedSolutionSteps==0,"Steps rebuilds original initial actors/mechanisms, not final proxies.");
                while(Time.time-entered<1) { Require("gm"+id+"-native-two-second-start",!Game.IsSolutionStepAnimating && Game.AppliedSolutionSteps==0,"Original okSteps Invoke(doMoveBloque2,2)."); yield return null; }
                until=EditorApplication.timeSinceStartup+30;
                while(!Game.IsSolutionStepAnimating && Game.AppliedSolutionSteps==0 && EditorApplication.timeSinceStartup<until) yield return null;
                Require("gm"+id+"-real-animation-started",Game.IsSolutionStepAnimating,"Original steps button must begin visible movement instead of an instantaneous placement.");
                var playback=Game.GetComponent<RecoveredNativeStepPlayback>();
                Vector3 first=playback.Actor.transform.position; int cursor=Game.AppliedSolutionSteps;
                for(int f=0;f<3;f++) yield return null;
                Require("gm"+id+"-transient-displacement",Game.IsSolutionStepAnimating && Vector3.Distance(first,playback.Actor.transform.position)>.01f && Game.AppliedSolutionSteps==cursor,"Actor changes position over real frames; cursor commits only after phase9.");
                // Recovery transaction coverage: snapshot/restore a genuine
                // in-flight frame, then Undo cancels before the recorded step.
                object snapshot=typeof(RecoveredFlowGame).GetMethod("CaptureSnapshot",Hidden).Invoke(Game,null);
                Vector3[] mid=Positions(); int phase=Game.NativeSolutionPhase;
                for(int f=0;f<3;f++) yield return null;
                typeof(RecoveredFlowGame).GetMethod("RestoreSnapshot",Hidden).Invoke(Game,new[]{snapshot});
                Require("gm"+id+"-midflight-snapshot",Same(mid) && Game.IsSolutionStepAnimating && Game.NativeSolutionPhase==phase,"Restore original poses, movement phase, selected body layer and pending water gate.");
                Game.Undo();
                Require("gm"+id+"-midflight-undo",!Game.IsSolutionStepAnimating && !Game.IsSolutionPlaybackRunning && Game.AppliedSolutionSteps==0,"Undo pauses and restores the pre-step transaction.");
                while(Time.time-entered<6.1f) yield return null;
                Click("doSolucionLevel"); for(int f=0;f<2;f++) yield return null;
                Require("gm"+id+"-exit-after-undo",!Game.IsSolutionPreview && Same(before),"Original back callback restores the complete pre-preview board.");
                Click("showSolution"); yield return null; Click("okSolution"); yield return null; Click("okSteps");
                var phases=new HashSet<int>(); bool sawMotion=false,sawWait=false; float highest=float.NegativeInfinity;
                int previousCursor=0; until=EditorApplication.timeSinceStartup+180;
                while((Game.IsSolutionPlaybackRunning || Game.AppliedSolutionSteps<Game.SolutionStepCount) && EditorApplication.timeSinceStartup<until)
                {
                    playback=Game.GetComponent<RecoveredNativeStepPlayback>();
                    if(playback!=null && playback.IsActive)
                    {
                        phases.Add(playback.Phase); sawWait|=playback.IsWaiting; sawMotion|=!playback.IsWaiting;
                        if(playback.Actor!=null) highest=Mathf.Max(highest,playback.Actor.transform.position.y);
                    }
                    Require("gm"+id+"-monotonic-recorded-cursor",Game.AppliedSolutionSteps>=previousCursor && Game.AppliedSolutionSteps<=Game.SolutionStepCount,"Cursor commits completed native phases only.");
                    previousCursor=Game.AppliedSolutionSteps; yield return null;
                }
                Require("gm"+id+"-automatic-all-recorded-steps",!Game.IsSolutionPlaybackRunning && Game.AppliedSolutionSteps==Game.SolutionStepCount,"Applied="+Game.AppliedSolutionSteps+"/"+Game.SolutionStepCount+",phase="+Game.NativeSolutionPhase+",wait="+(playback==null?-1:playback.WaitPhase)+",status="+Game.Status);
                Require("gm"+id+"-three-native-phases",sawMotion && phases.Contains(1) && phases.Contains(2) && phases.Contains(3),"Observed phases="+String.Join(",",phases.Select(x=>x.ToString()).ToArray())+",highestY="+highest+",strictWaitSeen="+sawWait);
                FlowWaterValidation.SaveVisibleFrame(Path.Combine(Output,"gm"+id+"-animated-preview.png"));
                Click("doSolucionLevel"); for(int f=0;f<3;f++) yield return null;
                Require("gm"+id+"-automatic-exit-restores-board",!Game.IsSolutionPreview && Same(before),"Original board, mechanisms and water snapshot restored after all animated steps.");
            }
        }
        static void Click(string method)
        {
            var control=UI.OriginalControls.FirstOrDefault(x=>x.method==method && x.source!=null && x.source.activeInHierarchy && (x.button==null || x.button.interactable));
            Require("original-control-"+method,control!=null,"Actual visible original Button/EventTrigger must be interactable.");
            if(control.button!=null) control.button.onClick.Invoke();
            else control.trigger.triggers.First(x=>x.eventID==control.eventType).callback.Invoke(new PointerEventData(EventSystem.current));
        }
        static Vector3[] Positions() { return Game.Pieces.Select(x=>x.transform.position).ToArray(); }
        static bool Same(Vector3[] values) { return values.Length==Game.PieceCount && Game.Pieces.Select((x,i)=>(x.transform.position-values[i]).sqrMagnitude<.0001f).All(x=>x); }
        static void Require(string name,bool passed,string detail)
        {
            report.checks.Add(new Check{name=name,passed=passed,detail=detail});
            if(!passed) throw new InvalidOperationException(name+": "+detail);
        }
        static bool Restore()
        {
            string json=SessionState.GetString(Key+".backup",""); if(String.IsNullOrEmpty(json)) return false;
            Backup saved=JsonUtility.FromJson<Backup>(json);
            foreach(Pref item in saved.prefs) if(item.existed) PlayerPrefs.SetInt(item.key,item.value); else PlayerPrefs.DeleteKey(item.key);
            Time.timeScale=saved.scale; PlayerPrefs.Save();
            return saved.prefs.All(x=>PlayerPrefs.HasKey(x.key)==x.existed && (!x.existed || PlayerPrefs.GetInt(x.key)==x.value));
        }
        static void OnLog(string message,string stack,LogType type)
        { if(report!=null && (type==LogType.Error || type==LogType.Exception || type==LogType.Assert)) report.runtimeErrors.Add(message+"\n"+stack); }
        static void OnPlayMode(PlayModeStateChange change)
        {
            if(change==PlayModeStateChange.ExitingPlayMode) Unlock();
            if(change!=PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Key,false)) return;
            Restore(); Backup saved=JsonUtility.FromJson<Backup>(SessionState.GetString(Key+".backup", ""));
            if(saved.scenes!=null && saved.scenes.Length>0) EditorSceneManager.RestoreSceneManagerSetup(saved.scenes);
            SessionState.EraseBool(Key);
        }
        static void Unlock() { if(!locked) return; locked=false; EditorApplication.UnlockReloadAssemblies(); AssetDatabase.AllowAutoRefresh(); }
        static void Finish()
        {
            if(report==null) report=new Report();
            if(Game!=null && Game.IsSolutionPreview) Game.ExitSolutionPreview();
            report.preferencesRestored=Restore(); report.finishedUtc=DateTime.UtcNow.ToString("o");
            report.passed=report.preferencesRestored && report.checks.Count>0 && report.checks.All(x=>x.passed) && report.runtimeErrors.Count==0;
            Directory.CreateDirectory(Output); File.WriteAllText(Path.Combine(Output,"native-step-playback-validation.json"),JsonUtility.ToJson(report,true));
            bool passed=report.passed; Debug.Log("Native animated step preview: "+(passed?"PASS":"FAIL")+" checks="+report.checks.Count);
            report=null;routine=null; Unlock();
            if(Application.isBatchMode) { SessionState.EraseBool(Key); EditorApplication.Exit(passed?0:1); }
            else EditorApplication.ExitPlaymode();
        }
    }
}
#endif
