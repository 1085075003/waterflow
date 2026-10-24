#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Recovered.FlowWater.Editor
{
    public static class FlowWaterPortraitGameplayDiagnostics
    {
        [Serializable] sealed class RectInfo
        {
            public string path; public bool active; public Vector3 localPosition,worldPosition,localScale,lossyScale;
            public Vector2 anchorMin,anchorMax,size; public Rect screenBounds; public float nearestDepth,farthestDepth;
            public string image; public bool imageEnabled;
        }
        [Serializable] sealed class CanvasInfo
        {
            public string path,mode; public float planeDistance,scaleFactor; public Vector3 localScale,lossyScale;
            public Vector2 canvasSize,referenceResolution; public List<RectInfo> rects=new List<RectInfo>();
        }
        [Serializable] sealed class Report
        {
            public string utc,scene; public int width,height; public bool playing;
            public Vector3 cameraPosition,cameraLocalPosition,cameraScale; public float fov,near,far,aspect;
            public string[] beforeOpaqueCommands; public List<CanvasInfo> canvases=new List<CanvasInfo>();
            public int backgroundGeometryLayers,backgroundImageLayers,remainingForegroundBackgroundLayers;
            public string[] backgroundLayerPaths;
            public RecoveredBoardFraming.BackgroundLayerInfo[] backgroundGeometryDiagnostics;
            public float nearestBoardDepth=float.MaxValue,farthestBoardDepth; public int boardRenderers;
        }
        public static void Audit() { Export(); }
        public static void Export()
        {
            Scene scene=SceneManager.GetActiveScene(); Camera camera=Camera.main;
            if(camera==null) camera=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).First(c=>c.CompareTag("MainCamera"));
            var report=new Report { utc=DateTime.UtcNow.ToString("o"),scene=scene.name,width=Screen.width,height=Screen.height,playing=Application.isPlaying,
                cameraPosition=camera.transform.position,cameraLocalPosition=camera.transform.localPosition,cameraScale=camera.transform.lossyScale,
                fov=camera.fieldOfView,near=camera.nearClipPlane,far=camera.farClipPlane,aspect=camera.aspect,
                beforeOpaqueCommands=camera.GetCommandBuffers(CameraEvent.BeforeForwardOpaque).Select(c=>c.name).ToArray() };
            var framing=camera.GetComponent<RecoveredBoardFraming>();
            if(framing!=null)
            {
                report.backgroundGeometryLayers=framing.BackgroundGeometryLayers;
                report.backgroundImageLayers=framing.BackgroundImageLayers;
                report.remainingForegroundBackgroundLayers=framing.RemainingForegroundBackgroundLayers;
                report.backgroundLayerPaths=framing.BackgroundLayerPaths;
                report.backgroundGeometryDiagnostics=framing.BackgroundGeometryDiagnostics;
            }
            foreach(GameObject root in scene.GetRootGameObjects())
            {
                foreach(Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
                {
                    if(canvas.transform.parent!=null) continue;
                    var scaler=canvas.GetComponent<CanvasScaler>(); var rect=canvas.transform as RectTransform;
                    var info=new CanvasInfo { path=Path(canvas.transform),mode=canvas.renderMode.ToString(),planeDistance=canvas.planeDistance,
                        scaleFactor=canvas.scaleFactor,localScale=canvas.transform.localScale,lossyScale=canvas.transform.lossyScale,canvasSize=rect==null?Vector2.zero:rect.rect.size,
                        referenceResolution=scaler==null?Vector2.zero:scaler.referenceResolution };
                    foreach(RectTransform child in canvas.GetComponentsInChildren<RectTransform>(true))
                        if(child==rect||child.name.StartsWith("PanelW",StringComparison.Ordinal)||child.name=="PanelRotation"||child.name=="panel1"||child.name=="panel2"||
                            child.GetComponent<Selectable>()!=null||(child.transform.parent==canvas.transform&&child.name.StartsWith("fondo",StringComparison.OrdinalIgnoreCase)))
                        {
                            var points=new Vector3[4]; child.GetWorldCorners(points); Vector3[] screen=points.Select(p=>camera.WorldToScreenPoint(p)).ToArray();
                            Image image=child.GetComponent<Image>();
                            info.rects.Add(new RectInfo { path=Path(child),active=child.gameObject.activeInHierarchy,localPosition=child.localPosition,worldPosition=child.position,
                                localScale=child.localScale,lossyScale=child.lossyScale,anchorMin=child.anchorMin,anchorMax=child.anchorMax,size=child.rect.size,
                                screenBounds=Rect.MinMaxRect(screen.Min(p=>p.x),screen.Min(p=>p.y),screen.Max(p=>p.x),screen.Max(p=>p.y)),nearestDepth=screen.Min(p=>p.z),farthestDepth=screen.Max(p=>p.z),
                                image=image==null?"none":image.sprite==null?"no sprite":image.sprite.name,imageEnabled=image!=null&&image.enabled });
                        }
                    report.canvases.Add(info);
                }
                foreach(MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(false))
                {
                    if(!RecoveredBoardFraming.IsStructuralRenderer(renderer)||renderer.GetComponentInParent<Canvas>()!=null) continue;
                    MeshFilter filter=renderer.GetComponent<MeshFilter>(); Bounds bounds=filter.sharedMesh.bounds; report.boardRenderers++;
                    for(int x=-1;x<=1;x+=2) for(int y=-1;y<=1;y+=2) for(int z=-1;z<=1;z+=2)
                    {
                        float depth=camera.WorldToViewportPoint(renderer.transform.TransformPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z)))).z;
                        report.nearestBoardDepth=Mathf.Min(report.nearestBoardDepth,depth); report.farthestBoardDepth=Mathf.Max(report.farthestBoardDepth,depth);
                    }
                }
            }
            string output=System.IO.Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Validation/Portrait"); Directory.CreateDirectory(output);
            File.WriteAllText(System.IO.Path.Combine(output,"gameplay-live-diagnostics.json"),JsonUtility.ToJson(report,true));
        }
        static string Path(Transform node) { return node.parent==null?node.name:Path(node.parent)+"/"+node.name; }
    }
}
#endif
