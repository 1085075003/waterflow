import json
import pathlib
import sys

here = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(here / "python"))
import UnityPy
from UnityPy.helpers import CompressionHelper
from UnityPy.streams import EndianBinaryReader
from UnityPy.export.ShaderConverter import ShaderProgram

root = pathlib.Path.cwd()
environment = UnityPy.load(str(root / "raw/base/assets/bin/Data/data.unity3d"))
names = {"Transparent/Diffuse ZWrite", "Transparent/Diffuse ZWrite1", "Mobile/Diffuse Color Mig", "Mobile/Diffuse Color Mig2", "Mobile/Diffuse Color", "Particles/Additive Intensify", "Feelnside/Mobile-ColoredRim"}
manifest = []
for obj in environment.objects:
    if obj.type.name != "Shader":
        continue
    shader = obj.read()
    name = shader.m_ParsedForm.m_Name
    if name not in names:
        continue
    safe = name.replace("/", "_")
    tree = obj.read_typetree()
    state = tree.get("m_ParsedForm", {})
    (here / (safe + ".parsed.json")).write_text(json.dumps(state, indent=2), encoding="utf-8")
    for platform_index, platform in enumerate(shader.platforms):
        chunks = []
        lengths = shader.compressedLengths[platform_index]
        offsets = shader.offsets[platform_index]
        decompressed = shader.decompressedLengths[platform_index]
        if not isinstance(lengths, list): lengths, offsets, decompressed = [lengths], [offsets], [decompressed]
        for offset, size, expanded in zip(offsets, lengths, decompressed):
            chunks.append(CompressionHelper.decompress_lz4(bytes(shader.compressedBlob)[offset:offset+size], expanded))
        raw_program = b"".join(chunks)
        (here / (safe + ".platform" + str(platform) + ".bin")).write_bytes(raw_program)
        try:
            program = ShaderProgram(EndianBinaryReader(raw_program, endian="<"), obj.version)
            text = "\n\n".join("// SUBPROGRAM " + str(i) + "\n" + part.Export() for i, part in enumerate(program.m_SubPrograms))
        except Exception as error:
            text = "// Parser error: " + str(error) + "\n" + raw_program.decode("utf-8", errors="replace")
        (here / (safe + ".platform" + str(platform) + ".txt")).write_text(text, encoding="utf-8", errors="replace")
    try:
        exported = shader.export()
        (here / (safe + ".glsl.txt")).write_text(exported, encoding="utf-8")
        detail = "exported " + str(len(exported))
    except Exception as error:
        detail = str(error)
    manifest.append({"name": name, "pathId": obj.path_id, "platforms": shader.platforms, "blobBytes": len(shader.compressedBlob or []), "detail": detail})
    print(manifest[-1], flush=True)
(here / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
