//-----------------------------------------------------------------------------
// SurfaceData and BSDFData
//-----------------------------------------------------------------------------
// SurfaceData is defined in AxF.cs which generates AxF.cs.hlsl
#include "AxF.cs.hlsl"
//#include "../SubsurfaceScattering/SubsurfaceScattering.hlsl"
//#include "CoreRP/ShaderLibrary/VolumeRendering.hlsl"

// Declare the BSDF specific FGD property and its fetching function
//#include "../PreIntegratedFGD/PreIntegratedFGD.hlsl"
#include "./Resources/PreIntegratedFGD.hlsl"

//NEWLITTODO : wireup CBUFFERs for ambientocclusion, and other uniforms and samplers used:
//
// We need this for AO, Depth/Color pyramids, LTC lights data, FGD pre-integrated data.
//
// Also add options at the top of this file, see Lit.hlsl.



//-----------------------------------------------------------------------------
// Debug method (use to display values)
//-----------------------------------------------------------------------------
void GetSurfaceDataDebug( uint paramId, SurfaceData surfaceData, inout float3 _result, inout bool _needLinearToSRGB ) {
    GetGeneratedSurfaceDataDebug( paramId, surfaceData, _result, _needLinearToSRGB );

    // Overide debug value output to be more readable
    switch ( paramId ) {
        case DEBUGVIEW_AXF_SURFACEDATA_NORMAL_VIEW_SPACE:
            // Convert to view space
            _result = TransformWorldToViewDir(surfaceData.normalWS) * 0.5 + 0.5;
            break;
    }
}

void GetBSDFDataDebug( uint paramId, BSDFData _BSDFData, inout float3 _result, inout bool _needLinearToSRGB ) {
    GetGeneratedBSDFDataDebug(paramId, _BSDFData, _result, _needLinearToSRGB);

    // Overide debug value output to be more readable
    switch ( paramId ) {
        case DEBUGVIEW_AXF_BSDFDATA_NORMAL_VIEW_SPACE:
            // Convert to view space
            _result = TransformWorldToViewDir(_BSDFData.normalWS) * 0.5 + 0.5;
            break;
    }
}



// This function is use to help with debugging and must be implemented by any lit material
// Implementer must take into account what are the current override component and
// adjust SurfaceData properties accordingdly
void ApplyDebugToSurfaceData( float3x3 _worldToTangent, inout SurfaceData surfaceData ) {
    #ifdef DEBUG_DISPLAY
        // NOTE: THe _Debug* uniforms come from /HDRP/Debug/DebugDisplay.hlsl

        // Override value if requested by user this can be use also in case of debug lighting mode like diffuse only
        bool overrideAlbedo = _DebugLightingAlbedo.x != 0.0;
        bool overrideSmoothness = _DebugLightingSmoothness.x != 0.0;
        bool overrideNormal = _DebugLightingNormal.x != 0.0;

        if ( overrideAlbedo ) {
	        surfaceData.diffuseColor = _DebugLightingAlbedo.yzw;
        }

        if ( overrideSmoothness ) {
            //NEWLITTODO
            float overrideSmoothnessValue = _DebugLightingSmoothness.y;
//            surfaceData.perceptualSmoothness = overrideSmoothnessValue;
            surfaceData.specularLobe = overrideSmoothnessValue;
        }

        if ( overrideNormal ) {
	        surfaceData.normalWS = _worldToTangent[2];
        }
    #endif
}

// This function is similar to ApplyDebugToSurfaceData but for BSDFData
//
// NOTE:
//
// This will be available and used in ShaderPassForward.hlsl since in AxF.shader,
// just before including the core code of the pass (ShaderPassForward.hlsl) we include
// Material.hlsl (or Lighting.hlsl which includes it) which in turn includes us,
// AxF.shader, via the #if defined(UNITY_MATERIAL_*) glue mechanism.
//
void ApplyDebugToBSDFData( inout BSDFData _BSDFData ) {
    #ifdef DEBUG_DISPLAY
        // Override value if requested by user
        // this can be use also in case of debug lighting mode like specular only

        //NEWLITTODO
        //bool overrideSpecularColor = _DebugLightingSpecularColor.x != 0.0;

        //if (overrideSpecularColor)
        //{
        //   float3 overrideSpecularColor = _DebugLightingSpecularColor.yzw;
        //    _BSDFData.fresnel0 = overrideSpecularColor;
        //}
    #endif


// DEBUG Anisotropy
//_BSDFData.anisotropyAngle = _DEBUG_anisotropyAngle;
//_BSDFData.anisotropyAngle += _DEBUG_anisotropyAngle;
//_BSDFData.roughness = _SVBRDF_SpecularLobeMap_Scale * float2( _DEBUG_anisotropicRoughessX, _DEBUG_anisotropicRoughessY );

// DEBUG Clear coat
//_BSDFData.clearCoatIOR = max( 1.001, _DEBUG_clearCoatIOR );


}

//----------------------------------------------------------------------
// From Walter 2007 eq. 40
// Expects _incoming pointing AWAY from the surface
// eta = IOR_above / IOR_below
//
float3	Refract( float3 _incoming, float3 _normal, float _eta ) {
	float	c = dot( _incoming, _normal );
	float	b = 1.0 + _eta * (c*c - 1.0);
	if ( b >= 0.0 ) {
		float	k = _eta * c - sign(c) * sqrt( b );
		float3	R = k * _normal - _eta * _incoming;
		return normalize( R );
	} else {
		return -_incoming;	// Total internal reflection
	}
}


//----------------------------------------------------------------------
// Cook-Torrance functions as provided by X-Rite in the "AxF-Decoding-SDK-1.5.1/doc/html/page2.html#carpaint_BrightnessBRDF" document from the SDK
//static const float  MIN_ROUGHNESS = 0.01;

float CT_D( float N_H, float m ) {
    float cosb_sqr = N_H*N_H;
    float m_sqr = m*m;
    float e = (cosb_sqr - 1.0) / (cosb_sqr*m_sqr);  // -tan(a)² / m²
    return exp(e) / (m_sqr*cosb_sqr*cosb_sqr);  // exp( -tan(a)² / m² ) / (m² * cos(a)^4)
}

// Classical Schlick approximation for Fresnel
float CT_F( float H_V, float F0 ) {
    float f_1_sub_cos = 1.0 - H_V;
    float f_1_sub_cos_sqr = f_1_sub_cos*f_1_sub_cos;
    float f_1_sub_cos_fifth= f_1_sub_cos_sqr*f_1_sub_cos_sqr*f_1_sub_cos;
    return F0 + (1.0 -F0) * f_1_sub_cos_fifth;
}

float CT_G( float N_H, float N_V, float N_L, float H_V ) {
    return min( 1.0, 2.0 * N_H * min( N_V, N_L ) / H_V );
}

float3  MultiLobesCookTorrance( float NdotL, float NdotV, float NdotH, float VdotH ) {
    // Ensure numerical stability
    if ( NdotV < 0.00174532836589830883577820272085 && NdotL < 0.00174532836589830883577820272085 ) //sin(0.1°)
        return 0.0;

    float   specularIntensity = 0.0;
    for ( uint lobeIndex=0; lobeIndex < _CarPaint_lobesCount; lobeIndex++ ) {
        float   F0 = _CarPaint_CT_F0s[lobeIndex];
        float   coeff = _CarPaint_CT_coeffs[lobeIndex];
        float   spread = _CarPaint_CT_spreads[lobeIndex];

//spread = max( MIN_ROUGHNESS, spread );

        specularIntensity += coeff * CT_D( NdotH, spread ) * CT_F( VdotH, F0 );
    }
    specularIntensity *= CT_G( NdotH, NdotV, NdotL, VdotH )  // Shadowing/Masking term
                       / (PI * max( 1e-3, NdotV * NdotL ));

    return specularIntensity;
}


