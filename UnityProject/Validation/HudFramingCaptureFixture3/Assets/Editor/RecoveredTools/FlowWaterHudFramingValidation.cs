#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Recovered.FlowWater.Editor
{
    // This fixture only loads real game/solution layouts and captures their
    // authored UI. No physics, native camera, actor or time-speed mutation.
    [InitializeOnLoad]
    public static class FlowWaterHudFramingValidation
    {
        const string Key="FlowWater.HudFramingValidation";
        [Serializable] public sealed class Check { public string name,detail; public bool passed; }
        [Serializable] public sealed class Region { public string path; public Rect viewport; }
        [Serializable] public sealed class Frame {
            public int level,width,height,structuralMeshes,structuralVertices,hudOverlapMeshes,outsideVertices;
            public string stage,screenshot;
            public List<string> captureTrace=new List<string>();
            public float backwardDistance,fov,nearClip,farClip;
            public Vector3 cameraPosition;
            public Quaternion cameraRotation;
            public List<Region> protectedHud=new List<Region>();
            public List<string> overlaps=new List<string>();
        }
        [Serializable] public sealed class Report {
            public string startedUtc,finishedUtc,unityVersion,runtimeSourceSha256;
            public bool passed,preferencesRestored,enteredEditMode,timeScaleRestored;
            public float originalTimeScale,restoredTimeScale;
            public List<Check> checks=new List<Check>();
            public List<Frame> frames=new List<Frame>();
            public List<string> errors=new List<string>();
        }
        sealed class CameraState { public Camera camera;public RenderTexture target;public float aspect;public int mask; }
        sealed class CanvasState { public Canvas canvas; public RenderMode mode;public Camera camera;public float distance,scale; }
        static Report report;
        static IEnumerator routine;
        static bool busy,locked;
        static double deadline;
        static int resumeFrame;
        static CaptureScope capture;
        static readonly Vector2Int[] Sizes={new Vector2Int(1280,720),new Vector2Int(885,630),new Vector2Int(2560,1080)};
        static string Output { get {return Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Validation","HudFraming");} }
        static FlowWaterHudFramingValidation() {
            EditorApplication.update+=Update;
            EditorApplication.playModeStateChanged+=state=>{
                if(state==PlayModeStateChange.ExitingPlayMode && SessionState.GetBool(Key,false)){DisposeCapture();Unlock();}
                if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key+".Finished",false)) {
                    float saved=SessionState.GetFloat(Key+".Scale",1);Time.timeScale=saved;
                    EditorApplication.delayCall+=()=>CompleteInEdit(saved);
                }
            };
            Application.logMessageReceived+=(message,stack,type)=>{
                if(SessionState.GetBool(Key,false) && report!=null && (type==LogType.Error || type==LogType.Exception || type==LogType.Assert))report.errors.Add(message+"\n"+stack);
            };
        }
        [MenuItem("Tools/Flow Water/Validate Original HUD Board Framing")]
        public static void Run() {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start HUD framing fixture from Edit mode.");
            BackupPrefs();SessionState.SetFloat(Key+".Scale",Time.timeScale);SessionState.EraseBool(Key+".Finished");SessionState.SetBool(Key,true);
            EditorSceneManager.OpenScene("Assets/Scenes/Portada.unity",OpenSceneMode.Single);EditorApplication.EnterPlaymode();
        }
        static void Update() {
            if(busy || !SessionState.GetBool(Key,false) || SessionState.GetBool(Key+".Finished",false) || !EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            busy=true;
            try {
                if(report==null){
                    AssetDatabase.DisallowAutoRefresh();EditorApplication.LockReloadAssemblies();locked=true;
                    report=new Report{startedUtc=DateTime.UtcNow.ToString("o"),unityVersion=Application.unityVersion,
                        runtimeSourceSha256=Sha(Path.Combine(Application.dataPath,"Scripts/RecoveryUI/RecoveredBoardFraming.cs")),originalTimeScale=SessionState.GetFloat(Key+".Scale",Time.timeScale)};
                    deadline=EditorApplication.timeSinceStartup+300;routine=Exercise();
                }
                if(Time.frameCount<resumeFrame)return;
                if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("HUD framing fixture 300second deadline");
                if(!routine.MoveNext())Finish();
                else resumeFrame=Time.frameCount+1;
            } catch(Exception error){if(report==null)report=new Report();report.errors.Add(error.ToString());Finish();}
            finally{busy=false;}
        }
        static IEnumerator Exercise() {
            CheckState("unity-2022.3.28f1",Application.unityVersion=="2022.3.28f1",Application.unityVersion);
            while(FlowWaterRecoveryUI.Instance==null)yield return null;
            foreach(int level in new[]{366,409,421,401}) {
                PlayerPrefs.SetInt("helpRotation",1);
                FlowWaterRecoveryUI.Instance.StartGame(level);
                while(RecoveredFlowGame.Instance==null || !RecoveredFlowGame.Instance.IsLevelActive || RecoveredFlowGame.Instance.CurrentLevelId!=level)yield return null;
                for(int i=0;i<16;i++)yield return null;
                // Observe the actual native Start result before a capture wrapper,
                // Solve or any explicit UI refresh can overwrite this title.
                ObserveInitialLevelIdentity(level);
                RecoveredFlowGame game=RecoveredFlowGame.Instance;Camera camera=game.GameCamera;
                float fov=camera.fieldOfView,near=camera.nearClipPlane,far=camera.farClipPlane;Quaternion rotation=camera.transform.rotation;
                for(int stage=0;stage<2;stage++) {
                    if(stage==1){game.Solve();for(int i=0;i<20;i++)yield return null;}
                    float wideDistance=-1;
                    foreach(Vector2Int size in Sizes){
                        capture=new CaptureScope(size.x,size.y);
                        // Let the actual CanvasScaler/render-target dimensions
                        // settle for two real player frames before measuring.
                        yield return null;yield return null;
                        Frame frame=null;
                        FlowWaterRecoveryUI.Instance.CaptureBoardFrame(()=>frame=Capture(game,size,stage==0?"initial":"solution",fov,near,far,rotation,true));
                        if(size.x==1280)wideDistance=frame.backwardDistance;
                        DisposeCapture();yield return null;
                    }
                    capture=new CaptureScope(1280,720);yield return null;yield return null;
                    float returned=-1;
                    FlowWaterRecoveryUI.Instance.CaptureBoardFrame(()=>{
                        Canvas.ForceUpdateCanvases();camera.GetComponent<RecoveredBoardFraming>().FitNow();returned=camera.GetComponent<RecoveredBoardFraming>().LastBackwardDistance;
                    });
                    CheckState(level+":"+stage+":resize-returns-to-wide-fit",Mathf.Abs(returned-wideDistance)<.025f,"first="+wideDistance+", returned="+returned);
                    DisposeCapture();yield return null;
                }
            }
            CheckState("all-24-real-layout-and-aspect-frames",report.frames.Count==24,"actual="+report.frames.Count);
            CheckState("fixture-does-not-change-time-speed",Mathf.Abs(Time.timeScale-report.originalTimeScale)<.00001f,"original="+report.originalTimeScale+", actual="+Time.timeScale);
        }
        static void ObserveInitialLevelIdentity(int level) {
            int expected=(level-1)%50+1;
            Leveltexto2[] titles=OriginalScenePresentation.Components<Leveltexto2>(SceneManager.GetActiveScene())
                .Where(x=>x.isActiveAndEnabled && x.texto1!=null && x.texto1.isActiveAndEnabled).ToArray();
            CheckState(level+":initial-before-capture:active-original-level-title",titles.Length>0,"active original Leveltexto2 title count="+titles.Length);
            CheckState(level+":initial-before-capture:native-global-asset-identity",ControladorJuego.nivelActual==level,"global="+ControladorJuego.nivelActual+", assetID="+level);
            foreach(Leveltexto2 title in titles) {
                string text=title.texto1.text??"";MatchCollection matches=Regex.Matches(text,@"[+-]?\d+");int number;
                bool exact=matches.Count==1 && Int32.TryParse(matches[0].Value,out number) && number==expected && number>0;
                CheckState(level+":initial-before-capture:original-positive-local-level-number",exact,
                    "path="+Hierarchy(title.texto1.transform)+", actual='"+text+"', expected="+expected+", observedFrame="+Time.frameCount);
            }
        }
        static Frame Capture(RecoveredFlowGame game,Vector2Int size,string stage,float fov,float near,float far,Quaternion rotation,bool save) {
            Camera camera=game.GameCamera;Canvas.ForceUpdateCanvases();
            var framing=camera.GetComponent<RecoveredBoardFraming>();framing.FitNow();
            var trace=new List<string>{capture.Describe("after-fit")};
            capture.Render(trace);
            var frame=new Frame{level=game.CurrentLevelId,width=size.x,height=size.y,stage=stage,backwardDistance=framing.LastBackwardDistance,fov=camera.fieldOfView,
                nearClip=camera.nearClipPlane,farClip=camera.farClipPlane,cameraPosition=camera.transform.position,cameraRotation=camera.transform.rotation,captureTrace=trace};
            var regions=new List<RecoveredBoardFraming.HudRegion>();framing.GetProtectedHudRegions(regions);
            frame.protectedHud=regions.Select(x=>new Region{path=x.path,viewport=x.viewport}).ToList();
            string tag=frame.level+":"+stage+":"+size.x+"x"+size.y;
            CheckState(tag+":actual-original-HUD-rectangles",regions.Count>=3 && regions.All(x=>x.viewport.yMin>.5f && x.viewport.height<.4f && x.viewport.width<.95f),String.Join(";",regions.Select(x=>x.path).ToArray()));
            CheckState(tag+":original-projection-and-rotation-retained",Mathf.Abs(camera.fieldOfView-fov)<.0001f && Mathf.Abs(camera.nearClipPlane-near)<.0001f && Mathf.Abs(camera.farClipPlane-far)<.0001f && Mathf.Abs(Quaternion.Dot(camera.transform.rotation,rotation))>.999999f,
                "FOV="+camera.fieldOfView+",near="+camera.nearClipPlane+",far="+camera.farClipPlane+",rotation="+camera.transform.rotation);
            foreach(Renderer renderer in Structural(game)) {
                MeshFilter filter=renderer.GetComponent<MeshFilter>();Mesh mesh=filter.sharedMesh;frame.structuralMeshes++;
                foreach(Vector3 v in mesh.vertices){Vector3 p=camera.WorldToViewportPoint(filter.transform.TransformPoint(v));frame.structuralVertices++;if(p.x<-.00001f || p.x>1.00001f || p.y<-.00001f || p.y>1.00001f || p.z<camera.nearClipPlane-.001f || p.z>camera.farClipPlane+.001f)frame.outsideVertices++;}
                Vector3[] corners=Corners(mesh.bounds).Select(x=>camera.WorldToViewportPoint(filter.transform.TransformPoint(x))).ToArray();
                Rect projected=Rect.MinMaxRect(corners.Min(x=>x.x),corners.Min(x=>x.y),corners.Max(x=>x.x),corners.Max(x=>x.y));
                if(regions.Any(x=>projected.xMin<x.viewport.xMax-.000001f && projected.xMax>x.viewport.xMin+.000001f && projected.yMin<x.viewport.yMax-.000001f && projected.yMax>x.viewport.yMin+.000001f)) {
                    frame.hudOverlapMeshes++;if(frame.overlaps.Count<12)frame.overlaps.Add(Hierarchy(renderer.transform));
                }
            }
            CheckState(tag+":all-structural-vertices-in-camera",frame.structuralVertices>0 && frame.outsideVertices==0,"vertices="+frame.structuralVertices+", outside="+frame.outsideVertices);
            CheckState(tag+":structural-meshes-clear-original-HUD",frame.structuralMeshes>0 && frame.hudOverlapMeshes==0,"meshes="+frame.structuralMeshes+", overlaps="+frame.hudOverlapMeshes+" "+String.Join(";",frame.overlaps.ToArray()));
            if(frame.level==401) {
                bool needed=BaseNeedsFit(game,camera,regions,frame.backwardDistance);
                CheckState(tag+":clear-small-level-no-unneeded-retreat",frame.backwardDistance<.025f || needed,"actual="+frame.backwardDistance+", original base needs viewport margin/HUD fit="+needed);
            }
            if(save){Directory.CreateDirectory(Output);frame.screenshot=Path.Combine(Output,"gm"+frame.level+"-"+stage+"-"+size.x+"x"+size.y+".png");capture.Save(frame.screenshot);report.frames.Add(frame);}
            return frame;
        }
        static bool BaseNeedsFit(RecoveredFlowGame game,Camera camera,List<RecoveredBoardFraming.HudRegion> regions,float backward) {
            float tanY=Mathf.Tan(camera.fieldOfView*.5f*Mathf.Deg2Rad),tanX=tanY*camera.aspect;
            foreach(Renderer renderer in Structural(game)) {
                MeshFilter filter=renderer.GetComponent<MeshFilter>();var points=new List<Vector2>();
                foreach(Vector3 corner in Corners(filter.sharedMesh.bounds)) {
                    Vector3 p=camera.transform.InverseTransformPoint(filter.transform.TransformPoint(corner));p.z-=backward;
                    if(p.z<=camera.nearClipPlane)return true;
                    Vector2 v=new Vector2(.5f+p.x/(2*tanX*p.z),.5f+p.y/(2*tanY*p.z));points.Add(v);
                    if(v.x<.015f || v.x>.985f || v.y<.015f || v.y>.985f)return true;
                }
                Rect bounds=Rect.MinMaxRect(points.Min(x=>x.x),points.Min(x=>x.y),points.Max(x=>x.x),points.Max(x=>x.y));
                if(regions.Any(x=>bounds.Overlaps(x.viewport)))return true;
            }
            return false;
        }
        static IEnumerable<Renderer> Structural(RecoveredFlowGame game) {
            var all=new List<Renderer>(game.PieceRoot.GetComponentsInChildren<Renderer>(true));
            foreach(ConfigSuelo floor in OriginalScenePresentation.Components<ConfigSuelo>(SceneManager.GetActiveScene())) {
                Renderer renderer=floor.GetComponent<Renderer>();if(renderer!=null)all.Add(renderer);
                if(floor.tierra!=null)all.AddRange(floor.tierra.GetComponentsInChildren<Renderer>(true));
            }
            foreach(GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())if(root.name=="Recovered original underground border")all.AddRange(root.GetComponentsInChildren<Renderer>(true));
            return all.Distinct().Where(RecoveredBoardFraming.IsStructuralRenderer);
        }
        sealed class CaptureScope : IDisposable {
            readonly List<CameraState> cameras=new List<CameraState>();readonly List<CanvasState> canvases=new List<CanvasState>();
            readonly Dictionary<GameObject,int> layers=new Dictionary<GameObject,int>();
            readonly RenderTexture target;readonly Camera uiCamera;readonly GameObject uiObject;readonly int layer;
            public CaptureScope(int width,int height){
                target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);target.Create();
                foreach(Camera camera in UnityEngine.Object.FindObjectsOfType<Camera>().Where(x=>x.isActiveAndEnabled && x.cameraType==CameraType.Game).OrderBy(x=>x.depth)){
                    cameras.Add(new CameraState{camera=camera,target=camera.targetTexture,aspect=camera.aspect,mask=camera.cullingMask});camera.targetTexture=target;camera.aspect=width/(float)height;
                }
                var occupied=new HashSet<int>(UnityEngine.Object.FindObjectsOfType<Transform>().Select(x=>x.gameObject.layer));layer=Enumerable.Range(8,24).Reverse().First(x=>!occupied.Contains(x));
                uiObject=new GameObject("HUD fixture capture UI camera",typeof(Camera));uiObject.hideFlags=HideFlags.HideAndDontSave;uiCamera=uiObject.GetComponent<Camera>();uiCamera.enabled=false;
                uiCamera.targetTexture=target;uiCamera.rect=new Rect(0,0,1,1);uiCamera.aspect=width/(float)height;uiCamera.clearFlags=CameraClearFlags.Depth;uiCamera.orthographic=true;uiCamera.orthographicSize=height*.5f;uiCamera.nearClipPlane=.1f;uiCamera.farClipPlane=1000;uiCamera.cullingMask=1<<layer;
                foreach(Canvas canvas in UnityEngine.Object.FindObjectsOfType<Canvas>()){
                    if(!canvas.isActiveAndEnabled || !canvas.isRootCanvas || canvas.renderMode!=RenderMode.ScreenSpaceOverlay)continue;
                    canvases.Add(new CanvasState{canvas=canvas,mode=canvas.renderMode,camera=canvas.worldCamera,distance=canvas.planeDistance,scale=canvas.scaleFactor});
                    canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=uiCamera;canvas.planeDistance=1;
                    foreach(Transform child in canvas.GetComponentsInChildren<Transform>(true)){if(!layers.ContainsKey(child.gameObject))layers.Add(child.gameObject,child.gameObject.layer);child.gameObject.layer=layer;}
                }
                foreach(CameraState state in cameras)state.camera.cullingMask&=~(1<<layer);
                Canvas.ForceUpdateCanvases();MethodInfo handle=typeof(CanvasScaler).GetMethod("Handle",BindingFlags.Instance|BindingFlags.NonPublic);
                foreach(CanvasScaler scaler in UnityEngine.Object.FindObjectsOfType<CanvasScaler>())if(scaler.isActiveAndEnabled && handle!=null)handle.Invoke(scaler,null);
                Canvas.ForceUpdateCanvases();
            }
            public string Describe(string stage){return stage+" frame="+Time.frameCount+" | "+String.Join(" | ",cameras.Select(x=>x.camera.name+" pos="+x.camera.transform.position.ToString("F4")+" rect="+x.camera.rect+" pixel="+x.camera.pixelRect+" aspect="+x.camera.aspect+" backward="+(x.camera.GetComponent<RecoveredBoardFraming>()==null?-1:x.camera.GetComponent<RecoveredBoardFraming>().LastBackwardDistance)).ToArray())+" | UI rect="+uiCamera.rect+" pixel="+uiCamera.pixelRect+" aspect="+uiCamera.aspect+" | "+String.Join(" | ",canvases.Select(x=>x.canvas.name+" pixel="+x.canvas.pixelRect+" rect="+((RectTransform)x.canvas.transform).rect+" scale="+x.canvas.scaleFactor+" camera="+(x.canvas.worldCamera==null?"null":x.canvas.worldCamera.name)).ToArray());}
            public void Render(List<string> trace){RenderTexture previous=RenderTexture.active;try{RenderTexture.active=target;GL.Clear(true,true,cameras.Count>0?cameras[0].camera.backgroundColor:Color.black);foreach(CameraState state in cameras){state.camera.Render();trace.Add(Describe("after-source-render"));}uiCamera.Render();trace.Add(Describe("after-ui-render"));}finally{RenderTexture.active=previous;}}
            public void Save(string path){RenderTexture previous=RenderTexture.active;Texture2D pixels=null;try{RenderTexture.active=target;pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0,false);pixels.Apply(false,false);File.WriteAllBytes(path,pixels.EncodeToPNG());}finally{RenderTexture.active=previous;if(pixels!=null)UnityEngine.Object.DestroyImmediate(pixels);}}
            public void Dispose(){foreach(var item in layers)if(item.Key!=null)item.Key.layer=item.Value;foreach(CanvasState state in canvases)if(state.canvas!=null){state.canvas.renderMode=state.mode;state.canvas.worldCamera=state.camera;state.canvas.planeDistance=state.distance;state.canvas.scaleFactor=state.scale;}foreach(CameraState state in cameras)if(state.camera!=null){state.camera.targetTexture=state.target;state.camera.aspect=state.aspect;state.camera.cullingMask=state.mask;}if(uiObject!=null)UnityEngine.Object.DestroyImmediate(uiObject);target.Release();UnityEngine.Object.DestroyImmediate(target);Canvas.ForceUpdateCanvases();}
        }
        static IEnumerable<Vector3> Corners(Bounds b){for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)yield return b.center+Vector3.Scale(b.extents,new Vector3(x,y,z));}
        static string Hierarchy(Transform t){string p=t.name;while(t.parent!=null){t=t.parent;p=t.name+"/"+p;}return p;}
        static string Sha(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        static void CheckState(string name,bool passed,string detail){report.checks.Add(new Check{name=name,passed=passed,detail=detail});}
        static void BackupPrefs(){
            var backup=new FlowWaterUIValidation.Preferences();
            foreach(string name in new[]{"Recovery.LastLevelId","esTablet","OptionSelLev","Soundlevel","MusicaON","primeraVez","helpRotation"})backup.entries.Add(new FlowWaterUIValidation.Preference{key=name,existed=PlayerPrefs.HasKey(name),integer=PlayerPrefs.GetInt(name)});
            foreach(string name in new[]{"Conf_music","Conf_sound"})backup.entries.Add(new FlowWaterUIValidation.Preference{key=name,existed=PlayerPrefs.HasKey(name),floating=true,number=PlayerPrefs.GetFloat(name)});
            foreach(int level in new[]{366,409,421,401})foreach(string prefix in new[]{"Recovery.Complete.","Recovery.Completed.gm"}){string name=prefix+level;backup.entries.Add(new FlowWaterUIValidation.Preference{key=name,existed=PlayerPrefs.HasKey(name),integer=PlayerPrefs.GetInt(name)});}
            SessionState.SetString(Key+".Prefs",JsonUtility.ToJson(backup));SessionState.SetString(Key+".Language",PlayerPrefs.GetString("language",""));SessionState.SetBool(Key+".LanguageExists",PlayerPrefs.HasKey("language"));
        }
        static bool RestorePrefs(){bool exact=true;var backup=JsonUtility.FromJson<FlowWaterUIValidation.Preferences>(SessionState.GetString(Key+".Prefs","{}"));foreach(var item in backup.entries){if(!item.existed)PlayerPrefs.DeleteKey(item.key);else if(item.floating)PlayerPrefs.SetFloat(item.key,item.number);else PlayerPrefs.SetInt(item.key,item.integer);exact&=PlayerPrefs.HasKey(item.key)==item.existed && (!item.existed || (item.floating?PlayerPrefs.GetFloat(item.key)==item.number:PlayerPrefs.GetInt(item.key)==item.integer));}if(SessionState.GetBool(Key+".LanguageExists",false))PlayerPrefs.SetString("language",SessionState.GetString(Key+".Language",""));else PlayerPrefs.DeleteKey("language");PlayerPrefs.Save();return exact;}
        static void DisposeCapture(){if(capture!=null){capture.Dispose();capture=null;}}
        static void Unlock(){if(locked){locked=false;EditorApplication.UnlockReloadAssemblies();AssetDatabase.AllowAutoRefresh();}}
        static void Finish(){DisposeCapture();report.preferencesRestored=RestorePrefs();report.finishedUtc=DateTime.UtcNow.ToString("o");Save();SessionState.SetBool(Key+".Finished",true);Unlock();EditorApplication.ExitPlaymode();}
        static void CompleteInEdit(float saved){if(!SessionState.GetBool(Key+".Finished",false))return;Time.timeScale=saved;string path=Path.Combine(Output,"hud-framing-validation.json");Report finished=report??JsonUtility.FromJson<Report>(File.ReadAllText(path));finished.enteredEditMode=true;finished.restoredTimeScale=Time.timeScale;finished.timeScaleRestored=Mathf.Abs(Time.timeScale-saved)<.00001f;finished.passed=finished.preferencesRestored && finished.timeScaleRestored && finished.errors.Count==0 && finished.frames.Count==24 && finished.checks.All(x=>x.passed);File.WriteAllText(path,JsonUtility.ToJson(finished,true));SessionState.EraseBool(Key);SessionState.EraseBool(Key+".Finished");bool passed=finished.passed;report=null;routine=null;EditorApplication.delayCall+=()=>{Time.timeScale=saved;if(Application.isBatchMode)EditorApplication.Exit(passed?0:1);};}
        static void Save(){Directory.CreateDirectory(Output);report.passed=false;File.WriteAllText(Path.Combine(Output,"hud-framing-validation.json"),JsonUtility.ToJson(report,true));}
    }
}
#endif
