import pathlib,re,json,collections
from inspect_preview_water import vector
def inspect(name):
    a=pathlib.Path('UnityProject/Assets')
    scripts={re.search(r'guid: (\w+)',p.read_text()).group(1):p.stem.replace('.cs','') for p in(a/'Scripts/Assembly-CSharp').glob('*.cs.meta')}
    text=(a/'GameObject'/f'{name}.prefab').read_text('utf-8-sig')
    blocks={int(i):(int(k),dict(re.findall(r'^  (\w+): ?(.*)$',b,re.M))) for k,i,b in re.findall(r'^--- !u!(\d+) &(\d+)\n(.*?)(?=^---|\Z)',text,re.M|re.S)}
    transforms={int(re.search(r'\d+',b['m_GameObject'])[0]):(i,b)for i,(k,b) in blocks.items()if k==4}
    def path(go):
        _,b=blocks[go]; _,t=transforms[go];father=int(re.search(r'\d+',t['m_Father'])[0]);return (path(int(re.search(r'\d+',blocks[father][1]['m_GameObject'])[0]))+'/'if father else '')+b.get('m_Name','?')
    for i,(k,b) in blocks.items():
        if k!=114:continue
        match=re.search(r'guid: (\w+)',b.get('m_Script',''));typ=scripts.get(match[1],'?')if match else '?'
        if typ not in ['AguaControlC','AguasControl3','AguaControlFuente','ControlPiezaFuente','FuenteTest3','FuenteTest1','AguasControl','AguaControl','AguaControlPFuente']:continue
        go=int(re.search(r'\d+',b['m_GameObject'])[0]);p=path(go)
        fields={k:v for k,v in b.items()if k.startswith(('ori','esf','tipo','config','color','dirPlano','activar','Aguas','planoCentro'))}
        print(i,typ,p,fields)
if __name__=='__main__':inspect('grupo6Prefab')
