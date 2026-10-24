using System.Collections.Generic;
using Recovered.FlowWater;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Presentation-only framing safeguard. Keeps the authored perspective and
/// rotation; portrait views fit the visible board between the header and tools.
/// </summary>
[RequireComponent(typeof(Camera))]
public sealed class RecoveredBoardFraming : MonoBehaviour
{
    public float LastBackwardDistance { get; private set; }
    public int MeshCount { get; private set; }
    public Rect PlayfieldViewport { get; private set; }
    const float EdgeMargin = .015f;
    Camera view;
    Transform pieceRoot;
    Vector3 authoredLocalPosition;
    Quaternion authoredLocalRotation;
    float authoredFov;
    float authoredFarClip;
    Vector3 basePosition, lastAppliedPosition;
    Quaternion lastAppliedRotation;
    bool hasApplied;
    Vector3 lastOffsetLocal;
    Renderer[] pieces = new Renderer[0];
    readonly List<Renderer> ground = new List<Renderer>();
    float refreshAt;
    Canvas presentationCanvas;
    float authoredCanvasDistance;
    CameraClearFlags authoredClearFlags;
    Color authoredClearColor;
    readonly Dictionary<Image,bool> backdropImages=new Dictionary<Image,bool>();
    readonly Dictionary<Renderer,bool> backdropRenderers=new Dictionary<Renderer,bool>();
    readonly List<Renderer> orderedBackdropRenderers=new List<Renderer>();
    readonly Dictionary<Image,Mesh> backdropImageMeshes=new Dictionary<Image,Mesh>();
    MaterialPropertyBlock backdropProperties;
    readonly List<string> renderedBackdropPaths=new List<string>();
    public int BackgroundGeometryLayers { get; private set; }
    public int BackgroundImageLayers { get; private set; }
    public int RemainingForegroundBackgroundLayers { get; private set; }
    public string[] BackgroundLayerPaths { get { return renderedBackdropPaths.ToArray(); } }
    [System.Serializable] public sealed class BackgroundLayerInfo
    {
        public string path,material,texture; public Color materialTint,ambient,tint;
        public Vector4 textureTransform,screenFade;
    }
    readonly List<BackgroundLayerInfo> backgroundLayerInfo=new List<BackgroundLayerInfo>();
    public BackgroundLayerInfo[] BackgroundGeometryDiagnostics { get { return backgroundLayerInfo.ToArray(); } }
    readonly Dictionary<RectTransform,float> uiDepths=new Dictionary<RectTransform,float>();
    CommandBuffer backdropCommands;
    Material backdropMaterial;
    bool portraitUIInitialized;

