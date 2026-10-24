//////////////////////////////////////////
//
// NOTE: This is *not* a valid shader file
//
///////////////////////////////////////////
Shader "Mobile/Bumped Diffuse" {
Properties {

_MainTex ("Base (RGB)", 2D) = "white" { }

_BumpMap ("Normalmap", 2D) = "bump" { }

}
SubShader {
 LOD 250
 Tags { "RenderType" = "Opaque" }
 Pass {
 Name "FORWARD"
  LOD 250
  Tags { "LIGHTMODE" = "FORWARDBASE" "RenderType" = "Opaque" "SHADOWSUPPORT" = "true" }
  GpuProgramID 16192
}
 Pass {
 Name "DEFERRED"
  LOD 250
  Tags { "LIGHTMODE" = "DEFERRED" "RenderType" = "Opaque" }
  GpuProgramID 130265
}
}
Fall back "Mobile/Diffuse"
}