//----------------------------------------------------------------------
// Simple Oren-Nayar implementation
//  _normal, unit surface normal
//  _light, unit vector pointing toward the light
//  _view, unit vector pointing toward the view
//  _roughness, Oren-Nayar roughness parameter in [0,PI/2]
//
float   OrenNayar( in float3 _normal, in float3 _view, in float3 _light, in float _roughness ) {
    float3  n = _normal;
    float3  l = _light;
    float3  v = _view;

    float   LdotN = dot( l, n );
    float   VdotN = dot( v, n );

    float   gamma = dot( v - n * VdotN, l - n * LdotN )
                    / (sqrt( saturate( 1.0 - VdotN*VdotN ) ) * sqrt( saturate( 1.0 - LdotN*LdotN ) ));

    float rough_sq = _roughness * _roughness;
//    float A = 1.0 - 0.5 * (rough_sq / (rough_sq + 0.33));   // You can replace 0.33 by 0.57 to simulate the missing inter-reflection term, as specified in footnote of page 22 of the 1992 paper
    float A = 1.0 - 0.5 * (rough_sq / (rough_sq + 0.57));   // You can replace 0.33 by 0.57 to simulate the missing inter-reflection term, as specified in footnote of page 22 of the 1992 paper
    float B = 0.45 * (rough_sq / (rough_sq + 0.09));

    // Original formulation
//  float angle_vn = acos( VdotN );
//  float angle_ln = acos( LdotN );
//  float alpha = max( angle_vn, angle_ln );
//  float beta  = min( angle_vn, angle_ln );
//  float C = sin(alpha) * tan(beta);

    // Optimized formulation (without tangents, arccos or sines)
    float2  cos_alpha_beta = VdotN < LdotN ? float2( VdotN, LdotN ) : float2( LdotN, VdotN );   // Here we reverse the min/max since cos() is a monotonically decreasing function
    float2  sin_alpha_beta = sqrt( saturate( 1.0 - cos_alpha_beta*cos_alpha_beta ) );           // Saturate to avoid NaN if ever cos_alpha > 1 (it happens with floating-point precision)
    float   C = sin_alpha_beta.x * sin_alpha_beta.y / (1e-6 + cos_alpha_beta.y);

    return A + B * max( 0.0, gamma ) * C;
}


//----------------------------------------------------------------------
float   G_smith( float _NdotV, float _roughness ) {
    float   a2 = Sq( _roughness );
    return 2 * _NdotV / (_NdotV + sqrt( a2 + (1 - a2) * Sq(_NdotV) ));
}


//-----------------------------------------------------------------------------
// conversion function for forward
//-----------------------------------------------------------------------------

BSDFData ConvertSurfaceDataToBSDFData( uint2 positionSS, SurfaceData surfaceData ) {
	BSDFData    data;
//	ZERO_INITIALIZE(BSDFData, data);

	data.normalWS = surfaceData.normalWS;
	data.tangentWS = surfaceData.tangentWS;
	data.biTangentWS = surfaceData.biTangentWS;

    ////////////////////////////////////////////////////////////////////////////////////////
    #ifdef _AXF_BRDF_TYPE_SVBRDF
	    data.diffuseColor = surfaceData.diffuseColor;
        data.specularColor = surfaceData.specularColor;
        data.fresnelF0 = surfaceData.fresnelF0;
        data.roughness = surfaceData.specularLobe;
        data.height_mm = surfaceData.height_mm;
        data.anisotropyAngle = surfaceData.anisotropyAngle;
        data.clearCoatColor = surfaceData.clearCoatColor;
        data.clearCoatNormalWS = surfaceData.clearCoatNormalWS;
        data.clearCoatIOR = surfaceData.clearCoatIOR;

data.flakesUV = 0.0;
data.flakesMipLevel = 0.0;

    ////////////////////////////////////////////////////////////////////////////////////////
    #elif defined(_AXF_BRDF_TYPE_CAR_PAINT)
	    data.diffuseColor = surfaceData.diffuseColor;
	    data.flakesUV = surfaceData.flakesUV;
        data.flakesMipLevel = surfaceData.flakesMipLevel;
        data.clearCoatColor = 1.0;  // Not provided, assume white...
        data.clearCoatIOR = surfaceData.clearCoatIOR;
        data.clearCoatNormalWS = surfaceData.clearCoatNormalWS;

// Although not used, needs to be initialized... :'(
data.specularColor = 0;
data.fresnelF0 = 0;
data.roughness = 0;
data.height_mm = 0;
data.anisotropyAngle = 0;
    #endif

	ApplyDebugToBSDFData(data);
	return data;
}

//-----------------------------------------------------------------------------
// PreLightData
//
// Make sure we respect naming conventions to reuse ShaderPassForward as is,
// ie struct (even if opaque to the ShaderPassForward) name is PreLightData,
// GetPreLightData prototype.
//-----------------------------------------------------------------------------

// Precomputed lighting data to send to the various lighting functions
struct PreLightData {
	float   NdotV;                  // Could be negative due to normal mapping, use ClampNdotV()
    float3  IOR;

    #ifdef _AXF_BRDF_TYPE_SVBRDF
        // Anisotropy
        float2  anisoX;
        float2  anisoY;
    #endif

    // Clear coat
    float   clearCoatF0;
    float3  clearCoatViewWS;        // World-space view vector refracted by clear coat

    // IBL
    float   IBLRoughness;
    #ifdef _AXF_BRDF_TYPE_SVBRDF
	    float   diffuseFGD;
    #endif
    float3  IBLDominantDirectionWS;   // Dominant specular direction, used for IBL in EvaluateBSDF_Env()
	float3  specularFGD;
};

PreLightData    GetPreLightData( float3 _viewWS, PositionInputs _posInput, inout BSDFData _BSDFData ) {
	PreLightData    _preLightData;
//	ZERO_INITIALIZE( PreLightData, _preLightData );

	float3  normalWS = _BSDFData.normalWS;
	_preLightData.NdotV = dot( normalWS, _viewWS );
    _preLightData.IOR = GetIorN( _BSDFData.fresnelF0, 1.0 );

	float   NdotV = ClampNdotV( _preLightData.NdotV );

    #ifdef _AXF_BRDF_TYPE_SVBRDF
        // Handle anisotropy
        float2  anisoDir = float2( 1, 0 );
        if ( _flags & 1 ) {
//            sincos( _BSDFData.anisotropyAngle, anisoDir.y, anisoDir.x );
            sincos( _BSDFData.anisotropyAngle, anisoDir.x, anisoDir.y );    // Eyeballed the fact that an angle of 0 is actually 90° from tangent axis!
        }

        _preLightData.anisoX = anisoDir;
        _preLightData.anisoY = float2( -anisoDir.y, anisoDir.x );
    #endif

    // Handle clear coat
//  _preLightData.clearCoatF0 = IorToFresnel0( _BSDFData.clearCoatIOR );
    _preLightData.clearCoatF0 = Sq( (_BSDFData.clearCoatIOR - 1) / (_BSDFData.clearCoatIOR + 1) );
    _preLightData.clearCoatViewWS = -Refract( _viewWS, _BSDFData.clearCoatNormalWS, _BSDFData.clearCoatIOR );    // This is independent of lighting

    #ifdef _AXF_BRDF_TYPE_SVBRDF
        // Handle IBL +  multiscattering
// @TODO FGD !!
        _preLightData.IBLRoughness = 0.5 * (_BSDFData.roughness.x + _BSDFData.roughness.y);    // @TODO => Anisotropic IBL?
        float specularReflectivity;
        GetPreIntegratedFGDWard( NdotV, _preLightData.IBLRoughness, _BSDFData.fresnelF0, _preLightData.specularFGD, _preLightData.diffuseFGD, specularReflectivity );
        #ifdef LIT_DIFFUSE_LAMBERT_BRDF
            _preLightData.diffuseFGD = 1.0;
        #endif

    #elif defined(_AXF_BRDF_TYPE_CAR_PAINT)
        // Computes weighted average of roughness values
        // Used to sample IBL with a single roughness but useless if we sample as many times as there are lobes?? (*gasp*)
        float2  sumRoughness = 0.0;
        for ( uint lobeIndex=0; lobeIndex < _CarPaint_lobesCount; lobeIndex++ ) {
            float   coeff = _CarPaint_CT_coeffs[lobeIndex];
            float   spread = _CarPaint_CT_spreads[lobeIndex];

//spread = max( MIN_ROUGHNESS, spread );

            sumRoughness += coeff * float2( spread, 1 );
        }
        _preLightData.IBLRoughness = sumRoughness.x / sumRoughness.y;  // Not used if sampling the environment for each Cook-Torrance lobe
    #endif

    _preLightData.IBLDominantDirectionWS = reflect( -_viewWS, normalWS );

	return _preLightData;
}


