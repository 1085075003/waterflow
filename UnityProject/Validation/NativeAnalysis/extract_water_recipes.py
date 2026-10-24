"""Narrow, read-only evaluation of the original fountain placement helpers.

Only numeric ARM64 operations, branches and a whitelist of Unity transform calls
are interpreted. This does not load or execute the recovered native application.
"""
import json, math, pathlib, re, struct
import disassemble as d

class Extractor:
    def __init__(self, inputs, class_name='FuenteTest3'):
        self.mem={}; self.refs={}; self.objects={}; self.calls=[]; self.trace=[]
        self.nextref=0x300000000; self.base=0x100000000
        self.fields=d.fields[class_name]; self.r={'sp':0x200000000,'xzr':0,'wzr':0,'x0':self.base}
        for off,name in self.fields.items():
            if name.startswith(('prefab','esfera','Chorro')) or name in ['Aguas','anim','esferita']:
                self.write(self.base+off,self.ref(name),8)
        for name,val in inputs.items():
            off=next(k for k,v in self.fields.items() if v==name)
            if isinstance(val,str):self.write(self.base+off,self.ref(val),8)
            elif isinstance(val,float):self.write(self.base+off,int.from_bytes(struct.pack('<f',val),'little'),4)
            else:self.write(self.base+off,val,4)
        self.compare=(0,0)
    def ref(self,value):
        p=self.nextref;self.nextref+=256;self.refs[p]=value;return p
    def write(self,addr,val,size):
        val=int(val)%(1<<(size*8))
        for i,b in enumerate(val.to_bytes(size,'little')):self.mem[addr+i]=b
    def read(self,addr,size):
        if addr in self.mem or self.base<=addr<self.base+4096 or 0x200000000<=addr<0x200100000:
            return int.from_bytes(bytes(self.mem.get(addr+i,0) for i in range(size)),'little')
        if addr in d.labels:
            label=d.labels[addr]
            if label.startswith("'"):
                import ast
                label=ast.literal_eval(label)
            return self.ref(label)
        if addr in self.refs:return addr
        if addr>=0x300000000:return 1
        return int.from_bytes(d.read_va(addr,size).ljust(size,b'\0'),'little')
    def get(self,name):
        if name in ['xzr','wzr']:return 0
        if name=='sp':return self.r.get(name,0)
        if name.startswith('w'):return self.r.get('x'+name[1:],0)&0xffffffff
        if name.startswith('s'):return struct.unpack('<f',struct.pack('<I',self.r.get('v'+name[1:],0)&0xffffffff))[0]
        if name.startswith('d'):return self.r.get('v'+name[1:],0)&0xffffffffffffffff
        return self.r.get(name,0)
    def put(self,name,value):
        if name in ['xzr','wzr']:return
        if name=='sp':self.r[name]=int(value);return
        if name.startswith('w'):self.r['x'+name[1:]]=int(value)&0xffffffff
        elif name.startswith('s'):self.r['v'+name[1:]]=int.from_bytes(struct.pack('<f',float(value)),'little')
        elif name.startswith('d'):self.r['v'+name[1:]]=int(value)&0xffffffffffffffff
        elif name.startswith('q'):self.r['v'+name[1:]]=int(value)
        else:self.r[name]=int(value)
    def reg(self,ins,op):return self.get(ins.reg_name(op.reg))
    def operand(self,ins,op):
        if op.type==1:return self.reg(ins,op)
        if op.type==2:return op.imm
        if op.type==4:return op.fp
        raise RuntimeError(('operand',ins.mnemonic,ins.op_str))
    def addr(self,ins,op):
        m=op.mem;value=self.get(ins.reg_name(m.base))+m.disp
        if m.index:value+=self.get(ins.reg_name(m.index))<<op.shift.value
        return value
    def condition(self,cond):
        a,b=self.compare
        return {'eq':a==b,'ne':a!=b,'gt':a>b,'ge':a>=b,'lt':a<b,'le':a<=b,'hi':a>b,'hs':a>=b,'lo':a<b,'ls':a<=b,'mi':a<b,'pl':a>=b}[cond]
    def floats(self,n,start=0):return [self.get('s'+str(i+start)) for i in range(n)]
    def call(self,address):
        name=d.method_map.get(address,{}).get('Name','')
        args=self.floats(7)
        if address in [0xe61b78,0xe61c80]:return
        if name=='UnityEngine.Quaternion$$Internal_FromEulerRad':
            x,y,z=args[:3];cx,sx=math.cos(x/2),math.sin(x/2);cy,sy=math.cos(y/2),math.sin(y/2);cz,sz=math.cos(z/2),math.sin(z/2)
            q=[cy*sx*cz+sy*cx*sz,sy*cx*cz-cy*sx*sz,cy*cx*sz-sy*sx*cz,cy*cx*cz+sy*sx*sz]
            for i,v in enumerate(q):self.put('s'+str(i),v)
        elif name=='UnityEngine.Object$$Instantiate<object>':
            field=self.refs.get(self.get('x0'),str(self.get('x0')))
            recipe={'prefabField':field,'initialPosition':args[:3],'quaternion':args[3:7],'nativeInstantiateVA':hex(self.pc)}
            obj=self.ref('instance'+str(len(self.calls)));self.objects[obj]=recipe;self.calls.append(recipe);self.put('x0',obj)
        elif name in ['UnityEngine.Component$$get_transform','UnityEngine.GameObject$$get_transform','UnityEngine.GameObject$$get_gameObject']:pass
        elif name.startswith('UnityEngine.Transform$$set_'):
            obj=self.objects.get(self.get('x0'))
            if obj is not None:
                field=name.split('$$set_')[1]
                if field=='parent':obj['parent']='self' if self.get('x1')==self.base else self.refs.get(self.get('x1'),self.get('x1'))
                else:obj[field]=args[:4] if 'Rotation' in field or field=='rotation' else args[:3]
        elif name=='UnityEngine.Transform$$get_eulerAngles':
            for i in range(3):self.put('s'+str(i),0)
        elif name=='UnityEngine.Transform$$get_position':
            for i in range(3):self.put('s'+str(i),0)
        elif name=='UnityEngine.Object$$op_Inequality':self.put('w0',int(self.get('x0')!=self.get('x1')))
        elif name in ['UnityEngine.GameObject$$SetActive','UnityEngine.GameObject$$set_tag','UnityEngine.Object$$set_name','UnityEngine.Object$$Destroy','UnityEngine.MonoBehaviour$$Invoke']:pass
        elif name=='UnityEngine.GameObject$$GetComponent<object>':self.put('x0',self.ref('component'))
        elif name in ['System.String$$op_Equality','System.String$$op_Inequality']:
            equal=self.refs.get(self.get('x0'))==self.refs.get(self.get('x1'))
            self.put('w0',int(equal if name.endswith('Equality') else not equal))
        elif name=='System.String$$Substring':
            value=self.refs.get(self.get('x0'),'');start=self.get('w1');length=self.get('w2');self.put('x0',self.ref(value[start:start+length]))
        elif name=='<PrivateImplementationDetails>$$ComputeStringHash':
            h=2166136261
            for c in self.refs.get(self.get('x0'),''):h=((h^ord(c))*16777619)&0xffffffff
            self.put('w0',h)
        elif name.startswith('FuenteTest3$$doFuente') or name.startswith('FuenteTest3$$doConfig') or name.startswith('FuenteTest3$$doCrearChorro') or name.startswith('FuenteTest3$$doPit'):
            preserved={k:self.r.get(k,0) for k in ['x'+str(i) for i in range(19,30)]+['v'+str(i) for i in range(8,16)]+['sp']}
            self.run(name);self.r.update(preserved)
        elif address==0xe61da0:raise RuntimeError('Native null failure')
        else:raise RuntimeError(('call',name,hex(address),hex(self.pc)))
    def run(self,name):
        method=next(m for m in d.data['ScriptMethod'] if m['Name']==name)
        savedpc=getattr(self,'pc',0);pc=method['Address'];end=d.addresses[d.addresses.index(pc)+1]
        for step in range(10000):
            self.pc=pc;self.trace.append(hex(pc));ins=list(d.md.disasm(d.read_va(pc,4),pc))[0];n=ins.mnemonic;ops=ins.operands
            nextpc=pc+4
            if n in ['ret']:break
            elif n in ['adrp','adr']:self.put(ins.reg_name(ops[0].reg),ops[1].imm)
            elif n in ['mov','fmov']:
                dst,src=ins.op_str.split(', ',1)
                if '.16b' in dst:self.r['v'+dst[1:].split('.')[0]]=self.r.get('v'+src[1:].split('.')[0],0)
                elif src.startswith('#') and dst.startswith('v'):
                    count=int(dst.split('.')[1][:-1]);unit=dst[-1]
                    packed=struct.pack('<f'if unit=='s'else'<d',float(src[1:]))*count
                    self.r[dst.split('.')[0]]=int.from_bytes(packed,'little')
                elif src.startswith('#'):
                    value=float(src[1:]) if dst.startswith('s') else int.from_bytes(struct.pack('<d',float(src[1:])),'little') if dst.startswith('d') else int(src[1:],0)
                    self.put(dst,value)
                elif dst.startswith('s') and src.startswith('w'):self.r['v'+dst[1:]]=self.get(src)
                elif dst.startswith('w') and src.startswith('s'):self.put(dst,self.r.get('v'+src[1:],0))
                else:self.put(dst,self.get(src))
            elif n=='movk':
                dst=ins.reg_name(ops[0].reg);shift=ops[1].shift.value;self.put(dst,(self.get(dst)&~(0xffff<<shift))|(ops[1].imm<<shift))
            elif n=='movi':
                dst,src=ins.op_str.split(', ',1);self.r['v'+dst[1:].split('.')[0]]=int(src.lstrip('#'),0)
            elif n in ['ldr','ldur','ldrb','ldrh','ldrsw']:
                dst=ins.reg_name(ops[0].reg);size=1 if n=='ldrb' else 2 if n=='ldrh' else 16 if dst.startswith('q') else 8 if dst.startswith(('x','d')) else 4
                value=self.read(self.addr(ins,ops[1]),size)
                if dst.startswith('s'):self.r['v'+dst[1:]]=value
                else:self.put(dst,value)
            elif n in ['str','stur','strb','strh']:
                src=ins.reg_name(ops[0].reg);size=1 if n=='strb' else 2 if n=='strh' else 16 if src.startswith('q') else 8 if src.startswith(('x','d')) else 4
                value=self.r.get('v'+src[1:],0) if src.startswith(('s','q')) else self.get(src)
                self.write(self.addr(ins,ops[1]),value,size)
            elif n in ['stp','ldp']:
                # D/X callee-save pairs are preserved by the call wrapper. S-register
                # stack pairs are actual helper data (e.g. C108B948/108BA24),
                # not register saves, and must participate in memory loads/stores.
                addr=self.addr(ins,ops[2]);src=ins.reg_name(ops[0].reg);size=16 if src.startswith('q')else 8 if src.startswith(('x','d')) else 4
                if ins.reg_name(ops[2].mem.base)!='sp' or src.startswith('s'):
                    for j in range(2):
                        reg=ins.reg_name(ops[j].reg)
                        if n=='ldp':
                            val=self.read(addr+size*j,size)
                            if reg.startswith(('s','q')):self.r['v'+reg[1:]]=val
                            else:self.put(reg,val)
                        else:self.write(addr+size*j,self.r.get('v'+reg[1:],0) if reg.startswith(('s','q')) else self.get(reg),size)
            elif n in ['add','sub','fadd','fsub','fmul','fdiv']:
                dst=ins.reg_name(ops[0].reg);a=self.operand(ins,ops[1]);b=self.operand(ins,ops[2]);b=b*(1<<ops[2].shift.value)
                val=a+b if n in ['add','fadd'] else a-b if n in ['sub','fsub'] else a*b if n=='fmul' else a/b
                self.put(dst,val)
            elif n in ['and','orr','eor']:
                a=self.operand(ins,ops[1]);b=self.operand(ins,ops[2]);self.put(ins.reg_name(ops[0].reg),a&b if n=='and' else a|b if n=='orr' else a^b)
            elif n in ['cmp','fcmp']:self.compare=(self.operand(ins,ops[0]),self.operand(ins,ops[1]))
            elif n=='tst':self.compare=(self.operand(ins,ops[0])&self.operand(ins,ops[1]),0)
            elif n in ['lsl','lsr']:
                value=self.operand(ins,ops[1]);shift=self.operand(ins,ops[2]);self.put(ins.reg_name(ops[0].reg),value<<shift if n=='lsl' else value>>shift)
            elif n=='cset':self.put(ins.reg_name(ops[0].reg),int(self.condition(ins.op_str.split(', ')[-1])))
            elif n in ['csel','fcsel']:
                self.put(ins.reg_name(ops[0].reg),self.operand(ins,ops[1] if self.condition(ins.op_str.split(', ')[-1]) else ops[2]))
            elif n in ['cbz','cbnz']:
                cond=self.reg(ins,ops[0])==0
                if cond==(n=='cbz'):nextpc=ops[1].imm
            elif n in ['tbz','tbnz']:
                cond=(self.reg(ins,ops[0])&(1<<ops[1].imm))==0
                if cond==(n=='tbz'):nextpc=ops[2].imm
            elif n.startswith('b.'):
                if self.condition(n[2:]):nextpc=ops[0].imm
            elif n=='br':nextpc=self.reg(ins,ops[0])
            elif n=='bl':self.call(ops[0].imm)
            elif n=='b':
                target=ops[0].imm
                if pc<=target<end or method['Address']<=target<end:nextpc=target
                else:self.call(target);break
            elif n=='dup':
                dst,src=ins.op_str.split(', ');bits=self.get(src)&0xffffffff;self.r['v'+dst[1:].split('.')[0]]=bits|(bits<<32)
            elif n=='fneg':self.put(ins.reg_name(ops[0].reg),-self.reg(ins,ops[1]))
            elif n=='nop':pass
            else:raise RuntimeError((hex(pc),n,ins.op_str))
            pc=nextpc
        else:raise RuntimeError('Exceeded instruction limit')
        self.pc=savedpc
        return self.calls

if __name__=='__main__':
    results={};failures={}
    names=[m['Name'] for m in d.data['ScriptMethod'] if m['Name'].startswith('FuenteTest3$$doFuente') and not m['Name'].endswith('5')]
    for name in names:
        cases=[]
        for color in [1,2,3,4]:
            for center in [0,8,9]:
                inputs={'color':color,'configFuente':1,'configFuenteCentro':center,'tipoPieza':2,'modofuente':0,'modofuente3':0,'rotay':0}
                try:
                    ex=Extractor(inputs);calls=ex.run(name);cases.append({'inputs':inputs,'recipes':calls})
                except Exception as e:failures[name]=str(e);break
            if name in failures:break
        if name not in failures:results[name]=cases
    (d.ROOT/'native-water-fountain-recipes.json').write_text(json.dumps({'methods':results,'unsupported':failures},ensure_ascii=False,indent=2),'utf-8')
    print(json.dumps({'success':list(results),'unsupported':failures},ensure_ascii=False,indent=2))
