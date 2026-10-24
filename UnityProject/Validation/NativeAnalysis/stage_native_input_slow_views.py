import pathlib,hashlib
root=pathlib.Path(__file__).resolve().parent
base=root/'FlowWaterSave1GameplayValidation.pending-native-input-v7.cs.src'
assert hashlib.sha256(base.read_bytes()).hexdigest()=='2584c7cdf18908f1f593e3aa316ce88dde2019dbcef74330573db278a357877c'
text=base.read_text('utf-8-sig')
def patch(a,b):
 global text
 assert text.count(a)==1,(a[:90],text.count(a))
 text=text.replace(a,b)
patch('            public int cameraQuarterTurns;', '            public int cameraQuarterTurns, cameraSlowGestures, cameraSlowFrames;\n            public List<float> cameraYawSamples=new List<float>();')
patch('        public static void RunNativeFocused() { Begin(new[] {1142,1149,1236,1656,1660},true); }', '        public static void RunNativeFocused() { Begin(new[] {1142,1149,1236,1656,1660},true); }\n        public static void RunNativeViewFailures() { Begin(new[] {754,761},true); }')
patch('''            Require("gm"+level.id+"-native-input-original-source-and-surface-"+step.number,planned && motion!=null && motion.IsActive,''','''            // The original camera also supports continuous slow mouse swipes.
            // Quarter-turn buttons alone do not prove a surface is unreachable.
            // Try this only after all four original quarter views fail; every
            // pose change still comes from original entrada/Begin/Continue/End.
            if(!planned)
            for(int quarter=0;quarter<4 && !planned;quarter++)
            {
                foreach(float offset in new[]{15f,30f,45f,60f,75f})
                {
                    foreach(object cameraWait in SlowNativeCamera(level,step,offset)) yield return cameraWait;
                    actor=Game.Pieces.FirstOrDefault(piece=>piece!=null && piece.SourceRecordIndex==step.sourceRecord && piece.CanMove && Near(piece.transform.position,step.origin));
                    if(actor==null) { step.inputDiagnostics.Add("Slow view: original actor moved or became fixed; no substitute capture."); break; }
                    if(TryOriginalPickPoint(actor,out screen,step))
                    {
                        NativeInput("BeginDrag",screen); motion=Game.GetComponent<RecoveredNativeDragMotion>();
                        if(motion!=null && motion.IsActive)
                        {
                            Vector3 dropScreen,drop; RaycastHit hit;
                            if(step.sameCellPickup || FindOriginalDropPoint(step.destination,out dropScreen,out drop,out hit,step)) { planned=true; break; }
                            NativeInput("CancelDrag");
                        }
                    }
                }
                if(!planned && quarter<3) foreach(object cameraWait in TurnNativeCamera(level,step)) yield return cameraWait;
            }
            Require("gm"+level.id+"-native-input-original-source-and-surface-"+step.number,planned && motion!=null && motion.IsActive,''')
