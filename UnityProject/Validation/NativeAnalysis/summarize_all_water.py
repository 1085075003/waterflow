"""Diagnostic only: static original prefab port graph, not Unity behavior validation."""
import collections
import contextlib
import io
import json
import pathlib
import time
import analyze_water_graph as graph

root = pathlib.Path.cwd()
output = root / "UnityProject/Validation/NativeAnalysis/all-static-water-diagnostics.json"
levels = sorted(int(p.stem[2:]) for p in (root / "LevelData/01_Layouts").glob("gm*.json"))
report = {"scope": __doc__, "completed": 0, "levels": [], "errors": []}
for level in levels:
    try:
        with contextlib.redirect_stdout(io.StringIO()):
            nodes = graph.analyze(level)
        targets = [n for n in nodes if n["target"]]
        report["levels"].append({"assetId": level, "nodes": len(nodes), "wet": sum(n["wet"] for n in nodes),
            "required": len(targets), "active": sum(n["wet"] for n in targets),
            "dryTargets": [n["name"] for n in targets if not n["wet"]],
            "dryTypes": dict(collections.Counter(n["typ"] for n in nodes if not n["wet"]))})
    except Exception as error:
        report["errors"].append({"assetId": level, "error": str(error)})
    report["completed"] += 1
    if report["completed"] % 100 == 0:
        print(report["completed"], "/", len(levels), flush=True)
output.write_text(json.dumps(report, indent=2), encoding="utf-8")
print("Finished", report["completed"], "errors", len(report["errors"]), flush=True)
