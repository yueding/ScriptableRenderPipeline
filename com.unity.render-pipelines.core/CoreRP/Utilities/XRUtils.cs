using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    public static class XRUtils
    {
        public static void RenderOcclusionMesh(CommandBuffer cmd, ScriptableRenderContext context, Camera camera)
        {   // This method expects XRGraphicsConfig.SetConfig() to have been called prior to it.
            // Then, engine code will check if occlusion mesh rendering is enabled, and apply occlusion mesh scale.
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            if (!XRGraphicsConfig.enabled)
                return;

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            context.RenderOcclusionMesh(camera);
        }

    }
}
