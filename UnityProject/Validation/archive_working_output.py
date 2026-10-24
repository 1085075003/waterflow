"""Move prior generated results aside before a fresh run; caller stops Unity first."""
import argparse,json,shutil
from datetime import datetime, timezone
from pathlib import Path
v=Path(__file__).resolve().parent;w=v.parent.parent
p=argparse.ArgumentParser(description=__doc__);p.add_argument('project',type=Path);p.add_argument('name',choices=['Save1Gameplay','FullContent']);p.add_argument('label');a=p.parse_args()
project=a.project.resolve();source=(project/'Validation'/a.name).resolve()
assert project.is_relative_to(w) and source.is_relative_to(project/'Validation')
assert a.label.replace('-','').replace('_','').isalnum()
target=(project/'Validation/History'/(a.name+'-'+a.label)).resolve()
assert target.is_relative_to(project/'Validation/History') and not target.exists()
target.parent.mkdir(parents=True,exist_ok=True)
if source.exists(): shutil.move(str(source),str(target))
source.mkdir()
record=dict(archivedUtc=datetime.now(timezone.utc).isoformat(),source=str(source),destination=str(target),scope='Prior generated validation output preserved before a fresh run; never considered current-run results.')
(source/'prior-output-location.json').write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(record))
