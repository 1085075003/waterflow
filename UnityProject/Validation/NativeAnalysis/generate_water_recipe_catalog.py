import json,sys,pathlib
sys.path.insert(0,'UnityProject/Validation/NativeAnalysis');from extract_water_recipes import Extractor
entries=[];fail=[]
for family,key,count,centers,pits,animations in [('primary','configFuente',9,[0,8,9],[0,5,6],[0]),('border','configFuenteB',8,[0,7,8,9],[0],[0]),('center','configFuenteCentro',9,[0],[0],[0,1])]:
 for config in range(1,count+1):
  for color in range(1,5):
   for center in centers:
    for pit in pits:
     for anim in animations:
      inputs={'color':color,'tipoPieza':2,'configFuenteCentro':center,'configPit1':str(pit)+'000','configPit2':'0000','configPit3':'0000','configPit4':'0000','configAnim':'1' if anim else '', 'configLi':'','configRes1':'','configRes2':'','configRes3':'','configRes4':'',key:config,'rotay':0.0}
      try:
       ex=Extractor(inputs);recipes=ex.run('FuenteTest3$$doConfigFuentes'+('B' if family=='border' else 'Centro' if family=='center' else ''))
       entry={'family':family,'configuration':config,'color':color,'centerMode':center,'pitMode':pit,'animation':anim,'recipes':[]}
       for r in recipes:
        if r['prefabField'].startswith('prefabEsfera'):continue
        entry['recipes'].append({'prefabField':r['prefabField'],'position':r.get('localPosition',r['initialPosition']),'rotation':r.get('localRotation',r.get('rotation',r['quaternion'])),'scale':r.get('localScale',[]),'nativeInstantiateVA':r['nativeInstantiateVA']})
       entries.append(entry)
      except Exception as e:fail.append([family,config,color,center,pit,anim,str(e)])
res=pathlib.Path('UnityProject/Assets/Resources/RecoveredNativeWaterRecipes.json');res.write_text(json.dumps({'source':'Static evaluation of original ARM64 FuenteTest3 placement methods. See Validation/NativeAnalysis/extract_water_recipes.py.','entries':entries},ensure_ascii=False,separators=(',',':')),'utf-8')
pathlib.Path('UnityProject/Validation/NativeAnalysis/native-water-recipe-extraction-report.json').write_text(json.dumps({'entries':len(entries),'failures':fail},indent=2),'utf-8')
print(json.dumps({'entries':len(entries),'recipes':sum(len(v['recipes']) for v in entries),'failures':fail,'bytes':res.stat().st_size},indent=2))
