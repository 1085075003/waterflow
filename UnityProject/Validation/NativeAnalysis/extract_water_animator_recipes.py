"""Read-only Animator operations and their exact instantiated object owners."""
import json,pathlib,sys
from extract_water_recipes import Extractor
import disassemble as d

class AnimatorExtractor(Extractor):
    def __init__(self,inputs):
        super().__init__(inputs)
        self.components={};self.animations=[]
    def call(self,address):
        name=d.method_map.get(address,{}).get('Name','')
        if name=='UnityEngine.GameObject$$GetComponent<object>':
            owner=self.get('x0');kind=self.refs.get(self.get('x1'),'')
            component=self.ref('component')
            self.components[component]={'owner':owner,'kind':kind}
            self.put('x0',component);return
        if name in ['UnityEngine.Animator$$SetInteger','UnityEngine.Behaviour$$set_enabled']:
            component=self.components.get(self.get('x0'))
            if component is None or 'Animator' not in str(component['kind']):
                if name.startswith('UnityEngine.Animator'):raise RuntimeError(('unresolved Animator component',hex(self.pc),component))
                return
            owner=self.objects.get(component['owner'])
            if owner is None:raise RuntimeError(('unresolved Animator owner',hex(self.pc),component))
            operation={'nativeVA':hex(self.pc)}
            if name.endswith('SetInteger'):
                operation.update({'kind':'integer','parameter':self.refs.get(self.get('x1')),'value':self.get('w2')})
            else:operation.update({'kind':'enabled','value':bool(self.get('w1'))})
            owner.setdefault('animatorOperations',[]).append(operation)
            self.animations.append({'prefabField':owner['prefabField'],'nativeInstantiateVA':owner['nativeInstantiateVA'],**operation})
            return
        super().call(address)

def inputs_for(family,configuration,color=1,center=0,pit=0,animation=''):
    values={'color':color,'tipoPieza':2,'configFuenteCentro':center,'configPit1':str(pit)+'000',
            'configPit2':'0000','configPit3':'0000','configPit4':'0000','configAnim':animation,
            'configLi':'','configRes1':'','configRes2':'','configRes3':'','configRes4':'','rotay':0.0}
    values[{'primary':'configFuente','border':'configFuenteB','center':'configFuenteCentro'}[family]]=configuration
    return values

if __name__=='__main__':
    result=[];fail=[]
    for family in ['primary','border','center']:
        for configuration in range(1,10):
            for animation in ['','1','2','3','4']:
                inputs=inputs_for(family,configuration,animation=animation)
                try:
                    ex=AnimatorExtractor(inputs)
                    recipes=ex.run('FuenteTest3$$doConfigFuentes'+('B'if family=='border'else'Centro'if family=='center'else''))
                    result.append({'family':family,'configuration':configuration,'animation':animation,'recipes':recipes,'animatorOperations':ex.animations})
                except Exception as error:fail.append({'family':family,'configuration':configuration,'animation':animation,'error':str(error)})
    output={'cases':result,'failures':fail,'scope':'Static ARM evaluation only; no Assets changes.'}
    (d.ROOT/'native-water-animator-recipes-audit.json').write_text(json.dumps(output,ensure_ascii=False,indent=2),encoding='utf-8')
    print('cases',len(result),'failures',len(fail),'withAnimator',sum(bool(v['animatorOperations'])for v in result))
    print(json.dumps(fail[:15]))
