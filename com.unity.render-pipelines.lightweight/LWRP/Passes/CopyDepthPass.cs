using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class CopyDepthPass : ScriptableRenderPass
    {
        private RenderTargetHandle source { get; set; }
        private RenderTargetHandle destination { get; set; }

        const string k_DepthCopyTag = "Depth Copy";

        public void Setup(RenderTargetHandle source, RenderTargetHandle destination)
        {
            this.source = source;
            this.destination = destination;
        }

        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(k_DepthCopyTag);
            RenderTargetIdentifier depthSurface = source.Identifier();
            RenderTargetIdentifier copyDepthSurface = destination.Identifier();
            Material depthCopyMaterial = renderer.GetMaterial(MaterialHandles.DepthCopy);

            RenderTextureDescriptor descriptor = ScriptableRenderer.CreateRenderTextureDescriptor(ref renderingData.cameraData);
            descriptor.colorFormat = RenderTextureFormat.Depth;
            descriptor.depthBufferBits = 32; //TODO: fix this ;
            descriptor.msaaSamples = 1;
            descriptor.bindMS = false;
            cmd.GetTemporaryRT(destination.id, descriptor, FilterMode.Point);

            cmd.SetGlobalTexture("_CameraDepthAttachment", source.Identifier());

            if (renderingData.cameraData.msaaSamples > 1)
            {
                cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthNoMsaa);
                if (renderingData.cameraData.msaaSamples == 4)
                {
                    cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                    cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                }
                else
                {
                    cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                    cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                }
                cmd.Blit(depthSurface, copyDepthSurface, depthCopyMaterial);
            }
            else
            {
                cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthNoMsaa);
                cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                ScriptableRenderer.CopyTexture(cmd, depthSurface, copyDepthSurface, depthCopyMaterial);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (destination != RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(destination.id);
                destination = RenderTargetHandle.CameraTarget;
            }
        }
    }
}
