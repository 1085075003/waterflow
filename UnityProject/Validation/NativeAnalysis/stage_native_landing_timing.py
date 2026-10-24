"""Exact native pointer landing; immutable staging only, never Assets edits."""
from pathlib import Path
import hashlib
import json
from datetime import datetime, timezone

project = Path(__file__).resolve().parents[2]
batch = project / 'Validation/NativeLandingTimingBatch1'

def read(relative, expected):
    path = project / relative
    assert hashlib.sha256(path.read_bytes()).hexdigest() == expected, path
    return path.read_text('utf-8-sig')

def replace(text, old, new):
    assert text.count(old) == 1, (old[:100], text.count(old))
    return text.replace(old, new)

motion_base = '2a332c574baee99a2a7479224684d8db1d55593835e6a184ccc424cbe77f13fb'
core_base = '1128317430e4d7e5f2bca260c83a0a61c544e0118ec86b59df1152ba09b44957'
editor_base = 'a2312072ef7ebc43acba8d7ccd672b58c58b87185e88b3c140af89c882662d84'
motion = read('Assets/Scripts/RecoveryRuntime/RecoveredNativeDragMotion.cs', motion_base)
motion = replace(motion, '        bool rendererWasEnabled;', '''        bool rendererWasEnabled;
        // doFijar102F10C..134 and doMovimiento restore cascades once below.5,
        // before the final snap/transaction. Subsequent frames must not re-gate.
        bool nativeWaterUnblocked;
        float nativeWaterRestoreDistance=-1;
        int nativeWaterRestoreFrame=-1;''')
motion = replace(motion, '            GateWater(true);\n            if (route.Count>0',
                 '            if(!nativeWaterUnblocked) GateWater(true);\n            if (route.Count>0')
motion = replace(motion, '''                actor.transform.position=final?Vector3.Lerp(actor.transform.position,target,original.smoothSpeed1*Time.deltaTime):Vector3.MoveTowards(actor.transform.position,target,original.smoothSpeed2*Time.deltaTime);
                if (Vector3.Distance''', '''                actor.transform.position=final?Vector3.Lerp(actor.transform.position,target,original.smoothSpeed1*Time.deltaTime):Vector3.MoveTowards(actor.transform.position,target,original.smoothSpeed2*Time.deltaTime);
                if(final) RestoreNativeWaterNearLanding();
                if (Vector3.Distance''')
motion = replace(motion, '''                actor.transform.position=Vector3.Lerp(actor.transform.position,Target,original.smoothSpeed*Time.deltaTime);
                if (Vector3.Distance''', '''                actor.transform.position=Vector3.Lerp(actor.transform.position,Target,original.smoothSpeed*Time.deltaTime);
                RestoreNativeWaterNearLanding();
                if (Vector3.Distance''')
motion = replace(motion, '        void CalculateHeight(Vector3 location)', '''        void RestoreNativeWaterNearLanding()
        {
            if(nativeWaterUnblocked || !released || actor==null) return;
            float distance=Vector3.Distance(actor.transform.position,Target);
            if(distance>=.5f) return;
            nativeWaterUnblocked=true; nativeWaterRestoreDistance=distance; nativeWaterRestoreFrame=Time.frameCount;
            GateWater(false);
            if(AudioManager.instance!=null && original!=null)
            {
                FieldInfo sound=typeof(MoveBloque).GetField("SoundBlock",Hidden);
                if(sound!=null) AudioManager.instance.PlaySound(sound.GetValue(original) as string);
            }
        }

        void CalculateHeight(Vector3 location)''')
motion = replace(motion, '''            active=released=pointerInitialized=guideMoved=escapeChecked=routeUp=fallback=false;''', '''            active=released=pointerInitialized=guideMoved=escapeChecked=routeUp=fallback=nativeWaterUnblocked=false;
            nativeWaterRestoreDistance=-1; nativeWaterRestoreFrame=-1;''')

core = read('Validation/NativeStepPlaybackBatch3/Assets/Scripts/RecoveryRuntime/RecoveredFlowGame.cs', core_base)
core = replace(core, '''            if (Occupant(destination, pieceIndex) >= 0)
            { Status = "That grid position is occupied. Use an empty position or another height."; Notify(); return false; }''', '''            // Native doEnd fixes a genuine pointer guide, then doFijar only
            // follows its original support and finishes the animation. It has
            // no second occupancy rejection after mechanisms may have moved.
            // Public instant TryMove still validates its requested destination.
            if (!nativeLanding && Occupant(destination, pieceIndex) >= 0)
            { Status = "That grid position is occupied. Use an empty position or another height."; Notify(); return false; }''')
