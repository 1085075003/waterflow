"""Convert statically derived original demo calls into JsonUtility-readable arrays."""
from pathlib import Path
import json

base=Path(__file__).resolve().parent
poses=json.loads((base/'preview-demo-motion-derived.json').read_text(encoding='utf-8'))
colors={x['type']:x['cycles'] for x in json.loads((base/'preview-demo-colors-derived.json').read_text(encoding='utf-8'))}
result=[]
for source in poses:
    entry={'type':source['type'],'evidence':source['method'],'positions':[], 'rotations':[], 'active':[], 'vectors':source['vectors'], 'motions':source['motions'], 'cycles':[]}
    for call in source['calls']:
        if 'setLocalPosition' in call:
            entry['positions'].append({'target':call['setLocalPosition'][1][1], 'value':call['value']})
        if 'rotation' in call:
            # Both original Resorte2 OnEnable rotations are local identity.
            entry['rotations'].append({'target':call['rotation'][1][1], 'value':call['values'][0][1][1:]})
        if 'activeTarget' in call:
            entry['active'].append({'target':call['activeTarget'][1], 'enabled':bool(call['value'])})
    for motion in entry['motions']:
        if motion['rotationAngle'] is None:motion['rotationAngle']=0
        # The forward native comparison deliberately includes the 360-degree
        # Euler wrap; reverse compares against .01 degrees. Do not guess it.
        motion['rotationEpsilon']=360.010009765625 if motion['rotation'] and not motion['method'].endswith('b') else .009999999776482582
    for cycle in colors.get(source['type'],[]):
        out={'cycle':cycle['cycle'],'values':[],'actions':cycle['actions']}
        for value in cycle['values']:
            typed={k:value[k] for k in ['target','field','type']}
            typed[{'Color':'rgba','int':'number','string':'text'}[value['type']]]=value['value']
            out['values'].append(typed)
        entry['cycles'].append(out)
    result.append(entry)
output=base.parents[1]/'Assets/Resources/NativePreviewDemo.json'
output.write_text(json.dumps({'entries':result},ensure_ascii=False,separators=(',',':')),encoding='utf-8')
print(output,output.stat().st_size)
