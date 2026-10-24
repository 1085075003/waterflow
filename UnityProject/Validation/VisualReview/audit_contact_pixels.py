from pathlib import Path
import json,datetime,hashlib
import numpy as np
from PIL import Image

base=Path(__file__).resolve().parents[1]
source=base/'FullContent'
report=json.loads((source/'all-runtime-validation.json').read_text(encoding='utf-8-sig'))
entries=[]
# Use the actual report list: a shorter rerun can leave older PNG files behind.
paths=[source/Path(name).name for name in report['contactSheets']]
for index,path in enumerate(paths):
    pixels=np.asarray(Image.open(path).convert('RGB'))
    lo=index*12; levels=report['levels'][lo:lo+12]
    magenta=(pixels[:,:,0]>240)&(pixels[:,:,1]<15)&(pixels[:,:,2]>240)
    entry={'sheet':path.name,'firstAsset':levels[0]['assetId'],'lastAsset':levels[-1]['assetId'],
           'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),
           'sourceStartedUtc':report['startedUtc'],
           'expectedFrames':len(levels)*2,'imageSize':[pixels.shape[1],pixels.shape[0]],
           'pureErrorMagentaPixels':int(magenta.sum()),'visuallyReviewed':False,'observations':[]}
    entries.append(entry)
output=base/'VisualReview'/'all1350-contact-review.json'
if output.exists():
    old={x['sheet']:x for x in json.loads(output.read_text(encoding='utf-8'))['sheets']}
    for entry in entries:
        previous=old.get(entry['sheet'],{})
        if (previous.get('sha256')==entry['sha256'] and
                previous.get('sourceStartedUtc')==entry['sourceStartedUtc']):
            entry.update({k:previous[k] for k in ['visuallyReviewed','observations']})
result={'updatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),
        'sourceStartedUtc':report['startedUtc'],'sourceFinishedUtc':report['finishedUtc'],
        'sourceSnapshotLimit':'These exact renders support static layout, model, UI, framing and error-shader review. Separate motion and gameplay reports establish their own coverage; static contact sheets do not prove animated equivalence.',
        'sheets':entries}
output.write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print('sheets',len(entries),'expected frames',sum(x['expectedFrames'] for x in entries),'error-magenta',sum(x['pureErrorMagentaPixels'] for x in entries))
print('magenta sheets',[(x['sheet'],x['pureErrorMagentaPixels']) for x in entries if x['pureErrorMagentaPixels']])
