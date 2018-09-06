#ifndef LIGHTLOOP_HD_SHADOW_HLSL
#define LIGHTLOOP_HD_SHADOW_HLSL

#define SHADOW_OPTIMIZE_REGISTER_USAGE 1

#define SHADOW_USE_VIEW_BIAS_SCALING            1   // Enable view bias scaling to mitigate light leaking across edges. Uses the light vector if SHADOW_USE_ONLY_VIEW_BASED_BIASING is defined, otherwise uses the normal.
// Note: Sample biasing work well but is very costly in term of VGPR, disable it for now
#define SHADOW_USE_SAMPLE_BIASING               0   // Enable per sample biasing for wide multi-tap PCF filters. Incompatible with SHADOW_USE_ONLY_VIEW_BASED_BIASING.
#define SHADOW_USE_DEPTH_BIAS                   0   // Enable clip space z biasing

# include "HDRP/Shadows/HDShadowContext.hlsl"

float GetDirectionalShadowAttenuation(HDShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L)
{
    return EvalShadow_CascadedDepth_Blend(shadowContext, _ShadowmapCascadeAtlas, sampler_ShadowmapCascadeAtlas, positionWS, normalWS, shadowDataIndex, L);
}

float GetDirectionalShadowAttenuation(HDShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L, float2 positionSS)
{
    return GetDirectionalShadowAttenuation(shadowContext, positionWS, normalWS, shadowDataIndex, L);
}

float GetSpotShadowAttenuation(HDShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L, float L_dist)
{
    HDShadowData sd = shadowContext.shadowDatas[shadowDataIndex];
    return EvalShadow_PunctualDepth(sd, _ShadowmapAtlas, sampler_ShadowmapAtlas, positionWS, normalWS, L, L_dist);
}

float GetSpotShadowAttenuation(HDShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L, float L_dist, float2 positionSS)
{
    return GetSpotShadowAttenuation(shadowContext, positionWS, normalWS, shadowDataIndex, L, L_dist);
}

float GetPointShadowAttenuation(HDShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L, float L_dist)
{
    // Note: Here we assume that all the shadow map cube faces have been added contiguously in the buffer to retreive the shadow information
    HDShadowData sd = shadowContext.shadowDatas[shadowDataIndex + CubeMapFaceID(-L)];

    return EvalShadow_PunctualDepth(sd, _ShadowmapAtlas, sampler_ShadowmapAtlas, positionWS, normalWS, L, L_dist);
}

float GetPointShadowAttenuation(HDShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L, float L_dist, float2 positionSS)
{
    return GetPointShadowAttenuation(shadowContext, positionWS, normalWS, shadowDataIndex, L, L_dist);
}

float GetPunctualShadowClosestDistance(HDShadowContext shadowContext, SamplerState sampl, real3 positionWS, int shadowDataIndex, float3 L, float3 lightPositionWS)
{
    // Note: Here we assume that all the shadow map cube faces have been added contiguously in the buffer to retreive the shadow information
    // TODO: if on the light type to retrieve the good shadow data
    HDShadowData sd = shadowContext.shadowDatas[shadowDataIndex + CubeMapFaceID(-L)];
    return EvalShadow_SampleClosestDistance_Punctual(sd, _ShadowmapAtlas, s_linear_clamp_sampler, positionWS, L, lightPositionWS);
}

#endif // LIGHTLOOP_HD_SHADOW_HLSL
