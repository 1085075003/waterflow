"""Hash-checked import of already staged native restoration fixes into the main project."""
import argparse
import hashlib
import json
import shutil
from datetime import datetime, timezone
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--apply", action="store_true")
args = parser.parse_args()
validation = Path(__file__).resolve().parent
project = validation.parent
workspace = project.parent
entries = {}

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest() if path.exists() else None

def source_path(name):
    path = Path(name)
    if path.is_absolute():
        return path
    return (workspace if path.parts[0] == "UnityProject" else project) / path

def add(target, source, sha, previous):
    source = source_path(source)
    assert digest(source) == sha, f"Staged source hash mismatch: {source}"
    if target in entries:
        assert entries[target]["sha256"] == previous, f"Broken staged chain: {target}"
        previous = entries[target]["baseSha256"]
    else:
        assert digest(project / target) == previous, f"Main base hash mismatch: {target}"
    entries[target] = {"target": target, "source": str(source.relative_to(workspace)),
                       "sha256": sha, "baseSha256": previous}

for name in ("NativeCameraTutorialBatch1", "NativeCameraTutorialBatch2", "NativeJetSpringBatch1", "NativeFountainAnimationBatch1"):
    data = json.loads((validation / name / "manifest.json").read_text(encoding="utf-8-sig"))
    for f in data["files"]:
        add(f["target"], f["source"], f["sha256"], f.get("previousSha256", f.get("previousBatchSha256")))

for name, target, base_key in (
    ("staged-toggle-release-alignment-manifest.json", "Assets/Scripts/RecoveryRuntime/RecoveredNativeSprings.cs", "baseSHA256"),
    ("staged-release-alignment-tests-manifest.json", "Assets/Editor/RecoveredTools/FlowWaterSpringModeValidation.cs", "baseSHA256"),
    ("staged-native-camera-integration-manifest.json", "Assets/Scripts/RecoveryRuntime/RecoveredFlowGame.cs", "baseSHA256"),
    ("staged-native-input-validation-v7-manifest.json", "Assets/Editor/RecoveredTools/FlowWaterSave1GameplayValidation.cs", "mainBaseSHA256"),
    ("staged-full-native-tutorial-manifest.json", "Assets/Editor/RecoveredTools/FlowWaterFullValidation.cs", "mainBaseSHA256"),
):
    data = json.loads((validation / "NativeAnalysis" / name).read_text(encoding="utf-8-sig"))
    add(target, data["sourceFile"], data["sourceSHA256"], data[base_key])

guard = json.loads((validation / "NativeAnalysis/staged-preference-guard-manifest.json").read_text(encoding="utf-8-sig"))
for f in guard["files"]:
    add(f["target"], f["source"], f["sha256"], None)

destination = validation / "IntegratedNativeBatch1"
destination.mkdir(exist_ok=True)
manifest = {"preparedUtc": datetime.now(timezone.utc).isoformat(), "files": list(entries.values()),
            "scope": "Stable native spring release alignment, jet slot decoding, camera/tutorial, existing fountain animation, and validation tools. Original resources and layouts remain source evidence; additional water provider and preview animation work remains separate."}
manifest_path = destination / "manifest.json"
if manifest_path.exists():
    raise RuntimeError("Integration batch already prepared; do not overwrite immutable evidence.")
manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
if args.apply:
    for f in entries.values():
        target = project / f["target"]
        if target.exists():
            backup = destination / "Before" / f["target"]
            backup.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(target, backup)
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(workspace / f["source"], target)
        assert digest(target) == f["sha256"]
print(json.dumps({"files": len(entries), "applied": args.apply, "manifest": str(manifest_path)}))
