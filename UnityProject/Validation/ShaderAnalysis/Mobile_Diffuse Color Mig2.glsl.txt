//////////////////////////////////////////
//
// NOTE: This is *not* a valid shader file
//
///////////////////////////////////////////
Shader "Mobile/Diffuse Color Mig2" {
Properties {

_Color ("Main Color", Color) = (1,1,1,1)

_MainTex ("Base (RGB) Trans (A)", 2D) = "white" { }

_Cutoff ("Alpha cutoff", Range(0, 1)) = 0.5

_Emission ("Emission", Float) = 0.0

_EmissionColor ("Color", Color) = (0,0,0,1)

_Glossiness ("Smoothness", Range(0, 1)) = 0.5

}
SubShader {
 LOD 200
 Tags { "IGNOREPROJECTOR" = "true" "QUEUE" = "Transparent" "RenderType" = "Transparent" }
 Pass {
 Name "FORWARD"
  LOD 200
  Tags { "IGNOREPROJECTOR" = "true" "LIGHTMODE" = "FORWARDBASE" "QUEUE" = "Transparent" "RenderType" = "Transparent" }
 ZWrite Off
  GpuProgramID 35827
}
}
Fall back "Diffuse"
}