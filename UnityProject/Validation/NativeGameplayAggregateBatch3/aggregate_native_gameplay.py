"""Audit separate actual Unity pointer-drag runs without rewriting their reports.

Usage (run only after the base 800-case run has finished):
  python aggregate_native_gameplay.py --base BASE.json --retest RETEST.json \
      --compatibility runtime-compatibility.json
Or use two finished disjoint400-case halves, both carrying the identical original
expectedCatalog800/11919 inventory:
  python aggregate_native_gameplay.py --base HALF1.json --base HALF2.json \
      --compatibility runtime-compatibility.json

Output defaults to BASE's directory/native-gameplay-coverage.json. A completed
but unsuccessful case produces passed=false and exit1. Invalid/incomplete input
or absent compatibility evidence produces exit2 and writes no coverage artifact.
The original reports are never modified. This is aggregate evidence, never a
claim that all 800 passed in a single execution.

Runtime evidence can be captured separately, without running Unity:
  python aggregate_native_gameplay.py --capture-runtime PROJECT \
      --snapshot runtime-main.json --capture-note "Exact frozen launch sources..."
Capture hashes files as they exist now. Its note must explain why those sources
represent the actual run; a hash snapshot alone cannot prove what Unity executed.
See native-gameplay-compatibility.example.json for the evidence schema.
"""

from __future__ import annotations

import argparse
from dataclasses import dataclass, field
from datetime import datetime, timezone
import hashlib
import json
import math
from pathlib import Path
import re
import sys
from typing import Any


UNITY = "2022.3.28f1"
STRATEGY = "production-pointer-drag"
LEVEL_COUNT = 800
RECORDED_STEPS = 11919
RUNTIME_DIR = "Assets/Scripts/RecoveryRuntime/"
STRICT_FILES = frozenset(RUNTIME_DIR + name for name in (
    "RecoveredNativeInteraction.cs", "RecoveredNativeDragMotion.cs",
    "RecoveredNativeSprings.cs", "RecoveredWaterNetwork.cs",
    "RecoveredWaterJets.cs", "RuntimeWaterMechanisms.cs",
    "RecoveredWaterPropagation.cs", "RecoveredWaterArrival.cs",
    "RecoveredWaterTiming.cs", "RecoveredWaterCollision.cs",
    "RecoveredWaterMotion.cs", "RecoveredNativeCollision.cs",
    "RecoveredNativeGeometry.cs", "RecoveredNativeFixedGeometry.cs",
    "RecoveredPieceConfiguration.cs", "RecoveryPiece.cs",
    "RecoveredNativeCamera.cs",
)) | frozenset((
    "Assets/Scripts/Assembly-CSharp/SwipeHandler2.cs",
    "ProjectSettings/DynamicsManager.asset", "ProjectSettings/InputManager.asset",
    "ProjectSettings/TimeManager.asset", "ProjectSettings/TagManager.asset",
))
REQUIRED_FILES = STRICT_FILES | {RUNTIME_DIR + "RecoveredFlowGame.cs"}
SHA_RE = re.compile(r"[0-9a-f]{64}\Z")


class EvidenceError(ValueError):
    """An input cannot support an honest completed-run coverage artifact."""


def ensure(condition: bool, message: str) -> None:
    if not condition:
        raise EvidenceError(message)


