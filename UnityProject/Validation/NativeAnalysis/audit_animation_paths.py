"""Resolve recovered AnimationClip synthetic paths against real Animator prefab hierarchies by the stored Unity path CRC32. Read-only assets."""
import pathlib,re,zlib,json,collections
A=pathlib.Path('Assets'); O=pathlib.Path('Validation/NativeAnalysis')
guid={}
for d in ['AnimatorController','AnimationClip']:
 for p in (A/d).glob('*.meta'):
  m=re.search(r'^guid: (\w+)',p.read_text(encoding='utf-8-sig'),re.M)
  if m:guid[m[1]]=p.with_suffix('')
clip_paths={}; placeholders={}
for p in (A/'AnimationClip').glob('*.anim'):
 s=p.read_text(encoding='utf-8-sig'); matches=re.findall(r'^\s+path: (path_0x([0-9A-Fa-f]+)_\S+)',s,re.M)
 if matches:placeholders[str(p)]=dict((v,int(h,16))for v,h in matches)
def clips(p,seen=None):
 seen=set()if seen is None else seen
 if p in seen:return set()
 seen.add(p)
 if p.suffix=='.anim':return {str(p)}
 r=set()
 for g in set(re.findall(r'guid: (\w+)',p.read_text(encoding='utf-8-sig'))):
  if g in guid:r.update(clips(guid[g],seen))
 return r
controller={str(p):clips(p)for p in(A/'AnimatorController').glob('*.controller')}
mapping=collections.defaultdict(lambda:collections.defaultdict(set)); contexts=[]; misses=collections.Counter()
for p in list((A/'GameObject').glob('*.prefab')) + list((A/'Scenes').glob('*.unity')):
 s=p.read_text(encoding='utf-8-sig'); b={int(i):(int(t),v)for t,i,v in re.findall(r'^--- !u!(\d+) &(\d+)\n(.*?)(?=^---|\Z)',s,re.M|re.S)}
 trs={};gonames={}
 for i,(t,v)in b.items():
  if t==1:
   m=re.search(r'^  m_Name: ?(.*)$',v,re.M);gonames[i]=m[1]if m else ''
  if t in (4,224):
   go=int(re.search(r'm_GameObject: \{fileID: (\d+)',v)[1]);father=int(re.search(r'm_Father: \{fileID: (\d+)',v)[1]);trs[go]=(i,father)
 trgo={v[0]:k for k,v in trs.items()}
 def paths(root):
  result={0:['']}
  for go,(i,father)in trs.items():
   cur=go;names=[]
   while cur!=root and cur in trs:
    names.insert(0,gonames.get(cur,''));f=trs[cur][1]
    if not f:break
    cur=trgo.get(f,0)
   if cur!=root:continue
   name='/'.join(names);h=zlib.crc32(name.encode())&0xffffffff
   result.setdefault(h,[]).append(name)
  return result
 for i,(t,v)in b.items():
  if t!=95:continue
  m=re.search(r'm_Controller: \{fileID: \d+, guid: (\w+)',v)
  if not m or m[1]not in guid:continue
  go=int(re.search(r'm_GameObject: \{fileID: (\d+)',v)[1]);rootpaths=paths(go);ctrl=str(guid[m[1]])
  for clip in controller.get(ctrl,set()):
   for placeholder,h in placeholders.get(clip,{}).items():
    matches=rootpaths.get(h,[])
    if matches:
     mapping[clip][placeholder].update(matches);contexts.append({'prefab':str(p),'animator':gonames.get(go),'controller':ctrl,'clip':clip,'placeholder':placeholder,'hash':h,'matches':matches})
    else:misses[(clip,placeholder)]+=1
resolved=[];ambiguous=[];unresolved=[]
for clip,ps in placeholders.items():
 for placeholder,h in ps.items():
  names=sorted(mapping[clip][placeholder]);v={'clip':clip,'placeholder':placeholder,'hash':h,'paths':names,'unmatchedContexts':misses[clip,placeholder]}
  (resolved if len(names)==1 else ambiguous if names else unresolved).append(v)
r={'placeholderClips':len(placeholders),'uniqueResolved':len(resolved),'ambiguous':len(ambiguous),'unresolved':len(unresolved),'resolved':resolved,'ambiguousEntries':ambiguous,'unresolvedEntries':unresolved,'matchedContexts':contexts}
(O/'animation-path-crc-audit.json').write_text(json.dumps(r,ensure_ascii=False,indent=2),encoding='utf-8')
r['classification']='Unresolved original clip path hashes; no replacement hierarchy or animation was guessed. Referenced prefab/scene contexts remain unknown binding compatibility, not proven unused.'
r['unresolvedClassification']=[{'clip':clip,'entries':len(ps),'referencedInAnimatorContexts':sum(misses[clip,v]for v in ps),'class':'referenced-but-binding-unresolved' if any(misses[clip,v]for v in ps)else 'no-matching-exported-animator-context-found'}for clip,ps in placeholders.items()]
(O/'animation-path-crc-audit.json').write_text(json.dumps(r,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({k:r[k]for k in['placeholderClips','uniqueResolved','ambiguous','unresolved']}))
for v in resolved[:15]:print(v['clip'],v['placeholder'],'=>',v['paths'])
