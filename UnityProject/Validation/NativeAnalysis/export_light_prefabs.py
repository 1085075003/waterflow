"""Statically resolve straight-line doLight helpers to prefab transform recipes."""
import json,re,struct,math
import disassemble as d

recipes={}
for path in (d.ROOT/'Methods').glob('*_doLight[1-5]_*.asm.txt'):
    cls=path.name.split('_doLight')[0]
    lines=path.read_text().splitlines()[2:]
    registers={'x19':'self','wzr':0,'xzr':0}
    def key(name):return re.sub(r'^v(\d+)\.16b$',r's\1',name)
    def value(name):return registers.get(key(name),'unknown:'+name)
    def neg(a):return -a if isinstance(a,(int,float)) else {'negate':a}
    current=None
    currentEuler=None
    calls=[]
    for line in lines:
        pc=int(line[:8],16)
        ins=list(d.md.disasm(d.read_va(pc,4),pc))[0]
        name,op=ins.mnemonic,ins.op_str
        if name=='adrp':registers[op.split(',')[0]]=ins.operands[1].imm
        elif name in ['mov','fmov']:
            dst,src=op.split(', ',1)
            if src.startswith('#'):
                registers[key(dst)]=float(src[1:]) if dst.startswith('s') else int(src[1:],0)
            elif dst.startswith('s') and src.startswith('w'):
                val=value(src)
                registers[key(dst)]=struct.unpack('<f',struct.pack('<I',val&0xffffffff))[0] if isinstance(val,int) else val
            else:registers[key(dst)]=value(src)
        elif name=='fneg':
            dst,src=op.split(', ')
            registers[dst]=neg(value(src))
        elif name=='ldr':
            dst=op.split(',')[0]
            mem=ins.operands[1].mem
            base=ins.reg_name(mem.base)
            if base=='x19':registers[dst]='self.'+d.fields[cls].get(mem.disp,hex(mem.disp))
            elif dst.startswith('s') and isinstance(value(base),int):
                registers[dst]=struct.unpack('<f',d.read_va(value(base)+mem.disp,4))[0]
            else:registers[dst]='unknown_load'
        elif name in ['bl','b']:
            target=d.method_map.get(ins.operands[0].imm,{}).get('Name','')
            if target=='UnityEngine.Quaternion$$Internal_FromEulerRad':
                currentEuler=[value('s'+str(i)) for i in range(3)]
                for i in range(4):registers['s'+str(i)]={'quaternion':currentEuler,'element':i}
            elif target=='UnityEngine.Object$$Instantiate<object>':
                current={'prefabField':value('x0'),'instantiateVA':hex(pc),'initialPosition':[value('s'+str(i)) for i in range(3)],
                         'eulerDegrees':[round(v*180/math.pi,6) if isinstance(v,(int,float)) else v for v in currentEuler]}
                calls.append(current)
            elif target=='UnityEngine.Transform$$set_localPosition' and current:
                current['localPosition']=[value('s'+str(i)) for i in range(3)]
            elif target=='UnityEngine.Transform$$set_localScale' and current:
                current['localScale']=[value('s'+str(i)) for i in range(3)]
        elif name=='str' and current and op.startswith('x0, [x19,'):
            current['instanceField']='self.'+d.fields[cls].get(ins.operands[1].mem.disp,hex(ins.operands[1].mem.disp))
    if cls=='ControlFijo1' and calls:
        # A later GetComponent<Light> is stored as myLight; it is not the
        # instantiated GameObject. These five helpers also alter intensity.
        number=int(re.search(r'_doLight([1-5])_',path.name)[1])
        calls[0]['instanceField']='self.Lights'+str(1 if number==5 else number)
        calls[0]['lightIntensityOverride']={'conditionField':'lig1','equals':'5' if number==5 else '6','value':9 if number==5 else 8,'lightField':'myLight'}
    recipes[path.name.split('_'+path.name.split('_')[-1])[0]]=calls

(d.ROOT/'native-light-prefab-recipes.json').write_text(json.dumps(recipes,ensure_ascii=False,indent=2),'utf-8')
print(json.dumps(recipes.get('ControlPieza_doLight1'),ensure_ascii=False,indent=2))
