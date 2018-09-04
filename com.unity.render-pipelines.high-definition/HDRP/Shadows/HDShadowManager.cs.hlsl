//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef HDSHADOWMANAGER_CS_HLSL
#define HDSHADOWMANAGER_CS_HLSL
//
// UnityEngine.Experimental.Rendering.HDPipeline.HDShadowFlag:  static fields
//
#define HDSHADOWFLAG_SAMPLE_BIAS_SCALE (1)
#define HDSHADOWFLAG_EDGE_LEAK_FIXUP (2)
#define HDSHADOWFLAG_EDGE_TOLERANCE_NORMAL (4)

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.HDShadowData
// PackingRules = Exact
struct HDShadowData
{
    float4x4 viewProjection;
    float4x4 shadowToWorld;
    float4 scaleOffset;
    float4 textureSize;
    float4 textureSizeRcp;
    float4 viewBias;
    float4 normalBias;
    int flags;
    float edgeTolerance;
    float4 shadowFilterParams1;
};

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.HDDirectionalShadowData
// PackingRules = Exact
struct HDDirectionalShadowData
{
    float4 sphereCascades[4];
    float4 cascadeDirection;
    float cascadeBorders[4];
};

//
// Accessors for UnityEngine.Experimental.Rendering.HDPipeline.HDShadowData
//
float4x4 GetViewProjection(HDShadowData value)
{
    return value.viewProjection;
}
float4x4 GetShadowToWorld(HDShadowData value)
{
    return value.shadowToWorld;
}
float4 GetScaleOffset(HDShadowData value)
{
    return value.scaleOffset;
}
float4 GetTextureSize(HDShadowData value)
{
    return value.textureSize;
}
float4 GetTextureSizeRcp(HDShadowData value)
{
    return value.textureSizeRcp;
}
float4 GetViewBias(HDShadowData value)
{
    return value.viewBias;
}
float4 GetNormalBias(HDShadowData value)
{
    return value.normalBias;
}
int GetFlags(HDShadowData value)
{
    return value.flags;
}
float GetEdgeTolerance(HDShadowData value)
{
    return value.edgeTolerance;
}
float4 GetShadowFilterParams1(HDShadowData value)
{
    return value.shadowFilterParams1;
}

//
// Accessors for UnityEngine.Experimental.Rendering.HDPipeline.HDDirectionalShadowData
//
float4 GetSphereCascades(HDDirectionalShadowData value, int index)
{
    return value.sphereCascades[index];
}
float4 GetCascadeDirection(HDDirectionalShadowData value)
{
    return value.cascadeDirection;
}
float GetCascadeBorders(HDDirectionalShadowData value, int index)
{
    return value.cascadeBorders[index];
}


#endif