//-----------------------------------------------------------------------------
// bake lighting function
//-----------------------------------------------------------------------------

//
// GetBakedDiffuseLighting will be called from ShaderPassForward.hlsl.
//
// GetBakedDiffuseLighting function compute the bake lighting + emissive color to be store in emissive buffer (Deferred case)
// In forward it must be add to the final contribution.
// This function require the 3 structure surfaceData, builtinData, _BSDFData because it may require both the engine side data, and data that will not be store inside the gbuffer.
float3  GetBakedDiffuseLighting( SurfaceData surfaceData, BuiltinData builtinData, BSDFData _BSDFData, PreLightData _preLightData ) {

    #ifdef DEBUG_DISPLAY
        if ( _DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER ) {
            // The lighting in SH or lightmap is assume to contain bounced light only (i.e no direct lighting), and is divide by PI (i.e Lambert is apply), so multiply by PI here to get back the illuminance
            return builtinData.bakeDiffuseLighting * PI;
        }
    #endif

	//NEWLITTODO
    #ifdef _AXF_BRDF_TYPE_SVBRDF
	    // Premultiply bake diffuse lighting information with DisneyDiffuse pre-integration
	    return builtinData.bakeDiffuseLighting * _preLightData.diffuseFGD * _BSDFData.diffuseColor + builtinData.emissiveColor;
    #else
	    return builtinData.bakeDiffuseLighting * _BSDFData.diffuseColor + builtinData.emissiveColor;
    #endif
}


//-----------------------------------------------------------------------------
// light transport functions
//-----------------------------------------------------------------------------
LightTransportData	GetLightTransportData( SurfaceData surfaceData, BuiltinData builtinData, BSDFData _BSDFData ) {
    LightTransportData lightTransportData;

    // diffuseColor for lightmapping should basically be diffuse color.
    // But rough metals (black diffuse) still scatter quite a lot of light around, so we want to take some of that into account too.

    //NEWLITTODO
    //float roughness = PerceptualRoughnessToRoughness(_BSDFData.perceptualRoughness);
    //lightTransportData.diffuseColor = _BSDFData.diffuseColor + _BSDFData.fresnel0 * roughness * 0.5 * surfaceData.metallic;
    lightTransportData.diffuseColor = _BSDFData.diffuseColor;
    lightTransportData.emissiveColor = builtinData.emissiveColor;

    return lightTransportData;
}

//-----------------------------------------------------------------------------
// LightLoop related function (Only include if required)
// HAS_LIGHTLOOP is define in Lighting.hlsl
//-----------------------------------------------------------------------------

#ifdef HAS_LIGHTLOOP

#ifndef _SURFACE_TYPE_TRANSPARENT
    // For /Lighting/LightEvaluation.hlsl:
    #define USE_DEFERRED_DIRECTIONAL_SHADOWS // Deferred shadows are always enabled for opaque objects
#endif

#include "../../Lighting/LightEvaluation.hlsl"
#include "../../Lighting/Reflection/VolumeProjection.hlsl"

//-----------------------------------------------------------------------------
// Lighting structure for light accumulation
//-----------------------------------------------------------------------------

// These structure allow to accumulate lighting accross the Lit material
// AggregateLighting is init to zero and transfer to EvaluateBSDF, but the LightLoop can't access its content.
//
// In fact, all structures here are opaque but used by LightLoop.hlsl.
// The Accumulate* functions are also used by LightLoop to accumulate the contributions of lights.
//
struct DirectLighting {
	float3  diffuse;
	float3  specular;
};

struct IndirectLighting {
	float3  specularReflected;
	float3  specularTransmitted;
};

struct AggregateLighting {
	DirectLighting      direct;
	IndirectLighting    indirect;
};

void AccumulateDirectLighting( DirectLighting src, inout AggregateLighting dst ) {
	dst.direct.diffuse += src.diffuse;
	dst.direct.specular += src.specular;
}

void AccumulateIndirectLighting( IndirectLighting src, inout AggregateLighting dst ) {
	dst.indirect.specularReflected += src.specularReflected;
	dst.indirect.specularTransmitted += src.specularTransmitted;
}

//-----------------------------------------------------------------------------
// BSDF share between directional light, punctual light and area light (reference)
//-----------------------------------------------------------------------------

float3  ComputeClearCoatExtinction( inout float3 _viewWS, inout float3 _lightWS, PreLightData _preLightData, BSDFData _BSDFData ) {
    // Compute input/output Fresnel attenuations
    float   LdotN = saturate( dot( _lightWS, _BSDFData.clearCoatNormalWS ) );
    float3  Fin = F_FresnelDieletric( _BSDFData.clearCoatIOR, LdotN );

    float   VdotN = saturate( dot( _viewWS, _BSDFData.clearCoatNormalWS ) );
    float3  Fout = F_FresnelDieletric( _BSDFData.clearCoatIOR, VdotN );

    // Apply optional refraction
    if ( _flags & 4U ) {
        _lightWS = -Refract( _lightWS, _BSDFData.clearCoatNormalWS, _BSDFData.clearCoatIOR );
        _viewWS = -Refract( _viewWS, _BSDFData.clearCoatNormalWS, _BSDFData.clearCoatIOR );
//        _viewWS = _preLightData.clearCoatViewWS;
    }

    return (1-Fin) * (1-Fout);
}


#ifdef _AXF_BRDF_TYPE_SVBRDF

float3  ComputeWard( float3 _H, float _LdotH, float _NdotL, float _NdotV, float3 _positionWS, PreLightData _preLightData, BSDFData _BSDFData ) {

    // Evaluate Fresnel term
    float3  F = 0.0;
    switch ( _SVBRDF_BRDFVariants & 3 ) {
        case 1: F = F_FresnelDieletric( _BSDFData.fresnelF0.y, _LdotH ); break;
        case 2: F = F_Schlick( _BSDFData.fresnelF0, _LdotH ); break;
    }

    // Evaluate normal distribution function
    float3  tsH = float3( dot( _H, _BSDFData.tangentWS ), dot( _H, _BSDFData.biTangentWS ), dot( _H, _BSDFData.normalWS ) );
    float2  rotH = (tsH.x * _preLightData.anisoX + tsH.y * _preLightData.anisoY) / tsH.z;
    float   N = exp( -Sq(rotH.x / _BSDFData.roughness.x) - Sq(rotH.y / _BSDFData.roughness.y) )
              / (PI * _BSDFData.roughness.x*_BSDFData.roughness.y);

    switch ( (_SVBRDF_BRDFVariants >> 2) & 3 ) {
        case 0: N /= 4.0 * Sq( _LdotH ) * Sq(Sq(tsH.z)); break; // Moroder
        case 1: N /= 4.0 * _NdotL; break;                       // Duer
        case 2: N /= 4.0 * sqrt( _NdotL ); break;               // Ward
    }

    return _BSDFData.specularColor * F * N;
}

float3  ComputeBlinnPhong( float3 _H, float _LdotH, float _NdotL, float _NdotV, float3 _positionWS, PreLightData _preLightData, BSDFData _BSDFData ) {
    float2  exponents = exp2( _BSDFData.roughness );

    // Evaluate normal distribution function
    float3  tsH = float3( dot( _H, _BSDFData.tangentWS ), dot( _H, _BSDFData.biTangentWS ), dot( _H, _BSDFData.normalWS ) );
    float2  rotH = tsH.x * _preLightData.anisoX + tsH.y * _preLightData.anisoY;

    float3  N = 0;
    switch ( (_SVBRDF_BRDFVariants >> 4) & 3 ) {
        case 0: {   // Ashikmin-Shirley
            N   = sqrt( (1+exponents.x) * (1+exponents.y) ) / (8 * PI)
                * pow( saturate( tsH.z ), (exponents.x * Sq(rotH.x) + exponents.y * Sq(rotH.y)) / (1 - Sq(tsH.z)) )
                / (_LdotH * max( _NdotL, _NdotV ));
            break;
        }

        case 1: {   // Blinn
            float   exponent = 0.5 * (exponents.x + exponents.y);    // Should be isotropic anyway...
            N   = (exponent + 2) / (8 * PI)
                * pow( saturate( tsH.z ), exponent );
            break;
        }

        case 2: // VRay
        case 3: // Lewis
            N = 1000 * float3( 1, 0, 1 );   // Not documented...
            break;
    }

    return _BSDFData.specularColor * N;
}

