from pathlib import Path
p=Path('Validation/ui-current-runtime.rsp')
s=p.read_text(encoding='utf-8')
for directory in ['Assets/Scripts/RecoveryRuntime','Assets/Scripts/RecoveryUI']:
    for source in Path(directory).glob('*.cs'):
        name=source.as_posix()
        if name not in s:s+='\n"'+name+'"\n'
p.write_text(s,encoding='utf-8')
p=Path('Validation/ui-current-editor.rsp')
s=p.read_text(encoding='utf-8')
for source in Path('Assets/Editor/RecoveredTools').glob('*.cs'):
    name=source.as_posix()
    if name not in s:s+='\n"'+name+'"\n'
p.write_text(s,encoding='utf-8')
