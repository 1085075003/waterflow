from pathlib import Path
import re
a=Path('Assets')
script={re.search(r'guid: (\w+)',p.read_text(encoding='utf-8')).group(1):p.stem.replace('.cs','') for p in (a/'Scripts/Assembly-CSharp').glob('*.cs.meta')}
for name in ['GrupoMech21','Grupo4Prefab','grupo5Prefab','grupo6Prefab']:
    text=(a/'GameObject'/f'{name}.prefab').read_text(encoding='utf-8-sig')
    blocks={int(i):(int(k),b,dict(re.findall(r'^  (\w+): ?(.*)$',b,re.M))) for k,i,b in re.findall(r'^--- !u!(\d+) &(\d+)\n(.*?)(?=^---|\Z)',text,re.M|re.S)}
    transforms={int(re.search(r'\d+',f['m_GameObject'])[0]):i for i,(k,b,f) in blocks.items() if k==4}
    for i,(k,b,f) in blocks.items():
        if k!=114:continue
        m=re.search(r'guid: (\w+)',f.get('m_Script',''));typ=script.get(m[1]) if m else None
        if typ not in ['Resorte2','Resorte2b','Resorte2c','Resorte2d']:continue
        print(name,typ,'speed',f.get('moveSpeed'),f.get('rotationSpeed'))
        for n in [9,10,11,12,14]:
            go=int(re.search(r'\d+',f['objeto'+str(n)])[0]) if 'objeto'+str(n) in f else 0
            if not go:continue
            tr=blocks[transforms[go]][1]
            children=re.search(r'  m_Children:(.*?)(?=  m_Father)',tr,re.S)
            ids=[int(x) for x in re.findall(r'fileID: (\d+)',children[1])] if children else []
            print(' objeto',n,blocks[go][2]['m_Name'],[(j,blocks[int(re.search(r'\d+',blocks[c][2]['m_GameObject'])[0])][2]['m_Name']) for j,c in enumerate(ids)])
        if typ=='Resorte2':
            for n in [3,7]:
                go=int(re.search(r'\d+',f['objeto'+str(n)])[0]);top=transforms[go]
                print(' ROTATING',n,blocks[go][2]['m_Name'])
                for c,(ck,cb,cf) in blocks.items():
                    if ck!=114:continue
                    match=re.search(r'guid: (\w+)',cf.get('m_Script',''));ct=script.get(match[1]) if match else None
                    if ct not in ['AguaControl','AguaControlC','AguaControlPFuente','AguasControl','AguasControl3','FuenteTest3','FuenteTest1','ControlPiezaFuente']:continue
                    cg=int(re.search(r'\d+',cf['m_GameObject'])[0]);cur=transforms[cg];parts=[]
                    while cur:
                        tf=blocks[cur][2];tg=int(re.search(r'\d+',tf['m_GameObject'])[0]);parts.insert(0,blocks[tg][2]['m_Name'])
                        if cur==top:
                            print('   ',ct,'/'.join(parts));break
                        cur=int(re.search(r'\d+',tf['m_Father'])[0])
