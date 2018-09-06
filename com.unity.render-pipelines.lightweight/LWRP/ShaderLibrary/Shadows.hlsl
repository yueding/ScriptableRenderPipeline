#ifndef LIGHTWEIGHT_SHADOWS_INCLUDED
#define LIGHTWEIGHT_SHADOWS_INCLUDED

#include "CoreRP/ShaderLibrary/Common.hlsl"
#include "CoreRP/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"
#include "Core.hlsl"

#define MAX_SHADOW_CASCADES 4

#ifndef SHADOWS_SCREEN
#if defined(_DIRECTIONAL_SHADOWS) && defined(_DIRECTIONAL_SHADOWS_CASCADE) && !defined(SHADER_API_GLES)
#define SHADOWS_SCREEN 1
#else
#define SHADOWS_SCREEN 0
#endif
#endif

SCREENSPACE_TEXTURE(_ScreenSpaceShadowMapTexture);
SAMPLER(sampler_ScreenSpaceShadowMapTexture);

TEXTURE2D_SHADOW(_DirectionalShadowmapTexture);
SAMPLER_CMP(sampler_DirectionalShadowmapTexture);

TEXTURE2D_SHADOW(_PunctualShadowmapTexture);
SAMPLER_CMP(sampler_PunctualShadowmapTexture);

CBUFFER_START(_DirectionalShadowBuffer)
// Last cascade is initialized with a no-op matrix. It always transforms
// shadow coord to half3(0, 0, NEAR_PLANE). We use this trick to avoid
// branching since ComputeCascadeIndex can return cascade index = MAX_SHADOW_CASCADES
float4x4    _DirectionalLightWorldToShadow[MAX_SHADOW_CASCADES + 1];
float4      _CascadeShadowSplitSpheres0;
float4      _CascadeShadowSplitSpheres1;
float4      _CascadeShadowSplitSpheres2;
float4      _CascadeShadowSplitSpheres3;
float4      _CascadeShadowSplitSphereRadii;
half4       _DirectionalShadowOffset0;
half4       _DirectionalShadowOffset1;
half4       _DirectionalShadowOffset2;
half4       _DirectionalShadowOffset3;
half4       _DirectionalShadowData;    // (x: shadowStrength)
float4      _DirectionalShadowmapSize; // (xy: 1/width and 1/height, zw: width and height)
CBUFFER_END

CBUFFER_START(_PunctualShadowBuffer)
float4x4    _PunctualLightsWorldToShadow[MAX_VISIBLE_LIGHTS];
half        _PunctualShadowStrength[MAX_VISIBLE_LIGHTS];
half4       _PunctualShadowOffset0;
half4       _PunctualShadowOffset1;
half4       _PunctualShadowOffset2;
half4       _PunctualShadowOffset3;
float4      _PunctualShadowmapSize; // (xy: 1/width and 1/height, zw: width and height)
CBUFFER_END

#if UNITY_REVERSED_Z
#define BEYOND_SHADOW_FAR(shadowCoord) shadowCoord.z <= UNITY_RAW_FAR_CLIP_VALUE
#else
#define BEYOND_SHADOW_FAR(shadowCoord) shadowCoord.z >= UNITY_RAW_FAR_CLIP_VALUE
#endif

struct ShadowSamplingData
{
    half4 shadowOffset0;
    half4 shadowOffset1;
    half4 shadowOffset2;
    half4 shadowOffset3;
    float4 shadowmapSize;
};

ShadowSamplingData GetDirectionalLightShadowSamplingData()
{
    ShadowSamplingData shadowSamplingData;
    shadowSamplingData.shadowOffset0 = _DirectionalShadowOffset0;
    shadowSamplingData.shadowOffset1 = _DirectionalShadowOffset1;
    shadowSamplingData.shadowOffset2 = _DirectionalShadowOffset2;
    shadowSamplingData.shadowOffset3 = _DirectionalShadowOffset3;
    shadowSamplingData.shadowmapSize = _DirectionalShadowmapSize;
    return shadowSamplingData;
}

ShadowSamplingData GetPunctualLightShadowSamplingData()
{
    ShadowSamplingData shadowSamplingData;
    shadowSamplingData.shadowOffset0 = _PunctualShadowOffset0;
    shadowSamplingData.shadowOffset1 = _PunctualShadowOffset1;
    shadowSamplingData.shadowOffset2 = _PunctualShadowOffset2;
    shadowSamplingData.shadowOffset3 = _PunctualShadowOffset3;
    shadowSamplingData.shadowmapSize = _PunctualShadowmapSize;
    return shadowSamplingData;
}

half GetDirectionalLightShadowStrength()
{
    return _DirectionalShadowData.x;
}

half GetPunctualLightShadowStrenth(int lightIndex)
{
    return _PunctualShadowStrength[lightIndex];
}

half SampleScreenSpaceShadowMap(float4 shadowCoord)
{
    shadowCoord.xy /= shadowCoord.w;

    // The stereo transform has to happen after the manual perspective divide
    shadowCoord.xy = UnityStereoTransformScreenSpaceTex(shadowCoord.xy);

#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    half attenuation = SAMPLE_TEXTURE2D_ARRAY(_ScreenSpaceShadowMapTexture, sampler_ScreenSpaceShadowMapTexture, shadowCoord.xy, unity_StereoEyeIndex).x;
#else
    half attenuation = SAMPLE_TEXTURE2D(_ScreenSpaceShadowMapTexture, sampler_ScreenSpaceShadowMapTexture, shadowCoord.xy).x;
#endif

    return attenuation;
}

