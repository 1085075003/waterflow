using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Recovered.FlowWater
{
    // Original MoveBloque movement: doProcessMove, doBuscarCamino,
    // doCheckCamino, DoMoverArriba/Abajo, doMovimiento and doFijarPieza.
    // Grid landing is supplied by RecoveredNativeInteraction.
    public sealed class RecoveredNativeDragMotion : MonoBehaviour
    {
        const float GuideOffset = 4.199999809265137f;
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        static readonly HashSet<RecoveryPiece> moving = new HashSet<RecoveryPiece>();
        readonly List<Vector3> route = new List<Vector3>();
        RecoveredFlowGame game;
        RecoveryPiece actor;
        Collider selectedCollider;
        MoveBloque original;
        Transform pieceRoot;
        Vector3 start, guide, oldGuide, pointerOffset, floatingTarget;
        Vector2 initialPointer;
        float increment, minHeight7, minHeight8;
        int routeIndex,upFirstStage=1;
        bool active, released, pointerInitialized, guideMoved, escapeChecked, routeUp, fallback;
        Vector3 routeSource, routeSide;
        float routeBelow;
        Renderer hiddenRenderer;
        bool rendererWasEnabled;
        // doFijar102F10C..134 and doMovimiento restore cascades once below.5,
        // before the final snap/transaction. Subsequent frames must not re-gate.
        bool nativeWaterUnblocked;
        float nativeWaterRestoreDistance=-1;
        int nativeWaterRestoreFrame=-1;
        Action<int,Vector3> commit;
        object beforeState;
        Component gatedWater;
        MethodInfo gateMethod;
        Transform initialSupport,finalSupport;
        bool physicalSupport,supportMovesDown;
        public bool IsActive { get { return active; } }
        public bool IsReleased { get { return released; } }
        public Vector3 Target { get { return finalSupport!=null?finalSupport.position+Vector3.up*GuideOffset:guide+Vector3.up*GuideOffset; } }
        public object BeforeState { get { return beforeState; } }
        public int RouteStage { get { return route.Count == 0 ? 0 : routeIndex+(routeUp?upFirstStage:1); } }
        public bool UsesFixedRoute { get { return original != null && original.fijacionPieza != 0; } }
        public static bool IsMoving(RecoveryPiece piece) { return piece != null && moving.Contains(piece); }

        // Resorte.doBajarPieza100BDF4..BE30 clears both original movement
        // flags before placing the collider's parent at the compressed plate Y.
        // Finish this released movement once, so its Lerp cannot overwrite that
        // native placement or leave its water gate and undo transaction open.
        public static bool FinishPressureLanding(RecoveryPiece piece,Vector3 destination)
        {
            if(piece==null || RecoveredFlowGame.Instance==null) return false;
            var motion=RecoveredFlowGame.Instance.GetComponent<RecoveredNativeDragMotion>();
            if(motion==null || !motion.active || !motion.released || motion.actor!=piece) return false;
            motion.finalSupport=null; motion.guide=destination-Vector3.up*GuideOffset;
            motion.Complete();
            return true;
        }

        public static RecoveredNativeDragMotion Begin(RecoveredFlowGame game,RecoveryPiece piece,RaycastHit picked,Vector2 pointer)
        { return Begin(game,piece,picked.collider,pointer); }

        public static RecoveredNativeDragMotion Begin(RecoveredFlowGame game,RecoveryPiece piece,Collider collider,Vector2 pointer)
        {
            if (game == null || piece == null || collider == null) return null;
            var motion=game.GetComponent<RecoveredNativeDragMotion>();
            if (motion==null) motion=game.gameObject.AddComponent<RecoveredNativeDragMotion>();
            motion.Cancel(); motion.game=game; motion.actor=piece; motion.selectedCollider=collider;
            motion.pieceRoot=game.PieceRoot; motion.start=piece.transform.position;
            motion.guide=motion.oldGuide=motion.start-Vector3.up*GuideOffset;
            motion.floatingTarget=motion.start; motion.initialPointer=pointer;
            motion.beforeState=typeof(RecoveredFlowGame).GetMethod("CaptureSnapshot",Hidden).Invoke(game,null);
            foreach (MoveBloque candidate in UnityEngine.Object.FindObjectsOfType<MoveBloque>(true))
                if (candidate.gameObject.scene==game.GameCamera.gameObject.scene) { motion.original=candidate; break; }
            if (motion.original==null) { motion.actor=null; return null; }
            // UIManagerScript.Start10639F4..1063A40, absent in the export stub:
            // parts20..23 and32..39 enable the fixed-obstacle routing mode.
            int part=OriginalScenePresentation.PartForLevel(game.CurrentLevelId);
            motion.original.fijacionPieza=(part>=20 && part<=23)||(part>=32 && part<=39)?1:0;
            motion.active=true; moving.Add(piece);
            MoveBloque.MoviendoBloque=true; MoveBloque.fijarPieza=false;
            if (motion.original.planoGuia!=null)
            { motion.original.planoGuia.transform.position=motion.guide; motion.original.planoGuia.SetActive(true); }
            motion.GateWater(true);
            // doProcess1 is followed by doProcessMove in the same native Update.
            // Its second ray already ignores the selected layer2 body. Capture
            // that underlying surface now, before the first pointer movement.
            using(RecoveredNativeInteraction.QueryOriginalColliders(game))
            {
                motion.ReadInitialSupport();
                RaycastHit underneath;
                if(Physics.Raycast(game.GameCamera.ScreenPointToRay(pointer),out underneath,Mathf.Infinity,Physics.DefaultRaycastLayers,QueryTriggerInteraction.UseGlobal))
                    motion.SetTarget(motion.start,underneath,pointer);
            }
            return motion;
        }

        public void SetTarget(Vector3 landing,RaycastHit surface,Vector2 pointer)
        {
            if (!active || released || surface.collider==null) return;
            bool changed=false;
            if (!routeUp)
            {
                if (Mathf.Abs((pointer.x+pointer.y)-(initialPointer.x+initialPointer.y))>original.margen) guideMoved=true;
                Vector3 next=guideMoved?landing-Vector3.up*GuideOffset:guide;
                changed=(next-guide).sqrMagnitude>.001f;
                guide=next;
                using(RecoveredNativeInteraction.QueryOriginalColliders(game))
                {
                    bool mechanismSurface=surface.collider.gameObject.layer==LayerMask.NameToLayer("Suelo1") ||
                        Physics.CheckSphere(guide+Vector3.up*original.subeRayo,original.sphereRadius,original.mascaraSuelo1.value,QueryTriggerInteraction.UseGlobal);
                    if(physicalSupport && initialSupport!=null && mechanismSurface && AnyMechanismBusy())
                        guide=initialSupport.position+Vector3.up*(supportMovesDown?.4000000059604645f:0);
                }
                if (changed || !pointerInitialized)
                {
                    using (RecoveredNativeInteraction.QueryOriginalColliders(game)) CalculateHeight(guide);
                    float delta=guide.y-oldGuide.y;
                    float lift=delta>.20000000298023224f && delta<.800000011920929f ? .5f : 0;
                    if (!guideMoved && surface.collider.name.IndexOf("CuboResorte",StringComparison.Ordinal)>=0)
                        lift=lift>.1f?1.7000000476837158f:1.2000000476837158f;
                    if (original.planoGuia!=null) original.planoGuia.transform.position=guide+Vector3.up*lift;
                    if (original.planoGuia1!=null) original.planoGuia1.transform.position=guide;
                    oldGuide=guide;
                }
                else if (original.planoGuia!=null) original.planoGuia.transform.position=guide;
            }
            SetPointer(surface,pointer);
            if (!escapeChecked && pointerInitialized)
            {
                escapeChecked=true;
                if (UsesFixedRoute)
                    using (RecoveredNativeInteraction.QueryOriginalColliders(game))
                        if (FindRoute(start,true)) { routeUp=true; routeIndex=0; }
            }
        }

        public void SetPointer(RaycastHit surface,Vector2 pointer)
        {
            if (!active || released || surface.collider==null) return;
            Vector3 point=surface.point;
            point.x=Mathf.Clamp(point.x,original.minX,original.maxX);
            point.z=Mathf.Clamp(point.z,original.minZ,original.maxZ);
            if (!pointerInitialized)
            {
                pointerOffset=guide-surface.point+Vector3.up*35;
                pointerInitialized=true;
            }
            floatingTarget=point+pointerOffset;
            floatingTarget.y+=increment;
            if (!UsesFixedRoute) floatingTarget.y=Mathf.Min(floatingTarget.y,90);
            floatingTarget.y=Mathf.Max(floatingTarget.y,Mathf.Max(minHeight7,minHeight8));
        }

        public void Release(Action<int,Vector3> onCommit)
        {
            if (!active || released) return;
            released=true; commit=onCommit;
            RecoveredNativeInteraction.EndSelection(selectedCollider);
            MoveBloque.fijarPieza=true; MoveBloque.objetoCambiado=true;
            using(RecoveredNativeInteraction.QueryOriginalColliders(game)) ReadFinalSupport();
            if (routeUp && route.Count>0)
            {
                // doEnd remaps native paso1/2/3/4 to4/3/2/1.
                int upStage=upFirstStage+routeIndex; route.Clear(); routeUp=false;
                if (!fallback && upStage>=3) route.Add(new Vector3(routeSide.x,routeBelow,routeSide.z));
                if (!fallback && upStage>=2) route.Add(new Vector3(routeSource.x,routeBelow,routeSource.z));
                route.Add(Target); routeIndex=0;
            }
            else
            {
                route.Clear(); routeIndex=0;
                bool routed=false;
                if (UsesFixedRoute)
                    using (RecoveredNativeInteraction.QueryOriginalColliders(game)) routed=FindRoute(Target,false);
                if (!routed) route.Clear();
            }
        }

        void LateUpdate()
        {
            if (!active) return;
            if (actor==null || game==null || game.PieceRoot!=pieceRoot || !game.IsLevelActive)
            { Cancel(); return; }
            if(!nativeWaterUnblocked) GateWater(true);
            if (route.Count>0 && routeIndex<route.Count)
            {
                bool final=released && routeIndex==route.Count-1;
                Vector3 target=route[routeIndex];
                actor.transform.position=final?Vector3.Lerp(actor.transform.position,target,original.smoothSpeed1*Time.deltaTime):Vector3.MoveTowards(actor.transform.position,target,original.smoothSpeed2*Time.deltaTime);
                if(final) RestoreNativeWaterNearLanding();
                if (Vector3.Distance(actor.transform.position,target)<.10000000149011612f)
                {
                    actor.transform.position=target; routeIndex++;
                    if (routeIndex==route.Count)
                    {
                        route.Clear(); routeIndex=0; RestoreRenderer();
                        if (released) Complete(); else routeUp=false;
                    }
                }
            }
            else if (released)
            {
                // doFijarPieza uses Lerp speed2.6, exact snap below0.3.
                actor.transform.position=Vector3.Lerp(actor.transform.position,Target,original.smoothSpeed*Time.deltaTime);
                RestoreNativeWaterNearLanding();
                if (Vector3.Distance(actor.transform.position,Target)<.30000001192092896f)
                { actor.transform.position=Target; Complete(); }
            }
            else if (pointerInitialized)
                actor.transform.position=Vector3.Lerp(actor.transform.position,floatingTarget,original.smoothSpeed1*Time.deltaTime);
        }

        void RestoreNativeWaterNearLanding()
        {
            if(nativeWaterUnblocked || !released || actor==null) return;
            float distance=Vector3.Distance(actor.transform.position,Target);
            if(distance>=.5f) return;
            nativeWaterUnblocked=true; nativeWaterRestoreDistance=distance; nativeWaterRestoreFrame=Time.frameCount;
            GateWater(false);
            if(AudioManager.instance!=null && original!=null)
            {
                FieldInfo sound=typeof(MoveBloque).GetField("SoundBlock",Hidden);
                if(sound!=null) AudioManager.instance.PlaySound(sound.GetValue(original) as string);
            }
        }

        void CalculateHeight(Vector3 location)
        {
            int highest7=0,highest8=0;
            foreach (float x in new[] { -20f,0f,20f })
            foreach (float z in new[] { -20f,0f,20f })
            for(int level=4;level<=12;level++)
                foreach(Collider collider in Physics.OverlapSphere(new Vector3(location.x+x,level*original.arribay,location.z+z),original.sphereRadius,original.mascaraAlturaMax.value,QueryTriggerInteraction.UseGlobal))
                    if(collider.CompareTag("Map"))
                    {
                        if(collider.gameObject.layer==LayerMask.NameToLayer("Suelo1")) highest8=Mathf.Max(highest8,level);
                        else highest7=Mathf.Max(highest7,level);
                    }
            minHeight7=highest7>=4?(highest7+1)*original.arribay:0;
            minHeight8=highest8>4?(highest8+1)*original.arribay:0;
            int guideLevel=(int)Math.Round(location.y/original.arribay,MidpointRounding.ToEven);
            increment=Mathf.Max(0,(Mathf.Max(highest7,4)-guideLevel-4)*10);
        }

        static bool AnyMechanismBusy()
        { return UIManagerScript.activadoRes1 || UIManagerScript.activadoRes2 || UIManagerScript.activadoRes3 || UIManagerScript.activadoRes4 || UIManagerScript.activadoRes5 || UIManagerScript.activadoRes6 || UIManagerScript.activadoRes7 || UIManagerScript.activadoRes8; }

        void ReadInitialSupport()
        {
            initialSupport=null; physicalSupport=supportMovesDown=false; MoveBloque.conRes3=false;
            foreach(Collider collider in Physics.OverlapSphere(guide-Vector3.up*GuideOffset,original.sphereRadius,original.mascaraSuelo.value,QueryTriggerInteraction.UseGlobal))
            {
                if(collider.name.IndexOf("OrigenCentro",StringComparison.Ordinal)>=0) continue;
                initialSupport=collider.transform;
                RecoveryPiece piece=collider.GetComponentInParent<RecoveryPiece>(); if(piece==null) continue;
                string plate="",action="",name=piece.BasePrefab??"";
                if(name.IndexOf("BloqueFijo",StringComparison.Ordinal)>=0)
                { var source=piece.GetComponent<ControlFijo>(); if(source!=null) {plate=source.conResorte3;action=source.tipoResorte;} }
                else if(name.IndexOf("Bloque",StringComparison.Ordinal)>=0)
                { var source=piece.GetComponent<ControlPieza>(); if(source!=null) {plate=source.conResorte3;action=source.tipoResorte;} }
                if(!String.IsNullOrEmpty(plate)) { physicalSupport=true; MoveBloque.conRes3=true; supportMovesDown|=action=="8"; }
            }
        }

        void ReadFinalSupport()
        {
            finalSupport=null;
            foreach(Collider collider in Physics.OverlapSphere(guide+Vector3.up*(GuideOffset-original.arribay),original.sphereRadius,original.mascaraSuelo1.value,QueryTriggerInteraction.UseGlobal))
            {
                if(collider.name.IndexOf("OrigenCentro",StringComparison.Ordinal)>=0) continue;
                RecoveryPiece piece=collider.GetComponentInParent<RecoveryPiece>(); if(piece==null) continue;
                string channel="",name=piece.BasePrefab??"";
                if(name.IndexOf("BloqueFijo",StringComparison.Ordinal)>=0)
                { var source=piece.GetComponent<ControlFijo>(); if(source!=null) channel=source.conResorte; }
                else if(name.IndexOf("Bloque",StringComparison.Ordinal)>=0)
                { var source=piece.GetComponent<ControlPieza>(); if(source!=null) channel=source.conResorte; }
                if(!String.IsNullOrEmpty(channel)) { finalSupport=collider.transform; return; }
            }
        }

        int FixedAbove(Vector3 location,out float below)
        {
            below=location.y;
            for(int level=1;level<=8;level++)
            {
                Vector3 probe=location+Vector3.up*(level*original.arribay+original.subeRayo);
                foreach(Collider collider in Physics.OverlapSphere(probe,original.sphereRadius,original.mascaraDefault.value,QueryTriggerInteraction.UseGlobal))
                    if(collider.CompareTag(collider.gameObject.layer==LayerMask.NameToLayer("Suelo")?"Map1":"Map"))
                    { below=location.y+(level-1)*original.arribay; return level; }
            }
            return 0;
        }

        bool FindRoute(Vector3 location,bool up)
        {
            int ceiling=FixedAbove(location,out float below); if(ceiling==0) return false;
            float yaw=game.GameCamera.transform.parent==null?game.GameCamera.transform.eulerAngles.y:game.GameCamera.transform.parent.eulerAngles.y;
            Vector3[] directions=yaw<=45 || yaw>=315 ? new[] {Vector3.back,Vector3.right,Vector3.left,Vector3.forward} :
                yaw>=225 ? new[] {Vector3.right,Vector3.forward,Vector3.back,Vector3.left} :
                yaw>=135 ? new[] {Vector3.forward,Vector3.left,Vector3.right,Vector3.back} :
                new[] {Vector3.left,Vector3.back,Vector3.forward,Vector3.right};
            foreach(Vector3 direction in directions)
            {
                Vector3 side=location+direction*20; int last=0;
                for(int level=1;level<=5;level++)
                {
                    Vector3 probe=new Vector3(side.x,Target.y+(level-1)*original.arribay,side.z);
                    foreach(Collider collider in Physics.OverlapSphere(probe,original.sphereRadius,original.mascaraSuelo2.value,QueryTriggerInteraction.UseGlobal))
                    {
                        bool occupied=collider.gameObject.layer==LayerMask.NameToLayer("Suelo")?collider.CompareTag("Map1"):collider.gameObject!=selectedCollider.gameObject;
                        if(occupied) last=level;
                    }
                }
                if(ceiling<=last) continue;
                routeSource=location; routeSide=side; routeBelow=below;
                Vector3 p1=new Vector3(side.x,up?50+increment:selectedCollider.transform.position.y,side.z);
                Vector3 p2=new Vector3(side.x,below,side.z),p3=new Vector3(location.x,below,location.z);
                route.Clear();
                if(up)
                {
                    CalculateHeight(start-Vector3.up*GuideOffset);
                    p1.y=50+increment;
                    upFirstStage=(int)p3.y==(int)Target.y?2:1;
                    if(upFirstStage==1) route.Add(p3);
                    route.Add(p2); route.Add(p1);
                }
                else
                {
                    route.Add(p1); route.Add(p2);
                    if((int)p3.y!=(int)Target.y) route.Add(p3);
                    route.Add(Target);
                }
                fallback=false; return true;
            }
            // Original camino5 hides only the selected body's Renderer while
            // raising it35 world units; return trip simply uses final Lerp.
            fallback=true; route.Clear();
            if(up)
            {
                upFirstStage=3; route.Add(selectedCollider.transform.position+Vector3.up*35);
                hiddenRenderer=selectedCollider.GetComponent<Renderer>();
                if(hiddenRenderer!=null) { rendererWasEnabled=hiddenRenderer.enabled; hiddenRenderer.enabled=false; }
            }
            return true;
        }

        void Complete()
        {
            RecoveryPiece piece=actor; Vector3 destination=Target; Action<int,Vector3> completed=commit;
            active=false; released=false; route.Clear(); RestoreRenderer();
            if(original!=null && original.planoGuia!=null) original.planoGuia.SetActive(false);
            MoveBloque.MoviendoBloque=false; MoveBloque.fijarPieza=false;
            RecoveredNativeInteraction.EndSelection(selectedCollider);
            // Root commits after animation; its history can use BeforeState
            // rather than capturing a partially activated pressure plate.
            if(piece!=null) piece.transform.position=start;
            try { if(piece!=null && completed!=null) completed(piece.Index,destination); }
            finally { GateWater(false); moving.Remove(piece); actor=null; selectedCollider=null; commit=null; }
        }
        public void Cancel()
        {
            if(actor!=null)
            {
                actor.transform.position=start; RecoveredNativeInteraction.EndSelection(selectedCollider);
                GateWater(false); moving.Remove(actor);
            }
            moving.Remove(actor);
            RestoreRenderer(); if(original!=null && original.planoGuia!=null) original.planoGuia.SetActive(false);
            active=released=pointerInitialized=guideMoved=escapeChecked=routeUp=fallback=nativeWaterUnblocked=false;
            nativeWaterRestoreDistance=-1; nativeWaterRestoreFrame=-1;
            route.Clear(); routeIndex=0; increment=minHeight7=minHeight8=0;
            actor=null; selectedCollider=null; original=null; commit=null; gatedWater=null; gateMethod=null;
            initialSupport=finalSupport=null; physicalSupport=supportMovesDown=false;
            MoveBloque.MoviendoBloque=false; MoveBloque.fijarPieza=false;
        }
        void RestoreRenderer() { if(hiddenRenderer!=null) hiddenRenderer.enabled=rendererWasEnabled; hiddenRenderer=null; }
        void GateWater(bool enabled)
        {
            Component water=RecoveredWaterNetwork.Instance;
            if(water==null || actor==null) return;
            if(gatedWater!=water)
            { gatedWater=water; gateMethod=water.GetType().GetMethod("SetPieceMoving",BindingFlags.Instance|BindingFlags.Public); }
            if(gateMethod!=null) gateMethod.Invoke(water,new object[] {actor,enabled});
        }
        void OnDestroy() { Cancel(); }
    }
}
