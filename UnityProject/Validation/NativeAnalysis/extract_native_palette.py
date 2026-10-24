"""Read-only native color recipes for C and wall sources, all configuration/color branches."""
import json,struct,pathlib,collections
from extract_water_recipes import Extractor
import disassemble as d
class NativePalette(Extractor):
    def __init__(self,cls,code,color):
        super().__init__({'confColor':code,'color':color},cls)
        self.materials=[]
        for off,name in self.fields.items():
            if name.startswith('material') or name=='shader1':self.write(self.base+off,self.ref(name),8)
    def call(self,address):
        name=d.method_map.get(address,{}).get('Name','')
        if name=='UnityEngine.Material$$set_color':
            self.materials.append({'field':self.refs.get(self.get('x0')),'color':self.floats(4)});return
        if name=='UnityEngine.Material$$set_shader':return
        return super().call(address)
    def value(self,name,size=4):
        off=next(k for k,v in self.fields.items() if v==name)
        return self.read(self.base+off,size)
    def color(self,name):return list(struct.unpack('<4f',int(self.value(name,16)).to_bytes(16,'little')))
def main():
    cases=[];failures=[]
    for cls in ['ControlPiezaC','ControlPiezaFuente']:
        for night in ([False] if cls=='ControlPiezaC' else [False,True]):
            for color in range(1,5):
                for code in ['']+[str(i) for i in range(24)]:
                    try:
                        ex=NativePalette(cls,code,color)
                        method=cls+'$$'+('doConfigureColors' if cls=='ControlPiezaC' else ('doColor2' if night else 'doColor1'))
                        ex.run(method)
                        cases.append(dict(type=cls,configuration=code,night=night,color=color,confC=ex.value('confC'),color1=ex.color('color1'),color2=ex.color('color2'),color3=ex.color('color3'),materials=ex.materials))
                    except Exception as error:failures.append(dict(type=cls,configuration=code,night=night,color=color,error=str(error),pc=hex(ex.pc)))
    output={'version':1,'source':'Original ARM64 ControlPiezaC.doConfigureColors1088ED4 and ControlPiezaFuente.doColor1/2; shader/night/global propagation from ConfigureColors1091F3C. No original code executed.','cases':cases}
    root=pathlib.Path(__file__).resolve().parent
    (root/'RecoveredNativePalette.json.ready').write_text(json.dumps(output,ensure_ascii=False,separators=(',',':')),encoding='utf-8')
    (root/'native-palette-extraction-report.json').write_text(json.dumps({'cases':len(cases),'failures':failures},ensure_ascii=False,indent=2),encoding='utf-8')
    print('cases',len(cases),'failures',len(failures));print(failures[:5])
if __name__=='__main__':main()
