import pathlib,sys,json,collections,bisect,struct,array
sys.path.insert(0,str(pathlib.Path('Validation/ShaderAnalysis/python').resolve()));import UnityPy
O=pathlib.Path('Validation/NativeAnalysis');e=UnityPy.load('../raw/base/assets/bin/Data/data.unity3d');legacy=[]
for o in e.objects:
 if o.type.name!='Animation':continue
 t=o.read_typetree();x={'file':o.assets_file.name,'pathID':o.path_id,'tree':t,'clipNames':[]};a=o.read()
 for p in [a.m_Animation]+list(a.m_Animations):
  if p:
   co=p.deref();x['clipNames'].append(co.read_typetree().get('m_Name'))
 legacy.append(x)
print('original legacy Animation',[(x['file'],x['pathID'],x['clipNames'])for x in legacy])
# Source native BL callsites for every original Animator.Play overload, including methods not exported previously.
sys.path.insert(0,str(O.resolve()));import disassemble as d
methods=d.data['ScriptMethod'];targets={m['Address']:m for m in methods if m['Name'].startswith('UnityEngine.Animator$$Play')};calls=[]
print('Play overload targets',[(hex(a),m['Signature'])for a,m in targets.items()])
for seg in d.segments:
 data=d.read_va(seg['p_vaddr'],seg['p_filesz']);base=seg['p_vaddr'];words=array.array('I');words.frombytes(data[:len(data)//4*4])
 for i,w in enumerate(words):
  if w&0x7c000000!=0x14000000:continue
  imm=w&0x3ffffff;imm=imm-(1<<26)if imm&(1<<25)else imm;va=base+i*4;target=va+imm*4
  if target not in targets:continue
  idx=bisect.bisect_right(d.addresses,va)-1;caller=d.method_map[d.addresses[idx]];line={'instruction':'BL'if w&0x80000000 else 'B','address':hex(va),'caller':caller['Name'],'target':targets[target]['Name'],'methodVA':hex(caller['Address'])};calls.append(line)
  if not caller['Name'].startswith('UnityEngine.'):
   out=O/'Methods'/(caller['Name'].replace('$$','_').replace('.','_')+'_'+format(caller['Address'],'X')+'.asm.txt');out.write_text(d.disassemble(caller),encoding='utf-8')
r={'originalBundleLegacyAnimation':legacy,'nativeAnimatorPlayCallsites':calls,'scope':'All original GameObject legacy components, and all IL2CPP executable BL callsites to every Animator.Play/PlayInFixedTime wrapper. No runtime/source mutation.'};(O/'animation-original-explicit-play-audit.json').write_text(json.dumps(r,ensure_ascii=False,indent=2),encoding='utf-8')
print('calls',len(calls));print('\n'.join(str(x)for x in calls))
