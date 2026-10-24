"""Stage per-layout checks of actual mesh vertices at both supported window ratios."""
import hashlib
import json
from pathlib import Path

validation = Path(__file__).resolve().parent
project = validation.parent
relative = "Assets/Editor/RecoveredTools/FlowWaterFullValidation.cs"
base = project / relative
text = base.read_text(encoding="utf-8-sig")
def replace_once(old, new):
    global text
    if text.count(old) != 1:
        raise RuntimeError("Expected a unique anchor: " + old[:90])
    text = text.replace(old, new)

replace_once("public List<string> failures = new List<string>();", "public int structuralVerticesChecked;\n            public List<string> geometryOutsideViewport = new List<string>();\n            public List<string> failures = new List<string>();")
replace_once("AddThumbnail(game.GameCamera);", "AddThumbnail(game.GameCamera, entry, false);")
replace_once('                    if (mode != "all" || entry.failures.Count > 0)', '                    ui.CaptureBoardFrame(() => AddThumbnail(game.GameCamera, entry, true));\n                    if (mode != "all" || entry.failures.Count > 0)')
replace_once("                    ui.CaptureBoardFrame(() => AddThumbnail(game.GameCamera));\n", "")
replace_once("static void AddThumbnail(Camera camera)", "static void AddThumbnail(Camera camera, LevelResult entry, bool solution)")
replace_once("                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;", "                Canvas.ForceUpdateCanvases(); camera.Render();\n                InspectProjectedGeometry(camera, entry, solution);\n                RenderTexture.active = target;")
helper = r'''
        static readonly Dictionary<Mesh, Vector3[]> VertexCache = new Dictionary<Mesh, Vector3[]>();
        static void InspectProjectedGeometry(Camera camera, LevelResult entry, bool solution)
        {
            var game = RecoveredFlowGame.Instance;
            IEnumerable<Renderer> geometry = game.PieceRoot.GetComponentsInChildren<MeshRenderer>();
            foreach (ConfigSuelo floor in OriginalScenePresentation.Components<ConfigSuelo>(SceneManager.GetActiveScene()))
            {
                Renderer surface = floor.GetComponent<Renderer>();
                if (surface != null) geometry = geometry.Concat(new[] { surface });
                if (floor.tierra != null) geometry = geometry.Concat(floor.tierra.GetComponentsInChildren<Renderer>(true));
            }
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                if (root.name == "Recovered original underground border")
                    geometry = geometry.Concat(root.GetComponentsInChildren<Renderer>(true));
            Renderer[] renderers = geometry.Where(RecoveredBoardFraming.IsStructuralRenderer).Distinct().ToArray();
            float aspect = camera.aspect;
            RecoveredBoardFraming framing = camera.GetComponent<RecoveredBoardFraming>();
            try
            {
                foreach (float ratio in new[] { 1280f / 720, 885f / 630 })
                {
                    camera.aspect = ratio;
                    if (framing != null) framing.FitNow();
                    foreach (Renderer renderer in renderers)
                    {
                        MeshFilter filter = renderer.GetComponent<MeshFilter>();
                        Mesh mesh = filter.sharedMesh;
                        Vector3[] vertices;
                        if (!VertexCache.TryGetValue(mesh, out vertices))
                        { vertices = mesh.vertices; VertexCache[mesh] = vertices; }
                        bool outside = false;
                        foreach (Vector3 vertex in vertices)
                        {
                            entry.structuralVerticesChecked++;
                            Vector3 point = camera.WorldToViewportPoint(filter.transform.TransformPoint(vertex));
                            if (point.z < camera.nearClipPlane || point.z > camera.farClipPlane ||
                                point.x < -.0001f || point.x > 1.0001f || point.y < -.0001f || point.y > 1.0001f)
                                outside = true;
                        }
                        if (!outside) continue;
                        string path = renderer.name;
                        for (Transform parent = renderer.transform.parent; parent != null; parent = parent.parent)
                            path = parent.name + "/" + path;
                        string detail = (solution ? "solution" : "initial") + " aspect=" + ratio + " " + path;
                        entry.geometryOutsideViewport.Add(detail);
                        entry.failures.Add("Actual structural mesh vertices cross the supported camera viewport: " + detail);
                    }
                }
            }
            finally
            { camera.aspect = aspect; if (framing != null) framing.FitNow(); }
        }
'''
replace_once("        static void FlushSheet()", helper + "\n        static void FlushSheet()")
stage = validation / "NativeAnalysis/FlowWaterFullValidation.pending-mesh-projection.cs.src"
stage.write_text(text, encoding="utf-8")
manifest = {"baseFile": "UnityProject/" + relative,
            "baseSHA256": hashlib.sha256(base.read_bytes()).hexdigest(),
            "sourceFile": "UnityProject/Validation/NativeAnalysis/" + stage.name,
            "sourceSHA256": hashlib.sha256(stage.read_bytes()).hexdigest(),
            "scope": "For every initial and solution board, project actual original structural mesh vertices at1280x720 and885x630. These are geometry projection checks; the native Unity render is still captured separately. No gameplay, target, camera framing rule or existing pass criterion is relaxed."}
(stage.parent / "staged-full-mesh-projection-manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
print(json.dumps(manifest))
