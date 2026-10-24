//////////////////////////////////////////
//
// NOTE: This is *not* a valid shader file
//
///////////////////////////////////////////
Shader "Legacy Shaders/Diffuse" {
Properties {

_Color ("Main Color", Color) = (1,1,1,1)

_MainTex ("Base (RGB)", 2D) = "white" { }

}
SubShader {
 LOD 200
 Tags { "RenderType" = "Opaque" }
 Pass {
 Name "FORWARD"
  LOD 200
  Tags { "LIGHTMODE" = "FORWARDBASE" "RenderType" = "Opaque" "SHADOWSUPPORT" = "true" }
  GpuProgramID 26796
}
 Pass {
 Name "FORWARD"
  LOD 200
  Tags { "LIGHTMODE" = "FORWARDADD" "RenderType" = "Opaque" }
 ZWrite Off
  GpuProgramID 128759
}
 Pass {
 Name "DEFERRED"
  LOD 200
  Tags { "LIGHTMODE" = "DEFERRED" "RenderType" = "Opaque" }
  GpuProgramID 179689
}
}
Fall back "Legacy Shaders/VertexLit"
}