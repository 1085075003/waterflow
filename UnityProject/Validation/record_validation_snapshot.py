"""Record on-disk sources alongside a Unity report; do not infer a test pass."""
import argparse
import hashlib
import json
import time
from datetime import datetime, timezone
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("report", help="Report path relative to Validation")
parser.add_argument("label", help="Snapshot filename stem")
args = parser.parse_args()
validation = Path(__file__).resolve().parent
project = validation.parent
report_path = validation / args.report
for attempt in range(5):
    try:
        raw_report = report_path.read_bytes()
        report = json.loads(raw_report.decode("utf-8-sig"))
        break
    except (OSError, ValueError):
        if attempt == 4:
            raise
        time.sleep(.1)
start = report.get("startedUtc")
started = datetime.fromisoformat(start.replace("Z", "+00:00")) if start else None
files = {path for path in (project / "Assets").rglob("*") if path.is_file()}
for directory in ("ProjectSettings", "Packages"):
    for pattern in ("*.json", "*.asset", "*.txt"):
        files.update((project / directory).glob(pattern))
entries = []
for path in sorted(files):
    data = path.read_bytes()
    modified = datetime.fromtimestamp(path.stat().st_mtime, timezone.utc)
    entries.append({"path": path.relative_to(project).as_posix(),
                    "sha256": hashlib.sha256(data).hexdigest(), "bytes": len(data),
                    "modifiedUtc": modified.isoformat(),
                    "modifiedAfterReportStarted": bool(started and modified > started)})
result = {
    "recordedUtc": datetime.now(timezone.utc).isoformat(),
    "report": args.report, "reportSha256AtRecording": hashlib.sha256(raw_report).hexdigest(),
    "reportStartedUtc": start, "reportFinishedUtc": report.get("finishedUtc", ""),
    "unityVersion": report.get("unityVersion", ""),
    "scope": "All on-disk Assets files, project settings and package configuration. This records file identity; the Unity report establishes test results. A snapshot taken during a run does not establish its eventual outcome.",
    "filesModifiedAfterReportStarted": [x["path"] for x in entries if x["modifiedAfterReportStarted"]],
    "files": entries,
}
destination = validation / (args.label + "-source-snapshot.json")
destination.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"snapshot": str(destination), "files": len(entries),
                  "modifiedAfterStart": result["filesModifiedAfterReportStarted"]}, ensure_ascii=False))
