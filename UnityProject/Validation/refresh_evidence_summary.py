"""Summarize actual Unity reports without turning partial coverage into a pass."""
import hashlib
import json
import time
from datetime import datetime, timezone
from pathlib import Path

root = Path(__file__).resolve().parent
paths = {
    "assets": "audit.json",
    "data": "data-validation.json",
    "media": "media-validation.json",
    "ui": "UIFlows/ui-flow-validation.json",
    "interaction": "Interaction/interaction-validation.json",
    "cameraTutorial": "CameraTutorial/camera-tutorial-validation.json",
    "fountainAnimation": "FountainAnimation/fountain-animation-validation.json",
    "fountainDispatch": "FountainDispatch/fountain-dispatch-validation.json",
    "decorativePits": "DecorativePits/decorative-pit-validation.json",
    "jetLifetime": "JetLifetime/jet-lifetime-validation.json",
    "uiAnimations": "UIAnimations/native-ui-animation-validation.json",
    "localization": "NativeLanguage/native-language-validation.json",
    "pfuenteMarker": "PfuenteOcclusion/pfuente-occlusion-validation.json",
    "standaloneFountain": "StandaloneFountain/standalone-fountain-validation.json",
    "waterUV": "WaterUV/native-water-uv-validation.json",
    "bottomMarker": "BottomMarker/bottom-marker-validation.json",
    "nativeStepPlayback": "NativeStepPlayback/Automatic/native-step-playback-validation.json",
    "nativeStepMechanisms": "NativeStepPlayback/Mechanisms/native-step-playback-validation.json",
    "nativeStepLifts": "NativeStepPlayback/Lifts/native-step-playback-validation.json",
    "nativeStepMissing": "NativeStepPlayback/MissingCapture/native-step-playback-validation.json",
    "nativeCaptureLifetime": "NativeStepPlayback/CaptureLifetime/native-step-playback-validation.json",
    "springs": "Springs/spring-validation.json",
    "springModes": "SpringModes/spring-mode-validation.json",
    "springConstraints": "SpringConstraints/spring-constraint-validation.json",
    "aspect": "Aspect/aspect-validation.json",
    "categoryPreview": "CategoryPreview/category-preview-validation.json",
    "waterPropagation": "WaterPropagation/water-propagation-validation.json",
    "normalGameplay": "Save1Gameplay/native-gameplay-coverage.json",
    "nativeRecipeRegression": "FinalGameplay/native-recipe-regression-coverage.json",
    "nativeGeometryPose": "NativeGeometryPose/native-geometry-pose-validation.json",
    "hudFraming": "HudFraming/hud-framing-validation.json",
    "windowsBuild": "WindowsBuild/windows-build-validation.json",
    "allLayouts": "FullContent/all-runtime-validation.json",
    "visualReview": "VisualReview/final-frame-review.json",
}
summary = {
    "generatedUtc": datetime.now(timezone.utc).isoformat(),
    "requestedUnityVersion": "2022.3.28f1",
    "scope": "Actual Unity validation coverage. Successful automated checks do not establish complete equivalence to the original IL2CPP binary or validate real advertising, billing and analytics services.",
    "reports": {},
}
for kind, relative in paths.items():
    path = root / relative
    if not path.exists():
        summary["reports"][kind] = {"path": relative, "exists": False, "passed": False}
        continue
    for attempt in range(5):
        try:
            raw = path.read_bytes()
            report = json.loads(raw.decode("utf-8-sig"))
            break
        except (OSError, ValueError) as error:
            if attempt == 4:
                summary["reports"][kind] = {"path": relative, "exists": True,
                    "passed": False, "readError": str(error)}
                report = None
            else:
                time.sleep(.1)
    if report is None:
        continue
    checks = report.get("checks", [])
    levels = report.get("levels", [])
    errors = report.get("runtimeErrors", report.get("errors", []))
    entry = {
        "path": relative,
        "exists": True,
        "sha256": hashlib.sha256(raw).hexdigest(),
        "startedUtc": report.get("startedUtc", ""),
        "finishedUtc": report.get("finishedUtc", ""),
        "unityVersion": report.get("unityVersion", ""),
        "passed": report.get("passed", False),
        "checks": len(checks),
        "failedChecks": sum(not c.get("passed", False) for c in checks),
        "runtimeErrors": len(errors),
        "levelsRecorded": len(levels),
    }
    if kind == "normalGameplay":
        aggregated = report.get("aggregatedVerification") is True
        selected = [level.get("selected", {}) for level in levels] if aggregated else levels
        complete = [level for level in levels if level.get("passed")]
        runs = report.get("baseRuns", []) + report.get("retests", [])
        if aggregated:
            entry["startedUtc"] = min((r.get("startedUtc", "") for r in runs), default="")
            entry["finishedUtc"] = report.get("createdUtc", "")
        entry.update({
            "aggregatedVerification": aggregated,
            "basePartition": report.get("basePartition", ""),
            "inputStrategy": report.get("inputStrategy", "instant-placement-api"),
            "plannedLevels": report.get("plannedLevels", 0),
            "levelsPassed": len(complete),
            "levelsFailed": [level["id"] for level in levels if not level.get("passed")],
            "recordedPlayerSteps": report.get("selectedRecordedSteps", 0) if aggregated else sum(len(level.get("steps", [])) for level in levels),
            "all800NormalGameplay": len(levels) == 800 and len(complete) == 800
                and all(level.get("normalGameplay") and level.get("complete")
                        and level.get("nativeMissionComplete") and level.get("finalRequired", 0) > 0
                        and level.get("finalActive", 0) >= level["finalRequired"]
                        and level.get("pendingWaterEvents", 0) == 0 for level in selected),
        })
        entry["all800ProductionPointerDrag"] = entry["all800NormalGameplay"] and entry["inputStrategy"] == "production-pointer-drag"
        entry["passed"] = entry["passed"] and entry["all800ProductionPointerDrag"]
    elif kind == "nativeRecipeRegression":
        entry.update({"plannedLevels": report.get("plannedLevels", 0),
            "levelsPassed": sum(level.get("passed") is True for level in levels),
            "levelsFailed": [level["id"] for level in levels if not level.get("passed")],
            "recordedPlayerSteps": report.get("selectedRecordedSteps", 0),
            "aggregatedVerification": report.get("aggregatedVerification", False),
            "sourceFinishedUtc": report.get("sourceFinishedUtc", "")})
        entry["passed"] = entry["passed"] and entry["plannedLevels"] == len(levels) == entry["levelsPassed"] == 169 and entry["recordedPlayerSteps"] == 2616 and report.get("replaces800Coverage") is False
    elif kind == "hudFraming":
        entry.update({"frames": len(report.get("frames", [])),
            "runtimeSourceSha256": report.get("runtimeSourceSha256"),
            "preferencesRestored": report.get("preferencesRestored", False),
            "timeScaleRestored": report.get("timeScaleRestored", False)})
        entry["passed"] = entry["passed"] and entry["frames"] == 24 and not entry["failedChecks"] and entry["preferencesRestored"] and entry["timeScaleRestored"]
    elif kind == "windowsBuild":
        entry.update({"player": report.get("player"), "playerSha256": report.get("playerSha256"),
            "outputFiles": report.get("outputFiles", 0), "outputBytes": report.get("outputBytes", 0),
            "changedSources": report.get("changedSources", [])})
        entry["passed"] = entry["passed"] and report.get("exitCode") == 0 and report.get("playerExists") is True and not entry["changedSources"]
    elif kind == "allLayouts":
        entry.update({
            "requested": report.get("requested", 0),
            "completed": report.get("completed", 0),
            "all1350Instantiated": report.get("all1350Instantiated", False),
            "levelsFailed": [level["assetId"] for level in levels if level.get("failures")],
            "contactSheets": len(report.get("contactSheets", [])),
        })
        entry["passed"] = entry["passed"] and entry["completed"] == 1350 and entry["all1350Instantiated"]
    elif kind == "media":
        entry["assetsChecked"] = sum(report.get(field, 0) for field in
            ("textures", "meshes", "audioClips", "animations", "controllers", "materials", "shaders"))
        entry["failedAssets"] = report.get("failures", 0)
    elif kind == "visualReview":
        capture = root / "FullContent/all-runtime-validation.json"
        entry.update({"framesReviewed": report.get("framesReviewed", 0),
            "expectedFrames": report.get("expectedFrames", 2700),
            "unresolvedIssues": report.get("unresolvedIssues", []),
            "captureReportMatches": capture.exists() and report.get("sourceReportSha256") == hashlib.sha256(capture.read_bytes()).hexdigest()})
        entry["passed"] = entry["passed"] and entry["captureReportMatches"] and entry["framesReviewed"] == entry["expectedFrames"] == 2700 and not entry["unresolvedIssues"]
    summary["reports"][kind] = entry

summary["allRequestedSuitesPassed"] = all(
    entry.get("passed", False) and entry.get("finishedUtc")
    and entry.get("unityVersion") == summary["requestedUnityVersion"]
    for entry in summary["reports"].values()
)
destination = root / "validation-summary.json"
destination.write_text(json.dumps(summary, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"summary": str(destination), "allRequestedSuitesPassed": summary["allRequestedSuitesPassed"]}))
