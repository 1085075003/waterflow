"""Animator operations for existing recovered recipes, derived from original methods."""
import json,pathlib,collections
from extract_water_animator_recipes import AnimatorExtractor,inputs_for
base=pathlib.Path('Validation/NativeAnalysis');source=pathlib.Path('Assets/Resources/RecoveredNativeWaterRecipes.json')
old=json.loads(source.read_text(encoding='utf-8'))['entries']
contexts={(r['family'],r['configuration'],r['color'],r['centerMode'],r['pitMode'])for r in old}
recipes={};failures=[];checked=0
for family,configuration,color,center,pit in sorted(contexts):
    for animation in ['','1','2','3','4']:
        inputs=inputs_for(family,configuration,color,center,pit,animation)
        try:
            ex=AnimatorExtractor(inputs)
            ex.run('FuenteTest3$$doConfigFuentes'+('B'if family=='border'else'Centro'if family=='center'else''))
            checked+=1
            for recipe in ex.calls:
                ops=recipe.get('animatorOperations',[])
                if not ops:continue
                key=(recipe['nativeInstantiateVA'],animation)
                stable=[{k:int(v) if isinstance(v,bool) else v for k,v in op.items()if k!='nativeVA'}for op in ops]
                # A second helper may reuse an already-created instance and
                # repeat its same writes. No animation evaluation occurs inside
                # these methods; compare final native property values.
                final={}
                for op in stable:final[(op['kind'],op.get('parameter',''))]=op
                stable=list(final.values())
                if key in recipes and recipes[key]['operations']!=stable:raise RuntimeError(('ambiguous Animator recipe',key,stable,recipes[key]))
                recipes[key]={'nativeInstantiateVA':key[0],'configuration':key[1],'operations':stable,'evidenceVAs':[o['nativeVA']for o in ops]}
        except Exception as error:failures.append({'inputs':inputs,'error':str(error)})
owners=sorted({key[0]for key in recipes})
catalog={'source':'Static original ARM64 FT3 family methods, original instance-owned Animator operations only.',
         'nativeOwners':owners,'entries':list(recipes.values())}
destination=base/'RecoveredNativeFountainAnimators.json'
destination.write_text(json.dumps(catalog,ensure_ascii=False,separators=(',',':')),encoding='utf-8')
(base/'native-fountain-animator-extraction-report.json').write_text(json.dumps({'contexts':len(contexts),'evaluated':checked,'animatorOwners':len(owners),'recipes':len(recipes),'failures':failures},ensure_ascii=False,indent=2),encoding='utf-8')
print('evaluated',checked,'Animator owners',len(owners),'recipes',len(recipes),'failures',len(failures))
print(json.dumps(failures[:6]))
