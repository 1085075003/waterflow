# Native Jet spring chain batch 1

Only RecoveredWaterJets.cs and its unchanged meta are imported. Main Assets untouched by the producer. Do not overwrite the frozen runtime while the all800 snapshot runs; root controls the import window.

Native FuenteTest3.doActivarChorro first dispatches configRes1[0] to a jet slot and only then passes configRes1.Substring(1,3) into CascadaTestE.conResorte1. CascadaTestE.Start parses that three-character payload as channel/mode/direction. Existing adapter erroneously used the four-character control string directly and selected configResN by jet slot.

Proof:
- Slot1:10A7848..786C tests prefix1;10A7878..7888 Substring(1,3) writes E+0xB8. Separate configRes2 prefix1 writes E+0xC0 at10A78A0..78E0; preserved in spawned E.conResorte2, without inventing a consumer.
- Slot2:10A7FF4..8034 still reads configRes1, compares2, then writes its suffix to E+0xB8.
- Slot3:10A84F0..8530 reads configRes1, compares3, suffix writes E+0xB8.
- Slot4:10A89EC..8A2C also reads configRes1 and compares literal3 (x21 loaded at10A8874), matching the original method rather than silently correcting its apparent copy-paste behavior.
- Only initial pit kinds3/4 run these spring assignment branches. Initial7 has separate E creation with no spring assignment; C/D also unaffected.
- E.Start FD9FB8..FDA008 parses conResorte1.Substring(0,1),(1,1),(2,1).
- E.doCheckConfig FDA2E8..FDA300 uses active pitR1/pitR2 only when state==1; every other state restores pitIni1/pitIni2. Existing graph/readRoutes already does exactly that fallback.

gm1656 record8 raw configRes1=1117 now produces E.conResorte1=117. When true channel1 publishes1, mode1/direction7 gives south(-Z)40, from(40,3.95,80) to record9(40,3.95,40), permitting record9 conOri5 B to activate channel2. No target color/geometry/physics constraint is weakened.

Renderer/particle consistency: route.mode/direction feed the same model localYaw and original native particle velocity/force/lifetime recipe that defines graph displacement. Spawned E stores the trimmed three-character value. Network ChannelSignature excludes route changes (VisualSignature alone includes mode/direction/displacement), so inlet water/front/count state persists while actual original jet visuals are refreshed.

Full current runtime compiled successfully with2022.3.28f1 Roslyn. Actual Unity normal gm1656 native-input replay remains root-owned validation; do not label it passed from compilation.
