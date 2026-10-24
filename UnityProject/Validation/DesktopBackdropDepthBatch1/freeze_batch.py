from pathlib import Path
import hashlib,json,difflib,datetime

PROJECT=Path(__file__).resolve().parents[2]
BATCH=Path(__file__).resolve().parent
FIXTURE=PROJECT/'Validation/DesktopBackdropDepthFixture1'
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def relative(path):return path.relative_to(PROJECT).as_posix()
def dump(path,value):path.write_text(json.dumps(value,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
def diff(before,after,target,destination):
    destination.write_text(''.join(difflib.unified_diff(before.read_text(encoding='utf-8-sig').splitlines(True),after.read_text(encoding='utf-8-sig').splitlines(True),fromfile='before/'+target,tofile='after/'+target)),encoding='utf-8')
    return sha(destination)
now=datetime.datetime.now(datetime.timezone.utc).isoformat()
runtime_files=[]
for path in sorted((BATCH/'Assets').rglob('*')):
    if not path.is_file():continue
    target=path.relative_to(BATCH).as_posix()
    if path.name=='RecoveredBoardFraming.cs':before=BATCH/'base-RecoveredBoardFraming.cs'
    else:before=PROJECT/target
    runtime_files.append(dict(target=target,source=relative(path),previousSha256=sha(before) if before.exists() else None,sha256=sha(path)))
framing_diff=diff(BATCH/'base-RecoveredBoardFraming.cs',BATCH/'Assets/Scripts/RecoveryUI/RecoveredBoardFraming.cs','Assets/Scripts/RecoveryUI/RecoveredBoardFraming.cs',BATCH/'framing-hook.diff')
shader_diff=diff(BATCH/'Evidence/UI-Default.shader',BATCH/'Assets/Resources/RecoveredBackdropDepth.shader','Assets/Resources/RecoveredBackdropDepth.shader',BATCH/'ui-default-to-far-depth.diff')
combined=diff(PROJECT/'Assets/Scripts/RecoveryUI/RecoveredBoardFraming.cs',BATCH/'Assets/Scripts/RecoveryUI/RecoveredBoardFraming.cs','Assets/Scripts/RecoveryUI/RecoveredBoardFraming.cs',BATCH/'framing-from-main-original.diff')
provenance=dict(officialRelease='https://unity.com/releases/editor/whats-new/2022.3.28f1',officialDownload='https://download.unity3d.com/download_unity/6bae5ce6b222/builtin_shaders-2022.3.28f1.zip',installedUnity='2022.3.28f1',revision='6bae5ce6b222',nestedArchiveMember='build/BuiltinShaders/builtin_shaders.zip',sourceMember='DefaultResourcesExtra/UI/UI-Default.shader',license='MIT; original Unity copyright retained; exact license is shipped beside shader.',files={relative(p):sha(p) for p in sorted((BATCH/'Evidence').iterdir()) if p.is_file()},changes=['Shader name only','ZTest unity_GUIZTestMode becomes explicit LEqual','Vertex clip z is far depth: 0 for UNITY_REVERSED_Z, w otherwise; XY/W and original mask calculations unchanged'],preserved=['Original 2022.3.28f1 UI/Default premultiplied Blend One OneMinusSrcAlpha','ZWrite Off, stencil properties/state, ColorMask, culling','Texture/Sprite UV, tint/vertex color, gamma conversion, rounded alpha','RectMask2D softness, stencil/mask and UNITY_UI_ALPHACLIP behavior','Original fondo Image hierarchy/Rect/activation/animation/sprite/color/raycast and Canvas plane distance'])
dump(BATCH/'shader-provenance.json',provenance)
dump(BATCH/'manifest.json',dict(preparedUtc=now,immutable=True,files=runtime_files,framingOriginalBaseSha256='47c836757825aa0c7347a99678d19a287f406e1fa8db0f48db5c9470068816d1',framingHudBatch7BaseSha256=sha(BATCH/'base-RecoveredBoardFraming.cs'),scope='Background-only desktop depth adaptation. Bind local material copies solely to exact original UIManagerScript fondo* Image fields. Shader is explicitly Resources.Load<Shader> referenced under existing Resources for Player inclusion. Original UI/scene/camera/geometry/physics/native water/actors/scheduler unchanged. Framing differs from HUD Batch7 only by RecoveredBackdropDepth.Apply(scene) in Begin.',diffs={'framing-hook.diff':framing_diff,'ui-default-to-far-depth.diff':shader_diff,'framing-from-main-original.diff':combined},shaderProvenance={'source':relative(BATCH/'shader-provenance.json'),'sha256':sha(BATCH/'shader-provenance.json')},compile={'runtimeRoslynExitCode':0,'runtimeAssemblySha256':sha(BATCH/'runtime.dll'),'responseSha256':sha(BATCH/'runtime.rsp'),'unityShaderGpuCompile':'Pending actual Unity fixture; no claim from C# compile.'},fixtureManifest=relative(FIXTURE/'manifest.json'),entry='Recovered.FlowWater.Editor.FlowWaterHudFramingValidation.Run',report='Validation/HudFraming/hud-framing-validation.json',actualStatus='Pending parent Unity actual 24+6 screenshots, native title/global/near-far/HUD/bounds proof, independent GPU original/far background raster equivalence and actual ground/tierra/border visibility. Windows build shader inclusion must also be measured.'))
fixture_files=[]
for path in sorted((FIXTURE/'Assets').rglob('*')):
    if not path.is_file():continue
    target=path.relative_to(FIXTURE).as_posix()
    before=FIXTURE/'base-FlowWaterHudFramingValidation.cs' if path.suffix=='.cs' else PROJECT/target
    fixture_files.append(dict(target=target,source=relative(path),previousSha256=sha(before) if before.exists() else None,sha256=sha(path)))
fixture_diff=diff(FIXTURE/'base-FlowWaterHudFramingValidation.cs',FIXTURE/'Assets/Editor/RecoveredTools/FlowWaterHudFramingValidation.cs','Assets/Editor/RecoveredTools/FlowWaterHudFramingValidation.cs',FIXTURE/'background-ground-fixture.diff')
dump(FIXTURE/'manifest.json',dict(preparedUtc=now,immutable=True,files=fixture_files,entry='Recovered.FlowWater.Editor.FlowWaterHudFramingValidation.Run',reportDirectory='Validation/HudFraming',reportFile='hud-framing-validation.json',scope='Editor-only own-active guarded fixture. Preserves all 24 HUD/title/global/prefs/Time/near-far/resize checks. Adds 6 real underground/night captures, all actual ground/tierra/border vertex depths plus independent GPU magenta ground-depth visibility and original-versus-far UI RGBA pixel equality. Temporary probe materials/enabled flags are restored synchronously; never changes production transforms, actor state or time speed.',samples={'mainLevels':[366,409,421,401],'mainStates':['initial','solution'],'sizes':[[1280,720],[885,630],[2560,1080]],'mainFrames':24,'extraLevels':[288,2001],'extraState':'backdrop-initial','extraFrames':6},diff={'background-ground-fixture.diff':fixture_diff},compile={'editorRoslynExitCode':0,'editorAssemblySha256':sha(FIXTURE/'editor.dll'),'responseSha256':sha(FIXTURE/'editor.rsp')},requiredRuntimeManifest=relative(BATCH/'manifest.json'),actualStatus='Pending parent Unity; fixture numerical checks must be complemented by actual normal PNG review, especially 409/421 at 885x630.'))
print(json.dumps({'runtimeManifest':relative(BATCH/'manifest.json'),'fixtureManifest':relative(FIXTURE/'manifest.json'),'runtimeFiles':len(runtime_files),'fixtureFiles':len(fixture_files),'framingSha256':sha(BATCH/'Assets/Scripts/RecoveryUI/RecoveredBoardFraming.cs'),'helperSha256':sha(BATCH/'Assets/Scripts/RecoveryUI/RecoveredBackdropDepth.cs'),'shaderSha256':sha(BATCH/'Assets/Resources/RecoveredBackdropDepth.shader'),'fixtureSha256':sha(FIXTURE/'Assets/Editor/RecoveredTools/FlowWaterHudFramingValidation.cs')},indent=2))
