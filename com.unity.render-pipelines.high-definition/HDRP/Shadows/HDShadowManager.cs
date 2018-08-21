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
        public Vector4      scaleOffset;
        
        // TODO: add all the bias and filter stuff
    }

    public class HDShadowRequest
    {
        public Matrix4x4            view;
        public Matrix4x4            projection;
        public Vector2              viewportSize;

        //TODO: add all the bias and filter stuff
    }

    public class HDShadowManager
    {
        List<HDShadowData>          m_ShadowDatas = new List<HDShadowData>();

        // By default we reserve a bit of space to prevent a part of the Add() allocation
        List<HDShadowRequest>       m_ShadowRequests = new List<HDShadowRequest>(50);

        // Structured buffer of shadow datas
        // TODO: hardcoded max shadow data value
        ComputeBuffer               m_ShadowDataBuffer = new ComputeBuffer(64, System.Runtime.InteropServices.Marshal.SizeOf(typeof(HDShadowData)));

        // The two shadowmaps atlases we uses, one for directional cascade and the second for the rest of the lights
        HDShadowAtlas               m_CascadesAtlas = new HDShadowAtlas();
        HDShadowAtlas               m_Atlas = new HDShadowAtlas();

        public HDShadowManager(int width, int height)
        {
            m_CascadesAtlas.AllocateShadowMaps(width, height);
            m_Atlas.AllocateShadowMaps(width, height);
        }

        public void AddShadowRequest(HDShadowRequest shadowRequest, HDShadowAtlas atlas)
        {
            m_ShadowRequests.Add(shadowRequest);

            atlas.Reserve(shadowRequest);
        }

        public void ProcessShadowRequests(CullResults cullResults, Camera camera)
        {
            // TODO: prune all shadow we dont need to render

            // TODO maybe: sort all shadows by "importance" (aka size on screen)

            m_ShadowDatas.Clear();

            // Create all HDShadowDatas and update them with shadow request datas
            uint requestCount = (uint)m_ShadowRequests.Count;
            for (uint i = 0; i < requestCount; i++)
            {
                var shadowRequest = m_ShadowRequests[(int)i];
                HDShadowData data = new HDShadowData();

                data.projection = shadowRequest.projection;
                data.view = shadowRequest.view;
                data.scaleOffset = new Vector4(0, 0, shadowRequest.viewportSize.x, shadowRequest.viewportSize.y);
            }

            // Sort and resize all the shadows in the atlas so everything can fit
            m_CascadesAtlas.LayoutAndResize();
            m_Atlas.LayoutAndResize();
        }
        
        public void RenderShadows(ScriptableRenderContext renderContext, CommandBuffer cmd, CullResults cullResults, List<VisibleLight> lights)
        { 

            // TODO: Render shadow maps
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
        
        public void BindResources(CommandBuffer cmd, ComputeShader computeShader, int computeKernel)
        {
            // This code must be in sync with ShadowContext.hlsl
            cmd.SetGlobalBuffer(HDShaderIDs._ShadowDatasExp, m_ShadowDataBuffer);

            cmd.SetGlobalTexture(HDShaderIDs._ShadowmapExp_PCF, m_Atlas.identifier);
        }

        public HDShadowAtlas GetCascadeAtlas()
        {
            return m_CascadesAtlas;
        }

        public HDShadowAtlas GetAtlas()
        {
            return m_Atlas;
        }
    }
}