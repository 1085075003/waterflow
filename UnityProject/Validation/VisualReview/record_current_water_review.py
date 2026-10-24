"""Record only explicitly inspected current-run contact frames, never infer review."""
import argparse,datetime,hashlib,json,pathlib

SOURCE_START='2026-10-16T05:10:20.9001744Z'
workspace=pathlib.Path(__file__).resolve().parents[3]
source=workspace/'.validation-visual-project/Validation/FullContent'
output=pathlib.Path(__file__).with_name('final-water-pending-038-075.json')
parser=argparse.ArgumentParser()
parser.add_argument('--notes',type=pathlib.Path,required=True)
args=parser.parse_args()
notes=json.loads(args.notes.read_text(encoding='utf-8-sig'))
report=json.loads((source/'all-runtime-validation.json').read_text(encoding='utf-8-sig'))
assert report['startedUtc']==SOURCE_START
listed={pathlib.Path(x.replace('\\','/')).name for x in report['contactSheets']}
if output.exists():
    audit=json.loads(output.read_text(encoding='utf-8-sig'))
    assert audit['sourceStartedUtc']==SOURCE_START
else:
    audit={'sourceStartedUtc':SOURCE_START,'finalReportSha256':None,'finalReportFinishedUtc':None,
           'partialReviewOnly':True,'reviewer':'original_water','sourceProject':str(source.parent.parent),
           'limitation':'Each320x180 original tile was visually inspected at contact-sheet resolution. This detects gross missing geometry/water, wrong colors, modal residue, clipping or pink shaders. It does not establish animation or pixel equivalence. Full-resolution follow-up is required for a suspicious detail.',
           'sheets':[]}

for sheet in notes['sheets']:
    number=sheet['number'];assert 38<=number<=75, 'Sheet is outside original_water assignment.';name=f'all-contact-{number:03}.png';path=source/name
    assert name in listed, 'Only closed sheets explicitly listed in the current report may be marked.'
    image_sha=hashlib.sha256(path.read_bytes()).hexdigest()
    assert sheet.get('viewedSha256')==image_sha, 'Actual-view SHA is required.'
    old=next((x for x in audit['sheets'] if x['number']==number),None)
    if old is not None:
        assert old['imageSha256']==image_sha, 'Fresh render requires a new actual inspection.'
        audit['sheets'].remove(old)
    frames=[]
    for offset in range(12):
        case_index=(number-1)*12+offset
        case=report['levels'][case_index]
        descriptor=sheet['levels'][str(case['assetId'])]
        for stage_index,stage in enumerate(['start','solution']):
            frames.append({'tileIndex':offset*2+stage_index,'sourceCaseIndex':case_index,'assetId':case['assetId'],
                           'stage':stage,'reviewed':True,'observation':descriptor[stage],
                           'issue':descriptor.get('issue'),'fullResolutionRequired':descriptor.get('fullResolutionRequired',False)})
    audit['sheets'].append({'number':number,'path':str(path.resolve()),'imageSha256':image_sha,'sourceStartedUtc':SOURCE_START,
                            'reviewedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'frames':frames})
audit['sheets'].sort(key=lambda x:x['number'])
audit['reviewedFrameCount']=sum(len(x['frames']) for x in audit['sheets'])
audit['reviewedLevelCount']=audit['reviewedFrameCount']//2
output.write_text(json.dumps(audit,ensure_ascii=False,indent=2),encoding='utf-8')
print('Recorded',len(notes['sheets']),'actually viewed sheets;',audit['reviewedFrameCount'],'frames total.')