    public static RecoveredBoardFraming Apply(Camera camera, Transform pieces)
    {
        if (camera == null || pieces == null) return null;
        var framing = camera.GetComponent<RecoveredBoardFraming>();
        if (framing == null) framing = camera.gameObject.AddComponent<RecoveredBoardFraming>();
        framing.Begin(pieces);
        return framing;
    }
    void Awake()
    {
        view=GetComponent<Camera>();
        backdropProperties=new MaterialPropertyBlock();
        authoredLocalPosition=transform.localPosition;
        authoredLocalRotation=transform.localRotation;
        authoredFov=view.fieldOfView;
        authoredFarClip=view.farClipPlane;
        authoredClearFlags=view.clearFlags; authoredClearColor=view.backgroundColor;
    }
    void Begin(Transform current)
    {
        pieceRoot=current; LastBackwardDistance=0;
        transform.localPosition=authoredLocalPosition; transform.localRotation=authoredLocalRotation;
        view.fieldOfView=authoredFov;
        view.farClipPlane=authoredFarClip;
        basePosition=transform.position; hasApplied=false; lastOffsetLocal=Vector3.zero;
        ground.Clear();
        Scene scene=SceneManager.GetActiveScene();
        RestorePortraitUI(); presentationCanvas=null; backdropImages.Clear(); backdropRenderers.Clear(); orderedBackdropRenderers.Clear(); uiDepths.Clear();
        foreach(Mesh mesh in backdropImageMeshes.Values) if(mesh!=null) Destroy(mesh); backdropImageMeshes.Clear();
        foreach(GameObject root in scene.GetRootGameObjects())
            if(root.name=="Canvas")
            {
                presentationCanvas=root.GetComponent<Canvas>();
                if(presentationCanvas==null) continue;
                authoredCanvasDistance=presentationCanvas.planeDistance;
                foreach(RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
                    if(rect!=root.transform) uiDepths[rect]=rect.localPosition.z;
                // A source background is a complete branch, not just its root
                // Image. Night themes also contain opaque mountain/cloud meshes.
                foreach(Transform branch in root.transform)
                    if(branch.name.StartsWith("fondo",System.StringComparison.OrdinalIgnoreCase))
                    {
                        foreach(Image image in branch.GetComponentsInChildren<Image>(true)) backdropImages[image]=image.enabled;
                        foreach(Renderer renderer in branch.GetComponentsInChildren<Renderer>(true))
                        { backdropRenderers[renderer]=renderer.enabled; orderedBackdropRenderers.Add(renderer); }
                    }
            }
        foreach(ConfigSuelo floor in OriginalScenePresentation.Components<ConfigSuelo>(scene))
        {
            Renderer renderer=floor.GetComponent<Renderer>(); if(renderer!=null) ground.Add(renderer);
            if(floor.tierra!=null) ground.AddRange(floor.tierra.GetComponentsInChildren<Renderer>(true));
        }
        foreach(GameObject root in scene.GetRootGameObjects())
            if(root.name=="Recovered original underground border") ground.AddRange(root.GetComponentsInChildren<Renderer>(true));
        refreshAt=0; FitNow();
    }
    void OnPreCull() { FitNow(); }
    // The camera must settle before the camera-space Canvas rebuilds its mesh.
    void LateUpdate() { FitNow(); }
    void OnDisable() { RestorePortraitUI(); }
    void OnDestroy()
    {
        RestorePortraitUI();
        if(backdropMaterial!=null) Destroy(backdropMaterial);
        foreach(Mesh mesh in backdropImageMeshes.Values) if(mesh!=null) Destroy(mesh); backdropImageMeshes.Clear();
    }
    public void FitNow()
    {
        if(view==null || pieceRoot==null || view.orthographic) return;
        // Parent rotation carries the presentation offset with it. Remove that
        // offset in the new camera orientation before fitting the next pose.
        if(hasApplied && ((transform.position-lastAppliedPosition).sqrMagnitude>.000001f || Quaternion.Angle(transform.rotation,lastAppliedRotation)>.0001f))
            basePosition=transform.position-transform.rotation*lastOffsetLocal;
        transform.position=basePosition;
        LastBackwardDistance=0;
        lastOffsetLocal=Vector3.zero;
        if(Time.unscaledTime>=refreshAt)
        { pieces=pieceRoot.GetComponentsInChildren<Renderer>(true); refreshAt=Time.unscaledTime+.25f; }
        if(view.aspect<1)
        {
            FitPortrait();
            UpdatePortraitUI();
            RecordAppliedPose();
            return;
        }
        PlayfieldViewport=new Rect(EdgeMargin,EdgeMargin,1-2*EdgeMargin,1-2*EdgeMargin);
        RestorePortraitUI();
        float tanY=Mathf.Tan(view.fieldOfView*.5f*Mathf.Deg2Rad)*(1-2*EdgeMargin);
        float tanX=tanY*view.aspect;
        float backward=0; MeshCount=0;
        foreach(Renderer renderer in pieces) Measure(renderer,tanX,tanY,ref backward);
        foreach(Renderer renderer in ground) Measure(renderer,tanX,tanY,ref backward);
        if(backward>.0001f) { lastOffsetLocal=new Vector3(0,0,-backward); transform.position+=transform.rotation*lastOffsetLocal; LastBackwardDistance=backward; }
        RecordAppliedPose();
    }
    void UpdatePortraitUI()
    {
        if(presentationCanvas==null||presentationCanvas.renderMode!=RenderMode.ScreenSpaceCamera) return;
        // Keep the authored Image available if its replacement shader has not
        // imported yet; never disable both background drawing paths at once.
        if(backdropMaterial==null)
        {
            Shader shader=Shader.Find("Hidden/FlowWater/PortraitBackdrop");
            if(shader==null||!shader.isSupported) return;
            backdropMaterial=new Material(shader) { name="Portrait background presentation",hideFlags=HideFlags.DontSave };
        }
        bool layoutChanged=!portraitUIInitialized;
        float frontDistance=view.nearClipPlane+30;
        if(Mathf.Abs(presentationCanvas.planeDistance-frontDistance)>.001f) { presentationCanvas.planeDistance=frontDistance; layoutChanged=true; }
        foreach(var item in uiDepths)
            if(item.Key!=null&&Mathf.Abs(item.Key.localPosition.z)>.0001f)
            { Vector3 position=item.Key.localPosition; position.z=0; item.Key.localPosition=position; layoutChanged=true; }
        Image selected=null;
        foreach(var item in backdropImages)
        {
            Image image=item.Key; if(image==null) continue;
            if(image.transform.parent==presentationCanvas.transform&&item.Value&&image.gameObject.activeInHierarchy&&image.color.a>0) selected=image;
            image.enabled=false;
        }
        foreach(var item in backdropRenderers) if(item.Key!=null) item.Key.enabled=false;
        Texture texture=Texture2D.whiteTexture; Rect uv=new Rect(0,0,1,1); Color tint=authoredClearColor;
        if(selected!=null)
        {
            tint=selected.color;
            if(selected.sprite!=null)
            {
                texture=selected.sprite.texture; Rect pixels=selected.sprite.textureRect;
                uv=new Rect(pixels.x/texture.width,pixels.y/texture.height,pixels.width/texture.width,pixels.height/texture.height);
            }
        }
        backdropMaterial.SetColor("_Tint",tint); backdropMaterial.SetVector("_UVRect",new Vector4(uv.x,uv.y,uv.width,uv.height));
        if(backdropCommands==null)
        { backdropCommands=new CommandBuffer { name="Portrait backdrop before board geometry" }; view.AddCommandBuffer(CameraEvent.BeforeForwardOpaque,backdropCommands); }
        if(layoutChanged) Canvas.ForceUpdateCanvases();
        backdropCommands.Clear(); backdropCommands.Blit(texture,BuiltinRenderTextureType.CameraTarget,backdropMaterial,0);
        // Blit uses screen-space matrices. Restore the authored camera matrices
        // explicitly before drawing the original background geometry.
        backdropCommands.SetViewProjectionMatrices(view.worldToCameraMatrix,view.projectionMatrix);
        BackgroundGeometryLayers=0; BackgroundImageLayers=selected==null?0:1; renderedBackdropPaths.Clear(); backgroundLayerInfo.Clear();
        if(selected!=null) renderedBackdropPaths.Add(BackdropPath(selected.transform));
        // Far-to-near preserves the original branch's layered mountain/cloud
        // silhouettes. Original textures, UV animation, tint and materials stay.
        orderedBackdropRenderers.Sort((a,b)=>
        {
            if(a==null) return 1; if(b==null) return -1;
            return view.WorldToViewportPoint(b.bounds.center).z.CompareTo(view.WorldToViewportPoint(a.bounds.center).z);
        });
        foreach(Renderer renderer in orderedBackdropRenderers)
        {
            if(renderer==null||!backdropRenderers[renderer]||!renderer.gameObject.activeInHierarchy) continue;
            MeshFilter filter=renderer.GetComponent<MeshFilter>(); Material[] materials=renderer.sharedMaterials;
            if(filter!=null&&filter.sharedMesh!=null)
            {
                for(int i=0;i<Mathf.Min(filter.sharedMesh.subMeshCount,materials.Length);i++)
                    if(materials[i]!=null)
                    {
                        DrawBackdropGeometry(renderer,filter.sharedMesh,materials[i],i);
                    }
            }
            else
            {
                for(int i=0;i<materials.Length;i++) if(materials[i]!=null) backdropCommands.DrawRenderer(renderer,materials[i],i,0);
            }
            BackgroundGeometryLayers++; renderedBackdropPaths.Add(BackdropPath(renderer.transform));
        }
        foreach(var item in backdropImages)
        {
            Image image=item.Key;
            if(image==null||image==selected||!item.Value||!image.gameObject.activeInHierarchy||image.color.a<=0) continue;
            DrawBackdropImage(image); BackgroundImageLayers++; renderedBackdropPaths.Add(BackdropPath(image.transform));
        }
        // Some original background shaders are Opaque/ZWrite On. Retain their
        // colour composition but reset their depth before any board is drawn.
        backdropCommands.ClearRenderTarget(true,false,Color.clear);
        RemainingForegroundBackgroundLayers=0;
        foreach(var item in backdropImages) if(item.Key!=null&&item.Key.enabled&&item.Key.gameObject.activeInHierarchy) RemainingForegroundBackgroundLayers++;
        foreach(var item in backdropRenderers) if(item.Key!=null&&item.Key.enabled&&item.Key.gameObject.activeInHierarchy) RemainingForegroundBackgroundLayers++;
        view.clearFlags=CameraClearFlags.SolidColor;
        portraitUIInitialized=true;
    }
    void DrawBackdropGeometry(Renderer renderer,Mesh mesh,Material source,int submesh)
    {
        // Surface-shader forward passes require Unity's normal per-renderer
        // lighting setup. The background composition is deliberately unlit and
        // carries the authored texture/UV/tint plus explicit scene ambience.
        backdropProperties.Clear(); renderer.GetPropertyBlock(backdropProperties);
        int colorId=Shader.PropertyToID("_Color"),textureId=Shader.PropertyToID("_MainTex"),transformId=Shader.PropertyToID("_MainTex_ST");
        Color sourceTint=source.HasProperty(colorId)?source.GetColor(colorId):Color.white;
        if(backdropProperties.HasColor(colorId)) sourceTint=backdropProperties.GetColor(colorId);
        Texture texture=source.HasProperty(textureId)?source.GetTexture(textureId):null;
        if(backdropProperties.HasTexture(textureId)) texture=backdropProperties.GetTexture(textureId);
        if(texture==null) texture=Texture2D.whiteTexture;
        Vector2 scale=source.HasProperty(textureId)?source.mainTextureScale:Vector2.one;
        Vector2 offset=source.HasProperty(textureId)?source.mainTextureOffset:Vector2.zero;
        Vector4 uv=new Vector4(scale.x,scale.y,offset.x,offset.y);
        if(backdropProperties.HasVector(transformId)) uv=backdropProperties.GetVector(transformId);
        Color ambient=RenderSettings.ambientLight;
        Color tint=new Color(sourceTint.r*ambient.r,sourceTint.g*ambient.g,sourceTint.b*ambient.b,sourceTint.a);
        if(source.HasProperty("_ArtTint"))
        { Color art=source.GetColor("_ArtTint"); tint.r*=art.r; tint.g*=art.g; tint.b*=art.b; }
        Vector4 fade=Vector4.zero;
        if(texture.name.StartsWith("grad",System.StringComparison.OrdinalIgnoreCase))
        {
            float bottom=float.PositiveInfinity; Bounds bounds=mesh.bounds;
            for(int x=-1;x<=1;x+=2) for(int y=-1;y<=1;y+=2) for(int z=-1;z<=1;z+=2)
            {
                Vector3 world=renderer.localToWorldMatrix.MultiplyPoint3x4(bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z)));
                bottom=Mathf.Min(bottom,view.WorldToViewportPoint(world).y);
            }
            // Fade only the newly exposed rectangular bottom of a mountain
            // plane; its original ridge silhouette remains crisp.
            fade=new Vector4(bottom,.10f,1,0);
        }
        backdropProperties.Clear(); backdropProperties.SetTexture("_MainTex",texture); backdropProperties.SetColor("_Tint",tint);
        backdropProperties.SetVector("_MainTex_ST",uv); backdropProperties.SetVector("_LayerScreenFade",fade);
        backdropProperties.SetFloat("_ViewVerticalScale",.5f/Mathf.Tan(view.fieldOfView*.5f*Mathf.Deg2Rad));
        backdropCommands.DrawMesh(mesh,renderer.localToWorldMatrix,backdropMaterial,submesh,2,backdropProperties);
        backgroundLayerInfo.Add(new BackgroundLayerInfo { path=BackdropPath(renderer.transform),material=source.name,texture=texture.name,
            materialTint=sourceTint,ambient=ambient,tint=tint,textureTransform=uv,screenFade=fade });
    }
    void DrawBackdropImage(Image image)
    {
        Mesh mesh;
        if(!backdropImageMeshes.TryGetValue(image,out mesh))
        { mesh=new Mesh { name="Portrait background image quad",hideFlags=HideFlags.DontSave }; backdropImageMeshes[image]=mesh; }
        Rect rect=image.rectTransform.rect;
        mesh.vertices=new[]{new Vector3(rect.xMin,rect.yMin,0),new Vector3(rect.xMax,rect.yMin,0),new Vector3(rect.xMax,rect.yMax,0),new Vector3(rect.xMin,rect.yMax,0)};
        mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up}; mesh.triangles=new[]{0,1,2,0,2,3};
        Texture texture=Texture2D.whiteTexture; Rect uv=new Rect(0,0,1,1);
        if(image.sprite!=null)
        { texture=image.sprite.texture; Rect pixels=image.sprite.textureRect; uv=new Rect(pixels.x/texture.width,pixels.y/texture.height,pixels.width/texture.width,pixels.height/texture.height); }
        backdropProperties.Clear(); backdropProperties.SetTexture("_MainTex",texture); backdropProperties.SetColor("_Tint",image.color);
        backdropProperties.SetVector("_UVRect",new Vector4(uv.x,uv.y,uv.width,uv.height));
        backdropCommands.DrawMesh(mesh,image.rectTransform.localToWorldMatrix,backdropMaterial,0,1,backdropProperties);
    }
    static string BackdropPath(Transform node) { return node.parent==null?node.name:BackdropPath(node.parent)+"/"+node.name; }
    void RestorePortraitUI()
    {
        foreach(var item in backdropImages) if(item.Key!=null) item.Key.enabled=item.Value;
        foreach(var item in backdropRenderers) if(item.Key!=null) item.Key.enabled=item.Value;
        foreach(var item in uiDepths) if(item.Key!=null)
        { Vector3 position=item.Key.localPosition; position.z=item.Value; item.Key.localPosition=position; }
        if(presentationCanvas!=null) presentationCanvas.planeDistance=authoredCanvasDistance;
        if(backdropCommands!=null)
        { if(view!=null) view.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque,backdropCommands); backdropCommands.Release(); backdropCommands=null; }
        if(view!=null) { view.clearFlags=authoredClearFlags; view.backgroundColor=authoredClearColor; }
        portraitUIInitialized=false;
    }
    void FitPortrait()
    {
        // Canvas width is 720 units. Reserve the actual HUD height, so the
        // board gains space on a tall phone and stays clear on a 3:4 tablet.
        float canvasHeight=720/Mathf.Max(.1f,view.aspect);
        float bottom=240/canvasHeight, top=1-144/canvasHeight;
        if(Screen.height>0)
        {
            Rect safe=Screen.safeArea;
            if(safe.height>0) { bottom=Mathf.Max(bottom,safe.yMin/Screen.height+.02f); top=Mathf.Min(top,safe.yMax/Screen.height-.025f); }
        }
        PlayfieldViewport=Rect.MinMaxRect(.045f,bottom,.955f,Mathf.Max(bottom+.30f,top));
        float tanY=Mathf.Tan(view.fieldOfView*.5f*Mathf.Deg2Rad), tanX=tanY*view.aspect;
        float left=(PlayfieldViewport.xMin*2-1)*tanX, right=(PlayfieldViewport.xMax*2-1)*tanX;
        float low=(PlayfieldViewport.yMin*2-1)*tanY, high=(PlayfieldViewport.yMax*2-1)*tanY;
        Vector4 limits=new Vector4(float.NegativeInfinity,float.PositiveInfinity,float.NegativeInfinity,float.PositiveInfinity);
        float nearest=float.PositiveInfinity, farthest=float.NegativeInfinity; MeshCount=0;
        foreach(Renderer renderer in pieces) MeasurePortrait(renderer,left,right,low,high,ref limits,ref nearest,ref farthest);
        foreach(Renderer renderer in ground) MeasurePortrait(renderer,left,right,low,high,ref limits,ref nearest,ref farthest);
        if(MeshCount==0) return;
        // Intersect the admissible camera translation intervals for every
        // mesh corner. Their shared slopes give the minimum backward distance.
        float distance=Mathf.Max(0,(limits.x-limits.y)/(right-left),(limits.z-limits.w)/(high-low),view.nearClipPlane+.01f-nearest);
        float shiftX=(limits.x-right*distance+limits.y-left*distance)*.5f;
        float shiftY=(limits.z-high*distance+limits.w-low*distance)*.5f;
        lastOffsetLocal=new Vector3(shiftX,shiftY,-distance);
        transform.position=basePosition+transform.rotation*lastOffsetLocal;
        view.farClipPlane=Mathf.Max(authoredFarClip,farthest+distance+1);
        LastBackwardDistance=distance;
    }
    void MeasurePortrait(Renderer renderer,float left,float right,float low,float high,ref Vector4 limits,ref float nearest,ref float farthest)
    {
        if(!IsStructuralRenderer(renderer)) return;
        MeshFilter mesh=renderer.GetComponent<MeshFilter>(); Bounds bounds=mesh.sharedMesh.bounds; MeshCount++;
        for(int x=-1;x<=1;x+=2) for(int y=-1;y<=1;y+=2) for(int z=-1;z<=1;z+=2)
        {
            Vector3 corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z));
            Vector3 p=view.transform.InverseTransformPoint(mesh.transform.TransformPoint(corner));
            limits.x=Mathf.Max(limits.x,p.x-right*p.z); limits.y=Mathf.Min(limits.y,p.x-left*p.z);
            limits.z=Mathf.Max(limits.z,p.y-high*p.z); limits.w=Mathf.Min(limits.w,p.y-low*p.z);
            nearest=Mathf.Min(nearest,p.z);
            farthest=Mathf.Max(farthest,p.z);
        }
    }
    void RecordAppliedPose() { lastAppliedPosition=transform.position; lastAppliedRotation=transform.rotation; hasApplied=true; }
    public static bool IsStructuralRenderer(Renderer renderer)
    {
        if(renderer==null || !renderer.enabled || !renderer.gameObject.activeInHierarchy || !(renderer is MeshRenderer)) return false;
        RecoveryPiece owner=renderer.GetComponentInParent<RecoveryPiece>();
        if(owner!=null && RecoveredNativeDragMotion.IsMoving(owner)) return false;
        for(Transform node=renderer.transform;node!=null;node=node.parent)
            if(node.name.StartsWith("Original water",System.StringComparison.Ordinal)) return false;
        MeshFilter mesh=renderer.GetComponent<MeshFilter>(); return mesh!=null && mesh.sharedMesh!=null;
    }
    void Measure(Renderer renderer,float tanX,float tanY,ref float backward)
    {
        if(!IsStructuralRenderer(renderer)) return;
        MeshFilter mesh=renderer.GetComponent<MeshFilter>(); if(mesh==null || mesh.sharedMesh==null) return;
        Bounds bounds=mesh.sharedMesh.bounds;
        // Per-mesh local boxes avoid inventing empty corners in one huge world
        // AABB spanning an elevated source and a diagonally opposite low border.
        MeshCount++;
        for(int x=-1;x<=1;x+=2) for(int y=-1;y<=1;y+=2) for(int z=-1;z<=1;z+=2)
        {
            Vector3 corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z));
            Vector3 p=view.transform.InverseTransformPoint(mesh.transform.TransformPoint(corner));
            backward=Mathf.Max(backward,Mathf.Abs(p.x)/tanX-p.z);
            backward=Mathf.Max(backward,Mathf.Abs(p.y)/tanY-p.z);
            backward=Mathf.Max(backward,view.nearClipPlane+.01f-p.z);
        }
    }
}
