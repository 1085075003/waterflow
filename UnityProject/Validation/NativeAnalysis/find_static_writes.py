import json,re,pathlib
import disassemble as d
classes={p.stem for p in (d.ROOT.parents[1]/'Assets/Scripts/Assembly-CSharp').glob('*.cs')}
target='UIManagerScript'
static_fields={}
block=d.dump_text.split('public class '+target+' :')[1].split('// Methods')[0]
for typ,name,off in re.findall(r'(?:public|private) static ([\w<>\[\]]+) (\w+); // 0x([0-9A-Fa-f]+)',block):static_fields[int(off,16)]=name
records=[]
for method in d.data['ScriptMethod']:
    if method['Name'].split('$$')[0] not in classes:continue
    va=method['Address']
    if va not in d.addresses:continue
    ix=d.addresses.index(va)
    if ix+1>=len(d.addresses):continue
    length=d.addresses[ix+1]-va
    if length>200000:continue
    regs={}
    for ins in d.md.disasm(d.read_va(va,length),va):
        ops=ins.operands;n=ins.mnemonic
        def key(reg):return ins.reg_name(reg).replace('w','x',1)
        def readop(op):return regs.get(key(op.reg)) if op.type==1 else op.imm if op.type==2 else None
        if n in ['str','strb','strh','stur','stp'] and len(ops)>1:
            mem=ops[-1].mem;base=regs.get(key(mem.base))
            if base==('static',target):
                off=mem.disp
                if off in static_fields:
                    records.append({'method':method['Name'],'va':hex(ins.address),'field':static_fields[off],'offset':hex(off),'instruction':n+' '+ins.op_str})
        out=None
        if n in ['adrp','adr']:out=ops[1].imm
        elif n=='mov':out=readop(ops[1])
        elif n=='add' and ops[2].type==2 and isinstance(readop(ops[1]),int):out=readop(ops[1])+ops[2].imm
        elif n in ['ldr','ldur'] and ops[1].type==3:
            mem=ops[1].mem;base=regs.get(key(mem.base));off=mem.disp
            if isinstance(base,int):
                label=d.labels.get(base+off,'')
                if label==target+'_TypeInfo':out=('got',target)
            elif base==('got',target) and off==0:out=('class',target)
            elif base==('class',target) and off==0xb8:out=('static',target)
        if ops and ops[0].type==1 and n not in ['str','strb','strh','stur','stp','cmp','fcmp','tst','cbz','cbnz','tbz','tbnz','b','br']:
            if out is None:regs.pop(key(ops[0].reg),None)
            else:regs[key(ops[0].reg)]=out
        if n=='bl' and ops[0].imm not in [0xe61b78,0xe61c80]:
            for i in range(19):regs.pop('x'+str(i),None)
(d.ROOT/'native-ui-static-writes.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),'utf-8')
print(json.dumps([r for r in records if r['field'].startswith('positionRes')],ensure_ascii=False,indent=2))
