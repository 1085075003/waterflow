using System;
using System.Collections.Generic;
using UnityEngine;

namespace Recovered.FlowWater
{
    // Transcription of MoveBloque.HandleStepsSolution10341F4,
    // doProcess1/2/3Solution103494C/1034EE8/103509C and
    // doProcessMoveSolu10352BC. This automatic preview is distinct from
    // the player's pointer drag and its doFijarPieza landing curve.
    public sealed class RecoveredNativeStepPlayback : MonoBehaviour
    {
        public sealed class State
        {
            public bool active, waiting, gated, pendingPhase, routed, routeUp, fallback, rendererEnabled, bodyRestored;
            public int actorIndex=-1, colliderIndex=-1, phase, routeIndex, waitPhase, previousMode;
            public float altitude, variay;
            public Vector3 destination, target, guide;
            public Vector3[] route;
            public string exception;
        }
        RecoveredFlowGame game;
        RecoveryPiece actor;
        Collider body;
        MoveBloque original;
        Transform pieceRoot;
        readonly List<Vector3> route = new List<Vector3>();
        Renderer hidden;
        bool active, waiting, gated, pendingPhase, routed, routeUp, fallback, rendererEnabled, bodyRestored;
        int phase, routeIndex, waitPhase, previousMode;
        float altitude;
        Vector3 destination, target, guide;
        string exception;
        public bool IsActive { get { return active; } }
        public bool IsWaiting { get { return active && waiting; } }
        public int Phase { get { return phase; } }
        public int WaitPhase { get { return waitPhase; } }
        public RecoveryPiece Actor { get { return actor; } }
        public Vector3 Target { get { return target; } }
        public float Altitude { get { return altitude; } }