real SampleShadowmap(float4 shadowCoord, TEXTURE2D_SHADOW_ARGS(ShadowMap, sampler_ShadowMap), ShadowSamplingData samplingData, half shadowStrength, bool isPerspectiveProjection = true)
{
    // Compiler will optimize this branch away as long as isPerspectiveProjection is known at compile time
    if (isPerspectiveProjection)
        shadowCoord.xyz /= shadowCoord.w;

    real attenuation;

#ifdef _SHADOWS_SOFT
    #ifdef SHADER_API_MOBILE
        // 4-tap hardware comparison
        real4 attenuation4;
        attenuation4.x = SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, shadowCoord.xyz + samplingData.shadowOffset0.xyz);
        attenuation4.y = SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, shadowCoord.xyz + samplingData.shadowOffset1.xyz);
        attenuation4.z = SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, shadowCoord.xyz + samplingData.shadowOffset2.xyz);
        attenuation4.w = SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, shadowCoord.xyz + samplingData.shadowOffset3.xyz);
        attenuation = dot(attenuation4, 0.25);
    #else
        float fetchesWeights[9];
        float2 fetchesUV[9];
        SampleShadow_ComputeSamples_Tent_5x5(samplingData.shadowmapSize, shadowCoord.xy, fetchesWeights, fetchesUV);

        attenuation  = fetchesWeights[0] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[0].xy, shadowCoord.z));
        attenuation += fetchesWeights[1] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[1].xy, shadowCoord.z));
        attenuation += fetchesWeights[2] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[2].xy, shadowCoord.z));
        attenuation += fetchesWeights[3] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[3].xy, shadowCoord.z));
        attenuation += fetchesWeights[4] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[4].xy, shadowCoord.z));
        attenuation += fetchesWeights[5] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[5].xy, shadowCoord.z));
        attenuation += fetchesWeights[6] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[6].xy, shadowCoord.z));
        attenuation += fetchesWeights[7] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[7].xy, shadowCoord.z));
        attenuation += fetchesWeights[8] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[8].xy, shadowCoord.z));
    #endif
#else
    // 1-tap hardware comparison
    attenuation = SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, shadowCoord.xyz);
#endif

    attenuation = LerpWhiteTo(attenuation, shadowStrength);

    // Shadow coords that fall out of the light frustum volume must always return attenuation 1.0
    return BEYOND_SHADOW_FAR(shadowCoord) ? 1.0 : attenuation;
}

half ComputeCascadeIndex(float3 positionWS)
{
    float3 fromCenter0 = positionWS - _CascadeShadowSplitSpheres0.xyz;
    float3 fromCenter1 = positionWS - _CascadeShadowSplitSpheres1.xyz;
    float3 fromCenter2 = positionWS - _CascadeShadowSplitSpheres2.xyz;
    float3 fromCenter3 = positionWS - _CascadeShadowSplitSpheres3.xyz;
    float4 distances2 = float4(dot(fromCenter0, fromCenter0), dot(fromCenter1, fromCenter1), dot(fromCenter2, fromCenter2), dot(fromCenter3, fromCenter3));

    half4 weights = half4(distances2 < _CascadeShadowSplitSphereRadii);
    weights.yzw = saturate(weights.yzw - weights.xyz);

    return 4 - dot(weights, half4(4, 3, 2, 1));
}

float4 TransformWorldToShadowCoord(float3 positionWS)
{
#ifdef _DIRECTIONAL_SHADOWS_CASCADE
    half cascadeIndex = ComputeCascadeIndex(positionWS);
    return mul(_DirectionalLightWorldToShadow[cascadeIndex], float4(positionWS, 1.0));
#else
    return mul(_DirectionalLightWorldToShadow[0], float4(positionWS, 1.0));
#endif
}

float4 ComputeShadowCoord(float4 clipPos)
{
    // TODO: This might have to be corrected for double-wide and texture arrays
    return ComputeScreenPos(clipPos);
}

half DirectionalLightRealtimeShadow(float4 shadowCoord)
{
#if !defined(_DIRECTIONAL_SHADOWS) || defined(_RECEIVE_SHADOWS_OFF)
    return 1.0h;
#endif

#if SHADOWS_SCREEN
    return SampleScreenSpaceShadowMap(shadowCoord);
#else
    ShadowSamplingData shadowSamplingData = GetDirectionalLightShadowSamplingData();
    half shadowStrength = GetDirectionalLightShadowStrength();
    return SampleShadowmap(shadowCoord, TEXTURE2D_PARAM(_DirectionalShadowmapTexture, sampler_DirectionalShadowmapTexture), shadowSamplingData, shadowStrength, false);
#endif
}

half PunctualLightRealtimeShadow(int lightIndex, float3 positionWS)
{
#if !defined(_PUNCTUAL_LIGHT_SHADOWS) || defined(_RECEIVE_SHADOWS_OFF)
    return 1.0h;
#else
    float4 shadowCoord = mul(_PunctualLightsWorldToShadow[lightIndex], float4(positionWS, 1.0));
    ShadowSamplingData shadowSamplingData = GetPunctualLightShadowSamplingData();
    half shadowStrength = GetPunctualLightShadowStrenth(lightIndex);
    return SampleShadowmap(shadowCoord, TEXTURE2D_PARAM(_PunctualShadowmapTexture, sampler_PunctualShadowmapTexture), shadowSamplingData, shadowStrength, true);
#endif
}

float4 GetShadowCoord(VertexPosition vertexPosition)
{
#if SHADOWS_SCREEN
    return ComputeShadowCoord(vertexPosition.hclipSpace);
#else
    return TransformWorldToShadowCoord(vertexPosition.worldSpace);
#endif
}

#endif
