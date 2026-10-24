"""Export verified native configuration facts (not gameplay implementation)."""
import csv
import json
import re
from collections import Counter
from pathlib import Path
import disassemble as d

root = Path(__file__).resolve().parent
def write(name, obj):
    (root / name).write_text(json.dumps(obj, ensure_ascii=False, indent=2), 'utf-8')

fixed = 'Y23456789CDEFGHIK'
texture = {'2':2,'3':3,'4':2,'5':3,'6':4,'7':4,'8':5,'9':5,
           'A':2,'B':3,'C':6,'D':6,'E':7,'F':7,'G':8,'H':8,'I':9,'K':9}
flag = {'2':2,'3':3,'4':2,'5':3,'6':5,'7':5,'8':6,'9':6,
        'A':2,'B':3,'C':7,'D':7,'E':8,'F':8,'G':11,'H':11,'I':12,'K':12}
prefixes = {}
for p in ['N','Y'] + list('23456789ABCDEFGHIJK'):
    if p in 'J':
        continue
    flags = {}
    if p in fixed:
        flags['esPiezaFija'] = True
    if p in flag:
        flags['esTextura' + str(flag[p])] = True
    if p in '456789CDEFGHIK':
        flags['esTextura4'] = p in '4579DFHK'
    prefixes[p] = {'controlPiezaAssignments': flags,
                   'fixed': p in fixed,
                   'materialField': ('materialTextura'+str(texture[p])) if p in texture else ('materialFijo' if p=='Y' else None),
                   'plants': p in '4579DFHK',
                   'evidence': 'SaveLoadManager.doControlPieza VA 0x101E54C; ControlPieza.Start VA 0x107F720'}
write('fixed-prefix-map.json', {'scope':'Verified for ControlPieza; other component types require their own dispatch mapping.',
       'notes':['N makes no flag assignments. Prefab serialized flags are retained.',
                'esTextura4 controls plant generation; it is not a fourth material selector.',
                'materialField mapping applies when texturaS is empty; texturaS overrides only for fixed blocks.'],
       'prefixes':prefixes})

write('field-offsets.json', {cls:{hex(k):v for k,v in fs.items()} for cls,fs in d.fields.items()})

entries=[]
for field,begin,end,length in [
    ('resorte','st','et',None),('tipoResorte','sz','ez',None),
    ('valEncima','sv','ev',1),('valMove','sw','ew',1),('semiCil','sm','em',1),
    ('tipoPlant','sd','ed',1),('hole1','<a','a>',1),('hole2','<b','b>',1),
    ('hole3','<c','c>',1),('hole4','<d','d>',1),('hole6','<e','e>',1),
    ('door1','<f','f>',1),('door2','<g','g>',1),('door3','<h','h>',1),
    ('door4','<k','k>',1),('door6','<l','l>',1),('conResorte3','<r','r>',None),
    ('valDebajo','<s','s>',None),('modoRes3','<t','t>',None),('conTipoRes3','<v','v>',None),
    ('longPlant','<p','>p',1),('configCho1','<m','m>',None),('configCho2','<n','n>',None),
    ('configAnim','<y','y>',1),('configTexture','<z','z>',1),('confColor','<q','q>',2),
    ('configLights','<j','j>',2),('walls','sr','er',None),('conF','sk','ek',None)]:
    entries.append({'field':field,'start':begin,'end':end,'firstCharactersOnly':length,
                    'evidence':'SaveLoadManager.doRescatarFijos VA 0x101CB48'})
write('configuration-tokens.json', {
    'tokens':entries,
    'special':{'fijo1':'First character of fijo.',
               'str1..str6':'j/u/n/i/o/p followed by one character -> corresponding conOrigen1..6; j ignored when immediately preceded by <.',
               'configRes1..4':'<x...x> split into four-character chunks.',
               'sr...er':'First 4 characters parsed separately as wall1..4; characters 5 and 6 parsed as configFuente and configFuenteCentro; rest split into configPit1..4.',
               'sh...eh':'Four characters parsed separately as palo1..4.',
               'confColor':'If payload length==1, preserve one; else preserve first 2.',
               'configTexture':'Passed to ControlPieza.texturaS; fixed block values 1..7 select materialTextura10..16.',
               'sm...em':'semiCil; actual application must be read from piece-type dispatch.',
               'sc...ec':'Board base modifications handled in doRescatarConfig; 3-character tuples.'}})

scene_masks={'Levely5_6x6':0x66ea62c8c0,'Levely5_basic':0x1104102600,'Levely5_7x7':0x8810811100}
parts=[]
for ix,value in enumerate(d.read_va(0x6215fd,39),1):
    branch=0x103d27c+value*4
    ins=list(d.md.disasm(d.read_va(branch,4),branch))[0]
    if ins.mnemonic=='add':
        offset=ins.operands[2].imm
    elif branch==0x103d398:
        offset=0
    else:
        offset=None
    scene=next((scene for scene,mask in scene_masks.items() if mask&(1<<ix)),'Levely5')
    parts.append({'parte':ix,'assetOffset':offset,'firstAsset':None if offset is None else offset+1,
                  'lastAsset':None if offset is None else offset+50,'scene':scene})
write('native-scene-map.json',{'evidence':['UIManagerSelScript.Level VA 0x103D208, jump table VA 0x6215FD',
      'UIManagerSelScript.doCallGame VA 0x103DDF8'],'parts':parts})

rows=list(csv.DictReader((root.parents[2]/'LevelData/fixed_configuration_values.csv').open(encoding='utf-8-sig')))
counts=Counter()
for r in rows:
    counts[r['configuration'][:1]]+=int(r['occurrences'])
write('configuration-state-summary.json',{'uniqueStates':len(rows),'prefixOccurrences':dict(counts),
      'unmappedPrefixes':{p:n for p,n in counts.items() if p not in prefixes}})
print(json.dumps({'parts':parts,'stateSummary':dict(counts)},ensure_ascii=False))
