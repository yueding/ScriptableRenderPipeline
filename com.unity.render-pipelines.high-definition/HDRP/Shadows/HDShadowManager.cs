using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [GenerateHLSL]
    public struct HDShadowData
    {
        public Matrix4x4    view;
        // TODO: rename this, it's the device projection matrix
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
        public int          flags;
        public float        edgeTolerance;

        // TODO: remove these fields, they should not be required in further version (and refactor HDShadowAlgorithms.hlsl)
        public Vector3      rot0;
        public Vector3      rot1;
        public Vector3      rot2;
        public Vector3      pos;
        public Vector4      proj;
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
        // Use device projection matrix for shader and projection for CommandBuffer.SetViewProjectionMatrices
        public Matrix4x4            deviceProjection;
        public Matrix4x4            projection;
        public Matrix4x4            shadowToWorld;
        public Vector2              viewportSize;
        // Warning: this field is updated by ProcessShadowRequests and is invalid before
        public Rect                 atlasViewport;
        public bool                 zClip;

        // Store the final shadow indice in the shadow data array
        // Warning: the index is computed during ProcessShadowRequest and so is invalid before calling this function
        public int                  shadowIndex;

        // Determine in which atlas the shadow will be rendered
        public bool                 allowResize = true;

        // TODO: Remove these field once scriptable culling is here (currently required by ScriptableRenderContext.DrawShadows)
        public int                  lightIndex;
        public ShadowSplitData      splitData;
        // end

        public Vector4      viewBias;
        public Vector4      normalBias;
        public float        edgeTolerance;
        public int          flags;
        
        public Vector3      rot0;
        public Vector3      rot1;
        public Vector3      rot2;
        public Vector3      pos;
        public Vector4      proj;
    }

    public class HDShadowManager : IDisposable
    {
        List<HDShadowData>          m_ShadowDatas = new List<HDShadowData>();
        List<HDShadowRequest>       m_ShadowRequests = new List<HDShadowRequest>();

        HDDirectionalShadowData     m_DirectionalShadowData;

        // Structured buffer of shadow datas
        // TODO: hardcoded max shadow data value
        ComputeBuffer               m_ShadowDataBuffer;
        ComputeBuffer               m_DirectionalShadowDataBuffer;

        // The two shadowmaps atlases we uses, one for directional cascade (without resize) and the second for the rest of the shadows
        HDShadowAtlas               m_CascadeAtlas;
        HDShadowAtlas               m_Atlas;

        int                         m_Width;
        int                         m_Height;
        int                         m_maxShadowRequests;

        public HDShadowManager(int width, int height, int maxShadowRequests, bool shadowMap16Bt, Shader clearShader)
        {
            Material clearMaterial = CoreUtils.CreateEngineMaterial(clearShader);
            // TODO: 32 bit shadowmap are not supported by RThandle currently, when it will be change Depth24 to Depth32
            DepthBits depthBits = (shadowMap16Bt) ? DepthBits.Depth16 : DepthBits.Depth24;
            m_CascadeAtlas = new HDShadowAtlas(width, height, clearMaterial, depthBufferBits: depthBits, name: "Cascade Shadow Map Atlas");
            m_Atlas = new HDShadowAtlas(width, height, clearMaterial, depthBufferBits: depthBits, name: "Shadow Map Atlas");

            m_ShadowDataBuffer = new ComputeBuffer(maxShadowRequests, System.Runtime.InteropServices.Marshal.SizeOf(typeof(HDShadowData)));
            m_DirectionalShadowDataBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(HDDirectionalShadowData)));

            m_Width = width;
            m_maxShadowRequests = maxShadowRequests;
            m_Height = height;
        }

        public int AddShadowRequest(HDShadowRequest shadowRequest)
        {
            if (m_ShadowRequests.Count >= m_maxShadowRequests)
                return -1;
            
            if (shadowRequest.allowResize)
                m_Atlas.Reserve(shadowRequest);
            else
                m_CascadeAtlas.Reserve(shadowRequest);
            
            // Keep track of all shadow request and the order they was requested
            m_ShadowRequests.Add(shadowRequest);

            return m_ShadowRequests.Count - 1;
        }

        public void UpdateCascade(int cascadeIndex, Vector4 cullingSphere, float border)
        {
            if (cullingSphere.w != float.NegativeInfinity)
            {
                cullingSphere.w *= cullingSphere.w;
            }

            // TODO: refactor this once we can generate arrays in hlsl structs
            switch (cascadeIndex)
            {
                case 0:
                    m_DirectionalShadowData.sphereCascade1 = cullingSphere;
                    m_DirectionalShadowData.cascadeBorder1 = border;
                    break ;
                case 1:
                    m_DirectionalShadowData.sphereCascade2 = cullingSphere;
                    m_DirectionalShadowData.cascadeBorder2 = border;
                    break ;
                case 2:
                    m_DirectionalShadowData.sphereCascade3 = cullingSphere;
                    m_DirectionalShadowData.cascadeBorder3 = border;
                    break ;
                case 3:
                    m_DirectionalShadowData.sphereCascade4 = cullingSphere;
                    m_DirectionalShadowData.cascadeBorder4 = border;
                    break ;
            }
        }

        HDShadowData CreateShadowData(HDShadowRequest shadowRequest)
        {
            HDShadowData data = new HDShadowData();

            data.projection = shadowRequest.deviceProjection;
            data.view = shadowRequest.view;
            data.shadowToWorld = shadowRequest.shadowToWorld;

            // Compute the scale and offset (between 0 and 1) for the atlas coordinates
            float rWidth = 1.0f / m_Width;
            float rHeight = 1.0f / m_Height;
            Vector4 atlasViewport = new Vector4(shadowRequest.atlasViewport.width, shadowRequest.atlasViewport.height, shadowRequest.atlasViewport.x, shadowRequest.atlasViewport.y);
            data.scaleOffset = Vector4.Scale(new Vector4(rWidth, rHeight, rWidth, rHeight), atlasViewport);

            data.textureSize = new Vector4(m_Width, m_Height, shadowRequest.atlasViewport.width, shadowRequest.atlasViewport.height);
            data.texelSizeRcp = new Vector4(rWidth, rHeight, 1.0f / shadowRequest.atlasViewport.width, 1.0f / shadowRequest.atlasViewport.height);

            data.viewBias = shadowRequest.viewBias;
            data.normalBias = shadowRequest.normalBias;
            data.edgeTolerance = shadowRequest.edgeTolerance;
            data.flags = shadowRequest.flags;

            data.pos = shadowRequest.pos;
            data.proj = shadowRequest.proj;
            data.rot0 = shadowRequest.rot0;
            data.rot1 = shadowRequest.rot1;
            data.rot2 = shadowRequest.rot2;

            return data;
        }

        public void ProcessShadowRequests(CullResults cullResults, Camera camera)
        {
            int shadowIndex = 0;

            // TODO: prune all shadow we dont need to render
            // TODO maybe: sort all shadows by "importance" (aka size on screen)
            
            // Assign a position to all the shadows in the atlas, and scale shadows if needed
            m_CascadeAtlas.Layout(false);
            m_Atlas.Layout();

            m_ShadowDatas.Clear();

            // Create all HDShadowDatas and update them with shadow request datas
            foreach (var shadowRequest in m_ShadowRequests)
            {
                m_ShadowDatas.Add(CreateShadowData(shadowRequest));
                shadowRequest.shadowIndex = shadowIndex++;
            }

            // Update directional datas:
            m_DirectionalShadowData.cascadeDirection = (m_DirectionalShadowData.sphereCascade2 - m_DirectionalShadowData.sphereCascade2).normalized;
        }
 
        public void RenderShadows(ScriptableRenderContext renderContext, CommandBuffer cmd, CullResults cullResults)
        {
            // Avoid to do any commands if there is no shadow to draw 
            if (m_ShadowRequests.Count == 0)
                return ;
            
            Debug.Log("shadow request count: " + m_ShadowRequests.Count);

            // TODO remove DrawShadowSettings, lightIndex and splitData when scriptable culling is available
            DrawShadowsSettings dss = new DrawShadowsSettings(cullResults, 0);

            // Clear atlas render targets and draw shadows
            m_Atlas.RenderShadows(renderContext, cmd, dss);
            m_CascadeAtlas.RenderShadows(renderContext, cmd, dss);
        }
        
        public void SyncData()
        {
            // Avoid to upload datas which will not be used
            if (m_ShadowRequests.Count == 0)
                return;
            
            // Upload the shadow buffers to GPU
            m_ShadowDataBuffer.SetData(m_ShadowDatas);
            m_DirectionalShadowDataBuffer.SetData(new HDDirectionalShadowData[]{ m_DirectionalShadowData });
        }
        
        public void BindResources(CommandBuffer cmd)
        {
            // This code must be in sync with ShadowContext.hlsl
            cmd.SetGlobalBuffer(HDShaderIDs._HDShadowDatas, m_ShadowDataBuffer);
            cmd.SetGlobalBuffer(HDShaderIDs._HDDirectionalShadowData, m_DirectionalShadowDataBuffer);

            cmd.SetGlobalTexture(HDShaderIDs._ShadowmapAtlas, m_Atlas.identifier);
            cmd.SetGlobalTexture(HDShaderIDs._ShadowmapCascadeAtlas, m_CascadeAtlas.identifier);
        }

        public int GetShadowRequestCount()
        {
            return m_ShadowRequests.Count;
        }

        public void Clear()
        {
            // Clear the shadows atlas infos and requests
            m_Atlas.Clear();
            m_CascadeAtlas.Clear();

            m_ShadowRequests.Clear();
        }

        // Warning: must be called after ProcessShadowRequests and RenderShadows to have valid informations
        public void DisplayShadowAtlas(CommandBuffer cmd, Material debugMaterial, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue, bool flipY)
        {
            //TODO display debug shadow map
            m_Atlas.DisplayAtlas(cmd, debugMaterial, new Rect(0, 0, m_Width, m_Height), screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue, flipY);
        }
        
        // Warning: must be called after ProcessShadowRequests and RenderShadows to have valid informations
        public void DisplayShadowCascadeAtlas(CommandBuffer cmd, Material debugMaterial, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue, bool flipY)
        {
            m_CascadeAtlas.DisplayAtlas(cmd, debugMaterial, new Rect(0, 0, m_Width, m_Height), screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue, flipY);
        }

        // Warning: must be called after ProcessShadowRequests and RenderShadows to have valid informations
        public void DisplayShadowMap(int shadowIndex, CommandBuffer cmd, Material debugMaterial, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue, bool flipY)
        {
            if (shadowIndex >= m_ShadowRequests.Count)
                return;

            HDShadowRequest   shadowRequest = m_ShadowRequests[shadowIndex];

            if (shadowRequest.allowResize)
                m_Atlas.DisplayAtlas(cmd, debugMaterial, shadowRequest.atlasViewport, screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue, flipY);
            else
                m_CascadeAtlas.DisplayAtlas(cmd, debugMaterial, shadowRequest.atlasViewport, screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue, flipY);
        }

        public void Dispose()
        {
            m_ShadowDataBuffer.Dispose();
            m_DirectionalShadowDataBuffer.Dispose();
            m_Atlas.Release();
            m_CascadeAtlas.Release();
        }
    }
}