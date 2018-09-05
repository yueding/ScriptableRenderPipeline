#ifndef LIGHTWEIGHT_PASS_META_INCLUDED
#define LIGHTWEIGHT_PASS_META_INCLUDED

#include "LWRP/ShaderLibrary/Lighting.hlsl"

CBUFFER_START(UnityMetaPass)
// x = use uv1 as raster position
// y = use uv2 as raster position
bool4 unity_MetaVertexControl;

// x = return albedo
// y = return normal
bool4 unity_MetaFragmentControl;
CBUFFER_END

float unity_OneOverOutputBoost;
float unity_MaxOutputValue;
float unity_UseLinearSpace;

struct MetaInput
{
    half3 Albedo;
    half3 Emission;
    half3 SpecularColor;
};

struct Attributes
{
    float4 vertex   : POSITION;
    half3 normal    : NORMAL;
    float2 uv0      : TEXCOORD0;
    float2 uv1      : TEXCOORD1;
    float2 uv2      : TEXCOORD2;
#ifdef _TANGENT_TO_WORLD
    half4 tangent   : TANGENT;
#endif
};

struct Varyings
{
    float4 pos      : SV_POSITION;
    float2 uv       : TEXCOORD0;
};

float4 MetaVertexPosition(float4 vertex, float2 uv1, float2 uv2, float4 lightmapST)
{
    if (unity_MetaVertexControl.x)
    {
        vertex.xy = uv1 * lightmapST.xy + lightmapST.zw;
        // OpenGL right now needs to actually use incoming vertex position,
        // so use it in a very dummy way
        vertex.z = vertex.z > 0 ? REAL_MIN : 0.0f;
    }
    return TransformWorldToHClip(vertex.xyz); // Need to transfer from world to clip compared to legacy
}

half4 MetaFragment(MetaInput input)
{
    half4 res = 0;
    if (unity_MetaFragmentControl.x)
    {
        res = half4(input.Albedo, 1);

        // d3d9 shader compiler doesn't like NaNs and infinity.
        unity_OneOverOutputBoost = saturate(unity_OneOverOutputBoost);

        // Apply Albedo Boost from LightmapSettings.
        res.rgb = clamp(PositivePow(res.rgb, unity_OneOverOutputBoost), 0, unity_MaxOutputValue);
    }
    if (unity_MetaFragmentControl.y)
    {
        res = half4(input.Emission, 1.0);
    }
    return res;
}

Varyings LightweightVertexMeta(Attributes input)
{
    Varyings output;
    output.pos = MetaVertexPosition(input.vertex, input.uv1.xy, input.uv2.xy, unity_LightmapST);
    output.uv = TRANSFORM_TEX(input.uv0, _MainTex);
    return output;
}

#endif
