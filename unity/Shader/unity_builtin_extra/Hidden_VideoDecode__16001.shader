//////////////////////////////////////////
//
// NOTE: This is *not* a valid shader file
//
///////////////////////////////////////////
Shader "Hidden/VideoDecode" {
Properties {

_MainTex ("_MainTex (A)", 2D) = "black" { }

_SecondTex ("_SecondTex (A)", 2D) = "black" { }

_ThirdTex ("_ThirdTex (A)", 2D) = "black" { }

}
SubShader {
 Pass {
 Name "YCbCr_To_RGB1"
 ZTest Always
 ZWrite Off
 Cull Off
  GpuProgramID 47600
}
 Pass {
 Name "YCbCrA_To_RGBAFull"
 ZTest Always
 ZWrite Off
 Cull Off
  GpuProgramID 90961
}
 Pass {
 Name "YCbCrA_To_RGBA"
 ZTest Always
 ZWrite Off
 Cull Off
  GpuProgramID 192437
}
 Pass {
 Name "Flip_RGBA_To_RGBA"
 ZTest Always
 ZWrite Off
 Cull Off
  GpuProgramID 244574
}
 Pass {
 Name "Flip_RGBASplit_To_RGBA"
 ZTest Always
 ZWrite Off
 Cull Off
  GpuProgramID 267143
}
 Pass {
 Name "Flip_NV12_To_RGB1"
 ZTest Always
 ZWrite Off
 Cull Off
  GpuProgramID 366377
}
 Pass {
 Name "Flip_NV12_To_RGBA"
 ZTest Always
 ZWrite Off
 Cull Off
  GpuProgramID 456580
}
 Pass {
 Name "Flip_P010_To_RGB1"
 ZTest Always
 ZWrite Off
 Cull Off
  GpuProgramID 498858
}
}
}