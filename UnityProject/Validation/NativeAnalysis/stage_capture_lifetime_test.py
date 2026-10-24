import pathlib,hashlib
root=pathlib.Path(__file__).resolve().parent
text=(root.parent/'NativeStepPlaybackBatch3/Assets/Editor/RecoveredTools/FlowWaterStepPlaybackValidation.cs').read_text('utf-8-sig')
def patch(a,b):
 global text
 assert text.count(a)==1,(a[:80],text.count(a))
 text=text.replace(a,b)
patch('        public static void RunMissingCapture() { Start(new[]{1225}); }','        public static void RunMissingCapture() { Start(new[]{1225}); }\n        public static void RunCaptureLifetime() { Start(new[]{0}); }')
patch('''                PlayerPrefs.SetInt("Recovery.LastLevelId",cases[0]);
                string name=OriginalScenePresentation.SceneForLevel(cases[0]);''','''                int first=cases[0]==0?601:cases[0];
                PlayerPrefs.SetInt("Recovery.LastLevelId",first);
                string name=OriginalScenePresentation.SceneForLevel(first);''')
patch('''            foreach(int id in cases)
            {
                UI.StartGame(id);''','''            foreach(int id in cases)
            {
                if(id==0) { foreach(object wait in CaptureLifetime()) yield return wait; continue; }
                UI.StartGame(id);''')
