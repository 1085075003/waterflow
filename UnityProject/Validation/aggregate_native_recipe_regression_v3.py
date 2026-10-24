"""Strict independent169-case coverage with exact native carry and shader source locks.

This imports the unchanged per-level real-input/native-victory audit from Scoped5.
All input reports must already be closed. Nothing launches or controls Unity.
"""
from pathlib import Path
from datetime import datetime, timezone
import argparse
import hashlib
import importlib.util
import json
import sys

ROOT = Path(__file__).resolve().parent
DEPENDENCY = ROOT / "NativeGameplayAggregateScoped5/aggregate_native_gameplay.py"
DEPENDENCY_SHA = "1376fce6aae8cecf99cc3821ccb897368c67ed8fffd6303db9168e26985926fc"
REQUEST_SHA = "a20f63421380c5b04b22817346aca77725ba58f7cb8b57b17c0215a8fb2ee994"
CATALOG_SHA = "fe00b5eb0a0baa923ee8b520661022046a1ea81cc19fbd1399c8001d47744e85"
assert hashlib.sha256(DEPENDENCY.read_bytes()).hexdigest() == DEPENDENCY_SHA
spec = importlib.util.spec_from_file_location("native_scoped5_audit", DEPENDENCY)
audit = importlib.util.module_from_spec(spec)
sys.modules[spec.name] = audit
spec.loader.exec_module(audit)
ensure = audit.ensure


