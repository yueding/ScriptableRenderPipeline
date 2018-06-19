// ===========================================================================
//                              WARNING:
// On PS4, texture/sampler declarations need to be outside of CBuffers
// Otherwise those parameters are not bound correctly at runtime.
// ===========================================================================
//

//TEXTURE2D(_DistortionVectorMap);
//SAMPLER(sampler_DistortionVectorMap);

//TEXTURE2D(_EmissiveColorMap);
//SAMPLER(sampler_EmissiveColorMap);

TEXTURE2D(_BaseColorMap);
SAMPLER(sampler_BaseColorMap);


//////////////////////////////////////////////////////////////////////////////
// SVBRDF
TEXTURE2D( _SVBRDF_DiffuseColorMap_sRGB );          // RGB Diffuse color (2.2 gamma must be applied)
TEXTURE2D( _SVBRDF_SpecularColorMap_sRGB );         // RGB Specular color (2.2 gamma must be applied)
TEXTURE2D( _SVBRDF_NormalMap );                     // Tangent-Space Normal vector with offset (i.e. in [0,1], need to 2*normal-1 to get actual vector)
TEXTURE2D( _SVBRDF_SpecularLobeMap );               // Specular lobe in [0,1]. Either a scalar if isotropic, or a float2 if anisotropic.
TEXTURE2D( _SVBRDF_OpacityMap );                    // Alpha (scalar in [0,1])
TEXTURE2D( _SVBRDF_FresnelMap_sRGB );               // RGB F0 (2.2 gamma must be applied)
TEXTURE2D( _SVBRDF_AnisotropicRotationAngleMap );   // Rotation angle (scalar in [0,1], needs to be remapped in [0,2PI])
TEXTURE2D( _SVBRDF_HeightMap );                     // Height map (scalar in [0,1], need to be remapped with heightmap

SAMPLER( sampler_SVBRDF_DiffuseColorMap_sRGB );
SAMPLER( sampler_SVBRDF_SpecularColorMap_sRGB );
SAMPLER( sampler_SVBRDF_NormalMap );
SAMPLER( sampler_SVBRDF_SpecularLobeMap );
SAMPLER( sampler_SVBRDF_OpacityMap );
SAMPLER( sampler_SVBRDF_FresnelMap_sRGB );
SAMPLER( sampler_SVBRDF_AnisotropicRotationAngleMap );
SAMPLER( sampler_SVBRDF_HeightMap );


//////////////////////////////////////////////////////////////////////////////
// Car Paint
TEXTURE2D( _CarPaint_BRDFColorMap_sRGB );       // RGB BRDF color (2.2 gamma must be applied + scale)
TEXTURE2D_ARRAY( _CarPaint_BTFFlakesMap_sRGB ); // RGB Flakes color (2.2 gamma must be applied + scale)
TEXTURE2D( _CarPaint_thetaFI_sliceLUTMap );     // UINT indirection values (must be scaled by 255 and cast as UINTs)

SAMPLER( sampler_CarPaint_BRDFColorMap_sRGB );
SAMPLER( sampler_CarPaint_BTFFlakesMap_sRGB );
SAMPLER( sampler_CarPaint_thetaFI_sliceLUTMap );


//////////////////////////////////////////////////////////////////////////////
// Other
TEXTURE2D( _SVBRDF_ClearCoatColorMap_sRGB );        // RGB Clear coat color (2.2 gamma must be applied)
TEXTURE2D( _SVBRDF_ClearCoatNormalMap );            // Tangent-Space clear coat Normal vector with offset (i.e. in [0,1], need to 2*normal-1 to get actual vector)
TEXTURE2D( _SVBRDF_ClearCoatIORMap_sRGB );          // Clear coat F0 (2.2 gamma must be applied)

SAMPLER( sampler_SVBRDF_ClearCoatColorMap_sRGB );
SAMPLER( sampler_SVBRDF_ClearCoatNormalMap );
SAMPLER( sampler_SVBRDF_ClearCoatIORMap_sRGB );


