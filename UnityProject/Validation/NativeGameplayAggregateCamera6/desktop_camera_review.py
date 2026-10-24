"""One exact desktop HUD framing source pair, with explicitly changed pointer view.

This is neither a native-camera waiver nor a statement that800 levels were replayed
under the final presentation. New actual reports are mandatory and source-bound.
"""
from pathlib import Path
import difflib
import hashlib
import importlib.util
import itertools
import math
import sys

RULE = "desktop-original-hud-minimum-view-retreat-20261016"
PATH = "Assets/Scripts/RecoveryUI/RecoveredBoardFraming.cs"
BASE = "47c836757825aa0c7347a99678d19a287f406e1fa8db0f48db5c9470068816d1"
OTHER = "764d13974fe55642f4c040548e29304f8c6c9d25a68c847d11f788bdffdcbd17"
DIFF = "464c50a5150c68cd763d8cdb06f2c14f70acf674b4c9fa2905644bdd69b2ffc5"
HUD_EDITOR = "1596a5f9c4fba956779f0d76bb11a281ff2e6e33d62cdb8b1e4dfca1e00a2554"
RECIPE_TOOL = "9c3623a1a12f519ff4696331c3a9f1a11caefcf012b3f01d83e7eb793729a418"
# Draft blocker: Batch7 cleared the measured HUD, but actual Basic screenshots
# exposed an opaque background plane in front of the most distant geometry.
# This tool must not issue final success until the exact final background
# presentation source and independent actual visibility proof are reviewed.
FINAL_BACKGROUND_VISIBILITY_REVIEWED = False


def source_pair(approval, parent, audit):
    ensure = audit.ensure
    ensure(isinstance(approval, dict) and approval.get("ruleId") == RULE
           and approval.get("path") == PATH and approval.get("baseSha256") == BASE
           and approval.get("otherSha256") == OTHER, "Only the exact reviewed desktop Framing source pair is allowed")
    ensure(approval.get("physicsUnchanged") is True and approval.get("pointerPresentationChanged") is True
           and approval.get("mode0Unchanged") is False,
           "Desktop camera adaptation must explicitly disclose changed mode0 pointer presentation")
    paths = {}
    for key, expected in (("baseSource", BASE), ("otherSource", OTHER), ("fullDiff", DIFF)):
        item = approval.get(key)
        audit.evidence_blob(item, parent)
        ensure(item["sha256"] == expected, "Desktop source/diff is not the pinned reviewed content")
        paths[key] = (parent/item["path"]).resolve()
    before, after = (paths[k].read_text(encoding="utf-8") for k in ("baseSource", "otherSource"))
    diff = "".join(difflib.unified_diff(before.splitlines(True), after.splitlines(True),
                                      fromfile="a/"+PATH, tofile="b/"+PATH))
    ensure(paths["fullDiff"].read_text(encoding="utf-8") == diff, "Desktop Framing full exact diff differs")
    audit.evidence_blob(approval.get("scopeReview"), parent)
    return {"ruleId": RULE, "path": PATH, "baseSha256": BASE, "otherSha256": OTHER,
            "physicsUnchanged": True, "pointerPresentationChanged": True, "mode0Unchanged": False,
            "scopeReview": approval["scopeReview"], "fullDiff": approval["fullDiff"]}


