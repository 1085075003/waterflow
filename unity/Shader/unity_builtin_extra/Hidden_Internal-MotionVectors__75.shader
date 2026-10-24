//////////////////////////////////////////
//
// NOTE: This is *not* a valid shader file
//
///////////////////////////////////////////
Shader "Hidden/Internal-MotionVectors" {
Properties {

}
SubShader {
 Pass {
  Tags { "LIGHTMODE" = "MOTIONVECTORS" }
 ZWrite Off
  GpuProgramID 51753
}
 Pass {
 ZTest Always
 ZWrite Off
 Cull Off
  GpuProgramID 81431
}
 Pass {
 ZTest Always
 Cull Off
  GpuProgramID 177029
}
}
}