import pathlib,shutil,json,hashlib,datetime,uuid
root=pathlib.Path.cwd();work=root/'Validation/FountainAnimationWork';batch=root/'Validation/NativeFountainAnimationBatch1'
if batch.exists():raise RuntimeError('Immutable batch exists; use a new version.')
files=[]
for p in(work/'Assets').rglob('*'):
 if not p.is_file():continue
 rel=p.relative_to(work);target=batch/rel;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(p,target)
 meta=root/(str(rel)+'.meta')
 if meta.exists():shutil.copyfile(meta,str(target)+'.meta')
 else:
  importer='MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}'if p.suffix=='.cs'else'TextScriptImporter:\n  externalObjects: {}'
  pathlib.Path(str(target)+'.meta').write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n'+importer+'\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8')
for n in['runtime.rsp','editor.rsp']:(batch/n).write_text((work/n).read_text(encoding='utf-8').replace('Validation/FountainAnimationWork','Validation/NativeFountainAnimationBatch1'),encoding='utf-8')
evidence=batch/'Evidence';evidence.mkdir()
for n in['native-fountain-animator-extraction-report.json','native-water-animator-recipes-audit.json']:
 shutil.copyfile(root/'Validation/NativeAnalysis'/n,evidence/n)
readme='''# Native fountain Animator batch1

This independent batch restores native Animator operations on the exact already-generated fountain instance. No water topology, physics, timing, counts or placement recipes change. Main Assets remain untouched until root imports this manifest.

## Original proof

The read-only ARM evaluator now tracks GetComponent<Animator> back to its exact Object.Instantiate object. It records SetInteger and enabled writes from doFuente1B/2B/1A/5A, with original call VAs. Existing placement-context/color/mode variants:2440 evaluations,0 extraction failures,7 native instance-owner VAs,23 final operation recipes. Repeated helper writes to the same reused object are compared by their final properties; no Animator.Update is called in those original helpers.

Examples: doFuente2B10AC710/760 assigns animcode2/3 and enables; doFuente1B10ABF2C/7C similarly; configAnim1 only enables; doFuente1A/5A additionally enable with other nonempty configuration values, according to exact extraction. No guessed animcode1/4 is assigned.

## Runtime chain

RecoveredNativeWaterAssets passes the existing recipe's nativeInstantiateVA plus actual configAnim into RecoveredNativeFountainAnimation.Bind. Only the exact original new object's root Animator is eligible; descendants are not searched. Only seven proved native owner VAs bind. For eligible instances with no native enabled operation, authored enabled state is preserved instead of WaterMotion globally enabling it.

RecoveredWaterMotion delegates those bound Animators to the helper. It restores authored/native enabled state, Rebinds, reapplies native integer parameters, then evaluates original motion at the retained source age. Applying before Rebind would lose parameters. Other original water/particle Animator behavior is unchanged. Unknown/synthetic clip paths are not renamed or fabricated.

## Actual Unity verification required

Entry: Recovered.FlowWater.Editor.FlowWaterFountainAnimationValidation.Run
Report: Validation/FountainAnimation/fountain-animation-validation.json

This opens actual1101 and1193, uses production Solve/final layout, waits for real all-target flow, finds actual post-water native generated fountains with configAnim3/2, asserts Animator integer survives Rebind, actual original controller transition state is reached, and actual local transform or particle velocity changes across real frames. It also checks unrelated Rebuild keeps the configured animation code. Preferences, scene setup and time scale restore on exit. Actual Unity execution is root-owned; compilation is not a pass claim.

## Separate known provider gap still under investigation

FT3.doInstanciarFuente10A684C..68A8 additionally calls doInstanciarChorros for type2 when configPit1 is nonempty and not5000/6000; doConfigPit invokes doPitA/B. The current renderer adapter has not restored that full decorative-pit provider, whose Animator operations are therefore not blindly applied to Geometry's different static hardware. This batch only covers the seven proved owner VAs already spawned by existing primary/border/center recipes. The original doPitA orphan path remains orphan; this patch does not claim it repaired.

Runtime and complete Editor compile with Unity2022.3.28f1 Roslyn, zero errors. No Unity process launched by this agent.
'''
(batch/'README.md').write_text(readme,encoding='utf-8')
manifest={'batch':'NativeFountainAnimationBatch1','immutable':True,'createdUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),
 'status':'Full runtime and Editor Roslyn0errors; actual original1101/1193 state/motion fixture pending root',
 'entry':'Recovered.FlowWater.Editor.FlowWaterFountainAnimationValidation.Run','files':[]}
for p in sorted((batch/'Assets').rglob('*')):
 if p.is_file():
  old=root/p.relative_to(batch)
  manifest['files'].append({'source':str(p.relative_to(root)).replace('\\','/'),'target':str(p.relative_to(batch)).replace('\\','/'),
    'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'previousSha256':hashlib.sha256(old.read_bytes()).hexdigest()if old.exists()else None})
(batch/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print('Immutable',manifest['batch'],len(manifest['files']),'Assets/meta files')
