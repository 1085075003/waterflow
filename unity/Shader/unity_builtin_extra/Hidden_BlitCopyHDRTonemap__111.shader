//////////////////////////////////////////
//
// NOTE: This is *not* a valid shader file
//
///////////////////////////////////////////
Shader "Hidden/BlitCopyHDRTonemap" {
Properties {

_MainTex ("Texture", any) = "" { }

_NitsForPaperWhite ("NitsForPaperWhite", Float) = 160.0

_ColorGamut ("ColorGamut", Float) = 0.0

_ForceGammaToLinear ("ForceGammaToLinear", Float) = 0.0

_MaxDisplayNits ("MaxDisplayNits", Float) = 160.0

}
SubShader {
 Pass {
 ZTest Always
 ZWrite Off
 Cull Off
  GpuProgramID 17300
}
}
}