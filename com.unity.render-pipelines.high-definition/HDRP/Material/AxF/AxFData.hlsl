//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------
#include "../MaterialUtilities.hlsl"

//#define APPLY_MANUAL_GAMMA  1     // Users have to tag sRGB textures themselves!

float3  ReadsRGBColor( float3 _gammaColor ) {
    #if APPLY_MANUAL_GAMMA
        return Gamma22ToLinear _gammaColor );
    #else
        return _gammaColor; // Taken care of by the hardware via "_sRGB" textures
    #endif
}

void GetSurfaceAndBuiltinData( FragInputs _input, float3 _viewWS, inout PositionInputs _posInput, out SurfaceData _surfaceData, out BuiltinData _builtinData ) {
    ApplyDoubleSidedFlipOrMirror( _input ); // Apply double sided flip on the vertex normal

    // NEWLITTODO: 
    // For now, just use the interpolated vertex normal. This has been normalized in the initial fragment interpolators unpacking.
    // Eventually, we want to share all the LitData LayerTexCoord (and surface gradient frame + uv, planar, triplanar, etc.) logic, also
    // spread in LitDataIndividualLayer and LitDataMeshModification.
    _surfaceData.normalWS = _input.worldToTangent[2].xyz;
    
    float2  UV0 = TRANSFORM_TEX( _input.texCoord0, _BaseColorMap );


UV0 *= float2( _materialSizeU_mm, _materialSizeV_mm );


    /////////////////////////////////////////////////////////////////////////////////////////////
    #ifdef _AXF_BRDF_TYPE_SVBRDF

        _surfaceData.diffuseColor = ReadsRGBColor( SAMPLE_TEXTURE2D( _SVBRDF_DiffuseColorMap_sRGB, sampler_SVBRDF_DiffuseColorMap_sRGB, UV0 ).xyz ) * _BaseColor.xyz;
        _surfaceData.specularColor = ReadsRGBColor( SAMPLE_TEXTURE2D( _SVBRDF_SpecularColorMap_sRGB, sampler_SVBRDF_SpecularColorMap_sRGB, UV0 ).xyz );
        _surfaceData.specularLobe = _SVBRDF_SpecularLobeMap_Scale * SAMPLE_TEXTURE2D( _SVBRDF_SpecularLobeMap, sampler_SVBRDF_SpecularLobeMap, UV0 ).xy;




// Check influence of anisotropy
//_surfaceData.specularLobe.y = lerp( _surfaceData.specularLobe.x, _surfaceData.specularLobe.y, saturate(_DEBUG_anisotropicRoughessX) );




        _surfaceData.fresnelF0 = ReadsRGBColor( SAMPLE_TEXTURE2D( _SVBRDF_FresnelMap_sRGB, sampler_SVBRDF_FresnelMap_sRGB, UV0 ).x );
        _surfaceData.height_mm = SAMPLE_TEXTURE2D( _SVBRDF_HeightMap, sampler_SVBRDF_HeightMap, UV0 ).x * _SVBRDF_heightMapMax_mm;
        _surfaceData.anisotropyAngle = PI * (2.0 * SAMPLE_TEXTURE2D( _SVBRDF_AnisotropicRotationAngleMap, sampler_SVBRDF_AnisotropicRotationAngleMap, UV0 ).x - 1.0);
        _surfaceData.clearCoatColor = ReadsRGBColor( SAMPLE_TEXTURE2D( _SVBRDF_ClearCoatColorMap_sRGB, sampler_SVBRDF_ClearCoatColorMap_sRGB, UV0 ).xyz );

        float  clearCoatF0 = ReadsRGBColor( SAMPLE_TEXTURE2D( _SVBRDF_ClearCoatIORMap_sRGB, sampler_SVBRDF_ClearCoatIORMap_sRGB, UV0 ).x ).x;
        float  sqrtF0 = sqrt( clearCoatF0 );
        _surfaceData.clearCoatIOR = max( 1.0, (1.0 + sqrtF0) / (1.00001 - sqrtF0) );    // We make sure it's working for F0=1

        // TBN
        GetNormalWS( _input, _viewWS, 2.0 * SAMPLE_TEXTURE2D( _SVBRDF_NormalMap, sampler_SVBRDF_NormalMap, UV0 ).xyz - 1.0, _surfaceData.normalWS );
        GetNormalWS( _input, _viewWS, 2.0 * SAMPLE_TEXTURE2D( _SVBRDF_ClearCoatNormalMap, sampler_SVBRDF_ClearCoatNormalMap, UV0 ).xyz - 1.0, _surfaceData.clearCoatNormalWS );

        float alpha = SAMPLE_TEXTURE2D( _SVBRDF_OpacityMap, sampler_SVBRDF_OpacityMap, UV0 ).x * _BaseColor.w;




// Hardcoded values for debug purpose
//_surfaceData.normalWS = _input.worldToTangent[2].xyz;
//_surfaceData.fresnelF0 = 0.04;
//_surfaceData.diffuseColor = pow( float3( 48, 54, 60 ) / 255.0, 2.2 );
//_surfaceData.specularColor = 1.0;
//_surfaceData.specularLobe = PerceptualSmoothnessToRoughness( 0.785 );



// Useless for SVBRDF
_surfaceData.flakesUV = _input.texCoord0;
_surfaceData.flakesMipLevel = 0.0;


    /////////////////////////////////////////////////////////////////////////////////////////////
    #elif defined(_AXF_BRDF_TYPE_CAR_PAINT)

        _surfaceData.diffuseColor = _CarPaint_CT_diffuse * _BaseColor.xyz;
        _surfaceData.clearCoatIOR = max( 1.001, _CarPaint_IOR );    // Can't be exactly 1 otherwise the precise fresnel divides by 0!

        GetNormalWS( _input, _viewWS, 2.0 * SAMPLE_TEXTURE2D( _SVBRDF_ClearCoatNormalMap, sampler_SVBRDF_ClearCoatNormalMap, UV0 ).xyz - 1.0, _surfaceData.clearCoatNormalWS );
//        _surfaceData.normalWS = _surfaceData.clearCoatNormalWS; // Use clear coat normal map as global surface normal map


        // Create mirrored UVs to hide flakes tiling
        _surfaceData.flakesUV = _CarPaint_FlakesTiling * UV0;

        _surfaceData.flakesMipLevel = _CarPaint_BTFFlakesMap_sRGB.CalculateLevelOfDetail( sampler_CarPaint_BTFFlakesMap_sRGB, _surfaceData.flakesUV );
//_surfaceData.flakesMipLevel = _DEBUG_clearCoatIOR;      // DEBUG!!!

        if ( (int(_surfaceData.flakesUV.y) & 1) == 0 )
            _surfaceData.flakesUV.x += 0.5;
        else if ( (uint(1000.0 + _surfaceData.flakesUV.x) % 3) == 0 )
            _surfaceData.flakesUV.y = 1.0 - _surfaceData.flakesUV.y;
        else
            _surfaceData.flakesUV.x = 1.0 - _surfaceData.flakesUV.x;


// Useless for car paint BSDF
_surfaceData.specularColor = 0;
_surfaceData.specularLobe = 0;
_surfaceData.fresnelF0 = 0;
_surfaceData.height_mm = 0;
_surfaceData.anisotropyAngle = 0;
_surfaceData.clearCoatColor = 0;

        float   alpha = 1.0;

    #endif
    /////////////////////////////////////////////////////////////////////////////////////////////


    // Finalize tangent space
//  _surfaceData.tangentWS = _input.worldToTangent[0];
//  _surfaceData.biTangentWS = _input.worldToTangent[1];
    _surfaceData.tangentWS = Orthonormalize( _input.worldToTangent[0], _surfaceData.normalWS );
    _surfaceData.biTangentWS = Orthonormalize( _input.worldToTangent[1], _surfaceData.normalWS );


    #ifdef _ALPHATEST_ON
        //NEWLITTODO: Once we include those passes in the main AxF.shader, add handling of CUTOFF_TRANSPARENT_DEPTH_PREPASS and _POSTPASS
        // and the related properties (in the .shader) and uniforms (in the AxFProperties file) _AlphaCutoffPrepass, _AlphaCutoffPostpass
        DoAlphaTest( alpha, _AlphaCutoff );
    #endif

    #if defined(DEBUG_DISPLAY)
        if ( _DebugMipMapMode != DEBUGMIPMAPMODE_NONE ) {
            _surfaceData.diffuseColor = GetTextureDataDebug( _DebugMipMapMode, UV0, _BaseColorMap, _BaseColorMap_TexelSize, _BaseColorMap_MipInfo, _surfaceData.diffuseColor );
        }
    #endif


    ///////////////////////////////////////////////////////////////////////////////////////////////
    // Builtin Data
    // (Used by legacy pipeline + GI)

    // NEWLITTODO: for all BuiltinData, might need to just refactor and use a comon function like that contained in LitBuiltinData.hlsl

    _builtinData.opacity = alpha;

    _builtinData.bakeDiffuseLighting = 0;

    // Emissive Intensity is only use here, but is part of BuiltinData to enforce UI parameters as we want the users to fill one color and one intensity
//	_builtinData.emissiveIntensity = _EmissiveIntensity;
//	_builtinData.emissiveColor = _EmissiveColor * _builtinData.emissiveIntensity * lerp(float3(1.0, 1.0, 1.0), _surfaceData.diffuseColor.rgb, _AlbedoAffectEmissive);
//    _builtinData.emissiveIntensity = 1e-6;
    _builtinData.emissiveColor = 0;


//	#ifdef _EMISSIVE_COLOR_MAP
//		_builtinData.emissiveColor *= SAMPLE_TEXTURE2D(_EmissiveColorMap, sampler_EmissiveColorMap, TRANSFORM_TEX(input.texCoord0, _EmissiveColorMap)).rgb;
//	#endif

    _builtinData.velocity = float2(0.0, 0.0);

    
    //NEWLITTODO: shader feature SHADOWS_SHADOWMASK not there yet. 
    _builtinData.shadowMask0 = 0.0;
    _builtinData.shadowMask1 = 0.0;
    _builtinData.shadowMask2 = 0.0;
    _builtinData.shadowMask3 = 0.0;

#if 0//(SHADERPASS == SHADERPASS_DISTORTION) || defined(DEBUG_DISPLAY)
    float3 distortion = SAMPLE_TEXTURE2D(_DistortionVectorMap, sampler_DistortionVectorMap, _input.texCoord0).rgb;
    distortion.rg = distortion.rg * _DistortionVectorScale.xx + _DistortionVectorBias.xx;
    _builtinData.distortion = distortion.rg * _DistortionScale;
    _builtinData.distortionBlur = clamp(distortion.b * _DistortionBlurScale, 0.0, 1.0) * (_DistortionBlurRemapMax - _DistortionBlurRemapMin) + _DistortionBlurRemapMin;
#else
    _builtinData.distortion = float2(0.0, 0.0);
    _builtinData.distortionBlur = 0.0;
#endif

    _builtinData.depthOffset = 0.0;
}
