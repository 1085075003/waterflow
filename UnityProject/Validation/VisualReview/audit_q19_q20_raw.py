"""Read-only original compressed mesh/UV payload and actual flipbook bindings."""
import hashlib,json,pathlib,re,sys
project=pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0,str(project/'Validation/ShaderAnalysis/python'))
import UnityPy
env=UnityPy.load(str(project.parent/'raw/base/assets/bin/Data/data.unity3d'))
meshes=[]
for obj in env.objects:
 if obj.type.name!='Mesh':continue
 data=obj.read()
 if data.m_Name not in ['q19','q20']:continue
 original=obj.read_typetree();path=project/'Assets/Mesh'/(data.m_Name+'.asset');text=path.read_text(encoding='utf-8-sig')
 packed=text.split('  m_CompressedMesh:\n',1)[1].split('\n  m_LocalAABB:',1)[0]
 vectors={name:body for name,body in re.findall(r'^    (m_\w+):\n(.*?)(?=^    m_|\Z)',packed,re.M|re.S)}
 proof=[]
 for name,raw in original['m_CompressedMesh'].items():
  if not isinstance(raw,dict) or 'm_Data' not in raw:continue
  body=vectors[name];encoded=re.search(r'^      m_Data:(.*)',body,re.M)[1].strip()
  before=bytes(raw['m_Data']);after=bytes.fromhex(encoded)
  values={}
  for field in ['m_NumItems','m_Range','m_Start','m_BitSize']:
   match=re.search(r'^      '+field+r': (.*)',body,re.M)
   if match:
    number=float(match[1]);value=raw[field];values[field]={'original':value,'exported':number,'sameWithinFloatYamlPrecision':abs(value-number)<max(1e-7,abs(value)*1e-6)}
  proof.append({'field':name,'bytes':len(before),'rawAndExportedPackedBytesEqual':before==after,
                'sha256':hashlib.sha256(before).hexdigest(),'numericFields':values})
 meshes.append({'name':data.m_Name,'originalPathId':obj.path_id,'asset':str(path.relative_to(project)),
                'sourceCompression':original.get('m_MeshCompression'),'packedVectors':proof,
                'allPackedBytesEqual':all(v['rawAndExportedPackedBytesEqual'] for v in proof)})

clips=[]
for name in ['water1a','water1b']:
 path=project/'Assets/AnimationClip'/(name+'.anim');text=path.read_text(encoding='utf-8-sig')
 source=text.split('  m_FloatCurves:\n',1)[1].split('  m_PPtrCurves:',1)[0]
 curves=[]
 for chunk in re.split(r'^  - serializedVersion: \d+\n    curve:\n',source,flags=re.M)[1:]:
  target=re.search(r'^    path: (.*)',chunk,re.M);attribute=re.search(r'^    attribute: (.*)',chunk,re.M)
  if not target:continue
  keys=[{'time':float(t),'active':int(float(v))} for t,v in re.findall(r'^        time: (.*)\n        value: (.*)',chunk,re.M)]
  curves.append({'path':target[1],'attribute':attribute[1] if attribute else '', 'keys':keys})
 stop=float(re.search(r'^    m_StopTime: (.*)',text,re.M)[1]);loop=int(re.search(r'^    m_LoopTime: (.*)',text,re.M)[1])
 timeline=[]
 for sample in range(int(stop*60)+1):
  t=min(sample/60,stop-1e-6);active=[]
  for curve in curves:
   if curve['attribute']!='m_IsActive':continue
   value=0
   for key in curve['keys']:
    if key['time']<=t:value=key['active']
   if value:active.append(curve['path'])
  timeline.append({'time':t,'activePaths':active})
 clips.append({'clip':name,'stop':stop,'loop':loop,'curves':curves,'sampledAuthoredStepCurveTimeline':timeline,
               'maximumSimultaneousActiveQFrames':max((len(t['activePaths']) for t in timeline),default=0)})
result={'scope':'Exact raw compressed mesh payload/UV comparison, and authored float-curve step timeline. Clip timeline is static evidence, not a claim of actual running Animator states.',
        'meshes':meshes,'clips':clips,'shaderEvidence':'ControlPiezaFuente.doConfigureColors1091F3C runtime Night shader1 replacement, with original shader Mobile/Diffuse Color fragment alpha1; runtime MatGrRo_chorro6/fontain20 and palette confirmed by actual gm2050 mesh diagnostics. Original fontain20 decoded PNG exact and alpha255 in raw-water-material-texture-audit.json.'}
out=pathlib.Path(__file__).with_name('q19-q20-raw-mesh-and-flipbook-audit.json');out.write_text(json.dumps(result,indent=2),encoding='utf-8')
print('Mesh evidence',[(x['name'],x['originalPathId'],x['allPackedBytesEqual'],len(x['packedVectors'])) for x in meshes])
print('Clip evidence',[(x['clip'],x['stop'],x['loop'],x['maximumSimultaneousActiveQFrames'],len(x['curves'])) for x in clips])
