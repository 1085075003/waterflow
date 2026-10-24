"""Blender 4.4 offline visual-only stone bevel pass.

Usage: blender -b --python Tools/ArtUpgrade/bevel_structural_meshes.py --
       --work Validation/ArtUpgrade/MeshWork
Unity exports readable source meshes and imports generated JSON via FlowWaterArtMeshes.
No collider, transform, script, prefab structure, source mesh or material is modified.
"""
import argparse
import json
import math
import os
import sys
from pathlib import Path

import bpy
import bmesh
from mathutils import Vector


def vec(item, dimensions=3):
    return tuple(item[k] for k in "xyz"[:dimensions])


def encode(values):
    return {key: round(float(value), 10) for key, value in zip("xyz", values)}


def process(source, output_path):
    positions = [vec(v) for v in source["vertices"]]
    faces, materials = [], []
    for index, group in enumerate(source["submeshes"]):
        indices = group["indices"]
        for at in range(0, len(indices), 3):
            tri = indices[at:at + 3]
            if len(set(tri)) == 3:
                faces.append(tri)
                materials.append(index)
    if not faces:
        return {"source": source["sourcePath"], "skipped": "No triangular faces"}
    mesh = bpy.data.meshes.new(source["name"] + "_GardenBevel")
    mesh.from_pydata(positions, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(mesh.name, mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    # Material slots represent existing submeshes; only indices are exported.
    for i in range(len(source["submeshes"])):
        mat = bpy.data.materials.new("slot_" + str(i))
        mesh.materials.append(mat)
    for poly, material in zip(mesh.polygons, materials):
        poly.material_index = material
    if len(source.get("uv", [])) == len(positions):
        layer = mesh.uv_layers.new(name="OriginalUV")
        for loop in mesh.loops:
            layer.data[loop.index].uv = vec(source["uv"][loop.vertex_index], 2)
    extent = vec(source["boundsSize"])
    positive = [v for v in extent if v > max(extent) * 1e-5]
    if len(positive) < 3 or min(positive) / max(extent) < 0.005:
        bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.meshes.remove(mesh)
        return {"source": source["sourcePath"], "skipped": "Planar interface mesh"}
    width = min(positive) * 0.012
    bm = bmesh.new()
    bm.from_mesh(mesh)
    # Reunite original split-normal/UV vertices; UV coordinates remain per loop.
    bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=max(extent) * 1e-7)
    # Reconstruct planar faces from the triangulated export before beveling.
    bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(0.05), verts=list(bm.verts),
                            edges=list(bm.edges), use_dissolve_boundaries=False,
                            delimit={"MATERIAL", "UV"})
    for face in bm.faces:
        face.smooth = True
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()
    original_vertices = len(positions)
    bevel = obj.modifiers.new("Fine stone edge chamfer", "BEVEL")
    bevel.width = width
    bevel.segments = 2
    bevel.limit_method = "ANGLE"
    bevel.angle_limit = math.radians(36.0)
    bevel.affect = "EDGES"
    bevel.use_clamp_overlap = True
    bevel.harden_normals = True
    bevel.loop_slide = True
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    weighted = obj.modifiers.new("Planar stone weighted normals", "WEIGHTED_NORMAL")
    weighted.keep_sharp = True
    weighted.weight = 50
    bpy.ops.object.modifier_apply(modifier=weighted.name)
    result = obj.data
    # Retain original local bounds/pivot, including at all water interfaces.
    for axis, dimension in enumerate(extent):
        lo = min(v.co[axis] for v in result.vertices)
        hi = max(v.co[axis] for v in result.vertices)
        if hi - lo > 1e-12:
            target_lo = source["boundsCenter"]["xyz"[axis]] - dimension * 0.5
            for vertex in result.vertices:
                vertex.co[axis] = target_lo + (vertex.co[axis] - lo) * dimension / (hi - lo)
    result.update()
    result.calc_loop_triangles()
    normals = result.corner_normals
    uvlayer = result.uv_layers.active
    exported_positions, exported_normals, exported_uvs = [], [], []
    groups = [{"indices": []} for _ in source["submeshes"]]
    # Split per loop to preserve UV seams, weighted normals and material boundaries.
    lookup = {}
    for triangle in result.loop_triangles:
        material = result.polygons[triangle.polygon_index].material_index
        for loop_index in triangle.loops:
            position = result.vertices[result.loops[loop_index].vertex_index].co
            normal = normals[loop_index].vector
            uv = uvlayer.data[loop_index].uv if uvlayer else Vector((0, 0))
            key = tuple(round(float(v), 9) for value in (position, normal, uv) for v in value)
            index = lookup.get(key)
            if index is None:
                index = len(exported_positions)
                lookup[key] = index
                exported_positions.append(encode(position))
                exported_normals.append(encode(normal))
                exported_uvs.append(encode(uv))
            groups[material]["indices"].append(index)
    output = {"sourceGuid": source["sourceGuid"], "sourcePath": source["sourcePath"],
              "name": source["name"], "vertices": exported_positions,
              "normals": exported_normals, "uv": exported_uvs, "submeshes": groups,
              "boundsCenter": source["boundsCenter"], "boundsSize": source["boundsSize"]}
    output_path.write_text(json.dumps(output, separators=(",", ":")), encoding="utf-8")
    summary = {"source": source["sourcePath"], "sourceVertices": original_vertices,
               "bevelVertices": len(exported_positions), "sourceTriangles": len(faces),
               "bevelTriangles": sum(len(g["indices"]) // 3 for g in groups),
               "widthLocal": width, "segments": 2, "boundsPreserved": True}
    bpy.data.objects.remove(obj, do_unlink=True)
    if mesh.users == 0:
        bpy.data.meshes.remove(mesh)
    return summary


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--work", required=True)
    arguments = parser.parse_args(sys.argv[sys.argv.index("--") + 1:])
    directory = Path(arguments.work).resolve()
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    report = []
    for path in sorted(directory.glob("*.source.json")):
        source = json.loads(path.read_text(encoding="utf-8-sig"))
        destination = path.with_name(path.name.replace(".source.json", ".bevel.json"))
        destination.unlink(missing_ok=True)
        summary = process(source, destination)
        report.append(summary)
        print("ART_BEVEL " + json.dumps(summary, ensure_ascii=False), flush=True)
    (directory / "blender-report.json").write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
    print("ART_BEVEL_COMPLETED " + str(len(report)), flush=True)


if __name__ == "__main__":
    main()
