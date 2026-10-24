"""Build the current idle Main project with the fixed installed Unity version."""
from pathlib import Path
import json
import subprocess
import sys
import run_unity_queue as queue

root = Path(__file__).resolve().parent
project = root.parent
queue.require(not queue.existing_unity(project), "Main is already open; no build launched")
queue.check_project_lock(project)
stamp = queue.utc().strftime("%Y%m%dT%H%M%S%fZ")
folder = root / "WindowsBuildRuns" / stamp
folder.mkdir(parents=True)
player = project / "Builds" / ("Windows-" + stamp) / "FlowWater.exe"
queue.require(queue.inside(player, project / "Builds") and not player.parent.exists(), "Build output must be a new workspace directory")
player.parent.mkdir(parents=True)
before, before_sha = queue.snapshot(project, folder / "sources-before.json", "Immediately before the actual Windows64 player build")
command = [str(queue.UNITY), "-batchmode", "-quit", "-projectPath", str(project),
    "-buildWindows64Player", str(player), "-logFile", str(folder / "unity-build.log")]
report = {"startedUtc": queue.iso(), "finishedUtc": None, "unityVersion": queue.VERSION,
    "project": str(project), "player": str(player), "command": command,
    "sourceBeforeSha256": before_sha, "scope": "Actual Windows64 build; no standalone player interaction or Android build is inferred."}
queue.write_json(folder / "windows-build-validation.json", report)
process = subprocess.Popen(command, cwd=project, **queue.hidden_options())
report["processId"] = process.pid
queue.write_json(folder / "windows-build-validation.json", report)
code = process.wait()
after, after_sha = queue.snapshot(project, folder / "sources-after.json", "After the actual Windows64 build process exited")
changes = queue.source_changes(before, after)
report.update({"finishedUtc": queue.iso(), "exitCode": code, "sourceAfterSha256": after_sha,
    "changedSources": changes, "playerExists": player.is_file(),
    "passed": code == 0 and player.is_file() and not changes})
log = folder / "unity-build.log"
if log.is_file():
    report["logSha256"] = queue.digest(log)
    report["buildSummary"] = [line for line in log.read_text(encoding="utf-8-sig", errors="replace").splitlines()
        if "Build completed with" in line or "BuildFailedException" in line or "error CS" in line][-20:]
if player.is_file():
    report["playerSha256"] = queue.digest(player)
    files = [path for path in player.parent.rglob("*") if path.is_file()]
    report["outputFiles"] = len(files)
    report["outputBytes"] = sum(path.stat().st_size for path in files)
queue.write_json(folder / "windows-build-validation.json", report)
print(json.dumps({key: report[key] for key in ["passed", "exitCode", "player", "changedSources", "finishedUtc"]}))
print(str(folder / "windows-build-validation.json"))
sys.exit(0 if report["passed"] else 1)
