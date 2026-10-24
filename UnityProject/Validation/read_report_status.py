"""Read live Unity reports, retrying partial writes without reusing stale data."""
import argparse
import json
import time
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("reports", nargs="+")
parser.add_argument("--failures", action="store_true")
args = parser.parse_args()
for name in args.reports:
    report = None
    error = None
    for attempt in range(5):
        try:
            report = json.loads(Path(name).read_text(encoding="utf-8-sig"))
            break
        except (OSError, ValueError) as exc:
            error = str(exc)
            time.sleep(.1)
    if report is None:
        print(json.dumps({"report": name, "readError": error}))
        continue
    levels = report.get("levels", [])
    completed = report.get("completed", len(levels))
    passed = sum(level.get("passed", not level.get("failures")) for level in levels[:completed])
    failed = [level for level in levels if level.get("failure") or level.get("failures")]
    row = {"report": name, "startedUtc": report.get("startedUtc"),
           "finishedUtc": report.get("finishedUtc"), "passed": report.get("passed"),
           "inputStrategy": report.get("inputStrategy"), "levelsPassed": report.get("passedLevels", passed),
           "levelsFailed": report.get("failedLevels", len(failed)), "levelsRecorded": report.get("completedLevels", len(levels)),
           "plannedLevels": report.get("plannedLevels", report.get("requested")),
           "current": report.get("id", levels[-1].get("id", levels[-1].get("assetId")) if levels else None),
           "currentStep": report.get("step", len(levels[-1].get("steps", [])) if levels else 0),
           "runtimeErrors": len(report.get("runtimeErrors", report.get("errors", []))),
           "checks": len(report.get("checks", []))}
    if args.failures:
        row["failures"] = [{"id": level.get("id", level.get("assetId")),
                            "reason": (level.get("failure") or "; ".join(level.get("failures", []))).splitlines()[0][:220]}
                           for level in failed]
    print(json.dumps(row, ensure_ascii=True))
