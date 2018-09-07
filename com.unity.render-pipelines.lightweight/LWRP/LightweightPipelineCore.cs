using System;
using System.Collections.Generic;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.XR;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    [Flags]
    public enum ShaderFeatures
    {
        RealtimeDirectionalLights       = (1 << 0),
        RealtimeDirectionalLightShadows = (1 << 1),
        RealtimePunctualLightsVertex     = (1 << 2),
        RealtimePunctualLights           = (1 << 3),
        RealtimePunctualLightShadows     = (1 << 4),
        SoftShadows                     = (1 << 5),
        MixedLighting                   = (1 << 6),
    }
    public enum MixedLightingSetup
    {
        None,
        ShadowMask,
        Subtractive,
    };

    public struct RenderingData
    {
        public CullResults cullResults;
        public CameraData cameraData;
        public LightData lightData;
        public ShadowData shadowData;
        public bool supportsDynamicBatching;
    }

    public struct LightData
    {
        public int punctualLightsCount;
        public bool shadePunctualLightsPerVertex;
        public int mainLightIndex;
        public List<VisibleLight> visibleLights;
        public List<int> visiblePunctualLightIndices;
        public bool supportsMixedLighting;
    }

    public struct CameraData
    {
        public Camera camera;
        public float renderScale;
        public int msaaSamples;
        public bool isSceneViewCamera;
        public bool isDefaultViewport;
        public bool isOffscreenRender;
        public bool isHdrEnabled;
        public bool requiresDepthTexture;
        public bool requiresSoftParticles;
        public bool requiresOpaqueTexture;
        public Downsampling opaqueTextureDownsampling;

        public SortFlags defaultOpaqueSortFlags;

        public bool isStereoEnabled;

        public float maxShadowDistance;
        public bool postProcessEnabled;
        public PostProcessLayer postProcessLayer;
    }

    public struct ShadowData
    {
        public bool renderDirectionalShadows;
        public bool requiresScreenSpaceShadowResolve;
        public int directionalShadowAtlasWidth;
        public int directionalShadowAtlasHeight;
        public int directionalLightCascadeCount;
        public Vector3 directionalLightCascades;
        public bool renderPunctualShadows;
        public int punctualShadowAtlasWidth;
        public int punctualShadowAtlasHeight;
        public bool supportsSoftShadows;
        public int bufferBitCount;
    }

    public static class ShaderKeywordStrings
    {
        public static readonly string RealtimeDirectionalShadows = "_DIRECTIONAL_SHADOWS";
        public static readonly string RealtimePunctualLightsVertex = "_PUNCTUAL_LIGHTS_VERTEX";
        public static readonly string RealtimePunctualLights = "_PUNCTUAL_LIGHTS";
        public static readonly string RealtimePunctualLightShadows = "_PUNCTUAL_LIGHT_SHADOWS";
        public static readonly string CascadeShadows = "_DIRECTIONAL_SHADOWS_CASCADE";
        public static readonly string SoftShadows = "_SHADOWS_SOFT";
        public static readonly string MixedLightingSubtractive = "_MIXED_LIGHTING_SUBTRACTIVE";

        public static readonly string DepthNoMsaa = "_DEPTH_NO_MSAA";
        public static readonly string DepthMsaa2 = "_DEPTH_MSAA_2";
        public static readonly string DepthMsaa4 = "_DEPTH_MSAA_4";
        public static readonly string SoftParticles = "SOFTPARTICLES_ON";
    }

    public sealed partial class LightweightRenderPipeline
    {
        static ShaderFeatures s_ShaderFeatures;

        public static ShaderFeatures GetSupportedShaderFeatures()
        {
            return s_ShaderFeatures;
        }

        void SortCameras(Camera[] cameras)
        {
            Array.Sort(cameras, (lhs, rhs) => (int)(lhs.depth - rhs.depth));
        }

        static void SetSupportedShaderFeatures(LightweightPipelineAsset pipelineAsset)
        {
            s_ShaderFeatures = ShaderFeatures.RealtimeDirectionalLights;

            if (pipelineAsset.supportsDirectionalShadows)
                s_ShaderFeatures |= ShaderFeatures.RealtimeDirectionalLightShadows;

            if (pipelineAsset.punctualLightsSupport == RealtimeLightSupport.PerVertex)
            {
                s_ShaderFeatures |= ShaderFeatures.RealtimePunctualLightsVertex;
            }
            else if (pipelineAsset.punctualLightsSupport == RealtimeLightSupport.PerPixel)
            {
                s_ShaderFeatures |= ShaderFeatures.RealtimeDirectionalLights;
                if (pipelineAsset.supportsPunctualShadows)
                    s_ShaderFeatures |= ShaderFeatures.RealtimePunctualLightShadows;
            }

            bool anyShadows = pipelineAsset.supportsDirectionalShadows || pipelineAsset.supportsPunctualShadows;
            if (pipelineAsset.supportsSoftShadows && anyShadows)
                s_ShaderFeatures |= ShaderFeatures.SoftShadows;

            if (pipelineAsset.mixedLightingSupported)
                s_ShaderFeatures |= ShaderFeatures.MixedLighting;
        }
        public static bool IsStereoEnabled(Camera camera)
        {
            bool isSceneViewCamera = camera.cameraType == CameraType.SceneView;
            return XRGraphicsConfig.enabled && !isSceneViewCamera && (camera.stereoTargetEye == StereoTargetEyeMask.Both);
        }
    }
}
