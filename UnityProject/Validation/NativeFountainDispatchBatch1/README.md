# Original FT3 fountain dispatch, wet lights and letter gradient selector

Depends on NativeDecorativePitBatch1 (WaterAssets/Motion hooks and provider catalog). Changes only rendered effects and their original script configuration. Main Assets remain untouched. Graph, source activation, first-color rules, frontier delays, collision, input, springs and target counts do not change. Runtime and complete Editor compile with Unity2022.3.28f1 Roslyn,0errors.

## Complete actual-kind audit

See Evidence/native-FT3-dispatch-coverage.md/json. Actual6032 configured C contexts: type2=5279,type3=50,type4=8,type6=3,type8=364,type9=328; types1/5/7 absent. This batch closes every identified actual-type provider omission together, rather than inventing an effect per rendered gap.

1. Original types3/4: doInstanciarFuente10A6798..67C4 calls doInstanciarChorros, plus doFuente3 for type4. Every actual3/4 config has empty Pit/Anim/Lights and zero primary/border/center. Native fallback depends only on dirPlano1/2==1 and inletcolor.32 exact original executions produce default Chorro at localX/Z±4,Y1.06; type4 additionally produces original Font at local(10,24,-10). Existing type6 Big atY25 stays separate and is tested.

2. B9 gate: doProcessEnter10A4E60..70 skips doInstanciarFuente completely for configFuenteB9. Existing adapter incorrectly spawned primary fountains on157 actual type2 Jet configurations with configFuente1. Correcting the visual gate retains the separate C/D/E output renderer, source counts and native routes.

3. Native wet center light: doProcessEnter10A4EB4..ED8 skips FuenteEB6 and calls doLights only under original FT3 night(part36..39), after fountain spawning. This is separate from static wall lights. All36 actual configured light strings were executed,0failures;16 produce lights when first digit1/5/7. Source7 moves original Light tolocalY6.5 and intensity6; other cases preserve originalY2.8/prefab defaults and exact doConfigColor1 RGB. Native doExit explicitly destroys Lights1/2; existing owned-channel disposal removes these instances too.

4. Letter particle palette: original FT3 encodes D as subcolor4 with colx1; numeric4 uses colx0. It does not rewrite lig2 D into another string. doLuzColor106EA34..70 branches on colx. Prior gradient adapter ignored this selector.368 exact original changeColorP/U colx!=0 evaluations added,0failures. Existing numeric-colx0 entries stay unchanged. Example rawD4: numeric palette starts(0.9607843,0.8705882,0.3215686); original letter palette starts(0.13333334,1,0.047058824). New provider fields now select their correct original table. The adapter retains original field selector for retained-age replay instead of running the disabled native Start stub.

All exact placement/state mutations share the already restored provider binding, including authored Animator enabled defaults. No guessed animcode4 or synthetic clip path is added.

## Actual Unity fixture

Entry: Recovered.FlowWater.Editor.FlowWaterFountainDispatchValidation.Run
Output:Validation/FountainDispatch/fountain-dispatch-validation.json and five board PNGs.

Actual456/549 test types3/4 original default-pit poses and emitting particles;518 tests actual type6 Big atY25 and emission;1058 tests a real wet type2/B9 skips incorrect fountain families while its original E routing object remains;2001 tests actual record82 Light71 atY6.5/intensity6, FuenteEB6 light-skip, and record81's exact original colx1/D4 gradient. Every case first requires real all-target correct-color flow. Production source-disable on456 then verifies old owned generated provider instances are actually disposed, matching native doExit. Scene/preferences/time restored; tutorial dismissed by real native turn/return.

## Kept separate actual failure evidence

ProviderBatch1 failed its47thcheck on code1 motion because its signature omitted ForceOverLifetime. Actual Diagnostic2 identified record74 chorroLateralRo playing An_testchorro1 on real FontTest9(2)/ForceModule.y.scalar, not velocity. Editor-only NativePitValidationBatch3 adds actual Force sampling, with a standalone native range test; original failed reports remain. Production Animator behavior is not changed to satisfy that assertion. The separate corrected full6case provider suite must still be run by root.

Root owns actual Unity launches and visual inspection. Compilation/static original evaluations are not presented as actual content-test passes.
