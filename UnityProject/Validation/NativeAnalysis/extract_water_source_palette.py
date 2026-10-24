import pathlib,sys,json,struct,collections
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parent))
from extract_water_recipes import Extractor
import disassemble as d

class Palette(Extractor):
    def __init__(self,configuration,night):
        super().__init__(dict(confColor=configuration,color=4),'ControlPiezaFuente')
        self.materials=[]
        for off,name in self.fields.items():
            if name.startswith('material')or name=='shader1':self.write(self.base+off,self.ref(name),8)
    def call(self,address):
        name=d.method_map.get(address,{}).get('Name','')
        if name=='UnityEngine.Material$$set_color':
            self.materials.append(dict(field=self.refs.get(self.get('x0')),color=self.floats(4)));return
        if name=='UnityEngine.Material$$set_shader':return
        return super().call(address)
    def value(self,name,size=4):
        off=next(k for k,v in self.fields.items()if v==name)
        return self.read(self.base+off,size)

def main():
    cases=[];fail=[]
    for night in[False,True]:
        for code in['']+[str(i)for i in range(24)]:
            try:
                ex=Palette(code,night);ex.run('ControlPiezaFuente$$doColor'+('2'if night else'1'))
                color=lambda name:list(struct.unpack('<4f',int(ex.value(name,16)).to_bytes(16,'little')))
                cases.append(dict(configuration=code,night=night,confC=ex.value('confC'),color1=color('color1'),color2=color('color2'),color3=color('color3'),materials=ex.materials))
            except Exception as error:fail.append(dict(configuration=code,night=night,error=str(error),pc=hex(ex.pc)))
    pathlib.Path(__file__).with_name('native-source-palette.json').write_text(json.dumps(dict(cases=cases,unsupported=fail),indent=2),encoding='utf-8')
    print('cases',len(cases),'failures',len(fail),collections.Counter(x['error']for x in fail))
    if cases:print(cases[0])
    if fail:print(fail[0])
if __name__=='__main__':main()