def no_duplicate_keys(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    result: dict[str, Any] = {}
    for key, value in pairs:
        ensure(key not in result, f"Duplicate JSON key: {key}")
        result[key] = value
    return result


def invalid_number(value: str) -> None:
    raise EvidenceError(f"Non-finite JSON number: {value}")


def read_json(path: Path) -> tuple[dict[str, Any], str]:
    data = path.read_bytes()
    document = json.loads(data.decode("utf-8-sig"), object_pairs_hook=no_duplicate_keys,
                          parse_constant=invalid_number)
    ensure(isinstance(document, dict), f"Expected JSON object: {path}")
    return document, hashlib.sha256(data).hexdigest()


def sha(value: Any, label: str) -> str:
    ensure(isinstance(value, str) and SHA_RE.fullmatch(value) is not None,
           f"Missing/invalid SHA256: {label}")
    return value


def timestamp(value: Any, label: str) -> datetime:
    ensure(isinstance(value, str) and bool(value), f"Missing timestamp: {label}")
    try:
        parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
    except ValueError as error:
        raise EvidenceError(f"Invalid timestamp {label}: {value}") from error
    ensure(parsed.tzinfo is not None, f"Timestamp lacks timezone: {label}")
    return parsed.astimezone(timezone.utc)


def integer(value: Any, minimum: int = 0) -> bool:
    return type(value) is int and value >= minimum


def evidence_file(item: dict[str, Any], parent: Path) -> tuple[Path, dict[str, Any], str]:
    ensure(isinstance(item, dict) and isinstance(item.get("path"), str),
           "Evidence item requires path and sha256")
    path = (parent / item["path"]).resolve()
    document, digest = read_json(path)
    ensure(digest == sha(item.get("sha256"), str(path)), f"Evidence hash mismatch: {path}")
    return path, document, digest


def evidence_blob(item: dict[str, Any], parent: Path) -> None:
    ensure(isinstance(item, dict) and isinstance(item.get("path"), str),
           "Evidence item requires path and sha256")
    path = (parent / item["path"]).resolve()
    digest = hashlib.sha256(path.read_bytes()).hexdigest()
    ensure(digest == sha(item.get("sha256"), str(path)), f"Evidence hash mismatch: {path}")


@dataclass
class Run:
    path: Path
    digest: str
    data: dict[str, Any]
    started: datetime
    finished: datetime
    levels: dict[int, dict[str, Any]]
    checks_by_level: dict[int, list[dict[str, Any]]] = field(default_factory=dict)

    def source(self) -> dict[str, Any]:
        return {
            "path": str(self.path), "sha256": self.digest,
            "startedUtc": self.data["startedUtc"], "finishedUtc": self.data["finishedUtc"],
            "unityVersion": UNITY, "inputStrategy": STRATEGY,
            "plannedLevels": self.data["plannedLevels"], "reportedPassed": self.data["passed"],
            "runtimeErrors": 0, "preferencesRestored": True,
            "playerReleaseStrategy": self.data.get("playerReleaseStrategy"),
            "retestSource": retest_origin(self.data.get("retestSource")),
            "serializedEmptyRetestSource": self.data.get("retestSource") is not None and retest_origin(self.data.get("retestSource")) is None,
        }


def retest_origin(value: Any) -> Any:
    # JsonUtility emits an unassigned Serializable reference as a default-valued
    # object on this Unity export. Only that exact all-empty shape means absent.
    # A partly filled or malformed provenance remains subject to strict checks.
    keys = {"path", "sha256", "startedUtc", "finishedUtc", "closedFailedIds"}
    if (isinstance(value, dict) and set(value) == keys
            and all(value[key] == "" for key in keys - {"closedFailedIds"})
            and value["closedFailedIds"] == []):
        return None
    return value


def load_run(path: Path, base: bool = False) -> Run:
    path = path.resolve()
    data, digest = read_json(path)
    started = timestamp(data.get("startedUtc"), f"{path}:startedUtc")
    finished = timestamp(data.get("finishedUtc"), f"{path}:finishedUtc")
    ensure(finished >= started, f"Run finished before it started: {path}")
    ensure(data.get("unityVersion") == UNITY, f"Wrong Unity version: {path}")
    ensure(data.get("inputStrategy") == STRATEGY, f"Not real pointer-drag input: {path}")
    ensure(data.get("runtimeErrors") == [], f"Missing/nonzero runtimeErrors: {path}")
    ensure(data.get("preferencesRestored") is True, f"Preferences not restored: {path}")
    ensure(data.get("originalTimeScale") == 4.0, f"Original timeScale4 not restored/recorded: {path}")
    ensure(type(data.get("passed")) is bool, f"Missing report passed flag: {path}")
    raw_levels = data.get("levels")
    ensure(isinstance(raw_levels, list) and len(raw_levels) > 0, f"No levels: {path}")
    ensure(integer(data.get("plannedLevels"), 1) and data["plannedLevels"] == len(raw_levels),
           f"Planned/completed count differs: {path}")
    ensure(not base or data["plannedLevels"] in (400, LEVEL_COUNT), f"Base part must contain exactly400 or800 planned levels: {path}")
    levels: dict[int, dict[str, Any]] = {}
    for level in raw_levels:
        ensure(isinstance(level, dict) and integer(level.get("id"), 1), f"Invalid level id in {path}")
        level_id = level["id"]
        ensure(level_id not in levels, f"Duplicate level{level_id} in {path}")
        ensure(level.get("passed") is True or bool(level.get("failure")),
               f"Unclosed/unfinished level{level_id} in {path}")
        levels[level_id] = level
    checks = data.get("checks")
    ensure(isinstance(checks, list) and checks, f"No recorded checks in {path}")
    global_checks = []
    checks_by_level: dict[int, list[dict[str, Any]]] = {}
    for check in checks:
        ensure(isinstance(check, dict) and isinstance(check.get("name"), str)
               and type(check.get("passed")) is bool, f"Malformed validation check: {path}")
        match = re.match(r"^gm(\d+)-", check["name"])
        if match:
            checked_id = int(match.group(1))
            ensure(checked_id in levels, f"Check for unknown level{checked_id}: {path}")
            checks_by_level.setdefault(checked_id, []).append(check)
        else:
            global_checks.append(check)
    ensure(global_checks and all(check.get("passed") is True for check in global_checks),
           f"Missing/failed global validation check: {path}")
    ensure(any(check.get("name") == "unity-version" for check in global_checks),
           f"Missing actual Unity version check: {path}")
    reported_success = (all(level.get("passed") is True and not level.get("failure") for level in levels.values())
                        and all(check["passed"] for check in checks))
    ensure(data["passed"] == reported_success, f"Finished report passed flag contradicts its recorded checks/levels: {path}")
    return Run(path, digest, data, started, finished, levels, checks_by_level)


def finite_vector(value: Any) -> bool:
    return (isinstance(value, dict) and all(type(value.get(k)) in (int, float)
            and math.isfinite(value[k]) for k in ("x", "y", "z")))


def distance_squared(a: Any, b: Any) -> float:
    if not finite_vector(a) or not finite_vector(b):
        return math.inf
    return sum((a[key] - b[key]) ** 2 for key in ("x", "y", "z"))


def audit_level(run: Run, level_id: int) -> dict[str, Any]:
    level = run.levels[level_id]
    issues: list[str] = []

    def check(condition: bool, detail: str) -> None:
        if not condition:
            issues.append(detail)

    check(level.get("passed") is True and not level.get("failure"), "Recorded level failure/not passed")
    check(level.get("normalGameplay") is True and level.get("proxies") == 0, "Not mode0 original gameplay")
    required = level.get("finalRequired")
    check(integer(required, 1), "Missing/zero final required targets")
    check(required == level.get("nativeRequired") == level.get("initialRequired"), "Native/initial/final target counts differ")
    check(integer(level.get("finalActive")) and integer(required, 1)
          and level["finalActive"] >= required, "Insufficient actual matching-color targets")
    check(level.get("complete") is True and level.get("nativeMissionComplete") is True,
          "Actual/native mission not complete")
    check(level.get("pendingWaterEvents") == 0 and level.get("waterPending") is False,
          "Water propagation pending/unknown")
    check(level.get("springsBusy") is False, "Mechanism still busy/unknown")
    check(integer(level.get("stableWetFrames"), 3) and level.get("nativePollingFrames") == 51,
          "Missing stable observation/native51 polling evidence")
    checks = run.checks_by_level.get(level_id, [])
    names = {item.get("name") for item in checks}
    check(bool(checks) and all(item.get("passed") is True for item in checks), "Missing/failed per-level checks")
    for suffix in ("production-water-propagation-api", "mode0-real-targets", "actual-water-and-completion"):
        check(f"gm{level_id}-{suffix}" in names, f"Missing check: {suffix}")
    steps = level.get("steps")
    check(isinstance(steps, list) and bool(steps), "No original recorded steps")
    steps = steps if isinstance(steps, list) else []
    input_count = marker_count = 0
    previous_moves = 0
    for index, step in enumerate(steps, 1):
        if not isinstance(step, dict):
            issues.append(f"Step{index} is not an object")
            continue
        check(step.get("number") == index, f"Non-contiguous/duplicate step number{index}")
        check(step.get("applied") is True, f"Step{index} not applied")
        check(f"gm{level_id}-player-step-{index}" in names, f"Step{index} lacks actual player-step check")
        before, after = step.get("beforeMoves"), step.get("afterMoves")
        check(integer(before) and integer(after) and before == previous_moves,
              f"Step{index} missing/nonsequential transaction count")
        if step.get("marker") is True:
            marker_count += 1
            legal_marker = step.get("exception") == "z" or (
                distance_squared(step.get("origin"), step.get("destination")) < .01
                and step.get("sameCellPickup") is False and step.get("movable") is False
                and step.get("sourceRecord") == -1 and step.get("actorIndex") == -1
                and not step.get("actor"))
            check(legal_marker and step.get("productionInput") is False and before == after,
                  f"Step{index} is not a legal z/empty-same-position marker")
        else:
            input_count += 1
            check(step.get("productionInput") is True and step.get("movable") is True
                  and integer(step.get("sourceRecord")) and integer(step.get("actorIndex"))
                  and bool(step.get("actor")), f"Step{index} lacks actual original actor input")
            check(integer(before) and integer(after) and after == before + 1,
                  f"Step{index} not one successful production transaction")
            pickup, release, committed = (step.get(k) for k in ("pickupFrame", "releaseFrame", "committedFrame"))
            check(integer(pickup, 1) and integer(release, 1) and integer(committed, 1)
                  and pickup <= release <= committed, f"Step{index} lacks ordered actual input frames")
            target = step.get("nativeLanding") if step.get("followsMovingSupport") is True else step.get("destination")
            check(distance_squared(step.get("actualAfter"), target) < .64,
                  f"Step{index} final pose fails unchanged native-validator Near(.8) bound")
        previous_moves = after if integer(after) else -1
    return {
        "source": run.source(), "passed": not issues, "reportedPassed": level.get("passed"),
        "failure": level.get("failure", ""), "auditIssues": issues,
        "normalGameplay": level.get("normalGameplay"), "nativeRequired": level.get("nativeRequired"),
        "finalRequired": required, "finalActive": level.get("finalActive"),
        "complete": level.get("complete"), "nativeMissionComplete": level.get("nativeMissionComplete"),
        "pendingWaterEvents": level.get("pendingWaterEvents"), "waterPending": level.get("waterPending"),
        "springsBusy": level.get("springsBusy"), "recordedSteps": len(steps),
        "productionInputSteps": input_count, "legalMarkerSteps": marker_count,
        "cameraSlowGestures": sum(step.get("cameraSlowGestures", 0) for step in steps if isinstance(step, dict)),
        "terminalQuickReleaseSteps": [step.get("number") for step in steps if isinstance(step, dict) and step.get("terminalQuickRelease") is True],
        "screenshot": level.get("screenshot"),
    }


def snapshot(item: dict[str, Any], parent: Path) -> tuple[Path, dict[str, Any], str]:
    path, data, digest = evidence_file(item, parent)
    ensure(data.get("schemaVersion") == 1 and isinstance(data.get("captureBasis"), str)
           and bool(data["captureBasis"].strip()), f"Runtime snapshot requires capture basis: {path}")
    timestamp(data.get("capturedUtc"), f"{path}:capturedUtc")
    files = data.get("files")
    ensure(isinstance(files, dict) and REQUIRED_FILES <= files.keys(),
           f"Runtime snapshot misses mandatory mode0 sources/settings: {path}")
    for key, digest_value in files.items():
        ensure(isinstance(key, str) and "\\" not in key and not key.startswith("/") and ".." not in key.split("/"),
               f"Snapshot requires relative normalized file paths: {path}")
        sha(digest_value, f"{path}:{key}")
    return path, data, digest


def base_parts(base: Run | list[Run]) -> list[Run]:
    result = [base] if isinstance(base, Run) else list(base)
    ensure(len(result) in (1, 2), "Use one800-case base or two400-case base parts")
    if len(result) == 1:
        ensure(result[0].data["plannedLevels"] == LEVEL_COUNT, "One400-case half is incomplete800-level evidence")
    else:
        ensure(all(run.data["plannedLevels"] == 400 for run in result), "Two base parts must each be exactly400 cases")
    ensure(len({run.digest for run in result}) == len(result), "Duplicate base report input")
    return result


def catalog(data: Any, label: str) -> dict[str, Any]:
    ensure(isinstance(data, dict), f"Missing expected original catalog: {label}")
    entries = data.get("levels")
    ensure(data.get("originalLevelCount") == LEVEL_COUNT and data.get("originalRecordedSteps") == RECORDED_STEPS
           and isinstance(entries, list) and len(entries) == LEVEL_COUNT, f"Invalid800/11919 expected catalog: {label}")
    ids, rows, total = [], [], 0
    for entry in entries:
        ensure(isinstance(entry, dict) and integer(entry.get("id"), 1)
               and integer(entry.get("recordedSteps"), 1), f"Invalid catalog row: {label}")
        layout_hash = sha(entry.get("layoutSha256"), f"{label}:layout")
        solution_hash = sha(entry.get("solutionSha256"), f"{label}:solution")
        ids.append(entry["id"])
        total += entry["recordedSteps"]
        rows.append(f"{entry['id']}|{entry['recordedSteps']}|{layout_hash}|{solution_hash}")
    ensure(ids == sorted(ids) and len(set(ids)) == LEVEL_COUNT and total == RECORDED_STEPS,
           f"Unsorted/duplicate ids or wrong original record count: {label}")
    digest = hashlib.sha256("\n".join(rows).encode("utf-8")).hexdigest()
    ensure(digest == sha(data.get("catalogSha256"), label), f"Expected catalog canonical hash mismatch: {label}")
    return data


def compatibility(path: Path, base: Run | list[Run], retests: list[Run]) -> dict[str, Any]:
    bases = base_parts(base)
    anchor = bases[0]
    path = path.resolve()
    data, digest = read_json(path)
    ensure(data.get("schemaVersion") == 1, "Unknown compatibility schema")
    bindings = data.get("runs")
    ensure(isinstance(bindings, list), "Compatibility evidence requires runs list")
    indexed = {}
    for binding in bindings:
        ensure(isinstance(binding, dict), "Invalid runtime binding")
        key = sha(binding.get("reportSha256"), "runtime report binding")
        ensure(key not in indexed, f"Duplicate runtime report binding:{key}")
        indexed[key] = binding
    all_runs = [*bases, *retests]
    expected = {run.digest for run in all_runs}
    ensure(set(indexed) == expected, "Compatibility must bind exactly these report SHA256 values")
    snapshots = {}
    sources = []
    for run in all_runs:
        bound_path, bound_data, bound_sha = snapshot(indexed[run.digest].get("runtimeSnapshot"), path.parent)
        snapshots[run.digest] = (bound_path, bound_data, bound_sha)
        sources.append({"reportSha256": run.digest, "snapshotPath": str(bound_path),
                        "snapshotSha256": bound_sha, "capturedUtc": bound_data["capturedUtc"],
                        "captureBasis": bound_data["captureBasis"]})
    approvals = data.get("approvedNonPhysicsDifferences", [])
    ensure(isinstance(approvals, list), "Invalid approved differences list")
    approval_map = {}
    for approval in approvals:
        ensure(isinstance(approval, dict) and isinstance(approval.get("path"), str), "Invalid difference approval")
        key = (approval["path"], approval.get("baseSha256"), approval.get("otherSha256"))
        ensure(key not in approval_map, f"Duplicate difference approval:{key}")
        ensure(approval["path"] not in STRICT_FILES, f"Cannot approve different production physics:{approval['path']}")
        ensure(bool(approval.get("explanation")) and approval.get("mode0Unchanged") is True,
               f"Difference requires explicit mode0 review:{approval['path']}")
        evidence = approval.get("evidence")
        ensure(isinstance(evidence, list) and evidence, f"Difference lacks review evidence:{approval['path']}")
        for evidence_item in evidence:
            evidence_blob(evidence_item, path.parent)
        for hash_key in ("baseSha256", "otherSha256"):
            if approval.get(hash_key) is not None:
                sha(approval[hash_key], f"difference {hash_key}")
        approval_map[key] = approval
    interaction = data.get("postImportInteraction")
    ensure(isinstance(interaction, dict), "Final independent Input61 evidence required")
    input_snapshot_path, input_snapshot_data, input_snapshot_sha = snapshot(interaction.get("runtimeSnapshot"), path.parent)
    actual_differences = []
    used_approvals = set()
    base_files = snapshots[anchor.digest][1]["files"]
    comparisons = [(str(run.path), run.digest, snapshots[run.digest][1]["files"]) for run in [*bases[1:], *retests]]
    comparisons.append((str(input_snapshot_path), "post-import-Input61", input_snapshot_data["files"]))
    for label, report_digest, other_files in comparisons:
        for key in sorted(base_files.keys() | other_files.keys()):
            before, after = base_files.get(key), other_files.get(key)
            if before == after:
                continue
            ensure(key not in STRICT_FILES, f"Incompatible production physics between base and {label}: {key}")
            approval_key = (key, before, after)
            ensure(approval_key in approval_map, f"Unreviewed runtime/config difference: {key} ({label})")
            used_approvals.add(approval_key)
            actual_differences.append({"reportSha256": report_digest, **approval_map[approval_key]})
    ensure(used_approvals == set(approval_map), "Unused difference approval: exact current comparisons required")
    input_path, input_data, input_digest = evidence_file(interaction.get("report"), path.parent)
    input_finished = timestamp(input_data.get("finishedUtc"), f"{input_path}:finishedUtc")
    input_started = timestamp(input_data.get("startedUtc"), f"{input_path}:startedUtc")
    ensure(input_finished >= input_started, "Input61 finished before it started")
    ensure(input_data.get("unityVersion") == UNITY and input_data.get("passed") is True
           and input_data.get("runtimeErrors") == [] and input_data.get("preferencesRestored") is True
           and input_data.get("cameraRestored") is True, "Final Input61 did not actually pass and restore state")
    input_checks = input_data.get("checks")
    ensure(isinstance(input_checks, list) and len(input_checks) >= 61
           and all(isinstance(check, dict) and check.get("passed") is True for check in input_checks), "Final Input61 needs61+ actual passing checks")
    input_names = {check.get("name") for check in input_checks}
    ensure({"native-held-pressure-undo-restores-start", "native-same-cell-return-undo",
            "pressure-plate-undo-restores-model-animation-and-collider-pose"} <= input_names,
           "Final Input61 misses independent native motion/snapshot Undo coverage")
    if retests:
        ensure(input_started >= max(run.finished for run in retests), "Input61 predates latest retest imports/run")
    return {"passed": True, "evidencePath": str(path), "sha256": digest,
            "strictIdenticalFiles": sorted(STRICT_FILES), "runSnapshots": sources,
            "explicitReviewedDifferences": actual_differences,
            "postImportInteraction": {"path": str(input_path), "sha256": input_digest,
                                      "finishedUtc": input_data["finishedUtc"], "checks": len(input_checks),
                                      "runtimeSnapshotPath": str(input_snapshot_path),
                                      "runtimeSnapshotSha256": input_snapshot_sha,
                                      "captureBasis": input_snapshot_data["captureBasis"]},
            "limitation": "Runtime hash bindings and explicit review notes are input evidence. The aggregator checks hashes and boundaries; it cannot prove semantic equivalence of an approved source diff or infer the executed assembly from current disk files."}


def aggregate(base: Run | list[Run], retests: list[Run], compat: dict[str, Any]) -> dict[str, Any]:
    bases = base_parts(base)
    initial_by_id = {}
    expected_catalog = None
    for run in bases:
        overlap = set(initial_by_id) & set(run.levels)
        ensure(not overlap, f"Duplicate/overlapping base half ids: {sorted(overlap)}")
        initial_by_id.update({level_id: run for level_id in run.levels})
        if len(bases) == 2 or run.data.get("expectedCatalog") is not None:
            inventory = catalog(run.data.get("expectedCatalog"), str(run.path))
            ensure(expected_catalog is None or inventory == expected_catalog, "Base parts have different original expected catalogs")
            expected_catalog = inventory
    ensure(len(initial_by_id) == LEVEL_COUNT, "Base union requires800 unique original level ids")
    if expected_catalog is not None:
        ensure(set(initial_by_id) == {entry["id"] for entry in expected_catalog["levels"]},
               "Base union has missing/foreign original catalog ids")
    ensure(len({run.digest for run in [*bases, *retests]}) == len(retests) + len(bases),
           "Duplicate base/retest report input")
    base_failures = {level_id for level_id, run in initial_by_id.items()
                     if run.levels[level_id].get("passed") is not True or bool(run.levels[level_id].get("failure"))}
    retests = sorted(retests, key=lambda run: (run.finished, run.started, run.digest))
    for run in retests:
        ensure(set(run.levels) <= base_failures, f"Retest adds non-base/previously-passed ids: {run.path}")
        ensure(all(run.started >= initial_by_id[level_id].started for level_id in run.levels),
               f"Retest predates its original base part: {run.path}")
        if expected_catalog is not None:
            ensure(catalog(run.data.get("expectedCatalog"), str(run.path)) == expected_catalog,
                   f"Retest changed the original catalog/data: {run.path}")
        origin = retest_origin(run.data.get("retestSource"))
        if origin is not None:
            ensure(isinstance(origin, dict), f"Invalid native failed-source provenance: {run.path}")
            sha(origin.get("sha256"), f"Retest source SHA:{run.path}")
            ensure(origin.get("startedUtc") in {known.data["startedUtc"] for known in [*bases, *retests]},
                   f"Retest source belongs to an unrelated run not included in this evidence: {run.path}")
            ensure(isinstance(origin.get("closedFailedIds"), list)
                   and len(origin["closedFailedIds"]) == len(run.levels)
                   and set(origin["closedFailedIds"]) == set(run.levels),
                   f"Retest closed failed-source ids differ: {run.path}")
    levels = []
    counts = {entry["id"]: entry["recordedSteps"] for entry in expected_catalog["levels"]} if expected_catalog else {}
    for level_id in sorted(initial_by_id):
        attempts = [audit_level(initial_by_id[level_id], level_id)]
        attempts += [audit_level(run, level_id) for run in retests if level_id in run.levels]
        selected = attempts[-1]
        if counts and selected["recordedSteps"] != counts[level_id]:
            selected["passed"] = False
            selected["auditIssues"].append(f"Selected original step count{selected['recordedSteps']} differs from catalog{counts[level_id]}")
        first = attempts[0]
        levels.append({"id": level_id, "passed": selected["passed"], "attempts": attempts,
                       "selectedSourceSha256": selected["source"]["sha256"],
                       "selected": selected,
                       "firstRunToSelected": {"firstPassed": first["passed"], "selectedPassed": selected["passed"],
                                              "firstFailure": first["failure"], "firstAuditIssues": first["auditIssues"],
                                              "firstCompletedSteps": first["recordedSteps"], "selectedCompletedSteps": selected["recordedSteps"],
                                              "firstActiveTargets": first["finalActive"], "selectedActiveTargets": selected["finalActive"],
                                              "firstPlayerReleaseStrategy": first["source"]["playerReleaseStrategy"],
                                              "selectedPlayerReleaseStrategy": selected["source"]["playerReleaseStrategy"],
                                              "firstTerminalQuickReleaseSteps": first["terminalQuickReleaseSteps"],
                                              "selectedTerminalQuickReleaseSteps": selected["terminalQuickReleaseSteps"],
                                              "selectedRequiredTargets": selected["finalRequired"]}})
    total_steps = sum(level["selected"]["recordedSteps"] for level in levels)
    unresolved = [level["id"] for level in levels if not level["passed"]]
    global_issues = []
    if total_steps != RECORDED_STEPS:
        global_issues.append(f"Selected original recorded-step total{total_steps} differs from expected{RECORDED_STEPS}")
    return {
        "schemaVersion": 1, "createdUtc": datetime.now(timezone.utc).isoformat(),
        "aggregatedVerification": True, "singleRunAllPassedClaim": False,
        "passed": not unresolved and not global_issues and compat.get("passed") is True,
        "unityVersion": UNITY, "inputStrategy": STRATEGY,
        "plannedLevels": LEVEL_COUNT, "uniqueBaseIds": len(initial_by_id),
        "basePartition": "two-disjoint400-case-runs" if len(bases) == 2 else "one800-case-run",
        "expectedCatalog": expected_catalog,
        "passedLevels": LEVEL_COUNT - len(unresolved), "failedLevels": len(unresolved),
        "unresolvedIds": unresolved, "globalAuditIssues": global_issues,
        "originalExpectedRecordedSteps": RECORDED_STEPS, "selectedRecordedSteps": total_steps,
        "baseFailedIds": sorted(base_failures),
        "retestedIds": sorted(set().union(*(set(run.levels) for run in retests))) if retests else [],
        "baseRuns": [run.source() for run in bases], "retests": [run.source() for run in retests],
        "runtimeCompatibility": compat, "levels": levels,
        "interpretation": "Separate completed actual Unity runs are combined over exactly800 original ids: one800-case run or two nonoverlapping400-case halves with identical original800/11919 data inventories. Each part remains its ids' first attempt even if its full-run finish is later than a parallel closed-case retest. Retests are applied by their finish time; a later failed retest is not hidden by an earlier pass. Each selected level must independently satisfy actual mode0 pointer input/legitimate markers, unchanged transaction/landing checks, positive original targets, native completion and no pending water/busy mechanisms. This artifact is aggregate coverage, not a single800-case all-pass execution. Original reports remain authoritative evidence and are unchanged.",
    }


def atomic_write(path: Path, document: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(path.name + ".pending")
    temporary.write_text(json.dumps(document, ensure_ascii=False, indent=2, allow_nan=False) + "\n", encoding="utf-8")
    temporary.replace(path)


def capture_runtime(project: Path, output: Path, note: str) -> None:
    project = project.resolve()
    ensure(bool(note.strip()), "Runtime snapshot needs an honest capture-note")
    candidates = list((project / "Assets/Scripts").rglob("*.cs"))
    candidates += list((project / "Assets/Resources").rglob("*.json"))
    candidates += [project / key for key in STRICT_FILES if key.startswith("ProjectSettings/")]
    files = {path.relative_to(project).as_posix(): hashlib.sha256(path.read_bytes()).hexdigest()
             for path in sorted(set(candidates))}
    ensure(REQUIRED_FILES <= files.keys(), "Project is missing required original runtime/settings sources")
    ensure(output.resolve() not in {path.resolve() for path in candidates}, "Cannot overwrite runtime source")
    atomic_write(output, {"schemaVersion": 1, "capturedUtc": datetime.now(timezone.utc).isoformat(),
                          "projectRoot": str(project), "captureBasis": note, "files": files,
                          "scope": "All Assets/Scripts C# plus Resources JSON and original physics/input/time/layer settings. This is current disk evidence; the capture basis must explain its relationship to the executed Unity snapshot. Art/prefab identity is independently covered by asset validation, not inferred by this code snapshot."})


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--base", type=Path, action="append")
    parser.add_argument("--retest", type=Path, action="append", default=[])
    parser.add_argument("--compatibility", type=Path)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--capture-runtime", type=Path, metavar="PROJECT")
    parser.add_argument("--snapshot", type=Path)
    parser.add_argument("--capture-note")
    args = parser.parse_args(argv)
    try:
        if args.capture_runtime is not None:
            ensure(args.snapshot is not None and args.capture_note is not None, "Capture needs --snapshot and --capture-note")
            ensure(args.base is None and not args.retest and args.compatibility is None and args.output is None,
                   "Runtime capture and final aggregation are separate commands")
            capture_runtime(args.capture_runtime, args.snapshot, args.capture_note)
            print(f"Runtime snapshot written: {args.snapshot.resolve()}; no gameplay coverage generated")
            return 0
        ensure(args.base is not None and args.compatibility is not None, "Aggregation needs --base and --compatibility")
        bases = [load_run(path, base=True) for path in args.base]
        retests = [load_run(path) for path in args.retest]
        runtime_evidence = compatibility(args.compatibility, bases, retests)
        result = aggregate(bases, retests, runtime_evidence)
        output = (args.output or bases[0].path.parent / "native-gameplay-coverage.json").resolve()
        source_paths = {args.compatibility.resolve(), *(run.path for run in [*bases, *retests])}
        ensure(output not in source_paths, "Coverage output cannot overwrite an input evidence/report")
        ensure(output.name == "native-gameplay-coverage.json", "Use a distinct native-gameplay-coverage.json artifact")
        atomic_write(output, result)
        print(f"Aggregate actual-input coverage {'PASS' if result['passed'] else 'FAIL'}: "
              f"{result['passedLevels']}/{LEVEL_COUNT}; unresolved={result['unresolvedIds']}; "
              f"recordedSteps={result['selectedRecordedSteps']}; {output}")
        return 0 if result["passed"] else 1
    except (EvidenceError, OSError, ValueError, TypeError, KeyError) as error:
        print(f"Invalid/incomplete evidence; no coverage artifact written: {error}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
