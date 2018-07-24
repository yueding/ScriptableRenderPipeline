//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------
#include "../MaterialUtilities.hlsl"

//#define APPLY_MANUAL_GAMMA  1     // Users have to tag sRGB textures themselves!

float3 ReadsRGBColor(float3 gammaColor)
{
    #if APPLY_MANUAL_GAMMA
        return Gamma22ToLinear(gammaColor);
    #else
        return gammaColor; // Taken care of by the hardware via "_sRGB" textures
    #endif
}

void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs _posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
    ApplyDoubleSidedFlipOrMirror(input); // Apply double sided flip on the vertex normal

    // NEWLITTODO: 
    // For now, just use the interpolated vertex normal. This has been normalized in the initial fragment interpolators unpacking.
    // Eventually, we want to share all the LitData LayerTexCoord (and surface gradient frame + uv, planar, triplanar, etc.) logic, also
    // spread in LitDataIndividualLayer and LitDataMeshModification.
    surfaceData.normalWS = input.worldToTangent[2].xyz;
    
    float2 UV0 = TRANSFORM_TEX(input.texCoord0, _BaseColorMap);

    UV0 *= float2(_materialSizeU_mm, _materialSizeV_mm);

    //-----------------------------------------------------------------------------
    // _AXF_BRDF_TYPE_SVBRDF
    //-----------------------------------------------------------------------------

#ifdef _AXF_BRDF_TYPE_SVBRDF

    surfaceData.diffuseColor = ReadsRGBColor( SAMPLE_TEXTURE2D( _SVBRDF_DiffuseColorMap_sRGB, sampler_SVBRDF_DiffuseColorMap_sRGB, UV0 ).xyz ) * _BaseColor.xyz;
    surfaceData.specularColor = ReadsRGBColor( SAMPLE_TEXTURE2D( _SVBRDF_SpecularColorMap_sRGB, sampler_SVBRDF_SpecularColorMap_sRGB, UV0 ).xyz );
    surfaceData.specularLobe = _SVBRDF_SpecularLobeMap_Scale * SAMPLE_TEXTURE2D( _SVBRDF_SpecularLobeMap, sampler_SVBRDF_SpecularLobeMap, UV0 ).xy;

    // Check influence of anisotropy
    //surfaceData.specularLobe.y = lerp( surfaceData.specularLobe.x, surfaceData.specularLobe.y, saturate(_DEBUG_anisotropicRoughessX) );

    surfaceData.fresnelF0 = ReadsRGBColor( SAMPLE_TEXTURE2D( _SVBRDF_FresnelMap_sRGB, sampler_SVBRDF_FresnelMap_sRGB, UV0 ).x );
    surfaceData.height_mm = SAMPLE_TEXTURE2D( _SVBRDF_HeightMap, sampler_SVBRDF_HeightMap, UV0 ).x * _SVBRDF_heightMapMax_mm;
    surfaceData.anisotropyAngle = PI * (2.0 * SAMPLE_TEXTURE2D( _SVBRDF_AnisotropicRotationAngleMap, sampler_SVBRDF_AnisotropicRotationAngleMap, UV0 ).x - 1.0);
    surfaceData.clearCoatColor = ReadsRGBColor( SAMPLE_TEXTURE2D( _SVBRDF_ClearCoatColorMap_sRGB, sampler_SVBRDF_ClearCoatColorMap_sRGB, UV0 ).xyz );

    float  clearCoatF0 = ReadsRGBColor( SAMPLE_TEXTURE2D( _SVBRDF_ClearCoatIORMap_sRGB, sampler_SVBRDF_ClearCoatIORMap_sRGB, UV0 ).x ).x;
    float  sqrtF0 = sqrt( clearCoatF0 );
    surfaceData.clearCoatIOR = max( 1.0, (1.0 + sqrtF0) / (1.00001 - sqrtF0) );    // We make sure it's working for F0=1

    // TBN
    GetNormalWS( input, V, 2.0 * SAMPLE_TEXTURE2D( _SVBRDF_NormalMap, sampler_SVBRDF_NormalMap, UV0 ).xyz - 1.0, surfaceData.normalWS );
    GetNormalWS( input, V, 2.0 * SAMPLE_TEXTURE2D( _SVBRDF_ClearCoatNormalMap, sampler_SVBRDF_ClearCoatNormalMap, UV0 ).xyz - 1.0, surfaceData.clearCoatNormalWS );

    float alpha = SAMPLE_TEXTURE2D( _SVBRDF_OpacityMap, sampler_SVBRDF_OpacityMap, UV0 ).x * _BaseColor.w;

    // Hardcoded values for debug purpose
    //surfaceData.normalWS = input.worldToTangent[2].xyz;
    //surfaceData.fresnelF0 = 0.04;
    //surfaceData.diffuseColor = pow( float3( 48, 54, 60 ) / 255.0, 2.2 );
    //surfaceData.specularColor = 1.0;
    //surfaceData.specularLobe = PerceptualSmoothnessToRoughness( 0.785 );

    // Useless for SVBRDF
    surfaceData.flakesUV = input.texCoord0;
    surfaceData.flakesMipLevel = 0.0;

    //-----------------------------------------------------------------------------
    // _AXF_BRDF_TYPE_CAR_PAINT
    //-----------------------------------------------------------------------------