float3  ComputeCookTorrance( float3 _H, float _LdotH, float _NdotL, float _NdotV, float3 _positionWS, PreLightData _preLightData, BSDFData _BSDFData ) {
    float   NdotH = dot( _H, _BSDFData.normalWS );
    float   sqNdotH = Sq( NdotH );

    // Evaluate Fresnel term
    float3  F = F_Schlick( _BSDFData.fresnelF0, _LdotH );

    // Evaluate (isotropic) normal distribution function (Beckmann)
    float   sqAlpha = _BSDFData.roughness.x * _BSDFData.roughness.y;
    float   N = exp( (sqNdotH - 1) / (sqNdotH * sqAlpha) )
              / (PI * Sq(sqNdotH) * sqAlpha);

    // Evaluate shadowing/masking term
    float   G = CT_G( NdotH, _NdotV, _NdotL, _LdotH );

    return _BSDFData.specularColor * F * N * G;
}

float3  ComputeGGX( float3 _H, float _LdotH, float _NdotL, float _NdotV, float3 _positionWS, PreLightData _preLightData, BSDFData _BSDFData ) {
    // Evaluate Fresnel term
    float3  F = F_Schlick( _BSDFData.fresnelF0, _LdotH );

    // Evaluate normal distribution function (Trowbridge-Reitz)
    float3  tsH = float3( dot( _H, _BSDFData.tangentWS ), dot( _H, _BSDFData.biTangentWS ), dot( _H, _BSDFData.normalWS ) );
    float3  rotH = float3( (tsH.x * _preLightData.anisoX + tsH.y * _preLightData.anisoY) / _BSDFData.roughness, tsH.z );
    float   N = 1.0 / (PI * _BSDFData.roughness.x*_BSDFData.roughness.y) * 1.0 / Sq( dot( rotH, rotH ) );

    // Evaluate shadowing/masking term
    float   roughness = 0.5 * (_BSDFData.roughness.x + _BSDFData.roughness.y);
    float   G = G_smith( _NdotL, roughness ) * G_smith( _NdotV, roughness );
            G /= 4.0 * _NdotL * _NdotV;

    return _BSDFData.specularColor * F * N * G;
}

float3  ComputePhong( float3 _H, float _LdotH, float _NdotL, float _NdotV, float3 _positionWS, PreLightData _preLightData, BSDFData _BSDFData ) {
    return 1000 * float3( 1, 0, 1 );
}


// This function applies the BSDF. Assumes that _NdotL is positive.
void	BSDF(   float3 _viewWS, float3 _lightWS, float _NdotL, float3 _positionWS, PreLightData _preLightData, BSDFData _BSDFData,
                out float3 _diffuseLighting, out float3 _specularLighting ) {

    // Compute half vector used by various components of the BSDF
    float3  H = normalize( _viewWS + _lightWS );
    float   LdotH = saturate( dot( H, _lightWS ) );

    // Apply clear coat
    float3  clearCoatExtinction = 1.0;
    float3  clearCoatReflection = 0.0;
    if ( _flags & 2 ) {
        clearCoatReflection = (_BSDFData.clearCoatColor / PI) * F_FresnelDieletric( _BSDFData.clearCoatIOR, LdotH ); // Full reflection in mirror direction (we use expensive Fresnel here so the clear coat properly disappears when IOR -> 1)
        clearCoatExtinction = ComputeClearCoatExtinction( _viewWS, _lightWS, _preLightData, _BSDFData );
//        if ( _flags & 4U ) {
//            // Recompute half vector after refraction
//            H = normalize( _viewWS + _lightWS );
//            LdotH = saturate( dot( H, _lightWS ) );
//            _preLightData.NdotV = dot( _BSDFData.normalWS, _viewWS );
//        }
    }

    float   NdotV = ClampNdotV( _preLightData.NdotV );

    // Compute diffuse term
    float3  diffuseTerm = Lambert();
    if ( _SVBRDF_BRDFType & 1 ) {
        float   diffuseRoughness = 0.5 * HALF_PI;    // Arbitrary roughness (not specified in the documentation...)
//        float   diffuseRoughness = _DEBUG_anisotropicRoughessX * HALF_PI;    // Arbitrary roughness (not specified in the documentation...)
        diffuseTerm = INV_PI * OrenNayar( _BSDFData.normalWS, _viewWS, _lightWS, diffuseRoughness );
    }

    // Compute specular term
    float3  specularTerm = float3( 1, 0, 0 );
    switch ( (_SVBRDF_BRDFType >> 1) & 7 ) {
        case 0: specularTerm = ComputeWard( H, LdotH, _NdotL, NdotV, _positionWS, _preLightData, _BSDFData ); break;
        case 1: specularTerm = ComputeBlinnPhong( H, LdotH, _NdotL, NdotV, _positionWS, _preLightData, _BSDFData ); break;
        case 2: specularTerm = ComputeCookTorrance( H, LdotH, _NdotL, NdotV, _positionWS, _preLightData, _BSDFData ); break;
        case 3: specularTerm = ComputeGGX( H, LdotH, _NdotL, NdotV, _positionWS, _preLightData, _BSDFData ); break;
        case 4: specularTerm = ComputePhong( H, LdotH, _NdotL, NdotV, _positionWS, _preLightData, _BSDFData ); break;
        default:    // @TODO
            specularTerm = 1000 * float3( 1, 0, 1 );
            break;
    }

    // We don't multiply by '_BSDFData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
    _diffuseLighting = clearCoatExtinction * diffuseTerm;
    _specularLighting = clearCoatExtinction * specularTerm + clearCoatReflection;
}

#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)

// Samples the "BRDF Color Table" as explained in "AxF-Decoding-SDK-1.5.1/doc/html/page2.html#carpaint_ColorTable" from the SDK
float3  GetBRDFColor( float thetaH, float thetaD ) {

#if 0   // <== Define this to use the code from the documentation
    // In the documentation they write that we must divide by PI/2 (it would seem)
    float2  UV = float2( 2.0 * thetaH / PI, 2.0 * thetaD / PI );

    // BMAYAUX: Problem here is that the BRDF color tables are only defined in the upper-left triangular part of the texture
    // It's not indicated anywhere in the SDK documentation but I decided to clamp to the diagonal otherwise we get black values if UV.x+UV.y > 0.5!
    UV *= 2.0;
    UV *= saturate( UV.x + UV.y ) / max( 1e-3, UV.x + UV.y );
    UV *= 0.5;
#else
    // But the acos yields values in [0,PI] and the texture seems to be indicating the entire PI range is covered so...
    float2  UV = float2( thetaH / PI, thetaD / PI );
#endif

    return _CarPaint_BRDFColorMap_Scale * SAMPLE_TEXTURE2D_LOD( _CarPaint_BRDFColorMap_sRGB, sampler_CarPaint_BRDFColorMap_sRGB, float2( UV.x, 1 - UV.y ), 0 ).xyz;
}

// Samples the "BTF Flakes" texture as explained in "AxF-Decoding-SDK-1.5.1/doc/html/page2.html#carpaint_FlakeBTF" from the SDK
uint    SampleFlakesLUT( uint _index ) {
    return 255.0 * _CarPaint_thetaFI_sliceLUTMap[uint2( _index, 0 )].x;
// Hardcoded LUT
//    uint    pipoLUT[] = { 0, 8, 16, 24, 32, 40, 47, 53, 58, 62, 65, 67 };
//    return pipoLUT[min(11, _index)];
}

float3  SamplesFlakes( float2 _UV, uint _sliceIndex, float _mipLevel ) {
    return _CarPaint_BTFFlakesMap_Scale * SAMPLE_TEXTURE2D_ARRAY_LOD( _CarPaint_BTFFlakesMap_sRGB, sampler_CarPaint_BTFFlakesMap_sRGB, _UV, _sliceIndex, _mipLevel ).xyz;
}

