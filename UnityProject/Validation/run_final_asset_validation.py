"""Run the actual asset suite, archiving only its three reports (never Validation recursively)."""
import json
from pathlib import Path
import shutil
import subprocess
import sys
import run_unity_queue as queue

root = Path(__file__).resolve().parent
project = root.parent
queue.require(not queue.existing_unity(project), "Main is already open; asset validation not launched")
queue.check_project_lock(project)
stamp = queue.utc().strftime("%Y%m%dT%H%M%S%fZ")
folder = root / "AssetSuiteRuns" / stamp
folder.mkdir(parents=True)
names = ("audit.json", "data-validation.json", "media-validation.json")
for name in names:
    if (root / name).is_file():
        previous = folder / "previous"
        previous.mkdir(exist_ok=True)
        shutil.copy2(root / name, previous / name)
before, before_sha = queue.snapshot(project, folder / "sources-before.json", "Immediately before the final actual asset suite")
command = [str(queue.UNITY), "-batchmode", "-projectPath", str(project),
    "-executeMethod", "Recovered.FlowWater.Editor.FlowWaterValidation.RunAssetSuite",
    "-logFile", str(folder / "unity-assets.log")]
started = queue.utc()
report = {"startedUtc": started.isoformat(), "finishedUtc": None, "unityVersion": queue.VERSION,
    "passed": False, "command": command, "sourceBeforeSha256": before_sha,
    "scope": "Actual asset, static data replay and media checks; gameplay and render verification are separate."}
queue.write_json(folder / "asset-suite-validation.json", report)
process = subprocess.Popen(command, cwd=project, **queue.hidden_options())
report["processId"] = process.pid
queue.write_json(folder / "asset-suite-validation.json", report)
code = process.wait()
after, after_sha = queue.snapshot(project, folder / "sources-after.json", "After the final actual asset suite exited")
changes = queue.source_changes(before, after)
results = []
for name in names:
    path = root / name
    document = json.loads(path.read_text("utf-8-sig"))
    finished = queue.timestamp(document.get("finishedUtc"))
    # The original media auditor records only its finish. Bind that report to
    # this real launch using both its new finish and file modification time.
    fresh = (queue.timestamp(document["startedUtc"]) >= started) if document.get("startedUtc") else (
        finished >= started and path.stat().st_mtime >= started.timestamp())
    closed = started <= finished <= queue.utc()
    passed = fresh and closed and document.get("passed") is True and document.get("unityVersion") == queue.VERSION
    passed = passed and document.get("errors", document.get("runtimeErrors", [])) == []
    shutil.copy2(path, folder / name)
    results.append({"path": name, "sha256": queue.digest(path), "fresh": fresh,
        "passed": passed, "startedUtc": document.get("startedUtc"), "finishedUtc": document.get("finishedUtc"),
        "freshnessBasis": "actual report start" if document.get("startedUtc") else "new actual finish plus file modification after launch"})
report.update({"finishedUtc": queue.iso(), "exitCode": code, "sourceAfterSha256": after_sha,
    "changedSources": changes, "reports": results, "logSha256": queue.digest(folder / "unity-assets.log"),
    "passed": code == 0 and not changes and all(item["passed"] for item in results)})
queue.write_json(folder / "asset-suite-validation.json", report)
print(json.dumps({"passed": report["passed"], "changedSources": changes, "report": str(folder / "asset-suite-validation.json")}))
sys.exit(0 if report["passed"] else 1)
