//////////////////////////////////////////
//
// NOTE: This is *not* a valid shader file
//
///////////////////////////////////////////
Shader "Mobile/Diffuse Color1" {
Properties {

_MainTex ("Texture", 2D) = "white" { }

_BumpMap ("Bumpmap", 2D) = "bump" { }

_Color ("Color", Color) = (0.5,0.5,0.5,1)

}
SubShader {
 Tags { "RenderType" = "Opaque" }
 Pass {
 Name "FORWARD"
  Tags { "LIGHTMODE" = "FORWARDBASE" "RenderType" = "Opaque" "SHADOWSUPPORT" = "true" }
  GpuProgramID 19468
}
 Pass {
 Name "DEFERRED"
  Tags { "LIGHTMODE" = "DEFERRED" "RenderType" = "Opaque" }
  GpuProgramID 84570
}
}
Fall back "Diffuse"
}