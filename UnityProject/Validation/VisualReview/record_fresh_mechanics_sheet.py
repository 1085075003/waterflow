"""Record explicit observations only after native_mechanics views a fresh sheet.

No screenshot or tile is marked reviewed by enumeration or automated success.
"""
from pathlib import Path
import argparse
import datetime as dt
import hashlib
import json

HERE = Path(__file__).resolve().parent
WORKSPACE = HERE.parents[2]
REPORT = WORKSPACE / ".validation-visual-project/Validation/FullContent/all-runtime-validation.json"
OUTPUT = HERE / "fresh-native-recipes-mechanics-review.json"
START = "2026-10-16T09:12:08.0173697Z"


def load(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("number", type=int)
parser.add_argument("observations", type=Path,
                    help="Explicit JSON array of24 observations after actual view_image inspection")
args = parser.parse_args()
assert 57 <= args.number <= 84
report = load(REPORT)
assert report["startedUtc"] == START and report["unityVersion"] == "2022.3.28f1"
image = REPORT.parent / ("all-contact-%03d.png" % args.number)
assert image.is_file()
observations = load(args.observations)
assert isinstance(observations, list) and len(observations) == 24
review = load(OUTPUT)
assert review["sourceStartedUtc"] == START
assert not any(x["number"] == args.number for x in review["sheets"])
frames = []
for tile, item in enumerate(observations):
    assert isinstance(item, dict) and isinstance(item.get("observation"), str) and len(item["observation"]) > 10
    assert "issue" in item and isinstance(item.get("fullResolutionRequired"), bool)
    absolute = (args.number - 1) * 24 + tile
    index, phase = divmod(absolute, 2)
    assert index < len(report["levels"])
    frames.append(dict(item, sourceCaseIndex=index, assetId=report["levels"][index]["assetId"],
                       stage=("start", "solution")[phase], tileIndex=tile, reviewed=True))
now = dt.datetime.now(dt.timezone.utc).isoformat()
review["sheets"].append({"number": args.number, "imageSha256": hashlib.sha256(image.read_bytes()).hexdigest(),
                          "reviewedUtc": now, "sourceStartedUtc": START, "frames": frames})
review["sheets"].sort(key=lambda x: x["number"])
review["lastReviewedUtc"] = now
review["framesReviewed"] = sum(len(x["frames"]) for x in review["sheets"])
review["assignmentComplete"] = len(review["sheets"]) == 28
OUTPUT.write_text(json.dumps(review, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"number": args.number, "framesReviewed": review["framesReviewed"], "issues":
                  sum(bool(x["issue"]) or x["fullResolutionRequired"] for x in frames)}))
