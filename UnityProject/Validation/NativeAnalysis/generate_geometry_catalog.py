import json,pathlib,re,time,functools,collections
import disassemble as d
from extract_native_geometry import GeometryExtractor,declared_fields

root=d.ROOT.parents[2];assets=root/'UnityProject/Assets'
aliases={a['originalName']:a['basePrefab'] for a in json.loads((assets/'RecoveredData/piece_aliases.json').read_text('utf-8-sig'))['aliases']}
schema=json.loads((assets/'Resources/NativeConfiguration.json').read_text('utf-8-sig'))
rules=next(c for c in schema['components'] if c['type']=='ControlPiezaC')
types={name:typ for typ,name in declared_fields('ControlPiezaC')}
guid=re.search(r'guid: (\w+)',(assets/'Scripts/Assembly-CSharp/ControlPiezaC.cs.meta').read_text())[1]
def fields(block):return dict(re.findall(r'^  (\w+): ?(.*)$',block,re.M))
def ref(v):return int(re.search(r'fileID: (-?\d+)',v)[1])
@functools.lru_cache(None)
def controllers(base):
    file=assets/'GameObject'/(base+'.prefab')
    if not file.exists():return []
    blocks={int(i):(int(k),fields(b)) for k,i,b in re.findall(r'^--- !u!(\d+) &(-?\d+)\n(.*?)(?=^---|\Z)',file.read_text('utf-8-sig'),re.M|re.S)}
    transforms={ref(f['m_GameObject']):(i,f) for i,(k,f) in blocks.items() if k==4}
    tr_byid={i:f for _,(i,f) in transforms.items()}
    result=[]
    for _,(kind,f) in blocks.items():
        if kind!=114 or guid not in f.get('m_Script',''):continue
        go=ref(f['m_GameObject']);_,transform=transforms[go]
        path=[];current=transform
        while ref(current['m_Father'])!=0:
            path.append(blocks[ref(current['m_GameObject'])][1]['m_Name'])
            current=tr_byid[ref(current['m_Father'])]
        path='/'.join(reversed(path))
        values={}
        for field,value in f.items():
            typ=types.get(field)
            if typ in ['GameObject','Material'] and 'fileID: 0}' in value:values[field]=None
            if typ=='string':values[field]=value
            elif typ in ['int','float','bool']:
                try:values[field]=float(value) if typ=='float' else int(value)
                except ValueError:pass
        result.append((path,blocks[go][1]['m_Name'],values))
    return result

def token_values(code):
    result={}
    for t in schema['tokens']:
        p=code.find(t['start'])
        if p<0:continue
        p+=len(t['start'])
        if p>=len(code):continue
        count=t.get('firstCharactersOnly',0)
        if count>0:
            end=code.find(t['end'],p)
            if count>1 and end>=p:count=min(count,end-p)
            result[t['field']]=code[p:p+count]
        else:
            end=code.find(t['end'],p)
            if end>=p:result[t['field']]=code[p:end]
    for i,marker in enumerate('juniop'):
        p=code.find(marker)
        if p>=0 and p+1<len(code) and not (i==0 and p>0 and code[p-1]=='<'):result['str'+str(i+1)]=code[p+1]
    for opening,closing,names,width in [('<x','x>',['configRes'+str(i+1) for i in range(4)],4),('sh','eh',['palo'+str(i+1) for i in range(4)],1)]:
        p=code.find(opening);e=code.find(closing,p+len(opening))
        if p>=0 and e>=p:
            text=code[p+len(opening):e]
            for i,name in enumerate(names):
                if (i+1)*width<=len(text):result[name]=text[i*width:(i+1)*width]
    walls=result.get('walls','')
    for i in range(min(4,len(walls))):result['wall'+str(i+1)]=walls[i]
    if len(walls)>=5:result['configFuente']=walls[4]
    if len(walls)>=6:result['configFuenteCentro']=walls[5]
    for i in range(4):
        if len(walls)>=10+i*4:result['configPit'+str(i+1)]=walls[6+i*4:10+i*4]
    if result.get('conF'):result['configFB']=result['conF'][0]
    return result
