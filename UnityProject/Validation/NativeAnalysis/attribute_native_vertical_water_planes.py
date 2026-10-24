"""Record concrete renderer/native lifecycle evidence for three visual questions, read only."""
import json,pathlib
base=pathlib.Path('Validation')
rows=[]
selectors={1851:['[023]'],1152:['[048]','[049]','[050]'],2001:['[072]','[073]','[075]','[080]','[083]','[087]']}
for level,prefixes in selectors.items():
 data=json.loads((base/'FullContent'/f'gm{level}-water-mesh-diagnostics.json').read_text(encoding='utf-8'))
 meshes=[m for m in data['meshes'] if any(m['path'].startswith(p) for p in prefixes)
         and ('/Original Fuente5Color' in m['path'] or '/Original FuenteColor1c' in m['path'])]
 rows.append({'level':level,'source':'Actual Unity 2022.3.28f1 representative mesh diagnostics, active renderers only',
              'verticalInputWaterRenderers':meshes})
report={'cases':rows,'nativeEvidence':[
 {'method':'FuenteTest3.doInstanciarAgua','VA':'0x10A5834','vertical5Instantiate':'0x10A64F8','vertical6Instantiate':'0x10A6638',
  'localPosition':[8.640000343322754,-1.350000023841858,0], 'localScale':[2.309999942779541,2.0999999046325684,1.5],
  'phase':'Vertical input A. Uses color-selected prefabAgua. At 0x10A6594 / 0x10A66D4 schedules B after original espera2.'},
 {'method':'FuenteTest3.doInstanciarAgua1','VA':'0x10B1F24','verticalBInstantiate':'0x10B24FC',
  'localPosition':[-14.640000343322754,-3.2799999713897705,0], 'localScale':[3.0929999351501465,3,2.7860000133514404],
  'phase':'Persistent phase B. Mesh water3/w18..w22 is original water5 animation; each original frame is approximately20x20.'},
 {'method':'FuenteTest3.doInstanciarAgua1','VA':'0x10B2590..0x10B25E4',
  'controlFlow':'origen-5 < 2 returns at0x10B25A0. Only origins outside5/6 proceed to Invoke(doDestroyAgua,2f) at0x10B25B8..E4.',
  'meaning':'Vertical A is expressly excluded from the side travel-water two-second cleanup. Removing the visible q* flat solely after2s would contradict this original branch.'}],
 'verifiedAttribution':'The outside square in gm1851 belongs to [023]...OrigenCentro/Original Fuente5Color4/water1/q17 (center88.64,5.266,-8.693). The right purple patch in gm1152 is [048]...OrigenCentro/Original Fuente5Color4/water1/a8+q18 (center~88.64,12.907,51.307). These are vertical input A animation meshes, rather than leftover portPlane or the CascadeTest flat/curve selector.',
 'limits':'The native pose and lifecycle select these original A meshes and explicitly retain them. This evidence does not establish a new delete/hide rule or prove pixel-equivalent geometry. No renderer was removed, no original prefab was edited, and no runtime behavior was changed.',
 'separateFixedIssue':'CascadeTest flat plano versus planoCurvo mutual exclusion was restored earlier at native callbacks FD14CC/FD18A8; the attributed OrigenCentro Fuente5 meshes are a distinct effect family.'}
(base/'NativeAnalysis'/'vertical-water-plane-attribution.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print('Recorded',sum(len(r['verticalInputWaterRenderers'])for r in rows),'actual renderer entries for',len(rows),'levels')
