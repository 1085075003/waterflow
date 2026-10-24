"""Synthetic rejection boundaries only; these are not actual Unity carry proof."""
import copy
from pathlib import Path
from types import SimpleNamespace
import unittest

import aggregate_native_recipe_regression_v3 as target
from test_aggregate_native_recipe_regression_v3 import level, report


def point(x=40, y=3.95, z=20):
    return dict(x=x, y=y, z=z)


class NativeCommitCarryTests(unittest.TestCase):
    def setUp(self):
        self.row = level(1216, 1)
        self.step = self.row["steps"][0]
        self.step.update(destination=point(), nativeLanding=point(), actualAfter=point(40.95),
            productionCommitPosition=point(), productionCommitTarget=point(), originalGuide=point(),
            productionCommitObserved=True, nativeCarryAfterCommit=True, nativeCarryMembership=True,
            nativeCarrierMoving=True, nativeCarrierTurning=False, nativeCarrierCommitObserved=True,
            nativeCarryEvidenceVersion=1, nativeCarrierRecord=52, nativeCarryLayer=1,
            nativeCarrierPosition=point(40.95, -3.65), nativeCarrierCommitPosition=point(40, -3.65),
            nativeCarryOffset=point(0, 7.6, 0), nativeCarryOffsetError=0,
            nativeActorTranslation=point(.95, 0, 0), nativeCarrierTranslation=point(.95, 0, 0),
            nativeCarryTranslationError=0, productionCommitCarriers=[dict(record=52, position=point(40,-3.65))],
            compressionLanding=False, followsMovingSupport=False,
            pickupFrame=10, releaseFrame=11, productionCommitFrame=12, motionFinishedFrame=13,
            committedFrame=13, actualAfterFrame=13)
        data = report([self.row])
        data["checks"].extend(dict(name="gm1216-native-input-"+name+"-1", passed=True)
                              for name in ("actual-commit-pose", "actual-postcommit-carry"))
        self.run = SimpleNamespace(levels={1216:self.row},
            checks_by_level={1216:[x for x in data["checks"] if x["name"].startswith("gm1216-")]},
            source=lambda: {"path":"SYNTHETIC", "sha256":"0"*64})
        self.files = {target.CARRY_EDITOR:target.CARRY_EDITOR_SHA}

    def evaluate(self):
        return target.audit_level_with_carry(self.run,1216,self.files,80)

    def reject(self):
        result=self.evaluate()
        self.assertFalse(result["passed"],result)
        self.assertTrue(any("native postcommit-carry rejected" in x for x in result["auditIssues"]))

    def test_exact_commit_and_observed_same_displacement_only_removes_pose_issue(self):
        self.assertFalse(target.audit.audit_level(self.run,1216)["passed"])
        result=self.evaluate()
        self.assertTrue(result["passed"],result)
        self.assertEqual(result["nativePostCommitCarryEvidence"][0]["actualAfter"],point(40.95))
        self.assertEqual(self.step["actualAfter"],point(40.95))

    def test_source_sha_missing_or_batch10_rejected(self):
        for digest in (None,"69fdf696b85b11ab4aba27e7c49d79748af6b1c63831f6ee415fa14595ebfe51"):
            with self.subTest(digest=digest):
                self.files[target.CARRY_EDITOR]=digest
                self.reject()

    def test_carry_does_not_hide_dry_target_or_failed_check(self):
        self.row["finalActive"]=1
        self.assertFalse(self.evaluate()["passed"])
        self.row["finalActive"]=2
        self.run.checks_by_level[1216][-1]["passed"]=False
        self.reject()

    def test_original_near_point8_commit_is_not_widened(self):
        self.step["productionCommitPosition"]=point(40.81)
        self.reject()

    def test_missing_real_observation_or_turning_rejected(self):
        for key,value in (("nativeCarryMembership",False),("nativeCarrierMoving",False),
                          ("nativeCarrierCommitObserved",False),("productionCommitObserved",False),
                          ("nativeCarrierTurning",True),("compressionLanding",True),
                          ("followsMovingSupport",True),("nativeCarryEvidenceVersion",0)):
            with self.subTest(key=key):
                before=copy.deepcopy(self.step)
                self.step[key]=value
                self.reject()
                self.step.clear();self.step.update(before)

    def test_frame_inversion_or_other_sampling_frame_rejected(self):
        for key,value in (("productionCommitFrame",9),("actualAfterFrame",14),("motionFinishedFrame",11)):
            with self.subTest(key=key):
                before=self.step[key];self.step[key]=value;self.reject();self.step[key]=before

    def test_self_owner_foreign_record_or_bad_layer_rejected(self):
        for key,value in (("nativeCarrierRecord",0),("nativeCarrierRecord",80),("nativeCarryLayer",0)):
            with self.subTest(key=key,value=value):
                before=self.step[key];self.step[key]=value;self.reject();self.step[key]=before

    def test_owner_missing_or_duplicate_in_actual_commit_snapshot_rejected(self):
        before=copy.deepcopy(self.step["productionCommitCarriers"])
        self.step["productionCommitCarriers"]=[];self.reject()
        self.step["productionCommitCarriers"]=before+copy.deepcopy(before);self.reject()

    def test_false_declared_errors_cannot_hide_different_actual_translation(self):
        self.step["nativeCarrierCommitPosition"]=point(39,-3.65)
        self.step["productionCommitCarriers"][0]["position"]=point(39,-3.65)
        self.step["nativeCarrierTranslation"]=point(1.95,0,0)
        self.reject()

    def test_wrong_seven_point6_height_rejected(self):
        self.step["nativeCarrierPosition"]=point(40.95,-3.60)
        self.step["nativeCarryOffset"]=point(0,7.55,0)
        self.step["nativeCarrierTranslation"]=point(.95,.05,0)
        self.reject()

    def test_carrier_declared_offset_error_cannot_be_nan_or_false(self):
        self.step["nativeCarryOffsetError"]=float("nan");self.reject()
        self.step["nativeCarryOffsetError"]=.02;self.reject()

    def test_unchanged_plain_landing_still_uses_old_audit(self):
        self.step["nativeCarryAfterCommit"]=False
        self.step["actualAfter"]=point()
        self.files={}
        self.assertTrue(self.evaluate()["passed"])


if __name__=="__main__":
    unittest.main()
