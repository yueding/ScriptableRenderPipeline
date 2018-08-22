//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef HDSHADOWMANAGER_CS_HLSL
#define HDSHADOWMANAGER_CS_HLSL
// Generated from UnityEngine.Experimental.Rendering.HDPipeline.HDShadowData
// PackingRules = Exact
struct HDShadowData
{
    float4x4 view;
    float4x4 projection;
    float4 scaleOffset;
    float4 textureSize;
    float4 texelSizeRcp;
};

//
// Accessors for UnityEngine.Experimental.Rendering.HDPipeline.HDShadowData
//
float4x4 GetView(HDShadowData value)
{
    return value.view;
}
float4x4 GetProjection(HDShadowData value)
{
    return value.projection;
}
float4 GetScaleOffset(HDShadowData value)
{
    return value.scaleOffset;
}
float4 GetTextureSize(HDShadowData value)
{
    return value.textureSize;
}
float4 GetTexelSizeRcp(HDShadowData value)
{
    return value.texelSizeRcp;
}


#endif
