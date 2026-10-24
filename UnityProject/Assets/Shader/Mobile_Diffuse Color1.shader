Shader "Mobile/Diffuse Color1" {
    Properties {
        _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
        _Color ("Color", Color) = (0.5,0.5,0.5,1)
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
    }
    // Warm miniature-garden material, preserving all recovered properties/UVs.
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 250
        CGPROGRAM
        #pragma surface surf Garden fullforwardshadows
        #pragma target 3.0
        #include "FlowWaterGarden.cginc"
        struct Input { float2 uv_MainTex; float2 uv_BumpMap; float2 uv_DetailTex; float3 viewDir; };
        void surf(Input IN, inout SurfaceOutput o) {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            if (_ArtWaterSurface > .5) GardenWater(IN.uv_MainTex, IN.uv_DetailTex, IN.viewDir, c, o);
            else GardenStone(IN.uv_BumpMap, c, o);
        }
        ENDCG
    }
    Fallback "Mobile/Diffuse"
}
