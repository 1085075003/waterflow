import pathlib,json,re,itertools,math,hashlib
root=pathlib.Path(__file__).resolve().parents[3];assets=root/'UnityProject/Assets'
def parse(p):
 t=p.read_text(encoding='utf-8-sig')
 return {int(i):(int(k),b,dict(re.findall(r'^  (\w+): ?(.*)$',b,re.M)))for k,i,b in re.findall(r'^--- !u!(\d+) &(\d+)\n(.*?)(?=^---|\Z)',t,re.M|re.S)}
def vec(t):return [float(v)for v in re.findall(r'[xyzw]: ([^,}]+)',t)]
def rotate(q,p):
 x,y,z,w=q;norm=math.sqrt(sum(v*v for v in q));x,y,z,w=[v/norm for v in q]
 return [(1-2*y*y-2*z*z)*p[0]+(2*x*y-2*z*w)*p[1]+(2*x*z+2*y*w)*p[2],(2*x*y+2*z*w)*p[0]+(1-2*x*x-2*z*z)*p[1]+(2*y*z-2*x*w)*p[2],(2*x*z-2*y*w)*p[0]+(2*y*z+2*x*w)*p[1]+(1-2*x*x-2*y*y)*p[2]]
def apply(p,t):
 p=rotate(vec(t['m_LocalRotation']),[x*y for x,y in zip(p,vec(t['m_LocalScale']))]);return [x+y for x,y in zip(p,vec(t['m_LocalPosition']))]
prefab=assets/'GameObject/Fuente5Color4.prefab';blocks=parse(prefab);ts={int(re.search(r'\d+',f['m_GameObject'])[0]):i for i,(k,b,f)in blocks.items()if k==4}
go={f['m_Name']:i for i,(k,b,f)in blocks.items()if k==1};water=blocks[ts[go['water1']]][2]
flow=json.loads((assets/'Resources/RecoveredNativeWaterFlows.json').read_text(encoding='utf-8-sig'))
base=root/'UnityProject/Validation/FinalRender/NativeGeometryRegression1';graph=json.loads((base/'gm2050-water-graph.json').read_text(encoding='utf-8-sig'));diag=json.loads((base/'gm2050-water-mesh-diagnostics.json').read_text(encoding='utf-8-sig'))
cases=[]
for record,name in [(13,'q20'),(42,'q19'),(55,'q20')]:
 node=next(n for n in graph['nodes']if n['sourceRecord']==record);incoming=node['incoming'];recipe=next(c['recipes'][0]for c in flow['cases']if c['phase']==0 and c['origen']==incoming)
 q=blocks[ts[go[name]]][2];mesh=(assets/f'Mesh/{name}.asset').read_text(encoding='utf-8-sig');aabb=mesh.split('  m_LocalAABB:\n')[1];center=vec(re.search(r'm_Center: (.*)',aabb)[1]);extent=vec(re.search(r'm_Extent: (.*)',aabb)[1])
 source=vec('{'+', '.join(k+': '+str(node['position'][k])for k in 'xyz')+'}')
 points=[]
 for signs in itertools.product([-1,1],repeat=3):
  p=apply(apply([c+e*s for c,e,s in zip(center,extent,signs)],q),water)
  p=rotate(recipe['quaternion'],[v*s for v,s in zip(p,recipe['localScale'])]);points.append([s+v+o for s,v,o in zip(source,p,recipe['localPosition'])])
 lo=[min(p[i]for p in points)for i in range(3)];hi=[max(p[i]for p in points)for i in range(3)];expectedCenter=[(a+b)/2 for a,b in zip(lo,hi)];expectedSize=[b-a for a,b in zip(lo,hi)]
 actual=next(m for m in diag['meshes']if m['path'].startswith(f'[{record:03d}]') and m['path'].endswith('/Original Fuente5Color4/water1/'+name));ac=[actual['center'][k]for k in 'xyz'];az=[actual['size'][k]for k in 'xyz']
 cases.append({'record':record,'incoming':incoming,'nativeSourcePosition':source,'nativeRootLocalPosition':recipe['localPosition'],'nativeWorldQuaternion':recipe['quaternion'],'nativeRootLocalScale':recipe['localScale'],'sourceRotation':node['rotation'],'sourceRootWorldScale':1,'originalPrefabQPose':q,'expectedWorldBoundsCenter':expectedCenter,'actualWorldBoundsCenter':ac,'maximumCenterError':max(abs(a-b)for a,b in zip(expectedCenter,ac)),'expectedWorldBoundsSize':expectedSize,'actualWorldBoundsSize':az,'maximumSizeError':max(abs(a-b)for a,b in zip(expectedSize,az)),'actualPath':actual['path']})
result={'scope':'Independent original mesh-AABB and authored transform composition with literal native vertical A setters, compared to actual Unity2050 diagnostics. Uses no actual q transform to compute expected result. Source piece worldposition comes from actual sourcegraph; original water root/OriginCentro/Origin6 all authored zero position and identity, worldscale1 after C scale10*Agua.1.','cases':cases,'rawEvidence':['ControlPiezaC.Awake1082AF0 Instantiate worldidentity;1082B24 parent;1082B4C localzero;1082B78 scale.1','FT3.doInstanciarAgua10A64F8/10A6638 Instantiate explicit worldposrot;10A652C/10A666C parent retainsworld;10A6554/10A6694 local(8.64,-1.35,0);10A6584/10A66C4 scale(2.31,2.1,1.5)','Fuente5Color4 root authored pos11.6,-1.7,0 is overwritten by these explicit native setters; Aguas/OrigenCentro/Origen6 original children have zero localposition and identity rotation/scale1'],'limits':['Only confirms actual2050 retained A-plane pose, not pixel equivalence or complete animation lifecycle. Original controller active-frame lifecycle audited separately by presentation.','No runtime, asset, renderer, shader or original data mutation.']}
out=pathlib.Path(__file__).with_name('native-q-pose-audit.json');out.write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
for c in cases:print(c['record'],'centerError',c['maximumCenterError'],'sizeError',c['maximumSizeError'])
