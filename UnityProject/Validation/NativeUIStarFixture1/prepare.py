from pathlib import Path
import json,hashlib,datetime
batch=Path(__file__).resolve().parent;project=batch.parents[1]
editor=(project/'Validation/ui-current-editor.rsp').read_text(encoding='utf-8')
editor=editor.replace('-out:"Validation/editor-compile-check.dll"','-out:"Validation/NativeUIStarFixture1/editor-stars.dll"')
editor=editor.replace('Validation/ui-current-runtime.dll','Validation/NativeUIStepControls2/runtime.dll')
editor+='\n"Validation/NativeUIStarFixture1/Assets/Editor/RecoveredTools/FlowWaterStarAnimationValidation.cs"\n'
(batch/'editor.rsp').write_text(editor,encoding='utf-8')
files=[]
for item in (batch/'Assets').rglob('*'):
    if not item.is_file():continue
    target=item.relative_to(batch)
    files.append({'target':target.as_posix(),'baseSha256':None,'staged':item.relative_to(project).as_posix(),'stagedSha256':hashlib.sha256(item.read_bytes()).hexdigest()})
manifest={'preparedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'assetsModified':False,'files':files,'editorCompile':'pending',
          'testEntry':'Recovered.FlowWater.Editor.FlowWaterStarAnimationValidation.Run','report':'Validation/UIAnimations/native-ui-animation-validation.json',
          'scope':'Actual natural water victory in4native scenes, original phone/tablet complete state and scale curve, assigned Star_B .2s delay/controller time, three originally disabled D Animator states, native configured-target Star_End; all1350 progress/audio/help prefs scoped and restored.',
          'requiredRuntime':'NativeUIAnimations1 (225ac UI+d0b0 helper), optional NativeUIStepControls2 compatible'}
(batch/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(manifest['files'])