patch('''        static void Click(string method)''','''        // Explicit controlled branch validation, not a new original level:
        // inject only a temporary empty origin in the in-memory descriptor.
        // Production capture/animation, real actor inventory, water/springs,
        // Undo and original raw descriptors remain otherwise unchanged.
        static IEnumerable<object> CaptureLifetime()
        {
            report.notes.Add("CONTROLLED CAPTURE-LIFETIME FIXTURE: gm601 is real; only its in-memory first/second RecordedStep.origin is temporarily replaced by an independently confirmed empty query point. Both descriptors are restored and the original board reloaded. This is a native empty-query/retained-field/Undo test, not recorded original whole-level gameplay or completion proof. No actor, guide, water, switch or collider transform is assigned by the validator.");
            UI.StartGame(601); double until=EditorApplication.timeSinceStartup+30;
            while((Game==null || Game.CurrentLevelId!=601 || !Game.IsLevelActive) && EditorApplication.timeSinceStartup<until) yield return null;
            Require("controlled-original-gm601",Game!=null && Game.CurrentLevelId==601 && Game.HasSolution,"Real gm601/so601 loaded through production UI.");
            for(int f=0;f<8;f++) yield return null;
            var steps=(IList)typeof(RecoveredFlowGame).GetField("steps",Hidden).GetValue(Game);
            object first=steps[0],second=steps[1];
            FieldInfo origin=first.GetType().GetField("origin"),destination=first.GetType().GetField("destination");
            Vector3 firstOrigin=(Vector3)origin.GetValue(first),secondOrigin=(Vector3)origin.GetValue(second),secondDestination=(Vector3)destination.GetValue(second);
            Vector3 empty=new Vector3(1000,3.95f,1000); int count=Game.PieceCount;
            bool descriptorsRestored=false;
            try
            {
                Require("controlled-initial-query-empty",EmptyCapture(empty),"Production original OverlapSphere/mask query independently finds no body at the injected descriptor origin.");
                origin.SetValue(first,empty); Game.ApplyHint();
                for(int f=0;f<3;f++) yield return null;
                RecoveryPiece ignored; Collider ignoredCollider;
                var playback=Game.GetComponent<RecoveredNativeStepPlayback>();
                Require("controlled-first-miss-has-no-reference",playback!=null && !playback.TryGetRetainedCapture(out ignored,out ignoredCollider),"First missing capture has no previous piezaCapturada.");
                Require("controlled-first-miss-stalls",!Game.IsSolutionStepAnimating && Game.AppliedSolutionSteps==0 && Game.MoveCount==0 && Game.PieceCount==count,"No fabricated actor, hidden skip, placement or transaction after a first empty capture.");
                origin.SetValue(first,firstOrigin); Game.ApplyHint();
                Require("controlled-real-first-step-starts",Game.IsSolutionStepAnimating,"Restore raw first origin and animate its genuine original actor.");
                RecoveryPiece previous=playback.Actor;
                until=EditorApplication.timeSinceStartup+30;
                while(Game.IsSolutionStepAnimating && EditorApplication.timeSinceStartup<until) yield return null;
                Require("controlled-real-first-step-completes",!Game.IsSolutionStepAnimating && Game.AppliedSolutionSteps==1,"First native three-phase animation reached phase9.");
                var springs=Game.GetComponent<RecoveredNativeSprings>(); until=EditorApplication.timeSinceStartup+30;
                while(springs!=null && springs.IsBusy && EditorApplication.timeSinceStartup<until) yield return null;
                Require("controlled-initial-mechanism-settles",springs==null || !springs.IsBusy,"No injected bus state or transform is used to settle the real pressure response.");
                Vector3[] before=Positions(); RecoveryPiece retained; Collider exactBody;
                Require("controlled-original-previous-body-stored",playback.TryGetRetainedCapture(out retained,out exactBody) && retained==previous,"The exact collider body remains in native piezaCapturada after step completion.");
                Require("controlled-second-query-still-empty",EmptyCapture(empty),"The injected next origin remains physically empty under the unchanged native query.");
                origin.SetValue(second,empty); Game.ApplyHint();
                MoveBloque native=OriginalScenePresentation.Components<MoveBloque>(Game.GameCamera.gameObject.scene).First();
                Require("controlled-miss-selects-retained-body",Game.IsSolutionStepAnimating && playback.Actor==previous && native.getTarget==exactBody.gameObject && exactBody.gameObject.layer==2 && native.isMouseDragging1,"Native doProcess1Solution reuses piezaCapturada and performs the full original layer2/guide/lift initialization.");
                Require("controlled-retained-trace",Game.SolutionCaptureTrace.Any(x=>x.StartsWith("2 RETAINED record"+previous.SourceRecordIndex+":",StringComparison.Ordinal)),"Diagnostics distinguish an empty query retaining an original actor from a fresh physical capture.");
                until=EditorApplication.timeSinceStartup+30;
                var phases=new HashSet<int>();
                while(Game.IsSolutionStepAnimating && EditorApplication.timeSinceStartup<until) { phases.Add(Game.NativeSolutionPhase); yield return null; }
                Require("controlled-retained-full-three-phases",!Game.IsSolutionStepAnimating && Game.AppliedSolutionSteps==2 && phases.Contains(1) && phases.Contains(2) && phases.Contains(3),"The retained body follows the same three native motion phases rather than jumping to a fabricated final state.");
                Require("controlled-retained-original-destination",(previous.transform.position-secondDestination).sqrMagnitude<.000001f && Game.PieceCount==count,"gm601 has no fixed-roof route: retained record"+previous.SourceRecordIndex+" must finish at raw second destination="+secondDestination+",actual="+previous.transform.position+"; inventory="+count);
                Game.Undo();
                Require("controlled-retained-undo-poses",Game.AppliedSolutionSteps==1 && Same(before) && !Game.IsSolutionStepAnimating,"Undo restores the whole genuine pre-step board and mechanisms.");
                Require("controlled-retained-undo-reference",playback.TryGetRetainedCapture(out retained,out exactBody) && retained==previous,"Snapshot restores the same original captured-reference lifetime.");
                foreach(string trace in Game.SolutionCaptureTrace) report.captureTraces.Add("CONTROLLED gm601 "+trace);
            }
            finally
            {
                origin.SetValue(first,firstOrigin); origin.SetValue(second,secondOrigin);
                descriptorsRestored=(Vector3)origin.GetValue(first)==firstOrigin && (Vector3)origin.GetValue(second)==secondOrigin;
                Game.RestartLevel();
            }
            for(int f=0;f<8;f++) yield return null;
            Require("controlled-descriptors-restored",descriptorsRestored && Game.CurrentLevelId==601 && Game.AppliedSolutionSteps==0 && !Game.IsSolutionStepAnimating,"Restore both exact raw origins in finally, then reload the unchanged original gm/so before exit.");
        }
        static bool EmptyCapture(Vector3 point)
        {
            var enabled=(List<Collider>)typeof(RecoveredFlowGame).GetMethod("EnableNativeCaptureColliders",Hidden).Invoke(Game,null);
            try
            {
                MethodInfo capture=typeof(RecoveredFlowGame).GetMethod("CaptureNativePiece",Hidden,null,new[]{typeof(Vector3),typeof(List<Collider>)},null);
                return capture.Invoke(Game,new object[]{point,enabled})==null;
            }
            finally { foreach(Collider collider in enabled) if(collider!=null) collider.enabled=false; }
        }
        static void Click(string method)''')
out=root/'FlowWaterStepPlaybackValidation.pending-capture-lifetime-test.cs.src';out.write_text(text,'utf-8');print(out.name,hashlib.sha256(out.read_bytes()).hexdigest())
