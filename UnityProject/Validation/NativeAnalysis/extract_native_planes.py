"""Read-only native surface rotation and water-instance recipes.
The interpreter follows original branches and whitelist Unity setters only.
rotay is fixed to zero so resulting world rotations expose the native basis.
"""
import json,struct
import disassemble as d
from extract_water_recipes import Extractor

class SurfaceExtractor(Extractor):
    def __init__(self,inputs):
        super().__init__(inputs)
        self.existing={}
        for off,name in self.fields.items():
            if name.startswith('plano') or name in ['esfera1']:
                ptr=self.ref(name);self.write(self.base+off,ptr,8)
                self.existing[ptr]={'existingField':name}
                self.objects[ptr]=self.existing[ptr]
    def call(self,address):
        name=d.method_map.get(address,{}).get('Name','')
        if name=='UnityEngine.GameObject$$GetComponent<object>':return
        if name=='UnityEngine.GameObject$$get_activeSelf':self.put('w0',0);return
        if name=='UnityEngine.GameObject$$SetActive':
            obj=self.objects.get(self.get('x0'))
            if obj is not None:obj['active']=bool(self.get('w1'))
            return
        if name=='UnityEngine.Renderer$$set_material':return
        if name=='UnityEngine.Material$$set_color':return
        return super().call(address)
    def surfaces(self):
        out=[]
        for ptr,recipe in self.existing.items():
            if len(recipe)==1:continue
            for off,name in [(0x20,'scrollSpeed_X'),(0x24,'scrollSpeed_Y'),(0x40,'offsetX')]:
                if ptr+off in self.mem:
                    recipe[name]=struct.unpack('<f',bytes(self.mem.get(ptr+off+i,0) for i in range(4)))[0]
            out.append(recipe)
        return out

result={'method':'FuenteTest3.doTratarDirecciones','VA':'0x10B146C','worldRotayBasis':0,'cases':[],'failures':[]}
for origin in range(1,5):
    for center in range(1,9):
        values={'origen':origin,'triggered':1,'rotay':0,'dirPlanoCentro':center,
                'dirPlano1':1,'dirPlano2':3,'dirPlano3':5,'dirPlano4':7}
        ex=SurfaceExtractor(values)
        try:
            ex.run('FuenteTest3$$doTratarDirecciones')
            result['cases'].append({'origen':origin,'dirPlanoCentro':center,'mutations':ex.surfaces()})
        except Exception as error:result['failures'].append({'inputs':values,'error':str(error)})
(d.ROOT/'native-C-surface-rotations.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),'utf-8')
print('surface cases',len(result['cases']),'failures',result['failures'][:2])

water={'method':'FuenteTest3.doInstanciarAgua/1','rotayBasis':0,'sphereAndReceiverWorldPositionBasis':[0,0,0],'cases':[],'failures':[]}
for phase in [0,1]:
    for origin in range(1,7):
        values={'origen':origin,'triggered':1,'creaAgua':1,'rotay':0}
        ex=SurfaceExtractor(values)
        method='FuenteTest3$$doInstanciarAgua'+('1' if phase else '')
        try:
            calls=ex.run(method)
            water['cases'].append({'phase':phase,'origen':origin,'recipes':calls})
        except Exception as error:water['failures'].append({'phase':phase,'inputs':values,'error':str(error)})
(d.ROOT/'native-C-water-instances.json').write_text(json.dumps(water,ensure_ascii=False,indent=2),'utf-8')
print('water cases',len(water['cases']),'failures',water['failures'][:3])
