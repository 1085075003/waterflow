"""Statically evaluate C's verified geometry segment into prefab recipes.
This never executes game code or opens Unity. Only ARM64 arithmetic, branches,
field accesses, and explicitly recognized Unity construction/setter calls run.
"""
import json,re,struct,sys,math,functools
import disassemble as d
from extract_water_recipes import Extractor

SCRIPTROOT=d.ROOT.parents[1]/'Assets/Scripts/Assembly-CSharp'
@functools.lru_cache(None)
def declared_fields(cls):
    s=(SCRIPTROOT/(cls+'.cs')).read_text('utf-8-sig')
    return re.findall(r'(?:public|private)\s+([\w<>\[\]]+)\s+(\w+)\s*;',s)
class GeometryExtractor(Extractor):
    def __init__(self,inputs,name='BloqueChorro1'):
        self.mem={};self.refs={};self.objects={};self.calls=[];self.trace=[]
        self.nextref=0x300000000;self.base=0x100000000
        self.fields=d.fields['ControlPiezaC'];self.r={'sp':0x200000000,'xzr':0,'wzr':0,'x0':self.base,'x19':self.base}
        self.name=name;self.nameptr=self.ref(name);self.compare=(0,0);self.instance_fields={};self.original_objects={};self.existing_changes={}
        types={n:t for t,n in declared_fields('ControlPiezaC')}
        for off,field in self.fields.items():
            typ=types.get(field,'')
            if field.startswith('prefab') or typ in ['GameObject','Material']:
                ptr=self.ref(field);self.write(self.base+off,ptr,8)
                self.original_objects[ptr]=field
        self.water_ptr=self.ref('AguaControlC')
        self.write(self.base+0xe0,self.water_ptr,8)
        for field,value in inputs.items():
            if field not in self.fields.values():continue
            off=next(o for o,n in self.fields.items() if n==field)
            typ=types.get(field)
            if value is None:self.write(self.base+off,0,8)
            elif isinstance(value,str):self.write(self.base+off,self.ref(value),8)
            elif typ=='float':self.write(self.base+off,struct.unpack('<I',struct.pack('<f',value))[0],4)
            else:self.write(self.base+off,value,1 if typ=='bool' else 4)
        for i in range(6):self.write(self.water_ptr+0x20+i,int(bool(inputs.get('origen'+str(i+1),0))),1)
        for i in range(4):self.write(self.water_ptr+0x80+i*4,int(inputs.get('dirCentro'+str(i+1),0)),4)
        self.put('x20',self.water_ptr)
        self.put('x22',0x23682b0);self.put('x23',0x23682a8)
        self.put('x24',0x236a6a0);self.put('x25',0x236d4e0)
    def write(self,addr,value,size):
        super().write(addr,value,size)
        if size==8 and value in self.objects and self.base<=addr<self.base+4096:
            field=self.fields.get(addr-self.base)
            if field:self.objects[value]['instanceField']=field
    def read(self,addr,size):
        if addr in self.mem and addr>=0x300000000:
            return int.from_bytes(bytes(self.mem.get(addr+i,0) for i in range(size)),'little')
        return super().read(addr,size)
    def call(self,address):
        name=d.method_map.get(address,{}).get('Name','')
        if name=='UnityEngine.Transform$$get_parent':self.put('x0',self.base);return
        if name=='UnityEngine.Object$$get_name':self.put('x0',self.nameptr);return
        if name in ['UnityEngine.GameObject$$GetComponent<object>','UnityEngine.Component$$get_gameObject']:return
        if name=='System.String$$Contains':
            s=self.refs.get(self.get('x0'),'');p=self.refs.get(self.get('x1'),'')
            self.put('w0',int(str(p) in str(s)));return
        if name=='UnityEngine.Renderer$$set_material':
            obj=self.objects.get(self.get('x0'))
            if obj is not None:obj['materialField']=self.refs.get(self.get('x1'),'')
            elif self.get('x0') in self.original_objects:self.change(self.get('x0'))['materialField']=self.refs.get(self.get('x1'),'')
            return
        if name in ['UnityEngine.GameObject$$SetActive','UnityEngine.GameObject$$set_tag','UnityEngine.Object$$Destroy']:
            ptr=self.get('x0');obj=self.objects.get(ptr)
            if obj is None and ptr in self.original_objects:obj=self.change(ptr)
            if obj is not None:
                if name.endswith('SetActive'):obj['active']=bool(self.get('w1'))
                elif name.endswith('set_tag'):obj['tag']=self.refs.get(self.get('x1'),'')
                else:obj['active']=False
            return
        if name=='UnityEngine.Transform$$GetChild':
            # Original shader variation targets child 0 of a spawned prefab.
            ptr=self.get('x0');base=self.objects.get(ptr)
            if base is not None:
                ref=self.ref('child');self.objects[ref]={'parentRecipe':self.calls.index(base),'childIndex':self.get('w1')};self.put('x0',ref)
            elif ptr in self.original_objects:
                target=self.original_objects[ptr]
                ref=self.ref('existing child');self.original_objects[ref]=target+'/'+str(self.get('w1'));self.put('x0',ref)
            return
        if name.startswith('ControlPiezaC$$doLight') or name=='ControlPiezaC$$doTextura4':return
        if name.startswith('ControlPiezaC$$'):
            keep={k:self.r.get(k,0) for k in ['x'+str(i) for i in range(19,30)]+['v'+str(i) for i in range(8,16)]+['sp']}
            self.run(name);self.r.update(keep);return
        if name=='UnityEngine.LayerMask$$NameToLayer':self.put('w0',0);return
        if name=='UnityEngine.Renderer$$get_material':return
        if name=='UnityEngine.Material$$set_color':return
        return super().call(address)
    def change(self,ptr):
        if ptr not in self.existing_changes:self.existing_changes[ptr]={'target':self.original_objects[ptr]}
        return self.existing_changes[ptr]
    def evaluate(self):
        # Original material selector precedes the plant construction pass.
        self.run('ControlPiezaC$$MaterialSegment')
        # esTextura9's rebose pass starts at 10847FC; all requested static
        # geometry is after this point. Upstream materials are already configured.
        self.run('ControlPiezaC$$GeometrySegment')
        for ptr,obj in self.objects.items():
            if 'parentRecipe' in obj and 'materialField' in obj:
                self.calls[obj['parentRecipe']].setdefault('childMaterials',[]).append({'childIndex':obj['childIndex'],'materialField':obj['materialField']})
            if ptr+0x58 in self.mem:
                obj['colorCilindro']=int.from_bytes(bytes(self.mem.get(ptr+0x58+i,0) for i in range(4)),'little')
        return self.calls
    def water_configuration(self):
        return {'ports':[bool(self.read(self.water_ptr+0x20+i,1)) for i in range(6)],
                'directions':[self.read(self.water_ptr+0x80+i*4,4) for i in range(4)],
                'rebose':[bool(self.read(self.water_ptr+0xb8+i,1)) for i in range(4)],
                'walls':[bool(self.read(self.water_ptr+0xbc+i,1)) for i in range(4)],
                'controllerWalls':[bool(self.read(self.base+0x258+i,1)) for i in range(4)]}

d.data['ScriptMethod'].append({'Name':'ControlPiezaC$$GeometrySegment','Address':0x10847FC})
d.data['ScriptMethod'].append({'Name':'ControlPiezaC$$MaterialSegment','Address':0x1082F98})
d.addresses=sorted(set(d.addresses+[0x10847FC,0x1082F98]))

if __name__=='__main__':
    inputs={'esTextura10':1,'esTextura9':1,'tipoPieza':9,'lado1':8,'lado2':7,'lado3':7,'lado4':7,'configFuente':1,'configFuenteB':0,'configFuenteCentro':0,'configPit1':'0000','configPit2':'0000','configPit3':'0000','configPit4':'0000','configAnim':'','configLights':'','texturaS':''}
    ex=GeometryExtractor(inputs)
    try:print(json.dumps(ex.evaluate(),ensure_ascii=False,indent=2))
    except Exception as err:print('ERROR',repr(err),'pc',hex(ex.pc),'trace',ex.trace[-10:]);raise
