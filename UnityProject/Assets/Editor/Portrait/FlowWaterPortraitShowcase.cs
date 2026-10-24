#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Recovered.FlowWater.Editor
{
    public static class FlowWaterPortraitCategoryShowcase
    {
        public static void Apply() { FlowWaterPortraitShowcase.ApplyCategoryMountsAnchored(); }
    }
    /// <summary>Fits the original menu dioramas into the portrait UI's open bands.</summary>
    public static class FlowWaterPortraitShowcase
    {
        const float FitAspect=.45f; // Includes horizontal breathing room on narrow, tall phones.
        static readonly string[] Scenes={"Assets/Scenes/Portada.unity","Assets/Scenes/SelLevel0.unity"};
        [Serializable] public sealed class MeshInfo { public string path,variant,excluded; public bool active; public Bounds worldBounds; }
        [Serializable] public sealed class SceneInfo
        {
            public string scene,camera,protectedBefore,protectedAfter; public bool passed;
            public Vector3 beforePosition,afterPosition; public Quaternion beforeRotation,afterRotation;
            public float fieldOfView,farClipPlane,projectionAspect; public Rect viewport; public int meshCount,cornerCount,viewportViolations;
            public Vector2 projectedMin,projectedMax; public bool geometryFits;
            public List<MeshInfo> meshes=new List<MeshInfo>(); public List<string> warnings=new List<string>();
        }
        [Serializable] public sealed class Report { public string generatedUtc; public bool passed=true; public float fitAspect=FitAspect; public List<SceneInfo> scenes=new List<SceneInfo>(); }
        [Serializable] public sealed class MountChange
        {
            public string path; public Vector3 beforeLocalPosition,afterLocalPosition,beforeLocalScale,afterLocalScale;
            public Quaternion beforeLocalRotation,afterLocalRotation; public Vector2 beforeAnchorMin,beforeAnchorMax,afterAnchorMin,afterAnchorMax;
            public float uniformScale=1; public bool passed; public SceneInfo geometry;
        }
        [Serializable] public sealed class CategoryReport
        {
            public string generatedUtc,protectedBefore,protectedAfter; public bool passed;
            public string allowedProperties="Existing showcase mount localPosition/anchoredPosition and uniform scale multiplier; original hierarchy, rotation, prefab, geometry, components, active states and events are protected.";
            public List<MountChange> changes=new List<MountChange>();
        }
        static string Project { get { return Directory.GetParent(Application.dataPath).FullName; } }
        [MenuItem("Tools/Flow Water/Portrait/Apply Menu Showcase Cameras")]
        public static void Apply() { Process(true,"showcase-camera-apply.json"); }
        public static void Export() { Process(false,"showcase-camera-inventory.json"); }
        public static void Audit() { Process(false,"showcase-camera-audit.json"); }
        static Rect CategoryArea(Scene scene)
        {
            RectTransform canvas=scene.GetRootGameObjects().Where(r=>r.name=="Canvas").Select(r=>r.transform as RectTransform).First();
            float height=Mathf.Max(1280,canvas.rect.height);
            return Rect.MinMaxRect(.08f,140/height,.92f,330/height);
        }
        public static void ApplyCategoryMounts() { ApplyCategoryMounts(false); }
        // Optional complete bottom anchoring. This is separate so its one
        // additional authored parent Rect change can be reviewed explicitly.
        public static void ApplyCategoryMountsAnchored() { ApplyCategoryMounts(true); }
        static void ApplyCategoryMounts(bool anchorBottom)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Apply category presentation in Edit mode.");
            var setup=EditorSceneManager.GetSceneManagerSetup(); string scenePath="Assets/Scenes/SelLevel0.unity";
            var report=new CategoryReport { generatedUtc=DateTime.UtcNow.ToString("o") };
            try
            {
                Backup(scenePath); Scene scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single); Canvas.ForceUpdateCanvases();
                Camera camera=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).First(c=>c.CompareTag("MainCamera"));
                if(camera.aspect>=1) throw new InvalidOperationException("Set the actual Game View to 720x1280 before applying category mounts.");
                UIManagerSelScript0 selector=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UIManagerSelScript0>(true)).First();
                Canvas canvas=scene.GetRootGameObjects().Where(r=>r.name=="Canvas").Select(r=>r.GetComponent<Canvas>()).First();
                Transform panelGr=canvas.transform.Find("Panel1/Panel/Panel2/PanelGr");
                if(panelGr==null) throw new InvalidOperationException("Missing original category showcase parent.");
                RectTransform panel2=panelGr.parent as RectTransform;
                GameObject[] prefabs={selector.prefabGrupo1,selector.prefabGrupo2,selector.prefabGrupo3,selector.prefabGrupo4,selector.prefabGrupo5,selector.prefabGrupo6};
                GameObject[] mounts={selector.panel1,selector.panel2,selector.panel3,selector.panel4,selector.panel5,selector.panel6};
                var allowed=new HashSet<Transform>(mounts.Where(m=>m!=null).Select(m=>m.transform)); allowed.Add(panelGr);
                if(anchorBottom) allowed.Add(panel2);
                report.protectedBefore=ProtectedFingerprint(scene,null,allowed);
                if(anchorBottom)
                {
                    MountChange parentChange=Before(panel2);
                    panel2.anchorMin=new Vector2(panel2.anchorMin.x,0); panel2.anchorMax=new Vector2(panel2.anchorMax.x,0);
                    panel2.anchoredPosition=new Vector2(panel2.anchoredPosition.x,0);
                    After(parentChange,panel2,1);
                    parentChange.passed=Quaternion.Angle(parentChange.beforeLocalRotation,parentChange.afterLocalRotation)<.001f&&parentChange.beforeLocalScale==parentChange.afterLocalScale&&
                        parentChange.beforeAnchorMin.x==parentChange.afterAnchorMin.x&&parentChange.beforeAnchorMax.x==parentChange.afterAnchorMax.x&&parentChange.afterAnchorMin.y==0&&parentChange.afterAnchorMax.y==0;
                    report.changes.Add(parentChange);
                    report.allowedProperties+=" Bottom anchoring additionally changes only the shared presentation parent Canvas/Panel1/Panel/Panel2 Y anchors from center to bottom and its Y anchored position to zero.";
                }
                MountChange groupChange=Before(panelGr); panelGr.localPosition=new Vector3(0,0,panelGr.localPosition.z); After(groupChange,panelGr,1); report.changes.Add(groupChange);
                Canvas.ForceUpdateCanvases();
                for(int i=0;i<mounts.Length;i++)
                {
                    if(prefabs[i]==null||mounts[i]==null) throw new InvalidOperationException("Missing category preview "+(i+1));
                    Transform mount=mounts[i].transform; MountChange change=Before(mount);
                    GameObject preview=null;
                    try
                    {
                        preview=CreateCategoryPreview(prefabs[i],mount,i);
                        var info=new SceneInfo { scene=scenePath,camera=PathOf(camera.transform),projectionAspect=camera.aspect,viewport=CategoryArea(scene) };
                        var corners=new List<Vector3>();
                        foreach(MeshRenderer renderer in preview.GetComponentsInChildren<MeshRenderer>(true)) Collect(renderer,preview.transform,"category-"+(i+1),camera,corners,info);
                        if(corners.Count==0) throw new InvalidOperationException("Category source has no visible geometry: "+(i+1));
                        // The menu's camera-space background is drawn at the
                        // canvas plane. Keep the 3D preview wholly in front of it.
                        float scale=FitMount(camera,mount,corners,info.viewport,canvas.planeDistance*.4f);
                        Canvas.ForceUpdateCanvases(); corners.Clear(); info.meshes.Clear();
                        foreach(MeshRenderer renderer in preview.GetComponentsInChildren<MeshRenderer>(true)) Collect(renderer,preview.transform,"category-"+(i+1),camera,corners,info);
                        info.meshCount=info.meshes.Count(m=>String.IsNullOrEmpty(m.excluded)); info.cornerCount=corners.Count; MeasureFit(camera,corners,info);
                        info.passed=info.geometryFits; change.geometry=info; After(change,mount,scale); report.changes.Add(change);
                        if(!change.passed||!info.geometryFits) throw new InvalidOperationException("Category showcase did not fit without changing its rotation: "+(i+1));
                    }
                    finally { if(preview!=null) UnityEngine.Object.DestroyImmediate(preview); }
                }
                report.protectedAfter=ProtectedFingerprint(scene,null,allowed);
                report.passed=report.protectedBefore==report.protectedAfter&&report.changes.All(c=>c.passed&&(c.geometry==null||c.geometry.geometryFits));
                if(!report.passed) throw new InvalidOperationException("Category hierarchy, events or protected properties changed.");
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
                string output=Path.Combine(Project,"Validation/Portrait"); Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output,"category-showcase-mounts.json"),JsonUtility.ToJson(report,true));
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
        static MountChange Before(Transform mount)
        {
            var change=new MountChange { path=PathOf(mount),beforeLocalPosition=mount.localPosition,beforeLocalRotation=mount.localRotation,beforeLocalScale=mount.localScale };
            RectTransform rect=mount as RectTransform; if(rect!=null) { change.beforeAnchorMin=rect.anchorMin; change.beforeAnchorMax=rect.anchorMax; }
            return change;
        }
        static void After(MountChange change,Transform mount,float scale)
        {
            change.afterLocalPosition=mount.localPosition; change.afterLocalRotation=mount.localRotation; change.afterLocalScale=mount.localScale; change.uniformScale=scale;
            RectTransform rect=mount as RectTransform; if(rect!=null) { change.afterAnchorMin=rect.anchorMin; change.afterAnchorMax=rect.anchorMax; }
            change.passed=Quaternion.Angle(change.beforeLocalRotation,change.afterLocalRotation)<.001f&&(change.afterLocalScale-change.beforeLocalScale*scale).sqrMagnitude<.00001f;
            if(rect!=null) change.passed&=change.beforeAnchorMin==change.afterAnchorMin&&change.beforeAnchorMax==change.afterAnchorMax;
            EditorUtility.SetDirty(mount);
        }
        static float FitMount(Camera camera,Transform mount,List<Vector3> corners,Rect area,float depth)
        {
            float tanY=Mathf.Tan(camera.fieldOfView*.5f*Mathf.Deg2Rad),tanX=tanY*camera.aspect;
            float left=(area.xMin*2-1)*tanX,right=(area.xMax*2-1)*tanX,bottom=(area.yMin*2-1)*tanY,top=(area.yMax*2-1)*tanY;
            Quaternion inverse=Quaternion.Inverse(camera.transform.rotation); Vector3 pivot=mount.position;
            Vector4 limits=new Vector4(float.NegativeInfinity,float.PositiveInfinity,float.NegativeInfinity,float.PositiveInfinity);
            float minZ=float.PositiveInfinity,maxZ=float.NegativeInfinity;
            foreach(Vector3 point in corners)
            {
                Vector3 delta=inverse*(point-pivot);
                limits.x=Mathf.Max(limits.x,left*delta.z-delta.x); limits.y=Mathf.Min(limits.y,right*delta.z-delta.x);
                limits.z=Mathf.Max(limits.z,bottom*delta.z-delta.y); limits.w=Mathf.Min(limits.w,top*delta.z-delta.y);
                minZ=Mathf.Min(minZ,delta.z); maxZ=Mathf.Max(maxZ,delta.z);
            }
            float maxScaleX=limits.x>limits.y?(right-left)*depth/(limits.x-limits.y):100;
            float maxScaleY=limits.z>limits.w?(top-bottom)*depth/(limits.z-limits.w):100;
            float scale=Mathf.Min(maxScaleX,maxScaleY);
            if(minZ<0) scale=Mathf.Min(scale,(depth-camera.nearClipPlane-1)/-minZ);
            if(maxZ>0) scale=Mathf.Min(scale,(camera.farClipPlane-1-depth)/maxZ);
            scale*=.94f; if(scale<=0||Single.IsNaN(scale)||Single.IsInfinity(scale)) throw new InvalidOperationException("Invalid category showcase scale.");
            Vector3 desired=new Vector3((left*depth+scale*limits.x+right*depth+scale*limits.y)*.5f,(bottom*depth+scale*limits.z+top*depth+scale*limits.w)*.5f,depth);
            mount.localScale*=scale; mount.position=camera.transform.position+camera.transform.rotation*desired;
            return scale;
        }
        static GameObject CreateCategoryPreview(GameObject prefab,Transform mount,int variant)
        {
            GameObject preview=UnityEngine.Object.Instantiate(prefab,Vector3.zero,Quaternion.identity);
            preview.name=prefab.name+" [portrait measurement]"; preview.transform.SetParent(mount,true); preview.transform.localPosition=Vector3.zero; preview.transform.localScale=Vector3.one;
            if(variant==2)
            {
                preview.transform.localPosition=new Vector3(-57.2f,-55f,63.3f); preview.transform.localScale=new Vector3(1.86431098f,2.20000005f,1.86431098f);
                preview.transform.localRotation=Quaternion.Euler(new Vector3(-.4571191669f,.6783745885f,-.2294060737f)*Mathf.Rad2Deg);
            }
            return preview;
        }
        static void Process(bool apply,string reportName)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use the showcase tools in Edit mode.");
            var setup=EditorSceneManager.GetSceneManagerSetup(); var report=new Report { generatedUtc=DateTime.UtcNow.ToString("o") };
            try
            {
                foreach(string path in Scenes)
                {
                    // Category dioramas are descendants of the camera-driven
                    // Canvas. Their existing presentation mounts, rather than
                    // this same camera, must define their screen placement.
                    if(apply&&path.EndsWith("SelLevel0.unity",StringComparison.Ordinal)) continue;
                    if(apply) Backup(path);
                    Scene scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                    Camera camera=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).FirstOrDefault(c=>c.CompareTag("MainCamera"));
                    if(camera==null) throw new InvalidOperationException("Missing menu main camera: "+path);
                    Canvas.ForceUpdateCanvases();
                    var info=new SceneInfo { scene=path,camera=PathOf(camera.transform),beforePosition=camera.transform.position,beforeRotation=camera.transform.rotation,
                        fieldOfView=camera.fieldOfView,projectionAspect=scene.name=="Portada"?FitAspect:camera.aspect,viewport=scene.name=="Portada"?Rect.MinMaxRect(.08f,.40f,.92f,.79f):CategoryArea(scene) };
                    info.protectedBefore=ProtectedFingerprint(scene,camera);
                    var corners=new List<Vector3>(); var temporary=new List<GameObject>();
                    try
                    {
                        if(scene.name=="SelLevel0") CollectCategoryVariants(scene,camera,corners,info,temporary);
                        else
                            foreach(MeshRenderer renderer in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MeshRenderer>(true)))
                                Collect(renderer,null,"home",camera,corners,info);
                        info.cornerCount=corners.Count; info.meshCount=info.meshes.Count(m=>String.IsNullOrEmpty(m.excluded));
                        if(corners.Count==0) throw new InvalidOperationException("No menu showcase geometry: "+path);
                        if(apply) Fit(camera,corners,info.viewport);
                        Canvas.ForceUpdateCanvases();
                        MeasureFit(camera,corners,info);
                    }
                    finally { foreach(GameObject temporaryRoot in temporary) if(temporaryRoot!=null) UnityEngine.Object.DestroyImmediate(temporaryRoot); }
                    Canvas.ForceUpdateCanvases();
                    info.afterPosition=camera.transform.position; info.afterRotation=camera.transform.rotation; info.farClipPlane=camera.farClipPlane;
                    info.protectedAfter=ProtectedFingerprint(scene,camera); info.passed=info.protectedBefore==info.protectedAfter && Quaternion.Angle(info.beforeRotation,info.afterRotation)<.001f;
                    if(!info.passed) throw new InvalidOperationException("Showcase operation changed protected content: "+path);
                    if(apply&&!info.geometryFits) throw new InvalidOperationException("Showcase camera failed geometry containment: "+path);
                    if(reportName!="showcase-camera-inventory.json") info.passed&=info.geometryFits;
                    report.passed&=info.passed;
                    report.scenes.Add(info);
                    if(apply) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
                }
                if(apply) AssetDatabase.SaveAssets();
                string output=Path.Combine(Project,"Validation/Portrait"); Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output,reportName),JsonUtility.ToJson(report,true));
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
        static void CollectCategoryVariants(Scene scene,Camera camera,List<Vector3> corners,SceneInfo info,List<GameObject> temporary)
        {
            UIManagerSelScript0 selector=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UIManagerSelScript0>(true)).FirstOrDefault();
            if(selector==null) throw new InvalidOperationException("Missing category selector.");
            info.warnings.Add("Category models are in the main camera-driven Canvas; use existing mount presentation transforms to arrange the showcase band.");
            GameObject[] prefabs={selector.prefabGrupo1,selector.prefabGrupo2,selector.prefabGrupo3,selector.prefabGrupo4,selector.prefabGrupo5,selector.prefabGrupo6};
            GameObject[] mounts={selector.panel1,selector.panel2,selector.panel3,selector.panel4,selector.panel5,selector.panel6};
            for(int i=0;i<prefabs.Length;i++)
            {
                if(prefabs[i]==null||mounts[i]==null) { info.warnings.Add("No source preview variant "+(i+1)); continue; }
                // Match the retained runtime's exact presentation instantiation.
                GameObject preview=UnityEngine.Object.Instantiate(prefabs[i],Vector3.zero,Quaternion.identity);
                temporary.Add(preview); preview.name=prefabs[i].name+" [portrait measurement]";
                preview.transform.SetParent(mounts[i].transform,true); preview.transform.localPosition=Vector3.zero; preview.transform.localScale=Vector3.one;
                if(i==2)
                {
                    preview.transform.localPosition=new Vector3(-57.2f,-55f,63.3f);
                    preview.transform.localScale=new Vector3(1.86431098f,2.20000005f,1.86431098f);
                    preview.transform.localRotation=Quaternion.Euler(new Vector3(-.4571191669f,.6783745885f,-.2294060737f)*Mathf.Rad2Deg);
                }
                foreach(MeshRenderer renderer in preview.GetComponentsInChildren<MeshRenderer>(true)) Collect(renderer,preview.transform,"category-"+(i+1),camera,corners,info);
            }
        }
        static void Collect(MeshRenderer renderer,Transform previewRoot,string variant,Camera camera,List<Vector3> corners,SceneInfo info)
        {
            var item=new MeshInfo { path=PathOf(renderer.transform),variant=variant,worldBounds=renderer.bounds,active=renderer.gameObject.activeInHierarchy }; info.meshes.Add(item);
            MeshFilter mesh=renderer.GetComponent<MeshFilter>();
            if(!renderer.enabled) item.excluded="disabled renderer";
            else if(mesh==null||mesh.sharedMesh==null) item.excluded="no visible mesh";
            else if(previewRoot==null&&!renderer.gameObject.activeInHierarchy) item.excluded="inactive source branch";
            else if(previewRoot!=null&&!ActiveWithin(renderer.transform,previewRoot)) item.excluded="inactive prefab branch";
            else if(renderer.name=="custom_plane"||item.path.Contains("/fondoz")||item.path.StartsWith("Sphere (1)/",StringComparison.Ordinal)) item.excluded="background or navigation geometry";
            else if(renderer.bounds.size.magnitude>5000) item.excluded="background plane extent";
            if(!String.IsNullOrEmpty(item.excluded)) return;
            Bounds bounds=mesh.sharedMesh.bounds;
            for(int x=-1;x<=1;x+=2) for(int y=-1;y<=1;y+=2) for(int z=-1;z<=1;z+=2)
                corners.Add(renderer.transform.TransformPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z))));
        }
        static bool ActiveWithin(Transform child,Transform root)
        {
            for(Transform node=child;node!=null;node=node.parent)
            { if(!node.gameObject.activeSelf) return false; if(node==root) return true; }
            return false;
        }
        static void MeasureFit(Camera camera,List<Vector3> corners,SceneInfo info)
        {
            float tanY=Mathf.Tan(camera.fieldOfView*.5f*Mathf.Deg2Rad),tanX=tanY*info.projectionAspect;
            Vector2 min=new Vector2(float.PositiveInfinity,float.PositiveInfinity),max=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
            foreach(Vector3 world in corners)
            {
                Vector3 p=Quaternion.Inverse(camera.transform.rotation)*(world-camera.transform.position);
                Vector2 point=new Vector2(.5f+p.x/p.z/tanX*.5f,.5f+p.y/p.z/tanY*.5f); min=Vector2.Min(min,point); max=Vector2.Max(max,point);
                if(point.x<info.viewport.xMin-.0001f||point.x>info.viewport.xMax+.0001f||point.y<info.viewport.yMin-.0001f||point.y>info.viewport.yMax+.0001f||p.z<camera.nearClipPlane||p.z>camera.farClipPlane) info.viewportViolations++;
            }
            info.projectedMin=min; info.projectedMax=max; info.geometryFits=info.viewportViolations==0;
        }
        static void Fit(Camera camera,List<Vector3> corners,Rect area)
        {
            if(camera.orthographic) throw new InvalidOperationException("Showcase camera must retain perspective.");
            float tanY=Mathf.Tan(camera.fieldOfView*.5f*Mathf.Deg2Rad),tanX=tanY*FitAspect;
            float left=(2*area.xMin-1)*tanX,right=(2*area.xMax-1)*tanX,bottom=(2*area.yMin-1)*tanY,top=(2*area.yMax-1)*tanY;
            Vector4 limits=new Vector4(float.NegativeInfinity,float.PositiveInfinity,float.NegativeInfinity,float.PositiveInfinity);
            float nearest=float.PositiveInfinity,farthest=float.NegativeInfinity;
            foreach(Vector3 world in corners)
            {
                Vector3 p=Quaternion.Inverse(camera.transform.rotation)*(world-camera.transform.position);
                limits.x=Mathf.Max(limits.x,p.x-right*p.z); limits.y=Mathf.Min(limits.y,p.x-left*p.z);
                limits.z=Mathf.Max(limits.z,p.y-top*p.z); limits.w=Mathf.Min(limits.w,p.y-bottom*p.z);
                nearest=Mathf.Min(nearest,p.z); farthest=Mathf.Max(farthest,p.z);
            }
            float distance=Mathf.Max((limits.x-limits.y)/(right-left),(limits.z-limits.w)/(top-bottom),camera.nearClipPlane+1-nearest)+.01f;
            float shiftX=(limits.x-right*distance+limits.y-left*distance)*.5f,shiftY=(limits.z-top*distance+limits.w-bottom*distance)*.5f;
            camera.transform.position+=camera.transform.rotation*new Vector3(shiftX,shiftY,-distance);
            camera.farClipPlane=Mathf.Max(camera.farClipPlane,farthest+distance+10);
            EditorUtility.SetDirty(camera.transform); EditorUtility.SetDirty(camera);
        }
        static string ProtectedFingerprint(Scene scene,Camera camera,HashSet<Transform> allowedTransforms=null)
        {
            var text=new StringBuilder();
            foreach(GameObject root in scene.GetRootGameObjects())
                foreach(Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    GameObject go=transform.gameObject; text.Append(PathOf(transform)).Append('|').Append(transform.GetSiblingIndex()).Append('|').Append(go.activeSelf).Append('|').Append(go.layer).Append('|').Append(go.tag).AppendLine();
                    foreach(Component component in go.GetComponents<Component>())
                    {
                        text.Append(component==null?"missing":component.GetType().FullName).Append('|');
                        bool drivenCanvas=component is RectTransform&&component.GetComponent<Canvas>()!=null&&component.GetComponent<Canvas>().renderMode==RenderMode.ScreenSpaceCamera&&component.GetComponent<Canvas>().worldCamera==camera;
                        bool cameraPose=camera!=null&&component==camera.transform;
                        bool allowedPose=component is Transform&&allowedTransforms!=null&&allowedTransforms.Contains((Transform)component);
                        if(component!=null&&component!=camera&&!cameraPose&&!drivenCanvas&&!allowedPose) text.Append(EditorJsonUtility.ToJson(component));
                        text.AppendLine();
                    }
                }
            using(var hash=SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()))).Replace("-","");
        }
        static string PathOf(Transform transform) { return transform.parent==null?transform.name:PathOf(transform.parent)+"/"+transform.name; }
        static void Backup(string path)
        {
            string copy=Path.Combine(Project,"Validation/Portrait/Before",path); Directory.CreateDirectory(Path.GetDirectoryName(copy));
            if(!File.Exists(copy)) File.Copy(Path.Combine(Project,path),copy);
        }
    }
}
#endif
