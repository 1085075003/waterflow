# Original FT3 fountain dispatch coverage

Actual6032 C configured contexts, before Awake/Start; native FT3 method dispatch cross-checked directly against ARM. Counts are configured records, not unique prefabs or game levels.

| tipo | actual contexts | original fountain dispatch | current recovered provider |
|---|---:|---|---|
| 1 | 0 | No fountain/count branch (<2); common flow only | No actual C config; no extra provider required |
| 2 | 5279 | B!=9: primary, border(if!=9), center, nonempty/non5000/6000 Pit1 decorative provider; B9 skips all fountains | NativeWaterAssets+FountainAnimation+DecorativePitBatch1; DispatchBatch1 adds proven B9 skip |
| 3 | 50 | doInstanciarChorros; actual Pit empty uses dirPlano1/2 fallback | DispatchBatch1 RestoreSmall: exact native default jet |
| 4 | 8 | doInstanciarChorros then doFuente3; actual empty Pit/default config | DispatchBatch1 RestoreSmall: default jet plus original Font at(10,24,-10) |
| 5 | 0 | doInstanciarChorros | No actual C config; native branch documented, no fabricated content |
| 6 | 3 | prefabBig[waterColor], parent origin, local(0,25,0), original scale | Existing SpawnAtPiece; Dispatch fixture actual518 separately checks Big and emitting particles |
| 7 | 0 | No extra fountain spawn inside doInstanciarFuente; count if reached | No actual C config |
| 8 | 364 | doProcessEnter excludes fountain/count; doActivarEsfera dispatches doActivarChorro | Existing native Jets C/D/E provider; authored Rendija configFuente2 supplies original source marker |
| 9 | 328 | Count if reached; no type-specific doInstanciarFuente object; doActivarEsfera dispatches doActivarChorro | Existing native Jets C/D/E provider; depends on actual inlet water, not unconditional source |

Actual nonzero kinds sum6032: kind2=5279,3=50,4=8,6=3,8=364,9=328. None of these kinds is treated as an unconditional source merely because it is a fountain/jet.

Common native entry:
- doProcessEnter10A4E44..E70: choose inletcolor1..4 water prefabs and doInstanciarAgua; tipo>=2 &&tipo!=8 counts; B9 skips doInstanciarFuente
- doActivarEsfera10A7044..706C: tipo8/9 or B9 dispatches doActivarChorro after actual native espera; original bottom6 is fixed marker, not arbitrary long fall
- Night10A4EB4..ED8: after fountain provider, skip FuenteEB6; doLights only when native FT3night true(part36..39); configLi first1/5/7 creates wet center light

All owned generated flow/fountain/light objects are disposed by the existing channel exit. Native doExit10A3FD8..4694 explicitly destroys Agua1/2/4..15 plus Lights1/2; the recovered root owns those exact instances and removes them together. Unrelated rebuilds retain IDs; actual disconnect disposal is tested separately.

Known visual omissions identified and closed together in the next immutable dispatch batch:
- actual58 type3/4 missing providers
- incorrect type2 B9 fountain-family spawning
- separate wet FT3.doLights missing (static wall lights do not replace it)
- letter-coded changeColorP/U colx1 gradient table was missing; add368 exact native evaluations; existing numeric-colx0 recipes unchanged

Actual verification status:
- Existing real native Jet1656/1660/1701... and full representative counts passed; parent retains exact suite reports
- C6 exact marker contact actual12/12 and real1236 pointer22steps16/16 passed
- Provider1 actual codes2/5/7 and child0 native y10/y16/z-2 passed before fixture47 failed on code1 signature omitting Force
- Actual diagnostic2001 explicit1 is record74 An_testchorro1 ForceModule.y.scalar; original code1 parameter/state/time and target valid. Corrected Force-aware validation pending parent
- Upcoming Dispatch fixture real456/518/549/1058/2001; real all-target flow plus subtype particles/poses/B9 split/wet light/colx1 color/dispose

Remaining scope:
- No additional actual C tipo dispatch branch remains without a specified runtime provider after DispatchBatch1; unused tipo1/5/7 are documented and not claimed content-tested
- Source IL2CPP trigger scripts remain reconstructed scheduler adapters; literal source-code equivalence is not claimed
- Full1350 final visual captures and all800 normal pointer/physics verification remain parent-owned; no static extraction or compile is claimed an actual content pass
