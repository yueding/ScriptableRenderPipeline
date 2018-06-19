using System;
using UnityEngine;
using UnityEngine.Rendering;

//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------
namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class AxF : RenderPipelineMaterial
    {
        //-----------------------------------------------------------------------------
        // SurfaceData
        //-----------------------------------------------------------------------------

        // Main structure that store the user data (i.e user input of master node in material graph)
        [GenerateHLSL(PackingRules.Exact, false, true, 1500)]
        public struct SurfaceData {

            [SurfaceDataAttributes(new string[]{"Normal", "Normal View Space"}, true)]
            public Vector3  normalWS;

            [SurfaceDataAttributes("Tangent", true)]
            public Vector3  tangentWS;

            [SurfaceDataAttributes("BiTangent", true)]
            public Vector3  biTangentWS;


            //////////////////////////////////////////////////////////////////////////
            // SVBRDF Variables
            //
            [SurfaceDataAttributes("Diffuse Color", false, true)]
            public Vector3  diffuseColor;

            [SurfaceDataAttributes("Specular Color", false, true)]
            public Vector3  specularColor;

            [SurfaceDataAttributes("Fresnel F0", false, true)]
            public Vector3  fresnelF0;

            [SurfaceDataAttributes("Specular Lobe", false, true)]
            public Vector2  specularLobe;

            [SurfaceDataAttributes("Height")]
            public float    height_mm;

            [SurfaceDataAttributes("Anisotropic Angle")]
            public float    anisotropyAngle;


            //////////////////////////////////////////////////////////////////////////
            // Car Paint Variables
            //
            [SurfaceDataAttributes("Flakes UV")]
            public Vector2	flakesUV;

            [SurfaceDataAttributes("Flakes Mip")]
            public float    flakesMipLevel;
            

            //////////////////////////////////////////////////////////////////////////
            // BTF Variables
            //


            //////////////////////////////////////////////////////////////////////////
            // Clear Coat
            [SurfaceDataAttributes("Clear Coat Color")]
            public Vector3  clearCoatColor;

            [SurfaceDataAttributes("Clear Coat Normal", true, true)]
            public Vector3  clearCoatNormalWS;

            [SurfaceDataAttributes("Clear Coat IOR")]
            public float    clearCoatIOR;

        };

        //-----------------------------------------------------------------------------
        // BSDFData
        //-----------------------------------------------------------------------------

        [GenerateHLSL(PackingRules.Exact, false, true, 1600)]
        public struct BSDFData {

            [SurfaceDataAttributes(new string[] { "Normal WS", "Normal View Space" }, true)]
            public Vector3	normalWS;

            [SurfaceDataAttributes("", true)]
            public Vector3  tangentWS;

            [SurfaceDataAttributes("", true)]
            public Vector3  biTangentWS;


            //////////////////////////////////////////////////////////////////////////
            // SVBRDF Variables
            //
            [SurfaceDataAttributes("", false, true)]
            public Vector3	diffuseColor;
     
            [SurfaceDataAttributes("", false, true)]
            public Vector3	specularColor;

            [SurfaceDataAttributes("", false, true)]
            public Vector3  fresnelF0;

            [SurfaceDataAttributes("", false, true)]
            public Vector2	roughness;
  
            [SurfaceDataAttributes("", false, true)]
            public float	height_mm;

            [SurfaceDataAttributes("", false, true)]
            public float	anisotropyAngle;


            //////////////////////////////////////////////////////////////////////////
            // Car Paint Variables
            //
            [SurfaceDataAttributes("")]
            public Vector2	flakesUV;

            [SurfaceDataAttributes("Flakes Mip")]
            public float    flakesMipLevel;


            //////////////////////////////////////////////////////////////////////////
            // BTF Variables
            //


            //////////////////////////////////////////////////////////////////////////
            // Clear Coat
            //
            [SurfaceDataAttributes("", false, true)]
            public Vector3  clearCoatColor;

            [SurfaceDataAttributes("", true)]
            public Vector3  clearCoatNormalWS;
  
            [SurfaceDataAttributes("", false, true)]
            public float	clearCoatIOR;
        };

//*
        //-----------------------------------------------------------------------------
        // Init precomputed texture
        //-----------------------------------------------------------------------------
        //
        Material        m_preIntegratedFGDMaterial = null;
        RenderTexture   m_preIntegratedFGD = null;
        bool            m_preIntegratedTableAvailable = false;

        public AxF() {}

        public override void Build( HDRenderPipelineAsset hdAsset ) {
            if ( m_preIntegratedFGDMaterial != null )
                return; // Already initialized

//             PreIntegratedFGD.instance.Build();
//             LTCAreaLight.instance.Build();

            string[]  shaderGUIDs = UnityEditor.AssetDatabase.FindAssets( "PreIntegratedFGD_AxFWard" );
            if ( shaderGUIDs == null || shaderGUIDs.Length == 0 )
                throw new Exception( "Shader for Ward BRDF pre-integration not found!" );

Debug.Log( "Found " + shaderGUIDs.Length + " Ward DFG shaders!" );

            Shader  preIntegratedFGD_AxFWard = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>( UnityEditor.AssetDatabase.GUIDToAssetPath( shaderGUIDs[0] ) );
            m_preIntegratedFGDMaterial = CoreUtils.CreateEngineMaterial( preIntegratedFGD_AxFWard );

//UnityEditor.AssetPostprocessor
//UnityEditor.AssetDatabase.

            m_preIntegratedFGD = new RenderTexture( 128, 128, 0, RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Linear );
            m_preIntegratedFGD.hideFlags = HideFlags.HideAndDontSave;
            m_preIntegratedFGD.filterMode = FilterMode.Bilinear;
            m_preIntegratedFGD.wrapMode = TextureWrapMode.Clamp;
            m_preIntegratedFGD.hideFlags = HideFlags.DontSave;
            m_preIntegratedFGD.name = CoreUtils.GetRenderTargetAutoName( 128, 128, 1, RenderTextureFormat.ARGB2101010, "PreIntegratedFGD_Ward" );
            m_preIntegratedFGD.Create();
        }

        public override void Cleanup() {
//             PreIntegratedFGD.instance.Cleanup();
//             LTCAreaLight.instance.Cleanup();

Debug.Log( "Destroying Ward DFG shaders!" );

            CoreUtils.Destroy( m_preIntegratedFGDMaterial );
            CoreUtils.Destroy( m_preIntegratedFGD );
            m_preIntegratedFGD = null;
            m_preIntegratedFGDMaterial = null;
            m_preIntegratedTableAvailable = false;
        }

        public override void RenderInit(CommandBuffer cmd) {
            if ( m_preIntegratedFGDMaterial == null )
                return;
            if ( m_preIntegratedTableAvailable ) {
//Debug.Log( "****Ward DFG table already computed****" );
                return;
            }

Debug.Log( "Rendering Ward DFG table!" );

            using ( new ProfilingSample(cmd, "PreIntegratedFGD Material Generation for Ward BRDF" ) ) {
                CoreUtils.DrawFullScreen( cmd, m_preIntegratedFGDMaterial, new RenderTargetIdentifier( m_preIntegratedFGD ) );
                m_preIntegratedTableAvailable = true;
            }
        }

        public override void Bind() {
            if ( m_preIntegratedFGD == null || !m_preIntegratedTableAvailable ) {
                throw new Exception( "Ward BRDF pre-integration table not available!" );
            }

//Debug.Log( "Binding Ward DFG table!" );

//             PreIntegratedFGD.instance.Bind();
//             LTCAreaLight.instance.Bind();
            Shader.SetGlobalTexture( "_PreIntegratedFGD", m_preIntegratedFGD );
        }
//*/
    }
}
