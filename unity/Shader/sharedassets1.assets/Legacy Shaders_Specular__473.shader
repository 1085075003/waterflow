//////////////////////////////////////////
//
// NOTE: This is *not* a valid shader file
//
///////////////////////////////////////////
Shader "Legacy Shaders/Specular" {
Properties {

_Color ("Main Color", Color) = (1,1,1,1)

_SpecColor ("Specular Color", Color) = (0.5,0.5,0.5,1)

[PowerSlider(5.0)] _Shininess ("Shininess", Range(0.01, 1)) = 0.078125

_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" { }

}
SubShader {
 LOD 300
 Tags { "RenderType" = "Opaque" }
 Pass {
 Name "FORWARD"
  LOD 300
  Tags { "LIGHTMODE" = "FORWARDBASE" "RenderType" = "Opaque" "SHADOWSUPPORT" = "true" }
  GpuProgramID 21132
}
 Pass {
 Name "FORWARD"
  LOD 300
  Tags { "LIGHTMODE" = "FORWARDADD" "RenderType" = "Opaque" }
 ZWrite Off
  GpuProgramID 120560
}
 Pass {
 Name "DEFERRED"
  LOD 300
  Tags { "LIGHTMODE" = "DEFERRED" "RenderType" = "Opaque" }
  GpuProgramID 145795
}
}
Fall back "Legacy Shaders/VertexLit"
}