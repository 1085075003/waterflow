from pathlib import Path
import datetime, hashlib, json, re

stage=Path(__file__).resolve().parent
project=stage.parents[1]
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def write(path,data): path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
now=datetime.datetime.now(datetime.timezone.utc).isoformat()
source=stage/'Sources/FlowWaterDirectSceneTitleValidation.cs.src'
meta=stage/'Sources/FlowWaterDirectSceneTitleValidation.cs.meta.src'
runtime=project/'Validation/NativeLevelIdentityCommonEntryBatch2/Sources/FlowWaterRecoveryUI.cs.src'
runtime_text=runtime.read_text(encoding='utf-8-sig')
entry_body=runtime_text.split('void LoadGameInOriginalScene(int id)',1)[1].split('var game =',1)[0]
assert entry_body.strip()=='{\n        ControladorJuego.nivelActual = id;'
assert runtime_text.count('ControladorJuego.nivelActual = id;')==1
assert runtime_text.count('LoadGameInOriginalScene(')==3
source_text=source.read_text(encoding='utf-8-sig')
for prohibited in ['.StartGame(','.RefreshGame(','.CaptureBoardFrame(','.LoadLevel(','.Solve(','.Simulate(']:
    assert prohibited not in source_text, prohibited
assert 'Time.timeScale=' in source_text
write(stage/'compile-evidence.json',{
    'createdUtc':now,'unityVersion':'2022.3.28f1',
    'compiler':'D:/UnityHub/Editor/2022.3.28f1/Editor/Data/DotNetSdkRoslyn/csc.dll',
    'invocation':'dotnet.exe csc.dll @Validation/NativeDirectSceneTitleFixture1/editor.rsp',
    'exitCode':0,'compilerOutput':'Microsoft Visual C# 4.3.1-3.22526.13 (41a5af9d); no errors or warnings',
    'scope':'All 36 current Assets/Editor C# sources plus staged fixture. Existing Assembly-CSharp public APIs referenced. No Unity execution claimed.',
    'responseSha256':sha(stage/'editor.rsp'),'outputAssemblySha256':sha(stage/'editor.dll'),
    'runtimeReferenceSha256':sha(project/'Library/ScriptAssemblies/Assembly-CSharp.dll'),
    'sources':[{'path':p.relative_to(project).as_posix(),'sha256':sha(p)} for p in sorted(project.joinpath('Assets/Editor').rglob('*.cs'))]+[{'path':source.relative_to(project).as_posix(),'sha256':sha(source)}]
})
write(stage/'native-level-identity-independent-review.json',{
    'createdUtc':now,'reviewer':'direct_scene_title',
    'scope':'Read-only code-path audit, not actual Unity verdict',
    'reviewedSource':runtime.relative_to(project).as_posix(),'reviewedSha256':sha(runtime),
    'commonEntryFirstStatement':'ControladorJuego.nivelActual = id;',
    'entryCallSites':[
        {'method':'OnSceneLoaded','sourceLines':[146,153],
         'path':'Recovery.LastLevelId/pending selection -> catalog and scene validation -> fallback -> original pack -> common loader',
         'fallbacks':{'Levely5_basic':401,'Levely5_6x6':251,'Levely5_7x7':351,'Levely5':1}},
        {'method':'StartGame','sourceLines':[505,522],
         'path':'available catalog -> original pack -> same scene common loader; different scene pending ID -> SceneManager.LoadScene -> OnSceneLoaded common loader'}
    ],
    'readers':[
        {'path':'Assets/Scripts/Assembly-CSharp/Leveltexto2.cs','line':17,
         'expression':'ControladorJuego.nivelActual + original part offset','phase':'Start after sceneLoaded'},
        {'path':'Assets/Scripts/Assembly-CSharp/ControladorJuego.cs','line':11,'usage':'static field declaration'}
    ],
    'comparedWithOriginalPatch':'The combined final implementation adds exactly one global assignment at the first line of the common gameplay loader; the Batch1 StartGame-only assignment is removed by Batch2.',
    'mechanicsScope':'No actor, input, collider, water, physics or scheduler statements changed.',
    'lifecycleExpectation':'Native Leveltexto2.Start should now consume the current original asset ID for both UI navigation and direct Editor Play. This remains a source-level expectation until the real fixture finishes.',
    'actualVerified':False
})
files=[]
for target,path in [('Assets/Editor/RecoveredTools/FlowWaterDirectSceneTitleValidation.cs',source),('Assets/Editor/RecoveredTools/FlowWaterDirectSceneTitleValidation.cs.meta',meta)]:
    assert not (project/target).exists(),target
    files.append({'target':target,'source':path.relative_to(project).as_posix(),'previousSha256':None,'sha256':sha(path)})
