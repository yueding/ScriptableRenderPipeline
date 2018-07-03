using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class HDRISkyRenderer : SkyRenderer
    {
        Material m_SkyHDRIMaterial; // Renders a cubemap into a render texture (can be cube or 2D)
        Material m_IntegrateHDRISkyMaterial; // Compute the HDRI sky intensity in lux for the skybox
        MaterialPropertyBlock m_PropertyBlock;
        HDRISky m_HdriSkyParams;
        RTHandleSystem.RTHandle m_IntensityTexture;

        public HDRISkyRenderer(HDRISky hdriSkyParams)
        {
            m_HdriSkyParams = hdriSkyParams;
            m_PropertyBlock = new MaterialPropertyBlock();
            m_IntensityTexture = RTHandles.Alloc(1, 1, colorFormat: RenderTextureFormat.RFloat, sRGB: false);
        }

        public override void Build()
        {
            var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            m_SkyHDRIMaterial = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.hdriSky);
            m_IntegrateHDRISkyMaterial = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.integrateHdriSky);
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_SkyHDRIMaterial);
        }

        public override void SetRenderTargets(BuiltinSkyParameters builtinParams)
        {
            if (builtinParams.depthBuffer == BuiltinSkyParameters.nullRT)
            {
                HDUtils.SetRenderTarget(builtinParams.commandBuffer, builtinParams.hdCamera, builtinParams.colorBuffer);
            }
            else
            {
                HDUtils.SetRenderTarget(builtinParams.commandBuffer, builtinParams.hdCamera, builtinParams.colorBuffer, builtinParams.depthBuffer);
            }
        }

        public override void RenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap)
        {
            m_SkyHDRIMaterial.SetTexture(HDShaderIDs._Cubemap, m_HdriSkyParams.hdriSky);
            m_SkyHDRIMaterial.SetVector(HDShaderIDs._SkyParam, new Vector4(GetExposure(m_HdriSkyParams, builtinParams.debugSettings), m_HdriSkyParams.multiplier, -m_HdriSkyParams.rotation, m_HdriSkyParams.lux)); // -rotation to match Legacy...
            m_SkyHDRIMaterial.SetTexture(HDShaderIDs._SkyIntensity, m_IntensityTexture);
            
            using (new ProfilingSample(builtinParams.commandBuffer, "Get hdri skybox intensity"))
            {
            // if (m_HdriSkyParams.updateHDRISkyIntensity)
            {
                m_IntegrateHDRISkyMaterial.SetTexture(HDShaderIDs._Cubemap, m_HdriSkyParams.hdriSky);
                CoreUtils.DrawFullScreen(builtinParams.commandBuffer, m_IntegrateHDRISkyMaterial, m_IntensityTexture);
                m_HdriSkyParams.updateHDRISkyIntensity.value = false;
            }
            }

            // This matrix needs to be updated at the draw call frequency.
            m_PropertyBlock.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, builtinParams.pixelCoordToViewDirMatrix);

            CoreUtils.DrawFullScreen(builtinParams.commandBuffer, m_SkyHDRIMaterial, m_PropertyBlock, renderForCubemap ? 0 : 1);
        }

        public override bool IsValid()
        {
            return m_HdriSkyParams != null && m_SkyHDRIMaterial != null;
        }
    }
}