core = replace(core, '''            if (AudioManager.instance != null) AudioManager.instance.PlaySound("Piedra");
            MoveCount++;''', '''            // Actual pointer motion already plays the original SoundBlock
            // at its native distance<.5 cascade-unlock phase.
            if (!nativeLanding && AudioManager.instance != null) AudioManager.instance.PlaySound("Piedra");
            MoveCount++;''')

editor = read('Validation/NativeInputFailedRetestBatch1/Assets/Editor/RecoveredTools/FlowWaterSave1GameplayValidation.cs', editor_base)
editor = replace(editor, '''        public static void RunNativeViewFailures() { Begin(new[] {754,761},true); }''', '''        public static void RunNativeViewFailures() { Begin(new[] {754,761},true); }
        public static void RunNativeLandingRegression() { Begin(new[] {1187},true); }''')
old = '''            NativeInput("EndDrag"); step.releaseFrame=Time.frameCount;
            Transform support=Read(motion,"finalSupport") as Transform;
            step.followsMovingSupport=support!=null && support.GetComponentInParent<RecoveryPiece>()!=actor;
            until=EditorApplication.timeSinceStartup+25;
            while(motion.IsActive && EditorApplication.timeSinceStartup<until) yield return null;
            step.committedFrame=Time.frameCount;'''
new = '''            bool diagnostic=level.id==1187 && step.number==18;
            Action commitObserver=null;
            if(diagnostic)
            {
                CaptureLandingContext(step,"before-release",actor);
                commitObserver=()=> {
                    if(Game!=null && Game.CurrentLevelId==level.id && (Game.MoveCount!=step.beforeMoves || Game.Status.IndexOf("occupied",StringComparison.OrdinalIgnoreCase)>=0))
                        CaptureLandingContext(step,"production-commit-notify",actor);
                };
                Game.StateChanged+=commitObserver;
            }
            try
            {
                NativeInput("EndDrag"); step.releaseFrame=Time.frameCount;
                Transform support=Read(motion,"finalSupport") as Transform;
                step.followsMovingSupport=support!=null && support.GetComponentInParent<RecoveryPiece>()!=actor;
                until=EditorApplication.timeSinceStartup+25;
                bool observedUnlock=false;
                while(motion.IsActive && EditorApplication.timeSinceStartup<until)
                {
                    if(diagnostic && !observedUnlock && (bool)Read(motion,"nativeWaterUnblocked"))
                    { observedUnlock=true; CaptureLandingContext(step,"native-below-.5-water-restored",actor); }
                    yield return null;
                }
                if(diagnostic)
                {
                    step.inputDiagnostics.Add("native-early-unlock frame="+Read(motion,"nativeWaterRestoreFrame")+",distance="+Read(motion,"nativeWaterRestoreDistance")+",unblocked="+Read(motion,"nativeWaterUnblocked")+",commitFrame="+Time.frameCount);
                    CaptureLandingContext(step,"after-native-animation",actor);
                }
                step.committedFrame=Time.frameCount;
            }
            finally { if(commitObserver!=null && Game!=null) Game.StateChanged-=commitObserver; }'''
