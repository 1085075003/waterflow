"""Synthetic boundary tests for the aggregator; these are NOT Unity game proof."""

import copy
import hashlib
import json
from pathlib import Path
import tempfile
import unittest

import aggregate_native_gameplay as audit


def point(x=0, y=3.95, z=0):
    return {"x": x, "y": y, "z": z}


def level(level_id, count=15):
    steps = []
    for number in range(1, count + 1):
        steps.append({"number": number, "exception": "", "sourceRecord": 0, "actorIndex": 0,
                      "actor": "synthetic-boundary-fixture-not-a-Unity-level", "applied": True,
                      "marker": False, "productionInput": True, "movable": True,
                      "beforeMoves": number - 1, "afterMoves": number,
                      "pickupFrame": number * 10, "releaseFrame": number * 10 + 1,
                      "committedFrame": number * 10 + 2, "destination": point(),
                      "origin": point(20), "actualAfter": point(), "nativeLanding": point(),
                      "followsMovingSupport": False, "cameraSlowGestures": 0})
    return {"id": level_id, "passed": True, "failure": "", "normalGameplay": True,
            "proxies": 0, "nativeRequired": 2, "initialRequired": 2, "finalRequired": 2,
            "finalActive": 2, "complete": True, "nativeMissionComplete": True,
            "pendingWaterEvents": 0, "waterPending": False, "springsBusy": False,
            "stableWetFrames": 3, "nativePollingFrames": 51, "steps": steps}


def report(levels, start="2026-10-16T01:00:00Z", finish="2026-10-16T05:00:00Z"):
    checks = [{"name": "unity-version", "passed": True}]
    for item in levels:
        suffixes = ["production-water-propagation-api", "mode0-real-targets", "actual-water-and-completion"]
        suffixes += [f"player-step-{step['number']}" for step in item["steps"]]
        checks += [{"name": f"gm{item['id']}-{suffix}", "passed": not bool(item["failure"])} for suffix in suffixes]
    return {"startedUtc": start, "finishedUtc": finish, "unityVersion": audit.UNITY,
            "inputStrategy": audit.STRATEGY, "plannedLevels": len(levels), "runtimeErrors": [],
            "passed": all(item["passed"] for item in levels), "preferencesRestored": True,
            "originalTimeScale": 4.0, "checks": checks, "levels": levels}


class AggregateTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        # 719*15 +81*14 = the real catalog's11919 expected records, using
        # explicitly SYNTHETIC fixtures solely to exercise audit boundaries.
        cls.template = report([level(i, 15 if i <= 719 else 14) for i in range(1, 801)])

    def setUp(self):
        self.test_root = Path(__file__).resolve().parent / "AggregateSyntheticTests"
        self.test_root.mkdir(exist_ok=True)
        self.temp = tempfile.TemporaryDirectory(dir=self.test_root)
        self.root = Path(self.temp.name)

    def tearDown(self):
        self.assertTrue(self.root.resolve().is_relative_to(self.test_root.resolve()))
        self.temp.cleanup()

    def write(self, name, document):
        path = self.root / name
        path.write_text(json.dumps(document), "utf-8")
        return path

    def run_from(self, name, document, base=False):
        return audit.load_run(self.write(name, document), base=base)

    def failed_base(self):
        data = copy.deepcopy(self.template)
        data["levels"][0]["passed"] = False
        data["levels"][0]["failure"] = "Synthetic first-view failure; NOT real Unity evidence"
        data["passed"] = False
        return self.run_from("base.json", data, True)

    def retest(self, item=None, name="retest.json", finish="2026-10-16T03:00:00Z"):
        return self.run_from(name, report([item or level(1)], "2026-10-16T02:00:00Z", finish))

    def compat(self, base, retests, change=None, approved=False):
        bases = [base] if isinstance(base, audit.Run) else base
        files = {key: "a" * 64 for key in audit.REQUIRED_FILES}
        base_snapshot = {"schemaVersion": 1, "capturedUtc": "2026-10-16T01:00:00Z",
                         "captureBasis": "SYNTHETIC aggregator test; not executed Unity source", "files": files}
        base_path = self.write("runtime-main.json", base_snapshot)
        other_snapshot = copy.deepcopy(base_snapshot)
        if change:
            other_snapshot["files"][change] = "b" * 64
        other_path = self.write("runtime-other.json", other_snapshot)

        def ref(path):
            return {"path": path.name, "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}

        input_names = ["native-held-pressure-undo-restores-start", "native-same-cell-return-undo",
                       "pressure-plate-undo-restores-model-animation-and-collider-pose"]
        input_names += ["synthetic-input-check-" + str(i) for i in range(58)]
        input_path = self.write("input61.json", {"startedUtc": "2026-10-16T06:00:00Z",
            "finishedUtc": "2026-10-16T06:01:00Z", "unityVersion": audit.UNITY,
            "passed": True, "runtimeErrors": [], "preferencesRestored": True, "cameraRestored": True,
            "checks": [{"name": name, "passed": True} for name in input_names]})
        data = {"schemaVersion": 1, "runs": [{"reportSha256": bases[0].digest, "runtimeSnapshot": ref(base_path)}]
                + [{"reportSha256": run.digest, "runtimeSnapshot": ref(other_path)} for run in bases[1:]]
                + [{"reportSha256": run.digest, "runtimeSnapshot": ref(other_path)} for run in retests],
                "approvedNonPhysicsDifferences": [],
                "postImportInteraction": {"report": ref(input_path), "runtimeSnapshot": ref(other_path)}}
        if approved:
            proof = self.write("review.json", {"scope": "synthetic preview-only review fixture"})
            data["approvedNonPhysicsDifferences"].append({"path": change, "baseSha256": "a" * 64,
                "otherSha256": "b" * 64, "mode0Unchanged": True,
                "explanation": "Synthetic explicit boundary approval, not production review", "evidence": [ref(proof)]})
        return audit.compatibility(self.write("compat.json", data), base, retests)

    def test_finished_retest_overrides_case_before_later_global_base_finish(self):
        base, retry = self.failed_base(), self.retest()
        result = audit.aggregate(base, [retry], self.compat(base, [retry]))
        self.assertTrue(result["passed"])
        self.assertTrue(result["aggregatedVerification"])
        self.assertFalse(result["singleRunAllPassedClaim"])
        self.assertEqual(result["selectedRecordedSteps"], 11919)
        self.assertIn("first-view failure", result["levels"][0]["attempts"][0]["failure"])
        self.assertEqual(result["levels"][0]["selectedSourceSha256"], retry.digest)

    def test_unfinished_base_is_not_coverage(self):
        data = copy.deepcopy(self.template)
        data["finishedUtc"] = ""
        with self.assertRaisesRegex(audit.EvidenceError, "timestamp"):
            self.run_from("unfinished.json", data, True)

    def test_instant_api_strategy_is_rejected(self):
        data = copy.deepcopy(self.template)
        data["inputStrategy"] = "instant-TryMove-baseline"
        with self.assertRaisesRegex(audit.EvidenceError, "pointer-drag"):
            self.run_from("instant.json", data, True)

    def test_zero_targets_cannot_turn_reported_success_into_coverage(self):
        base = self.failed_base()
        item = level(1)
        item.update(nativeRequired=0, initialRequired=0, finalRequired=0, finalActive=0)
        retry = self.retest(item)
        result = audit.aggregate(base, [retry], self.compat(base, [retry]))
        self.assertFalse(result["passed"])
        self.assertEqual(result["unresolvedIds"], [1])

    def test_changed_springs_are_never_silently_merged_or_whitelisted(self):
        base, retry = self.failed_base(), self.retest()
        with self.assertRaisesRegex(audit.EvidenceError, "production physics"):
            self.compat(base, [retry], audit.RUNTIME_DIR + "RecoveredNativeSprings.cs")

    def test_core_diff_needs_exact_explicit_review(self):
        base, retry = self.failed_base(), self.retest()
        core = audit.RUNTIME_DIR + "RecoveredFlowGame.cs"
        with self.assertRaisesRegex(audit.EvidenceError, "Unreviewed"):
            self.compat(base, [retry], core)
        evidence = self.compat(base, [retry], core, approved=True)
        self.assertTrue(evidence["passed"])
        self.assertEqual(len(evidence["explicitReviewedDifferences"]), 2)

    def test_newer_failed_retest_is_not_hidden_by_older_success(self):
        base, good = self.failed_base(), self.retest()
        item = level(1)
        item.update(passed=False, failure="Synthetic later failure")
        bad = self.retest(item, "later.json", "2026-10-16T04:00:00Z")
        result = audit.aggregate(base, [bad, good], self.compat(base, [bad, good]))
        self.assertFalse(result["passed"])
        self.assertEqual(result["levels"][0]["selectedSourceSha256"], bad.digest)

    def test_fake_non_input_move_does_not_count(self):
        base = self.failed_base()
        item = level(1)
        item["steps"][0]["productionInput"] = False
        retry = self.retest(item)
        result = audit.aggregate(base, [retry], self.compat(base, [retry]))
        self.assertFalse(result["passed"])
        self.assertIn("lacks actual original actor input", str(result["levels"][0]["selected"]["auditIssues"]))

    def test_retest_cannot_add_or_replace_unfailed_base_id(self):
        base = self.failed_base()
        retry = self.retest(level(2))
        with self.assertRaisesRegex(audit.EvidenceError, "non-base/previously-passed"):
            audit.aggregate(base, [retry], self.compat(base, [retry]))

    def test_duplicate_base_ids_rejected(self):
        data = copy.deepcopy(self.template)
        data["levels"][1]["id"] = data["levels"][0]["id"]
        with self.assertRaisesRegex(audit.EvidenceError, "Duplicate level"):
            self.run_from("duplicate.json", data, True)

    def test_truncated_passed_case_cannot_hide_missing_original_records(self):
        base = self.failed_base()
        retry = self.retest(level(1, 14))
        result = audit.aggregate(base, [retry], self.compat(base, [retry]))
        self.assertFalse(result["passed"])
        self.assertEqual(result["selectedRecordedSteps"], 11918)
        self.assertTrue(result["globalAuditIssues"])

    def test_marker_cannot_hide_an_ordinary_move(self):
        base = self.failed_base()
        item = level(1)
        item["steps"][0].update(marker=True, productionInput=False)
        retry = self.retest(item)
        result = audit.aggregate(base, [retry], self.compat(base, [retry]))
        self.assertFalse(result["passed"])
        self.assertIn("not a legal", str(result["levels"][0]["selected"]["auditIssues"]))

    def inventory(self):
        entries = [{"id": i, "recordedSteps": 15 if i <= 719 else 14,
                    "layoutSha256": "a" * 64, "solutionSha256": "b" * 64} for i in range(1, 801)]
        canonical = "\n".join(f"{x['id']}|{x['recordedSteps']}|{x['layoutSha256']}|{x['solutionSha256']}" for x in entries)
        return {"originalLevelCount": 800, "originalRecordedSteps": 11919,
                "catalogSha256": hashlib.sha256(canonical.encode()).hexdigest(), "levels": entries}

    def halves(self, overlapping=False):
        first = report(copy.deepcopy(self.template["levels"][:400]))
        second = report(copy.deepcopy(self.template["levels"][380:780] if overlapping else self.template["levels"][400:]),
                        "2026-10-16T01:01:00Z")
        first["expectedCatalog"] = self.inventory()
        second["expectedCatalog"] = self.inventory()
        return [self.run_from("half1.json", first, True), self.run_from("half2.json", second, True)]

    def test_two_disjoint_finished_halves_cover_exact_original_catalog(self):
        bases = self.halves()
        result = audit.aggregate(bases, [], self.compat(bases, []))
        self.assertTrue(result["passed"])
        self.assertEqual(result["basePartition"], "two-disjoint400-case-runs")
        self.assertEqual(len(result["baseRuns"]), 2)
        self.assertEqual(result["uniqueBaseIds"], 800)

    def test_one_half_cannot_claim_full_coverage(self):
        base = self.halves()[0]
        with self.assertRaisesRegex(audit.EvidenceError, "incomplete"):
            audit.aggregate(base, [], {"passed": True})

    def test_two_halves_with_overlap_are_rejected(self):
        bases = self.halves(overlapping=True)
        with self.assertRaisesRegex(audit.EvidenceError, "overlapping"):
            audit.aggregate(bases, [], {"passed": True})

    def test_corrupted_half_inventory_hash_is_rejected(self):
        bases = self.halves()
        bases[1].data["expectedCatalog"]["levels"][0]["solutionSha256"] = "c" * 64
        with self.assertRaisesRegex(audit.EvidenceError, "canonical hash mismatch"):
            audit.aggregate(bases, [], {"passed": True})

    def test_physics_mismatch_between_base_halves_is_rejected(self):
        bases = self.halves()
        with self.assertRaisesRegex(audit.EvidenceError, "production physics"):
            self.compat(bases, [], audit.RUNTIME_DIR + "RecoveredNativeDragMotion.cs")

    def test_exact_Unity_empty_serialized_provenance_is_absent_not_a_false_claim(self):
        base, retry = self.failed_base(), self.retest()
        retry.data["retestSource"] = {"path": "", "sha256": "", "startedUtc": "", "finishedUtc": "", "closedFailedIds": []}
        result = audit.aggregate(base, [retry], self.compat(base, [retry]))
        self.assertTrue(result["passed"])
        self.assertIsNone(result["retests"][0]["retestSource"])
        self.assertTrue(result["retests"][0]["serializedEmptyRetestSource"])

    def test_partly_filled_invalid_provenance_is_still_rejected(self):
        base, retry = self.failed_base(), self.retest()
        retry.data["retestSource"] = {"path": "claimed-source.json", "sha256": "", "startedUtc": "", "finishedUtc": "", "closedFailedIds": []}
        with self.assertRaisesRegex(audit.EvidenceError, "SHA256"):
            audit.aggregate(base, [retry], self.compat(base, [retry]))

    def test_legal_quick_strategy_is_disclosed_in_selected_difference(self):
        base, retry = self.failed_base(), self.retest()
        retry.data["playerReleaseStrategy"] = "settled-hover-prefix-and-quick-terminal-ordinary-guide"
        retry.levels[1]["steps"][-1]["terminalQuickRelease"] = True
        result = audit.aggregate(base, [retry], self.compat(base, [retry]))
        self.assertTrue(result["passed"])
        difference = result["levels"][0]["firstRunToSelected"]
        self.assertEqual(difference["selectedTerminalQuickReleaseSteps"], [15])
        self.assertIn("quick-terminal", difference["selectedPlayerReleaseStrategy"])


class ExactUVTests(unittest.TestCase):
    """Offline evidence boundaries; these are not extra Unity game tests."""
    def setUp(self):
        if not hasattr(AggregateTests, "template"):
            AggregateTests.setUpClass()
        self.helper = AggregateTests()
        self.helper.setUp()
        self.root = self.helper.root
        directory = Path(__file__).resolve().parent
        self.approval = json.loads((directory / "exact-uv-approval.json").read_text("utf-8"))
        for key in ("baseSource", "otherSource", "fullDiff", "nativeStartAsm", "nativeUpdateAsm", "actualUvValidation"):
            self.approval[key]["path"] = str((directory / self.approval[key]["path"]).resolve())

    def tearDown(self):
        self.helper.tearDown()

    def mutate(self, key, append):
        original = Path(self.approval[key]["path"])
        path = self.root / (key + original.suffix)
        path.write_bytes(original.read_bytes() + append)
        self.approval[key] = {"path": str(path), "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}

    def evidence(self, approved=True):
        base = self.helper.failed_base()
        self.helper.compat(base, [])
        path = self.root / "compat.json"
        data = json.loads(path.read_text("utf-8"))
        for filename, expected in (("runtime-main.json", audit.UV_BASE_SHA), ("runtime-other.json", audit.UV_OTHER_SHA)):
            snap = self.root / filename
            document = json.loads(snap.read_text("utf-8"))
            document["files"][audit.UV_PATH] = expected
            snap.write_text(json.dumps(document), "utf-8")
        data["runs"][0]["runtimeSnapshot"]["sha256"] = hashlib.sha256((self.root / "runtime-main.json").read_bytes()).hexdigest()
        data["postImportInteraction"]["runtimeSnapshot"]["sha256"] = hashlib.sha256((self.root / "runtime-other.json").read_bytes()).hexdigest()
        if approved:
            data["exactVisualScrollDifferences"] = [copy.deepcopy(self.approval)]
        path.write_text(json.dumps(data), "utf-8")
        return path, base

    def test_actual_uv_complete_hunk_and_formula_evidence_pass(self):
        proof = audit.exact_visual_scroll_approval(self.approval, self.root)
        self.assertEqual(proof["actualChecks"], 436)
        self.assertEqual(proof["actualPostRenderSamples"], 420)
        self.assertLess(proof["independentFormulaMaximumError"], 1e-6)

    def test_watermotion_still_strict_without_exact_evidence(self):
        path, base = self.evidence(False)
        self.assertIn(audit.UV_PATH, audit.STRICT_FILES)
        with self.assertRaisesRegex(audit.EvidenceError, "production physics"):
            audit.compatibility(path, base, [])

    def test_special_rule_only_exact_pair_exception(self):
        path, base = self.evidence()
        proof = audit.compatibility(path, base, [])
        self.assertIn(audit.UV_PATH, proof["strictDefaultFiles"])
        self.assertNotIn(audit.UV_PATH, proof["strictIdenticalFiles"])
        self.assertEqual(len(proof["exactVisualScrollExceptions"]), 1)

    def test_extra_watermotion_code_rejected(self):
        self.mutate("otherSource", b"\n// unreviewed edit\n")
        with self.assertRaisesRegex(audit.EvidenceError, "reviewed evidence"):
            audit.exact_visual_scroll_approval(self.approval, self.root)

    def test_other_hash_pair_rejected(self):
        self.approval["otherSha256"] = "b" * 64
        with self.assertRaisesRegex(audit.EvidenceError, "reviewed WaterMotion source pair"):
            audit.exact_visual_scroll_approval(self.approval, self.root)

    def test_incomplete_full_diff_rejected(self):
        self.mutate("fullDiff", b"\n")
        with self.assertRaisesRegex(audit.EvidenceError, "full diff"):
            audit.exact_visual_scroll_approval(self.approval, self.root)

    def test_different_native_asm_rejected(self):
        self.mutate("nativeUpdateAsm", b"\n// different evidence\n")
        with self.assertRaisesRegex(audit.EvidenceError, "reviewed evidence"):
            audit.exact_visual_scroll_approval(self.approval, self.root)

    def test_other_actual_uv_report_cannot_substitute(self):
        self.mutate("actualUvValidation", b"\n")
        with self.assertRaisesRegex(audit.EvidenceError, "reviewed actual passed Unity fixture"):
            audit.exact_visual_scroll_approval(self.approval, self.root)

    def test_input61_before_actual_uv_fix_rejected(self):
        path, base = self.evidence()
        data = json.loads(path.read_text("utf-8"))
        input_path = self.root / "input61.json"
        report = json.loads(input_path.read_text("utf-8"))
        report.update(startedUtc="2026-10-16T05:00:00Z", finishedUtc="2026-10-16T05:01:00Z")
        input_path.write_text(json.dumps(report), "utf-8")
        data["postImportInteraction"]["report"]["sha256"] = hashlib.sha256(input_path.read_bytes()).hexdigest()
        path.write_text(json.dumps(data), "utf-8")
        with self.assertRaisesRegex(audit.EvidenceError, "predates exact UV"):
            audit.compatibility(path, base, [])

    def test_generic_nonphysics_approval_cannot_whitelist_watermotion(self):
        base = self.helper.failed_base()
        with self.assertRaisesRegex(audit.EvidenceError, "Cannot approve different production physics"):
            self.helper.compat(base, [], audit.UV_PATH, approved=True)


if __name__ == "__main__":
    unittest.main()
