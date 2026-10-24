import json,pathlib,re,math
R=pathlib.Path.cwd(); A=R/'UnityProject/Assets'
alias={v['originalName']:v for v in json.loads((A/'RecoveredData/piece_aliases.json').read_text('utf-8-sig'))['aliases']}
scripts={re.search('guid: (\\w+)',p.read_text()).group(1):p.stem.replace('.cs','') for p in (A/'Scripts/Assembly-CSharp').glob('*.cs.meta')}
def fields(s):return dict(re.findall(r'^  (\w+): ?(.*)$',s,re.M))
def pf(name):
 t=(A/'GameObject'/f'{name}.prefab').read_text('utf-8-sig'); blocks={int(i):fields(b) for k,i,b in re.findall(r'^--- !u!(\d+) &(\d+)\n(.*?)(?=^---|\Z)',t,re.M|re.S)}
 trs={v['m_GameObject']:v for v in blocks.values() if 'm_LocalPosition' in v}
 def vec(s):return list(map(float,re.findall(r': ([\-\d.Ee]+)',s)))
 result=[]
 for b in blocks.values():
  if 'prefabAguaControl' not in b:continue
  typ=scripts.get(re.search('guid: (\\w+)',b['m_Script']).group(1),'?'); tr=trs[b['m_GameObject']]; off=vec(tr['m_LocalPosition']); rot=vec(tr['m_LocalRotation']); yaw=round(math.degrees(math.atan2(2*(rot[3]*rot[1]+rot[0]*rot[2]),1-2*(rot[1]**2+rot[2]**2))))
  root=tr['m_Father']=='{fileID: 0}'
  result.append((b,typ,[0,0,0] if root else off,yaw))
 return result
cache={}
def analyze(level):
 lay=json.loads((R/f'LevelData/01_Layouts/gm{level}.json').read_text('utf-8-sig'))['data']; sol=json.loads((R/f'LevelData/02_Solutions/so{level}.json').read_text('utf-8-sig'))['data']; pos=list(map(list,zip(lay['positionx'],lay['positiony'],lay['positionz'])))
 if 'move' in sol:
  for ox,oy,oz,dx,dy,dz in zip(sol['orix'],sol['oriy'],sol['oriz'],sol['desx'],sol['desy'],sol['desz']):
   ori=[(ox-3)*20,3.95+(oy-1)*7.6,(oz-3)*20];des=[(dx-3)*20,3.95+(dy-1)*7.6,(dz-3)*20]
   best=min(range(len(pos)),key=lambda j:sum((pos[j][k]-ori[k])**2 for k in range(3)))
   if sum((pos[best][k]-ori[k])**2 for k in range(3))<1:pos[best]=des
 else:pos=list(map(list,zip(sol['positionx'],sol['positiony'],sol['positionz'])));lay=sol
 nodes=[]
 for j,(name,p) in enumerate(zip(lay['pieza'],pos)):
  al=alias[name];base=al['basePrefab']
  if base not in cache:cache[base]=pf(base)
  for b,typ,off,yaw in cache[base]:
   source=typ=='ControlPiezaFuente';prefix='cascada' if source else 'origen' if 'origen1' in b else 'ori';ports=[b.get(prefix+str(k),'0')=='1' for k in range(1,7)]
   if not any(ports):continue
   color=int(b.get('color','0'));suffix=al.get('colorSuffix',0) or 0
   if suffix and (source or 'Cubeta' in typ or color):color=suffix
   nodes.append(dict(i=j,name=base,typ=typ,pos=[p[k]+off[k] for k in range(3)],yaw=yaw,ports=ports,color=color,source=source,wet=source,target='Cubeta' in typ or (typ=='ControlPiezaC' and int(b['tipoPieza'])>=2 and int(b['tipoPieza'])!=8),conf=lay['fijo'][j],links={}))
 dirs=[[-1,0,0],[0,0,1],[1,0,0],[0,0,-1],[0,1,0],[0,-1,0]]
 def direction(n,k):
  v=dirs[k];y=n['yaw']*math.pi/180;return [round(v[0]*math.cos(y)+v[2]*math.sin(y)),v[1],round(-v[0]*math.sin(y)+v[2]*math.cos(y))]
 for n in nodes:
  for k in range(6):
   if not n['ports'][k] or k==4 or 'Cubeta' in n['typ']:continue
   v=direction(n,k);p=n['pos'];exp=[p[z]+v[z]*20 for z in range(3)] if k<4 else p;high=-1000
   for other in nodes:
    q=other['pos']
    if other['i']==n['i'] or other['source'] or abs(exp[0]-q[0])>.4 or abs(exp[2]-q[2])>.4 or q[1]>p[1]+.45 or (k==5 and q[1]>p[1]-3) or q[1]<=high:continue
    accepted=q[1]<p[1]-3 and other['ports'][4]
    if k<4:accepted|=any(other['ports'][z] and sum(a*b for a,b in zip(direction(other,z),v))<-.9 for z in range(4))
    if not accepted:continue
    blocked=any(index not in [other['i'],n['i']] and abs(r[0]-exp[0])<.4 and abs(r[2]-exp[2])<.4 and q[1]+3<r[1]<=p[1]+.4 for index,r in enumerate(pos))
    if blocked:continue
    n['links'][k]=other;high=q[1]
 for n in nodes:
  if n['typ']=='ControlPiezaPfuente':
   for o in nodes:
    if o['i']!=n['i'] and o['ports'][5] and abs(o['pos'][0]-n['pos'][0])<.4 and abs(o['pos'][2]-n['pos'][2])<.4 and 3<o['pos'][1]-n['pos'][1]<8.1:n['links'][4]=o
 queue=[n for n in nodes if n['source']]
 for n in queue:
  for k,o in n['links'].items():
   if o['wet'] or (o['color'] and o['color']!=n['color']):continue
   o['wet']=True;o['color']=n['color'];queue.append(o)
 print('LEVEL',level,'wet',sum(n['wet'] for n in nodes),'targets',sum(n['wet'] and n['target'] for n in nodes),'/',sum(n['target'] for n in nodes))
 for n in nodes:
  print(n['i'],n['name'],[round(v,2) for v in n['pos']],''.join(str(k+1) for k,v in enumerate(n['ports']) if v),'C'+str(n['color']), 'WET' if n['wet'] else '-', 'TARGET' if n['target'] else '',{k+1:o['i'] for k,o in n['links'].items()},n['conf'])
 return nodes
if __name__=='__main__':
 import sys
 for level in map(int,sys.argv[1:] or [1101]):analyze(level)
