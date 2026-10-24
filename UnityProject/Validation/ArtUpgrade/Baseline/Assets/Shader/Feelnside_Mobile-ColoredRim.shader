Shader "Feelnside/Mobile-ColoredRim" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_BumpMap ("Bumpmap", 2D) = "bump" {}
		_RimValue ("Rim Value", Range(0, 1)) = 0.5
		_SpecColor ("Specular Color", Vector) = (0.5,0.5,0.5,1)
		_Shininess ("Shininess", Range(0.01, 1)) = 0.078125
	}
    // Restored from original GLES: white squared (_RimValue - N dot V)
    // emission and a fixed 0.75 specular strength.
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 250
        CGPROGRAM
        #pragma surface surf RecoveredRim
        #pragma target 3.0
        sampler2D _MainTex, _BumpMap;
        half _RimValue, _Shininess;
        half4 LightingRecoveredRim(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten) {
            half diffuse = max(0, dot(s.Normal, lightDir));
            half specular = pow(max(0, dot(s.Normal, normalize(lightDir + viewDir))), s.Specular * 128) * .75;
            return half4((s.Albedo * diffuse + specular) * _LightColor0.rgb * atten, 1);
        }
        struct Input { float2 uv_MainTex; float2 uv_BumpMap; float3 viewDir; };
        void surf(Input IN, inout SurfaceOutput o) {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            o.Albedo = c.rgb;
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
            half rim = _RimValue - saturate(dot(normalize(IN.viewDir), o.Normal));
            o.Emission = rim * rim;
            o.Specular = _Shininess;
            o.Gloss = .75;
            o.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Mobile/Diffuse"
}
