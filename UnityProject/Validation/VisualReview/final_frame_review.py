"""Frame-level human review ledger. Standard-library only; never inherits old marks.

init --report final-report.json --output my-ledger.json --ranges 1-200,251-300,351-550
html --ledger my-ledger.json --output my-review.html
mark --ledger my-ledger.json --ranges 1-12 --reviewer original_presentation --note ...
Only call mark after actually inspecting those displayed frames. A matching name
alone is insufficient: report and every selected sheet hash are checked again.
"""
from pathlib import Path
import argparse,datetime,hashlib,html,json,struct

def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def ranges(text):
    result=set()
    for part in text.split(','):
        if '-' in part:
            first,last=map(int,part.split('-'));result.update(range(first,last+1))
        elif part:result.add(int(part))
    return result
def save(path,data):
    path.parent.mkdir(parents=True,exist_ok=True)
    path.write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
def read(path):return json.loads(path.read_text(encoding='utf-8-sig'))
def init(args):
    source=args.report.resolve();report=read(source)
    if not report.get('finishedUtc') or report.get('completed')!=report.get('requested'):
        raise SystemExit('Refusing to initialize a final ledger from an unfinished capture report.')
    selected=ranges(args.ranges) if args.ranges else None
    root=args.sheet_root.resolve() if args.sheet_root else source.parent
    sheets=[]
    for value in report['contactSheets']:
        raw=value if isinstance(value,str) else value.get('path',value.get('file',''))
        # Copied reports may retain a different project's absolute path.
        path=root/Path(raw.replace('\\','/')).name
        if not path.is_file():raise SystemExit('Missing current sheet: '+str(path))
        header=path.read_bytes()[:24]
        if header[:8]!=b'\x89PNG\r\n\x1a\n':raise SystemExit('Not PNG: '+str(path))
        width,height=struct.unpack('>II',header[16:24])
        sheets.append({'path':str(path.resolve()),'sha256':sha(path),'width':width,'height':height})
    frames=[]
    for case_index,case in enumerate(report['levels']):
        asset=case['assetId']
        if selected is not None and asset not in selected:continue
        for stage_index,stage in enumerate(['start','solution']):
            index=case_index*2+stage_index;sheet_index,tile=divmod(index,24)
            sheet=sheets[sheet_index];tw,th=sheet['width']//6,sheet['height']//4
            frames.append({'assetId':asset,'stage':stage,'sourceCaseIndex':case_index,'sheetIndex':sheet_index,
                'tileIndex':tile,'rect':[tile%6*tw,tile//6*th,tw,th],
                'reviewed':False,'reviewer':None,'reviewedUtc':None,'note':None})
    data={'createdUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'sourceReport':str(source),'sourceReportSha256':sha(source),
          'sourceStartedUtc':report['startedUtc'],'sourceFinishedUtc':report['finishedUtc'],'sourceUnityVersion':report.get('unityVersion'),
          'limitation':'Original contact frames are320x180 (six columns/four rows). Inspect suspicious details at full resolution in a new level-specific capture. Human marks apply only to these exact sheet/report hashes; no original binary equivalence is implied.',
          'sheets':sheets,'frames':frames}
    save(args.output,data);print('New ledger:',len(frames),'frames;',len(frames)//2,'levels; reviewed=0')
def verify(data,chosen):
    report=Path(data['sourceReport'])
    if not report.is_file() or sha(report)!=data['sourceReportSha256']:raise SystemExit('Capture report changed; initialize a fresh ledger.')
    for index in set(frame['sheetIndex'] for frame in chosen):
        sheet=data['sheets'][index];path=Path(sheet['path'])
        if not path.is_file() or sha(path)!=sheet['sha256']:raise SystemExit('Sheet changed; no marks applied: '+str(path))
def mark(args):
    data=read(args.ledger);selected=ranges(args.ranges)
    chosen=[frame for frame in data['frames'] if frame['assetId'] in selected and (args.stage=='both' or frame['stage']==args.stage)]
    if not chosen:raise SystemExit('No frames selected.')
    verify(data,chosen)
    for frame in chosen:frame.update(reviewed=True,reviewer=args.reviewer,reviewedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),note=args.note)
    save(args.ledger,data);print('Marked',len(chosen),'frames; total reviewed',sum(x['reviewed'] for x in data['frames']),'/',len(data['frames']))
def webpage(args):
    data=read(args.ledger);verify(data,data['frames']);cards=[]
    for frame in data['frames']:
        sheet=data['sheets'][frame['sheetIndex']];x,y,w,h=frame['rect']
        image=html.escape(Path(sheet['path']).as_uri(),quote=True)
        caption='gm{} {} · sheet{} tile{} · {}'.format(frame['assetId'],frame['stage'],frame['sheetIndex']+1,frame['tileIndex'], 'reviewed' if frame['reviewed'] else 'pending')
        cards.append('<figure><div style="width:{}px;height:{}px;overflow:hidden"><img src="{}" style="width:{}px;height:{}px;max-width:none;transform:translate(-{}px,-{}px)"></div><figcaption>{}</figcaption></figure>'.format(w,h,image,sheet['width'],sheet['height'],x,y,html.escape(caption)))
    title='Final Unity frame review '+data['sourceFinishedUtc']
    page='<!doctype html><meta charset="utf-8"><title>'+html.escape(title)+'</title><style>body{background:#18222a;color:#eef2f5;font:14px sans-serif;margin:20px}main{display:grid;grid-template-columns:repeat(auto-fill,320px);gap:12px}figure{margin:0;background:#253442}figcaption{padding:8px}header{max-width:1100px;margin-bottom:20px}</style><header><h1>'+html.escape(title)+'</h1><p>'+html.escape(data['limitation'])+'</p><p>Report SHA256: '+data['sourceReportSha256']+'</p></header><main>'+''.join(cards)+'</main>'
    args.output.write_text(page,encoding='utf-8');print(args.output)
parser=argparse.ArgumentParser();sub=parser.add_subparsers(dest='command',required=True)
p=sub.add_parser('init');p.add_argument('--report',type=Path,required=True);p.add_argument('--sheet-root',type=Path);p.add_argument('--output',type=Path,required=True);p.add_argument('--ranges',default='')
p=sub.add_parser('mark');p.add_argument('--ledger',type=Path,required=True);p.add_argument('--ranges',required=True);p.add_argument('--stage',choices=['start','solution','both'],default='both');p.add_argument('--reviewer',required=True);p.add_argument('--note',required=True)
p=sub.add_parser('html');p.add_argument('--ledger',type=Path,required=True);p.add_argument('--output',type=Path,required=True)
args=parser.parse_args();{'init':init,'mark':mark,'html':webpage}[args.command](args)
