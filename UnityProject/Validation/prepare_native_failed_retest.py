"""Prepare a fresh actual retest of exactly the failed IDs in one closed run."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("project", type=Path)
parser.add_argument("source", type=Path)
parser.add_argument("label")
policies = parser.add_mutually_exclusive_group()
policies.add_argument("--settled", action="store_true", help="Use the standard settled original-guide input strategy.")
policies.add_argument("--early-held-settled", action="store_true", help="Early held release followed by the original settled post-step pauses.")
args = parser.parse_args()
root = Path(__file__).resolve().parent
workspace = root.parent.parent
project = args.project.resolve()
source = args.source.resolve()
assert project.is_relative_to(workspace) and source.is_relative_to(root)
assert args.label and all(c.isalnum() or c in "-_" for c in args.label)
raw = source.read_bytes()
report = json.loads(raw.decode("utf-8-sig"))
assert report.get("finishedUtc") and report.get("startedUtc")
assert report["unityVersion"] == "2022.3.28f1"
assert report["inputStrategy"] == "production-pointer-drag"
ids = [case["id"] for case in report["levels"] if not case["passed"]]
assert ids and len(ids) == len(set(ids))
destination = project / "Validation/Save1Gameplay/native-retest-source.json"
destination.parent.mkdir(parents=True, exist_ok=True)
shutil.copy2(source, destination)
queue = {"schemaVersion": 1, "project": str(project), "continueOnFailure": False,
    "jobs": [{"name": args.label,
        "method": "Recovered.FlowWater.Editor.FlowWaterSave1GameplayValidation." + ("RunNativeFailed" if args.settled else "RunNativeFailedEarlyHeldSettled" if args.early_held_settled else "RunNativeFailedEarlyRelease"),
        "reportDirectory": "Validation/Save1Gameplay", "reportFile": "save1-gameplay-validation.json",
        "archiveDestination": "FinalGameplay/" + args.label, "timeoutSeconds": 1800}],
    "baseSource": {"path": str(source), "sha256": hashlib.sha256(raw).hexdigest(), "failedIds": ids}}
output = root / "FinalQueues" / (args.label + ".json")
output.write_text(json.dumps(queue, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"preparedOnly": True, "queue": str(output), "failedIds": ids}))
