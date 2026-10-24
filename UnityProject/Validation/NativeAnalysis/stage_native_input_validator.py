import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parents[2]
analysis = root / 'Validation/NativeAnalysis'
base = analysis / 'FlowWaterSave1GameplayValidation.pending-release-sequence.cs.src'
assert hashlib.sha256(base.read_bytes()).hexdigest() == '000ef31e85011fd5f8dd352f45b2649d64922b8a09dae382d4c0e25ca187222a'
s = base.read_text('utf8')
s = s.replace('        const string TimeKey =', '        const string InputModeKey = "FlowWater.Save1GameplayValidation.NativeInput";\n        static bool NativeReplay { get { return SessionState.GetBool(InputModeKey,false); } }\n        const string TimeKey =')
s = s.replace('public float waitedSeconds;', 'public float waitedSeconds;\n            public bool productionInput, nativeWaitObservedBusy, waterPendingBefore, waterPendingAtRelease;\n            public int pickupFrame, nativeWaitBusyFrame=-1, nativeWaitIdleFrame=-1, releaseFrame, committedFrame;\n            public Vector3 pickScreen, dropScreen, originalGuide, heldPosition;')
s = s.replace('public string startedUtc, finishedUtc, unityVersion;', 'public string startedUtc, finishedUtc, unityVersion, inputStrategy;')
s = s.replace('        public static void Run1236()', '''        public static void RunNativeFocused() { Begin(new[] {1142,1149,1236,1656,1660},true); }
        public static void RunNativeAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start native-input Save1 validation from Edit mode.");
            RecoveryCatalogBuilder.Build();
            Begin(RecoveryCatalog.Load().levels.Where(e=>e.solution!=null && JsonUtility.FromJson<RecoveryEnvelope>(e.solution.text).data.IsValidSteps()).Select(e=>e.assetId).OrderBy(id=>id).ToArray(),true);
        }
        public static void Run1236()''')
s = s.replace('static void Begin(int[] cases)', 'static void Begin(int[] cases, bool nativeInput=false)')
s = s.replace('            SessionState.SetString(CasesKey, String.Join(",", cases));', '            SessionState.SetBool(InputModeKey,nativeInput);\n            SessionState.SetString(CasesKey, String.Join(",", cases));')
s = s.replace('originalTimeScale = SessionState.GetFloat(TimeKey, 1), plannedLevels', 'originalTimeScale = SessionState.GetFloat(TimeKey, 1), inputStrategy = NativeReplay ? "production-pointer-drag" : "instant-TryMove-baseline", plannedLevels')
old='                    report.notes.Add("All cases load original gm mode0 through the production UI adapter'
at=s.index(old)
s=s[:at]+'''                    if(NativeReplay) report.notes.Add("Native-input variant: every captured non-z actor uses the original camera screen ray, production BeginDrag/ContinueDrag/EndDrag, original grab offset and real motion. No actor transform, guide, switch or water state is assigned by this validator. A w starts selection/layer2 first and observes at least one real activadoRes busy-to-idle cycle before proceeding, following doProcess1Solution1034C00 and doProcesoEspera1035244..52B4. This proves a legal real-input player strategy, not the original automatic preview animation's exact phase timing.");
''' + s[at:]
s=s.replace('w enables the original wait before its real pickup.', 'Native w selects layer2 before waiting for a real busy-to-idle cycle; the instant baseline only waits for already-busy mechanisms and is identified separately in inputStrategy.')
s=s.replace('report.notes.Add("All cases load original gm mode0 through the production UI adapter', 'if(!NativeReplay) report.notes.Add("All cases load original gm mode0 through the production UI adapter')
s=s.replace('                else if(step.sameCellPickup)', '''                else if(NativeReplay)
                {
                    foreach(object inputWait in ExerciseNativeInputStep(level,step)) yield return inputWait;
                }
                else if(step.sameCellPickup)''')
