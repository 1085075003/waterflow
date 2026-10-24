//////////////////////////////////////////
//
// NOTE: This is *not* a valid shader file
//
///////////////////////////////////////////
Shader "Standard" {
Properties {

_Color ("Color", Color) = (1,1,1,1)

_MainTex ("Albedo", 2D) = "white" { }

_Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5

_Glossiness ("Smoothness", Range(0, 1)) = 0.5

_GlossMapScale ("Smoothness Scale", Range(0, 1)) = 1.0

[Enum(Metallic Alpha,0,Albedo Alpha,1)] _SmoothnessTextureChannel ("Smoothness texture channel", Float) = 0.0

_Metallic ("Metallic", Range(0, 1)) = 0.0

_MetallicGlossMap ("Metallic", 2D) = "white" { }

[ToggleOff] _SpecularHighlights ("Specular Highlights", Float) = 1.0

[ToggleOff] _GlossyReflections ("Glossy Reflections", Float) = 1.0

_BumpScale ("Scale", Float) = 1.0

_BumpMap ("Normal Map", 2D) = "bump" { }

_Parallax ("Height Scale", Range(0.005, 0.08)) = 0.019999999552965164

_ParallaxMap ("Height Map", 2D) = "black" { }

_OcclusionStrength ("Strength", Range(0, 1)) = 1.0

_OcclusionMap ("Occlusion", 2D) = "white" { }

_EmissionColor ("Color", Color) = (0,0,0,1)

_EmissionMap ("Emission", 2D) = "white" { }

_DetailMask ("Detail Mask", 2D) = "white" { }

_DetailAlbedoMap ("Detail Albedo x2", 2D) = "grey" { }

_DetailNormalMapScale ("Scale", Float) = 1.0

_DetailNormalMap ("Normal Map", 2D) = "bump" { }

[Enum(UV0,0,UV1,1)] _UVSec ("UV Set for secondary textures", Float) = 0.0

_Mode ("__mode", Float) = 0.0

_SrcBlend ("__src", Float) = 1.0

_DstBlend ("__dst", Float) = 0.0

_ZWrite ("__zw", Float) = 1.0

}
SubShader {
 LOD 300
 Tags { "PerformanceChecks" = "False" "RenderType" = "Opaque" }
 Pass {
 Name "FORWARD"
  LOD 300
  Tags { "LIGHTMODE" = "FORWARDBASE" "PerformanceChecks" = "False" "RenderType" = "Opaque" "SHADOWSUPPORT" = "true" }
 ZWrite Off
  GpuProgramID 3935
}
 Pass {
 Name "FORWARD_DELTA"
  LOD 300
  Tags { "LIGHTMODE" = "FORWARDADD" "PerformanceChecks" = "False" "RenderType" = "Opaque" "SHADOWSUPPORT" = "true" }
 ZWrite Off
  GpuProgramID 83234
}
 Pass {
 Name "ShadowCaster"
  LOD 300
  Tags { "LIGHTMODE" = "SHADOWCASTER" "PerformanceChecks" = "False" "RenderType" = "Opaque" "SHADOWSUPPORT" = "true" }
  GpuProgramID 143605
}
 Pass {
 Name "DEFERRED"
  LOD 300
  Tags { "LIGHTMODE" = "DEFERRED" "PerformanceChecks" = "False" "RenderType" = "Opaque" }
  GpuProgramID 257049
}
}
SubShader {
 LOD 150
 Tags { "PerformanceChecks" = "False" "RenderType" = "Opaque" }
 Pass {
 Name "FORWARD"
  LOD 150
  Tags { "LIGHTMODE" = "FORWARDBASE" "PerformanceChecks" = "False" "RenderType" = "Opaque" "SHADOWSUPPORT" = "true" }
 ZWrite Off
  GpuProgramID 343811
}
 Pass {
 Name "FORWARD_DELTA"
  LOD 150
  Tags { "LIGHTMODE" = "FORWARDADD" "PerformanceChecks" = "False" "RenderType" = "Opaque" "SHADOWSUPPORT" = "true" }
 ZWrite Off
  GpuProgramID 428635
}
 Pass {
 Name "ShadowCaster"
  LOD 150
  Tags { "LIGHTMODE" = "SHADOWCASTER" "PerformanceChecks" = "False" "RenderType" = "Opaque" "SHADOWSUPPORT" = "true" }
  GpuProgramID 507809
}
}
Fall back "VertexLit"
CustomEditor "StandardShaderGUI"
}