#ifndef HD_SHADOW_CONTEXT_HLSL
#define HD_SHADOW_CONTEXT_HLSL

#include "CoreRP/ShaderLibrary/Common.hlsl"
#include "HDRP/Shadows/HDShadowManager.cs.hlsl"

struct ShadowContext
{
    StructuredBuffer<HDShadowData>  shadowDatas;
    Texture2D                       atlas;
    Texture2D                       cascadeAtlas;
    SamplerComparisonState          compSamp;
    SamplerState                    samp;
};

#include "HDShadowSampling.hlsl"
#include "HDShadowAlgorithms.hlsl"

#endif