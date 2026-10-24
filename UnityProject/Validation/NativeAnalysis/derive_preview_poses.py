from pathlib import Path
import re,sys,struct,json,math
sys.path.insert(0,'Validation/NativeAnalysis');import disassemble as d
out=[]
for cls in ['Resorte2','Resorte2b','Resorte2c','Resorte2d']:
 m=next(x for x in d.data['ScriptMethod'] if x['Name']==cls+'$$OnEnable');idx=d.addresses.index(m['Address']);ins=list(d.md.disasm(d.read_va(m['Address'],d.addresses[idx+1]-m['Address']),m['Address']))
 x={'x0':'self','x19':'self','xzr':0,'wzr':0};s={i:0 for i in range(32)};mem={};calls=[];poses={};rots={}
 def expr(op,a,b):
  if isinstance(a,(int,float)) and isinstance(b,(int,float)):return {'+':lambda:a+b,'-':lambda:a-b,'*':lambda:a*b,'/':lambda:a/b}[op]()
  return [op,a,b]
 def reg(k):return s.get(int(k[1:]),['?',k]) if k[0] in 'svd' else x.get(k,x.get('x'+k[1:],['?',k]))
 def address(arg):
  z=re.search(r'\[(x\d+)(?:, #(-?0x[0-9a-f]+|-?\d+))?\]',arg)
  if not z:return None,None
  base=x.get(z[1]);off=int(z[2],0) if z[2] else 0;return base,off
 for i in ins:
  mn,op=i.mnemonic,i.op_str;p=[t.strip() for t in op.split(',')]
  if mn in ['adr','adrp']:x[p[0]]=int(p[1].lstrip('#'),0)
  elif mn=='mov' and p[0].startswith(('x','w')):
   try:x[p[0]]=int(p[1].lstrip('#'),0)
   except:x[p[0]]=reg(p[1])
   x[('x' if p[0][0]=='w' else 'w')+p[0][1:]]=x[p[0]]
  elif mn=='movk':
   old=x.get(p[0],0);val=int(p[1].lstrip('#'),0);shift=int(p[2].split('#')[1]) if len(p)>2 else 0
   x[p[0]]=(old&~(0xffff<<shift))|(val<<shift);x['x'+p[0][1:]]=x[p[0]]
  elif mn=='mov' and p[0].startswith('v'):s[int(p[0].split('.')[0][1:])]=reg(p[1].split('.')[0])
  elif mn=='movi':s[int(p[0].split('.')[0][1:])]=0
  elif mn=='fmov':
   dst=p[0];src=p[1]
   if src.startswith('#'):v=float(src[1:])
   elif src.startswith(('s','d')):v=reg(src)
   else:
    raw=reg(src);v=struct.unpack('<f',struct.pack('<I',raw&0xffffffff))[0] if isinstance(raw,int) else ['bits',raw]
   if dst[0] in 'sd':s[int(dst[1:])]=v
  elif mn in ['fadd','fsub','fmul','fdiv']:s[int(p[0][1:])]=expr({'fadd':'+','fsub':'-','fmul':'*','fdiv':'/'}[mn],reg(p[1]),reg(p[2]))
  elif mn in ['ldr','ldrb','ldrh']:
   base,off=address(op)
   if p[0][0] in 'sd':
    if base=='self':v=mem.get(off,['fieldfloat',hex(off)])
    elif isinstance(base,int):v=struct.unpack('<f',d.read_va(base+off,4))[0]
    else:v=['?',op]
    s[int(p[0][1:])]=v
   else:
    if base=='self':v=['field',d.fields[cls].get(off,hex(off))]
    elif isinstance(base,int):v=base+off
    else:v=['?',op]
    x[p[0]]=v
  elif mn=='ldp' and p[0][0]=='s':
   base,off=address(op)
   for k,r in enumerate(p[:2]):s[int(r[1:])]=mem.get(off+k*4,['fieldfloat',hex(off+k*4)]) if base=='self' else ['?',op]
  elif mn in ['str','stur'] and p[0][0]=='s':
   base,off=address(op)
   if base=='self':mem[off]=reg(p[0])
  elif mn=='stp' and p[0][0]=='s':
   base,off=address(op)
   if base=='self':
    for k,r in enumerate(p[:2]):mem[off+k*4]=reg(r)
  elif mn in ['bl','b']:
   try:a=int(op.lstrip('#'),0)
   except:continue
   name=d.method_map.get(a,{}).get('Name','')
   if name.endswith('GameObject$$get_transform'):x['x0']=['transform',x.get('x0')]
   elif name.endswith('Transform$$get_localPosition'):
    obj=x.get('x0');key=str(obj);v=poses.get(key,[['position',obj,c] for c in ['x','y','z']]);s.update(dict(enumerate(v)))
   elif name.endswith('Transform$$get_localEulerAngles') or name.endswith('Transform$$get_eulerAngles'):
    obj=x.get('x0');s.update({k:['euler',obj,c] for k,c in enumerate('xyz')})
   elif name.endswith('Transform$$set_localPosition'):
    obj=x.get('x0');v=[s[k] for k in range(3)];poses[str(obj)]=v;calls.append({'address':hex(i.address),'setLocalPosition':obj,'value':v})
   elif name.endswith('Quaternion$$Internal_FromEulerRad'):
    q=['eulerRadians',s[0],s[1],s[2]]
    for k in range(4):s[k]=['quaternion',q,k]
   elif name.endswith('Transform$$set_localRotation') or name.endswith('Transform$$set_rotation'):
    calls.append({'address':hex(i.address),'rotation':x.get('x0'),'values':[s[k] for k in range(4)]})
   elif name.endswith('GameObject$$SetActive'):calls.append({'address':hex(i.address),'activeTarget':x.get('x0'),'value':x.get('w1',x.get('x1'))})
   elif name.endswith('MonoBehaviour$$Invoke'):calls.append({'address':hex(i.address),'invokeLiteralAddress':x.get('x1'),'delay':s[0]})
   elif 'GetComponent' in name:x['x0']=['getComponent',x.get('x0'),name]
 block=next(z[2] for z in re.finditer(r'public class (\w+) :[^\n]+\n\{(.*?)\n\}',d.dump_text,re.S) if z[1]==cls)
 vectors=[]
 for field,off in re.findall(r'(?:public|private) Vector3 (\w+); // (0x[\da-fA-F]+)',block):
  off=int(off,16)
  if off in mem:vectors.append({'field':field,'value':[mem.get(off+k*4) for k in range(3)]})
 out.append({'type':cls,'method':hex(m['Address']),'calls':calls,'vectors':vectors})
Path('Validation/NativeAnalysis/preview-demo-onenable-derived.json').write_text(json.dumps(out,indent=2),encoding='utf-8')
for a in out:
 print(a['type']);print('CALLS',a['calls']);print('VECTORS',a['vectors'])
