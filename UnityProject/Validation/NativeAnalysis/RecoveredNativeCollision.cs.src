// Native layer assignments are recorded in
// Validation/NativeAnalysis/native-collider-lifecycle.json.
using System;
using UnityEngine;

namespace Recovered.FlowWater
{
    public static class RecoveredNativeCollision
    {
        public static void Apply(RecoveryPiece piece)
        {
            if (piece == null || UIManagerSelScript.var_design != 0) return;
            foreach (MonoBehaviour component in piece.GetComponentsInChildren<MonoBehaviour>(true))
            {
                var ordinary = component as ControlPieza;
                if (ordinary != null)
                {
                    // Start107F8D8: the nonfixed material branch jumps
                    // directly to1080CB0, bypassing the layer0 assignment.
                    if (ordinary.esPiezaFija) SetLayer(ordinary.objeto, 0);
                    continue;
                }
                var fixedPiece = component as ControlFijo;
                if (fixedPiece != null)
                {
                    // Piezatrans1 bypasses the fixed-material/layer block.
                    if (fixedPiece.esPiezaFija && !OriginalName(fixedPiece.gameObject, piece).Contains("Piezatrans1"))
                        SetLayer(fixedPiece.objeto, 0);
                    // Independent later branch1077FB0..1078050.
                    if (ParentName(fixedPiece.objeto, piece).Contains("BloqueJapo")) MapFloor(fixedPiece.objeto);
                    continue;
                }
                var wallPiece = component as ControlFijo1;
                if (wallPiece != null)
                {
                    if (wallPiece.esPiezaFija) SetLayer(wallPiece.objeto, 0);
                    continue;
                }
                var upward = component as ControlPiezaPfuente;
                if (upward != null)
                {
                    if (upward.esPiezaFija) SetLayer(upward.objeto, 0);
                    continue;
                }
                var source = component as ControlPiezaFuente;
                if (source != null)
                {
                    // Awake108FE34: applies regardless of fixedness.
                    if (source.objetoCollider != null)
                    {
                        source.objetoCollider.tag = "Map";
                        SetLayer(source.objetoCollider, source.indicadorSuelo1 ? LayerMask.NameToLayer("Suelo1") : 0);
                    }
                    continue;
                }
                var pool = component as ControlPiezaC;
                if (pool != null)
                {
                    // Start1084608: x25 is the literal FuenteED; the
                    // alternative literal FuenteEB6 is read at108468C.
                    string parent = ParentName(pool.objeto, piece);
                    if (parent.Contains("FuenteED") || parent.Contains("FuenteEB6")) SetLayer(pool.objeto, 0);
                    else
                    {
                        MapFloor(pool.objeto);
                        // 1085398..10853B4 branches to the out-of-line block
                        // 1088DD0: movable BloqueChorro2 explicitly returns
                        // to bloque12; fixed variants override Suelo1 with0.
                        if (parent.Contains("BloqueChorro2"))
                            SetLayer(pool.objeto, pool.esPiezaFija ? 0 : LayerMask.NameToLayer("bloque"));
                    }
                }
            }
        }
        static void SetLayer(GameObject target, int layer) { if (target != null && layer >= 0) target.layer = layer; }
        static void MapFloor(GameObject target)
        {
            if (target == null) return;
            target.tag = "Map";
            SetLayer(target, LayerMask.NameToLayer("Suelo1"));
        }
        static string ParentName(GameObject target, RecoveryPiece piece)
        { return target == null || target.transform.parent == null ? "" : OriginalName(target.transform.parent.gameObject, piece); }
        static string OriginalName(GameObject target, RecoveryPiece piece)
        { return target == piece.gameObject ? piece.BasePrefab ?? "" : target.name; }
    }
}
