"""Restore the Editor time setting after restoring the original scene setup."""
import hashlib,json
from pathlib import Path
v=Path(__file__).resolve().parent
w=v.parent.parent
key='Assets/Editor/RecoveredTools/FlowWaterPropagationValidation.cs'
source=w/'.validation-project'/key
raw=source.read_bytes(); text=raw.decode('utf-8-sig').replace('\r\r\n','\n').replace('\r\n','\n')
old='''                    RestorePreferences(); RestoreScenes(); SessionState.EraseBool(ActiveKey);
                    int exit = SessionState.GetInt(ExitKey, -1); SessionState.EraseInt(ExitKey);
                    if (Application.isBatchMode && exit >= 0) EditorApplication.Exit(exit);'''
new='''                    float restoredScale = SessionState.GetFloat(ScaleKey, Time.timeScale);
                    RestorePreferences(); RestoreScenes(); SessionState.EraseBool(ActiveKey);
                    int exit = SessionState.GetInt(ExitKey, -1); SessionState.EraseInt(ExitKey);
                    // Restoring an initially empty Editor scene can reset TimeManager.
                    // Reapply the captured value after scene restoration and after
                    // the EnteredEditMode callback has completed.
                    Time.timeScale = restoredScale;
                    if (Application.isBatchMode && exit >= 0)
                        EditorApplication.delayCall += () => { Time.timeScale = restoredScale; EditorApplication.Exit(exit); };'''
assert text.count(old)==1
batch=v/'PropagationTimeRestoreBatch2'; target=batch/key
target.parent.mkdir(parents=True,exist_ok=True);target.write_text(text.replace(old,new),encoding='utf-8',newline='\n')
record={'scope':'Editor-only: restore captured timeScale after EditorSceneManager restores the original empty scene, then defer batch exit until EnteredEditMode completes. No gameplay, assertion, or water change.',
 'files':[{'target':key,'source':str(target),'sha256':hashlib.sha256(target.read_bytes()).hexdigest(),'previousSha256':hashlib.sha256(raw).hexdigest()}]}
(batch/'manifest.json').write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');print(batch/'manifest.json')