def actual_report(item, parent, audit):
    """The actual fresh queue primary report and unchanged executed source pair."""
    ensure = audit.ensure
    report_path, report, report_sha = audit.evidence_file(item.get("report"), parent)
    started = audit.timestamp(report.get("startedUtc"), str(report_path)+":start")
    finished = audit.timestamp(report.get("finishedUtc"), str(report_path)+":finish")
    ensure(finished >= started and report.get("unityVersion") == audit.UNITY
           and report.get("passed") is True, "Desktop proof is unfinished, wrong-version or failed")
    errors = [report[key] for key in ("errors", "runtimeErrors") if key in report]
    ensure(errors and all(value == [] for value in errors), "Desktop proof lacks zero-error evidence")
    queue_path, queue, queue_sha = audit.evidence_file(item.get("queueEvidence"), parent)
    matched = []
    for job in queue.get("jobs", []):
        for fresh in job.get("freshReports", []):
            if fresh.get("sha256") == report_sha and fresh.get("relativePath") == job.get("reportFile"):
                matched.append((job, fresh))
    ensure(len(matched) == 1, "Desktop proof needs one actual fresh primary queue report")
    job, fresh = matched[0]
    ensure(job.get("exitCode") == 0 and job.get("changedSources") == []
           and fresh.get("startedUtc") == report["startedUtc"] and fresh.get("finishedUtc") == report["finishedUtc"],
           "Desktop proof process/source/timing binding differs")
    before_path, before, before_sha = audit.snapshot(job.get("beforeSnapshot"), queue_path.parent)
    after_path, after, after_sha = audit.snapshot(job.get("afterSnapshot"), queue_path.parent)
    ensure(before["files"] == after["files"], "Desktop proof source changed during execution")
    ensure(before["files"].get(PATH) == OTHER, "Desktop proof used an old/unreviewed camera presentation")
    ensure(audit.timestamp(before["capturedUtc"], "desktop before") <= started
           and audit.timestamp(after["capturedUtc"], "desktop after") >= finished,
           "Desktop proof source snapshots do not bracket the actual run")
    return {"report": report, "reportPath": report_path, "reportSha256": report_sha,
            "started": started, "finished": finished, "files": before["files"],
            "evidence": {"report": item["report"], "queueEvidence": item["queueEvidence"],
                         "beforeSnapshot": {"path": str(before_path), "sha256": before_sha},
                         "afterSnapshot": {"path": str(after_path), "sha256": after_sha}}}


def passing_checks(report, minimum, required, audit):
    checks = report.get("checks")
    audit.ensure(isinstance(checks, list) and len(checks) >= minimum
                 and all(isinstance(c, dict) and c.get("passed") is True for c in checks),
                 "Desktop proof is missing actual passing checks")
    audit.ensure(required <= {c.get("name") for c in checks}, "Desktop proof misses required actual checks")


def camera17(report, audit):
    passing_checks(report, 17, {"real-original-camera-binding", "production-quarter-turn",
        "actual-camera-follows-authored-sphere", "desktop-orbit-target-sync",
        "selected-piece-blocks-rotation", "production-begin-uses-original-entrada",
        "production-mobile-drag-moves-camera", "camera-pose-restored",
        "same-scene-level-load-resets-authored-camera"}, audit)
    audit.ensure(report.get("preferencesRestored") is True, "Camera17 did not restore preferences")


