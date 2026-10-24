import pathlib, re, collections, json
import numpy as np

A=pathlib.Path('UnityProject/Assets')
scripts={re.search(r'guid: (\w+)',p.read_text()).group(1):p.stem.replace('.cs','') for p in (A/'Scripts/Assembly-CSharp').glob('*.cs.meta')}
def vector(s):return list(map(float,re.findall(r': ([\-\d.Ee]+)',s)))
def inspect(name):
    text=(A/'GameObject'/f'{name}.prefab').read_text('utf-8-sig')
    blocks={int(i):(int(k),dict(re.findall(r'^  (\w+): ?(.*)$',b,re.M))) for k,i,b in re.findall(r'^--- !u!(\d+) &(\d+)\n(.*?)(?=^---|\Z)',text,re.M|re.S)}
    transforms={int(re.search(r'\d+',b['m_GameObject'])[0]):(i,b) for i,(k,b) in blocks.items() if k==4}
    def matrix(i):
        _,b=blocks[i];x,y,z,w=vector(b['m_LocalRotation']);p=vector(b['m_LocalPosition']);s=vector(b['m_LocalScale']);
        m=np.eye(4);m[:3,:3]=np.array([[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],[2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]])@np.diag(s);m[:3,3]=p
        father=int(re.search(r'\d+',b['m_Father'])[0]);return matrix(father)@m if father else np.eye(4)
    def active(go):
        i,tr=transforms[go];_,b=blocks[go];father=int(re.search(r'\d+',tr['m_Father'])[0]);return b.get('m_IsActive')=='1' and (not father or active(int(re.search(r'\d+',blocks[father][1]['m_GameObject'])[0])))
    result=[]
    for i,(k,b) in blocks.items():
        if k!=114:continue
        match=re.search(r'guid: (\w+)',b.get('m_Script',''));typ=scripts.get(match[1],'?') if match else '?'
        if typ not in ['AguaControlC','AguaControl','AguaControlPFuente','ControlPiezaFuente','activaEsfera']:continue
        go=int(re.search(r'\d+',b['m_GameObject'])[0]);mat=matrix(transforms[go][0]);
        result.append(dict(id=i,type=typ,name=blocks[go][1]['m_Name'],active=active(go),position=mat[:3,3].round(4).tolist(),scale=np.linalg.norm(mat[:3,:3],axis=0).round(4).tolist(),fields={k:v for k,v in b.items() if k.startswith(('ori','cascada','config','tipo','color','dirCentro','esfera1'))}))
    return result
if __name__=='__main__':
    report={name:inspect(name) for name in ['grupo1Prefab','grupo2Prefab','GrupoMech21','Grupo4Prefab','grupo5Prefab','grupo6Prefab']}
    pathlib.Path('UnityProject/Validation/NativeAnalysis/preview-water-components.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    for name,entries in report.items():
        print(name,dict(collections.Counter(e['type'] for e in entries if e['active'])))
        for entry in entries:
            if entry['active']:print(' ',entry['id'],entry['type'],entry['name'],entry['position'],entry['scale'])
