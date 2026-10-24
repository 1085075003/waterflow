"""Static inventory of every normal Save1 board; no Unity/native code execution."""
import collections,json,pathlib,re

ROOT=pathlib.Path(__file__).resolve().parent
PROJECT=ROOT.parents[1]
DATA=PROJECT/'Assets/RecoveredData'
def load(p):return json.loads(p.read_text(encoding='utf-8'))
def token(s,a,b):
    i=s.find(a)
    if i<0:return ''
    i+=len(a);j=s.find(b,i)
    return s[i:j] if j>=i else ''
def main():
    aliases={x['originalName']:x for x in load(DATA/'preview_aliases.json')['aliases']}
    names={}
    for p in (PROJECT/'Assets/Scripts/Assembly-CSharp').glob('*.cs.meta'):
        names[re.search(r'^guid: (\w+)',p.read_text('utf8'),re.M)[1]]=p.name[:-8]
    components={}
    for p in (PROJECT/'Assets/GameObject').glob('*.prefab'):
        docs={int(i):(k,b) for k,i,b in re.findall(r'--- !u!(\d+) &(\d+)\n(.*?)(?=\n--- !u!|\Z)',p.read_text('utf8'),re.S)}
        roots=[]
        for k,b in docs.values():
            if k=='4' and re.search(r'm_Father: \{fileID: 0\}',b):roots.append(int(re.search(r'm_GameObject: \{fileID: (\d+)\}',b)[1]))
        types=[]
        for k,b in docs.values():
            m=re.search(r'm_GameObject: \{fileID: (\d+)\}',b)
            script=re.search(r'm_Script: \{fileID: \d+, guid: (\w+)',b)
            if k=='114' and m and int(m[1]) in roots and script:types.append(names.get(script[1],script[1]))
        components['BloqueFijo' if p.stem=='BloqueFijo_0' else p.stem]=types
    counter=collections.defaultdict(collections.Counter);examples=collections.defaultdict(list)
    action_unsupported=[];channel_unsupported=[];long_sequences=[];missing_prefabs=[]
    levels=[];records=0;proxy=0
    definitions=load(ROOT/'configuration-tokens.json')['tokens']
    exceptions=collections.Counter();markers=[]
    for f in sorted((DATA/'LevelData/02_Solutions').glob('so*.json')):
        so=load(f)['data']
        if so['__type__']!='Save1':continue
        level=int(f.stem[2:]);levels.append(level)
        for i,value in enumerate(so['excep']):
            exceptions[value]+=1
            if value=='z' or all(so[a][i]==so[b][i] for a,b in [('orix','desx'),('oriy','desy'),('oriz','desz')]):markers.append([level,i+1,value])
        gm=load(DATA/f'LevelData/01_Layouts/gm{level}.json')['data']
        for i,(name,config) in enumerate(zip(gm['pieza'],gm['fijo'])):
            records+=1;alias=aliases.get(name,{});base=alias.get('basePrefab',name.replace('(Clone)','').strip());types=components.get(base,[])
            if base not in components:missing_prefabs.append([level,i,name,base])
            parsed={t['field']:token(config,t['start'],t['end']) for t in definitions}
            channel=parsed['resorte'];sequence=parsed['tipoResorte'];proxy+=channel=='9'
            for field in ['resorte','tipoResorte','valEncima','valMove','valDebajo','conResorte3','modoRes3','conTipoRes3','conF']:
                value=parsed[field]
                if value:
                    counter[field][value]+=1
                    key=field+'='+value
                    if len(examples[key])<4:examples[key].append({'level':level,'record':i,'prefab':base,'types':types,'configuration':config})
            if channel and channel!='9':
                if any(c not in '12345678' for c in channel):channel_unsupported.append([level,i,channel,sequence])
                for part in sequence.split('c'):
                    if len(part)>3:long_sequences.append([level,i,channel,sequence])
                    if any(c not in '0123456789' for c in part):action_unsupported.append([level,i,channel,sequence])
                counter['springRootComponent'].update(t for t in types if t.startswith('Control') or t=='Resorte')
    report={'scope':'All800 original Save1 layouts and their11919 recorded player steps; static inventory does not prove gameplay success.',
            'levels':len(levels),'layoutRecordsIncludingNativeSt9Proxies':records,'st9ProxyRecordsExcludedInNormalMode':proxy,
            'exceptions':dict(exceptions),'samePositionOrZMarkers':markers,'fields':{k:dict(v) for k,v in counter.items()},
            'unsupportedActionCharacters':action_unsupported,'unsupportedChannelCharacters':channel_unsupported,
            'sequencesLongerThanNativeThreePerChannel':long_sequences,'unresolvedCanonicalPrefabs':missing_prefabs,
            'examples':dict(examples),'limits':['Native mode0 and mode1 actor filtering remains authoritative.','A supported encoding inventory does not prove native collision, reversal or all water timing. RunAll is the independent production-gameplay verification.','Resorte3 modes other than0/1/2/3 require actual native branch review if present.']}
    (ROOT/'save1-mechanism-code-audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in report.items()if k not in ['fields','examples','samePositionOrZMarkers']},ensure_ascii=False,indent=2))
    print('FIELD_COUNTS',json.dumps(report['fields'],ensure_ascii=False))
if __name__=='__main__':main()
