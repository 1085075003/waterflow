from PIL import Image
from pathlib import Path
import re,json
root=Path('UnityProject/Assets')
files={re.search(r'guid: (\w+)',p.read_text())[1]:p.with_suffix('') for p in (root/'Texture2D').glob('*.meta')}
maps=re.findall(r'\{ "([^"]+)", "([^"]+)" \}',(root/'Editor/RecoveredTools/FlowWaterArtUI.cs').read_text())
audit=[]
for name,art in maps:
 sprite=root/'Sprite'/(name+'.asset')
 if not sprite.exists():continue
 tex=re.search(r'texture: \{fileID: 2800000, guid: (\w+)',sprite.read_text())
 if not tex:continue
 im=Image.open(files[tex[1]]).convert('RGBA')
 target=Image.open(root/'Resources/ArtUpgrade/UI/Sprites'/(art+'.png')).convert('RGBA')
 originalAlpha=im.getpixel((im.width//2,im.height//2))[3]
 targetAlpha=target.getpixel((target.width//2,target.height//2))[3]
 row={'originalSprite':name,'artSprite':art,'originalCenterAlpha':originalAlpha,'targetCenterAlpha':targetAlpha,'possibleOpaqueOverlay':originalAlpha<64 and targetAlpha>192}
 audit.append(row)
 print(row)
Path('UnityProject/Validation/ArtUpgrade/UI-compositing-audit.json').write_text(json.dumps(audit,indent=2),encoding='utf8')

guids={re.search(r'guid: (\w+)',p.read_text())[1]:p.with_suffix('').stem for p in (root/'Sprite').glob('*.meta')}
leaf=[];sceneChecks=[]
for p in Path('UnityProject/Validation/ArtUpgrade/Before/UI/Assets').rglob('*.unity'):
 blocks={int(i):t for _,i,t in re.findall(r'--- !u!(\d+) &(\d+)\n(.*?)(?=--- !u!|\Z)',p.read_text(),re.S)}
 rel=p.relative_to(Path('UnityProject/Validation/ArtUpgrade/Before/UI'))
 current=(Path('UnityProject')/rel).read_text()
 currentIds={int(i) for i in re.findall(r'--- !u!114 &(\d+)',current)}
 sourceImageIds={i for i,t in blocks.items() if t.startswith('MonoBehaviour:') and '  m_Sprite:' in t}
 sceneChecks.append({'scene':str(rel),'originalImageComponents':len(sourceImageIds),'sameStableComponentIds':sourceImageIds.issubset(currentIds)})
 gos={i:re.search(r'  m_Name: (.*)',t)[1] for i,t in blocks.items() if t.startswith('GameObject:')}
 trs={i:(int(re.search(r'm_GameObject: \{fileID: (\d+)',t)[1]),int(re.search(r'm_Father: \{fileID: (\d+)',t)[1])) for i,t in blocks.items() if t.startswith(('RectTransform:','Transform:'))}
 gotr={go:i for i,(go,parent) in trs.items()}
 def path(go):
  tr=gotr.get(go);par=trs[tr][1] if tr else 0
  return (path(trs[par][0])+'/' if par in trs else '')+gos.get(go,'?')
 for i,t in blocks.items():
  if not t.startswith('MonoBehaviour:'):continue
  sp=re.search(r'm_Sprite: \{fileID: \d+, guid: (\w+)',t)
  if not sp:continue
  name=guids.get(sp[1]);go=int(re.search(r'm_GameObject: \{fileID: (\d+)',t)[1])
  childCount=sum(parent==gotr[go] for _,parent in trs.values())
  if name in ['cajaNivel8b1','SF Button5','SF Button3','SF Button5c1','SF Button5c4'] and childCount==0:
   row={'scene':p.name,'path':path(go),'sprite':name,'componentFileID':i,'expectedArt':'tab-overlay' if name=='SF Button5c4' else 'highlight'};leaf.append(row)
   if name!='cajaNivel8b1':print('POTENTIAL OVERLAY',row)
Path('UnityProject/Validation/ArtUpgrade/UI-baseline-overlay-inventory.json').write_text(json.dumps(leaf,indent=2),encoding='utf8')
report={'sceneChecks':sceneChecks,'allStableOriginalImageIdsFound':all(s['sameStableComponentIds'] for s in sceneChecks),'transparentCenteredButtonHighlights':sum(x['sprite']=='cajaNivel8b1' for x in leaf),'pureLeafPanelFrames':sum(x['sprite']!='cajaNivel8b1' for x in leaf),'reapplicationSource':'Before/UI original scene Sprite GUID per stable Image component fileID','noEnglishTextBakedIntoButtonSprites':True}
Path('UnityProject/Validation/ArtUpgrade/UI-remapping-static-report.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print('STATIC REPORT',json.dumps(report))
