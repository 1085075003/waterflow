//////////////////////////////////////////
//
// NOTE: This is *not* a valid shader file
//
///////////////////////////////////////////
Shader "Legacy Shaders/Bumped Specular" {
Properties {

_Color ("Main Color", Color) = (1,1,1,1)

_SpecColor ("Specular Color", Color) = (0.5,0.5,0.5,1)

[PowerSlider(5.0)] _Shininess ("Shininess", Range(0.03, 1)) = 0.078125

_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" { }

_BumpMap ("Normalmap", 2D) = "bump" { }

}
SubShader {
 LOD 400
 Tags { "RenderType" = "Opaque" }
 Pass {
 Name "FORWARD"
  LOD 400
  Tags { "LIGHTMODE" = "FORWARDBASE" "RenderType" = "Opaque" "SHADOWSUPPORT" = "true" }
  GpuProgramID 35117
}
 Pass {
 Name "FORWARD"
  LOD 400
  Tags { "LIGHTMODE" = "FORWARDADD" "RenderType" = "Opaque" }
 ZWrite Off
  GpuProgramID 109342
}
 Pass {
 Name "DEFERRED"
  LOD 400
  Tags { "LIGHTMODE" = "DEFERRED" "RenderType" = "Opaque" }
  GpuProgramID 152511
}
}
SubShader {
 LOD 400
 Tags { "RenderType" = "Opaque" }
 Pass {
 Name "FORWARD"
  LOD 400
  Tags { "LIGHTMODE" = "FORWARDBASE" "RenderType" = "Opaque" "SHADOWSUPPORT" = "true" }
  GpuProgramID 308729
}
 Pass {
 Name "FORWARD"
  LOD 400
  Tags { "LIGHTMODE" = "FORWARDADD" "RenderType" = "Opaque" }
 ZWrite Off
  GpuProgramID 387992
}
 Pass {
 Name "DEFERRED"
  LOD 400
  Tags { "LIGHTMODE" = "DEFERRED" "RenderType" = "Opaque" }
  GpuProgramID 435863
}
}
Fall back "Legacy Shaders/Specular"
}