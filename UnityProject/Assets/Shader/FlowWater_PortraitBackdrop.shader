Shader "Hidden/FlowWater/PortraitBackdrop"
{
    Properties { _MainTex ("Background", 2D) = "white" {} _Tint ("Tint", Color) = (1,1,1,1) _UVRect ("Sprite UV", Vector) = (0,0,1,1) }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; fixed4 _Tint; float4 _UVRect;
            fixed4 frag(v2f_img input) : SV_Target { return tex2D(_MainTex,_UVRect.xy+input.uv*_UVRect.zw)*_Tint; }
            ENDCG
        }
        Pass
        {
            Name "Background Image Layer"
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; fixed4 _Tint; float4 _UVRect;
            struct BackgroundImageVaryings { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
            BackgroundImageVaryings vert(appdata_base input) { BackgroundImageVaryings output; output.position=UnityObjectToClipPos(input.vertex); output.uv=input.texcoord.xy; return output; }
            fixed4 frag(BackgroundImageVaryings input) : SV_Target { return tex2D(_MainTex,_UVRect.xy+input.uv*_UVRect.zw)*_Tint; }
            ENDCG
        }
        Pass
        {
            Name "Unlit Background Geometry"
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Tint; float4 _LayerScreenFade; float _ViewVerticalScale;
            struct BackgroundGeometryVaryings { float4 position : SV_POSITION; float2 uv : TEXCOORD0; float2 cameraRay : TEXCOORD1; };
            BackgroundGeometryVaryings vert(appdata_base input)
            {
                BackgroundGeometryVaryings output; output.position=UnityObjectToClipPos(input.vertex);
                output.uv=TRANSFORM_TEX(input.texcoord,_MainTex);
                float3 cameraPosition=UnityObjectToViewPos(input.vertex); output.cameraRay=float2(cameraPosition.y,-cameraPosition.z); return output;
            }
            fixed4 frag(BackgroundGeometryVaryings input) : SV_Target
            {
                fixed4 color=tex2D(_MainTex,input.uv)*_Tint;
                float viewportY=.5+input.cameraRay.x/max(.0001,input.cameraRay.y)*_ViewVerticalScale;
                if(_LayerScreenFade.z>.5) color.a*=smoothstep(_LayerScreenFade.x,_LayerScreenFade.x+_LayerScreenFade.y,viewportY);
                return color;
            }
            ENDCG
        }
    }
    Fallback Off
}
