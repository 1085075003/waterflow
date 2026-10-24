import pathlib,hashlib,json,difflib,shutil,datetime

project=pathlib.Path(__file__).resolve().parent.parent
native=project/'Validation/NativeAnalysis'
batch=project/'Validation/NativeGeometryStack1'
tools=batch/'Tools';tools.mkdir(parents=True,exist_ok=True)
old=(native/'extract_water_recipes.py').read_text(encoding='utf-8-sig')
new=old.replace("if self.base<=addr<self.base+4096 or 0x200000000<=addr<0x200100000:",
                "if addr in self.mem or self.base<=addr<self.base+4096 or 0x200000000<=addr<0x200100000:")
new=new.replace('# Stack saves/restores do not affect the helper result. Field pairs do.',
'''# D/X callee-save pairs are preserved by the call wrapper. S-register
                # stack pairs are actual helper data (e.g. C108B948/108BA24),
                # not register saves, and must participate in memory loads/stores.''')
new=new.replace("if ins.reg_name(ops[2].mem.base)!='sp':","if ins.reg_name(ops[2].mem.base)!='sp' or src.startswith('s'):")
assert new!=old
(tools/'extract_water_recipes.py').write_text(new,encoding='utf-8')
shutil.copyfile(native/'extract_native_geometry.py',tools/'extract_native_geometry.py')
generator=(native/'generate_geometry_catalog.py').read_text(encoding='utf-8-sig')
generator="import sys,pathlib\nsys.path.append("+repr(str(native.resolve()))+")\n"+generator
generator=generator.replace("(d.ROOT/'RecoveredNativeGeometry.json.ready')","(pathlib.Path(__file__).resolve().parents[1]/'regenerated-geometry.json')").replace("(d.ROOT/'native-geometry-extraction-report.json')","(pathlib.Path(__file__).resolve().parents[1]/'regeneration-report.json')")
(tools/'generate_geometry_catalog.py').write_text(generator,encoding='utf-8')
(batch/'extractor.diff').write_text(''.join(difflib.unified_diff(old.splitlines(True),new.splitlines(True),fromfile='Validation/NativeAnalysis/extract_water_recipes.py base',tofile='shared native VM stack fix')),encoding='utf-8')
evidence=batch/'evidence';evidence.mkdir(exist_ok=True)
shutil.copyfile(native/'Methods/ControlPiezaC_doConfigFuenteB_108B590.asm.txt',evidence/'ControlPiezaC_doConfigFuenteB_108B590.asm.txt')
manifest={'preparedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'assetsModified':False,'runtimeCSharpChanges':False,
 'extractor':{'target':'Validation/NativeAnalysis/extract_water_recipes.py','baseSha256':hashlib.sha256((native/'extract_water_recipes.py').read_bytes()).hexdigest(),'staged':str((tools/'extract_water_recipes.py').relative_to(project)).replace('\\','/'),'stagedSha256':hashlib.sha256((tools/'extract_water_recipes.py').read_bytes()).hexdigest()},
 'scope':'Pending full C geometry re-extraction and semantic diff; no broad visual-only claim before field/collider inventory is known.',
 'status':'regenerating'}
(batch/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print(batch)
