import sys,json,struct,pathlib
sys.path.insert(0,'UnityProject/Validation/NativeAnalysis');from extract_water_recipes import Extractor
import disassemble as d
class Motion(Extractor):
 def __init__(self,args,cls):super().__init__(args,cls);self.props=[];self.constant=0
 def call(self,a):
  n=d.method_map.get(a,{}).get('Name','')
  if n=='UnityEngine.Component$$get_gameObject':return
  if n=='UnityEngine.Transform$$GetChild':self.put('x0',self.ref('child'));return
  if n=='UnityEngine.ParticleSystem.MinMaxCurve$$op_Implicit':self.constant=self.get('s0');return
  if n.startswith('UnityEngine.ParticleSystem$$get_'):self.put('x0',self.ref('module'));return
  if n=='UnityEngine.ParticleSystem$$set_startLifetime':self.props.append({'module':'MainModule','field':'startLifetime','value':self.get('s0')});return
  if n.startswith('UnityEngine.ParticleSystem.') and '$$set_' in n:
   self.props.append({'module':n.split('.')[2].split('$$')[0],'field':n.split('$$set_')[1],'value':self.constant});return
  return super().call(a)
results=[];fail=[]
for mode,name in [(1,'doControlNormalLargo'),(2,'doControlNormalCorto'),(3,'doControlNormalCorto1')]:
 for height in range(-4,6):
  try:
   ex=Motion({'altura':height,'altura1':height,'color':1,'color_old':0,'rotacion':0.0,'noHayChorro':1},'CascadaTestE');ex.run('CascadaTestE$$'+name);results.append({'mode':mode,'height':height,'properties':ex.props});print(mode,height,ex.props)
  except Exception as e:fail.append([mode,height,str(e)])
pathlib.Path('UnityProject/Validation/NativeAnalysis/native-jet-particle-properties.json').write_text(json.dumps({'cases':results,'unsupported':fail},indent=2),'utf-8');print('FAIL',fail)
