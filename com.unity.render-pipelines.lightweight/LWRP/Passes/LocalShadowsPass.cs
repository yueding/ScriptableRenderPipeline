using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class LocalShadowsPass : ScriptableRenderPass
    {
        private static class PunctualShadowsConstantBuffer
        {
            public static int _PunctualWorldToShadow;
            public static int _PunctualShadowStrength;
            public static int _PunctualShadowOffset0;
            public static int _PunctualShadowOffset1;
            public static int _PunctualShadowOffset2;
            public static int _PunctualShadowOffset3;
            public static int _PunctualShadowmapSize;
        }

        const int k_ShadowmapBufferBits = 16;
        RenderTexture m_PunctualShadowmapTexture;
        RenderTextureFormat m_PunctualShadowmapFormat;

        Matrix4x4[] m_PunctualShadowMatrices;
        ShadowSliceData[] m_PunctualLightSlices;
        float[] m_PunctualShadowStrength;

        const string k_RenderPunctualShadows = "Render Punctual Shadows";


        private RenderTargetHandle destination { get; set; }

        public LocalShadowsPass()
        {
            RegisterShaderPassName("ShadowCaster");

            m_PunctualShadowMatrices = new Matrix4x4[0];
            m_PunctualLightSlices = new ShadowSliceData[0];
            m_PunctualShadowStrength = new float[0];

            PunctualShadowsConstantBuffer._PunctualWorldToShadow = Shader.PropertyToID("_PunctualLightsWorldToShadow");
            PunctualShadowsConstantBuffer._PunctualShadowStrength = Shader.PropertyToID("_PunctualShadowStrength");
            PunctualShadowsConstantBuffer._PunctualShadowOffset0 = Shader.PropertyToID("_PunctualShadowOffset0");
            PunctualShadowsConstantBuffer._PunctualShadowOffset1 = Shader.PropertyToID("_PunctualShadowOffset1");
            PunctualShadowsConstantBuffer._PunctualShadowOffset2 = Shader.PropertyToID("_PunctualShadowOffset2");
            PunctualShadowsConstantBuffer._PunctualShadowOffset3 = Shader.PropertyToID("_PunctualShadowOffset3");
            PunctualShadowsConstantBuffer._PunctualShadowmapSize = Shader.PropertyToID("_PunctualShadowmapSize");

            m_PunctualShadowmapFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Shadowmap)
                ? RenderTextureFormat.Shadowmap
                : RenderTextureFormat.Depth;
        }

        public void Setup(RenderTargetHandle destination, int maxVisiblePunctualLights)
        {
            this.destination = destination;

            if (m_PunctualShadowMatrices.Length != maxVisiblePunctualLights)
            {
                m_PunctualShadowMatrices = new Matrix4x4[maxVisiblePunctualLights];
                m_PunctualLightSlices = new ShadowSliceData[maxVisiblePunctualLights];
                m_PunctualShadowStrength = new float[maxVisiblePunctualLights];
            }
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderer == null)
                throw new ArgumentNullException("renderer");
            
            if (renderingData.shadowData.renderPunctualShadows)
            {
                Clear();
                RenderPunctualShadowmapAtlas(ref context, ref renderingData.cullResults, ref renderingData.lightData, ref renderingData.shadowData);
            }
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");
            
            if (m_PunctualShadowmapTexture)
            {
                RenderTexture.ReleaseTemporary(m_PunctualShadowmapTexture);
                m_PunctualShadowmapTexture = null;
            }
        }

        void Clear()
        {
            m_PunctualShadowmapTexture = null;

            for (int i = 0; i < m_PunctualShadowMatrices.Length; ++i)
                m_PunctualShadowMatrices[i] = Matrix4x4.identity;

            for (int i = 0; i < m_PunctualLightSlices.Length; ++i)
                m_PunctualLightSlices[i].Clear();

            for (int i = 0; i < m_PunctualShadowStrength.Length; ++i)
                m_PunctualShadowStrength[i] = 0.0f;
        }

        void RenderPunctualShadowmapAtlas(ref ScriptableRenderContext context, ref CullResults cullResults, ref LightData lightData, ref ShadowData shadowData)
        {
            List<int> punctualLightIndices = lightData.visiblePunctualLightIndices;
            List<VisibleLight> visibleLights = lightData.visibleLights;

            int shadowCastingLightsCount = 0;
            int punctualLightsCount = punctualLightIndices.Count;
            for (int i = 0; i < punctualLightsCount; ++i)
            {
                VisibleLight shadowLight = visibleLights[punctualLightIndices[i]];

                if (shadowLight.lightType == LightType.Spot && shadowLight.light.shadows != LightShadows.None)
                    shadowCastingLightsCount++;
            }

            if (shadowCastingLightsCount == 0)
                return;

            Matrix4x4 view, proj;
            Bounds bounds;

            CommandBuffer cmd = CommandBufferPool.Get(k_RenderPunctualShadows);
            using (new ProfilingSample(cmd, k_RenderPunctualShadows))
            {
                // TODO: Add support to point light shadows. We make a simplification here that only works
                // for spot lights and with max spot shadows per pass.
                int atlasWidth = shadowData.punctualShadowAtlasWidth;
                int atlasHeight = shadowData.punctualShadowAtlasHeight;
                int sliceResolution = LightweightShadowUtils.GetMaxTileResolutionInAtlas(atlasWidth, atlasHeight, shadowCastingLightsCount);

                m_PunctualShadowmapTexture = RenderTexture.GetTemporary(shadowData.punctualShadowAtlasWidth,
                    shadowData.punctualShadowAtlasHeight, k_ShadowmapBufferBits, m_PunctualShadowmapFormat);
                m_PunctualShadowmapTexture.filterMode = FilterMode.Bilinear;
                m_PunctualShadowmapTexture.wrapMode = TextureWrapMode.Clamp;

                SetRenderTarget(cmd, m_PunctualShadowmapTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    ClearFlag.Depth, Color.black, TextureDimension.Tex2D);

                for (int i = 0; i < punctualLightsCount; ++i)
                {
                    int shadowLightIndex = punctualLightIndices[i];
                    VisibleLight shadowLight = visibleLights[shadowLightIndex];
                    Light light = shadowLight.light;

                    // TODO: Add support to point light shadows
                    if (shadowLight.lightType != LightType.Spot || shadowLight.light.shadows == LightShadows.None)
                        continue;

                    if (!cullResults.GetShadowCasterBounds(shadowLightIndex, out bounds))
                        continue;

                    Matrix4x4 shadowTransform;
                    bool success = LightweightShadowUtils.ExtractSpotLightMatrix(ref cullResults, ref shadowData,
                        shadowLightIndex, out shadowTransform, out view, out proj);

                    if (success)
                    {
                        // This way of computing the shadow slice only work for spots and with most 4 shadow casting lights per pass
                        // Change this when point lights are supported.
                        Debug.Assert(shadowCastingLightsCount <= 4 && shadowLight.lightType == LightType.Spot);

                        // TODO: We need to pass bias and scale list to shader to be able to support multiple
                        // shadow casting punctual lights.
                        m_PunctualLightSlices[i].offsetX = (i % 2) * sliceResolution;
                        m_PunctualLightSlices[i].offsetY = (i / 2) * sliceResolution;
                        m_PunctualLightSlices[i].resolution = sliceResolution;
                        m_PunctualLightSlices[i].shadowTransform = shadowTransform;

                        m_PunctualShadowStrength[i] = light.shadowStrength;

                        if (shadowCastingLightsCount > 1)
                            LightweightShadowUtils.ApplySliceTransform(ref m_PunctualLightSlices[i], atlasWidth, atlasHeight);

                        var settings = new DrawShadowsSettings(cullResults, shadowLightIndex);
                        LightweightShadowUtils.SetupShadowCasterConstants(cmd, ref shadowLight, proj, sliceResolution);
                        LightweightShadowUtils.RenderShadowSlice(cmd, ref context, ref m_PunctualLightSlices[i], ref settings, proj, view);
                    }
                }

                SetupPunctualLightsShadowReceiverConstants(cmd, ref shadowData);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void SetupPunctualLightsShadowReceiverConstants(CommandBuffer cmd, ref ShadowData shadowData)
        {
            for (int i = 0; i < m_PunctualLightSlices.Length; ++i)
                m_PunctualShadowMatrices[i] = m_PunctualLightSlices[i].shadowTransform;

            float invShadowAtlasWidth = 1.0f / shadowData.punctualShadowAtlasWidth;
            float invShadowAtlasHeight = 1.0f / shadowData.punctualShadowAtlasHeight;
            float invHalfShadowAtlasWidth = 0.5f * invShadowAtlasWidth;
            float invHalfShadowAtlasHeight = 0.5f * invShadowAtlasHeight;

            cmd.SetGlobalTexture(destination.id, m_PunctualShadowmapTexture);
            cmd.SetGlobalMatrixArray(PunctualShadowsConstantBuffer._PunctualWorldToShadow, m_PunctualShadowMatrices);
            cmd.SetGlobalFloatArray(PunctualShadowsConstantBuffer._PunctualShadowStrength, m_PunctualShadowStrength);
            cmd.SetGlobalVector(PunctualShadowsConstantBuffer._PunctualShadowOffset0, new Vector4(-invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(PunctualShadowsConstantBuffer._PunctualShadowOffset1, new Vector4(invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(PunctualShadowsConstantBuffer._PunctualShadowOffset2, new Vector4(-invHalfShadowAtlasWidth, invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(PunctualShadowsConstantBuffer._PunctualShadowOffset3, new Vector4(invHalfShadowAtlasWidth, invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(PunctualShadowsConstantBuffer._PunctualShadowmapSize, new Vector4(invShadowAtlasWidth, invShadowAtlasHeight,
                shadowData.punctualShadowAtlasWidth, shadowData.punctualShadowAtlasHeight));
        }
    }
}