write(stage/'manifest.json',{
    'createdUtc':now,'immutable':True,'runtimeChanges':False,'files':files,
    'entry':'Recovered.FlowWater.Editor.FlowWaterDirectSceneTitleValidation.Run',
    'reportDirectory':'Validation/DirectSceneTitle','reportFile':'direct-scene-title-validation.json','timeoutSeconds':900,
    'scope':'Eight independent fresh Editor Play lifecycles across all four original gameplay scenes. Edit mode OpenScene + EnterPlaymode only. No UI StartGame, RefreshGame, capture wrapper or game-state calls. Read original active Leveltexto2.texto1 after at least 16 actual Time.frameCount frames; require exactly one signed integer equal positive original local number, native global/current asset IDs, original pack, exact scene and Time 4.',
    'cases':[
        {'scene':'Assets/Scenes/Levely5_basic.unity','storedLastLevelId':421,'expectedAssetId':421,'expectedLocalNumber':21},
        {'scene':'Assets/Scenes/Levely5_basic.unity','storedLastLevelId':2050,'expectedAssetId':401,'expectedLocalNumber':1},
        {'scene':'Assets/Scenes/Levely5_6x6.unity','storedLastLevelId':282,'expectedAssetId':282,'expectedLocalNumber':32},
        {'scene':'Assets/Scenes/Levely5_6x6.unity','storedLastLevelId':2050,'expectedAssetId':251,'expectedLocalNumber':1},
        {'scene':'Assets/Scenes/Levely5_7x7.unity','storedLastLevelId':366,'expectedAssetId':366,'expectedLocalNumber':16},
        {'scene':'Assets/Scenes/Levely5_7x7.unity','storedLastLevelId':421,'expectedAssetId':351,'expectedLocalNumber':1},
        {'scene':'Assets/Scenes/Levely5z/Levely5.unity','storedLastLevelId':13,'expectedAssetId':13,'expectedLocalNumber':13},
        {'scene':'Assets/Scenes/Levely5z/Levely5.unity','storedLastLevelId':2050,'expectedAssetId':1,'expectedLocalNumber':1}
    ],
    'restoreScope':'Typed preferences (all catalog completion keys, startup preferences, language/helpRotation/Recovery.LastLevelId), full original SceneSetup, and final Edit-mode original time speed. SessionState report/FSM persists through each actual domain reload. All callbacks return immediately unless ownActive.',
    'sourcePatchDependency':{'manifest':'Validation/NativeLevelIdentityCommonEntryBatch2/manifest.json','runtimeUiSha256':sha(runtime)},
    'compileEvidenceSha256':sha(stage/'compile-evidence.json'),
    'independentReviewSha256':sha(stage/'native-level-identity-independent-review.json'),
    'actualStatus':'Staged and full Editor compile successful only. No Unity process launched, no actual cases sampled, no restoration pass claimed.',
    'coverageRestriction':'Independent direct scene title lifecycle suite; cannot substitute for ordinary native 800 or affected 169 movement regressions.'
})
print(json.dumps({'manifest':str(stage/'manifest.json'),'manifestSha256':sha(stage/'manifest.json'),'sourceSha256':sha(source),'editorAssemblySha256':sha(stage/'editor.dll')},ensure_ascii=False))
