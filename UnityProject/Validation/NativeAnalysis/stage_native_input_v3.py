from pathlib import Path
import hashlib,json
p=Path(__file__).resolve().parent
base=p/'FlowWaterSave1GameplayValidation.pending-native-input-v2.cs.src'
assert hashlib.sha256(base.read_bytes()).hexdigest()=='041da6d16dbdfa24d25b3a7cf61e5bc0a40b36a8bbf930e95e85b40a010c3e12'
s=base.read_text('utf8')
s=s.replace('public bool compressionLanding, retainedOriginalGrabOffset;', 'public bool compressionLanding, retainedOriginalGrabOffset;\n            public int cameraQuarterTurns;\n            public List<string> inputDiagnostics=new List<string>();')
s=s.replace('"MusicaON", "Soundlevel", "Conf_music", "Conf_sound"', '"MusicaON", "Soundlevel", "Conf_music", "Conf_sound", "helpRotation"')
start=s.index('            Vector3 screen;\n            Require("gm"+level.id+"-native-input-original-screen-pick-')
end=s.index('            if(step.exception=="w")',start)
s=s[:start]+r'''            Vector3 screen=Vector3.zero;
            RecoveredNativeDragMotion motion=null;
            bool planned=false;
            for(int view=0;view<4;view++)
            {
                actor=Game.Pieces.FirstOrDefault(piece=>piece!=null && piece.SourceRecordIndex==step.sourceRecord && piece.CanMove && Near(piece.transform.position,step.origin));
                if(actor==null) { step.inputDiagnostics.Add("Original capture actor moved or became fixed while changing camera; no substitute capture."); break; }
                if(TryOriginalPickPoint(actor,out screen))
                {
                    NativeInput("BeginDrag",screen);
                    motion=Game.GetComponent<RecoveredNativeDragMotion>();
                    if(motion!=null && motion.IsActive)
                    {
                        Vector3 dropScreen,drop; RaycastHit hit;
                        if(step.sameCellPickup || FindOriginalDropPoint(step.destination,out dropScreen,out drop,out hit,step)) { planned=true; break; }
                        // Native camera input disallows rotating a held piece.
                        // Cancel through the production transaction first; it
                        // restores its original before-state and does not count
                        // as a successful recorded placement.
                        step.inputDiagnostics.Add("view="+view+" target occluded/unavailable; production CancelDrag before turning the original camera sphere.");
                        NativeInput("CancelDrag");
                    }
                }
                else step.inputDiagnostics.Add("view="+view+" original source closest-hit query is occluded.");
                if(view<3) foreach(object cameraWait in TurnNativeCamera(level,step)) yield return cameraWait;
            }
            Require("gm"+level.id+"-native-input-original-source-and-surface-"+step.number,planned && motion!=null && motion.IsActive,"Original pick and drop must be reachable through a real camera view. "+String.Join("; ",step.inputDiagnostics.Take(12).ToArray()));
            step.pickScreen=screen; step.pickupFrame=Time.frameCount; step.actualBefore=actor.transform.position;
            Collider body=Read(motion,"selectedCollider") as Collider;
            Require("gm"+level.id+"-native-input-selected-layer2-"+step.number,body!=null && body.gameObject.layer==2,"Native doProcess1Solution1034C00 selects the exact collider body into IgnoreRaycast before waiting/moving.");
''' +s[end:]
s=s.replace('FindOriginalDropPoint(step.destination,out screen,out landing,out surface)', 'FindOriginalDropPoint(step.destination,out screen,out landing,out surface,step)')
needle='        static bool NativeChannelsBusy()'
at=s.index(needle)
s=s[:at]+r'''        static IEnumerable TurnNativeCamera(Level level,Step step)
        {
            SwipeHandler2 swipe=UnityEngine.Object.FindObjectsOfType<SwipeHandler2>(true).FirstOrDefault(x=>x.EsferaCamera!=null && x.gameObject.scene==Game.GameCamera.gameObject.scene);
            MethodInfo tick=typeof(SwipeHandler2).GetMethod("TickRotation",BindingFlags.Instance|BindingFlags.Public);
            Require("gm"+level.id+"-native-input-camera-api-"+step.number,swipe!=null && swipe.enabled && tick!=null,"Native camera implementation must be configured and enabled. Test cannot replace it by assigning camera transforms.");
            Require("gm"+level.id+"-native-input-camera-not-holding-"+step.number,!MoveBloque.MoviendoBloque,"Original CanRotate forbids turning while holding a piece.");
            swipe.GirarDerecha(); step.cameraQuarterTurns++;
            float target=swipe.rotationFinal.y;
            double until=EditorApplication.timeSinceStartup+12;
            while(Mathf.Abs(Mathf.DeltaAngle(swipe.EsferaCamera.eulerAngles.y,target))>.25f && EditorApplication.timeSinceStartup<until) yield return null;
            Require("gm"+level.id+"-native-input-camera-quarter-turn-"+step.number+"-"+step.cameraQuarterTurns,Mathf.Abs(Mathf.DeltaAngle(swipe.EsferaCamera.eulerAngles.y,target))<=.25f,"Production GirarDerecha followed by original Update/LerpAngle; target="+target+", actual="+swipe.EsferaCamera.eulerAngles.y);
            yield return null;
        }
''' +s[at:]
s=s.replace('out Vector3 landing,out RaycastHit surface)\n        {\n            var points=', 'out Vector3 landing,out RaycastHit surface,Step diagnostic=null)\n        {\n            var points=')
old='''                    Vector3 point=collider.transform.position;
                    if(Mathf.Abs(point.x-destination.x)>4 || Mathf.Abs(point.z-destination.z)>4 || point.y>destination.y+5 || point.y<destination.y-12) continue;
                    points.Add(point); points.Add(collider.bounds.center);'''
