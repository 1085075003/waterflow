"""Do not let an inactive Editor fixture overwrite other tests' time settings."""
import hashlib, json
from pathlib import Path
v=Path(__file__).resolve().parent
w=v.parent.parent
key='Assets/Editor/RecoveredTools/FlowWaterPropagationValidation.cs'
source=w/'.validation-project'/key
raw=source.read_bytes()
text=raw.decode('utf-8-sig')
before='if (state == PlayModeStateChange.ExitingPlayMode) Unlock();'
after='if (state == PlayModeStateChange.ExitingPlayMode && SessionState.GetBool(ActiveKey, false)) Unlock();'
assert text.count(before)==1
batch=v/'PropagationTimeRestoreBatch1'
target=batch/key
target.parent.mkdir(parents=True, exist_ok=True)
target.write_text(text.replace(before, after), encoding='utf-8')
record={'scope':'Editor-only fixture lifecycle fix. Inactive propagation validator must not reset unrelated runs to its fallback timeScale1 on ExitingPlayMode. No runtime/physics/assertion changes.',
        'files':[{'target':key,'source':str(target),'sha256':hashlib.sha256(target.read_bytes()).hexdigest(),'previousSha256':hashlib.sha256(raw).hexdigest()}]}
(batch/'manifest.json').write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(batch/'manifest.json')
