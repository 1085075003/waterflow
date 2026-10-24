"""Read exported prefab UV fields; never modifies Unity assets."""
import collections, hashlib, json, pathlib, re

project = pathlib.Path(__file__).resolve().parents[2]
assets = project / 'Assets'
guid = '41bbf1fe8eb9d9077086e49783962cdb'
materials = {}
for meta in (assets / 'Material').glob('*.mat.meta'):
    found = re.search(r'^guid: (\w+)', meta.read_text(encoding='utf-8-sig'), re.M)
    if found:
        materials[found[1]] = meta.with_suffix('')

rows = []
for path in sorted((assets / 'GameObject').glob('*.prefab')):
    source = path.read_text(encoding='utf-8-sig')
    if guid not in source:
        continue
    blocks = {int(fid): body for fid, body in re.findall(r'^--- !u!\d+ &(\d+)\n(.*?)(?=^--- !u!|\Z)', source, re.M | re.S)}
    for file_id, body in blocks.items():
        if guid not in body:
            continue
        def number(field):
            match = re.search(r'^  ' + field + r': ([\d.eE+-]+)', body, re.M)
            return float(match[1]) if match else None
        go = re.search(r'm_GameObject: \{fileID: (\d+)\}', body)
        go_body = blocks.get(int(go[1]), '') if go else ''
        name = re.search(r'^  m_Name: (.*)', go_body, re.M)
        rend = re.search(r'^  rend: \{fileID: (\d+)\}', body, re.M)
        renderer = blocks.get(int(rend[1]), '') if rend else ''
        refs = re.findall(r'guid: (\w+)', renderer)
        mlist = [materials[g] for g in refs if g in materials]
        material_offset = []
        for mat in mlist:
            text = mat.read_text(encoding='utf-8-sig')
            match = re.search(r'_MainTex:\s*\n.*?m_Offset: \{x: ([\d.eE+-]+), y: ([\d.eE+-]+)\}', text, re.S)
            if match:
                material_offset.append({'material': mat.name, 'offset': [float(match[1]), float(match[2])]})
        mode, y = number('tipoOffset'), number('offsetInicialY')
        native_y = 0.0 if mode == 1 else 0.5 if mode == 2 else y
        rows.append({'prefab': path.name, 'componentFileId': file_id, 'object': name[1] if name else '',
                     'tipoOffset': mode, 'offsetInicialX': number('offsetInicialX'), 'offsetInicialY': y,
                     'nativeStartY': native_y, 'serializedStartYDiffers': y != native_y,
                     'materialOffsets': material_offset, 'scrollSpeed': [number('scrollSpeed_X'), number('scrollSpeed_Y')]})

result = {'scope': 'Exported GameObject prefabs, static field comparison; not a runtime gm2001 diagnosis.',
          'nativeEvidence': ['scrollTest1_Start_1060C98.asm.txt', 'scrollTest1_Update_1060CC0.asm.txt'],
          'nativeUV': 'offset=(offsetInicialX, Start-resolved offsetInicialY)+speed*Time.time; material initial offset is not added.',
          'runtimeUV': 'material.mainTextureOffset+serialized offsetInicial+speed*Time.time',
          'runtimeSHA256': hashlib.sha256((assets/'Scripts/RecoveryRuntime/RecoveredWaterMotion.cs').read_bytes()).hexdigest(),
          'componentCount': len(rows), 'startModeCounts': dict(collections.Counter(str(r['tipoOffset']) for r in rows)),
          'startYMismatchCount': sum(r['serializedStartYDiffers'] for r in rows),
          'nonzeroMaterialOffsetRows': sum(any(m['offset'] != [0,0] for m in r['materialOffsets']) for r in rows),
          'rows': rows}
out = pathlib.Path(__file__).with_name('water-uv-static-audit.json')
out.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k != 'rows'},ensure_ascii=True,indent=2))
print('Mismatch prefab counts:', dict(collections.Counter(r['prefab'] for r in rows if r['serializedStartYDiffers'])))
