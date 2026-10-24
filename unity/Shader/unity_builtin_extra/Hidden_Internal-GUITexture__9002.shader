//////////////////////////////////////////
//
// NOTE: This is *not* a valid shader file
//
///////////////////////////////////////////
Shader "Hidden/Internal-GUITexture" {
Properties {

_MainTex ("Texture", any) = "" { }

}
SubShader {
 Tags { "RenderType" = "Overlay" }
 Pass {
  Tags { "RenderType" = "Overlay" }
 ZTest Always
 ZWrite Off
 Cull Off
  GpuProgramID 25450
}
}
SubShader {
 Tags { "RenderType" = "Overlay" }
 Pass {
  Tags { "RenderType" = "Overlay" }
 ZTest Always
 ZWrite Off
 Cull Off
  GpuProgramID 67210
}
}
}