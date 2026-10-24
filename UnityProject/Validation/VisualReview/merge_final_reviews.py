"""Bind individually inspected current frames to a closed final capture report.

No unreviewed frame is inferred from a passing automated result or a filename.
"""
import argparse
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path

def read(path): return json.loads(path.read_text(encoding="utf-8-sig"))
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def require(ok, message):
    if not ok: raise ValueError(message)
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--report", type=Path, required=True)
parser.add_argument("--output", type=Path, required=True)
parser.add_argument("reviews", type=Path, nargs="+")
args = parser.parse_args()
report_path = args.report.resolve()
report = read(report_path)
require(report.get("passed") is True and report.get("finishedUtc") and report.get("unityVersion") == "2022.3.28f1", "A successful closed actual capture report is required")
require(report.get("completed") == report.get("requested") == len(report.get("levels", [])) == 1350, "Expected all1350 current cases")
expected = {}
for index, level in enumerate(report["levels"]):
    for phase, stage in enumerate(("start", "solution")):
        absolute = index * 2 + phase
        expected[(index, stage)] = (level["assetId"], absolute // 24 + 1, absolute % 24)
seen, sheets, inputs = {}, {}, []
for review_path in args.reviews:
    review_path = review_path.resolve()
    review = read(review_path)
    require(review.get("sourceStartedUtc") == report["startedUtc"], "Review belongs to a different capture run")
    if review.get("finalReportSha256"):
        require(review["finalReportSha256"] == sha(report_path), "Review final report binding differs")
    inputs.append({"path": str(review_path), "sha256": sha(review_path), "reviewer": review.get("reviewer")})
    for sheet in review["sheets"]:
        number = sheet["number"]
        path = report_path.parent / ("all-contact-{:03d}.png".format(number))
        require(path.is_file() and sha(path) == sheet["imageSha256"], "Reviewed sheet has changed: " + str(number))
        require(sheet.get("sourceStartedUtc") == report["startedUtc"], "Sheet source run differs")
        if number in sheets:
            require(sheets[number]["imageSha256"] == sheet["imageSha256"], "Conflicting sheet identity")
        else:
            sheets[number] = {"number": number, "path": str(path), "imageSha256": sheet["imageSha256"]}
        for frame in sheet["frames"]:
            key = (frame["sourceCaseIndex"], frame["stage"])
            require(key in expected and key not in seen, "Unexpected/duplicate reviewed frame: " + str(key))
            asset, expected_sheet, expected_tile = expected[key]
            require((frame["assetId"], number, frame["tileIndex"]) == (asset, expected_sheet, expected_tile), "Review tile does not map to the actual capture record")
            require(frame.get("reviewed") is True and frame.get("observation") and sheet.get("reviewedUtc"), "Unreviewed frame cannot be promoted")
            seen[key] = dict(frame, sheetNumber=number, reviewer=review["reviewer"], reviewedUtc=sheet["reviewedUtc"])
require(set(seen) == set(expected), "Missing actual visual reviews: {} of2700".format(len(set(expected) - set(seen))))
ordered = [seen[key] for key in expected]
issues = [{"assetId": x["assetId"], "stage": x["stage"], "issue": x.get("issue"), "fullResolutionRequired": x.get("fullResolutionRequired", False)}
    for x in ordered if x.get("issue") or x.get("fullResolutionRequired")]
now = datetime.now(timezone.utc).isoformat()
output = {"startedUtc": min(x["reviewedUtc"] for x in ordered), "finishedUtc": now,
    "unityVersion": report["unityVersion"], "passed": not issues, "errors": [],
    "sourceReport": str(report_path), "sourceReportSha256": sha(report_path),
    "sourceStartedUtc": report["startedUtc"], "sourceFinishedUtc": report["finishedUtc"],
    "expectedFrames": 2700, "framesReviewed": len(ordered), "levelsReviewed": len(ordered) // 2,
    "unresolvedIssues": issues, "reviewInputs": inputs,
    "limitation": "Actual original320x180 contact tiles inspected for geometry, materials, water, colors, UI and cropping. Static views do not establish animated or original-device pixel equivalence; separate actual gameplay and mechanism reports have their own scopes.",
    "sheets": [sheets[n] for n in sorted(sheets)], "frames": ordered}
args.output.parent.mkdir(parents=True, exist_ok=True)
args.output.write_text(json.dumps(output, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"output": str(args.output), "framesReviewed": len(ordered), "passed": output["passed"]}))
