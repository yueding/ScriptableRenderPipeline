using System.Collections.Generic;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class SetupLightweightConstanstPass : ScriptableRenderPass
    {
        public static class LightConstantBuffer
        {
            public static int _MainLightPosition;
            public static int _MainLightColor;

            public static int _PunctualLightsCount;
            public static int _PunctualLightsPosition;
            public static int _PunctualLightsColor;
            public static int _PunctualLightsAttenuation;
            public static int _PunctualLightsSpotDir;

            public static int _PunctualLightsBuffer;
        }

        const string k_SetupLightConstants = "Setup Light Constants";
        MixedLightingSetup m_MixedLightingSetup;

        Vector4 k_DefaultLightPosition = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
        Vector4 k_DefaultLightColor = Color.black;
        Vector4 k_DefaultLightAttenuation = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
        Vector4 k_DefaultLightSpotDirection = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);

        Vector4[] m_LightPositions;
        Vector4[] m_LightColors;
        Vector4[] m_LightAttenuations;
        Vector4[] m_LightSpotDirections;

        private int maxVisiblePunctualLights { get; set; }
        private ComputeBuffer perObjectLightIndices { get; set; }

        public SetupLightweightConstanstPass()
        {
            LightConstantBuffer._MainLightPosition = Shader.PropertyToID("_MainLightPosition");
            LightConstantBuffer._MainLightColor = Shader.PropertyToID("_MainLightColor");
            LightConstantBuffer._PunctualLightsCount = Shader.PropertyToID("_PunctualLightsCount");
            LightConstantBuffer._PunctualLightsPosition = Shader.PropertyToID("_PunctualLightsPosition");
            LightConstantBuffer._PunctualLightsColor = Shader.PropertyToID("_PunctualLightsColor");
            LightConstantBuffer._PunctualLightsAttenuation = Shader.PropertyToID("_PunctualLightsAttenuation");
            LightConstantBuffer._PunctualLightsSpotDir = Shader.PropertyToID("_PunctualLightsSpotDir");
            LightConstantBuffer._PunctualLightsBuffer = Shader.PropertyToID("_PunctualLightsBuffer");

            m_LightPositions = new Vector4[0];
            m_LightColors = new Vector4[0];
            m_LightAttenuations = new Vector4[0];
            m_LightSpotDirections = new Vector4[0];
        }

        public void Setup(int maxVisiblePunctualLights, ComputeBuffer perObjectLightIndices)
        {
            this.maxVisiblePunctualLights = maxVisiblePunctualLights;
            this.perObjectLightIndices = perObjectLightIndices;

            if (m_LightColors.Length != maxVisiblePunctualLights)
            {
                m_LightPositions = new Vector4[maxVisiblePunctualLights];
                m_LightColors = new Vector4[maxVisiblePunctualLights];
                m_LightAttenuations = new Vector4[maxVisiblePunctualLights];
                m_LightSpotDirections = new Vector4[maxVisiblePunctualLights];
            }
        }

        void InitializeLightConstants(List<VisibleLight> lights, int lightIndex, out Vector4 lightPos, out Vector4 lightColor, out Vector4 lightAttenuation, out Vector4 lightSpotDir)
        {
            lightPos = k_DefaultLightPosition;
            lightColor = k_DefaultLightColor;
            lightAttenuation = k_DefaultLightAttenuation;
            lightSpotDir = k_DefaultLightSpotDirection;

            float subtractiveMixedLighting = 0.0f;

            // When no lights are visible, main light will be set to -1.
            // In this case we initialize it to default values and return
            if (lightIndex < 0)
                return;

            VisibleLight lightData = lights[lightIndex];
            if (lightData.lightType == LightType.Directional)
            {
                Vector4 dir = -lightData.localToWorld.GetColumn(2);
                lightPos = new Vector4(dir.x, dir.y, dir.z, 0.0f);
            }
            else
            {
                Vector4 pos = lightData.localToWorld.GetColumn(3);
                lightPos = new Vector4(pos.x, pos.y, pos.z, 0.0f);
            }

            // VisibleLight.finalColor already returns color in active color space
            lightColor = lightData.finalColor;

            // Directional Light attenuation is initialize so distance attenuation always be 1.0
            if (lightData.lightType != LightType.Directional)
            {
                // Light attenuation in lightweight matches the unity vanilla one.
                // attenuation = 1.0 / distanceToLightSqr
                // We offer two different smoothing factors.
                // The smoothing factors make sure that the light intensity is zero at the light range limit.
                // The first smoothing factor is a linear fade starting at 80 % of the light range.
                // smoothFactor = (lightRangeSqr - distanceToLightSqr) / (lightRangeSqr - fadeStartDistanceSqr)
                // We rewrite smoothFactor to be able to pre compute the constant terms below and apply the smooth factor
                // with one MAD instruction
                // smoothFactor =  distanceSqr * (1.0 / (fadeDistanceSqr - lightRangeSqr)) + (-lightRangeSqr / (fadeDistanceSqr - lightRangeSqr)
                //                 distanceSqr *           oneOverFadeRangeSqr             +              lightRangeSqrOverFadeRangeSqr

                // The other smoothing factor matches the one used in the Unity lightmapper but is slower than the linear one.
                // smoothFactor = (1.0 - saturate((distanceSqr * 1.0 / lightrangeSqr)^2))^2
                float lightRangeSqr = lightData.range * lightData.range;
                float fadeStartDistanceSqr = 0.8f * 0.8f * lightRangeSqr;
                float fadeRangeSqr = (fadeStartDistanceSqr - lightRangeSqr);
                float oneOverFadeRangeSqr = 1.0f / fadeRangeSqr;
                float lightRangeSqrOverFadeRangeSqr = -lightRangeSqr / fadeRangeSqr;
                float oneOverLightRangeSqr = 1.0f / Mathf.Max(0.0001f, lightData.range * lightData.range);

                // On mobile: Use the faster linear smoothing factor.
                // On other devices: Use the smoothing factor that matches the GI.
                lightAttenuation.x = Application.isMobilePlatform ? oneOverFadeRangeSqr : oneOverLightRangeSqr;
                lightAttenuation.y = lightRangeSqrOverFadeRangeSqr;
                subtractiveMixedLighting = 1.0f;
            }

            if (lightData.lightType == LightType.Spot)
            {
                Vector4 dir = lightData.localToWorld.GetColumn(2);
                lightSpotDir = new Vector4(-dir.x, -dir.y, -dir.z, 0.0f);

                // Spot Attenuation with a linear falloff can be defined as
                // (SdotL - cosOuterAngle) / (cosInnerAngle - cosOuterAngle)
                // This can be rewritten as
                // invAngleRange = 1.0 / (cosInnerAngle - cosOuterAngle)
                // SdotL * invAngleRange + (-cosOuterAngle * invAngleRange)
                // If we precompute the terms in a MAD instruction
                float cosOuterAngle = Mathf.Cos(Mathf.Deg2Rad * lightData.spotAngle * 0.5f);
                // We neeed to do a null check for particle lights
                // This should be changed in the future
                // Particle lights will use an inline function
                float cosInnerAngle;
                if (lightData.light != null)
                    cosInnerAngle = Mathf.Cos(LightmapperUtils.ExtractInnerCone(lightData.light) * 0.5f);
                else
                    cosInnerAngle = Mathf.Cos((2.0f * Mathf.Atan(Mathf.Tan(lightData.spotAngle * 0.5f * Mathf.Deg2Rad) * (64.0f - 18.0f) / 64.0f)) * 0.5f);
                float smoothAngleRange = Mathf.Max(0.001f, cosInnerAngle - cosOuterAngle);
                float invAngleRange = 1.0f / smoothAngleRange;
                float add = -cosOuterAngle * invAngleRange;
                lightAttenuation.z = invAngleRange;
                lightAttenuation.w = add;
            }

            Light light = lightData.light;

            // TODO: Add support to shadow mask
            if (light != null && light.bakingOutput.mixedLightingMode == MixedLightingMode.Subtractive && light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed)
            {
                if (m_MixedLightingSetup == MixedLightingSetup.None && lightData.light.shadows != LightShadows.None)
                {
                    m_MixedLightingSetup = MixedLightingSetup.Subtractive;
                    subtractiveMixedLighting = 0.0f;
                }
            }

            // Use the w component of the light position to indicate subtractive mixed light mode.
            // The only directional light is the main light, and the rest are punctual lights.
            // The main light will always have w = 0 and the additional lights have w = 1.
            lightPos.w = subtractiveMixedLighting;
        }

        void SetupShaderLightConstants(CommandBuffer cmd, ref LightData lightData)
        {
            // Clear to default all light constant data
            for (int i = 0; i < maxVisiblePunctualLights; ++i)
                InitializeLightConstants(lightData.visibleLights, -1, out m_LightPositions[i],
                    out m_LightColors[i],
                    out m_LightAttenuations[i],
                    out m_LightSpotDirections[i]);

            m_MixedLightingSetup = MixedLightingSetup.None;

            // Main light has an optimized shader path for main light. This will benefit games that only care about a single light.
            // Lightweight pipeline also supports only a single shadow light, if available it will be the main light.
            SetupMainLightConstants(cmd, ref lightData);
            SetupAdditionalLightConstants(cmd, ref lightData);
        }

        void SetupMainLightConstants(CommandBuffer cmd, ref LightData lightData)
        {
            Vector4 lightPos, lightColor, lightAttenuation, lightSpotDir;
            InitializeLightConstants(lightData.visibleLights, lightData.mainLightIndex, out lightPos, out lightColor, out lightAttenuation, out lightSpotDir);

            cmd.SetGlobalVector(LightConstantBuffer._MainLightPosition, lightPos);
            cmd.SetGlobalVector(LightConstantBuffer._MainLightColor, lightColor);
        }

        void SetupAdditionalLightConstants(CommandBuffer cmd, ref LightData lightData)
        {
            List<VisibleLight> lights = lightData.visibleLights;
            if (lightData.punctualLightsCount > 0)
            {
                int punctualLightsCount = 0;
                for (int i = 0; i < lights.Count && punctualLightsCount < maxVisiblePunctualLights; ++i)
                {
                    VisibleLight light = lights[i];
                    if (light.lightType != LightType.Directional)
                    {
                        InitializeLightConstants(lights, i, out m_LightPositions[punctualLightsCount],
                            out m_LightColors[punctualLightsCount],
                            out m_LightAttenuations[punctualLightsCount],
                            out m_LightSpotDirections[punctualLightsCount]);
                        punctualLightsCount++;
                    }
                }

                cmd.SetGlobalVector(LightConstantBuffer._PunctualLightsCount, new Vector4(lightData.punctualLightsCount,
                    0.0f, 0.0f, 0.0f));

                // if not using a compute buffer, engine will set indices in 2 vec4 constants
                // unity_4LightIndices0 and unity_4LightIndices1
                if (perObjectLightIndices != null)
                    cmd.SetGlobalBuffer(LightConstantBuffer._PunctualLightsBuffer, perObjectLightIndices);
            }
            else
            {
                cmd.SetGlobalVector(LightConstantBuffer._PunctualLightsCount, Vector4.zero);
            }

            cmd.SetGlobalVectorArray(LightConstantBuffer._PunctualLightsPosition, m_LightPositions);
            cmd.SetGlobalVectorArray(LightConstantBuffer._PunctualLightsColor, m_LightColors);
            cmd.SetGlobalVectorArray(LightConstantBuffer._PunctualLightsAttenuation, m_LightAttenuations);
            cmd.SetGlobalVectorArray(LightConstantBuffer._PunctualLightsSpotDir, m_LightSpotDirections);
        }

        void SetShaderKeywords(CommandBuffer cmd, ref CameraData cameraData, ref LightData lightData, ref ShadowData shadowData)
        {
            bool realtimePunctualLightsPerPixel = lightData.punctualLightsCount > 0 && !lightData.shadePunctualLightsPerVertex;
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.RealtimePunctualLightsVertex, lightData.punctualLightsCount > 0 && lightData.shadePunctualLightsPerVertex);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.RealtimePunctualLights, realtimePunctualLightsPerPixel);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MixedLightingSubtractive, lightData.supportsMixedLighting && m_MixedLightingSetup == MixedLightingSetup.Subtractive);

            List<VisibleLight> visibleLights = lightData.visibleLights;

            // If shadows were resolved in screen space we don't sample shadowmap in lit shader. In that case we just set softDirectionalShadows to false.
            bool softDirectionalShadows = shadowData.renderDirectionalShadows && !shadowData.requiresScreenSpaceShadowResolve &&
                shadowData.supportsSoftShadows && lightData.mainLightIndex != -1 &&
                visibleLights[lightData.mainLightIndex].light.shadows == LightShadows.Soft;

            bool softPunctualShadows = false;
            if (shadowData.renderPunctualShadows && shadowData.supportsSoftShadows)
            {
                List<int> visiblePunctualLightIndices = lightData.visiblePunctualLightIndices;
                for (int i = 0; i < visiblePunctualLightIndices.Count; ++i)
                {
                    if (visibleLights[visiblePunctualLightIndices[i]].light.shadows == LightShadows.Soft)
                    {
                        softPunctualShadows = true;
                        break;
                    }
                }
            }

            // Currently shadow filtering keyword is shared between punctual and directional shadows.
            bool hasSoftShadows = softDirectionalShadows || softPunctualShadows;

            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.RealtimeDirectionalShadows, shadowData.renderDirectionalShadows);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.RealtimePunctualLightShadows, shadowData.renderPunctualShadows);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.SoftShadows, hasSoftShadows);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.CascadeShadows, shadowData.directionalLightCascadeCount > 1);

            // TODO: Remove this. legacy particles support will be removed from Unity in 2018.3. This should be a shader_feature instead with prop exposed in the Standard particles shader.
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.SoftParticles, cameraData.requiresSoftParticles);
        }

        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(k_SetupLightConstants);
            SetupShaderLightConstants(cmd, ref renderingData.lightData);
            SetShaderKeywords(cmd, ref renderingData.cameraData, ref renderingData.lightData, ref renderingData.shadowData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
