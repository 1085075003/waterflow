//////////////////////////////////////////
//
// NOTE: This is *not* a valid shader file
//
///////////////////////////////////////////
Shader "Particles/Additive Intensify" {
Properties {

_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)

_MainTex ("Particle Texture", 2D) = "white" { }

_InvFade ("Soft Particles Factor", Range(0.01, 3)) = 1.0

_Glow ("Intensity", Range(0, 5)) = 1.0

}
SubShader {
 Tags { "IGNOREPROJECTOR" = "true" "PreviewType" = "Plane" "QUEUE" = "Transparent" "RenderType" = "Transparent" }
 Pass {
  Tags { "IGNOREPROJECTOR" = "true" "PreviewType" = "Plane" "QUEUE" = "Transparent" "RenderType" = "Transparent" }
 ZWrite Off
 Cull Off
  GpuProgramID 18435
}
}
}