"""Blender offline nearest-surface and bounds audit for the generated visual meshes."""
import json
import math
import sys
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

directory = Path(sys.argv[sys.argv.index("--") + 1]).resolve()
rows = []
for path in sorted(directory.glob("*.bevel.json")):
    source = json.loads(path.with_name(path.name.replace(".bevel.json", ".source.json")).read_text(encoding="utf-8-sig"))
    target = json.loads(path.read_text(encoding="utf-8-sig"))
    positions = lambda data: [Vector((v["x"], v["y"], v["z"])) for v in data["vertices"]]
    def faces(data):
        return [group["indices"][i:i+3] for group in data["submeshes"] for i in range(0, len(group["indices"]), 3)]
    original, improved = positions(source), positions(target)
    ofaces, ifaces = faces(source), faces(target)
    original_bvh = BVHTree.FromPolygons(original, ofaces, all_triangles=True)
    target_bvh = BVHTree.FromPolygons(improved, ifaces, all_triangles=True)
    def deviation(vertices, triangles, surface):
        points = list(vertices) + [sum((vertices[i] for i in tri), Vector()) / 3 for tri in triangles]
        return max((surface.find_nearest(point)[3] or 0 for point in points), default=0)
    forward = deviation(original, ofaces, target_bvh)
    reverse = deviation(improved, ifaces, original_bvh)
    thickness = min(source["boundsSize"].values())
    limit = thickness * 0.04
    row = {"source": source["sourcePath"], "guid": source["sourceGuid"],
           "sourceToBevelMax": forward, "bevelToSourceMax": reverse,
           "limitLocal": limit, "passed": max(forward, reverse) <= limit}
    rows.append(row)
    if not row["passed"]:
        print("ART_MESH_AUDIT_FAIL " + json.dumps(row), flush=True)
(directory / "geometry-audit.json").write_text(json.dumps(rows, indent=2), encoding="utf-8")
print("ART_MESH_AUDIT " + json.dumps({"meshes": len(rows), "passed": sum(r["passed"] for r in rows),
                                      "failed": sum(not r["passed"] for r in rows)}), flush=True)
