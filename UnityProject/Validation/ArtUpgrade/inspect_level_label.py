from pathlib import Path
import re,json
result=[]
for p in (Path('UnityProject/Assets/Scenes')).rglob('Levely5*.unity'):
 blocks={int(i):t for _,i,t in re.findall(r'--- !u!(\d+) &(\d+)\n(.*?)(?=--- !u!|\Z)',p.read_text(),re.S)}
 gos={i:re.search(r'  m_Name: (.*)',t)[1] for i,t in blocks.items() if t.startswith('GameObject:')}
 trs={i:(int(re.search(r'm_GameObject: \{fileID: (\d+)',t)[1]),int(re.search(r'm_Father: \{fileID: (\d+)',t)[1])) for i,t in blocks.items() if t.startswith(('RectTransform:','Transform:'))}
 gotr={go:i for i,(go,parent) in trs.items()}
 def path(go):
  tr=gotr.get(go);par=trs[tr][1] if tr else 0
  return (path(trs[par][0])+'/' if par in trs else '')+gos.get(go,'?')
 for i,t in blocks.items():
  if not t.startswith('MonoBehaviour:'):continue
  gm=re.search(r'm_GameObject: \{fileID: (\d+)',t)
  if not gm:continue
  go=int(gm[1]);pth=path(go)
  if '/Level/' not in pth:continue
  row={'scene':str(p),'path':pth,'componentFileId':i,'scriptGuid':re.search(r'm_Script: \{fileID: \d+, guid: (\w+)',t)[1]}
  for key in ['m_Enabled','m_Text','m_EffectColor','m_EffectDistance','m_Color','m_Sprite']:
   found=re.search(r'  '+key+r': (.*)',t)
   if found:row[key]=found[1]
  result.append(row);print(json.dumps(row))
Path('UnityProject/Validation/ArtUpgrade/UI-level-label-components.json').write_text(json.dumps(result,indent=2),encoding='utf8')
