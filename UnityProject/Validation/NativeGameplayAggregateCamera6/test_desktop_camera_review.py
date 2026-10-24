"""Offline rejection boundaries; synthetic reports are not actual Unity proof."""
import copy
from pathlib import Path
import hashlib
import itertools
import json
import tempfile
import unittest
from unittest.mock import patch
from datetime import datetime, timezone

import aggregate_native_gameplay as audit
import desktop_camera_review as camera
import test_aggregate_native_gameplay as fixtures


class DesktopCameraTests(unittest.TestCase):
    def setUp(self):
        directory=Path(__file__).resolve().parent
        self.approval=json.loads((directory/"desktop-camera-proof.template.json").read_text("utf-8"))
        self.parent=directory/"DesktopCameraSyntheticTests"
        self.parent.mkdir(exist_ok=True)
        self.temp=tempfile.TemporaryDirectory(dir=self.parent)
        self.root=Path(self.temp.name)

    def tearDown(self):
        self.assertTrue(self.root.resolve().is_relative_to(self.parent.resolve()))
        self.temp.cleanup()

    def test_exact_source_pair_explicitly_changes_pointer_presentation(self):
        proof=camera.source_pair(self.approval,self.root,audit)
        self.assertTrue(proof["physicsUnchanged"])
        self.assertTrue(proof["pointerPresentationChanged"])
        self.assertFalse(proof["mode0Unchanged"])

    def test_cannot_call_changed_camera_mode0_unchanged(self):
        self.approval["mode0Unchanged"]=True
        with self.assertRaisesRegex(audit.EvidenceError,"explicitly disclose"):
            camera.source_pair(self.approval,self.root,audit)

    def test_native_camera_does_not_fit_desktop_exception(self):
        self.approval["path"]=audit.RUNTIME_DIR+"RecoveredNativeCamera.cs"
        with self.assertRaisesRegex(audit.EvidenceError,"exact reviewed"):
            camera.source_pair(self.approval,self.root,audit)
        self.assertIn(audit.RUNTIME_DIR+"RecoveredNativeCamera.cs",audit.STRICT_FILES)
        self.assertIn("Assets/Scripts/Assembly-CSharp/SwipeHandler2.cs",audit.STRICT_FILES)
        self.assertIn(audit.RUNTIME_DIR+"RecoveredNativeInteraction.cs",audit.STRICT_FILES)

    def test_other_framing_source_hash_rejected(self):
        p=self.root/"other.cs"
        p.write_bytes(Path(self.approval["otherSource"]["path"]).read_bytes()+b"\n// extra change\n")
        self.approval["otherSource"]={"path":str(p),"sha256":hashlib.sha256(p.read_bytes()).hexdigest()}
        with self.assertRaisesRegex(audit.EvidenceError,"pinned reviewed"):
            camera.source_pair(self.approval,self.root,audit)

    def test_source_only_review_cannot_generate_actual_coverage(self):
        self.approval["camera17"]={}
        with self.assertRaisesRegex(audit.EvidenceError,"Evidence item"):
            camera.verify(self.approval,self.root,audit,{})

    def test_generic_mode0_approval_cannot_waive_pointer_change(self):
        if not hasattr(fixtures.AggregateTests,"template"):
            fixtures.AggregateTests.setUpClass()
        helper=fixtures.AggregateTests();helper.setUp()
        try:
            with self.assertRaisesRegex(audit.EvidenceError,"typed disclosure"):
                helper.compat(helper.failed_base(),[],camera.PATH,approved=True)
        finally:
            helper.tearDown()

    def queued_fixture(self, old_view=False, changed_after=False):
        def save(name,data):
            p=self.root/name;p.write_text(json.dumps(data),encoding="utf-8")
            return {"path":str(p),"sha256":hashlib.sha256(p.read_bytes()).hexdigest()}
        report=dict(startedUtc="2026-10-16T11:00:00Z",finishedUtc="2026-10-16T11:01:00Z",
                    unityVersion=audit.UNITY,passed=True,errors=[])
        report_ref=save("SYNTHETIC-report.json",report)
        files={path:"a"*64 for path in audit.REQUIRED_FILES}
        files[camera.PATH]=camera.BASE if old_view else camera.OTHER
        before=dict(schemaVersion=1,capturedUtc="2026-10-16T10:59:00Z",captureBasis="SYNTHETIC offline test",files=files)
        after=copy.deepcopy(before);after["capturedUtc"]="2026-10-16T11:02:00Z"
        if changed_after:after["files"]["ProjectSettings/DynamicsManager.asset"]="b"*64
        job=dict(reportFile="SYNTHETIC-report.json",exitCode=0,changedSources=[],
                 beforeSnapshot=save("before.json",before),afterSnapshot=save("after.json",after),
                 freshReports=[dict(relativePath="SYNTHETIC-report.json",sha256=report_ref["sha256"],
                     startedUtc=report["startedUtc"],finishedUtc=report["finishedUtc"])])
        return dict(report=report_ref,queueEvidence=save("queue.json",dict(jobs=[job])))

    def test_old_full_capture_cannot_claim_new_camera_proof(self):
        with self.assertRaisesRegex(audit.EvidenceError,"old/unreviewed"):
            camera.actual_report(self.queued_fixture(old_view=True),self.root,audit)

    def test_before_after_physics_change_in_actual_queue_rejected(self):
        with self.assertRaisesRegex(audit.EvidenceError,"source changed"):
            camera.actual_report(self.queued_fixture(changed_after=True),self.root,audit)

    def test_queue_source_identity_must_bind_fresh_primary_report(self):
        item=self.queued_fixture()
        proof=camera.actual_report(item,self.root,audit)
        self.assertEqual(proof["reportSha256"],item["report"]["sha256"])
        self.assertEqual(proof["files"][camera.PATH],camera.OTHER)

    def hud_fixture(self):
        checks=[];frames=[]
        for level,stage,size in itertools.product((366,409,421,401),("initial","solution"),((1280,720),(885,630),(2560,1080))):
            tag="%d:%s:%dx%d"%(level,stage,*size)
            names=["actual-original-HUD-rectangles","original-projection-and-rotation-retained","all-structural-vertices-in-camera","structural-meshes-clear-original-HUD","protected-HUD-covers-actual-rendered-rects"]
            if level==401:names.append("clear-small-level-no-unneeded-retreat")
            checks.extend(dict(name=tag+":"+n,passed=True) for n in names)
            frames.append(dict(level=level,stage=stage,width=size[0],height=size[1],structuralVertices=8,structuralMeshes=1,
                outsideVertices=0,hudOverlapMeshes=0,overlaps=[],protectedHud=[{"path":"SYNTHETIC original control","viewport":dict(x=.1,y=.8,width=.1,height=.1)}],
                actualHudProjection=[dict(path="SYNTHETIC original control",covered=True,actualViewport=dict(x=.11,y=.81,width=.08,height=.08),protectedViewport=dict(x=.1,y=.8,width=.1,height=.1))],
                minStructuralDepth=200,maxStructuralDepth=400,
                fov=26,nearClip=20,farClip=1000,backwardDistance=0))
        for level in (366,409,421,401):
            for suffix in ("active-original-level-title","native-global-asset-identity","original-positive-local-level-number"):
                checks.append(dict(name=str(level)+":initial-before-capture:"+suffix,passed=True))
            for stage in (0,1):checks.append(dict(name=str(level)+":"+str(stage)+":resize-returns-to-wide-fit",passed=True))
        return dict(runtimeSourceSha256=camera.OTHER,preferencesRestored=True,enteredEditMode=True,timeScaleRestored=True,
                    originalTimeScale=4,restoredTimeScale=4,frames=frames,checks=checks)

    def test_hud_requires_exact24_case_stage_aspect_frames(self):
        report=self.hud_fixture();camera.hud24(report,audit)
        report["frames"][-1]=copy.deepcopy(report["frames"][0])
        with self.assertRaisesRegex(audit.EvidenceError,"duplicate/missing"):
            camera.hud24(report,audit)

    def test_real_hud_overlap_is_rejected(self):
        report=self.hud_fixture();report["frames"][0]["hudOverlapMeshes"]=1
        with self.assertRaisesRegex(audit.EvidenceError,"real overlap"):
            camera.hud24(report,audit)

    def test_original_projection_or_time_restore_omission_rejected(self):
        report=self.hud_fixture();report["timeScaleRestored"]=False
        with self.assertRaisesRegex(audit.EvidenceError,"restore"):
            camera.hud24(report,audit)

    def test_actual_hud_projection_cannot_be_smaller_than_rendered_artwork(self):
        report=self.hud_fixture()
        report["frames"][0]["actualHudProjection"][0]["actualViewport"]["width"]=.4
        with self.assertRaisesRegex(audit.EvidenceError,"contain actual"):
            camera.hud24(report,audit)

    def test_original_near_far_depth_is_not_replaced_by_xy_visibility(self):
        report=self.hud_fixture();report["frames"][0]["maxStructuralDepth"]=1001
        with self.assertRaisesRegex(audit.EvidenceError,"structural depth"):
            camera.hud24(report,audit)

    def test_prefix_only_or_refreshed_title_is_insufficient(self):
        report=self.hud_fixture()
        report["checks"]=[x for x in report["checks"] if x["name"]!="366:initial-before-capture:original-positive-local-level-number"]
        with self.assertRaisesRegex(audit.EvidenceError,"actual positive native title"):
            camera.hud24(report,audit)

    def test_final_input_must_follow_all_actual_new_camera_regressions(self):
        # Isolate the temporal gate. Other tests exercise real SHA/queue parsing;
        # these controlled values are deliberately not valid coverage evidence.
        def instant(hour):return datetime(2026,10,16,hour,tzinfo=timezone.utc)
        files={x:"a"*64 for x in audit.STRICT_FILES}
        def actual(finish,start=10):
            return dict(report={},reportPath=self.root/"SYNTHETIC",reportSha256="0"*64,
                started=instant(start),finished=instant(finish),files=files,
                evidence={"beforeSnapshot":{"sha256":"d"*64}})
        before=actual(11);before["files"]=dict(files,**{"Assets/Editor/RecoveredTools/FlowWaterHudFramingValidation.cs":camera.HUD_EDITOR})
        gameplay=dict(sourceFinishedUtc="2026-10-16T12:00:00Z",comparisons=[])
        with patch.object(camera,"source_pair",return_value={}), \
             patch.object(camera,"actual_report",side_effect=[before,before,before,actual(11)]), \
             patch.object(camera,"camera17"),patch.object(camera,"hud24"),patch.object(camera,"meshes1350"), \
             patch.object(camera,"affected169",return_value=gameplay), \
             patch.object(audit,"snapshot",return_value=(self.root/"SYNTHETIC",{"files":files},"d"*64)):
            with self.assertRaisesRegex(audit.EvidenceError,"predates"):
                camera.verify({},self.root,audit,{})

    def test_draft_cannot_approve_known_unresolved_background_visibility(self):
        self.assertFalse(camera.FINAL_BACKGROUND_VISIBILITY_REVIEWED)
        def instant(hour):return datetime(2026,10,16,hour,tzinfo=timezone.utc)
        files={x:"a"*64 for x in audit.STRICT_FILES}
        before=dict(report={},reportPath=self.root/"SYNTHETIC",reportSha256="0"*64,
                    started=instant(10),finished=instant(11),
                    files=dict(files,**{"Assets/Editor/RecoveredTools/FlowWaterHudFramingValidation.cs":camera.HUD_EDITOR}),
                    evidence={"beforeSnapshot":{"sha256":"d"*64}})
        later=dict(before,started=instant(13),finished=instant(14),files=files)
        with patch.object(camera,"source_pair",return_value={}), \
             patch.object(camera,"actual_report",side_effect=[before,before,before,later]), \
             patch.object(camera,"camera17"),patch.object(camera,"hud24"),patch.object(camera,"meshes1350"), \
             patch.object(camera,"affected169",return_value=dict(sourceFinishedUtc="2026-10-16T12:00:00Z",comparisons=[])), \
             patch.object(audit,"snapshot",return_value=(self.root/"SYNTHETIC",{"files":files},"d"*64)):
            with self.assertRaisesRegex(audit.EvidenceError,"background visibility"):
                camera.verify({},self.root,audit,{})

    def test_full1350_cannot_reuse_partial_or_cropped_capture(self):
        row=dict(originalPresentation=True,structuralVerticesChecked=8,geometryOutsideViewport=[],activeRenderers=1,
                 missingPrefabs=0,emptyMaterials=0,emptyMeshes=0,failures=[])
        report=dict(completed=1350,requested=1350,all1350Instantiated=True,failureCount=0,originalTimeScale=4,
                    levels=[dict(row,assetId=i) for i in range(1,1351)])
        camera.meshes1350(report,audit)
        report["levels"][0]["geometryOutsideViewport"]=["SYNTHETIC actual crop"]
        with self.assertRaisesRegex(audit.EvidenceError,"projection failed"):
            camera.meshes1350(report,audit)


if __name__=="__main__":
    unittest.main()
