import pathlib,hashlib
root=pathlib.Path(__file__).resolve().parent
batch=root.parent/'NativeStepPlaybackBatch2/Assets'
helper=(batch/'Scripts/RecoveryRuntime/RecoveredNativeStepPlayback.cs').read_text('utf-8-sig')
core=(batch/'Scripts/RecoveryRuntime/RecoveredFlowGame.cs').read_text('utf-8-sig')
assert hashlib.sha256((batch/'Scripts/RecoveryRuntime/RecoveredNativeStepPlayback.cs').read_bytes()).hexdigest()=='3d995b7ea506568386336be5473ac54db7c45c2dfd53e6bf7ceb9fe72eaadcce'
assert hashlib.sha256((batch/'Scripts/RecoveryRuntime/RecoveredFlowGame.cs').read_bytes()).hexdigest()=='8e90f07f9e6c19c347deb6b3f0c90dfb9fbb6715a9fe73fedb4d5e8f0a665bc4'
def patch(text,a,b):
 assert text.count(a)==1,(a[:90],text.count(a))
 return text.replace(a,b)
helper=patch(helper,'public int actorIndex=-1, colliderIndex=-1, phase, routeIndex, waitPhase, previousMode;', 'public int actorIndex=-1, colliderIndex=-1, lastActorIndex=-1, lastColliderIndex=-1, phase, routeIndex, waitPhase, previousMode;\n            public bool nativeDragging, targetPresent;')
helper=patch(helper,'        RecoveryPiece actor;\n        Collider body;','        RecoveryPiece actor, lastCapturedActor;\n        Collider body, lastCapturedBody;')
helper=patch(helper,'        public Vector3 Target { get { return target; } }', '''        public Vector3 Target { get { return target; } }
        // doCapturarPieza1034594 only resets checkCollisionSolu. Unlike the
        // Total caller1034138, doProcess1Solution never clears piezaCapturada.
        // A later empty query therefore retains the exact previous body.
        public bool TryGetRetainedCapture(out RecoveryPiece piece,out Collider selected)
        {
            piece=null; selected=null;
            if(lastCapturedActor==null || lastCapturedBody==null || game==null || game.PieceRoot==null) return false;
            int index=lastCapturedActor.Index;
            if(index<0 || index>=game.Pieces.Count || game.Pieces[index]!=lastCapturedActor || !lastCapturedActor.transform.IsChildOf(game.PieceRoot)) return false;
            piece=lastCapturedActor; selected=lastCapturedBody; return true;
        }''')
helper=patch(helper,'            actor=piece; body=selected; pieceRoot=game.PieceRoot;', '            actor=lastCapturedActor=piece; body=lastCapturedBody=selected; pieceRoot=game.PieceRoot;')
helper=patch(helper,'            if(automatic) MoveBloque.modoSolucion=2;', '''            if(automatic)
            {
                MoveBloque.modoSolucion=2;
                if(original!=null) { original.isMouseDragging1=true; original.getTarget=lastCapturedBody==null?null:lastCapturedBody.gameObject; }
            }''')
helper=patch(helper,'            if(!game.IsSolutionPlaybackRunning && original!=null && original.animBack!=null) original.animBack.enabled=true;', '''            if(!game.IsSolutionPlaybackRunning && original!=null)
            {
                original.getTarget=null;
                if(original.animBack!=null) original.animBack.enabled=true;
            }''')
helper=patch(helper,'            Cancel(); altitude=0;\n            if(original!=null) original.alturaSolution=0;', '''            Cancel(); altitude=0; lastCapturedActor=null; lastCapturedBody=null;
            // Board reconstruction destroys those original body objects.
            if(original!=null) { original.alturaSolution=0; original.getTarget=null; }''')
helper=patch(helper,'            Collider[] colliders=actor==null?new Collider[0]:actor.GetComponentsInChildren<Collider>(true);', '''            if(game!=null && game.GameCamera!=null) FindOriginal();
            Collider[] colliders=actor==null?new Collider[0]:actor.GetComponentsInChildren<Collider>(true);
            Collider[] lastColliders=lastCapturedActor==null?new Collider[0]:lastCapturedActor.GetComponentsInChildren<Collider>(true);''')
