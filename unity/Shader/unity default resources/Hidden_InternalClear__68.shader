//////////////////////////////////////////
//
// NOTE: This is *not* a valid shader file
//
///////////////////////////////////////////
Shader "Hidden/InternalClear" {
Properties {

}
SubShader {
 Pass {
 ZTest Always
 ZWrite Off
 Cull Off
  GpuProgramID 51877
}
 Pass {
 ZTest Always
 ZWrite Off
 Cull Off
  GpuProgramID 72773
}
 Pass {
 ZTest Always
 Cull Off
  GpuProgramID 147634
}
 Pass {
 ZTest Always
 Cull Off
  GpuProgramID 234628
}
 Pass {
 ZTest Always
 ZWrite Off
 Cull Off
  GpuProgramID 308930
}
 Pass {
 ZTest Always
 ZWrite Off
 Cull Off
  GpuProgramID 361614
}
 Pass {
 ZTest Always
 Cull Off
  GpuProgramID 438784
}
 Pass {
 ZTest Always
 Cull Off
  GpuProgramID 517664
}
}
}