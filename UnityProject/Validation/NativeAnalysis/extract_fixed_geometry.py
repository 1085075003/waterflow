"""Read-only ARM64 extraction of ControlFijo1's authored wall geometry."""
import json,pathlib,re,struct,functools,time
import disassemble as d
from extract_water_recipes import Extractor
from extract_native_geometry import GeometryExtractor,declared_fields

root=d.ROOT.parents[2];assets=root/'UnityProject/Assets'
schema=json.loads((assets/'Resources/NativeConfiguration.json').read_text('utf-8-sig'))
rules=next(c for c in schema['components'] if c['type']=='ControlFijo1')
types={name:typ for typ,name in declared_fields('ControlFijo1')}
aliases={a['originalName']:a['basePrefab'] for a in json.loads((assets/'RecoveredData/piece_aliases.json').read_text('utf-8-sig'))['aliases']}
guid=re.search(r'guid: (\w+)',(assets/'Scripts/Assembly-CSharp/ControlFijo1.cs.meta').read_text())[1]
def ref(value):return int(re.search(r'fileID: (-?\d+)',value)[1])
def fields(block):return dict(re.findall(r'^  (\w+): ?(.*)$',block,re.M))
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
        go=ref(f['m_GameObject']);_,transform=transforms[go];path=[];current=transform
        while ref(current['m_Father'])!=0:
            path.append(blocks[ref(current['m_GameObject'])][1]['m_Name']);current=tr_byid[ref(current['m_Father'])]
        values={}
        for name,value in f.items():
            typ=types.get(name)
            if typ in ['GameObject','Material'] and 'fileID: 0}' in value:values[name]=None
            elif typ=='string':values[name]=value
            elif typ in ['int','float','bool']:
                try:values[name]=float(value) if typ=='float' else int(value)
                except ValueError:pass
        result.append(('/'.join(reversed(path)),blocks[go][1]['m_Name'],values))
    return result
def configured(defaults,code,isroot):
    result=dict(defaults)
    if isroot:
        for p in rules['prefixes']:
            if p['prefix']==code[:1]:
                for a in p['assignments']:result[a['field']]=int(a['value'])
                break
        values={}
        for token in schema['tokens']:
            start=code.find(token['start'])
            if start<0:continue
            start+=len(token['start']);end=code.find(token['end'],start)
            count=token.get('firstCharactersOnly',0)
            if count:values[token['field']]=code[start:start+count]
            elif end>=start:values[token['field']]=code[start:end]
        for write in rules['tokens']:
            if write['source'] in values:result[write['target']]=values[write['source']]
        walls=values.get('walls','')
        # Native packed q0 write at doProcesoPiezas101A640.
        for index in range(min(4,len(walls))):result['lado'+str(index+1)]=int(walls[index])
    for name,typ in types.items():
        if typ=='string' and name not in result:result[name]=''
    return result

class FixedExtractor(GeometryExtractor):
    def __init__(self,inputs,name):
        Extractor.__init__(self,{},class_name='ControlFijo1')
        self.name=name;self.nameptr=self.ref(name);self.original_objects={};self.instance_fields={};self.existing_changes={}
        self.put('x19',self.base)
        for off,field in self.fields.items():
            if types.get(field) in ['GameObject','Material']:
                ptr=self.ref(field);self.write(self.base+off,ptr,8);self.original_objects[ptr]=field
        for field,value in inputs.items():
            if field not in self.fields.values():continue
            off=next(o for o,n in self.fields.items() if n==field);typ=types.get(field)
            if value is None:self.write(self.base+off,0,8)
            elif isinstance(value,str):self.write(self.base+off,self.ref(value),8)
            elif typ=='float':self.write(self.base+off,struct.unpack('<I',struct.pack('<f',value))[0],4)
            else:self.write(self.base+off,value,1 if typ=='bool' else 4)
        self.ui=self.ref('UIManagerSelScript_TypeInfo');self.statics=self.ref('UIManagerSelScript.Statics')
        self.write(self.ui+0xb8,self.statics,8);self.write(self.statics,0,4)
        self.put('x21',0x2368f50);self.put('x22',0x236a700);self.put('x23',self.base+0x70);self.put('x24',0x236a620)
    def read(self,addr,size):
        if addr==0x236a620:return self.ui
        return super().read(addr,size)
    def call(self,address):
        if d.method_map.get(address,{}).get('Name','')=='UnityEngine.Object$$Instantiate<object>':
            # Constructor stores the selected prefab in another private field.
            # It remains a reference to the original serialized prefab field.
            pass
        return super().call(address)
    def evaluate(self):
        fixed=self.read(self.base+0x60,1)
        if not fixed:return []
        self.run('ControlFijo1$$StaticGeometrySegment')
        for ptr,obj in self.objects.items():
            if 'parentRecipe' in obj and 'materialField' in obj:
                self.calls[obj['parentRecipe']].setdefault('childMaterials',[]).append({'childIndex':obj['childIndex'],'materialField':obj['materialField']})
        return self.calls

