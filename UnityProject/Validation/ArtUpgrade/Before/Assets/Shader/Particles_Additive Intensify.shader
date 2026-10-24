Shader "Particles/Additive Intensify" {
	Properties {
		_TintColor ("Tint Color", Vector) = (0.5,0.5,0.5,0.5)
		_MainTex ("Particle Texture", 2D) = "white" {}
		_InvFade ("Soft Particles Factor", Range(0.01, 3)) = 1
		_Glow ("Intensity", Range(0, 5)) = 1
	}
	    // Reconstructed additive particle shader with vertex color, tint and intensity.
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Blend SrcAlpha One
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
            float _Glow;
            float _InvFade;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            struct appdata { float4 vertex:POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 projected:TEXCOORD1; UNITY_FOG_COORDS(2) };
            v2f vert(appdata v) {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.color = v.color * _TintColor;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.projected = ComputeScreenPos(o.pos); COMPUTE_EYEDEPTH(o.projected.z);
                UNITY_TRANSFER_FOG(o,o.pos); return o;
            }
            fixed4 frag(v2f i):SV_Target {
                #ifdef SOFTPARTICLES_ON
                float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projected)));
                i.color.a *= saturate(_InvFade * (depth - i.projected.z));
                #endif
                fixed4 c = tex2D(_MainTex,i.uv) * i.color;
                c.rgb *= 2 * _Glow; UNITY_APPLY_FOG_COLOR(i.fogCoord,c,fixed4(0,0,0,0)); return c;
            }
            ENDCG
        }
    }
    Fallback Off
}
