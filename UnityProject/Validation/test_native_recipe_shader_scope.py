"""Offline shader-snapshot boundary tests, not actual Unity verification."""
import copy
import hashlib
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest

import aggregate_native_recipe_regression_v3 as target


class ShaderScopeTests(unittest.TestCase):
    def setUp(self):
        self.parent=Path(__file__).resolve().parent/"RecipeShaderSyntheticTests"
        self.parent.mkdir(exist_ok=True)
        self.temp=tempfile.TemporaryDirectory(dir=self.parent)
        self.root=Path(self.temp.name)
        self.shaders={"Assets/Shaders/SYNTHETIC-only.shader":"b"*64,
                      "Assets/Shaders/SYNTHETIC-only.cginc":"c"*64}

    def tearDown(self):
        self.assertTrue(self.root.resolve().is_relative_to(self.parent.resolve()))
        self.temp.cleanup()

    def save(self,name,data):
        p=self.root/name;p.write_text(json.dumps(data),"utf-8")
        return target.reference(p)

    def binding(self,after_shaders=None):
        report=dict(startedUtc="2026-10-16T12:00:00Z",finishedUtc="2026-10-16T12:01:00Z",passed=True)
        run=SimpleNamespace(data=report,digest="d"*64,
            started=target.audit.timestamp(report["startedUtc"],"SYNTHETIC"),
            finished=target.audit.timestamp(report["finishedUtc"],"SYNTHETIC"))
        files={k:"a"*64 for k in target.audit.REQUIRED_FILES}
        before=dict(schemaVersion=1,capturedUtc="2026-10-16T11:59:00Z",captureBasis="SYNTHETIC not a Unity run",files=files,shaderFiles=self.shaders)
        after=copy.deepcopy(before);after["capturedUtc"]="2026-10-16T12:02:00Z"
        if after_shaders is not None:after["shaderFiles"]=after_shaders
        job=dict(reportFile="SYNTHETIC.json",exitCode=0,changedSources=[],
            freshReports=[dict(relativePath="SYNTHETIC.json",sha256=run.digest,
               startedUtc=report["startedUtc"],finishedUtc=report["finishedUtc"],runtimeErrors=[])],
            beforeSnapshot=self.save("before.json",before),afterSnapshot=self.save("after.json",after))
        item=dict(reportSha256=run.digest,queueEvidence=self.save("queue.json",dict(jobs=[job])))
        return run,item

    def test_complete_identical_new_shader_snapshots_bind_actual_run(self):
        run,item=self.binding()
        locked=target.source_binding(run,item,self.root)
        self.assertEqual(locked["shaderFiles"],self.shaders)

    def test_unchanged_cs_does_not_excuse_changed_shader(self):
        after=dict(self.shaders);after["Assets/Shaders/SYNTHETIC-only.shader"]="e"*64
        run,item=self.binding(after)
        with self.assertRaisesRegex(target.audit.EvidenceError,"Shader sources before/after"):
            target.source_binding(run,item,self.root)

    def test_old_snapshot_missing_shader_capture_rejected(self):
        for snapshot in ({},{"shaderFiles":{}},{"shaderFiles":None}):
            with self.subTest(snapshot=snapshot):
                with self.assertRaisesRegex(target.audit.EvidenceError,"complete nonempty"):
                    target.shader_files(snapshot)

    def test_missing_or_removed_shader_is_changed_source(self):
        run,item=self.binding({"Assets/Shaders/SYNTHETIC-only.shader":"b"*64})
        with self.assertRaisesRegex(target.audit.EvidenceError,"Shader sources before/after"):
            target.source_binding(run,item,self.root)

    def test_non_normalized_or_non_shader_paths_rejected(self):
        for path in ("Assets/../bad.shader","/Assets/bad.shader","Assets\\bad.shader","Assets/test.cs"):
            with self.subTest(path=path):
                with self.assertRaisesRegex(target.audit.EvidenceError,"normalized"):
                    target.shader_files({"shaderFiles":{path:"b"*64}})

    def test_cannot_approve_wrong_shader_hash_as_string(self):
        with self.assertRaises(target.audit.EvidenceError):
            target.shader_files({"shaderFiles":{"Assets/bad.shader":"PENDING"}})

    def test_retests_with_different_shader_source_cannot_merge(self):
        first=dict(files={"SYNTHETIC":"a"*64},shaderFiles=self.shaders)
        second=copy.deepcopy(first);second["shaderFiles"]["Assets/Shaders/SYNTHETIC-only.shader"]="f"*64
        with self.assertRaisesRegex(target.audit.EvidenceError,"shaderFiles source dictionaries"):
            target.identical_run_sources([first,second])

    def test_cs_resources_settings_are_still_byte_identical(self):
        first=dict(files={"SYNTHETIC":"a"*64},shaderFiles=self.shaders)
        second=copy.deepcopy(first);second["files"]["SYNTHETIC"]="f"*64
        with self.assertRaisesRegex(target.audit.EvidenceError,"runtime, Resources JSON, Editor and Settings"):
            target.identical_run_sources([first,second])


if __name__=="__main__":
    unittest.main()
