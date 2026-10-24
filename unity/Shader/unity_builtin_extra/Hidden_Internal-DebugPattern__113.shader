//////////////////////////////////////////
//
// NOTE: This is *not* a valid shader file
//
///////////////////////////////////////////
Shader "Hidden/Internal-DebugPattern" {
Properties {

_MainTex ("Texture", 2D) = "white" { }

}
SubShader {
 Pass {
 Name "Target Color and DepthStencil"
 ZTest Always
  GpuProgramID 24322
}
 Pass {
 Name "Target only Color"
 ZTest Always
 ZWrite Off
  GpuProgramID 89907
}
 Pass {
 Name "Target only DepthStencil"
 ZTest Always
  GpuProgramID 195071
}
}
}