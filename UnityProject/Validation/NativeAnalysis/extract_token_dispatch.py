import json,re
import disassemble as d

result={}
for method in ['SaveLoadManager_doProcesoPiezas_10189CC.asm.txt','SaveLoadManager_doControlPieza_101E54C.asm.txt','SaveLoadManager_doPiezaNormal_101E39C.asm.txt']:
    lines=(d.ROOT/'Methods'/method).read_text().splitlines()[2:]
    klass='ControlPieza'
    sources={}
    targetreg='x20' if 'doControlPieza_' in method or 'doPiezaNormal_' in method else 'x25'
    for line in lines:
        a=int(line[:8],16)
        if a<0x1018e00 and 'doProceso' in method: continue
        component=re.search(r'Method\$UnityEngine.GameObject.GetComponent<(\w+)>',line)
        if component: klass=component[1]
        load=re.search(r'ldr\s+(x\d+|q\d+|d\d+), \[x19, #0x([\da-f]+)\]',line)
        if load:
            sources[load[1]]=int(load[2],16)
        else:
            overwrite=re.search(r'(?:ldr|mov|add)\s+(x\d+|q\d+|d\d+),',line)
            if overwrite: sources.pop(overwrite[1],None)
        store=re.search(r'(?:str|stur)\s+(x\d+|q\d+|d\d+), \['+targetreg+r', #0x([\da-f]+)\]',line)
        if not store or store[1] not in sources: continue
        src=sources[store[1]]
        dst=int(store[2],16)
        stride=4 if src+4 in d.fields['SaveLoadManager'] else 8
        width=16 if store[1].startswith('q') else 8
        count=width//stride
        for i in range(count):
            source=d.fields['SaveLoadManager'].get(src+i*stride,'offset_'+hex(src+i*stride))
            target=d.fields.get(klass,{}).get(dst+i*stride,'offset_'+hex(dst+i*stride))
            result.setdefault(klass,{})[source]={'field':target,'storeVA':hex(a),'nativeStoreInstruction':line}
for i in range(1,5):
    result['ControlPiezaC']['wall'+str(i)]={'field':'lado'+str(i),'storeVA':'0x101ac90',
        'nativeStoreInstruction':'0101AC88 ldr q0,[x8] (x8=self.wall1 via saved sp+8); 0101AC8C add x8,x25,#0x25c; 0101AC90 str q0,[x8]'}
for src,dst,va in [('configFuente','configFuente',0x101ac98),('configFB','configFuenteB',0x101aca0),
                   ('configFuenteCentro','configFuenteCentro',0x101acb0)]:
    result['ControlPiezaC'][src]={'field':dst,'storeVA':hex(va),'nativeStoreInstruction':'Verified direct int load/store at '+hex(va)}
(d.ROOT/'token-component-field-dispatch.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),'utf-8')
print(json.dumps(result,ensure_ascii=False,indent=2))
