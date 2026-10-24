from pathlib import Path
import hashlib, json, uuid

project = Path(__file__).resolve().parents[2]
stage = Path(__file__).resolve().parent
source = stage / 'Sources/FlowWaterDirectSceneTitleValidation.cs.src'
meta = stage / 'Sources/FlowWaterDirectSceneTitleValidation.cs.meta.src'
if not meta.exists():
    meta.write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid4().hex + '\nMonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n', encoding='utf-8')
template = project / 'Validation/HudFramingCaptureFixture3/editor.rsp'
options = [line for line in template.read_text(encoding='utf-8-sig').splitlines() if line.startswith('-')]
options = [('-out:"Validation/NativeDirectSceneTitleFixture1/editor.dll"' if line.startswith('-out:') else '-r:"Library/ScriptAssemblies/Assembly-CSharp.dll"' if line == '-r:"Validation/HudFramingVisibleGraphicsBatch4/runtime.dll"' else line) for line in options]
sources = sorted(project.joinpath('Assets/Editor').rglob('*.cs'))
options += ['"' + path.relative_to(project).as_posix() + '"' for path in sources]
options += ['"' + source.relative_to(project).as_posix() + '"']
(stage / 'editor.rsp').write_text('\n'.join(options) + '\n', encoding='utf-8')
print(json.dumps({'editorSourceFiles':len(sources)+1,'sourceSha256':hashlib.sha256(source.read_bytes()).hexdigest(),'response':'Validation/NativeDirectSceneTitleFixture1/editor.rsp'}, ensure_ascii=False))