#if 0
// Original code from the SDK, cleaned up a bit...
float3  CarPaint_BTF( float thetaH, float thetaD, BSDFData _BSDFData ) {
    float2  UV = _BSDFData.flakesUV;
    float   mipLevel = _BSDFData.flakesMipLevel;

    // thetaH sampling defines the angular sampling, i.e. angular flake lifetime
    float   binIndexH = _CarPaint_numThetaF * (2.0 * thetaH / PI) + 0.5;
    float   binIndexD = _CarPaint_numThetaF * (2.0 * thetaD / PI) + 0.5;

    // Bilinear interpolate indices and weights
    uint    thetaH_low = floor( binIndexH );
    uint    thetaD_low = floor( binIndexD );
    uint    thetaH_high = thetaH_low + 1;
    uint    thetaD_high = thetaD_low + 1;
    float   thetaH_weight = binIndexH - thetaH_low;
    float   thetaD_weight = binIndexD - thetaD_low;

    // To allow lower thetaD samplings while preserving flake lifetime, "virtual" thetaD patches are generated by shifting existing ones 
    float2   offset_l = 0;
    float2   offset_h = 0;
// BMAYAUX: At the moment I couldn't find any car paint material with the condition below
//    if ( _CarPaint_numThetaI < _CarPaint_numThetaF ) {
//        offset_l = float2( rnd_numbers[2*thetaD_low], rnd_numbers[2*thetaD_low+1] );
//        offset_h = float2( rnd_numbers[2*thetaD_high], rnd_numbers[2*thetaD_high+1] );
//        if ( thetaD_low & 1 )
//            UV.xy = UV.yx;
//        if ( thetaD_high & 1 )
//            UV.xy = UV.yx;
//
//        // Map to the original sampling
//        thetaD_low = floor( thetaD_low * float(_CarPaint_numThetaI) / _CarPaint_numThetaF );
//        thetaD_high = floor( thetaD_high * float(_CarPaint_numThetaI) / _CarPaint_numThetaF );
//    }

    float3  H0_D0 = 0.0;
    float3  H1_D0 = 0.0;
    float3  H0_D1 = 0.0;
    float3  H1_D1 = 0.0;

    // Access flake texture - make sure to stay in the correct slices (no slip over)
    if ( thetaD_low < _CarPaint_maxThetaI ) {
        float2  UVl = UV + offset_l;
        float2  UVh = UV + offset_h;

        uint    LUT0 = SampleFlakesLUT( thetaD_low );
        uint    LUT1 = SampleFlakesLUT( thetaD_high );
        uint    LUT2 = SampleFlakesLUT( thetaD_high+1 );

        if ( LUT0 + thetaH_low < LUT1 ) {
            H0_D0 = SamplesFlakes( UVl, LUT0 + thetaH_low, mipLevel );
            if ( LUT0 + thetaH_high < LUT1 ) {
                H1_D0 = SamplesFlakes( UVl, LUT0 + thetaH_high, mipLevel );
            }
            else H1_D0 = H0_D0 ??
        }

        if ( thetaD_high < _CarPaint_maxThetaI ) {
            if ( LUT1 + thetaH_low < LUT2 ) {
                H0_D1 = SamplesFlakes( UVh, LUT1 + thetaH_low, mipLevel );
                if ( LUT1 + thetaH_high < LUT2 ) {
                    H1_D1 = SamplesFlakes( UVh, LUT1 + thetaH_high, mipLevel );
                }
            }
        }
    }
    
    // Bilinear interpolation
    float3  D0 = lerp( H0_D0, H1_D0, thetaH_weight );
    float3  D1 = lerp( H0_D1, H1_D1, thetaH_weight );
    return lerp( D0, D1, thetaD_weight );
}

#else

// Simplified code
float3  CarPaint_BTF( float thetaH, float thetaD, BSDFData _BSDFData ) {
    float2  UV = _BSDFData.flakesUV;
    float   mipLevel = _BSDFData.flakesMipLevel;

    // thetaH sampling defines the angular sampling, i.e. angular flake lifetime
    float   binIndexH = _CarPaint_numThetaF * (2.0 * thetaH / PI) + 0.5;
    float   binIndexD = _CarPaint_numThetaI * (2.0 * thetaD / PI) + 0.5;

    // Bilinear interpolate indices and weights
    uint    thetaH_low = floor( binIndexH );
    uint    thetaD_low = floor( binIndexD );
    uint    thetaH_high = thetaH_low + 1;
    uint    thetaD_high = thetaD_low + 1;
    float   thetaH_weight = binIndexH - thetaH_low;
    float   thetaD_weight = binIndexD - thetaD_low;

    // Access flake texture - make sure to stay in the correct slices (no slip over)
    // @TODO: Store RGB value with all 3 integers? Single tap into LUT...
    uint    LUT0 = SampleFlakesLUT( min( _CarPaint_maxThetaI-1, thetaD_low ) );
    uint    LUT1 = SampleFlakesLUT( min( _CarPaint_maxThetaI-1, thetaD_high ) );
    uint    LUT2 = SampleFlakesLUT( min( _CarPaint_maxThetaI-1, thetaD_high+1 ) );

    float3  H0_D0 = SamplesFlakes( UV, min( LUT0 + thetaH_low, LUT1-1 ), mipLevel );
    float3  H1_D0 = SamplesFlakes( UV, min( LUT0 + thetaH_high, LUT1-1 ), mipLevel );
    float3  H0_D1 = SamplesFlakes( UV, min( LUT1 + thetaH_low, LUT2-1 ), mipLevel );
    float3  H1_D1 = SamplesFlakes( UV, min( LUT1 + thetaH_high, LUT2-1 ), mipLevel );
    
    // Bilinear interpolation
    float3  D0 = lerp( H0_D0, H1_D0, thetaH_weight );
    float3  D1 = lerp( H0_D1, H1_D1, thetaH_weight );
    return lerp( D0, D1, thetaD_weight );
}

#endif


// This function applies the BSDF. Assumes that _NdotL is positive.
void	BSDF(   float3 _viewWS, float3 _lightWS, float _NdotL, float3 _positionWS, PreLightData _preLightData, BSDFData _BSDFData,
                out float3 _diffuseLighting, out float3 _specularLighting ) {

    // Compute half vector used by various components of the BSDF
    float3  H = normalize( _viewWS + _lightWS );
    float   LdotH = dot( H, _lightWS );

    // Apply clear coat
    float3  clearCoatExtinction = 1.0;
    float3  clearCoatReflection = 0.0;
    if ( _flags & 2 ) {
        clearCoatReflection = (_BSDFData.clearCoatColor / PI) * F_FresnelDieletric( _BSDFData.clearCoatIOR, LdotH ); // Full reflection in mirror direction (we use expensive Fresnel here so the clear coat properly disappears when IOR -> 1)
        clearCoatExtinction = ComputeClearCoatExtinction( _viewWS, _lightWS, _preLightData, _BSDFData );
        if ( _flags & 4U ) {
            // Recompute half vector after refraction
            H = normalize( _viewWS + _lightWS );
            LdotH = saturate( dot( H, _lightWS ) );
            _preLightData.NdotV = dot( _BSDFData.normalWS, _viewWS );
        }
    }

    // Compute remaining values AFTER potential clear coat refraction
    float   NdotV = ClampNdotV( _preLightData.NdotV );
    float   NdotL = dot( _BSDFData.normalWS, _lightWS );
    float   NdotH = dot( _BSDFData.normalWS, H );
    float   VdotH = LdotH;

    float   thetaH = acos( clamp( NdotH, -1, 1 ) );
    float   thetaD = acos( clamp( LdotH, -1, 1 ) );

    // Simple lambert
    float3  diffuseTerm = Lambert();

    // Apply multi-lobes Cook-Torrance
    float3  specularTerm = MultiLobesCookTorrance( NdotL, NdotV, NdotH, VdotH );

    // Apply BRDF color
    float3  BRDFColor = GetBRDFColor( thetaH, thetaD );
    diffuseTerm *= BRDFColor;
    specularTerm *= BRDFColor;

    // Apply flakes
    specularTerm += CarPaint_BTF( thetaH, thetaD, _BSDFData );

    // We don't multiply by '_BSDFData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
    _diffuseLighting = clearCoatExtinction * diffuseTerm;
    _specularLighting = clearCoatExtinction * specularTerm + clearCoatReflection;


#if 0   // DEBUG

#if 0
    // Debug BRDF Color texture
//    float2  UV = float2( 2.0 * thetaH / PI, 2.0 * thetaD / PI );
//thetaD = min( thetaH, thetaD );
    float2  UV = float2( 2.0 * thetaH / PI, 2.0 * thetaD / PI );

//UV = _BSDFData.flakesUV;
    BRDFColor = _CarPaint_BRDFColorMap_Scale * SAMPLE_TEXTURE2D_LOD( _CarPaint_BRDFColorMap_sRGB, sampler_CarPaint_BRDFColorMap_sRGB, float2( UV.x, 1.0 - UV.y ), 0 ).xyz;

//BRDFColor = 2 * thetaH / PI;
//if ( UV.x + UV.y > 37.0 / 64.0 )
////if ( UV.y > 37.0 / 64.0 )
//    BRDFColor = _CarPaint_BRDFColorMap_Scale * float3( 1, 0, 1 );
////BRDFColor = float3( UV, 0 );

    _diffuseLighting = BRDFColor;
#else
    // Debug flakes
    _diffuseLighting = SamplesFlakes( _BSDFData.flakesUV, _DEBUG_clearCoatIOR, 0 );
    _diffuseLighting = CarPaint_BTF( thetaH, thetaD, _BSDFData );

#endif

// Normalize so 1 is white
_diffuseLighting /= _BSDFData.diffuseColor;

#endif
}

