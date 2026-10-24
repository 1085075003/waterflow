"""Preserve the deliberately stopped pre-physics-fix scan without calling it complete."""
import hashlib
import json
import shutil
from datetime import datetime, timezone
from pathlib import Path

v = Path(__file__).resolve().parent
source = v / 'Save1Gameplay'
archive = source / 'SupersededPreLandingJetPhysics'
archive.mkdir(exist_ok=False)
copied = []
for name in ['save1-gameplay-validation.json', 'progress.json', 'expected-catalog.json', 'NativeCases']:
    p = source / name
    if p.is_dir():
        shutil.copytree(p, archive / name)
    elif p.exists():
        shutil.copy2(p, archive / name)
    else:
        continue
    copied.append(name)
for name in ['native-gameplay-all-start-source-snapshot.json', 'native-gameplay-all-2022.3.28f1.log']:
    if (v / name).exists():
        shutil.copy2(v / name, archive / name)
        copied.append(name)
automation = v / 'automation'
for name in ['request.json', 'response.json']:
    if (automation / name).exists():
        shutil.copy2(automation / name, archive / ('close-' + name))
report_path = archive / 'save1-gameplay-validation.json'
report = json.loads(report_path.read_text(encoding='utf-8-sig'))
assert not report.get('finishedUtc'), 'This archive is only for the interrupted scan.'
levels = report.get('levels', [])
record = dict(archivedUtc=datetime.now(timezone.utc).isoformat(),
    status='superseded-incomplete', passed=False,
    reason='Safely closed after discovering native landing and world-space jet physics corrections. Final native coverage must use the same corrected runtime physics; these successes are not eligible final coverage.',
    reportSha256=hashlib.sha256(report_path.read_bytes()).hexdigest(),
    startedUtc=report.get('startedUtc'), finishedUtc=report.get('finishedUtc'),
    levelsRecorded=len(levels), levelsPassed=sum(bool(x.get('passed')) for x in levels),
    closedFailures=[x['id'] for x in levels if x.get('failure')], copied=copied)
(archive / 'archive-status.json').write_text(json.dumps(record, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
print(json.dumps(record))
