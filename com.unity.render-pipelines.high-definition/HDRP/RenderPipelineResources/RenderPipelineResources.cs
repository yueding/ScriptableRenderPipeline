using System;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class RenderPipelineResources : ScriptableObject
    {
        const int currentVersion = 4;
        [SerializeField]
        [FormerlySerializedAs("version")]
        int m_Version = 1;

        [Serializable]
        public sealed class ShaderResources
        {
            // Defaults
            public Shader defaultPS;

            // Debug
            public Shader debugDisplayLatlongPS;
            public Shader debugViewMaterialGBufferPS;
            public Shader debugViewTilesPS;
            public Shader debugFullScreenPS;
            public Shader debugColorPickerPS;
            public Shader debugLightVolumePS;

            // Lighting
            public Shader deferredPS;
            public ComputeShader colorPyramidCS;
            public ComputeShader depthPyramidCS;
            public ComputeShader copyChannelCS;
            public ComputeShader texturePaddingCS;
            public ComputeShader applyDistortionCS;
            public ComputeShader screenSpaceReflectionsCS;

            // Lighting tile pass
            public ComputeShader clearDispatchIndirectCS;
            public ComputeShader buildDispatchIndirectCS;
            public ComputeShader buildScreenAABBCS;
            public ComputeShader buildPerTileLightListCS;               // FPTL
            public ComputeShader buildPerBigTileLightListCS;
            public ComputeShader buildPerVoxelLightListCS;              // clustered
            public ComputeShader buildMaterialFlagsCS;
            public ComputeShader deferredCS;
            public ComputeShader screenSpaceShadowCS;
            public ComputeShader volumeVoxelizationCS;
            public ComputeShader volumetricLightingCS;

            public ComputeShader subsurfaceScatteringCS;                // Disney SSS
            public Shader subsurfaceScatteringPS;                       // Jimenez SSS
            public Shader combineLightingPS;

            // General
            public Shader cameraMotionVectorsPS;
            public Shader copyStencilBufferPS;
            public Shader copyDepthBufferPS;
            public Shader blitPS;

            // Sky
            public Shader blitCubemapPS;
            public ComputeShader buildProbabilityTablesCS;
            public ComputeShader computeGgxIblSampleDataCS;
            public Shader GGXConvolvePS;
            public Shader opaqueAtmosphericScatteringPS;
            public Shader hdriSkyPS;
            public Shader integrateHdriSkyPS;
            public Shader proceduralSkyPS;
            public Shader skyboxCubemapPS;
            public Shader gradientSkyPS;

            // Material
            public Shader preIntegratedFGD_GGXDisneyDiffusePS;
            public Shader preIntegratedFGD_CharlieFabricLambertPS;

            // Utilities / Core
            public ComputeShader encodeBC6HCS;
            public Shader cubeToPanoPS;
            public Shader blitCubeTextureFacePS;

            // Shadow
            public Shader shadowClearPS;
            public ComputeShader shadowBlurMomentsCS;
            public Shader debugShadowMapPS;

            // Decal
            public Shader decalNormalBufferPS;
        }

        [Serializable]
        public sealed class MaterialResources
        {
            // Defaults
            public Material defaultDiffuseMat;
            public Material defaultMirrorMat;
            public Material defaultDecalMat;
            public Material defaultTerrainMat;
        }

        [Serializable]
        public sealed class TextureResources
        {
            // Debug
            public Texture2D debugFontTex;
        }

        public ShaderResources shaders;
        public MaterialResources materials;
        public TextureResources textures;

#if UNITY_EDITOR
        public void UpgradeIfNeeded()
        {
            if (m_Version != currentVersion)
            {
                Init();

                m_Version = currentVersion;
            }
        }

        // Note: move this to a static using once we can target C#6+
        T Load<T>(string path) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        public void Init()
        {
            // Load default renderPipelineResources / Material / Shader
            string HDRenderPipelinePath = HDUtils.GetHDRenderPipelinePath();
            string CorePath = HDUtils.GetCorePath();

            // Shaders
            shaders = new ShaderResources
            {
                // Defaults
                defaultPS = Load<Shader>(HDRenderPipelinePath + "Material/Lit/Lit.shader"),

                // Debug
                debugDisplayLatlongPS = Load<Shader>(HDRenderPipelinePath + "Debug/DebugDisplayLatlong.Shader"),
                debugViewMaterialGBufferPS = Load<Shader>(HDRenderPipelinePath + "Debug/DebugViewMaterialGBuffer.Shader"),
                debugViewTilesPS = Load<Shader>(HDRenderPipelinePath + "Debug/DebugViewTiles.Shader"),
                debugFullScreenPS = Load<Shader>(HDRenderPipelinePath + "Debug/DebugFullScreen.Shader"),
                debugColorPickerPS = Load<Shader>(HDRenderPipelinePath + "Debug/DebugColorPicker.Shader"),
                debugLightVolumePS = Load<Shader>(HDRenderPipelinePath + "Debug/DebugLightVolume.Shader"),

                // Lighting
                deferredPS = Load<Shader>(HDRenderPipelinePath + "Lighting/Deferred.Shader"),
                colorPyramidCS = Load<ComputeShader>(HDRenderPipelinePath + "RenderPipelineResources/ColorPyramid.compute"),
                depthPyramidCS = Load<ComputeShader>(HDRenderPipelinePath + "RenderPipelineResources/DepthPyramid.compute"),
                copyChannelCS = Load<ComputeShader>(HDRenderPipelinePath + "CoreRP/CoreResources/GPUCopy.compute"),
                texturePaddingCS = Load<ComputeShader>(HDRenderPipelinePath + "CoreRP/CoreResources/TexturePadding.compute"),
                applyDistortionCS = Load<ComputeShader>(HDRenderPipelinePath + "RenderPipelineResources/ApplyDistorsion.compute"),
                screenSpaceReflectionsCS = Load<ComputeShader>(HDRenderPipelinePath + "RenderPipelineResources/ScreenSpaceReflections.compute"),

                // Lighting tile pass
                clearDispatchIndirectCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/cleardispatchindirect.compute"),
                buildDispatchIndirectCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/builddispatchindirect.compute"),
                buildScreenAABBCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/scrbound.compute"),
                buildPerTileLightListCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/lightlistbuild.compute"),
                buildPerBigTileLightListCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/lightlistbuild-bigtile.compute"),
                buildPerVoxelLightListCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/lightlistbuild-clustered.compute"),
                buildMaterialFlagsCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/materialflags.compute"),
                deferredCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/Deferred.compute"),

                screenSpaceShadowCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/ScreenSpaceShadow.compute"),
                volumeVoxelizationCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/Volumetrics/VolumeVoxelization.compute"),
                volumetricLightingCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/Volumetrics/VolumetricLighting.compute"),

                subsurfaceScatteringCS = Load<ComputeShader>(HDRenderPipelinePath + "Material/SubsurfaceScattering/SubsurfaceScattering.compute"),
                subsurfaceScatteringPS = Load<Shader>(HDRenderPipelinePath + "Material/SubsurfaceScattering/SubsurfaceScattering.shader"),
                combineLightingPS = Load<Shader>(HDRenderPipelinePath + "Material/SubsurfaceScattering/CombineLighting.shader"),

                // General
                cameraMotionVectorsPS = Load<Shader>(HDRenderPipelinePath + "RenderPipelineResources/CameraMotionVectors.shader"),
                copyStencilBufferPS = Load<Shader>(HDRenderPipelinePath + "RenderPipelineResources/CopyStencilBuffer.shader"),
                copyDepthBufferPS = Load<Shader>(HDRenderPipelinePath + "RenderPipelineResources/CopyDepthBuffer.shader"),
                blitPS = Load<Shader>(HDRenderPipelinePath + "RenderPipelineResources/Blit.shader"),

                // Sky
                blitCubemapPS = Load<Shader>(HDRenderPipelinePath + "Sky/BlitCubemap.shader"),
                buildProbabilityTablesCS = Load<ComputeShader>(HDRenderPipelinePath + "Material/GGXConvolution/BuildProbabilityTables.compute"),
                computeGgxIblSampleDataCS = Load<ComputeShader>(HDRenderPipelinePath + "Material/GGXConvolution/ComputeGgxIblSampleData.compute"),
                GGXConvolvePS = Load<Shader>(HDRenderPipelinePath + "Material/GGXConvolution/GGXConvolve.shader"),
                opaqueAtmosphericScatteringPS = Load<Shader>(HDRenderPipelinePath + "Lighting/AtmosphericScattering/OpaqueAtmosphericScattering.shader"),
                hdriSkyPS = Load<Shader>(HDRenderPipelinePath + "Sky/HDRISky/HDRISky.shader"),
                integrateHdriSkyPS = Load<Shader>(HDRenderPipelinePath + "Sky/HDRISky/IntegrateHDRISky.shader"),
                proceduralSkyPS = Load<Shader>(HDRenderPipelinePath + "Sky/ProceduralSky/ProceduralSky.shader"),
                gradientSkyPS = Load<Shader>(HDRenderPipelinePath + "Sky/GradientSky/GradientSky.shader"),

                // Skybox/Cubemap is a builtin shader, must use Shader.Find to access it. It is fine because we are in the editor
                skyboxCubemapPS = Shader.Find("Skybox/Cubemap"),

                // Material
                preIntegratedFGD_GGXDisneyDiffusePS = Load<Shader>(HDRenderPipelinePath + "Material/PreIntegratedFGD/PreIntegratedFGD_GGXDisneyDiffuse.shader"),
                preIntegratedFGD_CharlieFabricLambertPS = Load<Shader>(HDRenderPipelinePath + "Material/PreIntegratedFGD/PreIntegratedFGD_CharlieFabricLambert.shader"),

                // Utilities / Core
                encodeBC6HCS = Load<ComputeShader>(HDRenderPipelinePath + "CoreRP/CoreResources/EncodeBC6H.compute"),
                cubeToPanoPS = Load<Shader>(HDRenderPipelinePath + "CoreRP/CoreResources/CubeToPano.shader"),
                blitCubeTextureFacePS = Load<Shader>(HDRenderPipelinePath + "CoreRP/CoreResources/BlitCubeTextureFace.shader"),

                // Shadow
                shadowClearPS = Load<Shader>(HDRenderPipelinePath + "CoreRP/Shadow/ShadowClear.shader"),
                shadowBlurMomentsCS = Load<ComputeShader>(HDRenderPipelinePath + "CoreRP/Shadow/ShadowBlurMoments.compute"),
                debugShadowMapPS = Load<Shader>(HDRenderPipelinePath + "CoreRP/Shadow/DebugDisplayShadowMap.shader"),

                // Decal
                decalNormalBufferPS = Load<Shader>(HDRenderPipelinePath + "Material/Decal/DecalNormalBuffer.shader"),
            };

            // Materials
            materials = new MaterialResources
            {
                // Defaults
                defaultDiffuseMat = Load<Material>(HDRenderPipelinePath + "RenderPipelineResources/DefaultHDMaterial.mat"),
                defaultMirrorMat = Load<Material>(HDRenderPipelinePath + "RenderPipelineResources/DefaultHDMirrorMaterial.mat"),
                defaultDecalMat = Load<Material>(HDRenderPipelinePath + "RenderPipelineResources/DefaultHDDecalMaterial.mat"),
                defaultTerrainMat = Load<Material>(HDRenderPipelinePath + "RenderPipelineResources/DefaultHDTerrainMaterial.mat"),
            };

            // Textures
            textures = new TextureResources
            {
                // Debug
                debugFontTex = Load<Texture2D>(HDRenderPipelinePath + "RenderPipelineResources/DebugFont.tga"),
            };
        }
#endif
    }
}
