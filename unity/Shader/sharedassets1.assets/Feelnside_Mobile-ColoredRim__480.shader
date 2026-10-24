//////////////////////////////////////////
//
// NOTE: This is *not* a valid shader file
//
///////////////////////////////////////////
Shader "Feelnside/Mobile-ColoredRim" {
Properties {

_MainTex ("Texture", 2D) = "white" { }

_BumpMap ("Bumpmap", 2D) = "bump" { }

_RimValue ("Rim Value", Range(0, 1)) = 0.5

_SpecColor ("Specular Color", Color) = (0.5,0.5,0.5,1)

_Shininess ("Shininess", Range(0.01, 1)) = 0.078125

}
SubShader {
 Tags { "RenderType" = "Opaque" }
 Pass {
 Name "FORWARD"
  Tags { "LIGHTMODE" = "FORWARDBASE" "RenderType" = "Opaque" "SHADOWSUPPORT" = "true" }
  GpuProgramID 61588
}
 Pass {
 Name "FORWARD"
  Tags { "LIGHTMODE" = "FORWARDADD" "RenderType" = "Opaque" }
 ZWrite Off
  GpuProgramID 88207
}
}
Fall back "Mobile/Diffuse"
}