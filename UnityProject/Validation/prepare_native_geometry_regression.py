"""Prepare, without launching Unity, a full-resolution replay of closed render failures."""
import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parent
source = root / "FinalRender/FullContentNativeUV/all-runtime-validation.json"
raw = source.read_bytes()
report = json.loads(raw.decode("utf-8-sig"))
assert report["finishedUtc"] and report["completed"] == 1350
assert report["unityVersion"] == "2022.3.28f1" and not report["runtimeErrors"]
ids = [case["assetId"] for case in report["levels"] if case["failures"]]
assert ids and len(ids) == len(set(ids)) and len(ids) <= 100
request = {"ids": ids, "scope": "Closed previous render failures; original verdicts remain preserved.",
    "source": {"path": str(source), "sha256": hashlib.sha256(raw).hexdigest(),
        "startedUtc": report["startedUtc"], "finishedUtc": report["finishedUtc"]}}
output = root / "FinalValidationPlan"
output.mkdir(exist_ok=True)
(output / "native-geometry-detail-request.json").write_text(
    json.dumps(request, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
queue = {"schemaVersion": 1, "project": "../..", "continueOnFailure": False,
    "jobs": [{"name": "native-geometry-closed-render-failures",
        "method": "Recovered.FlowWater.Editor.FlowWaterFullValidation.RunVisualDetails",
        "reportDirectory": "Validation/FullContent", "reportFile": "detail-runtime-validation.json",
        "archiveDestination": "FinalRender/NativeGeometryRegression1", "timeoutSeconds": 1800}],
    "requestedIds": ids, "source": request["source"],
    "executionGate": "Import and review the immutable native geometry correction first; copy the prepared request to VisualReview/detail-request.json only while Main is idle."}
(output / "native-geometry-render-regression.queue.json").write_text(
    json.dumps(queue, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"preparedOnly": True, "closedFailedCount": len(ids), "ids": ids}))
