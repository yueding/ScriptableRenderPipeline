//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef AXF_CS_HLSL
#define AXF_CS_HLSL
//
// UnityEngine.Experimental.Rendering.HDPipeline.AxF+SurfaceData:  static fields
//
#define DEBUGVIEW_AXF_SURFACEDATA_NORMAL (1500)
#define DEBUGVIEW_AXF_SURFACEDATA_NORMAL_VIEW_SPACE (1501)
#define DEBUGVIEW_AXF_SURFACEDATA_TANGENT (1502)
#define DEBUGVIEW_AXF_SURFACEDATA_BI_TANGENT (1503)
#define DEBUGVIEW_AXF_SURFACEDATA_DIFFUSE_COLOR (1504)
#define DEBUGVIEW_AXF_SURFACEDATA_SPECULAR_COLOR (1505)
#define DEBUGVIEW_AXF_SURFACEDATA_FRESNEL_F0 (1506)
#define DEBUGVIEW_AXF_SURFACEDATA_SPECULAR_LOBE (1507)
#define DEBUGVIEW_AXF_SURFACEDATA_HEIGHT (1508)
#define DEBUGVIEW_AXF_SURFACEDATA_ANISOTROPIC_ANGLE (1509)
#define DEBUGVIEW_AXF_SURFACEDATA_FLAKES_UV (1510)
#define DEBUGVIEW_AXF_SURFACEDATA_FLAKES_MIP (1511)
#define DEBUGVIEW_AXF_SURFACEDATA_CLEAR_COAT_COLOR (1512)
#define DEBUGVIEW_AXF_SURFACEDATA_CLEAR_COAT_NORMAL (1513)
#define DEBUGVIEW_AXF_SURFACEDATA_CLEAR_COAT_IOR (1514)

//
// UnityEngine.Experimental.Rendering.HDPipeline.AxF+BSDFData:  static fields
//
#define DEBUGVIEW_AXF_BSDFDATA_NORMAL_WS (1600)
#define DEBUGVIEW_AXF_BSDFDATA_NORMAL_VIEW_SPACE (1601)
#define DEBUGVIEW_AXF_BSDFDATA_TANGENT_WS (1602)
#define DEBUGVIEW_AXF_BSDFDATA_BI_TANGENT_WS (1603)
#define DEBUGVIEW_AXF_BSDFDATA_DIFFUSE_COLOR (1604)
#define DEBUGVIEW_AXF_BSDFDATA_SPECULAR_COLOR (1605)
#define DEBUGVIEW_AXF_BSDFDATA_FRESNEL_F0 (1606)
#define DEBUGVIEW_AXF_BSDFDATA_ROUGHNESS (1607)
#define DEBUGVIEW_AXF_BSDFDATA_HEIGHT_MM (1608)
#define DEBUGVIEW_AXF_BSDFDATA_ANISOTROPY_ANGLE (1609)
#define DEBUGVIEW_AXF_BSDFDATA_FLAKES_UV (1610)
#define DEBUGVIEW_AXF_BSDFDATA_FLAKES_MIP (1611)
#define DEBUGVIEW_AXF_BSDFDATA_CLEAR_COAT_COLOR (1612)
#define DEBUGVIEW_AXF_BSDFDATA_CLEAR_COAT_NORMAL_WS (1613)
#define DEBUGVIEW_AXF_BSDFDATA_CLEAR_COAT_IOR (1614)

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.AxF+SurfaceData
// PackingRules = Exact
struct SurfaceData
{
    float3 normalWS;
    float3 tangentWS;
    float3 biTangentWS;
    float3 diffuseColor;
    float3 specularColor;
    float3 fresnelF0;
    float2 specularLobe;
    float height_mm;
    float anisotropyAngle;
    float2 flakesUV;
    float flakesMipLevel;
    float3 clearCoatColor;
    float3 clearCoatNormalWS;
    float clearCoatIOR;
};

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.AxF+BSDFData
// PackingRules = Exact
struct BSDFData
{
    float3 normalWS;
    float3 tangentWS;
    float3 biTangentWS;
    float3 diffuseColor;
    float3 specularColor;
    float3 fresnelF0;
    float2 roughness;
    float height_mm;
    float anisotropyAngle;
    float2 flakesUV;
    float flakesMipLevel;
    float3 clearCoatColor;
    float3 clearCoatNormalWS;
    float clearCoatIOR;
};

//
// Debug functions
//
void GetGeneratedSurfaceDataDebug(uint paramId, SurfaceData surfacedata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_AXF_SURFACEDATA_NORMAL:
            result = surfacedata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_NORMAL_VIEW_SPACE:
            result = surfacedata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_TANGENT:
            result = surfacedata.tangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_BI_TANGENT:
            result = surfacedata.biTangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_DIFFUSE_COLOR:
            result = surfacedata.diffuseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_SPECULAR_COLOR:
            result = surfacedata.specularColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_FRESNEL_F0:
            result = surfacedata.fresnelF0;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_SPECULAR_LOBE:
            result = float3(surfacedata.specularLobe, 0.0);
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_HEIGHT:
            result = surfacedata.height_mm.xxx;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_ANISOTROPIC_ANGLE:
            result = surfacedata.anisotropyAngle.xxx;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_FLAKES_UV:
            result = float3(surfacedata.flakesUV, 0.0);
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_FLAKES_MIP:
            result = surfacedata.flakesMipLevel.xxx;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_CLEAR_COAT_COLOR:
            result = surfacedata.clearCoatColor;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_CLEAR_COAT_NORMAL:
            result = surfacedata.clearCoatNormalWS * 0.5 + 0.5;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_CLEAR_COAT_IOR:
            result = surfacedata.clearCoatIOR.xxx;
            break;
    }
}

//
// Debug functions
//
void GetGeneratedBSDFDataDebug(uint paramId, BSDFData bsdfdata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_AXF_BSDFDATA_NORMAL_WS:
            result = bsdfdata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_NORMAL_VIEW_SPACE:
            result = bsdfdata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_TANGENT_WS:
            result = bsdfdata.tangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_BI_TANGENT_WS:
            result = bsdfdata.biTangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_DIFFUSE_COLOR:
            result = bsdfdata.diffuseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_SPECULAR_COLOR:
            result = bsdfdata.specularColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_FRESNEL_F0:
            result = bsdfdata.fresnelF0;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_ROUGHNESS:
            result = float3(bsdfdata.roughness, 0.0);
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_HEIGHT_MM:
            result = bsdfdata.height_mm.xxx;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_ANISOTROPY_ANGLE:
            result = bsdfdata.anisotropyAngle.xxx;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_FLAKES_UV:
            result = float3(bsdfdata.flakesUV, 0.0);
            break;
        case DEBUGVIEW_AXF_BSDFDATA_FLAKES_MIP:
            result = bsdfdata.flakesMipLevel.xxx / 9.0;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_CLEAR_COAT_COLOR:
            result = 0.5 * bsdfdata.clearCoatColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_CLEAR_COAT_NORMAL_WS:
            result = bsdfdata.clearCoatNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_CLEAR_COAT_IOR:
            result = bsdfdata.clearCoatIOR.xxx / 3.0;
            needLinearToSRGB = true;
            break;
    }
}


#endif
