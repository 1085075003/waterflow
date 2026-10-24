import collections,functools,hashlib,json,pathlib,re,shutil
batch=pathlib.Path(__file__).resolve().parent
project=batch.parents[1];assets=project/'Assets';workspace=project.parent
old=json.loads((assets/'Resources/RecoveredNativeGeometry.json').read_text(encoding='utf-8-sig'))
new=json.loads((batch/'regenerated-geometry.json').read_text(encoding='utf-8-sig'))
assert old['entries']==new['entries']
assert len(old['recipeSets'])==len(new['recipeSets'])
changed_sets={i for i,(a,b) in enumerate(zip(old['recipeSets'],new['recipeSets'])) if a!=b}
changed_entries=[x for x in old['entries'] if x['recipeSet'] in changed_sets]
guid_names={}
for meta in (assets/'GameObject').glob('*.prefab.meta'):
 m=re.search(r'^guid: (\w+)',meta.read_text(encoding='utf-8-sig'),re.M)
 if m:guid_names[m[1]]=meta.with_suffix('')
scriptguid=re.search(r'^guid: (\w+)',(assets/'Scripts/Assembly-CSharp/ControlPiezaC.cs.meta').read_text(encoding='utf-8-sig'),re.M)[1]
@functools.lru_cache(None)
def fields_for(base):
 text=(assets/'GameObject'/(base+'.prefab')).read_text(encoding='utf-8-sig')
 blocks={int(i):(int(t),b) for t,i,b in re.findall(r'^--- !u!(\d+) &(-?\d+)\n(.*?)(?=^--- !u!|\Z)',text,re.M|re.S)}
 transforms={int(re.search(r'm_GameObject: \{fileID: (\d+)\}',b)[1]):(i,b) for i,(t,b) in blocks.items() if t==4}
 byid={i:b for i,b in transforms.values()};result={}
 for _,(kind,body) in blocks.items():
  if kind!=114 or scriptguid not in body:continue
  go=int(re.search(r'm_GameObject: \{fileID: (\d+)\}',body)[1]);_,tr=transforms[go];path=[]
  while int(re.search(r'm_Father: \{fileID: (\d+)\}',tr)[1]):
   gi=int(re.search(r'm_GameObject: \{fileID: (\d+)\}',tr)[1]);path.append(re.search(r'^  m_Name: (.*)',blocks[gi][1],re.M)[1]);tr=byid[int(re.search(r'm_Father: \{fileID: (\d+)\}',tr)[1])]
  result['/'.join(reversed(path))]=dict(re.findall(r'^  (\w+): (.*)$',body,re.M))
 return result

mutations=[];prefab_proof={}
for entry in changed_entries:
 si=entry['recipeSet'];a=old['recipeSets'][si];b=new['recipeSets'][si]
 assert {k:v for k,v in a.items() if k!='recipes'}=={k:v for k,v in b.items() if k!='recipes'}
 for ri,(before,after) in enumerate(zip(a['recipes'],b['recipes'])):
  if before==after:continue
  changed=[k for k in set(before)|set(after) if before.get(k)!=after.get(k)]
  assert set(changed)<= {'position','rotation','localRotation','scale'}
  field=before['prefabField'];value=fields_for(entry['basePrefab'])[entry['path']][field]
  guid=re.search(r'guid: (\w+)',value)[1];path=guid_names[guid]
  mutations.append({'entry':entry,'recipeIndex':ri,'prefabField':field,'prefab':path.name,'nativeInstantiateVA':before.get('nativeInstantiateVA'),
                    'changedFields':{key:{'before':before.get(key),'after':after.get(key)} for key in changed}})
  if path.name not in prefab_proof:
   text=path.read_text(encoding='utf-8-sig');types=collections.Counter(int(x) for x in re.findall(r'^--- !u!(\d+)',text,re.M))
   prefab_proof[path.name]={'path':str(path.relative_to(project)),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'classIdCounts':dict(types),
                            'hasCollider':bool(set(types)&{56,64,65,135,136,143,154}),'hasRigidbody':54 in types,'hasMonoBehaviour':114 in types,
                            'hasAnimator':95 in types,'scriptGuids':re.findall(r'm_Script: \{fileID: \d+, guid: (\w+)',text)}

aliases={x['originalName']:x['basePrefab'] for x in json.loads((assets/'RecoveredData/piece_aliases.json').read_text(encoding='utf-8-sig'))['aliases']}
keys={(x['basePrefab'],x['configuration']) for x in changed_entries}
affected=[];save1_ids=set()
for path in sorted((workspace/'LevelData/02_Solutions').glob('*.json')):
 data=json.loads(path.read_text(encoding='utf-8-sig'))['data']
 if data.get('__type__')=='Save1':save1_ids.add(int(re.search(r'\d+',path.stem)[0]))
for directory in ['01_Layouts','02_Solutions']:
 for path in sorted((workspace/'LevelData'/directory).glob('*.json')):
  data=json.loads(path.read_text(encoding='utf-8-sig'))['data']
  if data.get('__type__')!='Save':continue
  hits=[{'record':i,'piece':name,'basePrefab':aliases.get(name),'configuration':code} for i,(name,code) in enumerate(zip(data['pieza'],data['fijo'])) if (aliases.get(name),code) in keys]
  if hits:
   level=int(re.search(r'\d+',path.stem)[0]);affected.append({'asset':path.stem,'assetId':level,'normalSave1':level in save1_ids,'pieces':hits})

ids=sorted({x['assetId'] for x in affected});native_ids=sorted(set(ids)&save1_ids)
result={'changedRecipeSets':len(changed_sets),'changedEntries':len(changed_entries),'expandedChangedRecipes':len(mutations),
 'uniqueRecipeChanges':len({(x['entry']['recipeSet'],x['recipeIndex']) for x in mutations}),
 'nonRecipeFieldsBitIdentical':True,'onlyChangedFields':['position','rotation','localRotation','scale'],
 'mutations':mutations,'prefabs':list(prefab_proof.values()),'affectedAssets':affected,'affectedAssetIds':ids,
 'affectedNativeSave1Ids':native_ids,'allChangedPrefabTreesContainNoColliderOrRigidbody':all(not x['hasCollider'] and not x['hasRigidbody'] for x in prefab_proof.values())}
(batch/'scope-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print({k:result[k] for k in ['changedRecipeSets','changedEntries','expandedChangedRecipes','uniqueRecipeChanges','allChangedPrefabTreesContainNoColliderOrRigidbody']})
print('prefabs',result['prefabs']);print('affected all',len(ids),ids);print('affected Save1',len(native_ids),native_ids)
out=batch/'Assets/Resources/RecoveredNativeGeometry.json';out.parent.mkdir(parents=True,exist_ok=True)
shutil.copyfile(batch/'regenerated-geometry.json',out)
shutil.copyfile(assets/'Resources/RecoveredNativeGeometry.json.meta',out.with_suffix('.json.meta'))
