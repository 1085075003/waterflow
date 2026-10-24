"""Import immutable staging manifests after the caller has stopped Unity on that project."""
import argparse
import hashlib
import json
import shutil
from datetime import datetime, timezone
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("project", type=Path)
parser.add_argument("manifests", nargs="+", type=Path)
args = parser.parse_args()
validation = Path(__file__).resolve().parent
workspace = validation.parent.parent
project = args.project.resolve()
assert project.is_relative_to(workspace), "Target must stay inside the workspace."
pending = {}

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest() if path.exists() else None

for manifest in args.manifests:
    for f in json.loads(manifest.read_text(encoding="utf-8-sig"))["files"]:
        target = project / f.get("target", f.get("path"))
        assert target.resolve().is_relative_to(project)
        source = Path(f.get("source", f.get("staged")))
        if not source.is_absolute():
            source = (workspace if source.parts[0] == "UnityProject" else validation.parent) / source
        wanted = f.get("sha256", f.get("stagedSha256"))
        previous = f.get("previousSha256", f.get("baseSha256"))
        assert sha(source) == wanted, f"Staged hash mismatch: {source}"
        actual = pending[str(target)]["sha256"] if str(target) in pending else sha(target)
        assert actual == previous or actual == wanted, f"Target base mismatch: {target}: {actual} != {previous}"
        pending[str(target)] = {"target": str(target.relative_to(project)), "source": str(source),
                                "sha256": wanted, "previousSha256": sha(target)}

archive = project / "Validation/StagedImports" / datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%S%fZ")
archive.mkdir(parents=True)
for f in pending.values():
    target = project / f["target"]
    if sha(target) == f["sha256"]:
        continue
    if target.exists():
        backup = archive / "Before" / f["target"]
        backup.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(target, backup)
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(f["source"], target)
    assert sha(target) == f["sha256"]
(archive / "manifest.json").write_text(json.dumps({"sources": list(map(str, args.manifests)),
    "files": list(pending.values())}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"verifiedFiles": len(pending), "importRecord": str(archive / "manifest.json")}))
