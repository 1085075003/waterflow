import pathlib,re,json
root=pathlib.Path('UnityProject')
guids={}
for p in (root/'Assets').rglob('*.meta'):
 m=re.search(r'^guid: (\w+)',p.read_text(errors='ignore'),re.M)
 if m: guids[m[1]]=p.with_suffix('').name
out=[]
for p in [*(root/'Assets/Scenes').rglob('*.unity'),root/'Assets/Settings.unity',root/'Assets/Store.unity']:
 s=p.read_text(errors='ignore'); blocks={int(i):t for _,i,t in re.findall(r'--- !u!(\d+) &(\d+)\n(.*?)(?=--- !u!|\Z)',s,re.S)}
 gos={i:re.search(r'  m_Name: (.*)',t).group(1) for i,t in blocks.items() if t.startswith('GameObject:')}
 trs={i:(int(re.search(r'm_GameObject: \{fileID: (\d+)',t).group(1)),int(re.search(r'm_Father: \{fileID: (\d+)',t).group(1))) for i,t in blocks.items() if t.startswith(('RectTransform:','Transform:'))}
 gotr={go:i for i,(go,parent) in trs.items()}
 def path(go):
  tr=gotr.get(go); par=trs[tr][1] if tr else 0
  return (path(trs[par][0])+'/' if par in trs else '')+gos.get(go,'?')
 for i,t in blocks.items():
  if not t.startswith('MonoBehaviour:'):continue
  gm=re.search(r'm_GameObject: \{fileID: (\d+)',t)
  if not gm:continue
  sp=re.search(r'm_Sprite: \{fileID: \d+(?:, guid: (\w+))?',t); tx=re.search(r'  m_Text: (.*)',t)
  if not(sp or tx):continue
  r={'path':path(int(gm[1]))}
  if sp:r['sprite']=guids.get(sp[1],sp[1])
  if tx:r['text']=tx[1]
  if 'm_FontData' in t:r['size']=re.search(r'm_FontSize: (.*)',t)[1]
  tr=blocks[gotr[int(gm[1])]]
  r['rect']=re.search(r'm_SizeDelta: (.*)',tr)[1]
  out.append({'scene':p.name,**r})
(root/'Validation/ArtUpgrade').mkdir(exist_ok=True)
(root/'Validation/ArtUpgrade/UI-inventory.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf-8')
for x in out:
 if x['scene'] in ['Portada.unity','Settings.unity','Store.unity','Levely5.unity']: print(x)
