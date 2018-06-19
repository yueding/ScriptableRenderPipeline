using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class AxFGUI : BaseUnlitGUI
    {
        protected static class Styles
        {
            public static string InputsText = "Inputs";

            /////////////////////////////////////////////////////////////////////////////////////////////////
            // SVBRDF Parameters
	        public static GUIContent	diffuseColorMapText = new GUIContent( "Diffuse Color (Gamma 2.2)" );
	        public static GUIContent	specularColorMapText = new GUIContent( "Specular Color (Gamma 2.2)" );
	        public static GUIContent	specularLobeMapText = new GUIContent( "Specular Lobe" );
	        public static GUIContent	specularLobeMapScaleText = new GUIContent( "Specular Lobe Scale" );
	        public static GUIContent	fresnelMapText = new GUIContent( "Fresnel (Gamma 2.2)" );
	        public static GUIContent	normalMapText = new GUIContent( "Normal" );

            // Opacity
	        public static GUIContent	opacityMapText = new GUIContent( "Opacity" );

            // Displacement
	        public static GUIContent	heightMapText = new GUIContent( "Height" );

            // Anisotropy
	        public static GUIContent	anisotropyRotationAngleMapText = new GUIContent( "Anisotropy Angle" );

            /////////////////////////////////////////////////////////////////////////////////////////////////
            // Car Paint Parameters
	        public static GUIContent	BRDFColorMapText = new GUIContent( "BRDF Color (Gamma 2.2)" );
	        public static GUIContent	BRDFColorMapScaleText = new GUIContent( "BRDF Color Scale" );

	        public static GUIContent	BTFFlakesMapText = new GUIContent( "BTF Flakes Color (Gamma 2.2) Texture2DArray" );
	        public static GUIContent	BTFFlakesMapScaleText = new GUIContent( "BTF Flakes Scale" );
	        public static GUIContent	FlakesTilingText = new GUIContent( "Flakes Tiling" );

	        public static GUIContent	thetaFI_sliceLUTMapText = new GUIContent( "ThetaFI Slice LUT" );

	        public static GUIContent	CarPaintIORText = new GUIContent( "IOR" );

            /////////////////////////////////////////////////////////////////////////////////////////////////
            // Generic
  
            // Clear-coat
	        public static GUIContent	clearCoatColorMapText = new GUIContent( "Clear-Coat Color (Gamma 2.2)" );
	        public static GUIContent	clearCoatNormalMapText = new GUIContent( "Clear-Coat Normal" );
	        public static GUIContent	clearCoatIORMapText = new GUIContent( "Clear-Coat IOR" );
        }

        enum	AXF_BRDF_TYPE {
            SVBRDF,
            CAR_PAINT,
            BTF,
        }
        static readonly string[]	AxFBRDFTypeNames = Enum.GetNames(typeof(AXF_BRDF_TYPE));

        enum    SVBRDF_DIFFUSE_TYPE {
	        LAMBERT = 0,
	        OREN_NAYAR = 1,
//			DISNEY,
        }
        static readonly string[]	SVBRDF_DIFFUSE_TYPENames = Enum.GetNames(typeof(SVBRDF_DIFFUSE_TYPE));

        enum    SVBRDF_SPECULAR_TYPE {
            WARD = 0,
            BLINN_PHONG = 1,
            COOK_TORRANCE = 2,
            GGX = 3,
            PHONG = 4,
        }
        static readonly string[]	SVBRDF_SPECULAR_TYPENames = Enum.GetNames(typeof(SVBRDF_SPECULAR_TYPE));

        enum    SVBRDF_SPECULAR_VARIANT_WARD {  // Ward variants
            GEISLERMORODER,		// 2010 (albedo-conservative, should always be preferred!)
            DUER,				// 2006
            WARD,				// 1992 (original paper)
        }
        static readonly string[]	SVBRDF_SPECULAR_VARIANT_WARDNames = Enum.GetNames(typeof(SVBRDF_SPECULAR_VARIANT_WARD));
        enum    SVBRDF_SPECULAR_VARIANT_BLINN { // Blinn-Phong variants
            ASHIKHMIN_SHIRLEY,	// 2000
            BLINN,				// 1977 (original paper)
            VRAY,
            LEWIS,				// 1993
        }
        static readonly string[]	SVBRDF_SPECULAR_VARIANT_BLINNNames = Enum.GetNames(typeof(SVBRDF_SPECULAR_VARIANT_BLINN));

        enum    SVBRDF_FRESNEL_VARIANT {
            NO_FRESNEL,			// No fresnel
            FRESNEL,			// Full fresnel (1818)
            SCHLICK,			// Schlick's Approximation (1994)
        }
        static readonly string[]	SVBRDF_FRESNEL_VARIANTNames = Enum.GetNames(typeof(SVBRDF_FRESNEL_VARIANT));

        /////////////////////////////////////////////////////////////////////////////////////////////////
        // Generic Parameters
        static string               m_materialSizeU_mmText = "_materialSizeU_mm";
        protected MaterialProperty  m_materialSizeU_mm;
        static string               m_materialSizeV_mmText = "_materialSizeV_mm";
        protected MaterialProperty  m_materialSizeV_mm;

        static string               m_AxF_BRDFTypeText = "_AxF_BRDFType";
        protected MaterialProperty  m_AxF_BRDFType = null;

        static string               m_flagsText = "_flags";
        protected MaterialProperty  m_flags;

        /////////////////////////////////////////////////////////////////////////////////////////////////
        // SVBRDF Parameters
        static string               m_SVBRDF_BRDFTypeText = "_SVBRDF_BRDFType";
        protected MaterialProperty  m_SVBRDF_BRDFType;
        static string               m_SVBRDF_BRDFVariantsText = "_SVBRDF_BRDFVariants";
        protected MaterialProperty  m_SVBRDF_BRDFVariants;
        static string               m_SVBRDF_heightMapMax_mmText = "_SVBRDF_heightMapMax_mm";
        protected MaterialProperty  m_SVBRDF_heightMapMax_mm;

        // Regular maps
        static string               m_diffuseColorMapText = "_SVBRDF_DiffuseColorMap_sRGB";
        protected MaterialProperty  m_diffuseColorMap = null;
        static string               m_specularColorMapText = "_SVBRDF_SpecularColorMap_sRGB";
		protected MaterialProperty  m_specularColorMap = null;

        static string               m_specularLobeMapText = "_SVBRDF_SpecularLobeMap";
		protected MaterialProperty	m_specularLobeMap = null;
        static string               m_specularLobeMap_ScaleText = "_SVBRDF_SpecularLobeMap_Scale";
        protected MaterialProperty  m_specularLobeMap_Scale;

        static string               m_fresnelMapText = "_SVBRDF_FresnelMap_sRGB";
		protected MaterialProperty	m_fresnelMap = null;
        static string               m_normalMapText = "_SVBRDF_NormalMap";
		protected MaterialProperty	m_normalMap = null;

        // Opacity
        static string               m_opacityMapText = "_SVBRDF_OpacityMap";
		protected MaterialProperty	m_opacityMap = null;

        // Displacement
        static string               m_heightMapText = "_SVBRDF_HeightMap";
		protected MaterialProperty	m_heightMap = null;

        // Anisotropy
        static string               m_anisotropicRotationAngleMapText = "_SVBRDF_AnisotropicRotationAngleMap";
		protected MaterialProperty	m_anisotropicRotationAngleMap = null;


        /////////////////////////////////////////////////////////////////////////////////////////////////
        // Car Paint Parameters
        static string               m_CarPaint_BRDFColorMap_sRGBText = "_CarPaint_BRDFColorMap_sRGB";
        protected MaterialProperty  m_CarPaint_BRDFColorMap_sRGB = null;

        static string               m_CarPaint_BRDFColorMap_ScaleText = "_CarPaint_BRDFColorMap_Scale";
        protected MaterialProperty  m_CarPaint_BRDFColorMap_Scale;

        static string               m_CarPaint_BTFFlakesMap_sRGBText = "_CarPaint_BTFFlakesMap_sRGB";
        protected MaterialProperty  m_CarPaint_BTFFlakesMap_sRGB = null;

        static string               m_CarPaint_BTFFlakesMap_ScaleText = "_CarPaint_BTFFlakesMap_Scale";
        protected MaterialProperty  m_CarPaint_BTFFlakesMap_Scale;

        static string               m_CarPaint_FlakesTilingText = "_CarPaint_FlakesTiling";
        protected MaterialProperty  m_CarPaint_FlakesTiling;

        static string               m_CarPaint_thetaFI_sliceLUTMapText = "_CarPaint_thetaFI_sliceLUTMap";
        protected MaterialProperty  m_CarPaint_thetaFI_sliceLUTMap;

        static string               m_CarPaint_maxThetaIText = "_CarPaint_maxThetaI";
        protected MaterialProperty  m_CarPaint_maxThetaI;
        static string               m_CarPaint_numThetaFText = "_CarPaint_numThetaF";
        protected MaterialProperty  m_CarPaint_numThetaF;
        static string               m_CarPaint_numThetaIText = "_CarPaint_numThetaI";
        protected MaterialProperty  m_CarPaint_numThetaI;

        static string               m_CarPaint_IORText = "_CarPaint_IOR";
        protected MaterialProperty  m_CarPaint_IOR;

        /////////////////////////////////////////////////////////////////////////////////////////////////
        // Clear-Coat
        static string               m_clearCoatColorMapText = "_SVBRDF_ClearCoatColorMap_sRGB";
		protected MaterialProperty	m_clearCoatColorMap = null;
        static string               m_clearCoatNormalMapText = "_SVBRDF_ClearCoatNormalMap";
		protected MaterialProperty	m_clearCoatNormalMap = null;
        static string               m_clearCoatIORMapText = "_SVBRDF_ClearCoatIORMap_sRGB";
		protected MaterialProperty	m_clearCoatIORMap = null;


// Exemple de lit.shader pour gérer les enums de shader variants
//     #pragma shader_feature _ _BLENDMODE_ALPHA _BLENDMODE_ADD _BLENDMODE_PRE_MULTIPLY

MaterialProperty    m_debug_prop0;
MaterialProperty    m_debug_prop1;
MaterialProperty    m_debug_prop2;
MaterialProperty    m_debug_prop3;
MaterialProperty    m_debug_prop4;

		override protected void FindMaterialProperties( MaterialProperty[] props ) {

            m_materialSizeU_mm = FindProperty( m_materialSizeU_mmText, props );
            m_materialSizeV_mm = FindProperty( m_materialSizeV_mmText, props );

 			m_AxF_BRDFType = FindProperty( m_AxF_BRDFTypeText, props );

            m_flags = FindProperty( m_flagsText, props );

            //////////////////////////////////////////////////////////////////////////
            // SVBRDF
            m_SVBRDF_BRDFType = FindProperty( m_SVBRDF_BRDFTypeText, props );
            m_SVBRDF_BRDFVariants = FindProperty( m_SVBRDF_BRDFVariantsText, props );
            m_SVBRDF_heightMapMax_mm = FindProperty( m_SVBRDF_heightMapMax_mmText, props );

            // Regular maps
			m_diffuseColorMap = FindProperty( m_diffuseColorMapText, props );
			m_specularColorMap = FindProperty( m_specularColorMapText, props );
			m_specularLobeMap = FindProperty( m_specularLobeMapText, props );
            m_specularLobeMap_Scale = FindProperty( m_specularLobeMap_ScaleText, props );
			m_fresnelMap = FindProperty( m_fresnelMapText, props );
			m_normalMap = FindProperty( m_normalMapText, props );

            // Opacity
			m_opacityMap = FindProperty( m_opacityMapText, props );

            // Displacement
			m_heightMap = FindProperty( m_heightMapText, props );

            // Anisotropy
			m_anisotropicRotationAngleMap = FindProperty( m_anisotropicRotationAngleMapText, props );


            //////////////////////////////////////////////////////////////////////////
            // Car Paint
			m_CarPaint_BRDFColorMap_sRGB = FindProperty( m_CarPaint_BRDFColorMap_sRGBText, props );
			m_CarPaint_BTFFlakesMap_sRGB = FindProperty( m_CarPaint_BTFFlakesMap_sRGBText, props );
			m_CarPaint_thetaFI_sliceLUTMap = FindProperty( m_CarPaint_thetaFI_sliceLUTMapText, props );

            m_CarPaint_BRDFColorMap_Scale = FindProperty( m_CarPaint_BRDFColorMap_ScaleText, props );
            m_CarPaint_BTFFlakesMap_Scale = FindProperty( m_CarPaint_BTFFlakesMap_ScaleText, props );
            m_CarPaint_FlakesTiling = FindProperty( m_CarPaint_FlakesTilingText, props );

            m_CarPaint_maxThetaI = FindProperty( m_CarPaint_maxThetaIText, props );
            m_CarPaint_numThetaF = FindProperty( m_CarPaint_numThetaFText, props );
            m_CarPaint_numThetaI = FindProperty( m_CarPaint_numThetaIText, props );

            m_CarPaint_IOR = FindProperty( m_CarPaint_IORText, props );

            //////////////////////////////////////////////////////////////////////////
            // Clear-Coat
			m_clearCoatColorMap = FindProperty( m_clearCoatColorMapText, props );
			m_clearCoatNormalMap = FindProperty( m_clearCoatNormalMapText, props );
			m_clearCoatIORMap = FindProperty( m_clearCoatIORMapText, props );



m_debug_prop0 = FindProperty( "_DEBUG_anisotropyAngle", props );
m_debug_prop1 = FindProperty( "_DEBUG_anisotropicRoughessX", props );
m_debug_prop2 = FindProperty( "_DEBUG_anisotropicRoughessY", props );
m_debug_prop3 = FindProperty( "_DEBUG_clearCoatIOR", props );
		}

	    protected unsafe override void MaterialPropertiesGUI( Material _material ) {



m_debug_prop0.floatValue = EditorGUILayout.FloatField( "Anisotropy Angle", m_debug_prop0.floatValue * 180.0f / Mathf.PI ) * Mathf.PI / 180.0f;
m_debug_prop1.floatValue = EditorGUILayout.FloatField( "Anisotropic Roughness X", m_debug_prop1.floatValue );
m_debug_prop2.floatValue = EditorGUILayout.FloatField( "Anisotropic Roughness Y", m_debug_prop2.floatValue );
m_debug_prop3.floatValue = EditorGUILayout.FloatField( "Clear Coat IOR", m_debug_prop3.floatValue );
//m_MaterialEditor.ShaderProperty( m_debug_prop0,  );
//m_MaterialEditor.ShaderProperty( m_debug_prop1, "Anisotropy Roughness X" );
//m_MaterialEditor.ShaderProperty( m_debug_prop2, "Anisotropy Roughness Y" );
//m_MaterialEditor.ShaderProperty( m_debug_prop3, "Clear Coat IOR" );




		    EditorGUILayout.LabelField( Styles.InputsText, EditorStyles.boldLabel );

            m_MaterialEditor.ShaderProperty( m_materialSizeU_mm, "Material Size U (mm)" );
            m_MaterialEditor.ShaderProperty( m_materialSizeV_mm, "Material Size V (mm)" );

            AXF_BRDF_TYPE	AxF_BRDFType = (AXF_BRDF_TYPE) m_AxF_BRDFType.floatValue;
                            AxF_BRDFType = (AXF_BRDF_TYPE) EditorGUILayout.Popup( "BRDF Type", (int) AxF_BRDFType, AxFBRDFTypeNames );
		    m_AxF_BRDFType.floatValue = (float) AxF_BRDFType;

            switch ( AxF_BRDFType ) {
                case AXF_BRDF_TYPE.SVBRDF: {
                    EditorGUILayout.Space();
                    ++EditorGUI.indentLevel;

                    // Read as compact flags
                    uint    flags = (uint) m_flags.floatValue;
                    uint    BRDFType = (uint) m_SVBRDF_BRDFType.floatValue;
                    uint    BRDFVariants = (uint) m_SVBRDF_BRDFVariants.floatValue;

                    SVBRDF_DIFFUSE_TYPE diffuseType = (SVBRDF_DIFFUSE_TYPE) (BRDFType & 0x1);
                    SVBRDF_SPECULAR_TYPE specularType = (SVBRDF_SPECULAR_TYPE) ((BRDFType >> 1) & 0x7);
                    SVBRDF_FRESNEL_VARIANT fresnelVariant = (SVBRDF_FRESNEL_VARIANT) (BRDFVariants & 0x3);
                    SVBRDF_SPECULAR_VARIANT_WARD wardVariant = (SVBRDF_SPECULAR_VARIANT_WARD) ((BRDFVariants >> 2) & 0x3);
                    SVBRDF_SPECULAR_VARIANT_BLINN blinnVariant = (SVBRDF_SPECULAR_VARIANT_BLINN) ((BRDFVariants >> 4) & 0x3);

                    // Expand as user-friendly UI
//                     EditorGUILayout.LabelField( "Flags", EditorStyles.boldLabel );
                    EditorGUILayout.LabelField( "BRDF Variants", EditorStyles.boldLabel );

                    diffuseType = (SVBRDF_DIFFUSE_TYPE) EditorGUILayout.Popup( "Diffuse Type", (int) diffuseType, SVBRDF_DIFFUSE_TYPENames );
                    specularType = (SVBRDF_SPECULAR_TYPE) EditorGUILayout.Popup( "Specular Type", (int) specularType, SVBRDF_SPECULAR_TYPENames );

                    if ( specularType == SVBRDF_SPECULAR_TYPE.WARD ) {
                        fresnelVariant = (SVBRDF_FRESNEL_VARIANT) EditorGUILayout.Popup( "Fresnel Variant", (int) fresnelVariant, SVBRDF_FRESNEL_VARIANTNames );
                        wardVariant = (SVBRDF_SPECULAR_VARIANT_WARD) EditorGUILayout.Popup( "Ward Variant", (int) wardVariant, SVBRDF_SPECULAR_VARIANT_WARDNames );
                    } else if ( specularType == SVBRDF_SPECULAR_TYPE.BLINN_PHONG ) {
                        blinnVariant = (SVBRDF_SPECULAR_VARIANT_BLINN) EditorGUILayout.Popup( "Blinn Variant", (int) blinnVariant, SVBRDF_SPECULAR_VARIANT_BLINNNames );
                    }

                    // Regular maps
                    m_MaterialEditor.TexturePropertySingleLine( Styles.diffuseColorMapText, m_diffuseColorMap );
                    m_MaterialEditor.TexturePropertySingleLine( Styles.specularColorMapText, m_specularColorMap );
                    m_MaterialEditor.TexturePropertySingleLine( Styles.specularLobeMapText, m_specularLobeMap );
                    m_specularLobeMap_Scale.floatValue = EditorGUILayout.FloatField( Styles.specularLobeMapScaleText, m_specularLobeMap_Scale.floatValue );
                    m_MaterialEditor.TexturePropertySingleLine( Styles.fresnelMapText, m_fresnelMap );
                    m_MaterialEditor.TexturePropertySingleLine( Styles.normalMapText, m_normalMap );

                    // Opacity
                    m_MaterialEditor.TexturePropertySingleLine( Styles.opacityMapText, m_opacityMap );

                    // Displacement
                    bool    useDisplacementMap = EditorGUILayout.Toggle( "Enable Displacement Map", (flags & 8) != 0 );
                    if ( useDisplacementMap ) {
                        ++EditorGUI.indentLevel;
                        m_MaterialEditor.TexturePropertySingleLine( Styles.heightMapText, m_heightMap );
                        m_MaterialEditor.ShaderProperty( m_SVBRDF_heightMapMax_mm, "Max Displacement (mm)" );
                        --EditorGUI.indentLevel;
                    }

                    // Anisotropy
                    bool    isAnisotropic = EditorGUILayout.Toggle( "Is Anisotropic", (flags & 1) != 0 );
                    if ( isAnisotropic ) {
                        ++EditorGUI.indentLevel;
                        m_MaterialEditor.TexturePropertySingleLine( Styles.anisotropyRotationAngleMapText, m_anisotropicRotationAngleMap );
                        --EditorGUI.indentLevel;
                    }

                    // Clear-coat
                    bool    hasClearCoat = EditorGUILayout.Toggle( "Enable Clear-Coat", (flags & 2) != 0 );
                    bool    clearCoatUsesRefraction = (flags & 4) != 0;
                    if ( hasClearCoat ) {
                        ++EditorGUI.indentLevel;
                        m_MaterialEditor.TexturePropertySingleLine( Styles.clearCoatColorMapText, m_clearCoatColorMap );
                        m_MaterialEditor.TexturePropertySingleLine( Styles.clearCoatNormalMapText, m_clearCoatNormalMap );
                        clearCoatUsesRefraction = EditorGUILayout.Toggle( "Enable Refraction", clearCoatUsesRefraction );
                        if ( clearCoatUsesRefraction ) {
                            ++EditorGUI.indentLevel;
                            m_MaterialEditor.TexturePropertySingleLine( Styles.clearCoatIORMapText, m_clearCoatIORMap );
                            --EditorGUI.indentLevel;
                        }
                        --EditorGUI.indentLevel;
                    }

                    // Write back as compact flags
                    flags = 0;
                    flags |= isAnisotropic ? 1U : 0U;
                    flags |= hasClearCoat ? 2U : 0U;
                    flags |= clearCoatUsesRefraction ? 4U : 0U;
                    flags |= useDisplacementMap ? 8U : 0U;
                    
                    BRDFType = 0;
                    BRDFType |= (uint) diffuseType;
                    BRDFType |= ((uint) specularType) << 1;

                    BRDFVariants = 0;
                    BRDFVariants |= (uint) fresnelVariant;
                    BRDFVariants |= ((uint) wardVariant) << 2;
                    BRDFVariants |= ((uint) blinnVariant) << 4;

//                    cmd.SetGlobalFloat( HDShaderIDs._TexturingModeFlags, *(float*) &texturingModeFlags );
                    m_flags.floatValue = (float) flags;
                    m_SVBRDF_BRDFType.floatValue = (float) BRDFType;
                    m_SVBRDF_BRDFVariants.floatValue = (float) BRDFVariants;

                    --EditorGUI.indentLevel;
                    break;
                }

                case AXF_BRDF_TYPE.CAR_PAINT: {
                    EditorGUILayout.Space();
                    ++EditorGUI.indentLevel;

                    // Read as compact flags
                    uint    flags = (uint) m_flags.floatValue;

                    bool    isAnisotropic = false;
                    bool    useDisplacementMap = false;

                    // Expand as user-friendly UI

                    // Regular maps
                    m_MaterialEditor.TexturePropertySingleLine( Styles.BRDFColorMapText, m_CarPaint_BRDFColorMap_sRGB );
                    m_CarPaint_BRDFColorMap_Scale.floatValue = EditorGUILayout.FloatField( Styles.BRDFColorMapScaleText, m_CarPaint_BRDFColorMap_Scale.floatValue );

                    m_MaterialEditor.TexturePropertySingleLine( Styles.BTFFlakesMapText, m_CarPaint_BTFFlakesMap_sRGB );
//EditorGUILayout.LabelField( "Texture Dimension = " + m_CarPaint_BTFFlakesMap_sRGB.textureDimension );
//EditorGUILayout.LabelField( "Texture Format = " + m_CarPaint_BTFFlakesMap_sRGB.textureValue. );
                    m_CarPaint_BTFFlakesMap_Scale.floatValue = EditorGUILayout.FloatField( Styles.BTFFlakesMapScaleText, m_CarPaint_BTFFlakesMap_Scale.floatValue );
                    m_CarPaint_FlakesTiling.floatValue = EditorGUILayout.FloatField( Styles.FlakesTilingText, m_CarPaint_FlakesTiling.floatValue );

                    m_MaterialEditor.TexturePropertySingleLine( Styles.thetaFI_sliceLUTMapText, m_CarPaint_thetaFI_sliceLUTMap );

// m_CarPaint_maxThetaI = FindProperty( m_CarPaint_maxThetaIText, props );
// m_CarPaint_numThetaF = FindProperty( m_CarPaint_numThetaFText, props );
// m_CarPaint_numThetaI = FindProperty( m_CarPaint_numThetaIText, props );


                    // Clear-coat
                    bool    hasClearCoat = EditorGUILayout.Toggle( "Enable Clear-Coat", (flags & 2) != 0 );
                    bool    clearCoatUsesRefraction = (flags & 4) != 0;
                    if ( hasClearCoat ) {
                        ++EditorGUI.indentLevel;
//                        m_MaterialEditor.TexturePropertySingleLine( Styles.clearCoatColorMapText, m_clearCoatColorMap );
                        m_MaterialEditor.TexturePropertySingleLine( Styles.clearCoatNormalMapText, m_clearCoatNormalMap );
                        clearCoatUsesRefraction = EditorGUILayout.Toggle( "Enable Refraction", clearCoatUsesRefraction );
                        if ( clearCoatUsesRefraction ) {
                            ++EditorGUI.indentLevel;
//                            m_MaterialEditor.TexturePropertySingleLine( Styles.clearCoatIORMapText, m_clearCoatIORMap );
                            m_CarPaint_IOR.floatValue = EditorGUILayout.FloatField( Styles.CarPaintIORText, m_CarPaint_IOR.floatValue );
                            --EditorGUI.indentLevel;
                        }
                        --EditorGUI.indentLevel;
                    }

                    // Write back as compact flags
                    flags = 0;
                    flags |= isAnisotropic ? 1U : 0U;
                    flags |= hasClearCoat ? 2U : 0U;
                    flags |= clearCoatUsesRefraction ? 4U : 0U;
                    flags |= useDisplacementMap ? 8U : 0U;

//                    cmd.SetGlobalFloat( HDShaderIDs._TexturingModeFlags, *(float*) &texturingModeFlags );
                    m_flags.floatValue = (float) flags;

                    --EditorGUI.indentLevel;
                    break;
                }
            }


//          m_MaterialEditor.TexturePropertySingleLine( Styles.baseColorText, m_baseColorMap, m_baseColor );
//          m_MaterialEditor.TextureScaleOffsetProperty( m_baseColorMap );

//          m_MaterialEditor.TexturePropertySingleLine(Styles.emissiveText, emissiveColorMap, emissiveColor);
//          m_MaterialEditor.TextureScaleOffsetProperty(emissiveColorMap);
//          m_MaterialEditor.ShaderProperty(emissiveIntensity, Styles.emissiveIntensityText);
//          m_MaterialEditor.ShaderProperty(albedoAffectEmissive, Styles.albedoAffectEmissiveText);

//             var surfaceTypeValue = (SurfaceType) surfaceType.floatValue;
//             if ( surfaceTypeValue == SurfaceType.Transparent ) {
//                 EditorGUILayout.Space();
//                 EditorGUILayout.LabelField( StylesBaseUnlit.TransparencyInputsText, EditorStyles.boldLabel );
//                 ++EditorGUI.indentLevel;
//      
//                 DoDistortionInputsGUI();
//      
//                 --EditorGUI.indentLevel;
//                }
        }

        protected override void MaterialPropertiesAdvanceGUI( Material _material ) {
        }

        protected override void VertexAnimationPropertiesGUI() {

        }

        protected override bool ShouldEmissionBeEnabled( Material _material ) {
           return false;//_material.GetFloat(kEmissiveIntensity) > 0.0f;
        }

        protected override void SetupMaterialKeywordsAndPassInternal( Material _material ) {
            SetupMaterialKeywordsAndPass( _material );
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static public void SetupMaterialKeywordsAndPass( Material _material ) {
            SetupBaseUnlitKeywords( _material );
            SetupBaseUnlitMaterialPass( _material );

//          CoreUtils.SetKeyword(_material, "_EMISSIVE_COLOR_MAP", _material.GetTexture(kEmissiveColorMap));

            AXF_BRDF_TYPE   BRDFType = (AXF_BRDF_TYPE) _material.GetFloat( m_AxF_BRDFTypeText );

            CoreUtils.SetKeyword( _material, "_AXF_BRDF_TYPE_SVBRDF", BRDFType == AXF_BRDF_TYPE.SVBRDF );
            CoreUtils.SetKeyword( _material, "_AXF_BRDF_TYPE_CAR_PAINT", BRDFType == AXF_BRDF_TYPE.CAR_PAINT );
            CoreUtils.SetKeyword( _material, "_AXF_BRDF_TYPE_BTF", BRDFType == AXF_BRDF_TYPE.BTF );
		}
	}
} // namespace UnityEditor
