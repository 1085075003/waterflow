import json,re,pathlib,struct,sys
sys.path.insert(0,'UnityProject/Validation/NativeAnalysis')
from extract_water_recipes import Extractor
import disassemble as d
class JetExtractor(Extractor):
 def __init__(self,inputs):
  self.components={};super().__init__(inputs)
 def read(self,a,n):
  if a>=0x300000000 and a in self.mem:return int.from_bytes(bytes(self.mem.get(a+j,0) for j in range(n)),'little')
  return super().read(a,n)
 def call(self,a):
  name=d.method_map.get(a,{}).get('Name','')
  if name=='UnityEngine.Object$$op_Equality':self.put('w0',int(self.get('x0')==self.get('x1')));return
  if name=='UnityEngine.GameObject$$GetComponent<object>':
   meta=self.refs.get(self.get('x1'),'');match=re.search(r'GetComponent<([^>]+)>',str(meta));typ=match.group(1) if match else '?';owner=self.get('x0');ptr=self.ref('component:'+typ);self.components[ptr]=(typ,owner);self.put('x0',ptr);return
  return super().call(a)
 def records(self):
  for ptr,(typ,owner) in self.components.items():
   record=self.objects.get(owner)
   if record is None or typ not in d.fields:continue
   types=dict((name,kind) for kind,name in re.findall(r'(?:public|private)\s+(\w+)\s+(\w+)\s*;',pathlib.Path('UnityProject/Assets/Scripts/Assembly-CSharp/'+typ+'.cs').read_text('utf-8-sig')))
   fields=[]
   for off,name in d.fields[typ].items():
    if ptr+off not in self.mem:continue
    kind=types.get(name)
    if kind not in ['string','int','bool','float']:continue
    value=self.read(ptr+off,8 if kind=='string' else 1 if kind=='bool' else 4)
    if kind=='string':value=self.refs.get(value,'')
    if kind=='float':value=struct.unpack('<f',value.to_bytes(4,'little'))[0]
    fields.append({'name':name,'kind':kind,'text':str(value) if kind=='string' else '', 'number':value if kind!='string' else 0})
   record.setdefault('components',[]).append({'type':typ,'fields':fields})
  return self.calls
if __name__=='__main__':
 catalog=json.load(open('UnityProject/Assets/Resources/RecoveredNativeGeometry.json'));configs=set()
 for v in catalog['entries']:
  s=v['configuration'];m=re.search('sr(.*?)er',s)
  if m:
   text=m.group(1)
   for i in range(6,len(text)-3,4):configs.add(text[i:i+4])
 results=[];fail=[]
 for config in sorted(configs):
  for color in [1,2,3,4]:
   args={'color':color,'tipoPieza':9,'origen':5,'configFuente':1,'configPit1':config,'configPit2':'','configPit3':'','configPit4':'','configRes1':'','configRes2':'','configRes3':'','configRes4':'','configAnim':'','configLi':'','rotay':0.0}
   try:
    ex=JetExtractor(args);ex.run('FuenteTest3$$doActivarChorro');results.append({'configuration':config,'color':color,'recipes':ex.records()})
   except Exception as e:fail.append([config,color,str(e)])
 out={'method':'FuenteTest3.doActivarChorro 0x10A72E4','entries':results,'unsupported':fail}
 pathlib.Path('UnityProject/Validation/NativeAnalysis/native-jet-activation-recipes.json').write_text(json.dumps(out,indent=2),'utf-8')
 print(json.dumps({'configurations':len(configs),'success':len(results),'recipes':sum(len(v['recipes']) for v in results),'failures':fail[:10]},indent=2))
