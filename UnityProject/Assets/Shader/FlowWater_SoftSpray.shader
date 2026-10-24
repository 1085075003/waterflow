Shader "FlowWater/Art/Soft Spray" {
    Properties {
        _TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
        _MainTex ("Droplet / Foam Sheet", 2D) = "white" {}
        _InvFade ("Soft Particles Factor", Range(0.01,3)) = 1
        _ArtSoftness ("Soft Droplet Edge", Range(0,1)) = .12
        _ArtOpacity ("Spray Opacity", Range(0,1)) = .58
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask RGB
        Cull Off Lighting Off ZWrite Off
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_particles
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _TintColor;
            half _InvFade, _ArtSoftness, _ArtOpacity;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            struct appdata { float4 vertex:POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 projected:TEXCOORD1; UNITY_FOG_COORDS(2) };
            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                // Matches the legacy Alpha Blended particle material's tint gain.
                o.color = v.color * _TintColor * 2;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.projected = ComputeScreenPos(o.pos);
                COMPUTE_EYEDEPTH(o.projected.z);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }
            fixed4 frag(v2f i):SV_Target {
                #ifdef SOFTPARTICLES_ON
                float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projected)));
                i.color.a *= saturate(_InvFade * (depth - i.projected.z));
                #endif
                fixed4 sheet = tex2D(_MainTex, i.uv);
                fixed4 c = sheet * i.color;
                c.a *= _ArtOpacity * smoothstep(0, max(.001h, _ArtSoftness), sheet.a);
                // Keep center droplets legible while feathering the sheet edge.
                c.rgb = lerp(c.rgb, min(1, c.rgb * 1.12h), sheet.a * .3h);
                UNITY_APPLY_FOG(i.fogCoord, c);
                return c;
            }
            ENDCG
        }
    }
    Fallback Off
}
