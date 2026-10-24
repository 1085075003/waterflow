#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Recovered.FlowWater.Editor
{
    // Observation only. Called from the existing original-collider query scope.
    public static class FlowWaterNativeTargetColliderAudit
    {
        static Transform capturedRoot;
        [Serializable] sealed class Item
        {
            public string path, type, scene, tag, prefab;
            public int record=-1, layer;
            public bool enabled, activeSelf, activeInHierarchy, trigger, inCapture, inPieceRoot, cameraScene;
            public bool geometryEnabledKnown, geometryEnabled;
            public Vector3 position, localPosition, euler, localScale, lossyScale, boundsCenter, boundsSize;
            public Vector3 boxCenter, boxSize, boxWorldCenter, sphereCenter;
            public float sphereRadius;
            public string mesh;
            public int meshVertices;
        }
        [Serializable] sealed class Report
        {
            public string startedUtc, finishedUtc, unityVersion, cameraScene, pieceRootScene;
            public bool observationOnly=true, sceneMismatchObserved;
            public int id, frame, nativeScopedCandidatesExcludedByOldSceneFilter;
            public Vector3 target;
            public List<string> movement=new List<string>();
            public List<Item> colliders=new List<Item>();
        }
        public static void WriteOnce(RecoveredFlowGame game,Vector3 target)
        {
            if(game==null || game.CurrentLevelId!=2033 || game.PieceRoot==null || capturedRoot==game.PieceRoot) return;
            capturedRoot=game.PieceRoot;
            var report=new Report { startedUtc=DateTime.UtcNow.ToString("o"),unityVersion=Application.unityVersion,
                id=game.CurrentLevelId,frame=Time.frameCount,target=target,cameraScene=game.GameCamera.gameObject.scene.name,
                pieceRootScene=game.PieceRoot.gameObject.scene.name };
            foreach(MoveBloque move in UnityEngine.Object.FindObjectsOfType<MoveBloque>(true))
                if(move.gameObject.scene==game.GameCamera.gameObject.scene)
                    report.movement.Add("scene="+move.gameObject.scene.name+",minX2="+move.minX2+",maxX2="+move.maxX2+",minZ2="+move.minZ2+",maxZ2="+move.maxZ2+",subeRayo="+move.subeRayo+",radius="+move.sphereRadius+",sueloMask="+move.mascaraSuelo.value+",suelo1Mask="+move.mascaraSuelo1.value+",suelo2Mask="+move.mascaraSuelo2.value+",queriesHitTriggers="+Physics.queriesHitTriggers);
            foreach(Collider collider in UnityEngine.Object.FindObjectsOfType<Collider>(true))
            {
                Vector3 p=collider.transform.position;
                if(Mathf.Abs(p.x-target.x)>20 || Mathf.Abs(p.z-target.z)>20 || Mathf.Abs(p.y-target.y)>25) continue;
                RecoveryPiece owner=collider.GetComponentInParent<RecoveryPiece>();
                var item=new Item { path=PathOf(collider.transform),type=collider.GetType().Name,scene=collider.gameObject.scene.name,
                    tag=collider.tag,layer=collider.gameObject.layer,record=owner==null?-1:owner.SourceRecordIndex,prefab=owner==null?"":owner.BasePrefab,
                    enabled=collider.enabled,activeSelf=collider.gameObject.activeSelf,activeInHierarchy=collider.gameObject.activeInHierarchy,trigger=collider.isTrigger,
                    inCapture=owner!=null && owner.OriginalCaptureColliders!=null && owner.OriginalCaptureColliders.Contains(collider),
                    inPieceRoot=collider.transform.IsChildOf(game.PieceRoot),cameraScene=collider.gameObject.scene==game.GameCamera.gameObject.scene,
                    position=p,localPosition=collider.transform.localPosition,euler=collider.transform.eulerAngles,
                    localScale=collider.transform.localScale,lossyScale=collider.transform.lossyScale,boundsCenter=collider.bounds.center,boundsSize=collider.bounds.size };
                bool originalEnabled;
                item.geometryEnabledKnown=RecoveredNativeGeometry.TryGetOriginalColliderEnabled(collider,out originalEnabled);
                if(item.geometryEnabledKnown) item.geometryEnabled=originalEnabled;
                BoxCollider box=collider as BoxCollider;
                if(box!=null) { item.boxCenter=box.center;item.boxSize=box.size;item.boxWorldCenter=box.transform.TransformPoint(box.center); }
                SphereCollider sphere=collider as SphereCollider;
                if(sphere!=null) { item.sphereCenter=sphere.center;item.sphereRadius=sphere.radius; }
                MeshCollider mesh=collider as MeshCollider;
                if(mesh!=null && mesh.sharedMesh!=null) { item.mesh=mesh.sharedMesh.name;item.meshVertices=mesh.sharedMesh.vertexCount; }
                if(item.inPieceRoot && !item.cameraScene && item.enabled && item.activeInHierarchy)
                { report.sceneMismatchObserved=true;report.nativeScopedCandidatesExcludedByOldSceneFilter++; }
                report.colliders.Add(item);
            }
            report.finishedUtc=DateTime.UtcNow.ToString("o");
            string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Validation","Save1Gameplay"));
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory,"gm2033-colliders.json"),JsonUtility.ToJson(report,true));
        }
        static string PathOf(Transform transform)
        {
            var names=new List<string>();while(transform!=null) { names.Add(transform.name);transform=transform.parent; }
            names.Reverse();return String.Join("/",names);
        }
    }
}
#endif
