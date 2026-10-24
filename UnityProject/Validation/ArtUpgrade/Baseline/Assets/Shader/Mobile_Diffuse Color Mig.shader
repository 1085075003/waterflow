Shader "Mobile/Diffuse Color Mig" {
	Properties {
		_Color ("Main Color", Vector) = (1,1,1,1)
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
		_Cutoff ("Alpha cutoff", Range(0, 1)) = 0.5
	}
    // Original GLES program: SrcAlpha/OneMinusSrcAlpha, RGB mask,
    // ZWrite Off; alpha <= _Cutoff produces zero output alpha.
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        ZWrite Off
        ColorMask RGB
        LOD 200
        CGPROGRAM
        #pragma surface surf Lambert alpha:fade keepalpha noforwardadd
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color;
        float _Cutoff;
        struct Input { float2 uv_MainTex; };
        void surf(Input IN, inout SurfaceOutput o) {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a > _Cutoff ? c.a : 0;
        }
        ENDCG
    }
    Fallback "Mobile/Diffuse"
}
