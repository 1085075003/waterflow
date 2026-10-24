"""Evaluate constant assignments from recognized confColor equality branches.

This is a narrow static evaluator, not an emulator capable of executing the app.
It only accepts constant loads, moves, stores, and unconditional jumps.
"""
import json,re,struct
import disassemble as d

path=d.ROOT/'Methods/ControlPiezaC_doConfigureColors_1088ED4.asm.txt'
lines=path.read_text().splitlines()[2:]
code={int(l[:8],16):l for l in lines}
output={}
for ix,line in enumerate(lines):
    if 'System.String$$op_Equality' not in line:continue
    value=next((re.search(r"\] '(\d+)'",x)[1] for x in reversed(lines[max(0,ix-8):ix]) if re.search(r"\] '(\d+)'",x)),None)
    if value is None:continue
    pc=int(line[:8],16)+8
    r={'wzr':0,'xzr':0,'x19':0x100000000}
    memory={}
    trace=[]
    for step in range(100):
        if pc==0x108992c:break
        ins=list(d.md.disasm(d.read_va(pc,4),pc))[0]
        trace.append(hex(pc))
        op=ins.op_str
        if ins.mnemonic=='b':
            pc=ins.operands[0].imm;continue
        elif ins.mnemonic=='adrp':r[op.split(',')[0]]=ins.operands[1].imm
        elif ins.mnemonic=='movk':
            dest=op.split(',')[0]
            shift=ins.operands[1].shift.value
            r[dest]=(r[dest]&~(0xffff<<shift))|(ins.operands[1].imm<<shift)
        elif ins.mnemonic in ['mov','fmov']:
            dest,src=op.split(', ',1)
            dest=re.sub(r'^v(\d+)\.16b$',r's\1',dest)
            src=re.sub(r'^v(\d+)\.16b$',r's\1',src)
            if src.startswith('#'):
                r[dest]=float(src[1:]) if dest.startswith('s') else int(src[1:],0)
            elif dest.startswith('s') and src.startswith('w'):
                r[dest]=struct.unpack('<f',struct.pack('<I',r[src]))[0]
            else:r[dest]=r[src]
        elif ins.mnemonic=='ldr':
            dest=op.split(',')[0]
            addr=ins.operands[1].mem
            base=ins.reg_name(addr.base)
            if base=='x19':r[dest]=0
            else:
                va=r[base]+addr.disp
                r[dest]=struct.unpack('<f',d.read_va(va,4))[0]
        elif ins.mnemonic=='str':
            src=op.split(',')[0]
            addr=ins.operands[1].mem
            off=addr.disp
            if src.startswith('s'):memory[off]=r[src]
            else:memory[off]=r[src] if off==0x158 else struct.unpack('<f',struct.pack('<I',r[src]))[0]
        elif ins.mnemonic=='cmp':pass
        else:raise RuntimeError((hex(pc),ins.mnemonic,ins.op_str))
        pc+=4
    else:raise RuntimeError('Exceeded 100 constant assignment instructions')
    output[value]={'color1':[memory[0x410+i*4] for i in range(4)],
                   'color2':[memory[0x420+i*4] for i in range(4)],
                   'color3':[memory[0x430+i*4] for i in range(4)],
                   'confC':memory[0x158],'monochromeWalls':bool(r.get('w20',0)),
                   'equalityCallVA':line[:8],'constantAssignmentTrace':trace}
(d.ROOT/'native-confColor-palette.json').write_text(json.dumps(output,ensure_ascii=False,indent=2),'utf-8')
print(json.dumps({k:{kk:vv for kk,vv in v.items() if kk!='constantAssignmentTrace'} for k,v in output.items()},ensure_ascii=False,indent=2))