def configured(defaults,code,isroot):
    values=dict(defaults)
    if isroot:
        prefix=code[:1]
        for p in rules['prefixes']:
            if p['prefix']==prefix:
                for a in p['assignments']:values[a['field']]=int(a['value'])
                break
        parsed=token_values(code)
        for t in rules['tokens']:
            if t['source'] not in parsed:continue
            value=parsed[t['source']]
            if types.get(t['target'])=='int':
                try:value=int(value)
                except ValueError:continue
            values[t['target']]=value
    # All blank serialized strings behave as empty in the restored loader.
    for name,typ in types.items():
        if typ=='string' and name not in values:values[name]=''
    return values

# Cache disassembly rather than repeatedly parsing each original instruction.
original_md=d.md
class CachedDisassembly:
    def __init__(self):self.cache={}
    def disasm(self,data,address):
        if address==0x1083B5C:return iter(original_md.disasm(bytes.fromhex('c0035fd6'),address))
        key=(address,len(data))
        if key not in self.cache:self.cache[key]=list(original_md.disasm(data,address))
        return iter(self.cache[key])
d.md=CachedDisassembly()

contexts=set();files=0
for directory in ['01_Layouts','02_Solutions']:
    for p in (root/'LevelData'/directory).glob('*.json'):
        data=json.loads(p.read_text('utf-8-sig'))['data'];files+=1
        if data.get('__type__')!='Save':continue
        for name,code in zip(data['pieza'],data['fijo']):
            base=aliases.get(name)
            if base is None:continue
            for choice in ([base,'BloqueChorro2'] if name.startswith('sq292eq') else [base]):
                if controllers(choice):contexts.add((choice,code))
print('C contexts',len(contexts),'files',files,flush=True)
entries=[];failures=[];recipe_sets=[];unique={};started=time.time()
for ix,(base,code) in enumerate(sorted(contexts)):
    for path,name,defaults in controllers(base):
        values=configured(defaults,code,path=='')
        ex=GeometryExtractor(values,name if path else base)
        try:
            recipes=ex.evaluate();changes=list(ex.existing_changes.values());water=ex.water_configuration()
            for recipe in recipes:
                if recipe['prefabField']=='0' and recipe['nativeInstantiateVA']=='0x108d194':
                    recipe['prefabField']='prefabPitorro1'
                    recipe['authoredPrefabMissing']=True
                recipe['hasActive']='active' in recipe
                recipe['hasCylinderColor']='colorCilindro' in recipe
                recipe['position']=recipe.get('localPosition',recipe['initialPosition'])
                recipe['rotationIsLocal']='localRotation' in recipe and 'rotation' not in recipe
                recipe['rotation']=recipe.get('rotation',recipe.get('localRotation',recipe['quaternion']))
                recipe['scale']=recipe.get('localScale',[])
                for key in ['initialPosition','localPosition','localScale','quaternion','rotation','parent']:
                    if key=='rotation':continue
                    recipe.pop(key,None)
            for change in changes:change['hasActive']='active' in change
            group={'recipes':recipes,'changes':changes,'waterPorts':water['ports'],'waterDirections':water['directions'],'waterRebose':water['rebose'],'waterWalls':water['walls'],'controllerWalls':water['controllerWalls']}
            signature=json.dumps(group,sort_keys=True,separators=(',',':'))
            if signature not in unique:unique[signature]=len(recipe_sets);recipe_sets.append(group)
            entries.append({'basePrefab':base,'configuration':code,'path':path,'recipeSet':unique[signature]})
        except Exception as error:failures.append({'basePrefab':base,'configuration':code,'path':path,'error':str(error),'pc':hex(ex.pc),'trace':ex.trace[-4:]})
    if ix and ix%200==0:print(ix,'contexts;',len(failures),'failures;',round(time.time()-started,1),'s',flush=True)
output={'version':2,'source':'Verified ARM64 ControlPiezaC.Start 10847FC geometry segment and called helpers; no native game code executed.','entries':entries,'recipeSets':recipe_sets}
(d.ROOT/'RecoveredNativeGeometry.json.ready').write_text(json.dumps(output,ensure_ascii=False,separators=(',',':')),'utf-8')
report={'contexts':len(contexts),'entries':len(entries),'uniqueRecipeSets':len(recipe_sets),'recipeCount':sum(len(g['recipes']) for g in recipe_sets),'failures':failures,'durationSeconds':round(time.time()-started,2)}
(d.ROOT/'native-geometry-extraction-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),'utf-8')
print(json.dumps({k:v for k,v in report.items() if k!='failures'}),flush=True)
print('FAILURE SAMPLES',json.dumps(failures[:10],ensure_ascii=False),flush=True)