CBUFFER_START(UnityPerMaterial)

    float   _materialSizeU_mm;              // Size of the U range, in millimeters (currently used as UV scale factor)
    float   _materialSizeV_mm;              // Size of the V range, in millimeters (currently used as UV scale factor)

    uint    _flags;                         // Bit 0 = Anisotropic. If true, specular lobe map contains 2 channels and the _AnisotropicRotationAngleMap needs to be read
                                            // Bit 1 = HasClearCoat. If true, the clear coat must be applied. The _ClearCoatNormalMap must be valid and contain clear coat normal data.
                                            // Bit 2 = ClearCoatUseRefraction. If true, then _ClearCoatIORMap must be valid and contain clear coat IOR data. If false then rays are not refracted by the clear coat layer.
                                            // Bit 3 = useHeightMap. If true then displacement mapping is used and _HeightMap must contain valid data.
                                            //

    //////////////////////////////////////////////////////////////////////////////
    // SVBRDF
    uint    _SVBRDF_BRDFType;               // Bit 0 = Diffuse Type. 0 = Lambert, 1 = Oren-Nayar
                                            // Bit 1-3 = Specular Type. 0 = Ward, 1 = Blinn-Phong, 2 = Cook-Torrance, 3 = GGX, 4 = Phong
                                            //

    uint    _SVBRDF_BRDFVariants;           // Bit 0-1 = Fresnel Variant. 0 = No Fresnel, 1 = Dielectric (Cook-Torrance 1981), 2 = Schlick (1994)
                                            // Bit 2-3 = Ward NDF Variant. 0 = Moroder (2010), 1 = Dur (2006), 2 = Ward (1992)
                                            // Bit 4-5 = Blinn-Phong Variant. 0 = Ashikmin-Shirley (2000), 1 = Blinn (1977), 2 = V-Ray, 3 = Lewis (1993)
                                            //

    float   _SVBRDF_SpecularLobeMap_Scale;  // Optional scale factor to the specularLob map (useful when the map contains arbitrary Phong exponents)

    float   _SVBRDF_heightMapMax_mm;        // Maximum height map displacement, in millimeters


    //////////////////////////////////////////////////////////////////////////////
    // Car Paint
    float   _CarPaint_CT_diffuse;           // Diffuse factor
    float   _CarPaint_IOR;                  // Clear coat IOR

        // BRDF
    float   _CarPaint_BRDFColorMap_Scale;   // Optional scale factor to the BRDFColor map
    float   _CarPaint_BTFFlakesMap_Scale;   // Optional scale factor to the BTFFlakes map

        // Cook-Torrance Lobes Descriptors
    uint    _CarPaint_lobesCount;           // Amount of valid components in the vectors below
    float4  _CarPaint_CT_F0s;               // Description of multi-lobes F0 values
    float4  _CarPaint_CT_coeffs;            // Description of multi-lobes coefficients values
    float4  _CarPaint_CT_spreads;           // Description of multi-lobes spread values

        // Flakes
    float   _CarPaint_FlakesTiling;         // Tiling factor for flakes
    uint    _CarPaint_maxThetaI;            // Maximum thetaI index
    uint    _CarPaint_numThetaF;            // Amount of thetaF entries (in litterature, that's called thetaH, the angle between the normal and the half vector)
    uint    _CarPaint_numThetaI;            // Amount of thetaI entries (in litterature, that's called thetaD, the angle between the light/view and the half vector)


    //////////////////////////////////////////////////////////////////////////////

float   _DEBUG_anisotropyAngle;
float   _DEBUG_anisotropicRoughessX;
float   _DEBUG_anisotropicRoughessY;
float   _DEBUG_clearCoatIOR;






float4 _BaseColor;
float4 _BaseColorMap_ST;
float4 _BaseColorMap_TexelSize;
float4 _BaseColorMap_MipInfo;

//float3 _EmissiveColor;
//float4 _EmissiveColorMap_ST;
//float _EmissiveIntensity;
//float _AlbedoAffectEmissive;

float _AlphaCutoff;
float4 _DoubleSidedConstants;

//float _DistortionScale;
//float _DistortionVectorScale;
//float _DistortionVectorBias;
//float _DistortionBlurScale;
//float _DistortionBlurRemapMin;
//float _DistortionBlurRemapMax;


// Caution: C# code in BaseLitUI.cs call LightmapEmissionFlagsProperty() which assume that there is an existing "_EmissionColor"
// value that exist to identify if the GI emission need to be enabled.
// In our case we don't use such a mechanism but need to keep the code quiet. We declare the value and always enable it.
// TODO: Fix the code in legacy unity so we can customize the behavior for GI
float3 _EmissionColor;

CBUFFER_END
