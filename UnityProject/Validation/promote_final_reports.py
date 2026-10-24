"""Publish hash-bound finished queue archives to the user-facing report folders.

Default is a dry run. --apply preserves each previous folder under History.
This copies evidence only; it never changes Assets, source reports or verdicts.
"""
import argparse
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import shutil

root = Path(__file__).resolve().parent
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("queues", nargs="+", type=Path)
parser.add_argument("--apply", action="store_true")
args = parser.parse_args()
def read(path): return json.loads(path.read_text(encoding="utf-8-sig"))
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def require(ok, message):
    if not ok: raise ValueError(message)
step_names = {"22-step-automatic": "Automatic", "23-step-mechanism": "Mechanisms",
    "24-step-lifts": "Lifts", "25-step-missing-capture": "MissingCapture",
    "26-step-reference-lifetime": "CaptureLifetime"}
selections = []
for queue_path in args.queues:
    queue_path = queue_path.resolve()
    record = read(queue_path)
    require(record.get("finishedUtc") and not record.get("dryRun"), "Queue has not finished: " + str(queue_path))
    for job in record["jobs"]:
        require(job.get("passed") is True and job.get("exitCode") == 0 and not job.get("changedSources"),
            "Job is not an unchanged-source success: " + job["name"])
        archive = Path(job["archiveDestination"]).resolve()
        require(archive.is_relative_to(root) and archive.is_dir(), "Archive outside Validation/missing")
        fresh = job.get("freshReports", [])
        require(len(fresh) == 1, "Expected one explicit primary report: " + job["name"])
        proof = fresh[0]
        relative = Path(proof["relativePath"])
        report_path = archive / relative
        require(report_path.is_relative_to(archive) and sha(report_path) == proof["sha256"], "Archived report hash changed")
        report = read(report_path)
        require(report.get("passed") and report.get("finishedUtc") and report.get("unityVersion") == "2022.3.28f1",
            "Archived report is incomplete or failed")
        report_directory = Path(job["reportDirectory"]).resolve()
        project = Path(record["project"]).resolve()
        relative_directory = report_directory.relative_to(project / "Validation")
        require(relative_directory.parts[0] not in ("History", "FinalGameplay"), "Unsupported canonical directory")
        if relative_directory == Path("Save1Gameplay"):
            # Native partitions/retests remain immutable aggregate inputs.
            continue
        if job["name"] == "04-input-precheck":
            relative_directory = Path("Interaction/Precheck")
        if relative_directory == Path("NativeStepPlayback"):
            require(job["name"] in step_names, "Unrecognized shared step output; provide explicit mapping")
            relative_directory /= step_names[job["name"]]
        destination = (root / relative_directory).resolve()
        require(destination.is_relative_to(root) and destination != root and not archive.is_relative_to(destination), "Unsafe promotion path")
        inventory = job["archive"]["files"]
        for name, expected in inventory.items():
            path = archive / name
            require(path.resolve().is_relative_to(archive) and path.is_file() and sha(path) == expected, "Archive inventory changed: " + name)
        selections.append({"job": job["name"], "queueRun": str(queue_path), "queueRunSha256": sha(queue_path),
            "archive": str(archive), "destination": str(destination), "report": str(destination / relative),
            "reportSha256": proof["sha256"], "startedUtc": report["startedUtc"], "finishedUtc": report["finishedUtc"]})
require(len({x["destination"] for x in selections}) == len(selections), "Duplicate canonical destinations; select a single final attempt")
stamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%S%fZ")
for selection in selections:
    destination = Path(selection["destination"])
    print(("APPLY " if args.apply else "DRY RUN ") + selection["job"] + " -> " + str(destination))
    if args.apply:
        if destination.exists():
            backup = root / "History" / ("promotion-" + stamp) / destination.relative_to(root)
            require(backup.resolve().is_relative_to(root / "History") and not backup.exists(), "Unsafe/existing backup")
            backup.parent.mkdir(parents=True, exist_ok=True)
            shutil.move(str(destination), str(backup))
            selection["previousDirectory"] = str(backup)
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copytree(selection["archive"], destination)
        require(sha(Path(selection["report"])) == selection["reportSha256"], "Promoted report hash differs")
if args.apply:
    output = root / "FinalValidationRuns" / ("promotion-" + stamp + ".json")
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps({"createdUtc": datetime.now(timezone.utc).isoformat(), "selections": selections,
        "scope": "Exact report bytes copied from successful finished source-bound queue jobs; all previous canonical folders preserved."}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(output)
