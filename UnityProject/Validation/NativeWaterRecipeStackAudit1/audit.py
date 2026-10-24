import pathlib,sys,importlib.util,json,collections,datetime,hashlib
workspace=pathlib.Path(__file__).resolve().parents[3]
native=workspace/'UnityProject/Validation/NativeAnalysis'
fixed=workspace/'UnityProject/Validation/NativeGeometryStack1/Tools/extract_water_recipes.py'
out=pathlib.Path(__file__).resolve().parent
sys.path.insert(0,str(native))
spec=importlib.util.spec_from_file_location('extract_water_recipes',fixed)
module=importlib.util.module_from_spec(spec);sys.modules['extract_water_recipes']=module;spec.loader.exec_module(module)
from extract_water_recipes import Extractor
from extract_water_animator_recipes import inputs_for
from extract_decorative_pit_recipes import PitExtractor
surface_ns={'__file__':str(native/'extract_native_planes.py')}
exec((native/'extract_native_planes.py').read_text(encoding='utf-8-sig').split("result={'method'")[0],surface_ns)
SurfaceExtractor=surface_ns['SurfaceExtractor']
report={'startedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'fixedVm':str(fixed),'fixedVmSha256':hashlib.sha256(fixed.read_bytes()).hexdigest(),'scope':'Re-evaluate existing water catalogs with presentation-owned stack-pair fix; Validation outputs only; no Assets edits or Unity execution.','catalogs':[]}
def load(name):return json.loads((workspace/'UnityProject/Assets/Resources'/name).read_text(encoding='utf-8-sig'))
def differences(a,b,path=''):
 if isinstance(a,dict) and isinstance(b,dict):
  result=[]
  for k in sorted(set(a)|set(b)):
   if k not in a or k not in b:result.append({'path':path+'/'+k,'old':a.get(k),'new':b.get(k)})
   else:result+=differences(a[k],b[k],path+'/'+k)
  return result
 if isinstance(a,list) and isinstance(b,list):
  if len(a)!=len(b):return [{'path':path,'old':a,'new':b}]
  return sum((differences(x,y,path+'/'+str(i))for i,(x,y)in enumerate(zip(a,b))),[])
 if isinstance(a,(int,float)) and isinstance(b,(int,float)) and not isinstance(a,bool) and not isinstance(b,bool):
  return [] if abs(a-b)<1e-6 else [{'path':path,'old':a,'new':b}]
 return [] if a==b else [{'path':path,'old':a,'new':b}]
def persist(section):
 report['catalogs'].append(section)
 (out/'audit-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
 print(section['name'],'evaluated',section['evaluated'],'changed',len(section['changes']),'failed',len(section['failures']),flush=True)
def section(name):return {'name':name,'evaluated':0,'changes':[],'failures':[]}

data=load('RecoveredNativeWaterSurfaces.json');s=section('WaterSurfaces')
for case in data['cases']:
 try:
  values={'origen':case['origen'],'triggered':1,'rotay':0,'dirPlanoCentro':case['dirPlanoCentro'],'dirPlano1':1,'dirPlano2':3,'dirPlano3':5,'dirPlano4':7}
  ex=SurfaceExtractor(values);ex.run('FuenteTest3$$doTratarDirecciones');fresh={'origen':case['origen'],'dirPlanoCentro':case['dirPlanoCentro'],'mutations':ex.surfaces()}
  delta=differences(case,fresh);s['evaluated']+=1
  if delta:s['changes'].append({'key':[case['origen'],case['dirPlanoCentro']],'delta':delta})
 except Exception as e:s['failures'].append({'case':case,'error':str(e)})
persist(s)

data=load('RecoveredNativeWaterFlows.json');s=section('WaterFlows');new_cases=[]
for case in data['cases']:
 try:
  values={'origen':case['origen'],'triggered':1,'creaAgua':1,'rotay':0};ex=SurfaceExtractor(values)
  calls=ex.run('FuenteTest3$$doInstanciarAgua'+('1'if case['phase']else''));fresh={'phase':case['phase'],'origen':case['origen'],'recipes':calls};new_cases.append(fresh)
  delta=differences(case,fresh);s['evaluated']+=1
  if delta:s['changes'].append({'key':[case['phase'],case['origen']],'delta':delta})
 except Exception as e:s['failures'].append({'case':case,'error':str(e)})
(out/'WaterFlows-fixed.json').write_text(json.dumps({'cases':new_cases},ensure_ascii=False,indent=2),encoding='utf-8');persist(s)

data=load('RecoveredNativeWaterRecipes.json');s=section('WaterRecipes');new_entries=[]
for case in data['entries']:
 try:
  family=case['family'];values=inputs_for(family,case['configuration'],case['color'],case['centerMode'],case['pitMode'],'1'if case['animation']else'')
  ex=Extractor(values);calls=ex.run('FuenteTest3$$doConfigFuentes'+('B'if family=='border'else'Centro'if family=='center'else''))
  recipes=[]
  for r in calls:
   if r['prefabField'].startswith('prefabEsfera'):continue
   recipes.append({'prefabField':r['prefabField'],'position':r.get('localPosition',r['initialPosition']),'rotation':r.get('localRotation',r.get('rotation',r['quaternion'])),'scale':r.get('localScale',[]),'nativeInstantiateVA':r['nativeInstantiateVA']})
  fresh={**case,'recipes':recipes};new_entries.append(fresh);delta=differences(case,fresh);s['evaluated']+=1
  if delta:s['changes'].append({'key':[case[k]for k in ['family','configuration','color','centerMode','pitMode','animation']],'delta':delta})
 except Exception as e:s['failures'].append({'case':case,'error':str(e)})
(out/'WaterRecipes-fixed.json').write_text(json.dumps({'source':data['source'],'entries':new_entries},ensure_ascii=False,indent=2),encoding='utf-8');persist(s)

def normalize(recipe):
 if recipe.get('parent')!='self':raise RuntimeError(('unsupported parent',recipe.get('parent')))
 result={'prefabField':recipe['prefabField'],'nativeInstantiateVA':recipe['nativeInstantiateVA'],'position':recipe.get('localPosition',recipe['initialPosition']),'rotation':recipe.get('rotation',recipe['quaternion']),'operations':[]}
 if 'localScale'in recipe:result['scale']=recipe['localScale']
 for source in recipe.get('operations',[]):
  op={k:v for k,v in source.items()if k!='value'};value=source.get('value');kind=source['kind']
  if kind in('componentField','animatorInteger'):op['integer']=value
  elif kind in('componentEnabled','active'):op['enabled']=value
  elif kind in('velocityConstant','lightFloat'):op['number']=value
  elif kind=='lightColor':op['rgba']=value
  else:raise RuntimeError(('unknown operation',kind))
  result['operations'].append(op)
 return result
data=load('RecoveredNativeDecorativePits.json');s=section('DecorativePits');new_sets=[];index={};new_entries=[]
for case in data['entries']:
 key=case['key'];parts=key.split('|')
 try:
  if parts[0]=='small':
   _,kind,dir1,dir2,color=parts;values=inputs_for('primary',0,int(color));values.update({'tipoPieza':int(kind),'configPit1':'','configPit2':'','configPit3':'','configPit4':'','configFuenteCentro':0,'configFuente':0,'dirPlano1':int(dir1),'dirPlano2':int(dir2)})
   ex=PitExtractor(values);ex.run('FuenteTest3$$doInstanciarChorros')
   if kind=='4':ex.run('FuenteTest3$$doFuente3')
  elif parts[0]=='wetlight':
   values=inputs_for('primary',0);values['configLi']=parts[1];values['night']=1;ex=PitExtractor(values);ex.run('FuenteTest3$$doLights')
  else:
   pit1,pit2,pit3,pit4,anim,li,color,night=parts;values=inputs_for('primary',0,int(color));values.update({'configPit1':pit1,'configPit2':pit2,'configPit3':pit3,'configPit4':pit4,'configAnim':anim,'configLi':li,'night':int(night)})
   ex=PitExtractor(values);ex.run('FuenteTest3$$doInstanciarChorros')
  recipes=[normalize(r)for r in ex.calls];old_recipes=data['recipeSets'][case['set']]['recipes'];delta=differences(old_recipes,recipes);s['evaluated']+=1
  encoded=json.dumps(recipes,sort_keys=True,separators=(',',':'))
  if encoded not in index:index[encoded]=len(new_sets);new_sets.append({'recipes':recipes})
  new_entries.append({'key':key,'set':index[encoded]})
  if delta:s['changes'].append({'key':key,'delta':delta})
 except Exception as e:s['failures'].append({'key':key,'error':str(e)})
 if(s['evaluated']+len(s['failures']))%500==0:print('pits progress',s['evaluated'],len(s['changes']),len(s['failures']),flush=True)
(out/'DecorativePits-fixed.json').write_text(json.dumps({'source':data['source'],'entries':new_entries,'recipeSets':new_sets},ensure_ascii=False,indent=2),encoding='utf-8');persist(s)
report['finishedUtc']=datetime.datetime.now(datetime.timezone.utc).isoformat();(out/'audit-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
