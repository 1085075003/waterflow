using System.Collections.Generic;
using Recovered.FlowWater;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Reconstructed desktop framing safeguard, not an original native method.
/// Keeps the authored perspective/rotation and moves only backward along its
/// viewing direction, by the minimum distance needed to contain visible board meshes clear of the original gameplay HUD.
/// </summary>
[RequireComponent(typeof(Camera))]
public sealed class RecoveredBoardFraming : MonoBehaviour
{
    public float LastBackwardDistance { get; private set; }
    public int MeshCount { get; private set; }
    [System.Serializable]
    public sealed class HudRegion
    {
        public string path;
        public Rect viewport;
    }
    const float EdgeMargin = .015f;
    readonly List<HudRegion> hudRegions = new List<HudRegion>();
    readonly List<Vector3[]> structuralBoxes = new List<Vector3[]>();
    readonly List<RectTransform> hudControls = new List<RectTransform>();
    Camera view;
    Transform pieceRoot;
    Vector3 authoredLocalPosition;
    Quaternion authoredLocalRotation;
    float authoredFov;
    Vector3 basePosition, lastAppliedPosition;
    Quaternion lastAppliedRotation;
    bool hasApplied;
    Renderer[] pieces = new Renderer[0];
    readonly List<Renderer> ground = new List<Renderer>();
    float refreshAt;

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
        authoredLocalPosition=transform.localPosition;
        authoredLocalRotation=transform.localRotation;
        authoredFov=view.fieldOfView;
    }
    void Begin(Transform current)
    {
        pieceRoot=current; LastBackwardDistance=0;
        transform.localPosition=authoredLocalPosition; transform.localRotation=authoredLocalRotation;
        view.fieldOfView=authoredFov;
        basePosition=transform.position; hasApplied=false;
        ground.Clear();
        Scene scene=SceneManager.GetActiveScene();
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
    public void FitNow()
    {
        if(view==null || pieceRoot==null || view.orthographic) return;
        // An external orbit/pan keeps the existing framing offset. Remove that
        // offset along the new view direction to recover the user's base pose.
        if(hasApplied && ((transform.position-lastAppliedPosition).sqrMagnitude>.000001f || Quaternion.Angle(transform.rotation,lastAppliedRotation)>.0001f))
            basePosition=transform.position+transform.forward*LastBackwardDistance;
        transform.position=basePosition;
        LastBackwardDistance=0;
        if(view.aspect<1) { RecordAppliedPose(); return; }
        if(Time.unscaledTime>=refreshAt)
        { pieces=pieceRoot.GetComponentsInChildren<Renderer>(true); refreshAt=Time.unscaledTime+.25f; }
        float tanY=Mathf.Tan(view.fieldOfView*.5f*Mathf.Deg2Rad)*(1-2*EdgeMargin);
        float tanX=tanY*view.aspect;
        float backward=0; MeshCount=0; structuralBoxes.Clear();
        foreach(Renderer renderer in pieces) Measure(renderer,tanX,tanY,ref backward);
        foreach(Renderer renderer in ground) Measure(renderer,tanX,tanY,ref backward);
        GetProtectedHudRegions(hudRegions);
        // A point moves toward the viewport centre when the camera retreats.
        // Clear each actual HUD rectangle toward that centre, rather than
        // inventing a fixed-height full-width band which would shrink clear levels.
        for(int pass=0;pass<=hudRegions.Count;pass++)
        {
            float previous=backward;
            foreach(HudRegion region in hudRegions)
                foreach(Vector3[] box in structuralBoxes)
                    if(OverlapsHud(box,region.viewport,backward))
                        backward=Mathf.Max(backward,ClearHud(box,region.viewport,backward));
            if(backward<=previous+.00001f) break;
        }
        if(backward>.0001f) { transform.position-=transform.forward*backward; LastBackwardDistance=backward; }
        RecordAppliedPose();
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
        var box=new Vector3[8]; int index=0;
        for(int x=-1;x<=1;x+=2) for(int y=-1;y<=1;y+=2) for(int z=-1;z<=1;z+=2)
        {
            Vector3 corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z));
            Vector3 p=view.transform.InverseTransformPoint(mesh.transform.TransformPoint(corner));
            box[index++]=p;
            backward=Mathf.Max(backward,Mathf.Abs(p.x)/tanX-p.z);
            backward=Mathf.Max(backward,Mathf.Abs(p.y)/tanY-p.z);
            backward=Mathf.Max(backward,view.nearClipPlane+.01f-p.z);
        }
        structuralBoxes.Add(box);
    }

    /// <summary>Only authored gameplay control references, never modal/tutorial roots.</summary>
    public void GetProtectedHudRegions(List<HudRegion> output)
    {
        output.Clear(); hudControls.Clear();
        Scene scene=gameObject.scene;
        foreach(SaveLoadManager loader in OriginalScenePresentation.Components<SaveLoadManager>(scene))
        {
            AddHudControl(loader.MenuButton); AddHudControl(loader.MusicButton);
            AddHudControl(loader.SoundButton); AddHudControl(loader.RestartButton);
            AddHudControl(loader.solutionButton); AddHudControl(loader.NextButton);
            AddHudControl(loader.backButton); AddHudControl(loader.backButton1);
            AddHudControl(loader.stepsButton); AddHudControl(loader.botonNumSol);
            AddHudControl(loader.barra==null ? null : loader.barra.gameObject);
            AddHudControl(loader.barra1==null ? null : loader.barra1.gameObject);
            // The source scene's panel2 draws the visible backdrop around these
            // controls. Stop at their lowest shared ancestor; reject screen-sized
            // wrappers below, even if a different original scene uses one.
            Transform common=CommonAncestor(loader.MusicButton,loader.SoundButton);
            if(common!=null) AddHudControl(common.gameObject);
        }
        foreach(RectTransform control in hudControls)
        {
            Rect rect;
            if(!TryHudViewportRect(control,out rect)) continue;
            float padX=2f/Mathf.Max(1,view.pixelWidth), padY=2f/Mathf.Max(1,view.pixelHeight);
            rect=Rect.MinMaxRect(Mathf.Max(0,rect.xMin-padX),Mathf.Max(0,rect.yMin-padY),
                Mathf.Min(1,rect.xMax+padX),Mathf.Min(1,rect.yMax+padY));
            output.Add(new HudRegion { path=Hierarchy(control),viewport=rect });
        }
    }
    void AddHudControl(GameObject item)
    {
        if(item==null || !item.activeInHierarchy) return;
        RectTransform rect=item.GetComponent<RectTransform>();
        if(rect!=null && !hudControls.Contains(rect)) hudControls.Add(rect);
    }
    static Transform CommonAncestor(GameObject a,GameObject b)
    {
        if(a==null || b==null) return null;
        for(Transform node=a.transform;node!=null;node=node.parent)
            if(b.transform.IsChildOf(node)) return node;
        return null;
    }
    bool TryHudViewportRect(RectTransform control,out Rect rect)
    {
        rect=new Rect(); Canvas canvas=control.GetComponentInParent<Canvas>();
        if(canvas==null || !canvas.isActiveAndEnabled) return false;
        Canvas root=canvas.rootCanvas;
        if(root.renderMode==RenderMode.WorldSpace) return false;
        Camera canvasCamera=root.renderMode==RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
        Rect pixels=root.pixelRect;
        if(pixels.width<=0 || pixels.height<=0) return false;
        var corners=new Vector3[4]; control.GetWorldCorners(corners);
        Vector2 low=new Vector2(float.PositiveInfinity,float.PositiveInfinity),high=-low;
        foreach(Vector3 corner in corners)
        {
            Vector2 screen=RectTransformUtility.WorldToScreenPoint(canvasCamera,corner);
            Vector2 normalized=new Vector2((screen.x-pixels.xMin)/pixels.width,(screen.y-pixels.yMin)/pixels.height);
            // Full-screen overlay coordinates and RT UI-camera coordinates both
            // normalize through the canvas's actual pixel rectangle.
            Vector2 viewport=new Vector2((normalized.x-view.rect.xMin)/view.rect.width,(normalized.y-view.rect.yMin)/view.rect.height);
            low=Vector2.Min(low,viewport); high=Vector2.Max(high,viewport);
        }
        rect=Rect.MinMaxRect(Mathf.Max(0,low.x),Mathf.Max(0,low.y),Mathf.Min(1,high.x),Mathf.Min(1,high.y));
        // Whitelisted controls can still reference a full-screen container.
        // Only the actual upper HUD, outside the centre, can constrain framing.
        return rect.width>0 && rect.height>0 && rect.width<.95f && rect.height<.4f && rect.yMin>.5f;
    }
    bool OverlapsHud(Vector3[] box,Rect hud,float backward)
    {
        float tanY=Mathf.Tan(view.fieldOfView*.5f*Mathf.Deg2Rad),tanX=tanY*view.aspect;
        Vector2 low=new Vector2(float.PositiveInfinity,float.PositiveInfinity),high=-low;
        foreach(Vector3 p in box)
        {
            float depth=p.z+backward;
            Vector2 point=new Vector2(.5f+p.x/(2*tanX*depth),.5f+p.y/(2*tanY*depth));
            low=Vector2.Min(low,point); high=Vector2.Max(high,point);
        }
        return low.x<hud.xMax && high.x>hud.xMin && low.y<hud.yMax && high.y>hud.yMin;
    }
    float ClearHud(Vector3[] box,Rect hud,float current)
    {
        float tanY=Mathf.Tan(view.fieldOfView*.5f*Mathf.Deg2Rad),tanX=tanY*view.aspect;
        float best=float.PositiveInfinity;
        if(hud.xMin>.5f) best=Mathf.Min(best,ClearToEdge(box,0,2*tanX*(hud.xMin-.5f),1,current));
        if(hud.xMax<.5f) best=Mathf.Min(best,ClearToEdge(box,0,2*tanX*(.5f-hud.xMax),-1,current));
        if(hud.yMin>.5f) best=Mathf.Min(best,ClearToEdge(box,1,2*tanY*(hud.yMin-.5f),1,current));
        if(hud.yMax<.5f) best=Mathf.Min(best,ClearToEdge(box,1,2*tanY*(.5f-hud.yMax),-1,current));
        return float.IsInfinity(best) ? current : best+.01f;
    }
    static float ClearToEdge(Vector3[] box,int axis,float divisor,float sign,float current)
    {
        foreach(Vector3 p in box) current=Mathf.Max(current,sign*p[axis]/divisor-p.z);
        return current;
    }
    static string Hierarchy(Transform node)
    {
        string path=node.name;
        while(node.parent!=null) { node=node.parent; path=node.name+"/"+path; }
        return path;
    }
}
