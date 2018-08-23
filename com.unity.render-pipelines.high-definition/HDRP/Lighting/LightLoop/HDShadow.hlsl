#ifndef LIGHTLOOP_HD_SHADOW_HLSL
#define LIGHTLOOP_HD_SHADOW_HLSL

# include "HDRP/Shadows/HDShadowContext.hlsl"

float GetDirectionalShadowAttenuation( HDShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L )
{
    // TODO: call the shadow sampling algorithm
    return 0;
}

float GetDirectionalShadowAttenuation( HDShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L, float2 positionSS )
{
    return GetDirectionalShadowAttenuation( shadowContext, positionWS, normalWS, shadowDataIndex, L );
}

// TODO: we may want to remove the Punctual functions and separate Point and Spot
float GetPunctualShadowAttenuation( HDShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L, float L_dist )
{
    // TODO: call the shadow sampling algorithm
    return 0;
}

float GetPunctualShadowAttenuation( HDShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L, float L_dist, float2 positionSS )
{
    return GetPunctualShadowAttenuation( shadowContext, positionWS, normalWS, shadowDataIndex, L, L_dist );
}

float GetPunctualShadowClosestDistance( HDShadowContext shadowContext, SamplerState sampl, real3 positionWS, int index, float3 L, float3 lightPositionWS)
{
    // TODO: call closest distance algorithm
    return 0;
}

#endif // LIGHTLOOP_HD_SHADOW_HLSL
