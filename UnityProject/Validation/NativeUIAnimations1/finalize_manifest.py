from pathlib import Path
import hashlib,json
batch=Path(__file__).resolve().parent
workspace=batch.parents[2]
meta=workspace/'.validation-project/Assets/Scripts/RecoveryUI/RecoveredOriginalUIAnimation.cs.meta'
target=batch/'Assets/Scripts/RecoveryUI/RecoveredOriginalUIAnimation.cs.meta'
target.write_bytes(meta.read_bytes())
manifest=json.loads((batch/'manifest.json').read_text(encoding='utf-8'))
manifest['runtimeCompile']='Unity2022.3.28f1 Roslyn exit0; runtime-ui.dll generated'
manifest['editorCompile']='Unity2022.3.28f1 Roslyn exit0; editor-ui.dll generated'
manifest['existingValidationEntry']='Recovered.FlowWater.Editor.FlowWaterUIValidation.Run'
manifest['actualCloneValidation']='759/759 PASS,0 runtimeErrors,2026-10-16T01:22:06.909Z; source root orchestration'
entry={'target':'Assets/Scripts/RecoveryUI/RecoveredOriginalUIAnimation.cs.meta','baseSha256':None,'staged':target.relative_to(batch.parents[1]).as_posix(),'stagedSha256':hashlib.sha256(target.read_bytes()).hexdigest()}
manifest['files']=[x for x in manifest['files'] if not x['target'].endswith('.meta')]+[entry]
(batch/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(entry)
