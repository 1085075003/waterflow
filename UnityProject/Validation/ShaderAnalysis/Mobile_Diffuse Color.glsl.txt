//////////////////////////////////////////
//
// NOTE: This is *not* a valid shader file
//
///////////////////////////////////////////
Shader "Mobile/Diffuse Color" {
Properties {

_MainTex ("Base (RGB)", 2D) = "white" { }

_Color ("Color", Color) = (0.5,0.5,0.5,1)

}
SubShader {
 LOD 150
 Tags { "RenderType" = "Opaque" }
 Pass {
 Name "FORWARD"
  LOD 150
  Tags { "LIGHTMODE" = "FORWARDBASE" "RenderType" = "Opaque" "SHADOWSUPPORT" = "true" }
  GpuProgramID 24681
}
 Pass {
 Name "DEFERRED"
  LOD 150
  Tags { "LIGHTMODE" = "DEFERRED" "RenderType" = "Opaque" }
  GpuProgramID 89338
}
}
Fall back "Mobile/VertexLit"
}