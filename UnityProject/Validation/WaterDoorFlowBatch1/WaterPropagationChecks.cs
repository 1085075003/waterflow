using System;
using System.Collections.Generic;
using Recovered.FlowWater;

static class WaterPropagationChecks
{
    static int checks, entered, exited, retained;
    static void Check(bool value, string message)
    { checks++; if (!value) throw new Exception(message); }
    static RecoveredWaterPropagation.Channel Channel(string key, bool source = false, float prime = 0)
    { return new RecoveredWaterPropagation.Channel { key = key, signature = 7, source = source,
        sourceColor = 1, sourceIncoming = 5, sourceDelay = prime }; }
    static void Link(RecoveredWaterPropagation.Channel a, RecoveredWaterPropagation.Channel b, float delay, string identity)
    { a.outputs.Add(new RecoveredWaterPropagation.Link { target = b, identity = identity, incoming = 1, delay = intake => delay }); }
    static void Rebuild(RecoveredWaterPropagation p, float now, params RecoveredWaterPropagation.Channel[] nodes)
    { p.Rebuild(nodes, now, c => entered++, c => exited++, (a,b) => retained++); }
    static void Main()
    {
        var p = new RecoveredWaterPropagation();
        var a = Channel("0/source", true, .5f); var b = Channel("1/pipe"); var c = Channel("2/fountain");
        Link(a,b,1.5f,"source-exit"); Link(b,c,1.5f,"pipe-exit");
        Rebuild(p,0,a,b,c);
        Check(entered == 0 && a.color == 0, "Priming source became wet before native .5s callback");
        p.Advance(.49f); Check(entered == 0, "Early source priming");
        p.Advance(.5f); Check(entered == 1 && b.color == 0, "Source/pipe front did not remain separate");
        a = Channel("0/source",true,.5f); b = Channel("1/pipe"); c = Channel("2/fountain");
        Link(a,b,1.5f,"source-exit"); Link(b,c,1.5f,"pipe-exit"); Rebuild(p,1,a,b,c);
        Check(retained == 1 && entered == 1, "Unrelated rebuild recreated an arrived source");
        p.Advance(1.99f); Check(b.color == 0, "Pipe callback became early");
        p.Advance(2); Check(b.color == 1 && c.color == 0, "Pipe did not enter at original source entry+espera");
        var saved = p.CaptureState(2.5f);
        a = Channel("0/source",true,.5f); b = Channel("1/pipe"); c = Channel("2/fountain");
        Link(a,b,1.5f,"source-exit"); Link(b,c,1.5f,"pipe-exit"); p.RestoreState(saved); Rebuild(p,100,a,b,c);
        p.Advance(100.99f); Check(c.color == 0, "Undo lost remaining output duration");
        p.Advance(101); Check(c.color == 1, "Undo did not restore pending front callback");
        a = Channel("0/source",true,.5f); b = Channel("1/pipe"); c = Channel("2/fountain");
        Link(b,c,1.5f,"pipe-exit"); Rebuild(p,102,a,b,c);
        Check(a.color == 1 && b.color == 0 && c.color == 0, "Disconnected downstream ancestry stayed wet");
        Check(p.PendingCount == 0, "Disconnected pending delivery survived");
        a = Channel("0/source",true,.5f); b = Channel("1/pipe"); c = Channel("2/fountain");
        Link(a,b,1.5f,"source-exit"); Link(b,c,1.5f,"pipe-exit"); Rebuild(p,103,a,b,c);
        Check(b.color == 1 && c.color == 0, "Already-active upstream outlet failed to reconnect immediately");
        p.Advance(104.5f); Check(c.color == 1, "Newly-entered reconnected branch lost its delay");

        p.Reset(); var red = Channel("red",true); red.sourceColor = 4;
        var blue = Channel("blue"); blue.expectedColor = 1; Link(red,blue,0,"colored-intake");
        Rebuild(p,0,red,blue); Check(blue.color == 0 && p.PendingCount == 0, "Wrong-color water entered a colored target");
        p.Reset(); a = Channel("source",true); b = Channel("overlap-A"); c = Channel("overlap-B");
        Link(a,b,.4f,"overlap-a"); Link(a,c,.4f,"overlap-b"); Rebuild(p,0,a,b,c); p.Advance(.4f);
        Check(b.color == 1 && c.color == 1, "Coincident independent intake triggers did not fan out");
        // Real gm1140 topology: water arriving at a downstream yellow hole
        // opens an earlier outlet while that earlier node keeps its intake.
        p.Reset(); a = Channel("source", true); b = Channel("door-node"); c = Channel("hole");
        var side = Channel("opened-branch"); b.inputMask = 3;
        Link(a,b,0,"upstream"); Link(b,c,.4f,"existing-flow"); Rebuild(p,0,a,b,c,side); p.Advance(.4f);
        float entry = b.enteredAt, holeEntry = c.enteredAt;
        a = Channel("source",true); b = Channel("door-node"); c = Channel("hole"); side = Channel("opened-branch");
        b.inputMask = 7; Link(a,b,0,"upstream"); Link(b,c,.4f,"existing-flow"); Link(b,side,.4f,"opened-door"); Rebuild(p,1,a,b,c,side);
        Check(b.color == 1 && c.color == 1 && b.enteredAt == entry && c.enteredAt == holeEntry,
            "Opening an outlet reset the still-fed channel or the downstream door hole");
        Check(side.color == 1, "An opened outlet did not deliver the existing mature flow");
        a = Channel("source",true); b = Channel("door-node"); c = Channel("hole"); side = Channel("opened-branch");
        b.inputMask = 3; Link(a,b,0,"upstream"); Link(b,c,.4f,"existing-flow"); Rebuild(p,2,a,b,c,side);
        Check(b.color == 1 && c.color == 1 && side.color == 0, "Closing a separate outlet interrupted an unrelated wet branch");
        a = Channel("source",true); b = Channel("door-node"); c = Channel("hole"); side = Channel("opened-branch");
        b.inputMask = 2; Link(a,b,0,"upstream"); Link(b,c,.4f,"existing-flow"); Rebuild(p,3,a,b,c,side);
        Check(b.color == 0 && c.color == 0 && a.color == 1 && p.PendingCount == 0,
            "Closing the real intake kept the old water or allowed stale delivery");
        Console.WriteLine("PASS " + checks + " propagation state/timing checks");
    }
}
