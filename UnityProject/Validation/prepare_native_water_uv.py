import datetime, difflib, hashlib, json, pathlib, shutil, uuid

project = pathlib.Path(__file__).resolve().parent.parent
batch = project / 'Validation/NativeWaterUV1'
target = 'Assets/Scripts/RecoveryRuntime/RecoveredWaterMotion.cs'
original = (project/target).read_text(encoding='utf-8-sig')
needle = '''                surfaces.Add(new Surface { renderer = renderer, speed = new Vector2(original.scrollSpeed_X, original.scrollSpeed_Y),
                    offset = material.mainTextureOffset + new Vector2(original.offsetInicialX, original.offsetInicialY), scale = material.mainTextureScale });'''
replacement = '''                // Original scrollTest1.Start1060C98 and Update1060CC0:
                // Start resolves Y; Update replaces, rather than adds to, the material offset.
                if (original.tipoOffset == 1) original.offsetInicialY = 0f;
                else if (original.tipoOffset == 2) original.offsetInicialY = .5f;
                surfaces.Add(new Surface { renderer = renderer, speed = new Vector2(original.scrollSpeed_X, original.scrollSpeed_Y),
                    offset = new Vector2(original.offsetInicialX, original.offsetInicialY), scale = material.mainTextureScale });'''
assert original.count(needle) == 1
updated = original.replace(needle, replacement)
path = batch/target
path.parent.mkdir(parents=True,exist_ok=True)
path.write_text(updated,encoding='utf-8',newline='')
shutil.copyfile(project/(target+'.meta'),batch/(target+'.meta'))
(batch/'water-motion.diff').write_text(''.join(difflib.unified_diff(original.splitlines(True),updated.splitlines(True),fromfile=target+' (frozen base)',tofile=target+' (native UV only)')),encoding='utf-8')
for name in ['scrollTest1_Start_1060C98.asm.txt','scrollTest1_Update_1060CC0.asm.txt']:
    directory=batch/'evidence';directory.mkdir(exist_ok=True)
    shutil.copyfile(project/'Validation/NativeAnalysis/Methods'/name,directory/name)
manifest={'preparedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),
          'assetsModified':False,'frozenSource':'FinalSource-20261016T043531611160Z',
          'files':[{'target':target,'baseSha256':hashlib.sha256((project/target).read_bytes()).hexdigest(),
                    'staged':str(path.relative_to(project)).replace('\\','/'),'stagedSha256':hashlib.sha256(path.read_bytes()).hexdigest()}],
          'runtimeScope':'UV-only: native initial offset including tipoOffset Start rules; all graph, physics, colliders, actors, schedulers, geometry, palette and render shader code unchanged.',
          'testEntry':'Recovered.FlowWater.Editor.FlowWaterWaterUVValidation.Run',
          'report':'Validation/WaterUV/native-water-uv-validation.json',
          'compile':'pending'}
(batch/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(manifest,ensure_ascii=True,indent=2))
