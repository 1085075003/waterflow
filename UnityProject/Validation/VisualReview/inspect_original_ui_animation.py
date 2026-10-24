from pathlib import Path
import re,json
project=Path(__file__).resolve().parents[2];assets=project/'Assets'
scripts={re.search(r'guid: (\w+)',p.read_text(encoding='utf-8')).group(1):p.name[:-8] for p in (assets/'Scripts').rglob('*.cs.meta')}
controllers={re.search(r'guid: (\w+)',p.with_suffix(p.suffix+'.meta').read_text(encoding='utf-8')).group(1):p.name for p in assets.rglob('*.controller')}
result=[]
for path in (assets/'Scenes').rglob('*.unity'):
    text=path.read_text(encoding='utf-8-sig')
    blocks={int(i):(int(k),b,dict(re.findall(r'^  (\w+): ?(.*)$',b,re.M))) for k,i,b in re.findall(r'^--- !u!(\d+) &(\d+)\n(.*?)(?=^---|\Z)',text,re.M|re.S)}
    def ref(text):
        m=re.search(r'fileID: (-?\d+)',text);return int(m[1]) if m else 0
    trs={ref(f['m_GameObject']):(i,f) for i,(k,b,f) in blocks.items() if k in [4,224]}
    def hierarchy(go):
        name=blocks[go][2]['m_Name'];_,tr=trs[go];parent=ref(tr['m_Father'])
        return hierarchy(ref(blocks[parent][2]['m_GameObject']))+'/'+name if parent else name
    entry={'scene':str(path.relative_to(project)),'complete':[],'uiAnimators':[],'delayComponents':[]}
    for i,(kind,raw,f) in blocks.items():
        go=ref(f.get('m_GameObject',''))
        if kind==114:
            match=re.search(r'guid: (\w+)',f.get('m_Script',''));typ=scripts.get(match[1]) if match else None
            if typ in ['SaveLoadManager','ControladorJuego']:
                cg=ref(f.get('cajaComplete',''));entry['complete'].append({'owner':typ,'target':hierarchy(cg) if cg else None,'go':cg})
            if typ=='EnableObjectsWithDelay':
                targets=[{'path':hierarchy(int(target)) if int(target) else None,'delay':float(delay)} for target,delay in re.findall(r'targetObject: \{fileID: (\d+)\}\n    startDelay: ([\d.]+)',raw)]
                entry['delayComponents'].append({'path':hierarchy(go),'targets':targets,'raw':raw})
        if kind==95 and go:
            name=hierarchy(go)
            if name.startswith('Canvas'):
                match=re.search(r'guid: (\w+)',f.get('m_Controller',''));ctrl=controllers.get(match[1]) if match else None
                entry['uiAnimators'].append({'path':name,'enabled':f.get('m_Enabled'),'controller':ctrl})
    result.append(entry)
out=project/'Validation/VisualReview/original-ui-animation-structure.json';out.write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
for entry in result:
    print(entry['scene'],entry['complete'],'delayComponents',[(x['path'],x['targets']) for x in entry['delayComponents']])
    print('\n'.join(str(a) for a in entry['uiAnimators'] if 'Complete' in a['path'] or 'Fade' in a['path'] or len(a['path'].split('/'))<4))
