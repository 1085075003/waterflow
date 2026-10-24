//////////////////////////////////////////
//
// NOTE: This is *not* a valid shader file
//
///////////////////////////////////////////
Shader "Mobile/VertexLit" {
Properties {

_MainTex ("Base (RGB)", 2D) = "white" { }

}
SubShader {
 LOD 80
 Tags { "RenderType" = "Opaque" }
 Pass {
  LOD 80
  Tags { "LIGHTMODE" = "Vertex" "RenderType" = "Opaque" }
  GpuProgramID 156332
}
 Pass {
  LOD 80
  Tags { "LIGHTMODE" = "VertexLM" "RenderType" = "Opaque" }
  GpuProgramID 36177
}
 Pass {
 Name "ShadowCaster"
  LOD 80
  Tags { "LIGHTMODE" = "SHADOWCASTER" "RenderType" = "Opaque" "SHADOWSUPPORT" = "true" }
 Cull Off
  GpuProgramID 127937
}
}
}