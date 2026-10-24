"""Stage native tutorial dismissal before the existing full-content board captures."""
import hashlib
import json
from pathlib import Path

validation = Path(__file__).resolve().parent
base = validation / "NativeAnalysis/FlowWaterFullValidation.pending-mesh-projection.cs.src"
text = base.read_text(encoding="utf-8-sig")

def replace_once(old, new):
    global text
    if text.count(old) != 1:
        raise RuntimeError("Expected unique anchor: " + old[:90])
    text = text.replace(old, new)

replace_once("public int structuralVerticesChecked;", "public bool tutorialShown, tutorialDismissedByNativeRotation;\n            public int structuralVerticesChecked;")
replace_once("static bool busy;", "static bool busy;\n        static int tutorialPhase;\n        static float tutorialYaw, tutorialTurnFinished;\n        static bool tutorialShown, tutorialDismissed;")
replace_once("ui.StartGame(id); phase = 1;", "tutorialPhase = 0; tutorialShown = tutorialDismissed = false;\n                    ui.StartGame(id); phase = 1;")
replace_once("                    var initialWater = RecoveredWaterNetwork.Instance;", "                    if (!PrepareNativeTutorialReview())\n                    {\n                        if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException(\"Native tutorial turn/return did not finish: gm\" + id);\n                        next = EditorApplication.timeSinceStartup + .02; return;\n                    }\n                    var initialWater = RecoveredWaterNetwork.Instance;")
replace_once("geometricSolutionAvailable = game.HasSolution, status = game.Status", "geometricSolutionAvailable = game.HasSolution, status = game.Status,\n                        tutorialShown = tutorialShown, tutorialDismissedByNativeRotation = tutorialDismissed")
helper = r'''
        // A board review performs the real retained quarter-turn and return.
        // It never sets tutorial preferences, girado, animator state or camera pose.
        // CameraTutorialValidation separately checks the first-use prompt itself.
        static bool PrepareNativeTutorialReview()
        {
            HelpRotate help = OriginalScenePresentation.Components<HelpRotate>(SceneManager.GetActiveScene())
                .FirstOrDefault(h => h.gameObject.activeInHierarchy && h.checkSwipe);
            if (tutorialPhase == 0)
            {
                if (help == null) return true;
                SwipeHandler2 swipe = RecoveredNativeCamera.CurrentSwipe;
                if (swipe == null || swipe.EsferaCamera == null)
                    throw new InvalidOperationException("Visible native tutorial has no original camera controller.");
                tutorialShown = true; tutorialYaw = swipe.EsferaCamera.eulerAngles.y;
                swipe.GirarDerecha(); tutorialPhase = 1; return false;
            }
            SwipeHandler2 current = RecoveredNativeCamera.CurrentSwipe;
            if (current == null || current.EsferaCamera == null) return false;
            if (tutorialPhase == 1)
            {
                if (Mathf.Abs(Mathf.DeltaAngle(current.EsferaCamera.eulerAngles.y, tutorialYaw + 90)) > .25f) return false;
                current.GirarIzquierda(); tutorialPhase = 2; return false;
            }
            if (tutorialPhase == 2)
            {
                if (Mathf.Abs(Mathf.DeltaAngle(current.EsferaCamera.eulerAngles.y, tutorialYaw)) > .25f) return false;
                tutorialTurnFinished = Time.time; tutorialPhase = 3; return false;
            }
            if (Time.time - tutorialTurnFinished < 1) return false;
            tutorialDismissed = help == null && PlayerPrefs.GetInt("helpRotation", 0) == 1;
            if (!tutorialDismissed) throw new InvalidOperationException("Actual camera turn did not dismiss the original tutorial.");
            return true;
        }
'''
replace_once("        static void Inspect(LevelResult entry, RecoveredFlowGame game)", helper + "\n        static void Inspect(LevelResult entry, RecoveredFlowGame game)")
replace_once('            AddPreference(backup, "Recovery.LastLevelId");', '            AddPreference(backup, "Recovery.LastLevelId");\n            AddPreference(backup, "helpRotation");')
stage = validation / "NativeAnalysis/FlowWaterFullValidation.pending-native-tutorial.cs.src"
stage.write_text(text, encoding="utf-8")
manifest = {
    "baseFile": "UnityProject/Validation/NativeAnalysis/" + base.name,
    "baseSHA256": hashlib.sha256(base.read_bytes()).hexdigest(),
    "mainBaseSHA256": hashlib.sha256((validation.parent / "Assets/Editor/RecoveredTools/FlowWaterFullValidation.cs").read_bytes()).hexdigest(),
    "sourceFile": "UnityProject/Validation/NativeAnalysis/" + stage.name,
    "sourceSHA256": hashlib.sha256(stage.read_bytes()).hexdigest(),
    "scope": "Cumulative strict original mesh projection plus actual native camera quarter-turn and return when a first-use tutorial is visible, before board captures. Tutorial behavior separately verified, no direct tutorial/camera/preference injection.",
    "validation": "Staged; requires actual representative run before full1350."
}
(stage.parent / "staged-full-native-tutorial-manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
print(json.dumps(manifest))
