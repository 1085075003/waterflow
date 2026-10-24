# Native C bottom marker contact, batch1

Only RecoveredWaterNetwork.cs changes. Main Assets remain untouched. Runtime and all current Editor sources compile with Unity2022.3.28f1 Roslyn, zero errors. Root owns actual Unity execution; no pass is claimed from compilation.

## Native causal proof

Actual normal native-pointer input gm1236 completes all22 legal moves but ends14/16. Record69 ControlPiezaC at(40,19.15,0) was linked through bottom6 to ordinary-pipe record33 at(40,3.95,0), a15.2 drop. Purple3 incorrectly arrives first, and downstream targets57/58 correctly refuse that color instead of red4. The original first-color rule is preserved.

CuFuenteEB6.prefab:113 prefabAguaControl references AguaControl5C. Its esfera6 Transform at1616 has localPosition(0,-8,0), localScale(2,2,2); SphereCollider1632 radius.1; Rigidbody1653 has useGravity0,isKinematic1. Original ControlC root10 times water root.1 gives worldoffset-8 and worldradius.2. FT3.doActivarEsfera10A6FD0..7010 enables/tags esfera6; doActivarCascada10A7080..72DC handles only1..4. There is no C Cascada6 to extend this marker down an empty layer.

Recta2j uses AguaControl5. Its original origen5 points to actual-named OrigenCentro: centered localPosition0/localScale1, SphereCollider radius.4. AguaControl5C.OrigenCentro has the same shape. Adjacent root spacing7.6 leaves marker/intake centers.4 apart, within radius sum.6; a15.2 drop leaves7.2 and cannot touch.

## Exact scoped fix

For ControlPiezaC bottom6 only, FindBottomReceiver now requires intersection of the original transformed esfera6 sphere and the candidate's actual Origin5 collider. Original root/local scales, actual pose delta, sphere center/radius and supported intake sphere/box shape determine contact. There is no arbitrary distance cap and no added reach fallback. The1e-6 squared-distance epsilon only covers float precision. Null/absent shapes fail closed.

C bottom output delivery retains the native FT3 espera delay, without the additional long-Cascada callback delay that C does not implement. Ordinary CP bottom6 still uses its original long waterfall reach and cascade timing. Water source order, first-color ownership, targets, collision layers and other port rules do not change.

## Root actual verification

Run existing production native-input focused fixture gm1236: all22 actual pointer pick/drop steps, all16 correctly colored target fountains, native win. Check graph69.links[5] is absent across15.2 and red enters33 from39. Then run representative/final-layout water cases to verify adjacent C bottom contacts remain connected and ordinary CP long falls still work. No lowering of target/pass criteria.
