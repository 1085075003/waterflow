"""Create a separate validation project, keeping its preferences isolated."""
import hashlib
import json
import re
import shutil
from pathlib import Path
from datetime import datetime, timezone

workspace = Path(__file__).resolve().parents[2]
source = workspace / '.validation-project'
target = workspace / '.validation-visual-project'
assert source.is_dir() and not target.exists()
target.mkdir()
for name in ['Assets', 'Packages', 'ProjectSettings']:
    shutil.copytree(source / name, target / name)
settings = target / 'ProjectSettings/ProjectSettings.asset'
text = settings.read_text(encoding='utf-8-sig')
text, count = re.subn(r'(?m)^  productName:.*$', '  productName: Flow Water - Visual Validation', text)
assert count == 1
settings.write_text(text, encoding='utf-8')
native = target / 'Validation/NativeAnalysis'
native.mkdir(parents=True)
shutil.copy2(workspace/'UnityProject/Validation/NativeAnalysis/native-scene-map.json', native/'native-scene-map.json')
record = {'createdUtc': datetime.now(timezone.utc).isoformat(), 'source': str(source),
          'scope': 'Independent Library and PlayerPrefs. Source must be synchronized again after final physics fixes before final tests.',
          'assetFiles': sum(p.is_file() for p in (target/'Assets').rglob('*'))}
(target/'Validation/creation.json').write_text(json.dumps(record, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
print(json.dumps(record))
