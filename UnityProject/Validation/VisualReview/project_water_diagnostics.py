from pathlib import Path
import re,json,math
import numpy as np
project=Path(__file__).resolve().parents[2]
scene=(project/'Assets/Scenes/Levely5_7x7.unity').read_text(encoding='utf-8-sig')
blocks={int(i):(int(k),dict(re.findall(r'^  (\w+): ?(.*)$',b,re.M))) for k,i,b in re.findall(r'^--- !u!(\d+) &(\d+)\n(.*?)(?=^---|\Z)',scene,re.M|re.S)}
def vals(value):return list(map(float,re.findall(r': ([\-\d.Ee]+)',value)))
transforms={int(re.search(r'\d+',b['m_GameObject'])[0]):i for i,(k,b) in blocks.items() if k==4}
def matrix(i):
    _,b=blocks[i];x,y,z,w=vals(b['m_LocalRotation']);p=vals(b['m_LocalPosition']);s=vals(b['m_LocalScale'])
    m=np.eye(4);m[:3,:3]=np.array([[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],[2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]])@np.diag(s);m[:3,3]=p
    father=int(re.search(r'\d+',b['m_Father'])[0]);return matrix(father)@m if father else m
cameras=[b for k,b in blocks.values() if k==20 and b.get('m_Enabled')=='1']
camera=cameras[0];go=int(re.search(r'\d+',camera['m_GameObject'])[0]);world=matrix(transforms[go]);rotation=world[:3,:3];rotation/=np.linalg.norm(rotation,axis=0)
position=world[:3,3]-rotation[:,2]*40.95803451538086
fov=float(camera['field of view']) if 'field of view' in camera else 26
tangent=math.tan(math.radians(fov*.5))
def screen(point):
    view=rotation.T@(np.array(point)-position);vx=.5+view[0]/(view[2]*tangent*(1280/720))*0.5;vy=.5+view[1]/(view[2]*tangent)*.5
    return [float(vx*1280),float((1-vy)*720),float(view[2])]
data=json.loads((project/'Validation/FullContent/gm2001-water-mesh-diagnostics.json').read_text(encoding='utf-8-sig'))
result=[]
for mesh in data['meshes']:
    center=np.array([mesh['center'][k] for k in 'xyz']);extent=np.array([mesh['size'][k]*.5 for k in 'xyz'])
    corners=np.array([screen(center+extent*np.array([x,y,z])) for x in [-1,1] for y in [-1,1] for z in [-1,1]])
    result.append(dict(mesh,path=mesh['path'],screenCenter=screen(center),screenBounds=[float(corners[:,0].min()),float(corners[:,1].min()),float(corners[:,0].max()),float(corners[:,1].max())]))
out={'projectionSource':'Original7x7 camera pose plus actual strictAspect backward distance40.9580345, FOV26,1280x720. Upper-left pixel origin. Independent computed projection, pending Unity full-renderer diagnostics.', 'cameraPosition':position.tolist(),'meshes':result}
(project/'Validation/VisualReview/gm2001-water-projections.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf-8')
point=np.array([800,650])
nearest=sorted(result,key=lambda x:np.linalg.norm(np.array(x['screenCenter'][:2])-point))[:12]
print('Camera',position)
for mesh in nearest:print(mesh['path'],mesh['screenCenter'],mesh['screenBounds'])
