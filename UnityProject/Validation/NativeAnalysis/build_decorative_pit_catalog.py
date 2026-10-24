"""Build original decorative-pit provider catalog from actual configured contexts."""
from pathlib import Path
import sys,json,collections,time
sys.path.insert(0,str(Path(__file__).resolve().parent))
from extract_decorative_pit_recipes import PitExtractor
from extract_water_animator_recipes import inputs_for
base=Path(__file__).resolve().parent
contexts=json.loads((base/'native-actual-decorative-pit-contexts.json').read_text(encoding='utf-8'))
sets=[];lookup={};entries=[];fail=[];evaluated=0;ops=collections.Counter()
def normalize(recipe):
 if recipe.get('parent')!='self':raise RuntimeError(('unsupported original parent',recipe.get('parent')))
 result={'prefabField':recipe['prefabField'],'nativeInstantiateVA':recipe['nativeInstantiateVA'],'position':recipe.get('localPosition',recipe['initialPosition']),'rotation':recipe.get('rotation',recipe['quaternion']),'operations':[]}
 if 'localScale'in recipe:result['scale']=recipe['localScale']
 for source in recipe.get('operations',[]):
  op={k:v for k,v in source.items()if k!='value'};value=source.get('value');kind=source['kind']
  if kind in('componentField','animatorInteger'):op['integer']=value
  elif kind in('componentEnabled','active'):op['enabled']=value
  elif kind=='velocityConstant':op['number']=value
  elif kind=='lightColor':op['rgba']=value
  else:raise RuntimeError(('unknown operation',kind))
  ops[kind]+=1;result['operations'].append(op)
 return result
for number,c in enumerate(contexts):
 for color in range(1,5):
  for night in(0,1):
   values=inputs_for('primary',0,color=color);values.update({k:v for k,v in c.items()if k!='configLights'});values['configLi']=c['configLights'];values['night']=night
   key='|'.join([c['configPit1'],c['configPit2'],c['configPit3'],c['configPit4'],c['configAnim'],c['configLights'],str(color),str(night)])
   try:
    ex=PitExtractor(values);ex.run('FuenteTest3$$doInstanciarChorros');recipes=[normalize(r)for r in ex.calls];encoded=json.dumps(recipes,sort_keys=True,separators=(',',':'))
    if encoded not in lookup:lookup[encoded]=len(sets);sets.append({'recipes':recipes})
    entries.append({'key':key,'set':lookup[encoded]});evaluated+=1
   except Exception as error:fail.append({'context':c,'color':color,'night':night,'error':str(error)})
 if(number+1)%100==0:print('contexts',number+1,'evaluated',evaluated,'failures',len(fail),flush=True)
catalog={'source':'Original ARM64 FT3.doInstanciarChorros->doConfigPit->doPitA/B, actual configured type2 contexts, exact original child mutations.','entries':entries,'recipeSets':sets}
(base/'RecoveredNativeDecorativePits.json').write_text(json.dumps(catalog,ensure_ascii=False,separators=(',',':')),encoding='utf-8')
report={'actualContexts':len(contexts),'evaluated':evaluated,'recipeSets':len(sets),'operations':dict(ops),'failures':fail,'scope':'Read-only original ARM evaluation; no Assets edits or Unity execution.'}
(base/'native-decorative-pit-extraction-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report)[:2500],flush=True)
