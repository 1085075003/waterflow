"""Freeze a reviewable source copy and manifests for the two idle target projects."""
import argparse
import hashlib
import json
import shutil
from datetime import datetime, timezone
from pathlib import Path

validation = Path(__file__).resolve().parent
workspace = validation.parent.parent
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--source', type=Path, default=workspace / '.validation-project',
    help='Authoritative idle project whose complete Assets will be frozen; no import or Unity launch occurs.')
args = parser.parse_args()
source = args.source.resolve()
projects = [workspace / 'UnityProject', workspace / '.validation-project', workspace / '.validation-visual-project']
assert source in projects and (source / 'Assets').is_dir(), 'Choose one of the three known projects.'
batch = validation / ('FinalSource-' + datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S%fZ'))
batch.mkdir()
def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest() if path.is_file() else None
source_files = {p.relative_to(source).as_posix(): p for p in (source / 'Assets').rglob('*') if p.is_file()}
targets = [project for project in projects if project != source]
staged = set()
for target in targets:
    target_keys = {p.relative_to(target).as_posix() for p in (target/'Assets').rglob('*') if p.is_file()}
    # A source sync cannot silently remove a target-only asset.
    assert not target_keys - source_files.keys(), sorted(target_keys - source_files.keys())
    records = []
    for key, path in sorted(source_files.items()):
        wanted, previous = sha(path), sha(target / key)
        if wanted == previous:
            continue
        snapshot = batch / key
        if key not in staged:
            snapshot.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(path, snapshot)
            assert sha(snapshot) == wanted
            staged.add(key)
        records.append({'target': key, 'source': str(snapshot), 'sha256': wanted, 'previousSha256': previous})
    manifest = {'createdUtc': datetime.now(timezone.utc).isoformat(), 'sourceProject': str(source),
        'targetProject': str(target), 'scope': 'Frozen actual validated Assets only; no Library/Temp/PlayerPrefs or product-name settings are copied.',
        'files': records}
    destination = batch / (target.name.strip('.') + '-manifest.json')
    destination.write_text(json.dumps(manifest, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    print(json.dumps({'manifest': str(destination), 'changedFiles': len(records)}))
inventory = {key: sha(path) for key,path in sorted(source_files.items())}
(batch/'asset-inventory.json').write_text(json.dumps({'sourceProject':str(source),'createdUtc':datetime.now(timezone.utc).isoformat(),'files':inventory},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
