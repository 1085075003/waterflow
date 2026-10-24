import pathlib,hashlib
root=pathlib.Path(__file__).resolve().parent
text=(root.parent/'NativeStepPlaybackBatch2/Assets/Editor/RecoveredTools/FlowWaterStepPlaybackValidation.cs').read_text('utf-8-sig')
def patch(a,b):
 global text
 assert text.count(a)==1,(a[:80],text.count(a))
 text=text.replace(a,b)
patch('            public List<string> runtimeErrors=new List<string>();','            public List<string> runtimeErrors=new List<string>();\n            public List<string> captureTraces=new List<string>();\n            public List<string> finalActorMaps=new List<string>();')
patch('        public static void RunLifts() { Start(new[]{1129,1754,1961}); }','        public static void RunLifts() { Start(new[]{1129,1754,1961}); }\n        public static void RunMissingCapture() { Start(new[]{1225}); }')
patch('                Vector3[] before=Positions(); int originalMode=MoveBloque.modoSolucion;', '                Vector3[] before=Positions(); int originalMode=MoveBloque.modoSolucion;\n                int[] originalRecords=Game.Pieces.Select(x=>x.SourceRecordIndex).OrderBy(x=>x).ToArray();')
patch('                FlowWaterValidation.SaveVisibleFrame(Path.Combine(Output,"gm"+id+"-animated-preview.png"));','''                Require("gm"+id+"-preserves-original-actor-inventory",Game.Pieces.Select(x=>x.SourceRecordIndex).OrderBy(x=>x).SequenceEqual(originalRecords),"Automatic animation reuses original actors; it does not fabricate missing captures.");
                foreach(string trace in Game.SolutionCaptureTrace) report.captureTraces.Add("gm"+id+" "+trace);
                foreach(RecoveryPiece piece in Game.Pieces) report.finalActorMaps.Add("gm"+id+" record"+piece.SourceRecordIndex+":"+piece.BasePrefab+" position="+piece.transform.position.ToString("F3")+" rotation="+piece.transform.rotation.ToString("F4"));
                if(id==1225)
                {
                    string capture=Game.SolutionCaptureTrace.FirstOrDefault(x=>x.StartsWith("28 FRESH ",StringComparison.Ordinal) || x.StartsWith("28 RETAINED ",StringComparison.Ordinal));
                    string previous=Game.SolutionCaptureTrace.LastOrDefault(x=>x.StartsWith("27 FRESH ",StringComparison.Ordinal) || x.StartsWith("27 RETAINED ",StringComparison.Ordinal));
                    Require("gm1225-original-last-capture-present",capture!=null && previous!=null,"Raw step28 is same-cell non-z; source may have arrived through a genuine spring, so it must be captured or retain the original previous body.");
                    int selected=Record(capture),preceding=Record(previous);
                    bool retained=capture.IndexOf(" RETAINED ",StringComparison.Ordinal)>=0;
                    Require("gm1225-exact-native-body-lifetime",retained?selected==preceding:selected==24,"Step28="+capture+"; step27="+previous+". Fresh capture is the original spring-carried record24; an empty query retains the exact previous body, never invents one.");
                    RecoveryPiece finalActor=Game.Pieces.FirstOrDefault(x=>x.SourceRecordIndex==selected);
                    Vector3 raw=RecoveredFlowGame.StepPosition(4,2,4);
                    bool exact=finalActor!=null && ((finalActor.transform.position-raw).sqrMagnitude<.000001f || (finalActor.transform.position-(raw+Vector3.up*.04999971389770508f)).sqrMagnitude<.000001f);
                    Require("gm1225-native-final-actor-map",exact,"Selected original record"+selected+" must finish at raw(4,2,4), or the exact native roof route guide(-4.15)+4.2 offset. Actual="+(finalActor==null?"missing":finalActor.transform.position.ToString("F4"))+"; retained="+retained+"; this is preview geometry proof, not normal target victory.");
                }
                FlowWaterValidation.SaveVisibleFrame(Path.Combine(Output,"gm"+id+"-animated-preview.png"));''')
patch('        static Vector3[] Positions()', '''        static int Record(string trace)
        {
            int start=trace.IndexOf("record",StringComparison.Ordinal)+6,end=trace.IndexOf(':',start);
            return Int32.Parse(trace.Substring(start,end-start));
        }
        static Vector3[] Positions()''')
out=root/'FlowWaterStepPlaybackValidation.pending-retained-capture.cs.src';out.write_text(text,'utf-8');print(out.name,hashlib.sha256(out.read_bytes()).hexdigest())
