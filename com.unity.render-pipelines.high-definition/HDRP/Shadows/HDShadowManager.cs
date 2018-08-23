using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [GenerateHLSL]
    public struct HDShadowData
    {
        public Matrix4x4    view;
        public Matrix4x4    projection;
        public Matrix4x4    shadowToWorld;
        public Vector4      scaleOffset;

        // These fields are only for test purpose for HDShadowAlgorithm.hlsl
        // and should be renamed/removed when we have a stable version
        public Vector4      textureSize;
        public Vector4      texelSizeRcp;

        // TODO: refactor the bias/filter params, they're currently based on the Core shadow system
        public Vector4      viewBias;
        public Vector4      normalBias;
        public float        edgeTolerance;
    }

    // We use a different structure for directional light because these is a lot of data there
    // and it will add too much useless stuff for other lights
    [GenerateHLSL]
    public struct HDDirectionalShadowData
    {
        public Vector4      sphereCascade1;
        public Vector4      sphereCascade2;
        public Vector4      sphereCascade3;
        public Vector4      sphereCascade4;

        public Vector4      cascadeDirection;

        public float        cascadeBorder1;
        public float        cascadeBorder2;
        public float        cascadeBorder3;
        public float        cascadeBorder4;
    }

    public class HDShadowRequest
    {
        public Matrix4x4            view;
        public Matrix4x4            projection;
        public Matrix4x4            shadowToWorld;
        public Vector2              viewportSize;
        // Warning: this field is updated by ProcessShadowRequests and is invalid before
        public Rect                 atlasViewport;

        // TODO: Remove these field once scriptable culling is here (currently required by ScriptableRenderContext.DrawShadows)
        public int                  lightIndex;
        public ShadowSplitData      splitData;
        // end

        // TODO: add all the bias and filter stuff
    }

    public class HDShadowManager : IDisposable
    {
        List<HDShadowData>          m_ShadowDatas = new List<HDShadowData>();

        // By default we reserve a bit of space to prevent a part of the Add() allocation
        List<HDShadowRequest>       m_ShadowRequests = new List<HDShadowRequest>(50);

        // Structured buffer of shadow datas
        // TODO: hardcoded max shadow data value
        ComputeBuffer               m_ShadowDataBuffer = new ComputeBuffer(64, System.Runtime.InteropServices.Marshal.SizeOf(typeof(HDShadowData)));

        // The two shadowmaps atlases we uses, one for directional cascade (without resize) and the second for the rest of the shadows
        HDShadowAtlas               m_CascadeAtlas = new HDShadowAtlas();
        HDShadowAtlas               m_Atlas = new HDShadowAtlas();

        int                         m_Width;
        int                         m_Height;

        public HDShadowManager(int width, int height)
        {
            m_CascadeAtlas.AllocateShadowMaps(width, height);
            m_Atlas.AllocateShadowMaps(width, height);
            m_Width = width;
            m_Height = height;
        }

        public void AddShadowRequest(HDShadowRequest shadowRequest, bool allowResize = true)
        {
            m_ShadowRequests.Add(shadowRequest);

            if (allowResize)
                m_CascadeAtlas.Reserve(shadowRequest);
            else
                m_Atlas.Reserve(shadowRequest);
        }

        public void ProcessShadowRequests(CullResults cullResults, Camera camera)
        {
            // TODO: prune all shadow we dont need to render

            // TODO maybe: sort all shadows by "importance" (aka size on screen)
            
            // Assign a position to all the shadows in the atlas, and scale shadows if needed
            m_CascadeAtlas.Layout(false);
            m_Atlas.Layout();

            m_ShadowDatas.Clear();

            // Create all HDShadowDatas and update them with shadow request datas
            uint requestCount = (uint)m_ShadowRequests.Count;
            for (uint i = 0; i < requestCount; i++)
            {
                var shadowRequest = m_ShadowRequests[(int)i];
                HDShadowData data = new HDShadowData();

                data.projection = shadowRequest.projection;
                data.view = shadowRequest.view;

                // Compute the scale and offset (between 0 and 1) for the atlas coordinates
                float rWidth = 1.0f / m_Width;
                float rHeight = 1.0f / m_Height;
                Vector4 atlasViewport = new Vector4(shadowRequest.atlasViewport.x, shadowRequest.atlasViewport.y, shadowRequest.atlasViewport.width, shadowRequest.atlasViewport.height);
                data.scaleOffset = Vector4.Scale(new Vector4(rWidth, rHeight, 1, 1), atlasViewport);

                data.textureSize = new Vector4(m_Width, m_Height, shadowRequest.atlasViewport.x, shadowRequest.atlasViewport.y);
                data.texelSizeRcp = new Vector4(rWidth, rHeight, 1.0f / shadowRequest.atlasViewport.x, 1.0f / shadowRequest.atlasViewport.y);
            }
        }
        
        public void RenderShadows(ScriptableRenderContext renderContext, CommandBuffer cmd, CullResults cullResults)
        {
            // TODO when scriptable culling is available: remove 
            DrawShadowsSettings dss = new DrawShadowsSettings(cullResults, 0);

            foreach (var shadowRequest in m_ShadowRequests)
            {
                cmd.SetViewport(shadowRequest.atlasViewport);
                cmd.SetViewProjectionMatrices(shadowRequest.view, shadowRequest.projection);

                dss.lightIndex = shadowRequest.lightIndex;
                dss.splitData = shadowRequest.splitData;

                // TODO: remove this execute when DrawShadows will use a CommandBuffer
                renderContext.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                renderContext.DrawShadows(ref dss);
            }
        
            // Clear the shadows atlas infos and requests
            m_ShadowRequests.Clear();
            m_Atlas.Clear();
            m_CascadeAtlas.Clear();
        }
        
        public void DisplayShadow(CommandBuffer cmd, Material debugMaterial, int shadowIndex, uint faceIndex, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue, bool flipY)
        {
            //TODO display debug shadow
        }
        
        public void DisplayShadowMap(CommandBuffer cmd, Material debugMaterial, uint shadowMapIndex, uint sliceIndex, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue, bool flipY)
        {
            //TODO display debug shadow map
        }
        
        public void SyncData()
        {
            // Upload the shadow buffers to GPU
            m_ShadowDataBuffer.SetData(m_ShadowDatas);
        }
        
        public void BindResources(CommandBuffer cmd)
        {
            // This code must be in sync with ShadowContext.hlsl
            cmd.SetGlobalBuffer(HDShaderIDs._HDShadowDatas, m_ShadowDataBuffer);
            cmd.SetGlobalBuffer(HDShaderIDs._HDDirectionalShadowData, m_ShadowDataBuffer);

            cmd.SetGlobalTexture(HDShaderIDs._ShadowmapAtlas, m_Atlas.identifier);
            cmd.SetGlobalTexture(HDShaderIDs._ShadowmapCascadeAtlas, m_CascadeAtlas.identifier);
        }

        public void Dispose()
        {
            m_ShadowDataBuffer.Dispose();
            m_Atlas.Release();
            m_CascadeAtlas.Release();
        }
    }
}