needle='        const BindingFlags AllFields ='
assert needle in s
methods=r'''
        static IEnumerable ExerciseNativeInputStep(Level level,Step step)
        {
            step.productionInput=true;
            double until=EditorApplication.timeSinceStartup+15;
            RecoveryPiece actor=null;
            while(actor==null && EditorApplication.timeSinceStartup<until)
            {
                actor=Game.Pieces.Where(p=>p!=null && p.CanMove && Near(p.transform.position,step.origin)).OrderBy(p=>(p.transform.position-step.origin).sqrMagnitude).FirstOrDefault();
                if(actor==null) yield return null;
            }
            Require("gm"+level.id+"-native-input-capture-"+step.number,actor!=null,"An actual movable actor must occupy the original recorded capture position.");
            step.sourceRecord=actor.SourceRecordIndex; step.actorIndex=actor.Index; step.actor=actor.OriginalName;
            step.actualBefore=actor.transform.position; step.movable=actor.CanMove;
            step.waterPendingBefore=Water!=null && Water.HasPendingPropagation;
            Vector3 screen;
            Require("gm"+level.id+"-native-input-original-screen-pick-"+step.number,TryOriginalPickPoint(actor,out screen),"Select through the unmodified scene camera and original closest-hit/layer queries.");
            step.pickScreen=screen; NativeInput("BeginDrag",screen); step.pickupFrame=Time.frameCount;
            var motion=Game.GetComponent<RecoveredNativeDragMotion>();
            Require("gm"+level.id+"-native-input-production-pickup-"+step.number,motion!=null && motion.IsActive,"Production BeginDrag must accept the real selected actor.");
            Collider body=Read(motion,"selectedCollider") as Collider;
            Require("gm"+level.id+"-native-input-selected-layer2-"+step.number,body!=null && body.gameObject.layer==2,"Native doProcess1Solution1034C00 selects the exact collider body into IgnoreRaycast before waiting/moving.");
            if(step.exception=="w")
            {
                until=EditorApplication.timeSinceStartup+35;
                while(EditorApplication.timeSinceStartup<until)
                {
                    bool active=NativeChannelsBusy();
                    if(active && !step.nativeWaitObservedBusy) { step.nativeWaitObservedBusy=true; step.nativeWaitBusyFrame=Time.frameCount; }
                    if(step.nativeWaitObservedBusy && !active) { step.nativeWaitIdleFrame=Time.frameCount; break; }
                    yield return null;
                }
                Require("gm"+level.id+"-native-w-selected-busy-idle-"+step.number,step.nativeWaitObservedBusy && step.nativeWaitIdleFrame>=step.nativeWaitBusyFrame,"Original w requires selected layer2, then a real busy frame, then all eight channels idle; merely starting idle cannot satisfy it.");
            }
            // The original fixed-roof escape precedes changing the final guide.
            until=EditorApplication.timeSinceStartup+20;
            while(motion.IsActive && motion.RouteStage>0 && !motion.IsReleased && EditorApplication.timeSinceStartup<until) yield return null;
            Require("gm"+level.id+"-native-input-up-route-settled-"+step.number,motion.IsActive && motion.RouteStage==0,"Original up/over path must finish before guiding the held actor into the destination.");
            RaycastHit surface; Vector3 landing;
            if(step.sameCellPickup)
            {
                screen=step.pickScreen;
                step.originalGuide=motion.Target;
                Require("gm"+level.id+"-native-input-same-cell-guide-"+step.number,Near(motion.Target,step.destination),"The unchanged native guide must match the same-cell recording.");
            }
            else
            {
                Require("gm"+level.id+"-native-input-real-drop-surface-"+step.number,FindOriginalDropPoint(step.destination,out screen,out landing,out surface),"An actual authored Map/Origen ray must resolve to the original recorded destination. Missing/occluded surfaces fail rather than fabricating a guide.");
                NativeInput("ContinueDrag",screen);
                step.originalGuide=motion.Target;
                Require("gm"+level.id+"-native-input-original-guide-"+step.number,OriginalGuideMatches(motion.Target,step.destination,surface),"Resolved production guide="+motion.Target.ToString("F3")+", recorded="+step.destination.ToString("F3"));
            }
            // Offset is the actual original grab point. Adjust only the screen
            // pointer, re-raycasting the surface each time, as a user centers it.
            for(int adjust=0;adjust<12;adjust++)
            {
                Vector3 floating=(Vector3)Read(motion,"floatingTarget");
                Vector3 target=motion.Target;
                Vector3 delta=new Vector3(target.x-floating.x,0,target.z-floating.z);
                if(delta.sqrMagnitude<.000025f) break;
                RaycastHit pointerHit;
                using(RecoveredNativeInteraction.QueryOriginalColliders(Game))
                    Require("gm"+level.id+"-native-input-pointer-ray-"+step.number+"-"+adjust,Physics.Raycast(Game.GameCamera.ScreenPointToRay(screen),out pointerHit,Mathf.Infinity,Physics.DefaultRaycastLayers,QueryTriggerInteraction.UseGlobal),"The pointer correction must remain a real scene surface ray.");
                Vector3 proposed=Game.GameCamera.WorldToScreenPoint(pointerHit.point+delta);
                Vector3 proposedLanding; RaycastHit proposedSurface;
                Require("gm"+level.id+"-native-input-corrected-guide-"+step.number+"-"+adjust,RecoveredNativeInteraction.TryGetDropPoint(Game.GameCamera,proposed,Game,out proposedLanding,out proposedSurface) && OriginalGuideMatches(proposedLanding,step.destination,proposedSurface),"The offset-corrected real pointer must preserve the original target cell.");
                screen=proposed; NativeInput("ContinueDrag",screen);
            }
            step.dropScreen=screen;
            until=EditorApplication.timeSinceStartup+20;
            while(EditorApplication.timeSinceStartup<until)
            {
                Vector3 target=motion.Target, position=actor.transform.position;
                if(new Vector2(position.x-target.x,position.z-target.z).sqrMagnitude<.0004f && position.y>=target.y+5) break;
                yield return null;
            }
            step.heldPosition=actor.transform.position;
            Vector3 heldTarget=motion.Target;
            Require("gm"+level.id+"-native-input-held-hover-"+step.number,new Vector2(step.heldPosition.x-heldTarget.x,step.heldPosition.z-heldTarget.z).sqrMagnitude<.0004f && step.heldPosition.y>=heldTarget.y+5,"Actual held actor="+step.heldPosition.ToString("F3")+", original guide="+heldTarget.ToString("F3"));
            step.waterPendingAtRelease=Water!=null && Water.HasPendingPropagation;
            NativeInput("EndDrag"); step.releaseFrame=Time.frameCount;
            Transform support=Read(motion,"finalSupport") as Transform;
            step.followsMovingSupport=support!=null && support.GetComponentInParent<RecoveryPiece>()!=actor;
            until=EditorApplication.timeSinceStartup+25;
            while(motion.IsActive && EditorApplication.timeSinceStartup<until) yield return null;
            step.committedFrame=Time.frameCount;
            step.actualAfter=actor.transform.position;
            step.nativeLanding=step.followsMovingSupport?motion.Target:step.destination;
            step.applied=!motion.IsActive && Game.MoveCount==step.beforeMoves+1 && Near(step.actualAfter,step.nativeLanding);
            step.status="Original screen input pickup/hover/release; "+Game.Status;
            Require("gm"+level.id+"-native-input-layer-restored-"+step.number,body!=null && body.gameObject.layer!=2,"EndDrag restores the selected layer; a new real carry may temporarily own native layer9.");
        }
        static bool NativeChannelsBusy()
        {
            for(int i=1;i<=8;i++) if((bool)typeof(UIManagerScript).GetField("activadoRes"+i).GetValue(null)) return true;
            return false;
        }
        static bool OriginalGuideMatches(Vector3 actual,Vector3 recorded,RaycastHit hit)
        {
            if(Near(actual,recorded)) return true;
            return hit.collider!=null && hit.collider.name.IndexOf("CuboResorte",StringComparison.Ordinal)>=0 && Near(actual,recorded+Vector3.up*1.2f);
        }
        static bool FindOriginalDropPoint(Vector3 destination,out Vector3 screen,out Vector3 landing,out RaycastHit surface)
        {
            var points=new List<Vector3>();
            points.Add(destination-Vector3.up*4.2f); points.Add(destination);
            using(RecoveredNativeInteraction.QueryOriginalColliders(Game))
            {
                foreach(Collider collider in UnityEngine.Object.FindObjectsOfType<Collider>(true))
                {
                    if(collider==null || !collider.enabled || !collider.gameObject.activeInHierarchy || collider.gameObject.scene!=Game.GameCamera.gameObject.scene) continue;
                    Vector3 point=collider.transform.position;
                    if(Mathf.Abs(point.x-destination.x)>4 || Mathf.Abs(point.z-destination.z)>4 || point.y>destination.y+5 || point.y<destination.y-12) continue;
                    points.Add(point); points.Add(collider.bounds.center);
                }
                foreach(Vector3 point in points)
                foreach(Vector3 delta in new[]{Vector3.zero,Vector3.right*3,Vector3.left*3,Vector3.forward*3,Vector3.back*3,Vector3.up*2,Vector3.down*2})
                {
                    Vector3 candidate=Game.GameCamera.WorldToScreenPoint(point+delta);
                    if(candidate.z<=0) continue;
                    if(RecoveredNativeInteraction.TryGetDropPoint(Game.GameCamera,candidate,Game,out landing,out surface) && OriginalGuideMatches(landing,destination,surface))
                    { screen=candidate; return true; }
                }
            }
            screen=landing=Vector3.zero; surface=new RaycastHit(); return false;
        }
'''
s=s.replace(needle,methods+'\n'+needle)
out=analysis/'FlowWaterSave1GameplayValidation.pending-native-input.cs.src'
out.write_text(s,encoding='utf8',newline='\n')
rsp=(analysis/'save1-gameplay-support-compile.rsp').read_text('utf8').replace('save1-gameplay-support-check.dll','save1-native-input-check.dll').replace('FlowWaterSave1GameplayValidation.pending-support.cs.src','FlowWaterSave1GameplayValidation.pending-native-input.cs.src')
(analysis/'save1-native-input-compile.rsp').write_text(rsp,encoding='utf8')
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
manifest={'status':'Immutable staging only; not imported or executed','baseFile':str(base.relative_to(root)),'baseSHA256':sha(base),'mainBaseFile':'Assets/Editor/RecoveredTools/FlowWaterSave1GameplayValidation.cs','mainBaseSHA256':sha(root/'Assets/Editor/RecoveredTools/FlowWaterSave1GameplayValidation.cs'),'sourceFile':str(out.relative_to(root)),'sourceSHA256':sha(out),'entries':['Recovered.FlowWater.Editor.FlowWaterSave1GameplayValidation.RunNativeFocused','Recovered.FlowWater.Editor.FlowWaterSave1GameplayValidation.RunNativeAll'],'cases':[1142,1149,1236,1656,1660],'scope':'All non-z captured actors use actual production camera picking and Begin/Continue/EndDrag, real original surface guide and held-hover pointer offset correction. w selects layer2 before requiring observed busy-to-idle. No transform/guide/switch/water writes. Existing RunAll remains explicit instant-TryMove baseline. RunNativeAll enumerates the same800 originalSave1 catalog; report.inputStrategy=production-pointer-drag. Failure to pick, ray guide, observe busy or finish real motion is reported, never bypassed.','nativeEvidence':['doProcess1Solution1034AEC enables w wait,1034C00 selects layer2 before HandleStepsSolution1034290..2AC calls doProcesoEspera','doProcesoEspera103528C..290 records observedbusy;10352A0..2B4 clears wait only after observedbusy then all eightidle'],'limitations':'This is a legal real-input player strategy, not exact recreation of native automatic preview animation phase timing. NativeFocused must actually run on cloned project, no result inferred from compile.'}
(analysis/'staged-native-input-validation-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
print(json.dumps(manifest,indent=2))