def reference(path):
    path = Path(path).resolve()
    return {"path": str(path), "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}


def request_evidence(path):
    data, digest = audit.read_json(path)
    ensure(digest == REQUEST_SHA, "Wrong prepared169 request bytes/SHA")
    ids = data.get("ids")
    ensure(isinstance(ids, list) and len(ids) == len(set(ids)) == 169
           and ids == sorted(ids) and all(audit.integer(x, 1) for x in ids), "Expected exact169 ordered unique ids")
    ensure(data.get("affectedNativeCount") == 166, "Affected native recipe count differs")
    _, scope, _ = audit.evidence_file(data.get("geometryScope"), path.parent)
    ensure(set(ids) == set(scope.get("affectedNativeSave1Ids", [])) | set(data.get("historicalFailureIds", [])),
           "169 request differs from affected166 plus historical failures")
    return data, digest


def shader_files(snapshot_data):
    shaders = snapshot_data.get("shaderFiles")
    ensure(isinstance(shaders, dict) and shaders, "Actual source snapshot lacks the complete nonempty shaderFiles dictionary")
    for path, digest in shaders.items():
        ensure(isinstance(path, str) and path.startswith("Assets/") and "\\" not in path
               and ".." not in path.split("/") and Path(path).suffix.lower() in (".shader", ".cginc", ".hlsl"),
               "Shader source paths must be normalized original Assets shader/include files")
        audit.sha(digest, "actual shader source:" + path)
    return shaders


def identical_run_sources(locked):
    ensure(locked and all(item["files"] == locked[0]["files"] for item in locked),
           "All169 base/retest runtime, Resources JSON, Editor and Settings sources must be byte-identical")
    ensure(all(item["shaderFiles"] == locked[0]["shaderFiles"] for item in locked),
           "All169 base/retest shaderFiles source dictionaries must be byte-identical")


def source_binding(run, item, parent):
    ensure(item.get("reportSha256") == run.digest, "Source binding report SHA differs")
    queue_path, queue, queue_sha = audit.evidence_file(item.get("queueEvidence"), parent)
    jobs = []
    for job in queue.get("jobs", []):
        matches = [entry for entry in job.get("freshReports", []) if entry.get("relativePath") == job.get("reportFile")
                   and entry.get("sha256") == run.digest]
        if matches:
            ensure(len(matches) == 1, "Ambiguous fresh primary report binding")
            jobs.append((job, matches[0]))
    ensure(len(jobs) == 1, "Run must bind exactly one actual fresh primary queue report")
    job, primary = jobs[0]
    ensure(primary.get("startedUtc") == run.data["startedUtc"] and primary.get("finishedUtc") == run.data["finishedUtc"],
           "Fresh queue report timing differs")
    ensure(primary.get("runtimeErrors") == [] and job.get("exitCode") == (0 if run.data["passed"] else 1),
           "Actual process did not close with its report verdict")
    ensure(job.get("changedSources") == [], "Source changed during actual run")
    before_path, before, before_sha = audit.snapshot(job.get("beforeSnapshot"), queue_path.parent)
    after_path, after, after_sha = audit.snapshot(job.get("afterSnapshot"), queue_path.parent)
    ensure(before["files"] == after["files"], "Source before/after actual run differs")
    before_shaders, after_shaders = shader_files(before), shader_files(after)
    ensure(before_shaders == after_shaders, "Shader sources before/after actual run differ")
    ensure(audit.timestamp(before["capturedUtc"], "before") <= run.started
           and audit.timestamp(after["capturedUtc"], "after") >= run.finished,
           "Actual source snapshots do not bracket report")
    return {"reportSha256": run.digest, "queueEvidence": {"path": str(queue_path), "sha256": queue_sha},
            "beforeSnapshot": {"path": str(before_path), "sha256": before_sha},
            "afterSnapshot": {"path": str(after_path), "sha256": after_sha},
            "files": before["files"], "shaderFiles": before_shaders, "processExitCode": job["exitCode"]}


CARRY_EDITOR = "Assets/Editor/RecoveredTools/FlowWaterSave1GameplayValidation.cs"
CARRY_EDITOR_SHA = "31ff8f90ae2ee52803ea34a5425fdd373f11219850cae868bdf5642de2a6b3fc"


def original_actor_count(level_id):
    layout, _ = audit.read_json(ROOT.parent / ("Assets/RecoveredData/LevelData/01_Layouts/gm%d.json" % level_id))
    pieces = layout.get("data", {}).get("pieza")
    ensure(isinstance(pieces, list) and pieces, "Original layout actor inventory missing")
    return len(pieces)


def native_commit_carry(step, run, level_id, source_files, actor_count):
    """Recompute every geometric relation; declared errors/booleans alone never suffice."""
    ensure(source_files.get(CARRY_EDITOR) == CARRY_EDITOR_SHA, "Carry proof requires the exact executed Batch11 Editor SHA")
    ensure(step.get("nativeCarryEvidenceVersion") == 1 and step.get("nativeCarryAfterCommit") is True
           and step.get("marker") is False and step.get("compressionLanding") is False
           and step.get("followsMovingSupport") is False, "Not the exact ordinary native postcommit-carry branch")
    for key in ("productionCommitObserved", "nativeCarryMembership", "nativeCarrierMoving", "nativeCarrierCommitObserved"):
        ensure(step.get(key) is True, "Missing actual native commit/carry observation: " + key)
    ensure(step.get("nativeCarrierTurning") is False, "Rotating carrier is outside the translating-carrier rule")
    frames = [step.get(k) for k in ("pickupFrame", "releaseFrame", "productionCommitFrame", "motionFinishedFrame", "committedFrame", "actualAfterFrame")]
    ensure(all(audit.integer(x, 1) for x in frames) and frames == sorted(frames)
           and step["actualAfterFrame"] == step["committedFrame"], "Native carry actual frame chain differs")
    carrier, selected, layer = (step.get(k) for k in ("nativeCarrierRecord", "sourceRecord", "nativeCarryLayer"))
    ensure(audit.integer(actor_count, 1) and audit.integer(carrier) and audit.integer(selected)
           and 0 <= carrier < actor_count and 0 <= selected < actor_count and carrier != selected
           and audit.integer(layer, 1), "Native carry owner/layer is missing, self or outside original layout")
    for value, target in ((step.get("productionCommitPosition"), step.get("destination")),
                          (step.get("productionCommitPosition"), step.get("productionCommitTarget")),
                          (step.get("originalGuide"), step.get("destination"))):
        ensure(audit.distance_squared(value, target) < .64, "Production commit/guide fails unchanged original Near(.8)")
    samples = step.get("productionCommitCarriers")
    ensure(isinstance(samples, list) and samples and all(isinstance(x, dict)
           and audit.integer(x.get("record")) and 0 <= x["record"] < actor_count
           and audit.finite_vector(x.get("position")) for x in samples), "Commit callback lacks original observer pose samples")
    ensure(len({x["record"] for x in samples}) == len(samples), "Duplicate original observer records in commit callback")
    match = [x for x in samples if x["record"] == carrier]
    ensure(len(match) == 1 and audit.distance_squared(match[0]["position"], step.get("nativeCarrierCommitPosition")) < 1e-8,
           "Actual carried owner does not match its synchronous commit sample")
    vectors = ("actualAfter", "productionCommitPosition", "nativeCarrierPosition", "nativeCarrierCommitPosition",
               "nativeCarryOffset", "nativeActorTranslation", "nativeCarrierTranslation")
    ensure(all(audit.finite_vector(step.get(key)) for key in vectors), "Native carry geometry is missing or nonfinite")
    def difference(a, b):
        return {key: step[a][key] - step[b][key] for key in ("x", "y", "z")}
    offset = difference("actualAfter", "nativeCarrierPosition")
    actor_delta = difference("actualAfter", "productionCommitPosition")
    carrier_delta = difference("nativeCarrierPosition", "nativeCarrierCommitPosition")
    ensure(audit.distance_squared(offset, step["nativeCarryOffset"]) < 1e-8
           and audit.distance_squared(actor_delta, step["nativeActorTranslation"]) < 1e-8
           and audit.distance_squared(carrier_delta, step["nativeCarrierTranslation"]) < 1e-8,
           "Declared native offset/translation differs from actual sampled poses")
    expected_offset = {"x": 0, "y": 7.6 * layer, "z": 0}
    offset_error = audit.distance_squared(offset, expected_offset) ** .5
    translation_error = audit.distance_squared(actor_delta, carrier_delta) ** .5
    ensure(offset_error < .03 and translation_error < .03, "Actual carry offset/displacement does not match original native carrier")
    for key, computed in (("nativeCarryOffsetError", offset_error), ("nativeCarryTranslationError", translation_error)):
        declared = step.get(key)
        ensure(type(declared) in (int, float) and 0 <= declared < .03 and abs(declared - computed) < .0001,
               "Declared native carry error does not match actual geometry")
    names = {x.get("name") for x in run.checks_by_level.get(level_id, []) if x.get("passed") is True}
    n = step.get("number")
    ensure(all("gm%d-native-input-%s-%s" % (level_id, suffix, n) in names
               for suffix in ("actual-commit-pose", "actual-postcommit-carry")), "Missing actual commit and native carry check results")
    return {"step": n, "sourceRecord": selected, "nativeCarrierRecord": carrier, "nativeCarryLayer": layer,
            "productionCommitFrame": step["productionCommitFrame"], "actualAfterFrame": step["actualAfterFrame"],
            "productionCommitPosition": step["productionCommitPosition"], "actualAfter": step["actualAfter"],
            "carrierAtCommit": match[0]["position"], "carrierAfter": step["nativeCarrierPosition"],
            "recomputedOffsetError": offset_error, "recomputedTranslationError": translation_error,
            "executedEditorSha256": CARRY_EDITOR_SHA}


def audit_level_with_carry(run, level_id, source_files, actor_count):
    result = audit.audit_level(run, level_id)
    proofs = []
    for step in run.levels[level_id].get("steps", []):
        if not isinstance(step, dict) or step.get("nativeCarryAfterCommit") is not True:
            continue
        issue = "Step%s final pose fails unchanged native-validator Near(.8) bound" % step.get("number")
        try:
            proof = native_commit_carry(step, run, level_id, source_files, actor_count)
        except audit.EvidenceError as error:
            result["auditIssues"].append("Step%s native postcommit-carry rejected: %s" % (step.get("number"), error))
            continue
        if issue in result["auditIssues"]:
            result["auditIssues"].remove(issue)
        proofs.append(proof)
    result["nativePostCommitCarryEvidence"] = proofs
    result["passed"] = not result["auditIssues"]
    return result


def select_attempts(base, retests, ids, catalog, source_files=None):
    ensure(base.data["plannedLevels"] == 169 and list(base.levels) == ids,
           "Base must contain all exact169 requested ids in request order")
    ensure(len({run.digest for run in [base, *retests]}) == 1 + len(retests), "Duplicate run input")
    failed = {n for n, level in base.levels.items() if not level.get("passed") or level.get("failure")}
    known = {base.digest: base}
    ordered = sorted(retests, key=lambda r: (r.finished, r.started, r.digest))
    for run in ordered:
        ensure(set(run.levels) <= failed, "Retest contains an original-base-passed or foreign id")
        ensure(run.started >= base.finished, "Retest predates closed169 base")
        ensure(audit.catalog(run.data.get("expectedCatalog"), str(run.path)) == catalog,
               "Retest changed original800 catalog/data")
        origin = audit.retest_origin(run.data.get("retestSource"))
        ensure(isinstance(origin, dict) and origin.get("sha256") in known, "Retest needs exact closed-source SHA binding")
        source = known[origin["sha256"]]
        ensure(origin.get("startedUtc") == source.data["startedUtc"] and origin.get("finishedUtc") == source.data["finishedUtc"]
               and run.started >= source.finished, "Retest source timing differs/not closed")
        closed_failed = sorted(n for n, level in source.levels.items() if not level.get("passed") or level.get("failure"))
        ensure(origin.get("closedFailedIds") == closed_failed and sorted(run.levels) == closed_failed,
               "Formal retest must replay exactly all failed ids of its bound closed source")
        known[run.digest] = run
    counts = {x["id"]: x["recordedSteps"] for x in catalog["levels"]}
    results = []
    for n in ids:
        attempts = [audit_level_with_carry(run, n, (source_files or {}).get(run.digest, {}), original_actor_count(n))
                    for run in [base, *ordered] if n in run.levels]
        selected = attempts[-1]
        if selected["recordedSteps"] != counts[n]:
            selected["passed"] = False
            selected["auditIssues"].append("Selected step count differs from original unchanged catalog")
        results.append({"id": n, "passed": selected["passed"], "attempts": attempts,
                        "selectedSourceSha256": selected["source"]["sha256"], "selected": selected})
    return results, sorted(failed)


def original_record_evidence(runs, ids, catalog):
    """Bind actual reported origin/destination/exception to original Save1 data."""
    rows = {x["id"]: x for x in catalog["levels"]}
    evidence = []
    for n in ids:
        raw = ROOT.parent / ("Assets/Resources/so%d.bytes" % n)
        layout = ROOT.parent / ("Assets/Resources/gm%d.bytes" % n)
        decoded = ROOT.parent / ("Assets/RecoveredData/LevelData/02_Solutions/so%d.json" % n)
        decoded_layout = ROOT.parent / ("Assets/RecoveredData/LevelData/01_Layouts/gm%d.json" % n)
        raw_ref, layout_ref, decoded_ref = reference(raw), reference(layout), reference(decoded)
        layout_decoded_ref = reference(decoded_layout)
        ensure(decoded_ref["sha256"] == rows[n]["solutionSha256"]
               and layout_decoded_ref["sha256"] == rows[n]["layoutSha256"], "Original Save1/layout JSON differs from catalog")
        envelope, _ = audit.read_json(decoded)
        layout_envelope, _ = audit.read_json(decoded_layout)
        ensure(envelope.get("source_sha256") == raw_ref["sha256"] and envelope.get("source_asset") == "so%d" % n,
               "Decoded Save1 source provenance differs from raw original")
        ensure(layout_envelope.get("source_sha256") == layout_ref["sha256"]
               and layout_envelope.get("source_asset") == "gm%d" % n, "Decoded layout provenance differs from raw original")
        data = envelope.get("data", {})
        count = rows[n]["recordedSteps"]
        ensure(data.get("__type__") == "Save1" and all(isinstance(data.get(k), list) and len(data[k]) >= count
               for k in ("move", "orix", "oriy", "oriz", "desx", "desy", "desz", "excep")), "Original Save1 arrays missing/truncated")
        ensure(len(data["move"]) == count, "Original Save1 length differs from catalog")
        for run in runs:
            if n not in run.levels:
                continue
            steps = run.levels[n].get("steps", [])
            ensure(len(steps) <= count, "Report adds original Save1 records")
            for index, step in enumerate(steps):
                def point(prefix):
                    return {axis: ((data[prefix + axis][index]-3)*20 if axis != "y"
                                   else 3.95+(data[prefix+axis][index]-1)*7.6) for axis in ("x", "y", "z")}
                ensure(step.get("number") == index+1 and step.get("exception", "") == (data["excep"][index] or "")
                       and audit.distance_squared(step.get("origin"), point("ori")) < .000001
                       and audit.distance_squared(step.get("destination"), point("des")) < .000001,
                       "Report origin/destination/exception differs from actual original Save1 record")
        evidence.append({"id": n, "originalRecords": count, "rawSave1": raw_ref,
                         "rawLayout": layout_ref, "decodedSave1": decoded_ref, "decodedLayout": layout_decoded_ref})
    return evidence


def aggregate(request_path, base_path, retest_paths, bindings_path):
    request, request_sha = request_evidence(request_path)
    base = audit.load_run(base_path)  # Deliberately not base=True:800 rules remain unchanged.
    ensure(base.data.get("requestedRegression", {}).get("sha256") == request_sha
           and base.data["requestedRegression"].get("ids") == request["ids"]
           and base.data["requestedRegression"].get("path"), "Base requestedRegression does not bind prepared request")
    catalog = audit.catalog(base.data.get("expectedCatalog"), str(base.path))
    ensure(catalog["catalogSha256"] == CATALOG_SHA, "Original800/11919 catalog SHA differs")
    retests = [audit.load_run(path) for path in retest_paths]
    records = original_record_evidence([base, *retests], request["ids"], catalog)
    bindings, bindings_sha = audit.read_json(bindings_path)
    entries = bindings.get("runs")
    ensure(isinstance(entries, list) and len(entries) == len([base, *retests]), "One actual queue binding per supplied report required")
    by_sha = {entry.get("reportSha256"): entry for entry in entries if isinstance(entry, dict)}
    ensure(len(by_sha) == len(entries) and set(by_sha) == {run.digest for run in [base, *retests]},
           "Source bindings have duplicate, unused or missing run SHA")
    locked = [source_binding(run, by_sha[run.digest], bindings_path.parent) for run in [base, *retests]]
    identical_run_sources(locked)
    levels, failed = select_attempts(base, retests, request["ids"], catalog,
                                     {item["reportSha256"]: item["files"] for item in locked})
    unresolved = [x["id"] for x in levels if not x["passed"]]
    runs_by_sha = {run.digest: run for run in [base, *retests]}
    selected_checks = [check for check in base.data["checks"] if not check["name"].startswith("gm")]
    for item in levels:
        selected_run = runs_by_sha[item["selectedSourceSha256"]]
        item["selectedChecks"] = selected_run.checks_by_level[item["id"]]
        selected_checks.extend(item["selectedChecks"])
    for item in locked:
        item["sourceFileCount"] = len(item.pop("files"))
        shaders = item.pop("shaderFiles")
        item["shaderFileCount"] = len(shaders)
        item["shaderFilesSha256"] = hashlib.sha256(json.dumps(shaders, sort_keys=True, separators=(",", ":")).encode("utf-8")).hexdigest()
    now = datetime.now(timezone.utc).isoformat()
    return {"schemaVersion": 3, "createdUtc": now,
            "startedUtc": base.data["startedUtc"], "finishedUtc": now,
            "sourceFinishedUtc": max([base, *retests], key=lambda run: run.finished).data["finishedUtc"],
            "scope": "Independent169 affected-native-recipe plus historical-failure normal gameplay regression",
            "aggregatedVerification": True, "singleRunAllPassedClaim": False, "replaces800Coverage": False,
            "passed": not unresolved, "unityVersion": audit.UNITY, "inputStrategy": audit.STRATEGY,
            "requestEvidence": reference(request_path), "requestedLevels": 169, "plannedLevels": 169,
            "originalTimeScale": 4, "preferencesRestored": True, "runtimeErrors": [],
            "affectedNativeCount": 166, "historicalFailureIds": request["historicalFailureIds"],
            "expectedCatalogSha256": CATALOG_SHA, "original800RecordedSteps": audit.RECORDED_STEPS,
            "selectedRecordedSteps": sum(x["selected"]["recordedSteps"] for x in levels),
            "passedLevels": 169-len(unresolved), "failedLevels": len(unresolved), "unresolvedIds": unresolved,
            "baseFailedIds": failed, "baseRun": base.source(), "retests": [run.source() for run in retests],
            "bindingsEvidence": {"path": str(bindings_path.resolve()), "sha256": bindings_sha},
            "actualSourceLocks": locked, "levels": levels, "checks": selected_checks,
            "auditImplementation": reference(DEPENDENCY),
            "scopeAuditImplementation": reference(Path(__file__)),
            "shaderSourceScope": {"capturedSeparatelyFromFiles": True, "completeSnapshotDictionariesRequired": True,
                "allBaseRetestShadersByteIdentical": True, "shaderDifferencesWaived": False},
            "nativeCarryEvidenceRule": {"version": 1, "editorPath": CARRY_EDITOR, "editorSha256": CARRY_EDITOR_SHA,
                "originalNearBound": .8, "maximumNativeOffsetAndTranslationError": .03,
                "onlyException": "Exact production commit followed by observed original carried membership and equal actual carrier displacement"},
            "originalRecordEvidence": records,
            "limitation": "Strict169 independent scope only. This artifact neither replaces original800 evidence nor weakens its400/800 base rule. Latest actual failed retests remain failures. Top finishedUtc is the aggregate creation time; sourceFinishedUtc is the latest actual Unity run finish. Selected checks are copied from actual source reports. Only the pinned Batch11 harness may prove exact normal commit followed by original native carrier translation; final actualAfter is preserved and no tolerance is enlarged. Current source hashes bind queue snapshots; they do not independently infer loaded assembly identity. New actual shaderFiles snapshots are additionally mandatory and byte-identical before/after and across every169 base/retest. No shader exception is supported. All raw reports remain unchanged."}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--request", type=Path, required=True)
    parser.add_argument("--base", type=Path, required=True)
    parser.add_argument("--retest", type=Path, action="append", default=[])
    parser.add_argument("--bindings", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    try:
        ensure(not args.output.exists(), "Never overwrite prior coverage/report evidence")
        result = aggregate(args.request.resolve(), args.base.resolve(), [p.resolve() for p in args.retest], args.bindings.resolve())
        audit.atomic_write(args.output.resolve(), result)
        print(json.dumps({"output": str(args.output.resolve()), "passed": result["passed"], "unresolvedIds": result["unresolvedIds"]}))
        return 0 if result["passed"] else 1
    except (audit.EvidenceError, OSError, ValueError, KeyError) as error:
        print("Evidence rejected: " + str(error), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
