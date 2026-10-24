#ifndef FLOW_WATER_GARDEN_INCLUDED
#define FLOW_WATER_GARDEN_INCLUDED

// Visual-only shading. Main and detail UV transforms remain controlled by the
// original material/animation property blocks, including their authored speed.
sampler2D _MainTex, _BumpMap, _DetailTex;
fixed4 _Color, _ArtTint, _ArtHighlight;
half _ArtSaturation, _ArtContrast, _ArtNormalStrength, _ArtWrap;
half _ArtSpecular, _ArtWaterSurface, _ArtOpacity, _ArtWaveStrength;

inline half3 GardenAlbedo(half3 source)
{
    half luminance = dot(source, half3(.2126, .7152, .0722));
    half3 albedo = lerp(luminance.xxx, source, _ArtSaturation);
    // A restrained shoulder keeps the old low-resolution stone texture from
    // dominating small boards while preserving all UV islands and seams.
    albedo = (albedo - .5h) * _ArtContrast + .5h;
    return saturate(albedo * _ArtTint.rgb);
}

inline half4 LightingGarden(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
{
    half nDotL = dot(s.Normal, lightDir);
    half diffuse = saturate((nDotL + _ArtWrap) / (1.0h + _ArtWrap));
    half3 halfDirection = normalize(lightDir + viewDir);
    half highlight = pow(saturate(dot(s.Normal, halfDirection)), lerp(26.0h, 82.0h, _ArtWaterSurface));
    half3 specular = highlight * s.Gloss * _ArtHighlight.rgb * saturate(nDotL * 4.0h);
    half3 result = (s.Albedo * diffuse + specular) * _LightColor0.rgb * atten;
    return half4(result, s.Alpha);
}

inline void GardenStone(float2 normalUV, fixed4 source, inout SurfaceOutput o)
{
    o.Albedo = GardenAlbedo(source.rgb);
    half3 normal = UnpackNormal(tex2D(_BumpMap, normalUV));
    o.Normal = normalize(half3(normal.xy * _ArtNormalStrength, max(.2h, normal.z)));
    o.Specular = .2h;
    o.Gloss = _ArtSpecular;
    o.Alpha = source.a;
}

inline void GardenWater(float2 mainUV, float2 detailUV, float3 viewDirection, fixed4 source, inout SurfaceOutput o)
{
    // Two scales of the same seamless normal produce broad flow plus minute
    // ripples. No mesh displacement, GrabPass, depth dependency or extra camera.
    half3 broad = UnpackNormal(tex2D(_DetailTex, detailUV * 1.8));
    half3 fine = UnpackNormal(tex2D(_DetailTex, detailUV * 5.3 + half2(.23, .47)));
    half2 waves = (broad.xy * .72h + fine.xy * .28h) * _ArtWaveStrength;
    o.Normal = normalize(half3(waves, 1));
    half fresnel = pow(1.0h - saturate(dot(normalize(viewDirection), o.Normal)), 3.0h);
    half3 water = lerp(source.rgb, _Color.rgb, .28h);
    o.Albedo = water * lerp(.86h, 1.05h, source.r) * _ArtTint.rgb;
    o.Emission = _ArtHighlight.rgb * fresnel * .13h + water * .025h;
    o.Specular = .72h;
    o.Gloss = _ArtSpecular;
    o.Alpha = saturate(source.a * _ArtOpacity + fresnel * .06h);
}

#endif
