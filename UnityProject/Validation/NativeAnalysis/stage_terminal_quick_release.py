"""Isolate a legal terminal pointer-release strategy; no production edits."""
from pathlib import Path
import hashlib
import json
from datetime import datetime, timezone

project=Path(__file__).resolve().parents[2]
base=project/'Validation/NativeInputHalvesBatch1/Assets/Editor/RecoveredTools/FlowWaterSave1GameplayValidation.cs'
base_sha='baba9b5fd7854b3397978e9b41c4239988ba5ab16115d587d9bac7a712ed2961'
assert hashlib.sha256(base.read_bytes()).hexdigest()==base_sha
text=base.read_text('utf-8')

def patch(before,after):
    global text
    assert text.count(before)==1,(before[:100],text.count(before))
    text=text.replace(before,after)

patch('        const string CatalogKey = "FlowWater.Save1GameplayValidation.ExpectedCatalog";', '''        const string CatalogKey = "FlowWater.Save1GameplayValidation.ExpectedCatalog";
        const string TerminalQuickKey = "FlowWater.Save1GameplayValidation.TerminalQuickRelease";''')
patch('            public bool compressionLanding, retainedOriginalGrabOffset;', '            public bool compressionLanding, retainedOriginalGrabOffset, terminalQuickRelease;')
patch('            public ExpectedCatalog expectedCatalog;', '            public ExpectedCatalog expectedCatalog;\n            public string playerReleaseStrategy;')
patch('        public static void RunNativeLandingRegression() { Begin(new[] {1187},true); }', '''        public static void RunNativeLandingRegression() { Begin(new[] {1187},true); }
        public static void RunNativeTerminalQuickRelease() { Begin(new[] {1187},true,null,true); }''')
patch('        static void Begin(int[] cases, bool nativeInput=false, RetestSource retestSource=null)',
      '        static void Begin(int[] cases, bool nativeInput=false, RetestSource retestSource=null, bool terminalQuick=false)')
patch('            SessionState.SetBool(InputModeKey,nativeInput);', '''            SessionState.SetBool(InputModeKey,nativeInput);
            SessionState.SetBool(TerminalQuickKey,terminalQuick);''')
patch('                    string catalogJson=SessionState.GetString(CatalogKey,"");', '''                    report.playerReleaseStrategy=SessionState.GetBool(TerminalQuickKey,false)?"settled-hover-prefix-and-quick-terminal-ordinary-guide":"settled-hover";
                    if(SessionState.GetBool(TerminalQuickKey,false)) report.notes.Add("Legal terminal-release diagnostic strategy: all preceding recorded actions retain the ordinary settled-hover strategy. Only the final recorded ordinary block releases after its exact original guide is resolved and one real held frame has elapsed, without waiting to rise/hover35 high. Original player input has no mandatory hover delay. CuboResorte keeps its original centered hold/compression behavior. Production Begin/Continue/End and actual animations/collisions still run; no actor/guide/water/switch is assigned. Final raw destinations, recorded count, transactions and positive native water/victory criteria remain unchanged.");
                    string catalogJson=SessionState.GetString(CatalogKey,"");''')
patch('            step.compressionLanding=surface.collider!=null && surface.collider.name.IndexOf("CuboResorte",StringComparison.Ordinal)>=0;', '''            step.compressionLanding=surface.collider!=null && surface.collider.name.IndexOf("CuboResorte",StringComparison.Ordinal)>=0;
            step.terminalQuickRelease=SessionState.GetBool(TerminalQuickKey,false) && !step.compressionLanding && report.expectedCatalog!=null && step.number==report.expectedCatalog.levels.First(x=>x.id==level.id).recordedSteps;''')
patch('''            until=EditorApplication.timeSinceStartup+20;
            while(EditorApplication.timeSinceStartup<until)
            {
                Vector3 target=motion.Target, position=actor.transform.position;''', '''            until=EditorApplication.timeSinceStartup+20;
            // One real frame preserves a real pointer-down/move/up interval;
            // the selected actor is allowed to lag its final guide, as native
            // doEnd/doFijar expressly animate it after release.
            if(step.terminalQuickRelease) yield return null;
            while(!step.terminalQuickRelease && EditorApplication.timeSinceStartup<until)
            {
                Vector3 target=motion.Target, position=actor.transform.position;''')
patch('''            Require("gm"+level.id+"-native-input-held-hover-"+step.number,heldReady && step.heldPosition.y>=heldTarget.y+5,"Actual held actor="+step.heldPosition.ToString("F3")+", original pointer floatingTarget="+step.heldFloatingTarget.ToString("F3")+", original guide="+heldTarget.ToString("F3")+"; ordinary doFijar Lerp centers the released actor at the guide, while CuboResorte requires exact held XZ before delayed compression.");''', '''            if(step.terminalQuickRelease)
                Require("gm"+level.id+"-native-terminal-quick-original-guide-"+step.number,motion.IsActive && !motion.IsReleased && Near(motion.Target,step.destination),"A real held pointer resolved the unchanged original destination before early release; original post-release motion must still complete. actor="+step.heldPosition.ToString("F4")+",guide="+motion.Target.ToString("F4"));
            else
                Require("gm"+level.id+"-native-input-held-hover-"+step.number,heldReady && step.heldPosition.y>=heldTarget.y+5,"Actual held actor="+step.heldPosition.ToString("F3")+", original pointer floatingTarget="+step.heldFloatingTarget.ToString("F3")+", original guide="+heldTarget.ToString("F3")+"; ordinary doFijar Lerp centers the released actor at the guide, while CuboResorte requires exact held XZ before delayed compression.");''')

batch=project/'Validation/NativeTerminalReleaseValidationBatch1'
target=batch/'Assets/Editor/RecoveredTools/FlowWaterSave1GameplayValidation.cs'
target.parent.mkdir(parents=True,exist_ok=True)
target.write_text(text,'utf-8')
manifest={'createdUtc':datetime.now(timezone.utc).isoformat(),
    'scope':'EDITOR STRATEGY DIAGNOSTIC ONLY. No production modification, no pose/water/bus/guide assignment. LandingTimingBatch2 exact physical runtime remains unchanged. Main/clone Assets untouched.',
    'files':[{'path':'Assets/Editor/RecoveredTools/FlowWaterSave1GameplayValidation.cs','source':target.relative_to(project).as_posix(),'sha256':hashlib.sha256(target.read_bytes()).hexdigest(),'baseSha256':base_sha,'ancestorInstalledEditorSha256':'22707476b03ad527a2fa3939a0febe223e7b107148e0390e01e7da6d8a01804a'}],
    'entrypoint':'Recovered.FlowWater.Editor.FlowWaterSave1GameplayValidation.RunNativeTerminalQuickRelease',
    'changes':'Only final recorded ordinary actor uses actual original pointer guide+one real heldframe then EndDrag rather than mandatory high hover. Earliersteps and CuboResorte still same settled-hover strategy. Native post-release interpolation and complete target/mission/transaction checks remain. Report explicitly records playerReleaseStrategy and terminalQuickRelease flag.',
    'evidence':'Original doEnd accepts current real guide immediately and starts doFijar interpolation. No native requirement waits for the held actor to reach35high. This is a legal player strategy, not an assertion of original automatic preview timing.',
    'retained':'All Half1/Half2, expectedCatalog, RunNativeFailed and1187 live diagnostics retained. Other entries clear terminalQuick flag automatically.',
    'compile':'Pending Roslyn Editor against LandingTimingBatch2 runtime. Actual Unity test not run.'}
(batch/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),'utf-8')
print(manifest['files'][0]['sha256'])
