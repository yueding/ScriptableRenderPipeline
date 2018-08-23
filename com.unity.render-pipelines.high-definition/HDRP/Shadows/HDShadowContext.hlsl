#ifndef HD_SHADOW_CONTEXT_HLSL
#define HD_SHADOW_CONTEXT_HLSL

#include "CoreRP/ShaderLibrary/Common.hlsl"
#include "HDRP/Shadows/HDShadowManager.cs.hlsl"

// Custom shadowmap sampling functions
#include "HDRP/Shadows/HDShadowTexFetch.hlsl"

struct HDDirectionalShadow
{
    float4      sphereCascades[4];
    float4      cascadeDirection;
    float       cascadeBorders[4];
};

struct HDShadowContext
{
    StructuredBuffer<HDShadowData>  shadowDatas;
    HDDirectionalShadow             directionalShadowData;
};

// HD shadow sampling bindings
#include "HDShadowSampling.hlsl"
#include "HDShadowAlgorithms.hlsl"

TEXTURE2D(_ShadowmapAtlas);
SAMPLER(sampler_ShadowmapAtlas);

TEXTURE2D(_ShadowmapCascadeAtlas);
SAMPLER(sampler_ShadowmapCascadeAtlas);

StructuredBuffer<HDShadowData>              _HDShadowDatas;
// Only the first element is used since we only support one directional light
StructuredBuffer<HDDirectionalShadowData>   _HDDirectionalShadowData;

HDShadowContext InitShadowContext()
{
    HDShadowContext         sc;
    HDDirectionalShadow     ds;
    HDDirectionalShadowData dsd = _HDDirectionalShadowData[0];

    // Repack these fields into array for convenience
    ds.sphereCascades[0] = dsd.sphereCascade1;
    ds.sphereCascades[1] = dsd.sphereCascade2;
    ds.sphereCascades[2] = dsd.sphereCascade3;
    ds.sphereCascades[3] = dsd.sphereCascade4;

    ds.cascadeDirection = dsd.cascadeDirection;

    ds.cascadeBorders[0] = dsd.cascadeBorder1;
    ds.cascadeBorders[1] = dsd.cascadeBorder2;
    ds.cascadeBorders[2] = dsd.cascadeBorder3;
    ds.cascadeBorders[3] = dsd.cascadeBorder4;

    sc.shadowDatas = _HDShadowDatas;
    sc.directionalShadowData = ds;

    return sc;
}

#endif // HD_SHADOW_CONTEXT_HLSL
