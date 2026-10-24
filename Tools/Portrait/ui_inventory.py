import re, pathlib
files=['Assets/Scenes/Portada.unity','Assets/Scenes/SelLevel0.unity','Assets/Scenes/SelLevel1.unity','Assets/Settings.unity','Assets/Store.unity']
root=pathlib.Path('UnityProject')
metas={}
for p in (root/'Assets').rglob('*.cs.meta'):
 m=re.search(r'guid: (\w+)',p.read_text(encoding='utf-8-sig')); 
 if m: metas[m[1]]=p.stem
for f in files:
 txt=(root/f).read_text(encoding='utf-8-sig'); docs={int(m[2]):(int(m[1]),m[3]) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)\r?\n(.*?)(?=^--- !u!|\Z)',txt,re.M|re.S)}
 gos={}; trans={}; comps={}
 for id,(typ,b) in docs.items():
  if typ==1:
   name=re.search(r'^  m_Name: (.*)$',b,re.M).group(1); act=re.search(r'm_IsActive: (\d)',b).group(1)
   gos[id]=(name,act)
  if typ in [4,224]:
   go=int(re.search(r'm_GameObject: \{fileID: (-?\d+)',b).group(1)); par=int(re.search(r'm_Father: \{fileID: (-?\d+)',b).group(1)); trans[id]=(go,par,b)
  if typ==114:
   go=int(re.search(r'm_GameObject: \{fileID: (-?\d+)',b).group(1)); mat=re.search(r'm_Script: \{fileID: \d+, guid: (\w+)',b); scr=metas.get(mat.group(1),'?'+mat.group(1)) if mat else '??'
   comps.setdefault(go,[]).append((scr,b))
 def path(id):
  go,par,b=trans[id]; return (path(par)+'/' if par in trans else '')+gos[go][0]
 out=[]
 for id,(go,par,b) in trans.items():
  p=path(id)
  if docs[id][0]!=224: continue
  comp=comps.get(go,[])
  desc=[]
  for scr,cb in comp:
   if 'm_Text:' in cb: desc.append('TXT '+re.search(r'm_Text: (.*)',cb).group(1))
   if 'm_MethodName:' in cb: desc.append('EVT '+','.join(re.findall(r'm_MethodName: (.*)',cb)))
   if 'm_AnimationTriggers:' in cb: desc.append('SELECTABLE')
  pos=re.search(r'm_AnchoredPosition: (.*)',b).group(1); size=re.search(r'm_SizeDelta: (.*)',b).group(1); amin=re.search(r'm_AnchorMin: (.*)',b).group(1); amax=re.search(r'm_AnchorMax: (.*)',b).group(1)
  out.append(p+' active='+gos[go][1]+' pos='+pos+' size='+size+' anchors='+amin+','+amax+' '+';'.join(desc))
 dst=root/'Validation'/'Portrait';dst.mkdir(parents=True,exist_ok=True); (dst/(pathlib.Path(f).stem+'-ui-tree.txt')).write_text('\n'.join(out),encoding='utf-8')
 print(f,'rects',len(out))
