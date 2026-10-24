"""Offline synthetic audit boundaries only; no Unity or game proof generated."""
from pathlib import Path
import copy
import json
import tempfile
import unittest
import sys
from types import SimpleNamespace

import aggregate_native_recipe_regression as target
sys.path.insert(0, str(Path(__file__).resolve().parent / "NativeGameplayAggregateScoped5"))
from test_aggregate_native_gameplay import level, report


class RecipeCoverageTests(unittest.TestCase):
    def setUp(self):
        self.parent = Path(__file__).resolve().parent / "RecipeCoverageSyntheticTests"
        self.parent.mkdir(exist_ok=True)
        self.temp = tempfile.TemporaryDirectory(dir=self.parent)
        self.root = Path(self.temp.name)
        self.request = Path(__file__).resolve().parent / "FinalValidationPlan/native-recipe-regression-request.json"
        self.ids = json.loads(self.request.read_text(encoding="utf-8-sig"))["ids"]
        self.catalog = json.loads((Path(__file__).resolve().parent / "FinalGameplay/Half1/expected-catalog.json").read_text(encoding="utf-8-sig"))
        self.counts = {x["id"]: x["recordedSteps"] for x in self.catalog["levels"]}
        self.data = report([level(n, self.counts[n]) for n in self.ids], "2026-10-16T01:00:00Z", "2026-10-16T02:00:00Z")
        self.data["expectedCatalog"] = self.catalog

    def tearDown(self):
        self.assertTrue(self.root.resolve().is_relative_to(self.parent.resolve()))
        self.temp.cleanup()

    def write(self, name, data):
        p = self.root/name
        p.write_text(json.dumps(data), encoding="utf-8")
        return p

    def run_from(self, name, data):
        return target.audit.load_run(self.write(name, data))

    def failed_base(self):
        data = copy.deepcopy(self.data)
        data["levels"][0]["passed"] = False
        data["levels"][0]["failure"] = "SYNTHETIC explicit failure"
        data["passed"] = False
        return self.run_from("base.json", data)

    def retry(self, base, bad=False, finish="2026-10-16T04:00:00Z", name="retry.json"):
        row = level(self.ids[0], self.counts[self.ids[0]])
        if bad:
            row.update(passed=False, failure="SYNTHETIC later failure")
        data = report([row], "2026-10-16T03:00:00Z", finish)
        data["expectedCatalog"] = self.catalog
        data["retestSource"] = dict(path=str(base.path), sha256=base.digest,
            startedUtc=base.data["startedUtc"], finishedUtc=base.data["finishedUtc"], closedFailedIds=[self.ids[0]])
        return self.run_from(name, data)

    def test_prepared_request_exact169_union_passes(self):
        data, digest = target.request_evidence(self.request)
        self.assertEqual(len(data["ids"]),169)
        self.assertEqual(digest,target.REQUEST_SHA)

    def test_changed_request_bytes_rejected(self):
        data=json.loads(self.request.read_text(encoding="utf-8-sig"))
        with self.assertRaisesRegex(target.audit.EvidenceError,"request bytes"):
            target.request_evidence(self.write("request.json",data))

    def test_169_does_not_weaken_800_base_rule(self):
        p=self.write("169.json",self.data)
        self.assertEqual(len(target.audit.load_run(p).levels),169)
        with self.assertRaisesRegex(target.audit.EvidenceError,"exactly400 or800"):
            target.audit.load_run(p,base=True)

    def test_unfinished_base_rejected(self):
        self.data["finishedUtc"]=""
        with self.assertRaisesRegex(target.audit.EvidenceError,"timestamp"):
            self.run_from("unfinished.json",self.data)

    def test_closed_failed_source_exact_retest_can_resolve(self):
        base=self.failed_base();retry=self.retry(base)
        rows,failed=target.select_attempts(base,[retry],self.ids,self.catalog)
        self.assertEqual(failed,[self.ids[0]])
        self.assertTrue(all(x["passed"] for x in rows))

    def test_later_failed_attempt_is_not_hidden(self):
        base=self.failed_base();good=self.retry(base);bad=self.retry(base,True,"2026-10-16T05:00:00Z","bad.json")
        rows,_=target.select_attempts(base,[bad,good],self.ids,self.catalog)
        self.assertFalse(rows[0]["passed"])
        self.assertEqual(rows[0]["selectedSourceSha256"],bad.digest)

    def test_base_passed_id_retest_rejected(self):
        base=self.failed_base();retry=self.retry(base)
        retry.levels[self.ids[1]]=retry.levels.pop(self.ids[0])
        with self.assertRaisesRegex(target.audit.EvidenceError,"original-base-passed"):
            target.select_attempts(base,[retry],self.ids,self.catalog)

    def test_unbound_retest_source_rejected(self):
        base=self.failed_base();retry=self.retry(base)
        retry.data["retestSource"]["sha256"]="c"*64
        with self.assertRaisesRegex(target.audit.EvidenceError,"exact closed-source SHA"):
            target.select_attempts(base,[retry],self.ids,self.catalog)

    def original_step_fixture(self):
        n=self.ids[0]
        p=Path(__file__).resolve().parent.parent/("Assets/RecoveredData/LevelData/02_Solutions/so%d.json"%n)
        data=json.loads(p.read_text(encoding="utf-8-sig"))["data"]
        def point(prefix):
            return {axis:((data[prefix+axis][0]-3)*20 if axis!="y" else 3.95+(data[prefix+axis][0]-1)*7.6) for axis in ("x","y","z")}
        return SimpleNamespace(levels={n:{"steps":[dict(number=1,exception=data["excep"][0],origin=point("ori"),destination=point("des"))]}})

    def test_original_raw_hash_and_step_coordinates_bound(self):
        rows=target.original_record_evidence([self.original_step_fixture()],self.ids[:1],self.catalog)
        self.assertEqual(rows[0]["id"],self.ids[0])
        self.assertEqual(rows[0]["originalRecords"],self.counts[self.ids[0]])

    def test_changed_report_destination_rejected(self):
        run=self.original_step_fixture()
        run.levels[self.ids[0]]["steps"][0]["destination"]["x"]+=20
        with self.assertRaisesRegex(target.audit.EvidenceError,"original Save1 record"):
            target.original_record_evidence([run],self.ids[:1],self.catalog)

    def test_source_before_after_resource_change_rejected(self):
        base=self.failed_base()
        files={k:"a"*64 for k in target.audit.REQUIRED_FILES}
        files["Assets/Resources/RecoveredNativeGeometry.json"]="b"*64
        before={"schemaVersion":1,"capturedUtc":"2026-10-16T00:59:00Z","captureBasis":"SYNTHETIC offline audit test","files":files}
        after=copy.deepcopy(before);after["capturedUtc"]="2026-10-16T02:01:00Z"
        after["files"]["Assets/Resources/RecoveredNativeGeometry.json"]="c"*64
        b=self.write("before.json",before);a=self.write("after.json",after)
        job={"reportFile":"report.json","freshReports":[{"relativePath":"report.json","sha256":base.digest,
            "startedUtc":base.data["startedUtc"],"finishedUtc":base.data["finishedUtc"],"runtimeErrors":[]}],
            "exitCode":1,"changedSources":[],"beforeSnapshot":target.reference(b),"afterSnapshot":target.reference(a)}
        q=self.write("queue.json",{"jobs":[job]})
        with self.assertRaisesRegex(target.audit.EvidenceError,"before/after"):
            target.source_binding(base,{"reportSha256":base.digest,"queueEvidence":target.reference(q)},self.root)


if __name__=="__main__":
    unittest.main()