def hud24(report, audit):
    ensure = audit.ensure
    passing_checks(report, 1, set(), audit)
    ensure(report.get("runtimeSourceSha256") == OTHER and report.get("preferencesRestored") is True
           and report.get("enteredEditMode") is True and report.get("timeScaleRestored") is True
           and report.get("originalTimeScale") == report.get("restoredTimeScale") == 4,
           "HUD proof did not test final Framing and restore the actual editor Time4/preferences")
    frames = report.get("frames")
    expected = set(itertools.product((366, 409, 421, 401), ("initial", "solution"),
                                     ((1280,720),(885,630),(2560,1080))))
    ensure(isinstance(frames, list) and len(frames) == 24, "HUD proof needs24 actual frames")
    actual = []
    suffixes = ("actual-original-HUD-rectangles", "original-projection-and-rotation-retained",
                "protected-HUD-covers-actual-rendered-rects",
                "all-structural-vertices-in-camera", "structural-meshes-clear-original-HUD")
    names = {c["name"] for c in report["checks"]}
    for frame in frames:
        key = (frame.get("level"), frame.get("stage"), (frame.get("width"), frame.get("height")))
        actual.append(key)
        tag = "%s:%s:%sx%s" % (key[0],key[1],key[2][0],key[2][1])
        ensure(all(tag+":"+suffix in names for suffix in suffixes), "HUD frame lacks independent projection/overlap checks")
        ensure(audit.integer(frame.get("structuralVertices"),1) and audit.integer(frame.get("structuralMeshes"),1)
               and frame.get("outsideVertices") == frame.get("hudOverlapMeshes") == 0
               and frame.get("overlaps") == [], "HUD frame has missing mesh data or a real overlap/crop")
        ensure(isinstance(frame.get("protectedHud"),list) and frame["protectedHud"], "HUD frame did not measure original controls")
        ensure(all(type(frame.get(k)) in (int,float) and math.isfinite(frame[k]) for k in ("fov","nearClip","farClip","backwardDistance"))
               and frame["fov"]>0 and frame["nearClip"]>0 and frame["farClip"]>frame["nearClip"]
               and frame["backwardDistance"]>=0, "HUD projection sample is invalid")
        ensure(all(type(frame.get(k)) in (int,float) and math.isfinite(frame[k])
                   for k in ("minStructuralDepth","maxStructuralDepth"))
               and frame["nearClip"]-.001 <= frame["minStructuralDepth"] <= frame["maxStructuralDepth"]
               and frame["maxStructuralDepth"] <= frame["farClip"]+.001,
               "Actual structural depth is outside the original near/far projection")
        projections=frame.get("actualHudProjection")
        ensure(isinstance(projections,list) and projections, "Missing independent actual HUD world-corner projections")
        protected={x.get("path"):x.get("viewport") for x in frame["protectedHud"] if isinstance(x,dict)}
        ensure(len(protected)==len(frame["protectedHud"]) and all(isinstance(k,str) and k for k in protected)
               and len(projections)==len(protected) and len({x.get("path") for x in projections})==len(projections)
               and {x.get("path") for x in projections}==set(protected),
               "HUD original regions and independently measured rendered regions differ")
        def rect(value):
            ensure(isinstance(value,dict) and all(type(value.get(k)) in (int,float)
                   and math.isfinite(value[k]) for k in ("x","y","width","height"))
                   and value["width"]>0 and value["height"]>0,
                   "Invalid independent HUD viewport rectangle")
            return value
        for projection in projections:
            p=rect(protected[projection["path"]]);a=rect(projection.get("actualViewport"));copied=rect(projection.get("protectedViewport"))
            ensure(all(abs(p[k]-copied[k])<1e-7 for k in ("x","y","width","height")),
                   "Independent HUD observer did not record the same protected region")
            ensure(projection.get("covered") is True and p["x"]<=a["x"]+.0001 and p["y"]<=a["y"]+.0001
                   and p["x"]+p["width"]>=a["x"]+a["width"]-.0001
                   and p["y"]+p["height"]>=a["y"]+a["height"]-.0001,
                   "Protected HUD rectangle does not contain actual independently projected artwork")
        if key[0] == 401:
            # Pinned Fixture4 independently tests whether the authored base
            # already needs viewport/HUD fitting. A genuinely clear base
            # must retain zero retreat; aspect-dependent fitting is allowed.
            ensure(tag+":clear-small-level-no-unneeded-retreat" in names,
                   "Clear small level received unnecessary desktop retreat")
    ensure(set(actual)==expected and len(set(actual))==24, "HUD proof has duplicate/missing case/aspect/stage samples")
    for level in (366,409,421,401):
        ensure(all(str(level)+":initial-before-capture:"+suffix in names for suffix in (
                   "active-original-level-title","native-global-asset-identity","original-positive-local-level-number")),
               "HUD proof lacks actual positive native title before any capture/refresh wrapper")
        ensure(all(str(level)+":"+str(stage)+":resize-returns-to-wide-fit" in names for stage in (0,1)),
               "HUD proof misses an actual resize reversal")


def meshes1350(report, audit):
    ensure = audit.ensure
    rows=report.get("levels")
    ensure(report.get("completed")==report.get("requested")==1350 and report.get("all1350Instantiated") is True
           and report.get("failureCount")==0 and report.get("originalTimeScale")==4
           and isinstance(rows,list) and len(rows)==1350, "New Framing needs a closed all1350 actual mesh capture")
    ensure(len({x.get("assetId") for x in rows})==1350, "New Framing1350 has duplicate/missing ids")
    for row in rows:
        ensure(row.get("failures")==[] and row.get("originalPresentation") is True
               and audit.integer(row.get("structuralVerticesChecked"),1)
               and row.get("geometryOutsideViewport")==[] and audit.integer(row.get("activeRenderers"),1)
               and row.get("missingPrefabs")==row.get("emptyMaterials")==row.get("emptyMeshes")==0,
               "New1350 actual original-mesh projection failed")


