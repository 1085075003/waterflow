from pathlib import Path
import json,sys,datetime
path=Path(__file__).resolve().parent/'all1350-contact-review.json'
report=json.loads(path.read_text(encoding='utf-8'))
indices=set()
for arg in sys.argv[1:]:
    if '-' in arg:
        first,last=map(int,arg.split('-'));indices.update(range(first,last+1))
    else:indices.add(int(arg))
for index,entry in enumerate(report['sheets'],1):
    if index not in indices:continue
    entry['visuallyReviewed']=True
    if not entry['observations']:entry['observations']=['Inspected all displayed start/solution pairs at original contact-sheet resolution for blank/missing board, overlapping modal, wrong-scene leftover strip, obvious shader-error magenta and model discontinuities.']
report['updatedUtc']=datetime.datetime.now(datetime.timezone.utc).isoformat()
path.write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print('Reviewed',sum(x['visuallyReviewed'] for x in report['sheets']),'/',len(report['sheets']))
