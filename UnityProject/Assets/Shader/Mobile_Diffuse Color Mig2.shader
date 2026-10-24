Shader "Mobile/Diffuse Color Mig2" {
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
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
        _Emission ("Emission", Float) = 0
        [HDR] _EmissionColor ("Color", Color) = (0,0,0,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        ZWrite Off
        ColorMask RGB
        LOD 250
        CGPROGRAM
        #pragma surface surf Garden alpha:fade keepalpha noforwardadd
        #pragma target 3.0
        #include "FlowWaterGarden.cginc"
        half _Cutoff;
        half _Emission; fixed4 _EmissionColor;
        struct Input { float2 uv_MainTex; float2 uv_BumpMap; float2 uv_DetailTex; float3 viewDir; };
        void surf(Input IN, inout SurfaceOutput o) {
            fixed4 textureColor = tex2D(_MainTex, IN.uv_MainTex);
            fixed4 c = textureColor * _Color;
            if (_ArtWaterSurface > .5) GardenWater(IN.uv_MainTex, IN.uv_DetailTex, IN.viewDir, c, o);
            else GardenStone(IN.uv_BumpMap, c, o);
            // Preserve native night-palette emission after the visual surface pass.
            o.Emission += textureColor.a * c.rgb * _EmissionColor.rgb;
            o.Alpha = c.a > _Cutoff ? o.Alpha : 0;
        }
        ENDCG
    }
    Fallback "Mobile/Diffuse"
}
