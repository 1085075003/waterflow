# gm2001 water shader, UV and alpha review

Read-only review, 2026-10-16. All three project Assets remained unchanged during the audit. The new UV fix is an immutable, separately tested candidate in `Validation/NativeWaterUV1`.

Inspected capture: `.validation-project/Validation/FountainDispatch/gm2001-native-dispatch.png`, actual Unity 2022.3.28f1 report started `2026-10-16T03:09:17.5310198Z`, finished `2026-10-16T03:09:42.6965860Z`. This screenshot predates the final capture run and is not a final-frame review mark.

## What the original evidence supports

- Night's shader replacement is original behavior. `ControlPiezaFuente.doConfigureColors`, VA `0x1091F3C`, tests `(parte & ~3) == 36`, assigns `shader1` to material1/2/3/4/5/7, then executes the night color recipe. Thus exported materials originally naming `Transparent/Diffuse ZWrite` legitimately use `Mobile/Diffuse Color` at runtime in gm2001. Reverting those runtime materials to transparency would contradict the native method.
- The original GLES `Mobile/Diffuse Color` fragment reads texture RGB, multiplies Lambert diffuse lighting and `_Color.rgb`, and writes alpha exactly `1.0`. Its LIGHTPROBE_SH variant additionally applies SH ambient. Current restored shader uses the same Lambert texture/tint model. The plane's hard outer edge cannot be diagnosed as a missing alpha blend merely from its appearance.
- Raw bundle decoding exactly matches the exported PNG pixels for `fontain20`, `fontain21`, `fontain1` and `fontain5`; none is accidentally vertically flipped. All four original decoded alpha channels are fully opaque (`255..255`). Original fontain20/21 use bilinear filtering, repeat wrapping, anisotropy1, mip bias0 and sRGB textures; exported import metadata preserves those settings. Project color space is Gamma. Detailed raw pathIDs, dimensions, mip counts and pixel SHA256 are in `raw-water-material-texture-audit.json`.
- Raw material `_MainTex` scale/offset and original color fields were checked for MatGrRo_1, MatGrRo_C, MatGrRo_chorro6, MatGrZ_1, MatGr16 and MatGr16_2. Ro_1/C start with offset0; Ro_chorro6 starts Y1 and its repeat texture makes an integer-cycle offset visually equivalent. Runtime native palette instances account for purple/teal colors; exported green/blue material defaults alone do not describe final Night colors.
- Current Mig/Mig2 restoration agrees with the original GLES strict `textureAlpha * _Color.a > _Cutoff` alpha selection and alpha blend. Mig2 emission is `textureAlpha * (textureRGB * _Color.rgb) * _EmissionColor.rgb`, not the unused `_Emission` scalar. These shaders must not be globally substituted for the opaque Night shader.
- Original particle color gradients are restored separately from water color tags. The current helper uses the extracted original gradient table, source color3/4 globals and original night predicate. A bright particle/spray overlap is not proof that an underlying purple water material lost its tint.

## Actual UV difference found and staged

Original `scrollTest1.Start` (`0x1060C98`) sets initial Y to0 for tipoOffset1 and .5 for tipoOffset2. Original `Update` (`0x1060CC0`) sets material offset to `initial + speed * Time.time`, replacing the prior material offset.

The frozen restored Motion instead added `material.mainTextureOffset` to initial. Static inspection found1718 scroll components; all serialize tipoOffset0, so the omitted Start branch does not affect those shipped prefab fields. However565 rows reference nonzero material offsets, including156 MatGr16 rows at Y.51 and107 MatGr16_2 rows at X.5. Those fractional offsets change the blue-water texture phase. Other nonzero cases are predominantly an integer Y1 cycle. Counts include duplicated authored group prefabs, not unique runtime water surfaces or level counts.

`Validation/NativeWaterUV1` removes only that extra material offset and implements the original Start rules. The runtime diff changes no graph, geometry, physics, collider, actor or scheduler logic. Its fixture samples actual production MaterialPropertyBlock UVs in automatic camera post-render events, at Time.timeScale1 and4, against original formulas; it also tests the controlled Start1/2 branches on original components. Roslyn compile passed; actual Unity result is pending root execution at time of this note.

## Boundaries

The UV difference above does not explain gm2001's purple hard-edged rectangles: affected Ro_1/C offsets are zero, the remaining Ro_chorro6 Y1 offset is a repeat cycle, and original opaque alpha is established. The front-right q* meshes were independently attributed to original retained FT3 vertical-input A surfaces; original vertical5/6 branches explicitly skip the side-A two-second removal. Their presence alone does not prove visual equivalence, but removing or fading them would require additional original evidence.

This audit does not establish Android GLES versus Unity2022 desktop pixel equivalence. The shader is reconstructed source compiled for the desktop backend; original fallback/LOD and platform variants are not bit-identical, and PNG reimport may use a different GPU compression format from original ETC2. No synchronized original-device screenshot at matching board/camera/time was available to compare the remaining flat/bright patch boundaries. Final full-capture visual review must retain this limit, with suspect tiles drilled into full-resolution captures rather than accepted solely from mesh existence or numeric pass counts.
