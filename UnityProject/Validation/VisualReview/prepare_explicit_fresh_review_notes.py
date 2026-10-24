"""Prepare note labels only after the caller actually views the named fresh PNGs.

The previous descriptive labels contain no review flags. They are reused solely
as board labels after a fresh human image inspection, and never cause a sheet to
be selected or marked automatically. record_fresh_native_recipe_review validates
the new sourceStart and explicit image SHA independently.
"""
import argparse,hashlib,json,pathlib
here=pathlib.Path(__file__).resolve().parent
parser=argparse.ArgumentParser();parser.add_argument('--sheets',required=True)
args=parser.parse_args();chosen=[int(x) for x in args.sheets.split(',')]
labels={}
for path in here.glob('current-reviewed-*-notes.json'):
 for sheet in json.loads(path.read_text(encoding='utf-8-sig'))['sheets']:labels[sheet['number']]=sheet['levels']
root=here.parents[2]/'.validation-visual-project/Validation/FullContent'
report=json.loads((root/'all-runtime-validation.json').read_text(encoding='utf-8-sig'))
assert report['startedUtc']=='2026-10-16T09:12:08.0173697Z'
result=[]
for number in chosen:
 path=root/f'all-contact-{number:03}.png'
 assert number in labels
 result.append({'number':number,'viewedSha256':hashlib.sha256(path.read_bytes()).hexdigest(),'levels':labels[number]})
out=here/('fresh-viewed-'+','.join(map(str,chosen))+'.json')
out.write_text(json.dumps({'sheets':result},ensure_ascii=False,indent=2),encoding='utf-8')
print(out)
