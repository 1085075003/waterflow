"""Stage the native current-level global before original scene Start callbacks."""
from pathlib import Path
import hashlib
import json
import difflib
import argparse
from datetime import datetime, timezone

root = Path(__file__).resolve().parent
project = root.parent
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--common-entry", action="store_true")
args = parser.parse_args()
target = project / "Assets/Scripts/RecoveryUI/FlowWaterRecoveryUI.cs"
base = root / "NativeLevelIdentityBatch1/Sources/FlowWaterRecoveryUI.cs.src" if args.common_entry else target
raw = base.read_bytes()
old = base.read_text(encoding="utf-8-sig")
if args.common_entry:
    assert old.count('        ControladorJuego.nivelActual = id;') == 1
    new = old.replace('        ControladorJuego.nivelActual = id;\n', '')
    needle = '    void LoadGameInOriginalScene(int id)\n    {\n'
    assert new.count(needle) == 1
    new = new.replace(needle, needle + '        ControladorJuego.nivelActual = id;\n')
else:
    needle = '        UIManagerSelScript0.parte = SelectedPart;\n        PlayerPrefs.SetInt("Recovery.LastLevelId", id);'
    assert old.count(needle) == 1
    new = old.replace(needle, '        UIManagerSelScript0.parte = SelectedPart;\n        ControladorJuego.nivelActual = id;\n        PlayerPrefs.SetInt("Recovery.LastLevelId", id);')
batch = root / ("NativeLevelIdentityCommonEntryBatch2" if args.common_entry else "NativeLevelIdentityBatch1")
source = batch / "Sources/FlowWaterRecoveryUI.cs.src"
source.parent.mkdir(parents=True, exist_ok=False)
source.write_text(new, encoding="utf-8")
diff = "".join(difflib.unified_diff(old.splitlines(True), new.splitlines(True), fromfile="original", tofile="native-level-identity"))
(batch / "source.diff").write_text(diff, encoding="utf-8")
digest = lambda data: hashlib.sha256(data).hexdigest()
manifest = {"createdUtc": datetime.now(timezone.utc).isoformat(), "immutable": True,
    "files": [{"target": target.relative_to(project).as_posix(), "source": source.relative_to(project).as_posix(),
        "previousSha256": digest(raw), "sha256": digest(source.read_bytes())}],
    "scope": "Set original current asset ID in the common LoadGameInOriginalScene entry, before loading the board and before native scene Start. This covers StartGame and direct Editor Play through OnSceneLoaded. Leveltexto2.Start must not replace a valid live title with a negative default-global value. The only current reader is Leveltexto2.Start; input, collision, actor, water and scheduler are unchanged." if args.common_entry else "Set original current asset ID before loading or reusing its scene. Leveltexto2.Start must not replace a valid live title with a negative default-global value. The only current reader is Leveltexto2.Start; input, collision, actor, water and scheduler are unchanged.",
    "diffSha256": digest((batch / "source.diff").read_bytes()),
    "actualStatus": "Prepared only; actual initial title must be tested before CaptureBoardFrame or RefreshGame."}
(batch / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"manifest": str(batch / "manifest.json"), "before": manifest["files"][0]["previousSha256"], "after": manifest["files"][0]["sha256"]}))
