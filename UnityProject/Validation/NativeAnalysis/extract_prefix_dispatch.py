"""Extract byte field assignments directly from original equality branches.

This small static extractor targets the idiomatic IL2CPP prefix dispatch blocks,
not arbitrary code. It records exact method addresses and reports unmapped stores.
"""
import json
import re
from pathlib import Path
import disassemble as d

out={}
for match in re.finditer(r'public class (\w+) :[^\n]+\n\{(.*?)\n\}', d.dump_text, re.S):
    cls, body=match.groups()
    d.fields[cls]={int(off,16):name for name,off in re.findall(r'(?:public|private) (?!static )[\w\[\]<>.]+ (\w+); // (0x[\da-fA-F]+)',body)}

for method in ['SaveLoadManager_doProcesoPiezas_10189CC.asm.txt','SaveLoadManager_doControlPieza_101E54C.asm.txt']:
    lines=(d.ROOT/'Methods'/method).read_text().splitlines()[2:]
    klass='ControlPieza'
    prefix=None
    for ix,line in enumerate(lines):
        a=int(line[:8],16)
        if a<0x1018e00 and 'doProceso' in method: continue
        component=re.search(r'Method\$UnityEngine.GameObject.GetComponent<(\w+)>',line)
        if component:
            klass=component[1]
        value=re.search(r"\] '([A-Z0-9])'",line)
        if value: prefix=value[1]
        if 'System.String$$op_Equality' not in line: continue
        if ix+1>=len(lines) or 'tbz' not in lines[ix+1]: continue
        end=int(re.search(r'#0x([a-f0-9]+)',lines[ix+1])[1],16)
        block=[l for l in lines[ix+2:] if int(l[:8],16)<end]
        constants={'w22':1,'x22':1,'wzr':0,'xzr':0}
        bases={'x20':0,'x25':0}
        assignments={}
        raw=[]
        for s in block:
            mov=re.search(r'mov\s+(w\d+), #(-?0x[\da-f]+|-?\d+)',s)
            if mov: constants[mov[1]]=int(mov[2],0)
            copy=re.search(r'mov\s+(w\d+), (w\d+)',s)
            if copy and copy[2] in constants: constants[copy[1]]=constants[copy[2]]
            add=re.search(r'add\s+(x\d+), (x20|x25), #0x([\da-f]+)',s)
            if add: bases[add[1]]=int(add[3],16)
            store=re.search(r'(strb|strh|sturh|str|stur)\s+(w\w+), \[(x\d+)(?:, #0x([\da-f]+))?\]',s)
            if store and store[2] in constants and store[3] in bases:
                off=bases[store[3]]+int(store[4] or '0',16)
                size=1 if store[1]=='strb' else 2 if store[1] in ['strh','sturh'] else 4
                val=constants[store[2]]
                for byte in range(size):
                    field=d.fields.get(klass,{}).get(off+byte,'offset_'+hex(off+byte))
                    assignments[field]=bool((val>>(8*byte))&255)
                raw.append(s)
        if assignments:
            out.setdefault(klass,{})[prefix]={'assignments':assignments,'equalityCallVA':hex(a),'nativeStoreInstructions':raw}

(d.ROOT/'all-component-prefix-dispatch.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),'utf-8')
(d.ROOT/'all-game-component-field-offsets.json').write_text(json.dumps({cls:{hex(off):field for off,field in d.fields[cls].items()} for cls in out},ensure_ascii=False,indent=2),'utf-8')
print(json.dumps({cls:{p:entry['assignments'] for p,entry in ps.items()} for cls,ps in out.items()},ensure_ascii=False,indent=2))