helper=patch(helper,'                actorIndex=actor==null?-1:actor.Index,colliderIndex=Array.IndexOf(colliders,body),phase=phase,', '                lastActorIndex=lastCapturedActor==null?-1:lastCapturedActor.Index,lastColliderIndex=Array.IndexOf(lastColliders,lastCapturedBody),nativeDragging=original!=null && original.isMouseDragging1,targetPresent=original!=null && original.getTarget!=null,\n                actorIndex=actor==null?-1:actor.Index,colliderIndex=Array.IndexOf(colliders,body),phase=phase,')
helper=patch(helper,'            Cancel(); if(saved==null) { altitude=0; waitPhase=0; return; }', '            Cancel(); lastCapturedActor=null; lastCapturedBody=null;\n            if(saved==null) { altitude=0; waitPhase=0; if(original!=null) original.getTarget=null; return; }')
helper=patch(helper,'            if(original!=null) { original.alturaSolution=altitude; original.activadoMech=waitPhase; original.variay=saved.variay; }', '''            if(saved.lastActorIndex>=0 && saved.lastActorIndex<game.Pieces.Count)
            {
                RecoveryPiece previous=game.Pieces[saved.lastActorIndex]; Collider[] previousColliders=previous.GetComponentsInChildren<Collider>(true);
                if(saved.lastColliderIndex>=0 && saved.lastColliderIndex<previousColliders.Length)
                { lastCapturedActor=previous; lastCapturedBody=previousColliders[saved.lastColliderIndex]; }
            }
            if(original!=null)
            {
                original.alturaSolution=altitude; original.activadoMech=waitPhase; original.variay=saved.variay;
                original.getTarget=!saved.targetPresent || lastCapturedBody==null?null:lastCapturedBody.gameObject; original.isMouseDragging1=saved.nativeDragging;
            }''')
core=patch(core,'''                if (captured == null || selected == null)
                {
                    Status = "Waiting for the original recorded actor at " + step.origin.ToString("F2") + ".";
                    Notify(); return;
                }
                PushSnapshot();''','''                bool retainedCapture = false;
                if (captured == null || selected == null)
                {
                    retainedCapture = RecoveredNativeStepPlayback.Attach(this).TryGetRetainedCapture(out captured, out selected);
                    SolutionCaptureMisses++;
                    if (!retainedCapture)
                    {
                        Status = "Waiting for the original recorded actor at " + step.origin.ToString("F2") + ".";
                        Notify(); return;
                    }
                }
                solutionCaptureTrace.Add((stepCursor + 1) + (retainedCapture ? " RETAINED" : " FRESH") + " record" + captured.SourceRecordIndex +
                    ":" + captured.BasePrefab + " origin=" + step.origin.ToString("F3") + " destination=" + step.destination.ToString("F3"));
                PushSnapshot();''')
core=patch(core,'            stepCursor++; MoveCount++; SelectedPieceIndex = actorIndex;', '''            stepCursor++; MoveCount++; SelectedPieceIndex = actorIndex;
            if (actorIndex >= 0 && actorIndex < pieces.Count)
                solutionCaptureTrace.Add(stepCursor + " COMPLETE record" + pieces[actorIndex].SourceRecordIndex + ":" + pieces[actorIndex].BasePrefab +
                    " position=" + pieces[actorIndex].transform.position.ToString("F3"));
            else solutionCaptureTrace.Add(stepCursor + " MARKER z");''')
core=patch(core,'                RecoveredNativeStepPlayback.Attach(this).ResetPlayback();\n                previewAutomatic = stepSolution;', '                RecoveredNativeStepPlayback.Attach(this).ResetPlayback();\n                solutionCaptureTrace.Clear(); SolutionCaptureMisses = 0;\n                previewAutomatic = stepSolution;')
for name,text in [('RecoveredNativeStepPlayback.pending-retained-capture.cs.src',helper),('RecoveredFlowGame.pending-retained-capture.cs.src',core)]:
 out=root/name;out.write_text(text,'utf-8');print(name,hashlib.sha256(out.read_bytes()).hexdigest())
