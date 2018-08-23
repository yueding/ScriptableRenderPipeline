using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class HDShadowAtlas
    {
        public RenderTargetIdentifier   identifier;

        int                             m_Width;
        int                             m_Height;

        RTHandleSystem.RTHandle         m_Atlas;
        List<HDShadowRequest>           m_ShadowRequests = new List<HDShadowRequest>();
        
        public void AllocateShadowMaps(int width, int height, DepthBits depthBits = DepthBits.Depth24, RenderTextureFormat format = RenderTextureFormat.Shadowmap)
        {
            m_Atlas = RTHandles.Alloc(width, height, depthBufferBits: depthBits, sRGB: false, colorFormat: format);
            m_Width = width;
            m_Height = height;
            identifier = new RenderTargetIdentifier(m_Atlas);
        }

        public void Reserve(HDShadowRequest shadowRequest)
        {
            m_ShadowRequests.Add(shadowRequest);
        }

        public void Layout(bool allowResize = true)
        {
            // TODO: change this sort (and maybe the list) by something that don't create garbage
            m_ShadowRequests.Sort((s1, s2) => s1.viewportSize.x.CompareTo(s2.viewportSize.x));
            
            float curX = 0, curY = 0, curH = 0, xMax = m_Width, yMax = m_Height;

            // Assign to every view shadow viewport request a position in the atlas
            foreach (var shadowRequest in m_ShadowRequests)
            {
                // shadow atlas layouting
                Rect viewport = new Rect(Vector2.zero, shadowRequest.viewportSize);
                curH = curH >= viewport.height ? curH : viewport.height;

                if (curX + viewport.width > xMax)
                {
                    curX = 0;
                    curY += curH;
                    curH = viewport.height;
                }
                if (curX + viewport.width > xMax || curY + curH > yMax)
                {
                    curX = 0;
                    curY = 0;
                    curH = viewport.height;
                }
                if (curX + viewport.width > xMax || curY + curH > yMax)
                {
                    Debug.LogWarning("Shadow atlasing has failed.");
                    // TODO: resize if possible and needed
                    return ;
                }
                viewport.x = curX;
                viewport.y = curY;
                shadowRequest.atlasViewport = viewport;
                curX += viewport.width;
            }
        }

        public void Clear()
        {
            m_ShadowRequests.Clear();
        }

        public void Release()
        {
            RTHandles.Release(m_Atlas);
        }
    }
}