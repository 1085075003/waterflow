Shader "Mobile/Diffuse Color Mig2" {
	Properties {
		_Color ("Main Color", Vector) = (1,1,1,1)
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
		_Cutoff ("Alpha cutoff", Range(0, 1)) = 0.5
		_Emission ("Emission", Float) = 0
		[HDR] _EmissionColor ("Color", Vector) = (0,0,0,1)
		_Glossiness ("Smoothness", Range(0, 1)) = 0.5
	}
    // Original GLES: transparent alpha threshold and texture-alpha-modulated
    // emission. The exported _Emission scalar is not read by the native shader.
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
        half _Emission; fixed4 _EmissionColor;
        float _Cutoff;
        struct Input { float2 uv_MainTex; };
        void surf(Input IN, inout SurfaceOutput o) {
            fixed4 textureColor = tex2D(_MainTex, IN.uv_MainTex);
            fixed4 c = textureColor * _Color;
            o.Albedo = c.rgb;
            o.Emission = textureColor.a * c.rgb * _EmissionColor.rgb;
            o.Alpha = c.a > _Cutoff ? c.a : 0;
        }
        ENDCG
    }
    Fallback "Mobile/Diffuse"
}
