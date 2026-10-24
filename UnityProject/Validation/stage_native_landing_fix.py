"""Prepare native landing changes separately from an active Unity test run."""
import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parent
source = root.parent / "Assets/Scripts/RecoveryRuntime/RecoveredFlowGame.cs"
raw = source.read_bytes()
text = raw.decode("utf-8-sig").replace("\r\n", "\n")
old = "if ((piece.transform.position - destination).sqrMagnitude < .05f) return false;"
new = "if (!nativeLanding && (piece.transform.position - destination).sqrMagnitude < .05f) return false;"
assert text.count(old) == 1
text = text.replace(old, new)
start = text.index("        int Occupant(Vector3 position, int except)")
end = text.index("        public void ApplyHint()", start)
text = text[:start] + '''        int Occupant(Vector3 position, int except)
        {
            // Native doCheckTarget probes the actual authored colliders at
            // guideY+subeRayo, before doEnd adds4.2 to the landing guide.
            // A thin Piezatrans support may share the actor root position.
            float offset = 2f - 4.199999809265137f, radius = 1f;
            int mask = 4097;
            foreach (MoveBloque movement in UnityEngine.Object.FindObjectsOfType<MoveBloque>(true))
                if (movement != null && GameCamera != null && movement.gameObject.scene == GameCamera.gameObject.scene)
                { offset = movement.subeRayo - 4.199999809265137f; radius = movement.sphereRadius; mask = movement.mascaraSuelo.value; break; }
            using (RecoveredNativeInteraction.QueryOriginalColliders(this))
                foreach (Collider collider in Physics.OverlapSphere(position + Vector3.up * offset, radius, mask, QueryTriggerInteraction.UseGlobal))
                {
                    if (collider.name.IndexOf("OrigenCentro", StringComparison.Ordinal) >= 0 ||
                        collider.name.IndexOf("Origen6", StringComparison.Ordinal) >= 0 ||
                        collider.name.IndexOf("CuboResorte", StringComparison.Ordinal) >= 0) continue;
                    RecoveryPiece occupant = collider.GetComponentInParent<RecoveryPiece>();
                    if (occupant == null || occupant.Index == except || occupant.GetComponent<Resorte>() != null) continue;
                    return occupant.Index;
                }
            return -1;
        }

''' + text[end:]
folder = root / "NativeLandingFix"
folder.mkdir(exist_ok=True)
destination = folder / "RecoveredFlowGame.cs.src"
destination.write_text(text, encoding="utf-8", newline="\n")
manifest = {
    "source": str(source),
    "baseSha256": hashlib.sha256(raw).hexdigest(),
    "staged": str(destination),
    "stagedSha256": hashlib.sha256(destination.read_bytes()).hexdigest(),
    "scope": [
        "Native same-cell pickup/release commits an undoable recovery transaction; no claim that this is an original visible move counter.",
        "Native serialized Map column sphere geometry replaces root-position occupancy; Piezatrans thin supports remain legal without exempting solid fixed blocks.",
    ],
}
(folder / "manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps(manifest, ensure_ascii=False))
