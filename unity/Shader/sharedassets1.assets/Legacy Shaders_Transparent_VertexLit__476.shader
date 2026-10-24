//////////////////////////////////////////
//
// NOTE: This is *not* a valid shader file
//
///////////////////////////////////////////
Shader "Legacy Shaders/Transparent/VertexLit" {
Properties {

_Color ("Main Color", Color) = (1,1,1,1)

_SpecColor ("Spec Color", Color) = (1,1,1,0)

_Emission ("Emissive Color", Color) = (0,0,0,0)

[PowerSlider(5.0)] _Shininess ("Shininess", Range(0.1, 1)) = 0.699999988079071

_MainTex ("Base (RGB) Trans (A)", 2D) = "white" { }

}
SubShader {
 LOD 100
 Tags { "IGNOREPROJECTOR" = "true" "QUEUE" = "Transparent" "RenderType" = "Transparent" }
 Pass {
  LOD 100
  Tags { "IGNOREPROJECTOR" = "true" "LIGHTMODE" = "Vertex" "QUEUE" = "Transparent" "RenderType" = "Transparent" }
 ZWrite Off
  GpuProgramID 78755
}
 Pass {
  LOD 100
  Tags { "IGNOREPROJECTOR" = "true" "LIGHTMODE" = "VertexLM" "QUEUE" = "Transparent" "RenderType" = "Transparent" }
 ZWrite Off
  GpuProgramID 24516
}
}
}