d.data['ScriptMethod'].append({'Name':'ControlFijo1$$StaticGeometrySegment','Address':0x107ADB0})
d.addresses=sorted(set(d.addresses+[0x107ADB0]))
original_md=d.md
class CachedDisassembly:
    def __init__(self):self.cache={}
    def disasm(self,data,address):
        if address==0x107DBDC:return iter(original_md.disasm(bytes.fromhex('c0035fd6'),address))
        key=(address,len(data))
        if key not in self.cache:self.cache[key]=list(original_md.disasm(data,address))
        return iter(self.cache[key])
d.md=CachedDisassembly()

if __name__=='__main__':
    contexts=set()
    for directory in ['01_Layouts','02_Solutions']:
        for p in (root/'LevelData'/directory).glob('*.json'):
            data=json.loads(p.read_text('utf-8-sig'))['data']
            if data.get('__type__')!='Save':continue
            for name,code in zip(data['pieza'],data['fijo']):
                base=aliases.get(name)
                if base and controllers(base):contexts.add((base,code))
    print('Fijo1 contexts',len(contexts),flush=True)
    entries=[];recipe_sets=[];unique={};failures=[];started=time.time()
    for ix,(base,code) in enumerate(sorted(contexts)):
        for path,name,defaults in controllers(base):
            values=configured(defaults,code,path=='');ex=FixedExtractor(values,name if path else base)
            try:
                recipes=ex.evaluate();changes=list(ex.existing_changes.values())
                for recipe in recipes:
                    recipe['hasActive']='active' in recipe
                    recipe['position']=recipe.get('localPosition',recipe['initialPosition'])
                    recipe['rotationIsLocal']='localRotation' in recipe and 'rotation' not in recipe
                    recipe['rotation']=recipe.get('rotation',recipe.get('localRotation',recipe['quaternion']))
                    recipe['scale']=recipe.get('localScale',[])
                    for k in ['initialPosition','localPosition','localScale','quaternion','parent']:recipe.pop(k,None)
                for change in changes:change['hasActive']='active' in change
                group={'recipes':recipes,'changes':changes};signature=json.dumps(group,sort_keys=True,separators=(',',':'))
                if signature not in unique:unique[signature]=len(recipe_sets);recipe_sets.append(group)
                entries.append({'basePrefab':base,'configuration':code,'path':path,'recipeSet':unique[signature]})
            except Exception as error:failures.append({'basePrefab':base,'configuration':code,'path':path,'error':str(error),'pc':hex(ex.pc),'trace':ex.trace[-5:]})
        if ix and ix%100==0:print(ix,len(failures),'failures',round(time.time()-started,1),'s',flush=True)
    result={'version':1,'source':'Native ControlFijo1.Start107ADB0..107DBD8; complete static wall/arch/central geometry and hide-cubo pass. Read-only static interpretation.','entries':entries,'recipeSets':recipe_sets}
    (d.ROOT/'RecoveredNativeFixedGeometry.json.ready').write_text(json.dumps(result,ensure_ascii=False,separators=(',',':')),'utf-8')
    report={'contexts':len(contexts),'entries':len(entries),'uniqueRecipeSets':len(recipe_sets),'recipeCount':sum(len(g['recipes']) for g in recipe_sets),'failures':failures,'durationSeconds':round(time.time()-started,2)}
    (d.ROOT/'native-fixed-geometry-extraction-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),'utf-8')
    print(json.dumps(report,ensure_ascii=False),flush=True)
