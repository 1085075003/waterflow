"""Raw per-Animator residual bindings; valid/orphan curves and scene ancestry, read only."""
import pathlib,sys,json,zlib,collections
sys.path.insert(0,str(pathlib.Path('Validation/ShaderAnalysis/python').resolve()))
import UnityPy
base=pathlib.Path('Validation/NativeAnalysis')
prior=json.loads((base/'animation-active-residuals-report.json').read_text(encoding='utf-8'))
wanted={v['clip'] for v in prior['clips']}
wanted.add('agua13')
env=UnityPy.load('../raw/base/assets/bin/Data/data.unity3d')
cache={}
def read(o):
 k=(o.assets_file.name,o.path_id)
 if k not in cache:cache[k]=o.read()
 return cache[k]
def ident(o):return {'file':o.assets_file.name,'pathID':o.path_id}
def transform(go):
 for pair in go.m_Component:
  o=pair.component.deref()
  if o.type.name in ['Transform','RectTransform']:return o
def paths(obj):
 result=collections.defaultdict(list);stack=[(obj,'')]
 while stack:
  o,p=stack.pop();t=read(o);g=read(t.m_GameObject.deref())
  result[zlib.crc32(p.encode())&0xffffffff].append({'path':p,'name':g.m_Name,**ident(o)})
  for cp in t.m_Children:
   co=cp.deref();cn=read(read(co).m_GameObject.deref()).m_Name
   stack.append((co,p+'/'+cn if p else cn))
 return result
def ancestry(tr):
 result=[];seen=set()
 while tr:
  k=(tr.assets_file.name,tr.path_id)
  if k in seen:break
  seen.add(k);t=read(tr);g=read(t.m_GameObject.deref())
  result.append({'name':g.m_Name,'activeSelf':g.m_IsActive,**ident(tr)})
  tr=t.m_Father.deref() if t.m_Father else None
 return list(reversed(result))
result=[]
for o in env.objects:
 if o.type.name!='Animator':continue
 a=read(o)
 if not a.m_Controller:continue
 ctrl=read(a.m_Controller.deref());clips=[]
 for ptr in ctrl.m_AnimationClips:
  c=ptr.deref();ct=c.read_typetree()
  if ct['m_Name'] in wanted:clips.append((c,ct))
 if not clips:continue
 tr=transform(read(a.m_GameObject.deref()));tree=paths(tr)
 for c,ct in clips:
  bindings=[]
  for b in ct['m_ClipBindingConstant']['genericBindings']:
   bindings.append({'hash':f'0x{b["path"]:08X}','attribute':b['attribute'],
                    'typeID':b['typeID'],'matches':tree.get(b['path'],[])})
  valid=[b for b in bindings if b['matches']];missing=[b for b in bindings if not b['matches']]
  result.append({'animator':ident(o),'enabled':a.m_Enabled,'controller':ctrl.m_Name,
                 'clip':ct['m_Name'],'clipRef':ident(c),'rootAncestors':ancestry(tr),
                 'validBindingCount':len(valid),'orphanBindingCount':len(missing),
                 'validBindings':valid,'orphanBindings':missing})
report={'scope':'Original bundle actual Animator subtrees and clip genericBindings, including valid bindings. No Assets mutation.',
        'crcCalibration':prior['crcCalibration'],'contexts':result,
        'candidateOriginalObsoletePath':{'path':'Size/Root/Icon','hash':f'0x{zlib.crc32(b"Size/Root/Icon")&0xffffffff:08X}',
                                       'status':'Exact CRC preimage from old star naming, absent in all original referenced Animator trees; not a proposed remapping.'}}
(base/'animation-residual-valid-and-orphan-bindings.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
for clip in sorted({r['clip']for r in result}):
 rows=[r for r in result if r['clip']==clip]
 print(clip,collections.Counter((r['validBindingCount'],r['orphanBindingCount'])for r in rows))
 if clip.startswith('Star_'):
  for r in rows:print(' ',r['controller'],'/'.join(v['name']for v in r['rootAncestors']),r['validBindingCount'],r['orphanBindingCount'])
