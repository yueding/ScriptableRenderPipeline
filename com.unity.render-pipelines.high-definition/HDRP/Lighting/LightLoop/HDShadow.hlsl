#ifndef LIGHTLOOP_HD_SHADOW_HLSL
#define LIGHTLOOP_HD_SHADOW_HLSL

# include "HDRP/Shadows/HDShadowContext.hlsl"

float GetDirectionalShadowAttenuation(HDShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L)
{
    // TODO: call the shadow sampling algorithm
    return EvalShadow_CascadedDepth_Blend(shadowContext, _ShadowmapCascadeAtlas, sampler_ShadowmapCascadeAtlas, positionWS, normalWS, shadowDataIndex, L);
}

float GetDirectionalShadowAttenuation(HDShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L, float2 positionSS)
{
    return GetDirectionalShadowAttenuation(shadowContext, positionWS, normalWS, shadowDataIndex, L);
}

// TODO: we may want to remove the Punctual functions and separate Point and Spot
float GetPunctualShadowAttenuation(HDShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L, float L_dist)
{
    // TODO: we may want to remove this function and replace it with two (one for spot and another for point)

    return EvalShadow_SpotDepth(shadowContext, _ShadowmapAtlas, sampler_ShadowmapAtlas, positionWS, normalWS, shadowDataIndex, L, L_dist);

    HDShadowData sd = shadowContext.shadowDatas[shadowDataIndex];
    real2 atlasUv = sd.scaleOffset.xy + sd.scaleOffset.zw / 2.0;
    
    return SAMPLE_TEXTURE2D_SHADOW(_ShadowmapAtlas, sampler_ShadowmapAtlas, real3(atlasUv, positionWS.x));
}

float GetPunctualShadowAttenuation(HDShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L, float L_dist, float2 positionSS)
{
    return GetPunctualShadowAttenuation(shadowContext, positionWS, normalWS, shadowDataIndex, L, L_dist);
}

float GetPunctualShadowClosestDistance(HDShadowContext shadowContext, SamplerState sampl, real3 positionWS, int index, float3 L, float3 lightPositionWS)
{
    // TODO: call closest distance algorithm
    return EvalShadow_SampleClosestDistance_Punctual(shadowContext, _ShadowmapAtlas, s_linear_clamp_sampler, positionWS, index, L, lightPositionWS);
}

#endif // LIGHTLOOP_HD_SHADOW_HLSL