#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)

    surfaceData.diffuseColor = _CarPaint_CT_diffuse * _BaseColor.xyz;
    surfaceData.clearCoatIOR = max(1.001, _CarPaint_IOR);    // Can't be exactly 1 otherwise the precise fresnel divides by 0!

    GetNormalWS(input, V, 2.0 * SAMPLE_TEXTURE2D(_SVBRDF_ClearCoatNormalMap, sampler_SVBRDF_ClearCoatNormalMap, UV0 ).xyz - 1.0, surfaceData.clearCoatNormalWS);
    // surfaceData.normalWS = surfaceData.clearCoatNormalWS; // Use clear coat normal map as global surface normal map


    // Create mirrored UVs to hide flakes tiling
    surfaceData.flakesUV = _CarPaint_FlakesTiling * UV0;

    surfaceData.flakesMipLevel = _CarPaint_BTFFlakesMap_sRGB.CalculateLevelOfDetail(sampler_CarPaint_BTFFlakesMap_sRGB, surfaceData.flakesUV);
    //surfaceData.flakesMipLevel = _DEBUG_clearCoatIOR;      // DEBUG!!!

    if ((int(surfaceData.flakesUV.y) & 1) == 0)
        surfaceData.flakesUV.x += 0.5;
    else if ((uint(1000.0 + surfaceData.flakesUV.x) % 3) == 0)
        surfaceData.flakesUV.y = 1.0 - surfaceData.flakesUV.y;
    else
        surfaceData.flakesUV.x = 1.0 - surfaceData.flakesUV.x;

    // Useless for car paint BSDF
    surfaceData.specularColor = 0;
    surfaceData.specularLobe = 0;
    surfaceData.fresnelF0 = 0;
    surfaceData.height_mm = 0;
    surfaceData.anisotropyAngle = 0;
    surfaceData.clearCoatColor = 0;

    float   alpha = 1.0;

#endif

    // Finalize tangent space
    // surfaceData.tangentWS = input.worldToTangent[0];
    // surfaceData.biTangentWS = input.worldToTangent[1];
    surfaceData.tangentWS = Orthonormalize( input.worldToTangent[0], surfaceData.normalWS );
    surfaceData.biTangentWS = Orthonormalize( input.worldToTangent[1], surfaceData.normalWS );

    #ifdef _ALPHATEST_ON
        //NEWLITTODO: Once we include those passes in the main AxF.shader, add handling of CUTOFF_TRANSPARENT_DEPTH_PREPASS and _POSTPASS
        // and the related properties (in the .shader) and uniforms (in the AxFProperties file) _AlphaCutoffPrepass, _AlphaCutoffPostpass
        DoAlphaTest( alpha, _AlphaCutoff );
    #endif

    #if defined(DEBUG_DISPLAY)
        if ( _DebugMipMapMode != DEBUGMIPMAPMODE_NONE ) {
            surfaceData.diffuseColor = GetTextureDataDebug( _DebugMipMapMode, UV0, _BaseColorMap, _BaseColorMap_TexelSize, _BaseColorMap_MipInfo, surfaceData.diffuseColor );
        }
    #endif


    // -------------------------------------------------------------
    // Builtin Data:
    // -------------------------------------------------------------

    builtinData.opacity = alpha;

    builtinData.bakeDiffuseLighting = SampleBakedGI(input.positionRWS, surfaceData.normalWS, input.texCoord1, input.texCoord2);

    builtinData.emissiveColor = 0.0;

    builtinData.velocity = float2(0.0, 0.0);

#ifdef SHADOWS_SHADOWMASK
    float4 shadowMask = SampleShadowMask(input.positionRWS, input.texCoord1);
    builtinData.shadowMask0 = shadowMask.x;
    builtinData.shadowMask1 = shadowMask.y;
    builtinData.shadowMask2 = shadowMask.z;
    builtinData.shadowMask3 = shadowMask.w;
#else
    builtinData.shadowMask0 = 0.0;
    builtinData.shadowMask1 = 0.0;
    builtinData.shadowMask2 = 0.0;
    builtinData.shadowMask3 = 0.0;
#endif

    builtinData.distortion = float2(0.0, 0.0);
    builtinData.distortionBlur = 0.0;

    // Use uniform directly - The float need to be cast to uint (as unity don't support to set a uint as uniform)
    builtinData.renderingLayers = _EnableLightLayers ? asuint(unity_RenderingLayer.x) : DEFAULT_LIGHT_LAYERS;

    builtinData.depthOffset = 0.0;
}