new='''                    Vector3 point=collider.transform.position; Bounds bounds=collider.bounds;
                    bool centered=Mathf.Abs(point.x-destination.x)<=4 && Mathf.Abs(point.z-destination.z)<=4 && point.y<=destination.y+5 && point.y>=destination.y-12;
                    bool spans=bounds.min.x<=destination.x+1 && bounds.max.x>=destination.x-1 && bounds.min.z<=destination.z+1 && bounds.max.z>=destination.z-1 && bounds.min.y<=destination.y+1 && bounds.max.y>=destination.y-12;
                    if(!centered && !spans) continue;
                    points.Add(point); points.Add(bounds.center);
                    if(diagnostic!=null && diagnostic.inputDiagnostics.Count<32) diagnostic.inputDiagnostics.Add("surface-near "+ColliderPath(collider)+" layer="+collider.gameObject.layer+" tag="+collider.tag+" pos="+point.ToString("F3")+" bounds="+bounds);
                    MeshCollider mesh=collider as MeshCollider;
                    if(mesh!=null && mesh.sharedMesh!=null && mesh.sharedMesh.isReadable)
                    {
                        Vector3[] vertices=mesh.sharedMesh.vertices; int count=0;
                        for(int v=0;v<vertices.Length && count<96;v++)
                        {
                            Vector3 world=mesh.transform.TransformPoint(vertices[v]);
                            if(Mathf.Abs(world.x-destination.x)>10 || Mathf.Abs(world.z-destination.z)>10 || world.y>destination.y+5 || world.y<destination.y-12) continue;
                            points.Add(world); count++;
                        }
                    }'''
assert old in s;s=s.replace(old,new)
old='''                    if(RecoveredNativeInteraction.TryGetDropPoint(Game.GameCamera,candidate,Game,out landing,out surface) && OriginalGuideMatches(landing,destination,surface))
                    { screen=candidate; return true; }'''
new='''                    bool valid=RecoveredNativeInteraction.TryGetDropPoint(Game.GameCamera,candidate,Game,out landing,out surface);
                    if(valid && OriginalGuideMatches(landing,destination,surface)) { screen=candidate; return true; }
                    if(diagnostic!=null && diagnostic.inputDiagnostics.Count<96)
                    {
                        RaycastHit blocked;
                        string obstruction=Physics.Raycast(Game.GameCamera.ScreenPointToRay(candidate),out blocked,Mathf.Infinity,Physics.DefaultRaycastLayers,QueryTriggerInteraction.UseGlobal)?ColliderPath(blocked.collider)+" layer="+blocked.collider.gameObject.layer+" tag="+blocked.collider.tag+" at="+blocked.point.ToString("F3"):"no-hit";
                        diagnostic.inputDiagnostics.Add("ray "+candidate.ToString("F1")+" guideValid="+valid+" guide="+landing.ToString("F3")+" first="+obstruction);
                    }'''
assert old in s;s=s.replace(old,new)
needle='        const BindingFlags AllFields ='
s=s.replace(needle,'''        static string ColliderPath(Collider collider)
        {
            if(collider==null) return "none";
            var names=new List<string>(); Transform at=collider.transform;
            while(at!=null) { names.Add(at.name); at=at.parent; }
            names.Reverse(); return String.Join("/",names.ToArray());
        }
'''+needle)
s=s.replace('This proves a legal real-input player strategy, not the original automatic preview animation\'s exact phase timing.', 'A blocked view is cancelled through production CancelDrag before original SwipeHandler2.GirarDerecha turns the native EsferaCamera parent, with no direct camera transform write. Original mesh vertices supplement real surface-ray candidates. helpRotation preferences are backed up and restored. This proves a legal real-input player strategy, not the original automatic preview animation\'s exact phase timing.')
out=p/'FlowWaterSave1GameplayValidation.pending-native-input-v3.cs.src'
out.write_text(s,encoding='utf8',newline='\n')
rsp=(p/'save1-native-input-v2-compile.rsp').read_text('utf8').replace('save1-native-input-v2-check.dll','save1-native-input-v3-check.dll').replace('pending-native-input-v2.cs.src','pending-native-input-v3.cs.src')
(p/'save1-native-input-v3-compile.rsp').write_text(rsp,encoding='utf8')
m=json.loads((p/'staged-native-input-validation-v2-manifest.json').read_text('utf8'))
m.update(baseFile=str(base),baseSHA256=hashlib.sha256(base.read_bytes()).hexdigest(),sourceFile=str(out),sourceSHA256=hashlib.sha256(out.read_bytes()).hexdigest())
m['changesV3']=['Before real w/motion, require a camera view where original source pick and real target surface are both reachable; if occluded production CancelDrag restores BeforeState, then original GirarDerecha turns actual EsferaCamera naturally, up to four views','Never turns while holding or assigns camera/actor transforms; requires new CameraTutorialWork native SwipeHandler2 enabled by production configuration','Readable original MeshCollider vertices add real ray candidates; diagnostics capture nearby collider paths/layers/tags/bounds and first ray obstruction','Back up/restore native helpRotation preference used by camera tutorial']
m['runtimeDependency']='Native CameraTutorialWork batch from original_water must be imported/configured in isolated project before v3 runs. No production changes included in this staging.'
(p/'staged-native-input-validation-v3-manifest.json').write_text(json.dumps(m,indent=2),encoding='utf8')
print(m['sourceSHA256'])
