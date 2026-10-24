"""Keep the fixed-route completion gate distinct from ordinary doFijar<.5."""
from pathlib import Path
import datetime
import hashlib
import json

project=Path(__file__).resolve().parents[2]
old=project/'Validation/NativeLandingTimingBatch1'
batch=project/'Validation/NativeLandingTimingBatch2'
manifest=json.loads((old/'manifest.json').read_text('utf-8'))
for item in manifest['files']:
    path=project/item['source']
    assert hashlib.sha256(path.read_bytes()).hexdigest()==item['sha256']
    text=path.read_text('utf-8')
    if item['path'].endswith('/RecoveredNativeDragMotion.cs'):
        before='                if(final) RestoreNativeWaterNearLanding();\n'
        assert text.count(before)==1
        text=text.replace(before,'')
        before='                        if (released) Complete(); else routeUp=false;'
        assert text.count(before)==1
        text=text.replace(before,'''                        // Native doMovimiento1033570..1033724 restores water
                        // only after the last route point reaches its .1 bound.
                        // Ordinary doFijar below has a distinct early .5 bound.
                        if (released) { RestoreNativeWaterNearLanding(); Complete(); } else routeUp=false;''')
        before='        // doFijar102F10C..134 and doMovimiento restore cascades once below.5,\n        // before the final snap/transaction. Subsequent frames must not re-gate.'
        assert text.count(before)==1
        text=text.replace(before,'''        // Ordinary doFijar102F10C..134 restores cascades once below.5.
        // Fixed doMovimiento restores only on final route arrival below.1.
        // Subsequent frames must not re-gate an already restored water node.''')
    target=batch/item['path']
    target.parent.mkdir(parents=True,exist_ok=True)
    target.write_text(text,'utf-8')
    item['source']=target.relative_to(project).as_posix()
    item['sha256']=hashlib.sha256(target.read_bytes()).hexdigest()
manifest['createdUtc']=datetime.datetime.now(datetime.timezone.utc).isoformat()
manifest['supersedes']='NativeLandingTimingBatch1. Do not import Batch1: its final fixed-route unlock was too early; full doMovimiento branch evidence corrects that here.'
manifest['changes'][1]='Ordinary released doFijar Lerp restores water once at distance<.5; final fixed-route doMovimiento restores only on its existing <.1 final arrival, after exact final route snap. Subsequent LateUpdate never re-gates. Landing thresholds and speeds unchanged.'
manifest['changes'][2]='SoundBlock occurs at the exact corresponding native unlock: ordinary<.5, fixed-route final completion. No duplicate private-pointer commit sound; public TryMove sound remains.'
manifest['nativeEvidence'].append({'method':'MoveBloque.doMovimiento','va':'0x10332D0','range':'1033570..574 then10333710..724','rule':'Only when route point reaches native .1 threshold, final-down branch clears route and unlocks cascades+SoundBlock. Do not apply ordinary doFijar early.5 to this route.'})
manifest['compile']='Pending Roslyn full runtime+Editor for Batch2. No Unity launched.'
(batch/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),'utf-8')
print(json.dumps(manifest['files'],ensure_ascii=False))
