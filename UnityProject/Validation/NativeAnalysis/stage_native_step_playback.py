import pathlib,hashlib,json
root=pathlib.Path(__file__).resolve().parent
base=root/'RecoveredFlowGame.pending-native-camera.cs.src'
expected='99d31d648615ed9e6fe001d41e1d1ec5a0b343491c5222a6a51e1c4bc55ce0bf'
assert hashlib.sha256(base.read_bytes()).hexdigest()==expected
text=base.read_text('utf-8-sig')
def replace(a,b,count=1):
 global text
 assert text.count(a)==count,(a[:90],text.count(a),count)
 text=text.replace(a,b)
replace('        public bool IsShowingFinalSolution { get { return solutionBoard; } }', '''        public bool IsShowingFinalSolution { get { return solutionBoard; } }
        public bool IsSolutionStepAnimating { get { var playback=GetComponent<RecoveredNativeStepPlayback>(); return playback!=null && playback.IsActive; } }
        public int NativeSolutionPhase { get { var playback=GetComponent<RecoveredNativeStepPlayback>(); return playback==null?0:playback.Phase; } }''')
replace('            public object waterState;','            public object waterState;\n            public RecoveredNativeStepPlayback.State stepPlaybackState;')
replace('            if (IsSolutionPreview) return false;', '            if (IsSolutionPreview || IsSolutionStepAnimating) return false;')
start=text.index('                RecordedStep step = steps[stepCursor];',text.index('        public void ApplyHint()'))
end=text.index('\n            }\n            // Prefer a goal',start)
text=text[:start]+'''                if (IsSolutionStepAnimating) return;
                RecordedStep step = steps[stepCursor];
                // Native z alone skips capture. Same-cell w still selects a
                // body into layer2 and waits before all three motion phases.
                if (step.exception == "z")
                {
                    PushSnapshot(); CompleteNativeRecordedStep(-1); return;
                }
                Collider selected;
                RecoveryPiece captured;
                List<Collider> colliders = EnableNativeCaptureColliders();
                try { captured = CaptureNativePiece(step.origin, colliders, out selected); }
                finally { foreach (Collider collider in colliders) if (collider != null) collider.enabled = false; }
                if (captured == null || selected == null)
                {
                    Status = "Waiting for the original recorded actor at " + step.origin.ToString("F2") + ".";
                    Notify(); return;
                }
                PushSnapshot();
                if (!RecoveredNativeStepPlayback.Attach(this).Begin(captured, selected, step.destination, step.exception))
                { history.Pop(); Status = "Original step animation could not start."; Notify(); return; }
                SelectedPieceIndex = captured.Index;
                Status = "Playing original step " + (stepCursor + 1) + "/" + steps.Count + ".";
                Physics.SyncTransforms(); Notify(); return;'''+text[end:]
replace('        /// <summary>Applies the recorded solution. Victory still requires actual matching-color water.</summary>', '''        internal void CompleteNativeRecordedStep(int actorIndex)
        {
            stepCursor++; MoveCount++; SelectedPieceIndex = actorIndex;
            Status = "Original step " + stepCursor + "/" + steps.Count + " complete.";
            CheckComplete(true); Physics.SyncTransforms(); Notify();
        }

        void CancelStepPlayback()
        {
            var playback = GetComponent<RecoveredNativeStepPlayback>();
            if (playback != null) playback.Cancel();
        }

        /// <summary>Applies the recorded solution. Victory still requires actual matching-color water.</summary>''')
replace('''        RecoveryPiece CaptureNativePiece(Vector3 origin, List<Collider> colliders)
        {''','''        RecoveryPiece CaptureNativePiece(Vector3 origin, List<Collider> colliders)
        {
            Collider ignored;
            return CaptureNativePiece(origin, colliders, out ignored);
        }

        RecoveryPiece CaptureNativePiece(Vector3 origin, List<Collider> colliders, out Collider selected)
        {
            selected = null;''')
replace('                captured = piece;\n            }\n            return captured;', '                captured = piece; selected = collider;\n            }\n            return captured;')
replace('waterState = water == null ? null : water.CaptureState() };','waterState = water == null ? null : water.CaptureState(), stepPlaybackState = RecoveredNativeStepPlayback.Attach(this).CaptureState() };')
replace('''        void RestoreSnapshot(Snapshot saved)
        {''','''        void RestoreSnapshot(Snapshot saved)
        {
            CancelStepPlayback();''')
replace('            if (water != null) water.RestoreState(saved.waterState);','            if (water != null) water.RestoreState(saved.waterState);\n            RecoveredNativeStepPlayback.Attach(this).RestoreState(saved.stepPlaybackState);')
replace('''        public void Undo()
        {
            CancelDrag();''','''        public void Undo()
        {
            CancelStepPlayback(); CancelDrag();''')
replace('''        public void Solve()
        {
            CancelDrag();''','''        public void Solve()
        {
            CancelStepPlayback(); CancelDrag();''')
