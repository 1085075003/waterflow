Shader "Mobile/Diffuse Color1" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_BumpMap ("Bumpmap", 2D) = "bump" {}
		_Color ("Color", Vector) = (0.5,0.5,0.5,1)
	}
	    // Reconstructed for Unity 2022.3: material tint, lighting and texture sampling.
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Lambert fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color;
        struct Input { float2 uv_MainTex; };
        void surf(Input IN, inout SurfaceOutput o) {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
        }
        ENDCG
    }
    Fallback "Mobile/Diffuse"
}