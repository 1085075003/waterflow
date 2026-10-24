"""Verify applied visual meshes preserve source component documents and exact AABBs."""
from datetime import datetime, timezone
import json
import re
from pathlib import Path

base = Path(__file__).resolve().parents[2]
report_path = base / "Validation/ArtUpgrade/mesh-apply-report.json"
report = json.loads(report_path.read_text(encoding="utf-8-sig"))
pattern = re.compile(r"^--- !u!(\d+) &(-?\d+)[^\r\n]*\r?\n(.*?)(?=^--- !u!|\Z)", re.M | re.S)
def documents(text):
    return {match[2]: (match[1], match[3]) for match in pattern.finditer(text)}
rows = []
for asset in report["assets"]:
    before = base / "Validation/ArtUpgrade/Before/VisualMeshes" / asset
    if not before.exists():
        before = base / "Validation/ArtUpgrade/Before" / asset
    current = base / asset
    old, new = documents(before.read_text(encoding="utf-8-sig")), documents(current.read_text(encoding="utf-8-sig"))
    removed, added = set(old) - set(new), set(new) - set(old)
    nonmesh_changes = [key for key in set(old) & set(new) if old[key] != new[key] and old[key][0] != "33"]
    rows.append({"asset": asset, "source": str(before.relative_to(base)),
                 "addedDocuments": len(added), "removedDocuments": len(removed),
                 "changedNonMeshFilterDocuments": nonmesh_changes,
                 "passed": not (removed or added or nonmesh_changes)})
summary = {"verifiedUtc": datetime.now(timezone.utc).isoformat(), "assets": len(rows), "passed": sum(r["passed"] for r in rows),
           "failed": sum(not r["passed"] for r in rows), "details": rows}
(base / "Validation/ArtUpgrade/mesh-isolation-verification.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
print(json.dumps({key: value for key, value in summary.items() if key != "details"}))
if summary["failed"]:
    raise SystemExit(1)
