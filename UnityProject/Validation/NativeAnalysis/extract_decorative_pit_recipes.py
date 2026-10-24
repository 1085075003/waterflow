"""Read-only original FT3 decorative-pit instances and exact post-spawn mutations.
No Unity execution or Assets edits; unresolved native operations fail explicitly.
"""
import pathlib,json,struct,sys
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parent))
from extract_water_recipes import Extractor
from extract_water_animator_recipes import inputs_for
import disassemble as d

class PitExtractor(Extractor):
 def __init__(self,inputs):
  self.components={};self.nodes={};self.last_constant=None;self.modules={}
  super().__init__(inputs)
 def read(self,addr,size):
  if any(addr+i in self.mem for i in range(size)):
   return int.from_bytes(bytes(self.mem.get(addr+i,0) for i in range(size)),'little')
  return super().read(addr,size)
 def write(self,addr,val,size):
  for ptr,component in getattr(self,'components',{}).items():
   if ptr<=addr<ptr+256:
    fields=d.fields.get(component['kind'],{});field=fields.get(addr-ptr)
    if field is None:raise RuntimeError(('unknown component field',component,hex(addr-ptr),hex(getattr(self,'pc',0))))
    self.mutate(component['owner'],{'kind':'componentField','component':component['kind'],'field':field,'value':int(val)})
    break
  super().write(addr,val,size)
 def mutate(self,owner,op):
  node=self.nodes.get(owner)
  if node is None:raise RuntimeError(('unresolved mutated object',owner,op,hex(self.pc)))
  root,path=node;self.objects[root].setdefault('operations',[]).append({'childIndices':path,'nativeVA':hex(self.pc),**op})
 def call(self,address):
  name=d.method_map.get(address,{}).get('Name','')
  if name=='UnityEngine.Object$$Instantiate<object>':
   super().call(address);ptr=self.get('x0');self.nodes[ptr]=(ptr,[]);return
  if name=='UnityEngine.GameObject$$GetComponent<object>':
   owner=self.get('x0');kind=str(self.refs.get(self.get('x1'),''))
   kind=kind.split('<')[-1].split('>')[0]
   ptr=self.ref('component');self.components[ptr]={'owner':owner,'kind':kind};self.put('x0',ptr);return
  if name in ['UnityEngine.Component$$get_transform','UnityEngine.Component$$get_gameObject']:
   component=self.components.get(self.get('x0'))
   if component:self.put('x0',component['owner'])
   return
  if name=='UnityEngine.Transform$$GetChild':
   parent=self.get('x0');node=self.nodes.get(parent)
   if node is None:raise RuntimeError(('unknown child parent',parent,hex(self.pc)))
   root,path=node;ptr=self.ref('child');self.nodes[ptr]=(root,path+[self.get('w1')]);self.put('x0',ptr);return
  if name=='UnityEngine.Animator$$SetInteger':
   component=self.components.get(self.get('x0'))
   if component is None or component['kind']!='Animator':raise RuntimeError(('unresolved Animator',component,hex(self.pc)))
   self.mutate(component['owner'],{'kind':'animatorInteger','parameter':self.refs.get(self.get('x1')),'value':self.get('w2')});return
  if name=='UnityEngine.Behaviour$$set_enabled':
   component=self.components.get(self.get('x0'))
   if component is None:raise RuntimeError(('unresolved Behaviour',hex(self.pc)))
   self.mutate(component['owner'],{'kind':'componentEnabled','component':component['kind'],'value':bool(self.get('w1'))});return
  if name=='UnityEngine.Light$$set_color':
   component=self.components.get(self.get('x0'))
   if component is None or component['kind']!='Light':raise RuntimeError(('unresolved Light',component,hex(self.pc)))
   self.mutate(component['owner'],{'kind':'lightColor','value':self.floats(4)});return
  if name in ['UnityEngine.Light$$set_intensity','UnityEngine.Light$$set_range','UnityEngine.Light$$set_spotAngle']:
   component=self.components.get(self.get('x0'))
   if component is None or component['kind']!='Light':raise RuntimeError(('unresolved Light',component,hex(self.pc)))
   self.mutate(component['owner'],{'kind':'lightFloat','field':name.split('$$set_')[1],'value':self.get('s0')});return
  if name=='UnityEngine.ParticleSystem$$get_velocityOverLifetime':
   component=self.components.get(self.get('x0'))
   if component is None or component['kind']!='ParticleSystem':raise RuntimeError(('unresolved ParticleSystem',component,hex(self.pc)))
   ptr=self.ref('velocityModule');self.modules[ptr]=component['owner'];self.put('x0',ptr);return
  if name=='UnityEngine.ParticleSystem.MinMaxCurve$$op_Implicit':self.last_constant=self.get('s0');return
  if name.startswith('UnityEngine.ParticleSystem.VelocityOverLifetimeModule$$set_'):
   module=self.read(self.get('x0'),8);owner=self.modules.get(module)
   if owner is None or self.last_constant is None:raise RuntimeError(('unresolved module',module,hex(self.pc)))
   self.mutate(owner,{'kind':'velocityConstant','field':name.split('$$set_')[1],'value':self.last_constant});return
  if name=='System.Int32$$TryParse':
   value=self.refs.get(self.get('x0'),'')
   try:n=int(value);ok=True
   except ValueError:n=0;ok=False
   self.write(self.get('x1'),n,4);self.put('w0',int(ok));return
  if name=='UnityEngine.GameObject$$SetActive':
   self.mutate(self.get('x0'),{'kind':'active','value':bool(self.get('w1'))});return
  if name=='UnityEngine.Object$$op_Equality':self.put('w0',int(self.get('x0')==self.get('x1')));return
  return super().call(address)

if __name__=='__main__':
 codes=json.loads(pathlib.Path(__file__).with_name('native-actual-decorative-pit-codes.json').read_text(encoding='utf-8'))
 rows=[];fail=[]
 for code in codes:
  values=inputs_for('primary',0);values['night']=0
  try:
   ex=PitExtractor(values);ex.put('x1',ex.ref(code));ex.run('FuenteTest3$$doConfigPit');rows.append({'code':code,'recipes':ex.calls})
  except Exception as error:fail.append({'code':code,'error':str(error)})
 output={'scope':'Original ARM64 read-only decorative FT3 pit provider; color1/empty lights prototype cases. Actual context/color/light expansion pending.','cases':rows,'failures':fail}
 pathlib.Path(__file__).with_name('native-decorative-pit-prototype-audit.json').write_text(json.dumps(output,ensure_ascii=False,indent=2),encoding='utf-8')
 print('cases',len(rows),'failures',len(fail),'withOperations',sum(any(r.get('operations')for r in row['recipes'])for row in rows));print(json.dumps(fail[:10]))