        public static RecoveredNativeStepPlayback Attach(RecoveredFlowGame owner)
        {
            var result=owner.GetComponent<RecoveredNativeStepPlayback>();
            if(result==null) result=owner.gameObject.AddComponent<RecoveredNativeStepPlayback>();
            result.game=owner; return result;
        }
        void FindOriginal()
        {
            if(original!=null && game.GameCamera!=null && original.gameObject.scene==game.GameCamera.gameObject.scene) return;
            original=null;
            foreach(MoveBloque item in OriginalScenePresentation.Components<MoveBloque>(game.GameCamera.gameObject.scene))
            { original=item; break; }
            if(original!=null) { altitude=original.alturaSolution; waitPhase=original.activadoMech; }
        }
        public bool Begin(RecoveryPiece piece,Collider selected,Vector3 recordedDestination,string code)
        {
            if(active || piece==null || selected==null || game==null || game.GameCamera==null) return false;
            FindOriginal(); if(original==null) return false;
            actor=piece; body=selected; pieceRoot=game.PieceRoot; destination=recordedDestination; exception=code??"";
            if(exception=="a") altitude=7;
            if(exception=="b") altitude=14;
            if(exception=="c") altitude=21;
            if(exception=="d") altitude=28;
            previousMode=MoveBloque.modoSolucion; MoveBloque.modoSolucion=2;
            active=true; waiting=exception=="w"; bodyRestored=false;
            MoveBloque.MoviendoBloque=MoveBloque.moveActivo=true;
            original.getTarget=body.gameObject; original.isMouseDragging1=true;
            original.alturaSolution=altitude; original.activadaEspera=waiting; original.activadoMech=waitPhase;
            body.gameObject.layer=2;
            guide=actor.transform.position+Vector3.down*4.150000095367432f;
            SetGuide(true); Gate(true); phase=1; SetupPhase();
            return true;
        }
        void SetGuide(bool visible)
        {
            if(original==null || original.planoGuia2==null) return;
            original.planoGuia2.transform.position=guide; original.planoGuia2.SetActive(visible);
        }
        void SetupPhase()
        {
            pendingPhase=false; route.Clear(); routeIndex=0; routed=false; fallback=false;
            int part=OriginalScenePresentation.PartForLevel(game.CurrentLevelId);
            original.fijacionPieza=(part>=20 && part<=23)||(part>=32 && part<=39)?1:0;
            if(phase==1)
            {
                target=actor.transform.position+Vector3.up*(35+altitude);
                if(original.fijacionPieza!=0)
                    using(RecoveredNativeInteraction.QueryOriginalColliders(game)) routed=BuildRoute(actor.transform.position,true);
            }
            else if(phase==2)
            {
                Vector3 intermediate=destination+(exception=="r"?Vector3.up*1.2000000476837158f:Vector3.zero);
                target=intermediate+Vector3.up*(35+altitude);
                guide=intermediate+Vector3.down*4.150000095367432f; SetGuide(true);
            }
            else if(phase==3)
            {
                target=destination;
                if(original.fijacionPieza!=0)
                {
                    original.variay=destination.y;
                    using(RecoveredNativeInteraction.QueryOriginalColliders(game)) routed=BuildRoute(destination,false);
                }
            }
            original.hitPosition1=target;
        }
        static bool AnyBusy()
        { return UIManagerScript.activadoRes1||UIManagerScript.activadoRes2||UIManagerScript.activadoRes3||UIManagerScript.activadoRes4||UIManagerScript.activadoRes5||UIManagerScript.activadoRes6||UIManagerScript.activadoRes7||UIManagerScript.activadoRes8; }
        void LateUpdate()
        {
            if(!active) return;
            if(actor==null || game==null || !game.IsLevelActive || pieceRoot!=game.PieceRoot) { Cancel(); return; }
            if(pendingPhase) SetupPhase();
            if(waiting)
            {
                if(AnyBusy()) waitPhase=1;
                else if(waitPhase==1) { waitPhase=2; waiting=false; original.activadaEspera=false; }
                original.activadoMech=waitPhase; return;
            }
            if(routed) { TickRoute(); return; }
            actor.transform.position=Vector3.Lerp(actor.transform.position,target,original.smoothSpeed3*Time.deltaTime);
            float distance=Vector3.Distance(actor.transform.position,target);
            if(distance<.800000011920929f) RestoreBody();
            if(phase==3 && distance<.5f && gated) { Gate(false); Sound(); }
            if(distance>=.10000000149011612f) return;
            if(phase==1) { Sound(); phase=2; pendingPhase=true; }
            else if(phase==2) { phase=3; pendingPhase=true; }
            else { actor.transform.position=target; Sound(); Complete(); }
        }
        int FixedAbove(Vector3 location,out float below)
        {
            below=location.y;
            for(int level=1;level<=8;level++)
                foreach(Collider collider in Physics.OverlapSphere(location+Vector3.up*(level*original.arribay+original.subeRayo),original.sphereRadius,original.mascaraDefault.value,QueryTriggerInteraction.UseGlobal))
                    if(collider.CompareTag(collider.gameObject.layer==LayerMask.NameToLayer("Suelo")?"Map1":"Map"))
                    { below=location.y+(level-1)*original.arribay; return level; }
            return 0;
        }
        float HeightIncrement(Vector3 location)
        {
            int highest=0;
            foreach(float x in new[]{-20f,0f,20f}) foreach(float z in new[]{-20f,0f,20f})
            for(int level=4;level<=12;level++)
                foreach(Collider collider in Physics.OverlapSphere(new Vector3(location.x+x,level*original.arribay,location.z+z),original.sphereRadius,original.mascaraAlturaMax.value,QueryTriggerInteraction.UseGlobal))
                    if(collider.CompareTag("Map") && collider.gameObject.layer!=LayerMask.NameToLayer("Suelo1")) highest=Mathf.Max(highest,level);
            int guideLevel=(int)Math.Round(location.y/original.arribay,MidpointRounding.ToEven);
            return Mathf.Max(0,(Mathf.Max(highest,4)-guideLevel-4)*10);
        }
        bool BuildRoute(Vector3 location,bool up)
        {
            float below; int ceiling=FixedAbove(location,out below); if(ceiling==0) return false;
            float yaw=game.GameCamera.transform.parent==null?game.GameCamera.transform.eulerAngles.y:game.GameCamera.transform.parent.eulerAngles.y;
            Vector3[] directions=yaw<=45||yaw>=315?new[]{Vector3.back,Vector3.right,Vector3.left,Vector3.forward}:
                yaw>=225?new[]{Vector3.right,Vector3.forward,Vector3.back,Vector3.left}:
                yaw>=135?new[]{Vector3.forward,Vector3.left,Vector3.right,Vector3.back}:new[]{Vector3.left,Vector3.back,Vector3.forward,Vector3.right};
            routeUp=up;
            foreach(Vector3 direction in directions)
            {
                Vector3 side=location+direction*20; int highest=0;
                // doBuscarCamino uses the persistent native variay field;
                // phase3 explicitly sets it to raw finy at10351C8.
                for(int level=1;level<=5;level++)
                    foreach(Collider collider in Physics.OverlapSphere(new Vector3(side.x,original.variay+4.199999809265137f+(level-1)*original.arribay,side.z),original.sphereRadius,original.mascaraSuelo2.value,QueryTriggerInteraction.UseGlobal))
                        if(collider.gameObject.layer==LayerMask.NameToLayer("Suelo")?collider.CompareTag("Map1"):collider.gameObject!=body.gameObject) highest=level;
                if(ceiling<=highest) continue;
                Vector3 p1=new Vector3(side.x,up?50+HeightIncrement(actor.transform.position-Vector3.up*4.199999809265137f):body.transform.position.y,side.z);
                Vector3 p2=new Vector3(side.x,below,side.z),p3=new Vector3(location.x,below,location.z),p4=guide+Vector3.up*4.199999809265137f;
                if(up) { if((int)p3.y!=(int)p4.y) route.Add(p3); route.Add(p2); route.Add(p1); }
                else { route.Add(p1); route.Add(p2); if((int)p3.y!=(int)p4.y) route.Add(p3); route.Add(p4); }
                return true;
            }
            fallback=true;
            if(up)
            {
                route.Add(body.transform.position+Vector3.up*35);
                hidden=body.GetComponent<Renderer>();
                if(hidden!=null) { rendererEnabled=hidden.enabled; hidden.enabled=false; }
            }
            else route.Add(guide+Vector3.up*4.199999809265137f);
            return true;
        }
        void TickRoute()
        {
            Vector3 next=route[routeIndex];
            bool lastDown=!routeUp && routeIndex==route.Count-1;
            actor.transform.position=lastDown?Vector3.Lerp(actor.transform.position,next,original.smoothSpeed1*Time.deltaTime):Vector3.MoveTowards(actor.transform.position,next,original.smoothSpeed2*Time.deltaTime);
            if(Vector3.Distance(actor.transform.position,next)>=.10000000149011612f) return;
            actor.transform.position=next; routeIndex++;
            if(routeIndex<route.Count) return;
            RestoreRenderer(); routed=false; route.Clear();
            if(routeUp) { phase=2; pendingPhase=true; }
            else { Gate(false); Sound(); Complete(); }
        }
        void Sound() { if(AudioManager.instance!=null) AudioManager.instance.PlaySound("SoundBlock"); }
        void RestoreBody()
        {
            if(body!=null) body.gameObject.layer=LayerMask.NameToLayer("bloque");
            bodyRestored=true;
        }
        void RestoreRenderer() { if(hidden!=null) hidden.enabled=rendererEnabled; hidden=null; }
        void Gate(bool value)
        {
            if(actor!=null && RecoveredWaterNetwork.Instance!=null) RecoveredWaterNetwork.Instance.SetPieceMoving(actor,value);
            gated=value;
        }
        void Complete()
        {
            int index=actor.Index; Cancel(); phase=9;
            // HandleSolution keeps the automatic chain's global drag flag
            // across phase9→phase1. Manual input is only released at the end.
            MoveBloque.MoviendoBloque=game.IsSolutionPlaybackRunning;
            game.CompleteNativeRecordedStep(index);
        }
        public void Cancel()
        {
            bool wasActive=active;
            if(gated) Gate(false);
            RestoreBody(); RestoreRenderer(); SetGuide(false);
            if(original!=null) { original.isMouseDragging1=false; original.activadaEspera=false; }
            active=waiting=routed=pendingPhase=false; route.Clear(); body=null; actor=null;
            if(wasActive) { MoveBloque.MoviendoBloque=false; MoveBloque.fijarPieza=false; MoveBloque.modoSolucion=previousMode; }
        }
        public void ResetPlayback()
        {
            // doInicializar102E45C resets alturaSolution. activadoMech is
            // persistent: only doProcesoEspera writes1/2 in the original.
            Cancel(); altitude=0;
            if(original!=null) original.alturaSolution=0;
        }
        public State CaptureState()
        {
            Collider[] colliders=actor==null?new Collider[0]:actor.GetComponentsInChildren<Collider>(true);
            return new State {active=active,waiting=waiting,gated=gated,pendingPhase=pendingPhase,routed=routed,routeUp=routeUp,fallback=fallback,rendererEnabled=rendererEnabled,bodyRestored=bodyRestored,
                actorIndex=actor==null?-1:actor.Index,colliderIndex=Array.IndexOf(colliders,body),phase=phase,routeIndex=routeIndex,waitPhase=waitPhase,previousMode=previousMode,altitude=altitude,variay=original==null?0:original.variay,destination=destination,target=target,guide=guide,route=route.ToArray(),exception=exception};
        }
        public void RestoreState(State saved)
        {
            Cancel(); if(saved==null) { altitude=0; waitPhase=0; return; }
            if(game.GameCamera!=null) FindOriginal();
            altitude=saved.altitude; waitPhase=saved.waitPhase;
            if(original!=null) { original.alturaSolution=altitude; original.activadoMech=waitPhase; original.variay=saved.variay; }
            if(!saved.active || saved.actorIndex<0 || saved.actorIndex>=game.Pieces.Count || game.GameCamera==null || original==null) return;
            actor=game.Pieces[saved.actorIndex]; Collider[] colliders=actor.GetComponentsInChildren<Collider>(true);
            if(saved.colliderIndex<0 || saved.colliderIndex>=colliders.Length) { actor=null; return; }
            body=colliders[saved.colliderIndex]; pieceRoot=game.PieceRoot;
            active=true; waiting=saved.waiting; pendingPhase=saved.pendingPhase; routed=saved.routed; routeUp=saved.routeUp; fallback=saved.fallback; rendererEnabled=saved.rendererEnabled; bodyRestored=saved.bodyRestored;
            phase=saved.phase; routeIndex=saved.routeIndex; previousMode=saved.previousMode; destination=saved.destination; target=saved.target; guide=saved.guide; exception=saved.exception;
            route.Clear(); if(saved.route!=null) route.AddRange(saved.route);
            MoveBloque.modoSolucion=2; MoveBloque.MoviendoBloque=true; original.isMouseDragging1=true; original.getTarget=body.gameObject;
            original.alturaSolution=altitude; original.activadoMech=waitPhase; original.activadaEspera=waiting;
            body.gameObject.layer=bodyRestored?LayerMask.NameToLayer("bloque"):2;
            if(routed && fallback && routeUp) { hidden=body.GetComponent<Renderer>(); if(hidden!=null) hidden.enabled=false; }
            SetGuide(true); Gate(saved.gated);
        }
        void OnDisable() { Cancel(); }
    }
}
