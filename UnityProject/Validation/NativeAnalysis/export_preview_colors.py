from pathlib import Path
import re,sys,json,struct,ast
sys.path.insert(0,str(Path(__file__).resolve().parent));import disassemble as d
blocks={m[1]:m[2] for m in re.finditer(r'public class (\w+) :[^\n]+\n\{(.*?)\n\}',d.dump_text,re.S)}
def fields(cls,static=False):
 patt=r'(?:public|private) '+('static ' if static else '')+r'([\w\[\]<>.]+) (\w+); // (0x[\da-fA-F]+)'
 return {int(off,16):(typ,name) for typ,name,off in re.findall(patt,blocks.get(cls,'')) if static or typ!='static'}
allfields={c:fields(c) for c in blocks};allstatic={c:fields(c,True) for c in blocks}
def run(cls,vez):
 method=next(x for x in d.data['ScriptMethod'] if x['Name']==cls+'$$doConfigureColors');ix=d.addresses.index(method['Address']);ins=list(d.md.disasm(d.read_va(method['Address'],d.addresses[ix+1]-method['Address']),method['Address']));byaddr={i.address:n for n,i in enumerate(ins)}
 x={'x0':('ref','',0,cls),'x19':('ref','',0,cls),'xzr':0,'wzr':0};v={i:[0.,0.,0.,0.] for i in range(32)};mem={};actions=[];cmp=0;cmpa=0;cmpb=0;pc=0;steps=0
 def val(r):
  if r.startswith(('s','d','q','v')):return v[int(r.split('.')[0][1:])][0]
  return x.get(r,x.get('x'+r[1:],0))
 def literal(a):
  label=d.labels.get(a,'')
  if label.startswith("'") or label.startswith('"'):
   try:return ast.literal_eval(label)
   except:return None
  return None
 def resolve(base,off):
  if isinstance(base,int):return base+off
  if isinstance(base,tuple) and base[0]=='ref':return ('ref',base[1],base[2]+off,base[3])
  if isinstance(base,tuple) and base[0]=='meta' and off==0xb8:return ('static',base[1])
  return base
 def pointer(arg):
  z=re.search(r'\[(x\d+)(?:, #(-?0x[0-9a-f]+|-?\d+))?(?:, (w\d+), sxtw #(\d+))?\]',arg)
  if not z:raise RuntimeError(arg)
  off=int(z[2],0) if z[2] else 0
  if z[3]:off+=int(val(z[3]))<<int(z[4])
  return resolve(val(z[1]),off)
 def load(a,kind):
  if isinstance(a,int):
   label=d.labels.get(a,'')
   if kind in 'sqd':return list(struct.unpack('<2I',d.read_va(a,8))) if kind=='d' else (list(struct.unpack('<4f',d.read_va(a,16))) if kind=='q' else struct.unpack('<f',d.read_va(a,4))[0])
   lit=literal(a)
   if lit is not None:return ('literal',lit)
   if label.endswith('_TypeInfo'):return ('meta',label[:-9])
   if kind=='w':
    blob=d.read_va(a,4);return struct.unpack('<i',blob)[0] if len(blob)==4 else 0
   return a
  if isinstance(a,tuple) and a[0]=='literal':return a[1]
  if isinstance(a,tuple) and a[0]=='meta':return a
  if isinstance(a,tuple) and a[0]=='static':return ('ref','$static.'+a[1],0,a[1])
  if not (isinstance(a,tuple) and a[0]=='ref'):return 0
  _,path,off,typ=a;key=(path,off)
  if key in mem:
   z=mem[key]
   return struct.unpack('<f',struct.pack('<I',z[1]&0xffffffff))[0] if isinstance(z,tuple) and z[0]=='bits' and kind=='s' else z
  f=(allstatic if path.startswith('$static') else allfields).get(typ,{}).get(off)
  if not f:return 0
  ft,fn=f
  if fn=='vez':return vez
  if kind=='w':return 0
  if kind=='s':return 0.
  return ('ref',(path+'.' if path else '')+fn,0,ft)
 def store(a,value,kind='s'):
  if not(isinstance(a,tuple) and a[0]=='ref'):return
  _,path,off,typ=a
  if kind=='d':
   for k,z in enumerate(value):mem[(path,off+k*4)]=('bits',z)
  elif kind=='q':
   for k,z in enumerate(value):mem[(path,off+k*4)]=z
  else:mem[(path,off)]=('bits',int(value)&0xffffffff) if kind=='w' and isinstance(value,int) else value
 while pc<len(ins) and steps<4000:
  i=ins[pc];pc+=1;steps+=1;mn=i.mnemonic;op=i.op_str;p=[s.strip() for s in op.split(',')]
  if mn=='ret':break
  if mn in ['adr','adrp']:x[p[0]]=int(p[1].lstrip('#'),0)
  elif mn in ['add','sub'] and p[0].startswith(('x','w')):
   a=val(p[1]);b=int(p[2].lstrip('#'),0) if p[2].startswith('#') else val(p[2]);z=resolve(a,b) if mn=='add' else a-b
   x[p[0]]=z;x['x'+p[0][1:]]=z
  elif mn=='mov' and p[0].startswith(('x','w')):
   z=int(p[1].lstrip('#'),0) if p[1].startswith('#') else val(p[1]);x[p[0]]=z;x['x'+p[0][1:]]=z
  elif mn=='movk':
   z=val(p[0]);shift=int(p[2].split('#')[1]) if len(p)>2 else 0;z=(z&~(0xffff<<shift))|(int(p[1].lstrip('#'),0)<<shift);x[p[0]]=z;x['x'+p[0][1:]]=z
  elif mn=='mov' and p[0].startswith('v'):v[int(p[0].split('.')[0][1:])]=v[int(p[1].split('.')[0][1:])][:]
  elif mn=='fmov':
   a=p[1];z=float(a[1:]) if a.startswith('#') else val(a)
   if a.startswith(('w','x')) and isinstance(z,int):z=struct.unpack('<f',struct.pack('<I',z&0xffffffff))[0]
   v[int(p[0][1:])][0]=z
  elif mn in ['ldr','ldrb','ldrh','ldur']:
   if '[sp' in op:continue
   a=pointer(op);dst=p[0];kind=dst[0];z=load(a,kind)
   if kind in 'sqd':
    if kind in 'qd':v[int(dst[1:])]=z
    else:v[int(dst[1:])][0]=z
   else:x[dst]=z;x['x'+dst[1:]]=z
  elif mn in ['str','stur','strb','strh']:
   if '[sp' in op:continue
   a=pointer(op);r=p[0];store(a,v[int(r[1:])][:] if r.startswith(('q','d')) else val(r),r[0])
  elif mn=='stp' and '[sp' not in op:
   a=pointer(op)
   for n,r in enumerate(p[:2]):store(resolve(a,n*(16 if r[0]=='q' else 4)),v[int(r[1:])][:] if r[0]=='q' else val(r),r[0])
  elif mn in ['cmp','cmn']:
   a=val(p[0]);b=int(p[1][1:],0) if p[1].startswith('#') else val(p[1]);cmpa=a;cmpb=b;cmp=(a-b) if mn=='cmp' else a+b
  elif mn in ['tbnz','tbz']:
   if mn=='tbnz':pc=byaddr[int(p[-1].lstrip('#'),0)]
  elif mn in ['cbz','cbnz']:
   z=val(p[0]);take=(not z) if mn=='cbz' else bool(z)
   if take:pc=byaddr[int(p[-1].lstrip('#'),0)]
  elif mn.startswith('b.'):
   cond=mn[2:];take={'eq':cmp==0,'ne':cmp!=0,'hi':(cmpa&0xffffffff)>(cmpb&0xffffffff),'gt':cmp>0,'lt':cmp<0,'le':cmp<=0,'ge':cmp>=0,'ls':(cmpa&0xffffffff)<=(cmpb&0xffffffff)}.get(cond,False)
   if take:pc=byaddr[int(op.lstrip('#'),0)]
  elif mn in ['bl','b']:
   a=int(op.lstrip('#'),0);name=d.method_map.get(a,{}).get('Name','')
   if mn=='b' and a in byaddr:pc=byaddr[a];continue
   if name.endswith('GameObject$$SetActive'):actions.append({'kind':'active','target':val('x0')[1],'enabled':bool(val('w1'))})
   elif name.endswith('Material$$set_color'):actions.append({'kind':'material','target':val('x0')[1],'color':[v[k][0] for k in range(4)]})
   elif name.endswith('Object$$Destroy'):actions.append({'kind':'destroy','target':val('x0')[1]})
   if mn=='b':break
 # Material colors already captured; export typed controller/child/static fields.
 values=[]
 refs={('',cls)}
 for path,off in mem:
  if path:
   if path.startswith('$static.'):refs.add((path,path.split('.')[1]))
   else:
    parent=allfields[cls];base=next((typ for _,(typ,name) in parent.items() if name==path),None)
    if base:refs.add((path,base))
 for path,typ in refs:
  fmap=(allstatic if path.startswith('$static.') else allfields).get(typ,{})
  for off,(ft,fn) in fmap.items():
   if (path,off) not in mem or fn=='vez':continue
   raw=mem[(path,off)]
   if ft=='Color':
    value=[load(('ref',path,off+k*4,typ),'s') for k in range(4)]
   else:
    value=raw[1] if isinstance(raw,tuple) and raw[0] in ['bits','literal'] else raw
    if isinstance(value,tuple) and value[0]=='literal':value=value[1]
   values.append({'target':path,'field':fn,'type':ft,'value':value})
 return {'cycle':vez,'values':values,'actions':actions,'steps':steps}
report=[]
for cls in ['Resorte2b','Resorte2c','Resorte2d']:
 cases=[run(cls,n) for n in [1,2,3]];report.append({'type':cls,'cycles':cases});print(cls,[(x['cycle'],len(x['values']),len(x['actions']),x['steps']) for x in cases])
Path('Validation/NativeAnalysis/preview-demo-colors-derived.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
