using System.Collections.Generic;
using Recovered.FlowWater;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Reconstructed desktop framing safeguard, not an original native method.
/// Keeps the authored perspective/rotation and moves only backward along its
/// viewing direction, by the minimum distance needed to contain visible board meshes.
/// </summary>
[RequireComponent(typeof(Camera))]
public sealed class RecoveredBoardFraming : MonoBehaviour
{
    public float LastBackwardDistance { get; private set; }
    public int MeshCount { get; private set; }
    const float EdgeMargin = .015f;
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
        float backward=0; MeshCount=0;
        foreach(Renderer renderer in pieces) Measure(renderer,tanX,tanY,ref backward);
        foreach(Renderer renderer in ground) Measure(renderer,tanX,tanY,ref backward);
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
