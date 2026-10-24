#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace Recovered.FlowWater.Editor
{
    // An observer of the existing real-scene FullValidation path. It never
    // changes time, physics, controller fields or generated transforms.
    [InitializeOnLoad]
    public static class FlowWaterNativeGeometryStackValidation
    {
        const string Key="FlowWater.NativeGeometryStackPoseFixture";
        [Serializable] public sealed class Check { public string name,detail; public bool passed; }
        [Serializable] public sealed class Pose
        {
            public int level,record;
            public string instance,prefab;
            public Vector3 position,scale,worldPosition;
            public Quaternion rotation;
            public int childColliders,childRigidbodies,childScripts;
            public Vector3[] meshCenters;
        }
        [Serializable] public sealed class Report
        {
            public string startedUtc,finishedUtc,unityVersion,catalogSha256;
            public bool passed;
            public List<Check> checks=new List<Check>();
            public List<Pose> samples=new List<Pose>();
            public List<string> errors=new List<string>();
        }
        [Serializable] sealed class Request { public int[] ids; }
        [Serializable] sealed class OriginalLayout { public OriginalData data; }
        [Serializable] sealed class OriginalData { public string[] pieza,fijo; }
        static Report report;
        static readonly HashSet<int> observed=new HashSet<int>();
        static int waitingLevel=-1,loadedFrame;
        static string Output { get {return Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Validation","NativeGeometryPose");} }
        static FlowWaterNativeGeometryStackValidation()
        {
            EditorApplication.update+=Update;
            EditorApplication.playModeStateChanged+=state=>{
                if(!SessionState.GetBool(Key,false))return;
                if(state==PlayModeStateChange.ExitingPlayMode){
                    if(report!=null){
                        CheckState("all-four-real-layouts-observed",new[]{1095,610,626,634}.All(x=>observed.Contains(x)),String.Join(",",observed.Select(x=>x.ToString()).ToArray()));
                        report.finishedUtc=DateTime.UtcNow.ToString("o");Save();
                    }
                }
                if(state==PlayModeStateChange.EnteredEditMode)SessionState.EraseBool(Key);
            };
            Application.logMessageReceived+=(message,stack,type)=>{
                if(SessionState.GetBool(Key,false) && report!=null && (type==LogType.Error || type==LogType.Exception || type==LogType.Assert))report.errors.Add(message+"\n"+stack);
            };
        }
        [MenuItem("Tools/Flow Water/Validate Native Geometry Stack Poses")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start pose fixture from Edit mode.");
            string path=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Validation","VisualReview","detail-request.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(new Request{ids=new[]{1095,610,626,634}}));
            SessionState.SetBool(Key,true);FlowWaterFullValidation.RunVisualDetails();
        }
        static void Update()
        {
            if(!SessionState.GetBool(Key,false) || !EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            try{
                if(report==null){
                    using(var sha=SHA256.Create())report=new Report{startedUtc=DateTime.UtcNow.ToString("o"),unityVersion=Application.unityVersion,
                        catalogSha256=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(Application.dataPath,"Resources/RecoveredNativeGeometry.json")))).Replace("-","").ToLowerInvariant()};
                    CheckState("real-installed2022.3.28f1",Application.unityVersion=="2022.3.28f1",Application.unityVersion);
                    CheckState("exact-fixed-native-catalog",report.catalogSha256=="2a5cb9cfc4b1941f767d0058c54295dca34bdc507b5b8746439158bb41fe5361",report.catalogSha256);
                }
                RecoveredFlowGame game=RecoveredFlowGame.Instance;
                if(game==null || !game.IsLevelActive || game.IsSolutionPreview || game.PieceRoot==null)return;
                int level=game.CurrentLevelId;if(observed.Contains(level))return;
                if(waitingLevel!=level){waitingLevel=level;loadedFrame=Time.frameCount;return;}
                if(Time.frameCount-loadedFrame<3)return;
                if(level==1095){
                    Observe(game,29,"obj5",new Vector3(1,-.38f,.2f),new Vector3(50,50,38),new Quaternion(.5f,.5f,.5f,-.5f));
                    Observe(game,29,"obj6",new Vector3(1,-.38f,-.2f),new Vector3(50,50,38),new Quaternion(.5f,.5f,.5f,-.5f));
                    Camera camera=Camera.main;var framing=camera==null?null:camera.GetComponent<RecoveredBoardFraming>();
                    CheckState("1095-no-outlier-driven1476-backward",framing!=null && framing.LastBackwardDistance<200,"actual backward="+(framing==null?-1:framing.LastBackwardDistance));
                    CheckState("1095-authored-far-clip-unchanged",camera!=null && Mathf.Abs(camera.farClipPlane-1000)<.001f,"No far-clip expansion masks wrong geometry");
                }
                else if(level==610)Observe(game,21,"obj7",null,new Vector3(.07f,.1f,.1f),null);
                else if(level==626){Observe(game,2,"obj7",null,new Vector3(.1f,.1f,.07f),null);Observe(game,24,"obj7",null,new Vector3(.07f,.1f,.1f),null);}
                else if(level==634){Observe(game,0,"obj7",null,new Vector3(.1f,.1f,.1f),null);ObserveInitialProxyAbsent(game,35);}
                else return;
                observed.Add(level);
                if(new[]{1095,610,626,634}.All(x=>observed.Contains(x)) && String.IsNullOrEmpty(report.finishedUtc)) {
                    CheckState("all-four-real-layouts-observed",true,"1095,610,626,634 original production layouts observed");
                    report.finishedUtc=DateTime.UtcNow.ToString("o");
                }
                Save();
            }catch(Exception error){report.errors.Add(error.ToString());observed.Add(waitingLevel);Save();}
        }
        static void ObserveInitialProxyAbsent(RecoveredFlowGame game,int record)
        {
            RecoveryPiece piece=game.PieceRoot.GetComponentsInChildren<RecoveryPiece>(true).FirstOrDefault(x=>x.SourceRecordIndex==record);
            OriginalLayout original=JsonUtility.FromJson<OriginalLayout>(File.ReadAllText(Path.Combine(Application.dataPath,"RecoveredData/LevelData/01_Layouts/gm634.json")));
            bool proxy=original!=null && original.data!=null && original.data.pieza.Length>record && original.data.fijo.Length>record &&
                original.data.pieza[record]=="CuFuenteEB5Col1" && original.data.fijo[record]=="Ist9etsz8ezsr2100305162ersh0000eh";
            CheckState("634:35:original-st9-preview-proxy-identity",proxy,"Source gm634 layout record35 CuFuenteEB5Col1 Ist9; mode0 legitimately does not instantiate this RecoveryPiece");
            ControlPiezaC source=piece==null?null:piece.GetComponent<ControlPiezaC>();
            GameObject instance=source==null?null:source.obj7;
            CheckState("634:35:native-mode0-does-not-create-preview-Palo",!game.IsSolutionPreview && instance==null,
                "Mode0 initial st9 final-preview proxy/obj7 absent; active st1 record0 above is separately positively tested; runtimeRecordPresent="+(piece!=null));
        }
        static void Observe(RecoveredFlowGame game,int record,string field,Vector3? expectedPosition,Vector3 expectedScale,Quaternion? expectedRotation)
        {
            RecoveryPiece piece=game.PieceRoot.GetComponentsInChildren<RecoveryPiece>().FirstOrDefault(x=>x.SourceRecordIndex==record);
            ControlPiezaC source=piece==null?null:piece.GetComponent<ControlPiezaC>();
            GameObject instance=source==null?null:typeof(ControlPiezaC).GetField(field).GetValue(source) as GameObject;
            string name=game.CurrentLevelId+":"+record+":"+field;
            CheckState(name+":production-instance",instance!=null,"Real configured source field after original game load");if(instance==null)return;
            Transform tr=instance.transform;Renderer[] meshes=instance.GetComponentsInChildren<MeshRenderer>(true);
            var pose=new Pose{level=game.CurrentLevelId,record=record,instance=field,prefab=instance.name,position=tr.localPosition,scale=tr.localScale,
                rotation=tr.localRotation,worldPosition=tr.position,childColliders=instance.GetComponentsInChildren<Collider>(true).Length,
                childRigidbodies=instance.GetComponentsInChildren<Rigidbody>(true).Length,childScripts=instance.GetComponentsInChildren<MonoBehaviour>(true).Length,
                meshCenters=meshes.Select(x=>x.bounds.center).ToArray()};report.samples.Add(pose);
            CheckState(name+":exact-native-local-scale",(tr.localScale-expectedScale).sqrMagnitude<.000001f,"actual="+tr.localScale+",native="+expectedScale);
            CheckState(name+":tree-has-no-physics-components",pose.childColliders==0 && pose.childRigidbodies==0,"Colliders="+pose.childColliders+",bodies="+pose.childRigidbodies);
            if(expectedPosition.HasValue)CheckState(name+":exact-native-local-position",(tr.localPosition-expectedPosition.Value).sqrMagnitude<.000001f,"actual="+tr.localPosition+",native="+expectedPosition);
            if(expectedRotation.HasValue)CheckState(name+":exact-native-local-rotation",Quaternion.Angle(tr.localRotation,expectedRotation.Value)<.1f,"actual="+tr.localRotation+",native="+expectedRotation);
            if(instance.name.StartsWith("Chorros4",StringComparison.Ordinal)){
                CheckState(name+":both-original-pitorro-meshes",meshes.Length==2 && meshes.All(x=>x.name=="pitorro"),"Original two mesh children");
                CheckState(name+":pitorros-remain-near-owner",meshes.All(x=>Vector3.Distance(x.bounds.center,piece.transform.position)<25),"Centers="+String.Join(";",pose.meshCenters.Select(x=>x.ToString()).ToArray()));
                CheckState(name+":no-child-script",pose.childScripts==0,"Pure original mesh prefab");
            }
        }
        static void CheckState(string name,bool passed,string detail){report.checks.Add(new Check{name=name,passed=passed,detail=detail});}
        static void Save(){if(report==null)return;report.passed=!String.IsNullOrEmpty(report.finishedUtc) && report.errors.Count==0 && report.checks.All(x=>x.passed);Directory.CreateDirectory(Output);File.WriteAllText(Path.Combine(Output,"native-geometry-pose-validation.json"),JsonUtility.ToJson(report,true));}
    }
}
#endif