#else

// This function applies the BSDF. Assumes that _NdotL is positive.
void	BSDF(   float3 _viewWS, float3 _lightWS, float _NdotL, float3 _positionWS, PreLightData _preLightData, BSDFData _BSDFData,
                out float3 _diffuseLighting, out float3 _specularLighting ) {

    float  diffuseTerm = Lambert();

    // We don't multiply by '_BSDFData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
    _diffuseLighting = diffuseTerm;
    _specularLighting = float3(0.0, 0.0, 0.0);
}

#endif

//-----------------------------------------------------------------------------
// EvaluateBSDF_Directional
//-----------------------------------------------------------------------------

DirectLighting  EvaluateBSDF_Directional(   LightLoopContext _lightLoopContext,
                                            float3 _viewWS, PositionInputs _posInput, PreLightData _preLightData,
                                            DirectionalLightData _lightData, BSDFData _BSDFData,
                                            BakeLightingData _bakedLightingData ) {

    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    float3  normalWS = _BSDFData.normalWS;
    float3  lightWS = -_lightData.forward; // Lights point backward in Unity
    //float  NdotV = ClampNdotV(_preLightData.NdotV);
    float   NdotL = dot(normalWS, lightWS);
    //float  LdotV = dot(lightWS, _viewWS);

    // color and attenuation are outputted  by EvaluateLight:
    float3  color;
    float   attenuation = 0;
    EvaluateLight_Directional( _lightLoopContext, _posInput, _lightData, _bakedLightingData, normalWS, lightWS, color, attenuation );

    float intensity = max(0, attenuation * NdotL); // Warning: attenuation can be greater than 1 due to the inverse square attenuation (when position is close to light)

    // Note: We use NdotL here to early out, but in case of clear coat this is not correct. But we are ok with this
    UNITY_BRANCH if ( intensity > 0.0 ) {
        BSDF( _viewWS, lightWS, NdotL, _posInput.positionWS, _preLightData, _BSDFData, lighting.diffuse, lighting.specular );

        lighting.diffuse  *= intensity * _lightData.diffuseScale;
        lighting.specular *= intensity * _lightData.specularScale;
    }

    // NEWLITTODO: Mixed thickness, transmission

    // Save ALU by applying light and cookie colors only once.
    lighting.diffuse  *= color;
    lighting.specular *= color;

    #ifdef DEBUG_DISPLAY
        if ( _DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER ) {
            lighting.diffuse = color * intensity * _lightData.diffuseScale;	// Only lighting, not BSDF
        }


//lighting.specular = -Refract( lightWS, _BSDFData.clearCoatNormalWS, _BSDFData.clearCoatIOR );
//lighting.specular = dot( -Refract( lightWS, _BSDFData.clearCoatNormalWS, _BSDFData.clearCoatIOR ), _BSDFData.clearCoatNormalWS );
//lighting.specular = dot( -Refract( _viewWS, _BSDFData.clearCoatNormalWS, _BSDFData.clearCoatIOR ), _BSDFData.clearCoatNormalWS );

//lighting.specular = (_BSDFData.clearCoatIOR - 1.0) * 1;
//lighting.specular = 0.5 * (1.0 + _BSDFData.clearCoatNormalWS);
//lighting.specular = 100.0 * (1.0 - dot( _BSDFData.normalWS, _BSDFData.clearCoatNormalWS) );


//lighting.diffuse = 0;
//lighting.specular = 0;

    #endif

    return lighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Punctual (supports spot, point and projector lights)
//-----------------------------------------------------------------------------

DirectLighting  EvaluateBSDF_Punctual(  LightLoopContext _lightLoopContext,
                                        float3 _viewWS, PositionInputs _posInput,
                                        PreLightData _preLightData, LightData _lightData, BSDFData _BSDFData, BakeLightingData _bakedLightingData ) {
    DirectLighting	lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    float3	lightToSample = _posInput.positionWS - _lightData.positionWS;
    int		lightType     = _lightData.lightType;

    float3 lightWS;
    float4 distances; // {d, d^2, 1/d, d_proj}
    distances.w = dot(lightToSample, _lightData.forward);

    if ( lightType == GPULIGHTTYPE_PROJECTOR_BOX ) {
	    lightWS = -_lightData.forward;
	    distances.xyz = 1; // No distance or angle attenuation
    } else {
	    float3 unL     = -lightToSample;
	    float  distSq  = dot(unL, unL);
	    float  distRcp = rsqrt(distSq);
	    float  dist    = distSq * distRcp;

	    lightWS = unL * distRcp;
	    distances.xyz = float3(dist, distSq, distRcp);
    }

    float3 normalWS     = _BSDFData.normalWS;
    float  NdotV = ClampNdotV(_preLightData.NdotV);
    float  NdotL = dot(normalWS, lightWS);
    float  LdotV = dot(lightWS, _viewWS);

    // NEWLITTODO: mixedThickness, transmission

    float3 color;
    float attenuation;
    EvaluateLight_Punctual( _lightLoopContext, _posInput, _lightData, _bakedLightingData, normalWS, lightWS,
						    lightToSample, distances, color, attenuation);


    float intensity = max(0, attenuation * NdotL); // Warning: attenuation can be greater than 1 due to the inverse square attenuation (when position is close to light)

    // Note: We use NdotL here to early out, but in case of clear coat this is not correct. But we are ok with this
    UNITY_BRANCH if ( intensity > 0.0 ) {
        // Simulate a sphere light with this hack
        // Note that it is not correct with our pre-computation of PartLambdaV (mean if we disable the optimization we will not have the
        // same result) but we don't care as it is a hack anyway

        //NEWLITTODO: Do we want this hack in stacklit ? Yes we have area lights, but cheap and not much maintenance to leave it here.
        // For now no roughness anyways.

        //_BSDFData.coatRoughness = max(_BSDFData.coatRoughness, _lightData.minRoughness);
        //_BSDFData.roughnessT = max(_BSDFData.roughnessT, _lightData.minRoughness);
        //_BSDFData.roughnessB = max(_BSDFData.roughnessB, _lightData.minRoughness);

        BSDF(_viewWS, lightWS, NdotL, _posInput.positionWS, _preLightData, _BSDFData, lighting.diffuse, lighting.specular);

        lighting.diffuse  *= intensity * _lightData.diffuseScale;
        lighting.specular *= intensity * _lightData.specularScale;
    }

    // Save ALU by applying light and cookie colors only once.
    lighting.diffuse  *= color;
    lighting.specular *= color;

    #ifdef DEBUG_DISPLAY
        if ( _DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER ) {
            lighting.diffuse = color * intensity * _lightData.diffuseScale;		// Only lighting, not BSDF
        }
    #endif

	return lighting;
}

// NEWLITTODO: For a refence rendering option for area light, like LIT_DISPLAY_REFERENCE_AREA option in eg EvaluateBSDF_<area light type> :
//#include "LitReference.hlsl"

//-----------------------------------------------------------------------------
// EvaluateBSDF_Line - Approximation with Linearly Transformed Cosines
//-----------------------------------------------------------------------------

DirectLighting  EvaluateBSDF_Line(  LightLoopContext _lightLoopContext,
                                    float3 _viewWS, PositionInputs _posInput,
                                    PreLightData _preLightData, LightData _lightData, BSDFData _BSDFData, BakeLightingData _bakedLightingData ) {
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    //NEWLITTODO

// Apply coating
//specularLighting += F_FresnelDieletric( _BSDFData.clearCoatIOR, LdotN ) * Irradiance;

    return lighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Area - Approximation with Linearly Transformed Cosines
//-----------------------------------------------------------------------------

// #define ELLIPSOIDAL_ATTENUATION

DirectLighting  EvaluateBSDF_Rect(  LightLoopContext _lightLoopContext,
                                    float3 _viewWS, PositionInputs _posInput,
                                    PreLightData _preLightData, LightData _lightData, BSDFData _BSDFData, BakeLightingData _bakedLightingData ) {
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    //NEWLITTODO

// Apply coating
//specularLighting += F_FresnelDieletric( _BSDFData.clearCoatIOR, LdotN ) * Irradiance;

    return lighting;
}

DirectLighting  EvaluateBSDF_Area(  LightLoopContext _lightLoopContext,
                                    float3 _viewWS, PositionInputs _posInput,
                                    PreLightData _preLightData, LightData _lightData,
                                    BSDFData _BSDFData, BakeLightingData _bakedLightingData ) {

    if (_lightData.lightType == GPULIGHTTYPE_LINE) {
        return EvaluateBSDF_Line( _lightLoopContext, _viewWS, _posInput, _preLightData, _lightData, _BSDFData, _bakedLightingData );
    } else {
        return EvaluateBSDF_Rect( _lightLoopContext, _viewWS, _posInput, _preLightData, _lightData, _BSDFData, _bakedLightingData );
    }
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_SSLighting for screen space lighting
// ----------------------------------------------------------------------------

IndirectLighting    EvaluateBSDF_SSLighting(    LightLoopContext _lightLoopContext,
                                                float3 _viewWS, PositionInputs _posInput,
                                                PreLightData _preLightData, BSDFData _BSDFData,
                                                EnvLightData _envLightData,
                                                int _GPUImageBasedLightingType,
                                                inout float _hierarchyWeight ) {

    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);

    //NEWLITTODO

// Apply coating
//specularLighting += F_FresnelDieletric( _BSDFData.clearCoatIOR, LdotN ) * Irradiance;

    return lighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Env
// ----------------------------------------------------------------------------

// _preIntegratedFGD and _CubemapLD are unique for each BRDF
IndirectLighting    EvaluateBSDF_Env(   LightLoopContext _lightLoopContext,
                                        float3 _viewWS, PositionInputs _posInput,
                                        PreLightData _preLightData, EnvLightData _lightData, BSDFData _BSDFData,
                                        int _influenceShapeType, int _GPUImageBasedLightingType,
                                        inout float _hierarchyWeight ) {

    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);

return lighting;


    if ( _GPUImageBasedLightingType != GPUIMAGEBASEDLIGHTINGTYPE_REFLECTION )
        return lighting;    // We don't support transmission

    float3  positionWS = _posInput.positionWS;
    float   weight = 1.0;

    float   NdotV = ClampNdotV( _preLightData.NdotV );

    float3  environmentSamplingDirectionWS = _preLightData.IBLDominantDirectionWS;

    float   IBLRoughness = _preLightData.IBLRoughness;
//  float   IBLRoughness = PerceptualRoughnessToRoughness( _preLightData.IBLRoughness );

    if ( (_lightData.envIndex & 1) == ENVCACHETYPE_CUBEMAP ) {
        // When we are rough, we tend to see outward shifting of the reflection when at the boundary of the projection volume
        // Also it appear like more sharp. To avoid these artifact and at the same time get better match to reference we lerp to original unmodified reflection.
        // Formula is empirical.
        environmentSamplingDirectionWS = GetSpecularDominantDir( _BSDFData.normalWS, environmentSamplingDirectionWS, IBLRoughness, NdotV );
        environmentSamplingDirectionWS = lerp( environmentSamplingDirectionWS, _preLightData.IBLDominantDirectionWS, saturate(smoothstep(0, 1, IBLRoughness * IBLRoughness)) );
    }

    // Note: using _influenceShapeType and projectionShapeType instead of (_lightData|proxyData).shapeType allow to make compiler optimization in case the type is know (like for sky)
    EvaluateLight_EnvIntersection( positionWS, _BSDFData.normalWS, _lightData, _influenceShapeType, environmentSamplingDirectionWS, weight );

    // TODO: We need to match the PerceptualRoughnessToMipmapLevel formula for planar, so we don't do this test (which is specific to our current lightloop)
    // Specific case for Texture2Ds, their convolution is a gaussian one and not a GGX one - So we use another roughness mip mapping.
    float   IBLMipLevel;
    if ( IsEnvIndexTexture2D( _lightData.envIndex ) ) {
        // Empirical remapping
        IBLMipLevel = PositivePow( IBLRoughness, 0.8 ) * uint( max( 0, _ColorPyramidScale.z - 1 ) );
    } else {
        IBLMipLevel = PerceptualRoughnessToMipmapLevel( IBLRoughness );
    }

    //-----------------------------------------------------------------------------
    #if defined(_AXF_BRDF_TYPE_SVBRDF)
        // Use FGD as factor for the env map
        float3  envBRDF = _preLightData.specularFGD;

        // Sample the actual environment lighting
        float4  preLD = SampleEnv( _lightLoopContext, _lightData.envIndex, environmentSamplingDirectionWS, IBLMipLevel );
        weight *= preLD.w; // Used by planar reflection to discard pixel

        float3  envLighting = envBRDF * preLD.xyz;

    //-----------------------------------------------------------------------------
    #elif defined(_AXF_BRDF_TYPE_CAR_PAINT)
        // Evaluate average BRDF response in specular direction
// @TODO: Use FGD table! => Ward / Cook-Torrance both use Beckmann so it should be easy...

        float3  safeLightWS = environmentSamplingDirectionWS;
//        float3  safeLightWS = _preLightData.IBLDominantDirectionWS;
//                safeLightWS += max( 1e-2, dot( safeLightWS, _BSDFData.normalWS ) ) * _BSDFData.normalWS;    // Move away from surface to avoid super grazing angles
//                safeLightWS = normalize( safeLightWS );

        float3  H = normalize( _viewWS + safeLightWS );
        float   NdotL = saturate( dot( _BSDFData.normalWS, safeLightWS ) );
        float   NdotH = dot( _BSDFData.normalWS, H );
        float   VdotH = dot( _viewWS, H );

//NdotH = max( 1e-3, NdotH );
//NdotL = 1;//max( 1e-1, NdotL );
//NdotV = max( 1e-3, NdotV );
//NdotV = 1;

        float   thetaH = acos( clamp( NdotH, -1, 1 ) );
        float   thetaD = acos( clamp( VdotH, -1, 1 ) );

        //-----------------------------------------------------------------------------
        #if 0
            // Single lobe approach
            // We computed an average mip level stored in _preLightData.IBLRoughness that we use for all CT lobes
            //
            float3  envBRDF = MultiLobesCookTorrance( NdotL, NdotV, NdotH, VdotH ); // Specular multi-lobes CT
                    envBRDF *= GetBRDFColor( thetaH, thetaD );
                    envBRDF += CarPaint_BTF( thetaH, thetaD, _BSDFData );           // Sample flakes

            envBRDF *= NdotL;

            // Sample the actual environment lighting
            float4  preLD = SampleEnv( _lightLoopContext, _lightData.envIndex, environmentSamplingDirectionWS, IBLMipLevel );
            float3  envLighting = envBRDF * preLD.xyz;

            weight *= preLD.w; // Used by planar reflection to discard pixel

        //-----------------------------------------------------------------------------
        #else
            // Multi-lobes approach
            // Each CT lobe samples the environment with the appropriate roughness
            float3  envLighting = 0.0;
            float   sumWeights = 0.0;
            for ( uint lobeIndex=0; lobeIndex < _CarPaint_lobesCount; lobeIndex++ ) {
                float   F0 = _CarPaint_CT_F0s[lobeIndex];
                float   coeff = _CarPaint_CT_coeffs[lobeIndex];
                float   spread = _CarPaint_CT_spreads[lobeIndex];

                float   lobeIntensity = coeff * CT_D( NdotH, spread ) * CT_F( VdotH, F0 );
                float   lobeMipLevel = PerceptualRoughnessToMipmapLevel( spread );
                float4  preLD = SampleEnv( _lightLoopContext, _lightData.envIndex, environmentSamplingDirectionWS, lobeMipLevel );
                envLighting += lobeIntensity * preLD.xyz;
                sumWeights += preLD.w;
            }
            envLighting *= CT_G( NdotH, NdotV, NdotL, VdotH )  // Shadowing/Masking term
                         / (PI * max( 1e-3, NdotV * NdotL ));
            envLighting *= GetBRDFColor( thetaH, thetaD );

            // Sample flakes
            float   flakesMipLevel = 0;   // Flakes are supposed to be perfect mirrors...
            envLighting += CarPaint_BTF( thetaH, thetaD, _BSDFData ) * SampleEnv( _lightLoopContext, _lightData.envIndex, environmentSamplingDirectionWS, flakesMipLevel ).xyz;

            envLighting *= NdotL;

            weight *= sumWeights / _CarPaint_lobesCount;

        #endif

    #endif

    //-----------------------------------------------------------------------------
    // Evaluate the Clear Coat component if needed
    if ( _flags & 2 ) {

            // Evaluate clear coat sampling direction
        float   unusedWeight = 0.0;
        float3  clearCoatSamplingDirectionWS = environmentSamplingDirectionWS;
        EvaluateLight_EnvIntersection( positionWS, _BSDFData.clearCoatNormalWS, _lightData, _influenceShapeType, clearCoatSamplingDirectionWS, unusedWeight );

        // Evaluate clear coat fresnel
        #if 1   // Use LdotH ==> Makes more sense! Stick to Cook-Torrance here...
            float3  H = normalize( _viewWS + clearCoatSamplingDirectionWS );
            float   LdotH = dot( clearCoatSamplingDirectionWS, H );
            float3  clearCoatF = F_FresnelDieletric( _BSDFData.clearCoatIOR, LdotH );
        #else   // Use LdotN
            float   LdotN = saturate( dot( clearCoatSamplingDirectionWS, _BSDFData.clearCoatNormalWS ) );
            float3  clearCoatF = F_FresnelDieletric( _BSDFData.clearCoatIOR, LdotN );
        #endif

        // Attenuate environment lighting under the clear coat by the complement to the Fresnel term
        envLighting *= 1.0 - clearCoatF;

        // Then add the environment lighting reflected by the clear coat
        // We assume the BRDF here is perfect mirror so there's no masking/shadowing, only the Fresnel term * clearCoatColor/PI
        float4  preLD = SampleEnv( _lightLoopContext, _lightData.envIndex, clearCoatSamplingDirectionWS, 0.0 );
        envLighting += (_BSDFData.clearCoatColor / PI) * clearCoatF * preLD.xyz;

        // Can't attenuate diffuse lighting here, may try to apply something on bakeLighting in PostEvaluateBSDF
    }

    UpdateLightingHierarchyWeights( _hierarchyWeight, weight );
    envLighting *= weight * _lightData.multiplier;

    lighting.specularReflected = envLighting;

    return lighting;
}

//-----------------------------------------------------------------------------
// PostEvaluateBSDF
// ----------------------------------------------------------------------------

void    PostEvaluateBSDF(   LightLoopContext _lightLoopContext,
                            float3 _viewWS, PositionInputs _posInput,
                            PreLightData _preLightData, BSDFData _BSDFData, BakeLightingData _bakedLightingData, AggregateLighting _lighting,
                            out float3 _diffuseLighting, out float3 _specularLighting ) {

//    AmbientOcclusionFactor  AOFactor;
//    // Use GTAOMultiBounce approximation for ambient occlusion (allow to get a tint from the baseColor)
//#if 0
//    GetScreenSpaceAmbientOcclusion( _posInput.positionSS, _preLightData.NdotV, _BSDFData.perceptualRoughness, 1.0, _BSDFData.specularOcclusion, AOFactor );
//#else
//    GetScreenSpaceAmbientOcclusionMultibounce( _posInput.positionSS, _preLightData.NdotV, _BSDFData.perceptualRoughness, 1.0, _BSDFData.specularOcclusion, _BSDFData.diffuseColor, _BSDFData.fresnel0, AOFactor);
//#endif
//
//    // Add indirect diffuse + emissive (if any) - Ambient occlusion is multiply by emissive which is wrong but not a big deal
//    bakeDiffuseLighting                 *= AOFactor.indirectAmbientOcclusion;
//    _lighting.indirect.specularReflected *= AOFactor.indirectSpecularOcclusion;
//    _lighting.direct.diffuse             *= AOFactor.directAmbientOcclusion;


    // Apply the albedo to the direct diffuse lighting and that's about it.
    // diffuse lighting has already had the albedo applied in GetBakedDiffuseLighting().
    _diffuseLighting = _BSDFData.diffuseColor * _lighting.direct.diffuse + _bakedLightingData.bakeDiffuseLighting;
    _specularLighting = _lighting.direct.specular + _lighting.indirect.specularReflected;

#if !defined(_AXF_BRDF_TYPE_SVBRDF) && !defined(_AXF_BRDF_TYPE_CAR_PAINT)
    _diffuseLighting = 10 * float3( 1, 0.3, 0.01 );  // @TODO!
#endif

    #ifdef DEBUG_DISPLAY
        if ( _DebugLightingMode != 0 ) {
            bool keepSpecular = false;

            switch ( _DebugLightingMode ) {
                case DEBUGLIGHTINGMODE_SPECULAR_LIGHTING:
                    keepSpecular = true;
                    break;

                case DEBUGLIGHTINGMODE_LUX_METER:
                    _diffuseLighting = _lighting.direct.diffuse + _bakedLightingData.bakeDiffuseLighting;
                    break;

                case DEBUGLIGHTINGMODE_INDIRECT_DIFFUSE_OCCLUSION:
//                  _diffuseLighting = AOFactor.indirectAmbientOcclusion;
                    break;

                case DEBUGLIGHTINGMODE_INDIRECT_SPECULAR_OCCLUSION:
//                  _diffuseLighting = AOFactor.indirectSpecularOcclusion;
                    break;

                case DEBUGLIGHTINGMODE_SCREEN_SPACE_TRACING_REFRACTION:
//                  if (_DebugLightingSubMode != DEBUGSCREENSPACETRACING_COLOR)
//                  	_diffuseLighting = _lighting.indirect.specularTransmitted;
//                  else
//                  	keepSpecular = true;
                    break;

                case DEBUGLIGHTINGMODE_SCREEN_SPACE_TRACING_REFLECTION:
//                  if (_DebugLightingSubMode != DEBUGSCREENSPACETRACING_COLOR)
//                      _diffuseLighting = _lighting.indirect.specularReflected;
//                  else
//                      keepSpecular = true;
                    break;
            }

            if ( !keepSpecular )
                _specularLighting = float3(0.0, 0.0, 0.0); // Disable specular lighting

        } else if ( _DebugMipMapMode != DEBUGMIPMAPMODE_NONE ) {
            _diffuseLighting = _BSDFData.diffuseColor;
            _specularLighting = float3(0.0, 0.0, 0.0); // Disable specular lighting
        }

//_diffuseLighting = float3( 1, 0, 0 );

    #endif

// DEBUG: Make sure the flakes texture2DArray is correct!
//#if defined(_AXF_BRDF_TYPE_CAR_PAINT)
//_diffuseLighting = 0;
////_specularLighting = float3( 1, 0, 0 );
//_specularLighting = SamplesFlakes( _BSDFData.flakesUV, _DEBUG_clearCoatIOR, 0 );
//#endif

}

#endif // #ifdef HAS_LIGHTLOOP
