"""Prepare a separate actual input regression for all affected original recipe IDs."""
import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parent
scope_path = root / "NativeGeometryStack1/scope-audit.json"
scope_bytes = scope_path.read_bytes()
scope = json.loads(scope_bytes.decode("utf-8-sig"))
assert scope["allChangedPrefabTreesContainNoColliderOrRigidbody"]
assert scope["nonRecipeFieldsBitIdentical"]
base = json.loads((root / "FinalGameplay/Half1/save1-gameplay-validation.json").read_text(encoding="utf-8-sig"))
catalog = {case["id"] for case in base["expectedCatalog"]["levels"]}
affected = set(scope["affectedNativeSave1Ids"])
assert affected and affected <= catalog
historical_failures = {690, 1105, 1187, 1216, 2033}
ids = sorted(affected | historical_failures)
request = {"ids": ids, "scope": "Affected original recipe input regression, plus the five historical failures. Separate from failed-only aggregate closure.",
    "affectedNativeCount": len(affected), "historicalFailureIds": sorted(historical_failures),
    "geometryScope": {"path": str(scope_path), "sha256": hashlib.sha256(scope_bytes).hexdigest()}}
output = root / "FinalValidationPlan"
(output / "native-recipe-regression-request.json").write_text(
    json.dumps(request, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
queue = {"schemaVersion": 1, "project": "../..", "continueOnFailure": False,
    "jobs": [{"name": "native-recipe-affected-input-regression",
        "method": "Recovered.FlowWater.Editor.FlowWaterSave1GameplayValidation.RunNativeRequestedRegression",
        "reportDirectory": "Validation/Save1Gameplay", "reportFile": "save1-gameplay-validation.json",
        "archiveDestination": "FinalGameplay/NativeRecipeRegression1", "timeoutSeconds": 5400}],
    "executionGate": "Complete reviewed resource and Editor guard imports, freeze final sources, and copy the prepared request to idle Main Validation/Save1Gameplay/native-regression-request.json before launch.",
    "requestedIds": ids, "scope": request["scope"], "geometryScope": request["geometryScope"]}
(output / "native-recipe-input-regression.queue.json").write_text(
    json.dumps(queue, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"preparedOnly": True, "affectedNative": len(affected), "totalIncludingHistoricalFailures": len(ids)}))
