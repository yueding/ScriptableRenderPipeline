#ifndef LIGHTLOOP_HD_SHADOW_HLSL
#define LIGHTLOOP_HD_SHADOW_HLSL

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
    return EvalShadow_SpotDepth(shadowContext, _ShadowmapAtlas, sampler_ShadowmapAtlas, positionWS, normalWS, shadowDataIndex, L, L_dist);
}

float GetSpotShadowAttenuation(HDShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L, float L_dist, float2 positionSS)
{
    return GetSpotShadowAttenuation(shadowContext, positionWS, normalWS, shadowDataIndex, L, L_dist);
}

float GetPointShadowAttenuation(HDShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L, float L_dist)
{
    return EvalShadow_PointDepth(shadowContext, _ShadowmapAtlas, sampler_ShadowmapAtlas, positionWS, normalWS, shadowDataIndex, L, L_dist);
}

float GetPointShadowAttenuation(HDShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L, float L_dist, float2 positionSS)
{
    return GetPointShadowAttenuation(shadowContext, positionWS, normalWS, shadowDataIndex, L, L_dist);
}

float GetPunctualShadowClosestDistance(HDShadowContext shadowContext, SamplerState sampl, real3 positionWS, int index, float3 L, float3 lightPositionWS)
{
    // TODO: call closest distance algorithm
    return EvalShadow_SampleClosestDistance_Punctual(shadowContext, _ShadowmapAtlas, s_linear_clamp_sampler, positionWS, index, L, lightPositionWS);
}

#endif // LIGHTLOOP_HD_SHADOW_HLSL