patch('''        static bool NativeChannelsBusy()''','''        static IEnumerable SlowNativeCamera(Level level,Step step,float offset)
        {
            SwipeHandler2 swipe=UnityEngine.Object.FindObjectsOfType<SwipeHandler2>(true).FirstOrDefault(x=>x.EsferaCamera!=null && x.gameObject.scene==Game.GameCamera.gameObject.scene);
            Require("gm"+level.id+"-slow-camera-not-holding-"+step.number,swipe!=null && swipe.enabled && !MoveBloque.MoviendoBloque,"Cancel production selection before original rotation; never move the camera while holding an actor.");
            double until=EditorApplication.timeSinceStartup+12;
            while(Mathf.Abs(Mathf.DeltaAngle(swipe.EsferaCamera.eulerAngles.y,swipe.rotationFinal.y))>.25f && EditorApplication.timeSinceStartup<until) yield return null;
            Require("gm"+level.id+"-slow-camera-native-return-"+step.number,Mathf.Abs(Mathf.DeltaAngle(swipe.EsferaCamera.eulerAngles.y,swipe.rotationFinal.y))<=.25f,"Original TickRotation settles the previous gesture to its existing native target, without assigning a camera pose.");
            Vector2 start=Vector2.zero; bool began=false; Rect rect=Game.GameCamera.pixelRect;
            for(int y=2;y<=7 && !began;y++) for(int x=1;x<=8 && !began;x++)
            {
                Vector2 candidate=new Vector2(rect.x+rect.width*x/12f,rect.y+rect.height*y/9f);
                if(candidate.x>rect.xMax-170) continue;
                if(swipe.BeginRotation(candidate,false,Game.GameCamera)) { began=true; start=candidate; }
            }
            Require("gm"+level.id+"-slow-camera-real-entrada-"+step.number,began && swipe.getAreaRotation!=null && swipe.getAreaRotation.layer==LayerMask.NameToLayer("entrada"),"Production BeginRotation must hit the authored entrada collider and pass original UI/piece-selection gates.");
            Vector2 pointer=start; float target=swipe.rotationFinal.y+offset,started=Time.time; int frames=0;
            try
            {
                // InputManager.asset: Mouse X sensitivity=.1. Original desktop
                // ContinueRotation uses degree=MouseX*multiplicador (scene5).
                // One pixel per real frame gives axis.1 / approximately.5deg.
                while(Mathf.Abs(Mathf.DeltaAngle(swipe.EsferaCamera.eulerAngles.y,target))>.05f && frames<200)
                {
                    float axis=Mathf.Clamp(Mathf.DeltaAngle(swipe.EsferaCamera.eulerAngles.y,target)/swipe.multiplicador,-.1f,.1f);
                    pointer.x+=axis/.1f;
                    Require("gm"+level.id+"-slow-camera-pointer-in-view-"+step.number,rect.Contains(pointer),"The gentle mouse gesture stays inside the real gameplay viewport.");
                    swipe.ContinueRotation(pointer,axis); frames++; yield return null;
                }
                Require("gm"+level.id+"-slow-camera-continuous-yaw-"+step.number,frames>0 && Mathf.Abs(Mathf.DeltaAngle(swipe.EsferaCamera.eulerAngles.y,target))<=.05f,"Actual original slow gesture reaches target="+target+",yaw="+swipe.EsferaCamera.eulerAngles.y+",frames="+frames);
                // Release slowly enough to stay below the original flick
                // threshold. No change to handleFlicks/force/rotationFinal.
                double releaseDeadline=EditorApplication.timeSinceStartup+3;
                while(Time.time-started<Mathf.Abs(pointer.x-start.x)*Time.deltaTime*.5f/Mathf.Max(.1f,swipe.requiredForceForFlick*.8f) && EditorApplication.timeSinceStartup<releaseDeadline) yield return null;
                swipe.EndRotation(pointer);
            }
            finally { if(swipe.IsPointerRotating) swipe.EndRotation(pointer,true); }
            Require("gm"+level.id+"-slow-camera-low-force-release-"+step.number,!swipe.IsPointerRotating && Mathf.Abs(swipe.NativeFlickForce)<=swipe.requiredForceForFlick,"Actual EndRotation releases without a quarter-turn flick; force="+swipe.NativeFlickForce+",threshold="+swipe.requiredForceForFlick);
            step.cameraSlowGestures++; step.cameraSlowFrames+=frames;
            // A real frame after pointer-up permits the original release Lerp.
            // The subsequent real pickup freezes that current intermediate yaw.
            yield return null;
            step.cameraYawSamples.Add(swipe.EsferaCamera.eulerAngles.y);
            step.inputDiagnostics.Add("slow-native-mouse-view requestedOffset="+offset+", actualYaw="+swipe.EsferaCamera.eulerAngles.y+",frames="+frames+",nativeForce="+swipe.NativeFlickForce+",entrada="+swipe.getAreaRotation.name);
        }
        static bool NativeChannelsBusy()''')
patch('                    if(NativeReplay) report.notes.Add("Native-input variant:', '                    if(NativeReplay) report.notes.Add("After four occluded quarter views, v8 may use genuine gentle original mouse rotation: actual entrada BeginRotation, pointer displacement with original MouseX sensitivity.1, per-frame ContinueRotation, low-force EndRotation and one real release frame. Intermediate angles are observed through the original camera; no camera transform/pitch/mask, guide, actor or switch is assigned.\");\n                    if(NativeReplay) report.notes.Add("Native-input variant:')
out=root/'FlowWaterSave1GameplayValidation.pending-native-input-v8.cs.src';out.write_text(text,'utf-8');print(out.name,hashlib.sha256(out.read_bytes()).hexdigest())
