using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class HDShadowAtlas
    {
        public RenderTargetIdentifier   identifier;

        RTHandleSystem.RTHandle         m_Atlas;
        List<HDShadowRequest>           m_shadowRequests = new List<HDShadowRequest>();
        
        public void AllocateShadowMaps(int width, int height, DepthBits depthBits = DepthBits.Depth24, RenderTextureFormat format = RenderTextureFormat.Shadowmap)
        {
            m_Atlas = RTHandles.Alloc(width, height, depthBufferBits: depthBits, sRGB: false, colorFormat: format);
            identifier = new RenderTargetIdentifier(m_Atlas);
        }

        public void Reserve(HDShadowRequest shadowRequest)
        {
            
        }

        public void LayoutAndResize()
        {
            // TODO: sort shadowRequests by size and assign an x and y offset

            // TODO: resize if too small
        }

        public void Clear()
        {

        }
    }
}