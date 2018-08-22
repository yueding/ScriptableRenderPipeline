#ifndef HD_SHADOW_CONTEXT_HLSL
#define HD_SHADOW_CONTEXT_HLSL

// #include "CoreRP/ShaderLibrary/Shadow/Shadow.hlsl"
#include "HDRP/Shadows/HDShadow.hlsl"

TEXTURE2D(_ShadowmapAtlas);
SAMPLER(sampler_ShadowmapAtlas);

TEXTURE2D(_ShadowmapCascadeAtlas);
SAMPLER(sampler_ShadowmapCascadeAtlas);

StructuredBuffer<HDShadowData>  _HDShadowDatas;

ShadowContext InitShadowContext()
{
    ShadowContext sc;

    sc.shadowDatas = _HDShadowDatas;

    return sc;
}

#endif // HD_SHADOW_CONTEXT_HLSL