def affected169(item, parent, audit):
    path, report, digest = audit.evidence_file(item.get("report"),parent)
    ensure = audit.ensure
    tool_path = Path(__file__).resolve().parent.parent / "aggregate_native_recipe_regression_v2.py"
    ensure(hashlib.sha256(tool_path.read_bytes()).hexdigest()==RECIPE_TOOL, "Independent169 audit implementation changed")
    spec=importlib.util.spec_from_file_location("camera6_native169_audit",tool_path)
    tool=importlib.util.module_from_spec(spec);sys.modules[spec.name]=tool;spec.loader.exec_module(tool)
    base=Path(report.get("baseRun",{}).get("path",""))
    retests=[Path(x["path"]) for x in report.get("retests",[])]
    request=(path.parent/report.get("requestEvidence",{}).get("path","")).resolve()
    bindings=(path.parent/report.get("bindingsEvidence",{}).get("path","")).resolve()
    recalculated=tool.aggregate(request,base,retests,bindings)
    for key in ("passed","plannedLevels","selectedRecordedSteps","baseFailedIds","unresolvedIds",
                "levels","checks","actualSourceLocks","originalRecordEvidence","sourceFinishedUtc",
                "baseRun","retests","bindingsEvidence","requestEvidence","expectedCatalogSha256",
                "schemaVersion","scopeAuditImplementation","nativeCarryEvidenceRule"):
        ensure(report.get(key)==recalculated[key], "Independent169 aggregate differs from re-audited actual source reports: "+key)
    ensure(report.get("passed") is True and report.get("plannedLevels")==169
           and report.get("selectedRecordedSteps")==2616 and report.get("unresolvedIds")==[],
           "New desktop Framing169 actual gameplay is incomplete/failed")
    comparisons=[]
    for lock in recalculated["actualSourceLocks"]:
        before_path,before,before_sha=audit.snapshot(lock["beforeSnapshot"],path.parent)
        ensure(before["files"].get(PATH)==OTHER,"Independent169 used old/unreviewed desktop Framing")
        comparisons.append((str(before_path),lock["reportSha256"],before["files"]))
    return {"report":item["report"],"sourceFinishedUtc":report["sourceFinishedUtc"],
            "reportSha256":digest,"comparisons":comparisons,"levels":169,"recordedSteps":2616}


def verify(approval, parent, audit, interaction):
    proof=source_pair(approval,parent,audit)
    sources=[];actual={}
    for field,checker in (("camera17",camera17),("hud24",hud24),("mesh1350",meshes1350)):
        checked=actual_report(approval.get(field,{}),parent,audit);checker(checked["report"],audit)
        if field=="hud24":
            audit.ensure(checked["files"].get("Assets/Editor/RecoveredTools/FlowWaterHudFramingValidation.cs")==HUD_EDITOR,
                         "HUD proof used an unreviewed measurement fixture")
        actual[field]=checked
        sources.append((str(checked["reportPath"]),checked["reportSha256"],checked["files"]))
        proof[field]=checked["evidence"]
    gameplay=affected169(approval.get("affected169",{}),parent,audit)
    sources.extend(gameplay.pop("comparisons"));proof["affected169"]=gameplay
    final_input=actual_report(interaction,parent,audit)
    input_path,input_snapshot,input_sha=audit.snapshot(interaction.get("runtimeSnapshot"),parent)
    audit.ensure(final_input["files"]==input_snapshot["files"],"Final Input61 queued executed snapshot differs")
    audit.ensure(final_input["evidence"]["beforeSnapshot"]["sha256"]==input_sha,
                 "Final Input61 must bind its actual queue-before snapshot")
    last=max([x["finished"] for x in actual.values()]+[audit.timestamp(gameplay["sourceFinishedUtc"],"169 finish")])
    audit.ensure(final_input["started"]>=last,"Final Input61 predates a required final-camera regression")
    for _,_,files in sources:
        audit.ensure(all(files.get(k)==final_input["files"].get(k) for k in audit.STRICT_FILES),
                     "New camera evidence changed strict native interaction/physics/water/input/time sources")
    proof["finalInput61"]=final_input["evidence"]
    audit.ensure(FINAL_BACKGROUND_VISIBILITY_REVIEWED,
                 "Draft only: final original background visibility source/proof remains unreviewed")
    proof["scopeDisclosure"]={"original800InputEvidenceUsesPreviousDesktopView":True,
        "all800ReplayedUnderFinalDesktopView":False,"nativePhysicsStrictFilesUnchanged":True,
        "finalDesktopPointerPresentationChanged":True,"newDesktopViewRegressionLevels":169,
        "newDesktopViewActualMeshCases":1350,"newDesktopViewCameraChecks":17,
        "newDesktopViewHudFrames":24,"finalIndependentInputChecksAtLeast":61,
        "nativeDevicePixelEquivalenceClaim":False}
    return proof,sources
