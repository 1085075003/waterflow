"""Replace obsolete manual-step assertions with the original automatic playback UI timing."""
import hashlib
import json
from pathlib import Path

validation = Path(__file__).resolve().parent
project = validation.parent
target = "Assets/Editor/RecoveredTools/FlowWaterUIValidation.cs"
base = project / target
text = base.read_text(encoding="utf-8-sig")
old = '''            InvokeOriginal("okSteps"); yield return new WaitFrames(3);
            int previousStep = Game.AppliedSolutionSteps;
            InvokeOriginal("okSteps"); yield return new WaitFrames(3);
            CheckState("original-step-button-advances", Game.IsSolutionPreview && Game.AppliedSolutionSteps > previousStep, Game.AppliedSolutionSteps + "/" + Game.SolutionStepCount);
            Capture("07-original-steps-preview.png");'''
new = '''            float nativeStepsEntered = Time.time;
            InvokeOriginal("okSteps"); yield return new WaitFrames(3);
            CheckState("original-steps-startup", Game.IsSolutionPreview && Game.IsSolutionPlaybackRunning && Game.AppliedSolutionSteps == 0,
                "Original okSteps rebuilds initial mode0 and waits2 scaled seconds before automatic playback.");
            CheckState("original-steps-control-disabled", !HasInteractableControl("okSteps"), "Automatic playback cannot be stacked by another steps click.");
            CheckState("original-steps-back-initially-disabled", !HasInteractableControl("doSolucionLevel"), "Original backButton1 waits6 scaled seconds.");
            double autoStepDeadline = EditorApplication.timeSinceStartup + 30;
            while (Game.AppliedSolutionSteps == 0 && EditorApplication.timeSinceStartup < autoStepDeadline) yield return new WaitFrames(1);
            CheckState("original-step-playback-advances-automatically", Game.IsSolutionPreview && Game.AppliedSolutionSteps > 0,
                "No second steps click; completed phases=" + Game.AppliedSolutionSteps + "/" + Game.SolutionStepCount);
            while (Time.time < nativeStepsEntered + 6.1f) yield return new WaitFrames(1);
            CheckState("original-steps-back-enabled-after-delay", HasInteractableControl("doSolucionLevel"), "Original backbutton2 restores the actual exit button after6 scaled seconds.");
            Capture("07-original-steps-preview.png");'''
assert text.count(old) == 1
text = text.replace(old, new)
anchor = "        static bool HasVisibleControl(string method)"
assert text.count(anchor) == 1
text = text.replace(anchor, '''        static bool HasInteractableControl(string method)
        { return UI.OriginalControls.Any(x => x.method == method && x.source != null && x.source.activeInHierarchy && (x.button == null || x.button.interactable)); }

''' + anchor)
stage = validation / "NativeUIFlowValidationBatch1" / target
stage.parent.mkdir(parents=True, exist_ok=True)
stage.write_text(text, encoding="utf-8")
manifest = {"files": [{"target": target, "source": stage.relative_to(project).as_posix(),
    "sha256": hashlib.sha256(stage.read_bytes()).hexdigest(), "previousSha256": hashlib.sha256(base.read_bytes()).hexdigest()}],
    "scope": "Editor only. Preserves all existing UI flows; original steps starts automatically after2 scaled seconds, disables its repeated click, and restores actual exit control after6 seconds. Original actor/guide/water/scene state is not injected.",
    "status": "Staged; actual Unity Run required with native playback runtime and native UI control patch."}
(stage.parents[3] / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
print(json.dumps(manifest))
