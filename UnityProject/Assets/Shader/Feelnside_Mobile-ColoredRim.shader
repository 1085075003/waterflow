Shader "Feelnside/Mobile-ColoredRim" {
    Properties {
        _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _BumpMap ("Surface Normal", 2D) = "bump" {}
        _DetailTex ("Fine Water Normal", 2D) = "bump" {}
        _ArtTint ("Art Tint", Color) = (1,1,1,1)
        _ArtHighlight ("Soft Highlight", Color) = (0.86,0.94,1,1)
        _ArtSaturation ("Texture Saturation", Range(0,1.5)) = 1
        _ArtContrast ("Texture Contrast", Range(0.5,1.2)) = 1
        _ArtNormalStrength ("Stone Normal Strength", Range(0,1)) = 0.28
        _ArtWrap ("Soft Light Wrap", Range(0,0.4)) = 0.1
        _ArtSpecular ("Highlight Strength", Range(0,1)) = 0.04
        _ArtWaterSurface ("Water Surface", Range(0,1)) = 0
        _ArtOpacity ("Water Opacity", Range(0,1)) = 0.93
        _ArtWaveStrength ("Fine Ripple Strength", Range(0,1)) = 0.28
        _RimValue ("Rim Value", Range(0,1)) = 0.5
        _SpecColor ("Specular Color", Color) = (0.5,0.5,0.5,1)
        _Shininess ("Shininess", Range(0.01,1)) = 0.078125
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 250
        CGPROGRAM
        #pragma surface surf Garden fullforwardshadows
        #pragma target 3.0
        #include "FlowWaterGarden.cginc"
        half _RimValue, _Shininess;
        struct Input { float2 uv_MainTex; float2 uv_BumpMap; float3 viewDir; };
        void surf(Input IN, inout SurfaceOutput o) {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            GardenStone(IN.uv_BumpMap, c, o);
            half rim = pow(1 - saturate(dot(normalize(IN.viewDir), o.Normal)), 4);
            o.Emission = _ArtHighlight.rgb * rim * _RimValue * .08;
            o.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Mobile/Diffuse"
}