editor = replace(editor, old, new)
editor = replace(editor, '        static IEnumerable TurnNativeCamera(Level level,Step step)', '''        static void CaptureLandingContext(Step step,string phase,RecoveryPiece selected)
        {
            if(Game==null) return;
            step.inputDiagnostics.Add(phase+" frame="+Time.frameCount+",time="+Time.time+",status="+Game.Status+",switches="+SwitchState());
            foreach(RecoveryPiece actor in Game.Pieces)
                if(actor!=null && Mathf.Abs(actor.transform.position.x-step.destination.x)<=25 && Mathf.Abs(actor.transform.position.z-step.destination.z)<=25)
                    step.inputDiagnostics.Add(phase+" actor record"+actor.SourceRecordIndex+":"+actor.BasePrefab+" pose="+actor.transform.position.ToString("F4")+",movable="+actor.CanMove);
            float offset=2-4.199999809265137f,radius=1; int mask=4097;
            foreach(MoveBloque source in UnityEngine.Object.FindObjectsOfType<MoveBloque>(true))
                if(source!=null && source.gameObject.scene==Game.GameCamera.gameObject.scene)
                { offset=source.subeRayo-4.199999809265137f;radius=source.sphereRadius;mask=source.mascaraSuelo.value;break; }
            using(RecoveredNativeInteraction.QueryOriginalColliders(Game))
                foreach(Collider hit in Physics.OverlapSphere(step.destination+Vector3.up*offset,radius,mask,QueryTriggerInteraction.UseGlobal))
                {
                    RecoveryPiece owner=hit.GetComponentInParent<RecoveryPiece>();
                    if(owner==selected) continue;
                    step.inputDiagnostics.Add(phase+" former-late-probe hit="+hit.name+",layer="+hit.gameObject.layer+",record="+(owner==null?-1:owner.SourceRecordIndex)+",pose="+hit.transform.position.ToString("F4"));
                }
        }

        static IEnumerable TurnNativeCamera(Level level,Step step)''')

files=[]
for relative, text, original_sha in [
    ('Assets/Scripts/RecoveryRuntime/RecoveredNativeDragMotion.cs',motion,motion_base),
    ('Assets/Scripts/RecoveryRuntime/RecoveredFlowGame.cs',core,core_base),
    ('Assets/Editor/RecoveredTools/FlowWaterSave1GameplayValidation.cs',editor,editor_base),
]:
    target=batch/relative; target.parent.mkdir(parents=True,exist_ok=True); target.write_text(text,'utf-8')
    files.append({'path':relative,'source':target.relative_to(project).as_posix(),
                  'sha256':hashlib.sha256(target.read_bytes()).hexdigest(),'baseSha256':original_sha})
manifest={'createdUtc':datetime.now(timezone.utc).isoformat(),
    'scope':'Immutable staging only. Main/clone Assets untouched. Actual Unity validation pending root. This changes critical production DragMotion: do not aggregate its cases with old-motion successes; rerun complete final800 against one compatible physics snapshot.',
    'files':files,'entrypoint':'Recovered.FlowWater.Editor.FlowWaterSave1GameplayValidation.RunNativeLandingRegression',
    'retainedEntrypoint':'RunNativeFailed (same v8 legal pointer/view strategy and original positive native target/completion criteria)',
    'changes':[
        'Actual pointer TryMoveCore(nativeLanding=true) accepts the genuine original guide after animation without a second late Occupant rejection. Public TryMove(nativeLanding=false) keeps original destination probing and bounds/finite/actor checks.',
        'Released normal Lerp and final fixed-route Lerp restore the moving water node once at distance<.5; subsequent LateUpdate no longer gates it again. Threshold, landing<.3/fixed<.1 and animation speeds unchanged.',
        'Play actual original SoundBlock at the native early cascade-unlock phase. Prevent duplicated private-pointer commit Piedra sound; public TryMove sound remains.',
        'Read-only1187step18 release/notify/early-unlock/after positions, original masks and live overlap owner diagnostics. No actor/switch/guide/water writes by test.',
        'Core based exactly on112831 native-camera+step Batch3; no preview/lifetime/snapshot fix lost.'],
    'nativeEvidence':[
        {'method':'MoveBloque.doEnd','va':'0x102EC58','range':'102ED04..7C','rule':'Copy actual planoGuia to puntoFinal+4.2, restore selected body layer, start fixation; no occupancy reject.'},
        {'method':'MoveBloque.doFijarPieza','va':'0x102EE14','range':'102F10C..134','rule':'distance<.5 and noDesbloqueado: doDesbloquearCascadasTarget, SoundBlock, clear noDesbloqueado.'},
        {'method':'MoveBloque.doFijarPieza','range':'102F1D0..21C','rule':'distance<.3: hide guide, directly set parent to puntoFinal; no final Occupant rejection.'},
        {'method':'MoveBloque.doCorregirPuntoFinal','va':'0x10311B8','rule':'mascaraSuelo1 support probe only sets checkCollision9 for conResorte-bearing support, follows that original moving support; not final arbitrary occupancy veto.'}],
    'compile':'Pending Roslyn runtime/editor. No Unity launched.',
    'actualUnityValidation':'Not run. Need clone1187 and public occupancy/Input61 negative regression before final800.'}
(batch/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),'utf-8')
print(json.dumps(files,ensure_ascii=False))
