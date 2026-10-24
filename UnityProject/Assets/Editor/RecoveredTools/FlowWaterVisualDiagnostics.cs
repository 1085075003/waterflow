#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Recovered.FlowWater.Editor
{
    public static class FlowWaterVisualDiagnostics
    {
        [Serializable] public sealed class MaterialInfo { public string name, shader; public Color color; public int renderQueue; }
        [Serializable] public sealed class RendererInfo
        {
            public string path, type, mesh;
            public int layer, vertices, triangles;
            public bool boundsFinite;
            public Vector3 localPosition, worldPosition, localScale, lossyScale, localEuler, worldEuler;
            public Vector3 worldBoundsCenter, worldBoundsExtents, centerViewport, centerScreen, centerScreenTopLeft;
            public Vector3[] boundsViewport, boundsScreen, meshViewport;
            public List<MaterialInfo> materials = new List<MaterialInfo>();
        }
        [Serializable] public sealed class Report
        {
            public string unityVersion, capturedUtc, camera;
            public int width, height;
            public float fieldOfView, aspect;
            public Vector3 cameraPosition, cameraEuler;
            public Rect cameraRect;
            public string projection = "Screen coordinates derive from WorldToViewportPoint and the captured camera rect; top-left coordinates have y=height-screenY.";
            public List<RendererInfo> renderers = new List<RendererInfo>();
        }
        public static void Write(string path, Camera camera, int width, int height, Transform root = null)
        {
            if (camera == null) return;
            float oldAspect = camera.aspect;
            try
            {
                camera.aspect = width / (float)height;
                var report = new Report { unityVersion = Application.unityVersion, capturedUtc = DateTime.UtcNow.ToString("o"),
                    camera = Hierarchy(camera.transform), width = width, height = height, fieldOfView = camera.fieldOfView,
                    aspect = camera.aspect, cameraPosition = camera.transform.position, cameraEuler = camera.transform.eulerAngles, cameraRect = camera.rect };
                var renderers = root == null ? UnityEngine.Object.FindObjectsOfType<Renderer>() : root.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers.Where(x => x != null && x.enabled && x.gameObject.activeInHierarchy))
                {
                    var tr = renderer.transform;
                    var item = new RendererInfo { path = Hierarchy(tr), type = renderer.GetType().Name, layer = tr.gameObject.layer,
                        localPosition = tr.localPosition, worldPosition = tr.position, localScale = tr.localScale, lossyScale = tr.lossyScale,
                        localEuler = tr.localEulerAngles, worldEuler = tr.eulerAngles, worldBoundsCenter = renderer.bounds.center,
                        worldBoundsExtents = renderer.bounds.extents, boundsFinite = Finite(renderer.bounds.center) && Finite(renderer.bounds.extents) };
                    if (item.boundsFinite)
                    {
                        item.centerViewport = camera.WorldToViewportPoint(renderer.bounds.center);
                        item.centerScreen = Screen(camera, item.centerViewport, width, height);
                        item.centerScreenTopLeft = new Vector3(item.centerScreen.x, height - item.centerScreen.y, item.centerScreen.z);
                        item.boundsViewport = Corners(renderer.bounds).Select(camera.WorldToViewportPoint).ToArray();
                        item.boundsScreen = item.boundsViewport.Select(x => Screen(camera, x, width, height)).ToArray();
                    }
                    var filter = renderer.GetComponent<MeshFilter>();
                    Mesh mesh = filter == null ? null : filter.sharedMesh;
                    if (mesh == null && renderer is SkinnedMeshRenderer) mesh = ((SkinnedMeshRenderer)renderer).sharedMesh;
                    if (mesh != null)
                    {
                        item.mesh = mesh.name; item.vertices = mesh.vertexCount;
                        long indices = 0; for (int sub = 0; sub < mesh.subMeshCount; sub++) indices += mesh.GetIndexCount(sub);
                        item.triangles = (int)(indices / 3);
                        item.meshViewport = Corners(mesh.bounds).Select(x => camera.WorldToViewportPoint(tr.TransformPoint(x))).ToArray();
                    }
                    foreach (var material in renderer.sharedMaterials)
                        item.materials.Add(new MaterialInfo { name = material == null ? "<missing>" : material.name,
                            shader = material == null || material.shader == null ? "<missing>" : material.shader.name,
                            color = material != null && material.HasProperty("_Color") ? material.color : Color.clear,
                            renderQueue = material == null ? 0 : material.renderQueue });
                    report.renderers.Add(item);
                }
                Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, JsonUtility.ToJson(report, true));
            }
            finally { camera.aspect = oldAspect; }
        }
        static Vector3 Screen(Camera camera, Vector3 viewport, int width, int height)
        { Rect rect = camera.rect; return new Vector3((rect.x + viewport.x * rect.width) * width, (rect.y + viewport.y * rect.height) * height, viewport.z); }
        static bool Finite(Vector3 point)
        { return !Single.IsNaN(point.x) && !Single.IsNaN(point.y) && !Single.IsNaN(point.z) && !Single.IsInfinity(point.x) && !Single.IsInfinity(point.y) && !Single.IsInfinity(point.z); }
        static IEnumerable<Vector3> Corners(Bounds bounds)
        { for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2) yield return bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z)); }
        static string Hierarchy(Transform tr)
        { string path = tr.name; while (tr.parent != null) { tr = tr.parent; path = tr.name + "/" + path; } return path; }
    }
}
#endif
