# Native fountain Animator batch1

This independent batch restores native Animator operations on the exact already-generated fountain instance. No water topology, physics, timing, counts or placement recipes change. Main Assets remain untouched until root imports this manifest.

## Original proof

The read-only ARM evaluator now tracks GetComponent<Animator> back to its exact Object.Instantiate object. It records SetInteger and enabled writes from doFuente1B/2B/1A/5A, with original call VAs. Existing placement-context/color/mode variants:2440 evaluations,0 extraction failures,7 native instance-owner VAs,23 final operation recipes. Repeated helper writes to the same reused object are compared by their final properties; no Animator.Update is called in those original helpers.

Examples: doFuente2B10AC710/760 assigns animcode2/3 and enables; doFuente1B10ABF2C/7C similarly; configAnim1 only enables; doFuente1A/5A additionally enable with other nonempty configuration values, according to exact extraction. No guessed animcode1/4 is assigned.

## Runtime chain

RecoveredNativeWaterAssets passes the existing recipe's nativeInstantiateVA plus actual configAnim into RecoveredNativeFountainAnimation.Bind. Only the exact original new object's root Animator is eligible; descendants are not searched. Only seven proved native owner VAs bind. For eligible instances with no native enabled operation, authored enabled state is preserved instead of WaterMotion globally enabling it.

RecoveredWaterMotion delegates those bound Animators to the helper. It restores authored/native enabled state, Rebinds, reapplies native integer parameters, then evaluates original motion at the retained source age. Applying before Rebind would lose parameters. Other original water/particle Animator behavior is unchanged. Unknown/synthetic clip paths are not renamed or fabricated.

## Actual Unity verification required

Entry: Recovered.FlowWater.Editor.FlowWaterFountainAnimationValidation.Run
Report: Validation/FountainAnimation/fountain-animation-validation.json

This opens actual1101 and1193, uses production Solve/final layout, waits for real all-target flow, finds actual post-water native generated fountains with configAnim3/2, asserts Animator integer survives Rebind, actual original controller transition state is reached, and actual local transform or particle velocity changes across real frames. It also checks unrelated Rebuild keeps the configured animation code. Preferences, scene setup and time scale restore on exit. Actual Unity execution is root-owned; compilation is not a pass claim.

## Separate known provider gap still under investigation

FT3.doInstanciarFuente10A684C..68A8 additionally calls doInstanciarChorros for type2 when configPit1 is nonempty and not5000/6000; doConfigPit invokes doPitA/B. The current renderer adapter has not restored that full decorative-pit provider, whose Animator operations are therefore not blindly applied to Geometry's different static hardware. This batch only covers the seven proved owner VAs already spawned by existing primary/border/center recipes. The original doPitA orphan path remains orphan; this patch does not claim it repaired.

Runtime and complete Editor compile with Unity2022.3.28f1 Roslyn, zero errors. No Unity process launched by this agent.
