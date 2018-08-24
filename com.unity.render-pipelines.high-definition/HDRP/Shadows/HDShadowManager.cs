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
        // Use device projection matrix for shader and projection for CommandBuffer.SetViewProjectionMatrices
        public Matrix4x4            deviceProjection;
        public Matrix4x4            projection;
        public Matrix4x4            shadowToWorld;
        public Vector2              viewportSize;
        // Warning: this field is updated by ProcessShadowRequests and is invalid before
        public Rect                 atlasViewport;

        // Determine in which atlas the shadow will be rendered
        public bool                 allowResize = true;

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
        // List<HDShadowRequest>       m_ShadowRequests = new List<HDShadowRequest>(50);
        // List<HDShadowRequest>       m_ShadowCascadesRequests = new List<HDShadowRequest>(4);

        // Structured buffer of shadow datas
        // TODO: hardcoded max shadow data value
        ComputeBuffer               m_ShadowDataBuffer = new ComputeBuffer(64, System.Runtime.InteropServices.Marshal.SizeOf(typeof(HDShadowData)));

        // The two shadowmaps atlases we uses, one for directional cascade (without resize) and the second for the rest of the shadows
        HDShadowAtlas               m_CascadeAtlas;
        HDShadowAtlas               m_Atlas;

        int                         m_Width;
        int                         m_Height;

#if UNITY_EDITOR
        Dictionary<Light, List<HDShadowRequest>>    m_LightShadowRequests = new Dictionary<Light, List<HDShadowRequest>>();
        List<HDShadowRequest>                       m_CurrentLightShadowRequests;
#endif

        public HDShadowManager(int width, int height, Shader clearShader)
        {
            Material clearMaterial = CoreUtils.CreateEngineMaterial(clearShader);
            m_CascadeAtlas = new HDShadowAtlas(width, height, clearMaterial, name: "Cascade Shadow Map Atlas");
            m_Atlas = new HDShadowAtlas(width, height, clearMaterial, name: "Shadow Map Atlas");
            m_Width = width;
            m_Height = height;
        }

        public void AddShadowRequest(HDShadowRequest shadowRequest)
        {
            if (shadowRequest.allowResize)
            {
                // m_ShadowRequests.Add(shadowRequest);
                m_Atlas.Reserve(shadowRequest);
            }
            else
            {
                // m_ShadowCascadesRequests.Add(shadowRequest);
                m_CascadeAtlas.Reserve(shadowRequest);
            }
            
#if UNITY_EDITOR
            if (m_CurrentLightShadowRequests != null)
                m_CurrentLightShadowRequests.Add(shadowRequest);
#endif
        }

        HDShadowData CreateShadowData(HDShadowRequest shadowRequest)
        {
            HDShadowData data = new HDShadowData();

            data.projection = shadowRequest.deviceProjection;
            data.view = shadowRequest.view;

            // Compute the scale and offset (between 0 and 1) for the atlas coordinates
            float rWidth = 1.0f / m_Width;
            float rHeight = 1.0f / m_Height;
            Vector4 atlasViewport = new Vector4(shadowRequest.atlasViewport.x, shadowRequest.atlasViewport.y, shadowRequest.atlasViewport.width, shadowRequest.atlasViewport.height);
            data.scaleOffset = Vector4.Scale(new Vector4(rWidth, rHeight, 1, 1), atlasViewport);

            data.textureSize = new Vector4(m_Width, m_Height, shadowRequest.atlasViewport.x, shadowRequest.atlasViewport.y);
            data.texelSizeRcp = new Vector4(rWidth, rHeight, 1.0f / shadowRequest.atlasViewport.x, 1.0f / shadowRequest.atlasViewport.y);

            return data;
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
            foreach (var shadowRequest in m_Atlas.shadowRequests)
                m_ShadowDatas.Add(CreateShadowData(shadowRequest));
            foreach (var shadowRequest in m_CascadeAtlas.shadowRequests)
                m_ShadowDatas.Add(CreateShadowData(shadowRequest));
        }
 
        public void RenderShadows(ScriptableRenderContext renderContext, CommandBuffer cmd, CullResults cullResults)
        {
            // TODO remove DrawShadowSettings, lightIndex and splitData when scriptable culling is available
            DrawShadowsSettings dss = new DrawShadowsSettings(cullResults, 0);
            
            // Clear atlas render targets and draw shadows
            m_Atlas.RenderShadows(renderContext, cmd, dss);
            m_CascadeAtlas.RenderShadows(renderContext, cmd, dss);
        
            // Clear the shadows atlas infos and requests
            m_Atlas.Clear();
            m_CascadeAtlas.Clear();
        }

#if UNITY_EDITOR
        // To keep track of which light execute which shadow requests (only used for the debug menu option VisualizeShadowMap with Use Object Selection)
        public void SetCurrentLightShadows(Light light)
        {
            m_CurrentLightShadowRequests = m_LightShadowRequests[light] = new List<HDShadowRequest>();
        }

        public List<Light> GetLights()
        {
            return m_LightShadowRequests.Keys.ToList();
        }

        public int GetShadowRequestCountForLight(Light light)
        {
            List<HDShadowRequest>   shadowRequests;
            int                     count = 0;

            if (m_LightShadowRequests.TryGetValue(light, out shadowRequests))
                count = shadowRequests.Count;
            return count;
        }
#endif
        
        // Warning: must be called after ProcessShadowRequests and RenderShadows to have valid informations
        public void DisplayShadowAtlas(CommandBuffer cmd, Material debugMaterial, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue, bool flipY)
        {
            //TODO display debug shadow map
            m_Atlas.DisplayAtlas(cmd, debugMaterial, new Rect(m_Width, m_Height, 0, 0), screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue, flipY);
        }
        
        // Warning: must be called after ProcessShadowRequests and RenderShadows to have valid informations
        public void DisplayShadowCascadeAtlas(CommandBuffer cmd, Material debugMaterial, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue, bool flipY)
        {
            m_CascadeAtlas.DisplayAtlas(cmd, debugMaterial, new Rect(m_Width, m_Height, 0, 0), screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue, flipY);
        }

        // Warning: must be called after ProcessShadowRequests and RenderShadows to have valid informations
        public void DisplayShadowMapForLight(Light light, int shadowMapIndex, CommandBuffer cmd, Material debugMaterial, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue, bool flipY)
        {
            List<HDShadowRequest>   shadowRequests;

            if (!m_LightShadowRequests.TryGetValue(light, out shadowRequests))
                return;
            if (shadowMapIndex >= shadowRequests.Count)
                return;

            // TODO: manage cascade shadow atlas here
            m_Atlas.DisplayAtlas(cmd, debugMaterial, shadowRequests[shadowMapIndex].atlasViewport, screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue, flipY);
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