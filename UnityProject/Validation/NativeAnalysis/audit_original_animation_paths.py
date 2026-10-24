"""Audit unresolved exported clip hashes against original bundled object trees, without modifying Assets."""
import pathlib,sys,json,zlib,collections
sys.path.insert(0,str(pathlib.Path('Validation/ShaderAnalysis/python').resolve()))
import UnityPy
out=pathlib.Path('Validation/NativeAnalysis');exported=json.loads((out/'animation-path-crc-audit.json').read_text(encoding='utf-8'))
wanted={v['hash']:[]for v in exported['unresolvedEntries']}
for v in exported['unresolvedEntries']:wanted[v['hash']].append(v['clip'])
e=UnityPy.load('../raw/base/assets/bin/Data/data.unity3d');cache={};errors=[]
def read(obj):
 k=(obj.assets_file.name,obj.path_id)
 if k not in cache:cache[k]=obj.read()
 return cache[k]
def ident(obj):return {'file':obj.assets_file.name,'pathID':obj.path_id}
def root_transform(go):
 for c in go.m_Component:
  o=c.component.deref()
  if o.type.name in ['Transform','RectTransform']:return o
 return None
def tree(root):
 found={};stack=[(root,'')]
 while stack:
  obj,path=stack.pop();t=read(obj);gobj=t.m_GameObject.deref();go=read(gobj);h=zlib.crc32(path.encode())&0xffffffff
  found.setdefault(h,[]).append({'path':path,'gameObject':ident(gobj),'transform':ident(obj),'name':go.m_Name})
  for ptr in t.m_Children:
   c=ptr.deref();cn=read(read(c).m_GameObject.deref()).m_Name;stack.append((c,(path+'/'if path else '')+cn))
 return found
contexts=[];hits=[];alltrees=collections.defaultdict(list);cliprefs=collections.Counter();clippaths=collections.defaultdict(set)
for o in e.objects:
 if o.type.name!='Animator':continue
 try:
  a=read(o)
  if not a.m_Controller:continue
  ctrlobj=a.m_Controller.deref();ctrl=read(ctrlobj);goobj=a.m_GameObject.deref();go=read(goobj);tr=root_transform(go)
  if tr is None:continue
  paths=tree(tr)
  for h in wanted:
   if h in paths:
    for match in paths[h]:alltrees[h].append({'animator':ident(o),'rootName':go.m_Name,**match})
  for ptr in ctrl.m_AnimationClips:
   co=ptr.deref();ct=co.read_typetree();name=ct['m_Name'];bindings=ct.get('m_ClipBindingConstant',{}).get('genericBindings',[])
   for b in bindings:
    h=b['path']
    if h not in wanted:continue
    cliprefs[(name,h)]+=1;clippaths[(name,h)].add((co.assets_file.name,co.path_id))
    v={'animator':ident(o),'root':ident(goobj),'rootName':go.m_Name,'controller':{**ident(ctrlobj),'name':ctrl.m_Name},'clip':{**ident(co),'name':name},'hash':h,'matches':paths.get(h,[]),'childPaths':[p['path']for ps in paths.values()for p in ps]}
    contexts.append(v)
    if v['matches']:hits.append(v)
 except Exception as ex:errors.append({'animator':ident(o),'error':str(ex)})
r={'originalBundle':'../raw/base/assets/bin/Data/data.unity3d','animatorCount':sum(o.type.name=='Animator'for o in e.objects),'matchedReferencedBindings':len(hits),'matchingHashAnywhereInOriginalAnimatorTrees':sum(bool(v)for v in alltrees.values()),'matchingHashes':dict(alltrees),'referencedContexts':contexts,'unmatchedBindingContexts':sum(not v['matches']for v in contexts),'errors':errors,'status':'Original bundle read only; no fabricated mapping or Assets mutation'}
(out/'animation-original-tree-crc-audit.json').write_text(json.dumps(r,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({k:r[k]for k in['animatorCount','matchedReferencedBindings','matchingHashAnywhereInOriginalAnimatorTrees','unmatchedBindingContexts']}))
print('errors',len(errors))
for h,v in alltrees.items():print(hex(h),v[:2])
