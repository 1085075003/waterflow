"""Evaluate original particle ColorOverLifetime constructors, without native execution."""
import sys,json,pathlib,struct,collections
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parent))
import disassemble as d
from extract_water_recipes import Extractor

class GradientExtractor(Extractor):
    def __init__(self, cls, values, name):
        super().__init__(values,cls)
        self.colors=[];self.alpha=[];self.name=name;self.enabled=False
        for off,field in self.fields.items():
            if field=='ps':self.write(self.base+off,self.ref('particle'),8)
    def read(self,address,size):
        if address>=0x300000000 and any(address+i in self.mem for i in range(size)):
            return int.from_bytes(bytes(self.mem.get(address+i,0)for i in range(size)),'little')
        return super().read(address,size)
    def call(self,address):
        name=d.method_map.get(address,{}).get('Name','')
        if address in(0xe61d94,0xe61bf0):
            ptr=self.ref('allocated')
            self.write(ptr+0x18,max(4,self.get('w1')),4)
            self.put('x0',ptr);return
        if name=='UnityEngine.Object$$get_name':self.put('x0',self.ref(self.name));return
        if name=='UnityEngine.Component$$get_gameObject':return
        if name=='System.String$$Contains':self.put('w0',int(self.refs.get(self.get('x1'),'')in self.refs.get(self.get('x0'),'')));return
        if name=='UnityEngine.GradientColorKey$$.ctor':
            values=self.floats(5);self.colors.append(dict(color=values[:4],time=values[4]))
            for i,value in enumerate(values):self.write(self.get('x0')+4*i,int.from_bytes(struct.pack('<f',value),'little'),4)
            return
        if name=='UnityEngine.GradientAlphaKey$$.ctor':
            values=self.floats(2);self.alpha.append(dict(alpha=values[0],time=values[1]))
            for i,value in enumerate(values):self.write(self.get('x0')+4*i,int.from_bytes(struct.pack('<f',value),'little'),4)
            return
        if name=='UnityEngine.ParticleSystem.ColorOverLifetimeModule$$set_enabled':self.enabled=bool(self.get('w1'));return
        if name in ['UnityEngine.ParticleSystem$$get_colorOverLifetime','UnityEngine.Gradient$$.ctor','UnityEngine.Gradient$$SetKeys',
            'UnityEngine.ParticleSystem.MinMaxGradient$$op_Implicit','UnityEngine.ParticleSystem.ColorOverLifetimeModule$$set_color']:
            return
        return super().call(address)

def main():
    cases=[];fail=[]
    for cls in ['changeColorP','changeColorU']:
        for family in ['day','night','light']:
            for waterfall in [False,True]:
                codes=range(1,24)if family=='light'else range(24)
                for code in codes:
                    for color in [1,2,3,4]:
                        values=dict(color=color,subcolor=code if family=='light'else 0,colx=0,confColor=code if family!='light'else 0)
                        ex=GradientExtractor(cls,values,'WFall4'if waterfall else 'Font4')
                        try:
                            method='doColor2'if family=='night'else'doLuzColor'if family=='light'else'doColor1'
                            ex.run(cls+'$$'+method)
                            cases.append(dict(type=cls,family=family,color=color,configuration=code,waterfall=waterfall,enabled=ex.enabled,colors=ex.colors,alpha=ex.alpha))
                        except Exception as error:fail.append(dict(type=cls,family=family,color=color,configuration=code,waterfall=waterfall,error=str(error),pc=hex(ex.pc),instruction=str(list(d.md.disasm(d.read_va(ex.pc,4),ex.pc))[0])))
    destination=pathlib.Path(__file__).with_name('native-water-particle-colors.json')
    destination.write_text(json.dumps(dict(cases=cases,unsupported=fail),indent=2),encoding='utf-8')
    print('cases',len(cases),'failures',len(fail),collections.Counter(x['error']for x in fail))
    if cases:print(cases[0])
    if fail:print(fail[0])
    import re
    contexts=collections.Counter()
    for path in pathlib.Path('Assets/GameObject').glob('*.prefab'):
        text=path.read_text('utf-8-sig')
        for block in re.split('--- !u!',text):
            if any(g in block for g in ['e65eac55dc869ef07c2ec7fac301c009','a3c695c1d7c273f39451b849b52ed9af']):
                fields=dict(re.findall(r'^  (color|subcolor|colx): (.*)$',block,re.M))
                contexts[tuple(fields.values())]+=1
    print('authored component contexts',contexts)
if __name__=='__main__':main()
