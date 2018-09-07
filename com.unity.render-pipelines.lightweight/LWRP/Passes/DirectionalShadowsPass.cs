using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class DirectionalShadowsPass : ScriptableRenderPass
    {
        private static class DirectionalShadowConstantBuffer
        {
            public static int _WorldToShadow;
            public static int _ShadowData;
            public static int _CascadeShadowSplitSpheres0;
            public static int _CascadeShadowSplitSpheres1;
            public static int _CascadeShadowSplitSpheres2;
            public static int _CascadeShadowSplitSpheres3;
            public static int _CascadeShadowSplitSphereRadii;
            public static int _ShadowOffset0;
            public static int _ShadowOffset1;
            public static int _ShadowOffset2;
            public static int _ShadowOffset3;
            public static int _ShadowmapSize;
        }

        const int k_MaxCascades = 4;
        const int k_ShadowmapBufferBits = 16;
        int m_ShadowCasterCascadesCount;

        RenderTexture m_DirectionalShadowmapTexture;
        RenderTextureFormat m_ShadowmapFormat;

        Matrix4x4[] m_DirectionalShadowMatrices;
        ShadowSliceData[] m_CascadeSlices;
        Vector4[] m_CascadeSplitDistances;

        const string k_RenderDirectionalShadowmapTag = "Render Directional Shadowmap";

        private RenderTargetHandle destination { get; set; }

        public DirectionalShadowsPass()
        {
            RegisterShaderPassName("ShadowCaster");

            m_DirectionalShadowMatrices = new Matrix4x4[k_MaxCascades + 1];
            m_CascadeSlices = new ShadowSliceData[k_MaxCascades];
            m_CascadeSplitDistances = new Vector4[k_MaxCascades];

            DirectionalShadowConstantBuffer._WorldToShadow = Shader.PropertyToID("_DirectionalLightWorldToShadow");
            DirectionalShadowConstantBuffer._ShadowData = Shader.PropertyToID("_DirectionalShadowData");
            DirectionalShadowConstantBuffer._CascadeShadowSplitSpheres0 = Shader.PropertyToID("_CascadeShadowSplitSpheres0");
            DirectionalShadowConstantBuffer._CascadeShadowSplitSpheres1 = Shader.PropertyToID("_CascadeShadowSplitSpheres1");
            DirectionalShadowConstantBuffer._CascadeShadowSplitSpheres2 = Shader.PropertyToID("_CascadeShadowSplitSpheres2");
            DirectionalShadowConstantBuffer._CascadeShadowSplitSpheres3 = Shader.PropertyToID("_CascadeShadowSplitSpheres3");
            DirectionalShadowConstantBuffer._CascadeShadowSplitSphereRadii = Shader.PropertyToID("_CascadeShadowSplitSphereRadii");
            DirectionalShadowConstantBuffer._ShadowOffset0 = Shader.PropertyToID("_DirectionalShadowOffset0");
            DirectionalShadowConstantBuffer._ShadowOffset1 = Shader.PropertyToID("_DirectionalShadowOffset1");
            DirectionalShadowConstantBuffer._ShadowOffset2 = Shader.PropertyToID("_DirectionalShadowOffset2");
            DirectionalShadowConstantBuffer._ShadowOffset3 = Shader.PropertyToID("_DirectionalShadowOffset3");
            DirectionalShadowConstantBuffer._ShadowmapSize = Shader.PropertyToID("_DirectionalShadowmapSize");

            m_ShadowmapFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Shadowmap)
                ? RenderTextureFormat.Shadowmap
                : RenderTextureFormat.Depth;
        }

        public void Setup(RenderTargetHandle destination)
        {
            this.destination = destination;
        }
        
        /// <inheritdoc/>
        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderer == null)
                throw new ArgumentNullException("renderer");

            if (!renderingData.shadowData.renderDirectionalShadows) 
                return;
            
            Clear();
            RenderDirectionalCascadeShadowmap(ref context, ref renderingData.cullResults, ref renderingData.lightData, ref renderingData.shadowData);
        }
        
        /// <inheritdoc/>
        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");
            
            if (m_DirectionalShadowmapTexture)
            {
                RenderTexture.ReleaseTemporary(m_DirectionalShadowmapTexture);
                m_DirectionalShadowmapTexture = null;
            }
        }

        void Clear()
        {
            m_DirectionalShadowmapTexture = null;

            for (int i = 0; i < m_DirectionalShadowMatrices.Length; ++i)
                m_DirectionalShadowMatrices[i] = Matrix4x4.identity;

            for (int i = 0; i < m_CascadeSplitDistances.Length; ++i)
                m_CascadeSplitDistances[i] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

            for (int i = 0; i < m_CascadeSlices.Length; ++i)
                m_CascadeSlices[i].Clear();
        }

        void RenderDirectionalCascadeShadowmap(ref ScriptableRenderContext context, ref CullResults cullResults, ref LightData lightData, ref ShadowData shadowData)
        {
            int shadowLightIndex = lightData.mainLightIndex;
            if (shadowLightIndex == -1)
                return;

            VisibleLight shadowLight = lightData.visibleLights[shadowLightIndex];
            Light light = shadowLight.light;
            Debug.Assert(shadowLight.lightType == LightType.Directional);

            if (light.shadows == LightShadows.None)
                return;

            Bounds bounds;
            if (!cullResults.GetShadowCasterBounds(shadowLightIndex, out bounds))
                return;

            CommandBuffer cmd = CommandBufferPool.Get(k_RenderDirectionalShadowmapTag);
            using (new ProfilingSample(cmd, k_RenderDirectionalShadowmapTag))
            {
                m_ShadowCasterCascadesCount = shadowData.directionalLightCascadeCount;

                int shadowResolution = LightweightShadowUtils.GetMaxTileResolutionInAtlas(shadowData.directionalShadowAtlasWidth, shadowData.directionalShadowAtlasHeight, m_ShadowCasterCascadesCount);
                float shadowNearPlane = light.shadowNearPlane;

                Matrix4x4 view, proj;
                var settings = new DrawShadowsSettings(cullResults, shadowLightIndex);

                m_DirectionalShadowmapTexture = RenderTexture.GetTemporary(shadowData.directionalShadowAtlasWidth,
                    shadowData.directionalShadowAtlasHeight, k_ShadowmapBufferBits, m_ShadowmapFormat);
                m_DirectionalShadowmapTexture.filterMode = FilterMode.Bilinear;
                m_DirectionalShadowmapTexture.wrapMode = TextureWrapMode.Clamp;
                SetRenderTarget(cmd, m_DirectionalShadowmapTexture, RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.Store, ClearFlag.Depth, Color.black, TextureDimension.Tex2D);

                bool success = false;
                for (int cascadeIndex = 0; cascadeIndex < m_ShadowCasterCascadesCount; ++cascadeIndex)
                {
                    success = LightweightShadowUtils.ExtractDirectionalLightMatrix(ref cullResults, ref shadowData, shadowLightIndex, cascadeIndex, shadowResolution, shadowNearPlane, out m_CascadeSplitDistances[cascadeIndex], out m_CascadeSlices[cascadeIndex], out view, out proj);
                    if (success)
                    {
                        settings.splitData.cullingSphere = m_CascadeSplitDistances[cascadeIndex];
                        LightweightShadowUtils.SetupShadowCasterConstants(cmd, ref shadowLight, proj, shadowResolution);
                        LightweightShadowUtils.RenderShadowSlice(cmd, ref context, ref m_CascadeSlices[cascadeIndex], ref settings, proj, view);
                    }
                }

                if (success)
                    SetupDirectionalShadowReceiverConstants(cmd, ref shadowData, shadowLight);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void SetupDirectionalShadowReceiverConstants(CommandBuffer cmd, ref ShadowData shadowData, VisibleLight shadowLight)
        {
            Light light = shadowLight.light;

            int cascadeCount = m_ShadowCasterCascadesCount;
            for (int i = 0; i < k_MaxCascades; ++i)
                m_DirectionalShadowMatrices[i] = (cascadeCount >= i) ? m_CascadeSlices[i].shadowTransform : Matrix4x4.identity;

            // We setup and additional a no-op WorldToShadow matrix in the last index
            // because the ComputeCascadeIndex function in Shadows.hlsl can return an index
            // out of bounds. (position not inside any cascade) and we want to avoid branching
            Matrix4x4 noOpShadowMatrix = Matrix4x4.zero;
            noOpShadowMatrix.m33 = (SystemInfo.usesReversedZBuffer) ? 1.0f : 0.0f;
            m_DirectionalShadowMatrices[k_MaxCascades] = noOpShadowMatrix;

            float invShadowAtlasWidth = 1.0f / shadowData.directionalShadowAtlasWidth;
            float invShadowAtlasHeight = 1.0f / shadowData.directionalShadowAtlasHeight;
            float invHalfShadowAtlasWidth = 0.5f * invShadowAtlasWidth;
            float invHalfShadowAtlasHeight = 0.5f * invShadowAtlasHeight;
            cmd.SetGlobalTexture(destination.id, m_DirectionalShadowmapTexture);
            cmd.SetGlobalMatrixArray(DirectionalShadowConstantBuffer._WorldToShadow, m_DirectionalShadowMatrices);
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._ShadowData, new Vector4(light.shadowStrength, 0.0f, 0.0f, 0.0f));
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._CascadeShadowSplitSpheres0, m_CascadeSplitDistances[0]);
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._CascadeShadowSplitSpheres1, m_CascadeSplitDistances[1]);
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._CascadeShadowSplitSpheres2, m_CascadeSplitDistances[2]);
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._CascadeShadowSplitSpheres3, m_CascadeSplitDistances[3]);
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._CascadeShadowSplitSphereRadii, new Vector4(m_CascadeSplitDistances[0].w * m_CascadeSplitDistances[0].w,
                m_CascadeSplitDistances[1].w * m_CascadeSplitDistances[1].w,
                m_CascadeSplitDistances[2].w * m_CascadeSplitDistances[2].w,
                m_CascadeSplitDistances[3].w * m_CascadeSplitDistances[3].w));
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._ShadowOffset0, new Vector4(-invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._ShadowOffset1, new Vector4(invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._ShadowOffset2, new Vector4(-invHalfShadowAtlasWidth, invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._ShadowOffset3, new Vector4(invHalfShadowAtlasWidth, invHalfShadowAtlasHeight, 0.0f, 0.0f));
            cmd.SetGlobalVector(DirectionalShadowConstantBuffer._ShadowmapSize, new Vector4(invShadowAtlasWidth, invShadowAtlasHeight,
                shadowData.directionalShadowAtlasWidth, shadowData.directionalShadowAtlasHeight));
        }
    };
}
