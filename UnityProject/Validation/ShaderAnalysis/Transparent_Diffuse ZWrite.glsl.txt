//////////////////////////////////////////
//
// NOTE: This is *not* a valid shader file
//
///////////////////////////////////////////
Shader "Transparent/Diffuse ZWrite" {
Properties {

_Color ("Main Color", Color) = (1,1,1,1)

_MainTex ("Base (RGB) Trans (A)", 2D) = "white" { }

}
SubShader {
 LOD 200
 Tags { "IGNOREPROJECTOR" = "true" "QUEUE" = "Transparent" "RenderType" = "Transparent" }
 Pass {
  LOD 200
  Tags { "IGNOREPROJECTOR" = "true" "QUEUE" = "Transparent" "RenderType" = "Transparent" }
  GpuProgramID 11839
}
 UsePass "Transparent/Diffuse/FORWARD"
}
Fall back "Transparent/VertexLit"
}