replace('''            if (stepsMode)
            {
                for (int i = 0; i < pieces.Count; i++)
                {
                    pieces[i].transform.position = pieces[i].InitialPosition;
                    pieces[i].transform.rotation = pieces[i].InitialRotation;
                }
                MoveCount = 0; stepCursor = 0; IsComplete = false; history.Clear();
                Physics.SyncTransforms(); Notify();
            }''','''            if (stepsMode)
            {
                // Native Save1 step preview rebuilds the initial mode0 actors
                // and mechanisms before beginning its recorded animation.
                BuildBoard(layout, false); ConfigureSolution(); CreateBoardAndView();
                RecoveredNativeCamera.ConfigureLevel(CurrentLevelId);
                if (GameCamera != null)
                {
                    var framing = GameCamera.GetComponent<RecoveredBoardFraming>();
                    if (framing != null) framing.FitNow();
                }
                RecoveredNativeStepPlayback.Attach(this).ResetPlayback();
                MoveCount = 0; stepCursor = 0; IsComplete = false; history.Clear();
                Physics.SyncTransforms(); Notify();
            }''')
replace('''            CancelDrag();
            RestoreSnapshot(previewOrigin);''','''            CancelStepPlayback(); CancelDrag();
            RestoreSnapshot(previewOrigin);''')
replace('''        void ClearBoard()
        {
            if (boardRoot''','''        void ClearBoard()
        {
            var playback = GetComponent<RecoveredNativeStepPlayback>();
            if (playback != null) playback.ResetPlayback();
            if (boardRoot''')
replace('''            if (IsSolutionPreview) return;
            if (dragMotion''','''            if (IsSolutionPreview || IsSolutionStepAnimating) return;
            if (dragMotion''')
replace('        bool previewSteps;', '        bool previewSteps, previewAutomatic;\n        float previewNextStepTime;')
replace('        public int NativeSolutionPhase', '        public bool IsSolutionPlaybackRunning { get { return previewAutomatic || IsSolutionStepAnimating; } }\n        public int NativeSolutionPhase')
replace('            public bool solutionBoard;', '            public bool solutionBoard, previewAutomatic;\n            public float previewDelay;')
replace('''            stepCursor++; MoveCount++; SelectedPieceIndex = actorIndex;
            Status =''','''            stepCursor++; MoveCount++; SelectedPieceIndex = actorIndex;
            if (previewAutomatic)
            {
                previewNextStepTime = Time.time;
                if (stepCursor >= steps.Count) { previewAutomatic = false; MoveBloque.modoSolucion = 3; MoveBloque.MoviendoBloque = false; }
            }
            Status =''')
replace('''        void CancelStepPlayback()
        {
            var playback''','''        void CancelStepPlayback()
        {
            previewAutomatic = false;
            MoveBloque.MoviendoBloque = false;
            var playback''')
replace('stepPlaybackState = RecoveredNativeStepPlayback.Attach(this).CaptureState() };', 'stepPlaybackState = RecoveredNativeStepPlayback.Attach(this).CaptureState(), previewAutomatic = previewAutomatic, previewDelay = Mathf.Max(0, previewNextStepTime - Time.time) };')
replace('            RecoveredNativeStepPlayback.Attach(this).RestoreState(saved.stepPlaybackState);', '            RecoveredNativeStepPlayback.Attach(this).RestoreState(saved.stepPlaybackState);\n            previewAutomatic = saved.previewAutomatic; previewNextStepTime = Time.time + saved.previewDelay;')
replace('''            RestoreSnapshot(saved);
            Status = "Last move undone.";''','''            RestoreSnapshot(saved);
            // Recovery Undo pauses the recording at the restored transaction.
            previewAutomatic = false;
            Status = "Last move undone.";''')
replace('''                RecoveredNativeStepPlayback.Attach(this).ResetPlayback();
                MoveCount''','''                RecoveredNativeStepPlayback.Attach(this).ResetPlayback();
                previewAutomatic = stepSolution; previewNextStepTime = Time.time + 2f;
                MoveCount''')
replace('''        void ClearBoard()
        {
            var playback''','''        void ClearBoard()
        {
            previewAutomatic = false;
            var playback''')
replace('''            CheckWaterVictory();
            if (Input.GetKeyDown(KeyCode.H))''','''            CheckWaterVictory();
            if (previewAutomatic && !IsSolutionStepAnimating && Time.time >= previewNextStepTime)
            {
                // HandleSolution102E854..8DC begins phase1 after phase9.
                // Missing original capture stalls, rather than silently skipping.
                previewNextStepTime = float.PositiveInfinity;
                ApplyHint();
            }
            if (Input.GetKeyDown(KeyCode.H))''')
replace('            public int moves, cursor;', '            public int moves, cursor, movementMode;')
replace('moves = MoveCount, cursor = stepCursor,', 'moves = MoveCount, cursor = stepCursor, movementMode = MoveBloque.modoSolucion,')
replace('''            previewAutomatic = saved.previewAutomatic; previewNextStepTime = Time.time + saved.previewDelay;''','''            previewAutomatic = saved.previewAutomatic; previewNextStepTime = Time.time + saved.previewDelay;
            MoveBloque.modoSolucion = saved.movementMode;''')
replace('''                previewAutomatic = stepSolution; previewNextStepTime = Time.time + 2f;''','''                previewAutomatic = stepSolution; previewNextStepTime = Time.time + 2f;
                MoveBloque.modoSolucion = 0;''')
replace('''                previewNextStepTime = float.PositiveInfinity;
                ApplyHint();''','''                previewNextStepTime = float.PositiveInfinity;
                MoveBloque.modoSolucion = 2;
                ApplyHint();''')
out=root/'RecoveredFlowGame.pending-native-step-playback.cs.src'
out.write_text(text,'utf-8')
print(out,hashlib.sha256(out.read_bytes()).hexdigest())
