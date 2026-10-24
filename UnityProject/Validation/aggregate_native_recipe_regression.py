"""Strict independent169-case affected-recipe coverage; never substitutes for800.

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
    ensure(audit.timestamp(before["capturedUtc"], "before") <= run.started
           and audit.timestamp(after["capturedUtc"], "after") >= run.finished,
           "Actual source snapshots do not bracket report")
    return {"reportSha256": run.digest, "queueEvidence": {"path": str(queue_path), "sha256": queue_sha},
            "beforeSnapshot": {"path": str(before_path), "sha256": before_sha},
            "afterSnapshot": {"path": str(after_path), "sha256": after_sha},
            "files": before["files"], "processExitCode": job["exitCode"]}


def select_attempts(base, retests, ids, catalog):
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
        attempts = [audit.audit_level(run, n) for run in [base, *ordered] if n in run.levels]
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
    ensure(all(item["files"] == locked[0]["files"] for item in locked),
           "All169 base/retest runtime, Resources JSON, Editor and Settings sources must be byte-identical")
    levels, failed = select_attempts(base, retests, request["ids"], catalog)
    unresolved = [x["id"] for x in levels if not x["passed"]]
    runs_by_sha = {run.digest: run for run in [base, *retests]}
    selected_checks = [check for check in base.data["checks"] if not check["name"].startswith("gm")]
    for item in levels:
        selected_run = runs_by_sha[item["selectedSourceSha256"]]
        item["selectedChecks"] = selected_run.checks_by_level[item["id"]]
        selected_checks.extend(item["selectedChecks"])
    for item in locked:
        item["sourceFileCount"] = len(item.pop("files"))
    now = datetime.now(timezone.utc).isoformat()
    return {"schemaVersion": 1, "createdUtc": now,
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
            "originalRecordEvidence": records,
            "limitation": "Strict169 independent scope only. This artifact neither replaces original800 evidence nor weakens its400/800 base rule. Latest actual failed retests remain failures. Top finishedUtc is the aggregate creation time; sourceFinishedUtc is the latest actual Unity run finish. Selected checks are copied from actual source reports. Current source hashes bind queue snapshots; they do not independently infer loaded assembly identity. All raw reports remain unchanged."}


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
