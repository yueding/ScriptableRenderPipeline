using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class HDShadowAtlas
    {
        public readonly RenderTargetIdentifier  identifier;
        public readonly List<HDShadowRequest>   shadowRequests = new List<HDShadowRequest>();

        int                             m_Width;
        int                             m_Height;

        RTHandleSystem.RTHandle         m_Atlas;
        Material                        m_ClearMaterial;
        
        public HDShadowAtlas(int width, int height, Material clearMaterial, FilterMode filterMode = FilterMode.Bilinear, DepthBits depthBits = DepthBits.Depth24, RenderTextureFormat format = RenderTextureFormat.Shadowmap, string name = "")
        {
            m_Atlas = RTHandles.Alloc(width, height, filterMode: filterMode, depthBufferBits: depthBits, sRGB: false, colorFormat: format, name: name);
            m_Width = width;
            m_Height = height;
            identifier = new RenderTargetIdentifier(m_Atlas);
            m_ClearMaterial = clearMaterial;
        }

        public void Reserve(HDShadowRequest shadowRequest)
        {
            shadowRequests.Add(shadowRequest);
        }

        public void Layout(bool allowResize = true)
        {
            // TODO: change this sort (and maybe the list) by something that don't create garbage
            // Note: it is very important to keep the added order for shadow maps that are the same size (for punctual lights)
            // shadowRequests.Sort((s1, s2) => s1.viewportSize.x.CompareTo(s2.viewportSize.x));
            
            float curX = 0, curY = 0, curH = 0, xMax = m_Width, yMax = m_Height;

            // Assign to every view shadow viewport request a position in the atlas
            foreach (var shadowRequest in shadowRequests)
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

        public void RenderShadows(ScriptableRenderContext renderContext, CommandBuffer cmd, DrawShadowsSettings dss)
        {
            cmd.SetRenderTarget(identifier);
            CoreUtils.DrawFullScreen(cmd, m_ClearMaterial, null, 0);

            foreach (var shadowRequest in shadowRequests)
            {
                cmd.SetViewport(shadowRequest.atlasViewport);
                cmd.SetViewProjectionMatrices(shadowRequest.view, shadowRequest.projection);
                
                cmd.SetGlobalFloat(HDShaderIDs._ZClip, shadowRequest.zClip ? 1.0f : 0.0f);

                dss.lightIndex = shadowRequest.lightIndex;
                dss.splitData = shadowRequest.splitData;

                // TODO: remove this execute when DrawShadows will use a CommandBuffer
                renderContext.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                renderContext.DrawShadows(ref dss);
            }
            
            cmd.SetGlobalFloat(HDShaderIDs._ZClip, 1.0f);   // Re-enable zclip globally
        }

        public void DisplayAtlas(CommandBuffer cmd, Material debugMaterial, Rect atlasViewport, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue, bool flipY)
        {
            Vector4 validRange = new Vector4(minValue, 1.0f / (maxValue - minValue));
            float rWidth = 1.0f / m_Width;
            float rHeight = 1.0f / m_Height;
            Vector4 scaleBias = Vector4.Scale(new Vector4(rWidth, rHeight, rWidth, rHeight), new Vector4(atlasViewport.width, atlasViewport.height, atlasViewport.x, atlasViewport.y));

            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetTexture("_AtlasTexture", m_Atlas.rt);
            propertyBlock.SetVector("_TextureScaleBias", scaleBias);
            propertyBlock.SetVector("_ValidRange", validRange);
            propertyBlock.SetFloat("_RequireToFlipInputTexture", flipY ? 1.0f : 0.0f);
            cmd.SetViewport(new Rect(screenX, screenY, screenSizeX, screenSizeY));
            cmd.DrawProcedural(Matrix4x4.identity, debugMaterial, debugMaterial.FindPass("VARIANCESHADOW"), MeshTopology.Triangles, 3, 1, propertyBlock);
        }

        public void Clear()
        {
            shadowRequests.Clear();
        }

        public void Release()
        {
            RTHandles.Release(m_Atlas);
        }
    }
}