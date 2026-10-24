//////////////////////////////////////////
//
// NOTE: This is *not* a valid shader file
//
///////////////////////////////////////////
Shader "Mobile/Diffuse" {
Properties {

_MainTex ("Base (RGB)", 2D) = "white" { }

}
SubShader {
 LOD 150
 Tags { "RenderType" = "Opaque" }
 Pass {
 Name "FORWARD"
  LOD 150
  Tags { "LIGHTMODE" = "FORWARDBASE" "RenderType" = "Opaque" "SHADOWSUPPORT" = "true" }
  GpuProgramID 45630
}
 Pass {
 Name "DEFERRED"
  LOD 150
  Tags { "LIGHTMODE" = "DEFERRED" "RenderType" = "Opaque" }
  GpuProgramID 87558
}
}
Fall back "Mobile/VertexLit"
}