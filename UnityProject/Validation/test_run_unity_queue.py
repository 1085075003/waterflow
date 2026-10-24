"""Queue boundary tests use fake processes only. They never launch Unity."""
import json
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch

import run_unity_queue as queue


class FakeUnity:
    def __init__(self, project, method, state):
        self.pid = 900000 + len(state["calls"])
        self.project, self.method, self.state = project, method, state
        self.exited = False
        state["calls"].append(self)

    def wait(self, timeout):
        index = len(self.state["calls"])
        if self.state.get("timeout"):
            raise subprocess.TimeoutExpired("FAKE_NOT_UNITY", timeout)
        root = self.project / "Validation/Reports"
        root.mkdir(parents=True, exist_ok=True)
        passed = not (self.state.get("firstFails") and index == 1)
        started = "2020-01-01T00:00:00Z" if self.state.get("stale") else queue.iso()
        document = {"startedUtc": started, "finishedUtc": queue.iso(),
                    "unityVersion": queue.VERSION, "passed": passed, "runtimeErrors": []}
        (root / "report.json").write_text(json.dumps(document), "utf-8")
        if self.state.get("changeSource"):
            (self.project / "Assets/Runtime.cs").write_text("// changed synthetic fixture", "utf-8")
        self.exited = True
        return 0 if passed else 1

    def poll(self):
        return 0 if self.exited else None


class QueueTests(unittest.TestCase):
    def setUp(self):
        self.test_root = Path(__file__).resolve().parent / "QueueRunnerSyntheticTests"
        self.test_root.mkdir(exist_ok=True)
        self.temporary = tempfile.TemporaryDirectory(dir=self.test_root)
        self.root = Path(self.temporary.name)
        self.project = self.root / "project"
        for part in ("Assets/Resources", "Packages", "ProjectSettings", "Validation"):
            (self.project / part).mkdir(parents=True)
        (self.project / "Assets/Runtime.cs").write_text("// SYNTHETIC. Never imported into Unity", "utf-8")
        (self.project / "Assets/Resources/data.json").write_text("{}", "utf-8")
        (self.project / "ProjectSettings/ProjectVersion.txt").write_text("m_EditorVersion: 2022.3.28f1\n", "utf-8")
        (self.project / "ProjectSettings/TimeManager.asset").write_text("m_TimeScale: 4\n", "utf-8")
        self.exe = self.root / "FAKE_UNITY_NOT_EXECUTED.exe"
        self.exe.write_bytes(b"SYNTHETIC-NOT-A-PROGRAM")
        self.state = {"calls": []}
        self.patches = [patch.object(queue, "MAIN_VALIDATION", self.root), patch.object(queue, "UNITY", self.exe),
                        patch.object(queue, "existing_unity", lambda project: []), patch.object(queue, "check_project_lock", lambda project: None),
                        patch.object(queue.subprocess, "Popen", self.popen)]
        for item in self.patches:
            item.start()

    def tearDown(self):
        for item in reversed(self.patches):
            item.stop()
        self.assertTrue(self.root.resolve().is_relative_to(self.test_root.resolve()))
        self.temporary.cleanup()

    def popen(self, args, **kwargs):
        self.assertTrue(all(proc.exited for proc in self.state["calls"]), "Never overlap even fake project processes")
        return FakeUnity(self.project, args[-1], self.state)

    def plan(self, count=2, continue_on_failure=False):
        document = {"continueOnFailure": continue_on_failure, "jobs": [
            {"name": "synthetic" + str(index), "method": "Synthetic.NoActualUnity.Run",
             "reportDirectory": "Validation/Reports", "reportFile": "report.json",
             "archiveDestination": "archives/job" + str(index), "timeoutSeconds": 1}
            for index in range(count)]}
        path = self.root / "queue.json"
        path.write_text(json.dumps(document), "utf-8")
        return path

    def test_serial_self_exit_fresh_reports_and_snapshots(self):
        code = queue.run_queue(self.project, self.plan(), self.root / "run")
        self.assertEqual(code, 0)
        record = json.loads((self.root / "run/queue-run.json").read_text("utf-8"))
        self.assertTrue(record["passed"])
        self.assertEqual(len(self.state["calls"]), 2)
        self.assertEqual(len(record["jobs"][0]["freshReports"]), 1)
        self.assertTrue((self.root / "archives/job0/report.json").exists())
        self.assertIn("Assets/Runtime.cs", json.loads((self.root / "run/01-synthetic0/sources-before.json").read_text("utf-8"))["files"])

    def test_stale_success_report_never_counts(self):
        self.state["stale"] = True
        self.assertEqual(queue.run_queue(self.project, self.plan(), self.root / "run"), 1)
        self.assertEqual(len(self.state["calls"]), 1)

    def test_continue_on_failure_runs_rest_without_claiming_success(self):
        self.state["firstFails"] = True
        self.assertEqual(queue.run_queue(self.project, self.plan(continue_on_failure=True), self.root / "run"), 1)
        self.assertEqual(len(self.state["calls"]), 2)

    def test_timeout_does_not_kill_or_launch_next_job(self):
        self.state["timeout"] = True
        self.assertEqual(queue.run_queue(self.project, self.plan(continue_on_failure=True), self.root / "run"), 3)
        self.assertEqual(len(self.state["calls"]), 1)
        self.assertFalse(self.state["calls"][0].exited)
        self.assertTrue(list((self.root / "UnityQueueLocks").glob("*.json")))

    def test_source_change_cannot_be_a_passing_frozen_snapshot(self):
        self.state["changeSource"] = True
        self.assertEqual(queue.run_queue(self.project, self.plan(1), self.root / "run"), 1)
        record = json.loads((self.root / "run/queue-run.json").read_text("utf-8"))
        self.assertEqual(record["jobs"][0]["changedSources"], ["Assets/Runtime.cs"])

    def test_nested_archive_is_rejected_before_launch(self):
        path = self.plan(1)
        data = json.loads(path.read_text("utf-8"))
        data["jobs"][0]["archiveDestination"] = str(self.project / "Validation/Reports/archive")
        path.write_text(json.dumps(data), "utf-8")
        with self.assertRaisesRegex(queue.QueueError, "Nested"):
            queue.run_queue(self.project, path, self.root / "run")
        self.assertEqual(self.state["calls"], [])

    def test_dry_run_never_invokes_process(self):
        self.assertEqual(queue.run_queue(self.project, self.plan(), self.root / "run", dry_run=True), 0)
        self.assertEqual(self.state["calls"], [])


if __name__ == "__main__":
    unittest.main()
