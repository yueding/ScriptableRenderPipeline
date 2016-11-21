using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine.MaterialGraph;
using UnityEngine.Graphing;

namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    [Serializable]
    public abstract class AbstractHDRenderLoopMasterNode : AbstractMasterNode
    {
        public AbstractHDRenderLoopMasterNode()
        {
            name = GetName();
            UpdateNodeAfterDeserialization();
        }

        protected abstract Type GetSurfaceType();
        protected abstract string GetName();
        protected abstract int GetMatchingMaterialID();

        public sealed override void UpdateNodeAfterDeserialization()
        {
            var surfaceType = GetSurfaceType();
            if (surfaceType != null)
            {
                var fieldsBuiltIn = typeof(Builtin.BuiltinData).GetFields();
                var fieldsSurface = surfaceType.GetFields();
                var slots = fieldsSurface.Concat(fieldsBuiltIn).Select((field, index) =>
                {
                    var attributes = (SurfaceDataAttributes[])field.GetCustomAttributes(typeof(SurfaceDataAttributes), false);
                    var attribute = attributes.Length > 0 ? attributes[0] : new SurfaceDataAttributes();

                    var valueType = SlotValueType.Dynamic;
                    var fieldType = field.FieldType;
                    if (fieldType == typeof(float))
                    {
                        valueType = SlotValueType.Vector1;
                    }
                    else if (fieldType == typeof(Vector2))
                    {
                        valueType = SlotValueType.Vector2;
                    }
                    else if (fieldType == typeof(Vector3))
                    {
                        valueType = SlotValueType.Vector3;
                    }
                    else if (fieldType == typeof(Vector2))
                    {
                        valueType = SlotValueType.Vector4;
                    }

                    return new
                    {
                        index = index,
                        priority = attribute.priority,
                        displayName = attribute.displayName,
                        materialID = attribute.filter,
                        shaderOutputName = field.Name,
                        valueType = valueType
                    };
                })
                .Where(o => (o.materialID == null || o.materialID.Contains(GetMatchingMaterialID())) && o.valueType != SlotValueType.Dynamic)
                .OrderBy(o => o.priority)
                .ThenBy(o => o.displayName)
                .ToArray();

                foreach (var slot in slots)
                {
                    if (slot.displayName == "Normal") //WIP : should be a setting in attribute
                    {
                        AddSlot(new MaterialSlotDefaultInput(slot.index, slot.displayName, slot.shaderOutputName, Graphing.SlotType.Input, slot.valueType, new WorldSpaceNormalNode(), 0));
                    }
                    else
                    {
                        AddSlot(new MaterialSlot(slot.index, slot.displayName, slot.shaderOutputName, Graphing.SlotType.Input, slot.valueType, Vector4.zero));
                    }
                }
            }
        }

        private static void CollectFromNodesFromNodes(List<INode> nodeList, INode node, List<int> slotId)
        {
            // no where to start
            if (node == null)
                return;

            // allready added this node
            if (nodeList.Contains(node))
                return;

            // if we have a slot passed in but can not find it on the node abort
            if (slotId != null && node.GetInputSlots<ISlot>().All(x => !slotId.Contains(x.id)))
                return;

            var validSlots = ListPool<int>.Get();
            if (slotId != null)
                slotId.ForEach(x => validSlots.Add(x));
            else
                validSlots.AddRange(node.GetInputSlots<ISlot>().Select(x => x.id));

            foreach (var slot in validSlots)
            {
                foreach (var edge in node.owner.GetEdges(node.GetSlotReference(slot)))
                {
                    var outputNode = node.owner.GetNodeFromGuid(edge.outputSlot.nodeGuid);
                    CollectFromNodesFromNodes(nodeList, outputNode, null);
                }
            }
            nodeList.Add(node);
            ListPool<int>.Release(validSlots);
        }

        private struct Vayring
        {
            public string attributeName;
            public string semantic;
            public string vayringName;
            public SlotValueType type;
            public string vertexCode;
            public string pixelCode;
        };

        private string GenerateLitDataTemplate(GenerationMode mode, string useDataInput, string needFragInput, PropertyGenerator propertyGenerator, ShaderGenerator propertyUsagesVisitor, ShaderGenerator shaderFunctionVisitor)
        {
            var activeNodeList = new List<INode>();

            var useDataInputRegex = new Regex(useDataInput);
            var needFragInputRegex = new Regex(needFragInput);
            var slotIDList = GetInputSlots<MaterialSlot>().Where(s => useDataInputRegex.IsMatch(s.shaderOutputName)).Select(s => s.id).ToList();

            CollectFromNodesFromNodes(activeNodeList, this, slotIDList);

            var vayrings = new List<Vayring>();
            if (needFragInputRegex.IsMatch("meshUV0") || activeNodeList.OfType<IMayRequireMeshUV>().Any(x => x.RequiresMeshUV()))
            {
                vayrings.Add(new Vayring()
                {
                    attributeName = "meshUV0",
                    semantic = "TEXCOORD0",
                    vayringName = "meshUV0",
                    type = SlotValueType.Vector2,
                    vertexCode = "output.meshUV0 = input.meshUV0;",
                    pixelCode = string.Format("float4 {0} = float4(fragInput.meshUV0, 0, 0);", ShaderGeneratorNames.UV0)
                });
            }

            if (needFragInputRegex.IsMatch("normalWS") || activeNodeList.OfType<IMayRequireNormal>().Any(x => x.RequiresNormal()))
            {
                vayrings.Add(new Vayring()
                {
                    attributeName = "normalOS",
                    semantic = "NORMAL",
                    vayringName = "normalWS",
                    type = SlotValueType.Vector3,
                    vertexCode = "output.normalWS = TransformObjectToWorldNormal(input.normalOS);",
                    pixelCode = string.Format("float3 {0} = normalize(fragInput.normalWS);", ShaderGeneratorNames.WorldSpaceNormal)
                });
            }

            if (needFragInputRegex.IsMatch("positionWS") || activeNodeList.OfType<IMayRequireWorldPosition>().Any(x => x.RequiresWorldPosition()))
            {
                vayrings.Add(new Vayring()
                {
                    vayringName = "positionWS",
                    type = SlotValueType.Vector3,
                    vertexCode = "output.positionWS = TransformObjectToWorld(input.positionOS);",
                    pixelCode = string.Format("float3 {0} = fragInput.positionWS;", ShaderGeneratorNames.WorldSpacePosition)
                });
            }

            if (needFragInputRegex.IsMatch("viewDirectionWS") || activeNodeList.OfType<IMayRequireViewDirection>().Any(x => x.RequiresViewDirection()))
            {
                vayrings.Add(new Vayring()
                {
                    vayringName = "viewDirectionWS",
                    type = SlotValueType.Vector3,
                    vertexCode = "output.viewDirectionWS = GetWorldSpaceNormalizeViewDir(TransformObjectToWorld(input.positionOS));",
                    pixelCode = string.Format("float3 {0} = normalize(fragInput.viewDirectionWS);", ShaderGeneratorNames.WorldSpaceViewDirection)
                });
            }

            Func<SlotValueType, int> _fnTypeToSize = o =>
            {
                switch (o)
                {
                    case SlotValueType.Vector1: return 1;
                    case SlotValueType.Vector2: return 2;
                    case SlotValueType.Vector3: return 3;
                    case SlotValueType.Vector4: return 4;
                }
                return 0;
            };

            var packedVarying = new ShaderGenerator();
            int totalSize = vayrings.Sum(x => _fnTypeToSize(x.type));

            if (totalSize > 0)
            {
                var interpolatorCount = Mathf.Ceil((float)totalSize / 4.0f);
                packedVarying.AddShaderChunk(string.Format("float4 interpolators[{0}] : TEXCOORD0;", (int)interpolatorCount), false);
            }

            var vayringVisitor = new ShaderGenerator();
            var pixelShaderInitVisitor = new ShaderGenerator();
            var vertexAttributeVisitor = new ShaderGenerator();
            var vertexShaderBodyVisitor = new ShaderGenerator();
            var packInterpolatorVisitor = new ShaderGenerator();
            var unpackInterpolatorVisitor = new ShaderGenerator();
            int currentIndex = 0;
            int currentChannel = 0;
            foreach (var vayring in vayrings)
            {
                var typeSize = _fnTypeToSize(vayring.type);
                if (!string.IsNullOrEmpty(vayring.attributeName))
                {
                    vertexAttributeVisitor.AddShaderChunk(string.Format("float{0} {1} : {2};", typeSize, vayring.attributeName, vayring.semantic), true);
                }

                vayringVisitor.AddShaderChunk(string.Format("float{0} {1};", typeSize, vayring.vayringName), false);
                vertexShaderBodyVisitor.AddShaderChunk(vayring.vertexCode, false);
                pixelShaderInitVisitor.AddShaderChunk(vayring.pixelCode, false);

                for (int channel = 0; channel < typeSize; ++channel)
                {
                    var packed = string.Format("interpolators[{0}][{1}]", currentIndex, currentChannel);
                    var source = string.Format("{0}[{1}]", vayring.vayringName, channel);
                    packInterpolatorVisitor.AddShaderChunk(string.Format("output.{0} = input.{1};", packed, source), false);
                    unpackInterpolatorVisitor.AddShaderChunk(string.Format("output.{0} = input.{1};", source, packed), false);

                    if (currentChannel == 3)
                    {
                        currentChannel = 0;
                        currentIndex++;
                    }
                    else
                    {
                        currentChannel++;
                    }
                }
            }

            foreach (var node in activeNodeList.OfType<AbstractMaterialNode>())
            {
                if (node is IGeneratesFunction) (node as IGeneratesFunction).GenerateNodeFunction(shaderFunctionVisitor, mode);
                if (node is IGenerateProperties)
                {
                    (node as IGenerateProperties).GeneratePropertyBlock(propertyGenerator, mode);
                    (node as IGenerateProperties).GeneratePropertyUsages(propertyUsagesVisitor, mode);
                }
            }

            var pixelShaderBodyVisitor = new ShaderGenerator();
            foreach (var node in activeNodeList)
            {
                if (node is IGeneratesBodyCode)
                    (node as IGeneratesBodyCode).GenerateNodeCode(pixelShaderBodyVisitor, mode);
            }

            foreach (var slot in GetInputSlots<MaterialSlot>())
            {
                if (!slotIDList.Contains(slot.id))
                    continue;

                foreach (var edge in owner.GetEdges(slot.slotReference))
                {
                    var outputRef = edge.outputSlot;
                    var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(outputRef.nodeGuid);
                    if (fromNode == null)
                        continue;

                    var slotOutputName = slot.shaderOutputName;

                    var inputStruct = typeof(Lit.SurfaceData).GetFields().Any(o => o.Name == slotOutputName) ? "surfaceData" : "builtinData";
                    pixelShaderBodyVisitor.AddShaderChunk(inputStruct + "." + slot.shaderOutputName + " = " + fromNode.GetVariableNameForSlot(outputRef.slotId) + ";", true);
                }
            }

            var template =
@"struct FragInput
{
    float4 unPositionSS;
${VaryingAttributes}
};

void GetSurfaceAndBuiltinData(FragInput fragInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
    ZERO_INITIALIZE(SurfaceData, surfaceData);
    ZERO_INITIALIZE(BuiltinData, builtinData);
${PixelShaderInitialize}
${PixelShaderBody}
}

struct Attributes
{
    float3 positionOS : POSITION;
${VertexAttributes}
};

struct Varyings
{
    float4 positionHS;
${VaryingAttributes}
};

struct PackedVaryings
{
    float4 positionHS : SV_Position;
${PackedVaryingAttributes}
};

PackedVaryings PackVaryings(Varyings input)
{
    PackedVaryings output;
    output.positionHS = input.positionHS;
${PackingVaryingCode}
    return output;
}

FragInput UnpackVaryings(PackedVaryings input)
{
    FragInput output;
    ZERO_INITIALIZE(FragInput, output);

    output.unPositionSS = input.positionHS;
${UnpackVaryingCode}
    return output;
}

PackedVaryings VertDefault(Attributes input)
{
    Varyings output;
    output.positionHS = TransformWorldToHClip(TransformObjectToWorld(input.positionOS));
${VertexShaderBody}
    return PackVaryings(output);
}";

            var resultShader = template.Replace("${VaryingAttributes}", vayringVisitor.GetShaderString(1));
            resultShader = resultShader.Replace("${PixelShaderInitialize}", pixelShaderInitVisitor.GetShaderString(1));
            resultShader = resultShader.Replace("${PixelShaderBody}", pixelShaderBodyVisitor.GetShaderString(1));
            resultShader = resultShader.Replace("${VertexAttributes}", vertexAttributeVisitor.GetShaderString(1));
            resultShader = resultShader.Replace("${PackedVaryingAttributes}", packedVarying.GetShaderString(1));
            resultShader = resultShader.Replace("${PackingVaryingCode}", packInterpolatorVisitor.GetShaderString(1));
            resultShader = resultShader.Replace("${UnpackVaryingCode}", unpackInterpolatorVisitor.GetShaderString(1));
            resultShader = resultShader.Replace("${VertexShaderBody}", vertexShaderBodyVisitor.GetShaderString(1));
            return resultShader;
        }

        public override string GetShader(MaterialOptions options, GenerationMode mode, out List<PropertyGenerator.TextureInfo> configuredTextures)
        {
            configuredTextures = new List<PropertyGenerator.TextureInfo>();

            var path = "Assets/ScriptableRenderLoop/HDRenderLoop/Shaders/Material/Lit/Lit.template";
            if (!System.IO.File.Exists(path))
                return "";

            var templateText = System.IO.File.ReadAllText(path);

            var shaderPropertiesVisitor = new PropertyGenerator();
            var propertyUsagesVisitor = new ShaderGenerator();
            var shaderFunctionVisitor = new ShaderGenerator();
            var templateToShader = new Dictionary<string, string>();

            var findLitShareTemplate = new System.Text.RegularExpressions.Regex("#{LitTemplate.*}");
            var findUseDataInput = new System.Text.RegularExpressions.Regex("useDataInput:{(.*?)}");
            var findNeedFragInput = new System.Text.RegularExpressions.Regex("needFragInput:{(.*?)}");
            foreach (System.Text.RegularExpressions.Match match in findLitShareTemplate.Matches(templateText))
            {
                if (match.Captures.Count > 0)
                {
                    var capture = match.Captures[0].Value;

                    if (!templateToShader.ContainsKey(capture))
                    {
                        var useUseDataInputRegex = "";
                        if (findUseDataInput.IsMatch(capture))
                        {
                            var useInputMatch = findUseDataInput.Match(capture);
                            useUseDataInputRegex = useInputMatch.Groups.Count > 1 ? useInputMatch.Groups[1].Value : "";
                        }

                        var needFragInputRegex = "";
                        if (findNeedFragInput.IsMatch(capture))
                        {
                            var useInputMatch = findNeedFragInput.Match(capture);
                            needFragInputRegex = useInputMatch.Groups.Count > 1 ? useInputMatch.Groups[1].Value : "";
                        }

                        var generatedShader = GenerateLitDataTemplate(mode, useUseDataInputRegex, needFragInputRegex, shaderPropertiesVisitor, propertyUsagesVisitor, shaderFunctionVisitor);
                        templateToShader.Add(capture, generatedShader);
                    }
                }
            }

            var resultShader = templateText.Replace("${ShaderName}", GetType() + guid.ToString());
            resultShader = resultShader.Replace("${ShaderPropertiesHeader}", shaderPropertiesVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${ShaderPropertyUsages}", propertyUsagesVisitor.GetShaderString(1));
            resultShader = resultShader.Replace("${ShaderFunctions}", shaderFunctionVisitor.GetShaderString(1));
            foreach (var entry in templateToShader)
            {
                resultShader = resultShader.Replace(entry.Key, entry.Value);
            }

           configuredTextures = shaderPropertiesVisitor.GetConfiguredTexutres();
           resultShader = Regex.Replace(resultShader, @"\t", "    ");

            return Regex.Replace(resultShader, @"\r\n|\n\r|\n|\r", Environment.NewLine);
        }
    }

    [Serializable]
    [Title("HDRenderLoop/StandardLit")]
    public class StandardtLit : AbstractHDRenderLoopMasterNode
    {
        protected override string GetName()
        {
            return "MasterNodeStandardLit";
        }

        protected override Type GetSurfaceType()
        {
            return typeof(Lit.SurfaceData);
        }

        protected override int GetMatchingMaterialID()
        {
            return (int)Lit.MaterialId.LitStandard;
        }
    }

    [Serializable]
    [Title("HDRenderLoop/SubsurfaceScatteringLit")]
    public class SubsurfaceScatteringLit : AbstractHDRenderLoopMasterNode
    {
        protected override string GetName()
        {
            return "MasterNodeSubsurfaceScatteringLit";
        }

        protected override Type GetSurfaceType()
        {
            return typeof(Lit.SurfaceData);
        }

        protected override int GetMatchingMaterialID()
        {
            return (int)Lit.MaterialId.LitSSS;
        }
    }

    [Serializable]
    [Title("HDRenderLoop/SubsurfaceClearCoatLit")]
    public class SubsurfaceClearCoatLit : AbstractHDRenderLoopMasterNode
    {
        protected override string GetName()
        {
            return "MasterNodeSubsurfaceClearCoatLit";
        }

        protected override Type GetSurfaceType()
        {
            return typeof(Lit.SurfaceData);
        }

        protected override int GetMatchingMaterialID()
        {
            return (int)Lit.MaterialId.LitClearCoat;
        }
    }

    [Serializable]
    [Title("HDRenderLoop/SpecularColorLit")]
    public class SpecularColorLit : AbstractHDRenderLoopMasterNode
    {
        protected override string GetName()
        {
            return "MasterNodeSpecularColorLit";
        }

        protected override Type GetSurfaceType()
        {
            return typeof(Lit.SurfaceData);
        }

        protected override int GetMatchingMaterialID()
        {
            return (int)Lit.MaterialId.LitSpecular;
        }
    }
}

namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    [ExecuteInEditMode]
    // This HDRenderLoop assume linear lighting. Don't work with gamma.
    public class HDRenderLoop : ScriptableRenderLoop
    {
        private const string k_HDRenderLoopPath = "Assets/ScriptableRenderLoop/HDRenderLoop/HDRenderLoop.asset";

        // Must be in sync with DebugViewMaterial.hlsl
        public enum DebugViewVaryingMode
        {
            TexCoord0 = 1,
            TexCoord1 = 2,
            TexCoord2 = 3,
            VertexTangentWS = 4,
            VertexBitangentWS = 5,
            VertexNormalWS = 6,
            VertexColor = 7,
        }

        // Must be in sync with DebugViewMaterial.hlsl
        public enum DebugViewGbufferMode
        {
            Depth = 10,
            BakeDiffuseLighting = 11,
        }

        public class DebugParameters
        {
            // Material Debugging
            public int debugViewMaterial = 0;

            // Rendering debugging
            public bool displayOpaqueObjects = true;
            public bool displayTransparentObjects = true;

            public bool useForwardRenderingOnly = false;

            public bool enableTonemap = true;
            public float exposure = 0;
        }

        private DebugParameters m_DebugParameters = new DebugParameters();
        public DebugParameters debugParameters
        {
            get { return m_DebugParameters; }
        }

#if UNITY_EDITOR
        [MenuItem("Renderloop/CreateHDRenderLoop")]
        static void CreateHDRenderLoop()
        {
            var instance = ScriptableObject.CreateInstance<HDRenderLoop>();
            UnityEditor.AssetDatabase.CreateAsset(instance, k_HDRenderLoopPath);
        }

#endif

        public class GBufferManager
        {
            public const int MaxGbuffer = 8;

            public void SetBufferDescription(int index, string stringId, RenderTextureFormat inFormat, RenderTextureReadWrite inSRGBWrite)
            {
                IDs[index] = Shader.PropertyToID(stringId);
                RTIDs[index] = new RenderTargetIdentifier(IDs[index]);
                formats[index] = inFormat;
                sRGBWrites[index] = inSRGBWrite;
            }

            public void InitGBuffers(int width, int height, CommandBuffer cmd)
            {
                for (int index = 0; index < gbufferCount; index++)
                {
                    /* RTs[index] = */ cmd.GetTemporaryRT(IDs[index], width, height, 0, FilterMode.Point, formats[index], sRGBWrites[index]);
                }
            }

            public RenderTargetIdentifier[] GetGBuffers(CommandBuffer cmd)
            {
                var colorMRTs = new RenderTargetIdentifier[gbufferCount];
                for (int index = 0; index < gbufferCount; index++)
                {
                    colorMRTs[index] = RTIDs[index];
                }

                return colorMRTs;
            }

            /*
            public void BindBuffers(Material mat)
            {
                for (int index = 0; index < gbufferCount; index++)
                {
                    mat.SetTexture(IDs[index], RTs[index]);
                }
            }
            */


            public int gbufferCount { get; set; }
            int[] IDs = new int[MaxGbuffer];
            RenderTargetIdentifier[] RTIDs = new RenderTargetIdentifier[MaxGbuffer];
            RenderTextureFormat[] formats = new RenderTextureFormat[MaxGbuffer];
            RenderTextureReadWrite[] sRGBWrites = new RenderTextureReadWrite[MaxGbuffer];
        }

        public const int MaxLights = 32;
        public const int MaxShadows = 16; // Max shadow allowed on screen simultaneously - a point light is 6 shadows
        public const int MaxProbes = 32;

        [SerializeField]
        ShadowSettings m_ShadowSettings = ShadowSettings.Default;
        ShadowRenderPass m_ShadowPass;

        [SerializeField]
        TextureSettings m_TextureSettings = TextureSettings.Default;

        Material m_DeferredMaterial;
        Material m_FinalPassMaterial;

        // TODO: Find a way to automatically create/iterate through these kind of class
        Lit.RenderLoop m_LitRenderLoop;

        // Debug
        Material m_DebugViewMaterialGBuffer;

        GBufferManager m_gbufferManager = new GBufferManager();

        static private int s_CameraColorBuffer;
        static private int s_CameraDepthBuffer;

        static private ComputeBuffer s_punctualLightList;
        static private ComputeBuffer s_envLightList;
        static private ComputeBuffer s_punctualShadowList;

        private TextureCacheCubemap m_cubeReflTexArray;

        void OnEnable()
        {
            Rebuild();
        }

        void OnValidate()
        {
            Rebuild();
        }

        void ClearComputeBuffers()
        {
            if (s_punctualLightList != null)
                s_punctualLightList.Release();

            if (s_punctualShadowList != null)
                s_punctualShadowList.Release();

            if (s_envLightList != null)
                s_envLightList.Release();
        }

        Material CreateEngineMaterial(string shaderPath)
        {
            var mat = new Material(Shader.Find(shaderPath) as Shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            return mat;
        }

        public override void Rebuild()
        {
            ClearComputeBuffers();

            s_CameraColorBuffer = Shader.PropertyToID("_CameraColorTexture");
            s_CameraDepthBuffer = Shader.PropertyToID("_CameraDepthTexture");

            s_punctualLightList = new ComputeBuffer(MaxLights, System.Runtime.InteropServices.Marshal.SizeOf(typeof(PunctualLightData)));
            s_envLightList = new ComputeBuffer(MaxLights, System.Runtime.InteropServices.Marshal.SizeOf(typeof(EnvLightData)));
            s_punctualShadowList = new ComputeBuffer(MaxShadows, System.Runtime.InteropServices.Marshal.SizeOf(typeof(PunctualShadowData)));

            m_DeferredMaterial = CreateEngineMaterial("Hidden/HDRenderLoop/Deferred");
            m_FinalPassMaterial = CreateEngineMaterial("Hidden/HDRenderLoop/FinalPass");

            // Debug
            m_DebugViewMaterialGBuffer = CreateEngineMaterial("Hidden/HDRenderLoop/DebugViewMaterialGBuffer");

            m_ShadowPass = new ShadowRenderPass (m_ShadowSettings);

            m_cubeReflTexArray = new TextureCacheCubemap();
            m_cubeReflTexArray.AllocTextureArray(32, (int)m_TextureSettings.reflectionCubemapSize, TextureFormat.BC6H, true);

            // Init Lit material buffer - GBuffer and init
            m_LitRenderLoop = new Lit.RenderLoop(); // Our object can be garbacge collected, so need to be allocate here

            m_gbufferManager.gbufferCount = m_LitRenderLoop.GetGBufferCount();
            for (int gbufferIndex = 0; gbufferIndex < m_gbufferManager.gbufferCount; ++gbufferIndex)
            {
                m_gbufferManager.SetBufferDescription(gbufferIndex, "_CameraGBufferTexture" + gbufferIndex, m_LitRenderLoop.RTFormat[gbufferIndex], m_LitRenderLoop.RTReadWrite[gbufferIndex]);
            }

            m_LitRenderLoop.Rebuild();
        }

        void OnDisable()
        {
            m_LitRenderLoop.OnDisable();

            s_punctualLightList.Release();
            s_envLightList.Release();
            s_punctualShadowList.Release();

            if (m_DeferredMaterial) DestroyImmediate(m_DeferredMaterial);
            if (m_FinalPassMaterial) DestroyImmediate(m_FinalPassMaterial);

            m_cubeReflTexArray.Release();
        }

        void InitAndClearBuffer(Camera camera, RenderLoop renderLoop)
        {
            // We clear only the depth buffer, no need to clear the various color buffer as we overwrite them.
            // Clear depth/stencil and init buffers
            {
                var cmd = new CommandBuffer();
                cmd.name = "InitGBuffers and clear Depth/Stencil";

                // Init buffer
                // With scriptable render loop we must allocate ourself depth and color buffer (We must be independent of backbuffer for now, hope to fix that later).
                // Also we manage ourself the HDR format, here allocating fp16 directly.
                // With scriptable render loop we can allocate temporary RT in a command buffer, they will not be release with ExecuteCommandBuffer
                // These temporary surface are release automatically at the end of the scriptable renderloop if not release explicitly
                int w = camera.pixelWidth;
                int h = camera.pixelHeight;

                cmd.GetTemporaryRT(s_CameraColorBuffer, w, h, 0, FilterMode.Point, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Default);
                cmd.GetTemporaryRT(s_CameraDepthBuffer, w, h, 24, FilterMode.Point, RenderTextureFormat.Depth);
                m_gbufferManager.InitGBuffers(w, h, cmd);

                cmd.SetRenderTarget(new RenderTargetIdentifier(s_CameraColorBuffer), new RenderTargetIdentifier(s_CameraDepthBuffer));
                cmd.ClearRenderTarget(true, false, new Color(0, 0, 0, 0));
                renderLoop.ExecuteCommandBuffer(cmd);
                cmd.Dispose();
            }


            // TEMP: As we are in development and have not all the setup pass we still clear the color in emissive buffer and gbuffer, but this will be removed later.

            // Clear HDR target
            {
                var cmd = new CommandBuffer();
                cmd.name = "Clear HDR target";
                cmd.SetRenderTarget(new RenderTargetIdentifier(s_CameraColorBuffer), new RenderTargetIdentifier(s_CameraDepthBuffer));
                cmd.ClearRenderTarget(false, true, new Color(0, 0, 0, 0));
                renderLoop.ExecuteCommandBuffer(cmd);
                cmd.Dispose();
            }


            // Clear GBuffers
            {
                var cmd = new CommandBuffer();
                cmd.name = "Clear GBuffer";
                // Write into the Camera Depth buffer
                cmd.SetRenderTarget(m_gbufferManager.GetGBuffers(cmd), new RenderTargetIdentifier(s_CameraDepthBuffer));
                // Clear everything
                // TODO: Clear is not required for color as we rewrite everything, will save performance.
                cmd.ClearRenderTarget(false, true, new Color(0, 0, 0, 0));
                renderLoop.ExecuteCommandBuffer(cmd);
                cmd.Dispose();
            }

            // END TEMP
        }

        void RenderOpaqueRenderList(CullResults cull, Camera camera, RenderLoop renderLoop, string passName)
        {
            if (!debugParameters.displayOpaqueObjects)
                return;

            DrawRendererSettings settings = new DrawRendererSettings(cull, camera, new ShaderPassName(passName));
            settings.sorting.sortOptions = SortOptions.SortByMaterialThenMesh;
            settings.inputFilter.SetQueuesOpaque();
			settings.rendererConfiguration = RendererConfiguration.PerObjectLightProbe | RendererConfiguration.PerObjectReflectionProbes | RendererConfiguration.PerObjectLightmaps | RendererConfiguration.PerObjectLightProbeProxyVolume;
            renderLoop.DrawRenderers(ref settings);
        }

        void RenderTransparentRenderList(CullResults cull, Camera camera, RenderLoop renderLoop, string passName)
        {
            if (!debugParameters.displayTransparentObjects)
                return;

            var settings = new DrawRendererSettings(cull, camera, new ShaderPassName(passName))
            {
                rendererConfiguration = RendererConfiguration.PerObjectLightProbe | RendererConfiguration.PerObjectReflectionProbes,
                sorting = { sortOptions = SortOptions.SortByMaterialThenMesh }
            };
            settings.inputFilter.SetQueuesTransparent();
            renderLoop.DrawRenderers(ref settings);
        }

        void RenderGBuffer(CullResults cull, Camera camera, RenderLoop renderLoop)
        {
            if (debugParameters.useForwardRenderingOnly)
            {
                return ;
            }

            // setup GBuffer for rendering
            var cmd = new CommandBuffer { name = "GBuffer Pass" };
            cmd.SetRenderTarget(m_gbufferManager.GetGBuffers(cmd), new RenderTargetIdentifier(s_CameraDepthBuffer));
            renderLoop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();

            // render opaque objects into GBuffer
            RenderOpaqueRenderList(cull, camera, renderLoop, "GBuffer");
        }

        void RenderDebugViewMaterial(CullResults cull, Camera camera, RenderLoop renderLoop)
        {
            // Render Opaque forward
            {
                var cmd = new CommandBuffer { name = "DebugView Material Mode Pass" };
                cmd.SetRenderTarget(new RenderTargetIdentifier(s_CameraColorBuffer), new RenderTargetIdentifier(s_CameraDepthBuffer));
                cmd.ClearRenderTarget(true, true, new Color(0, 0, 0, 0));
                renderLoop.ExecuteCommandBuffer(cmd);
                cmd.Dispose();

                Shader.SetGlobalInt("_DebugViewMaterial", (int)debugParameters.debugViewMaterial);

                RenderOpaqueRenderList(cull, camera, renderLoop, "DebugViewMaterial");
            }

            // Render GBUffer opaque
            {
                Vector4 screenSize = ComputeScreenSize(camera);
                m_DebugViewMaterialGBuffer.SetVector("_ScreenSize", screenSize);
                m_DebugViewMaterialGBuffer.SetFloat("_DebugViewMaterial", (float)debugParameters.debugViewMaterial);

                // m_gbufferManager.BindBuffers(m_DeferredMaterial);
                // TODO: Bind depth textures
                var cmd = new CommandBuffer { name = "GBuffer Debug Pass" };
                cmd.Blit(null, new RenderTargetIdentifier(s_CameraColorBuffer), m_DebugViewMaterialGBuffer, 0);
                renderLoop.ExecuteCommandBuffer(cmd);
                cmd.Dispose();
            }

            // Render forward transparent
            {
                RenderTransparentRenderList(cull, camera, renderLoop, "DebugViewMaterial");
            }

            // Last blit
            {
                var cmd = new CommandBuffer { name = "Blit DebugView Material Debug" };
                cmd.Blit(s_CameraColorBuffer, BuiltinRenderTextureType.CameraTarget);
                renderLoop.ExecuteCommandBuffer(cmd);
                cmd.Dispose();
            }
        }

        Matrix4x4 GetViewProjectionMatrix(Camera camera)
        {
            // The actual projection matrix used in shaders is actually massaged a bit to work across all platforms
            // (different Z value ranges etc.)
            var gpuProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
            var gpuVP = gpuProj * camera.worldToCameraMatrix;

            return gpuVP;
        }

        Vector4 ComputeScreenSize(Camera camera)
        {
            return new Vector4(camera.pixelWidth, camera.pixelHeight, 1.0f / camera.pixelWidth, 1.0f / camera.pixelHeight);
        }

        void RenderDeferredLighting(Camera camera, RenderLoop renderLoop)
        {
            if (debugParameters.useForwardRenderingOnly)
            {
                return;
            }

            // Bind material data
            m_LitRenderLoop.Bind();

            var invViewProj = GetViewProjectionMatrix(camera).inverse;
            m_DeferredMaterial.SetMatrix("_InvViewProjMatrix", invViewProj);

            var screenSize = ComputeScreenSize(camera);
            m_DeferredMaterial.SetVector("_ScreenSize", screenSize);

            // m_gbufferManager.BindBuffers(m_DeferredMaterial);
            // TODO: Bind depth textures
            var cmd = new CommandBuffer { name = "Deferred Ligthing Pass" };
            cmd.Blit(null, new RenderTargetIdentifier(s_CameraColorBuffer), m_DeferredMaterial, 0);
            renderLoop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

        void RenderForward(CullResults cullResults, Camera camera, RenderLoop renderLoop)
        {
            // Bind material data
            m_LitRenderLoop.Bind();

            var cmd = new CommandBuffer { name = "Forward Pass" };
            cmd.SetRenderTarget(new RenderTargetIdentifier(s_CameraColorBuffer), new RenderTargetIdentifier(s_CameraDepthBuffer));
            renderLoop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();

            if (debugParameters.useForwardRenderingOnly)
            {
                RenderOpaqueRenderList(cullResults, camera, renderLoop, "Forward");
            }

            RenderTransparentRenderList(cullResults, camera, renderLoop, "Forward");
        }

        void RenderForwardUnlit(CullResults cullResults, Camera camera, RenderLoop renderLoop)
        {
            // Bind material data
            m_LitRenderLoop.Bind();

            var cmd = new CommandBuffer { name = "Forward Unlit Pass" };
            cmd.SetRenderTarget(new RenderTargetIdentifier(s_CameraColorBuffer), new RenderTargetIdentifier(s_CameraDepthBuffer));
            renderLoop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();

            RenderOpaqueRenderList(cullResults, camera, renderLoop, "ForwardUnlit");
            RenderTransparentRenderList(cullResults, camera, renderLoop, "ForwardUnlit");
        }

        void FinalPass(RenderLoop renderLoop)
        {
            // Those could be tweakable for the neutral tonemapper, but in the case of the LookDev we don't need that
            const float blackIn = 0.02f;
            const float whiteIn = 10.0f;
            const float blackOut = 0.0f;
            const float whiteOut = 10.0f;
            const float whiteLevel = 5.3f;
            const float whiteClip = 10.0f;
            const float dialUnits = 20.0f;
            const float halfDialUnits = dialUnits * 0.5f;

            // converting from artist dial units to easy shader-lerps (0-1)
            var tonemapCoeff1 = new Vector4((blackIn * dialUnits) + 1.0f, (blackOut * halfDialUnits) + 1.0f, (whiteIn / dialUnits), (1.0f - (whiteOut / dialUnits)));
            var tonemapCoeff2 = new Vector4(0.0f, 0.0f, whiteLevel, whiteClip / halfDialUnits);

            m_FinalPassMaterial.SetVector("_ToneMapCoeffs1", tonemapCoeff1);
            m_FinalPassMaterial.SetVector("_ToneMapCoeffs2", tonemapCoeff2);

            m_FinalPassMaterial.SetFloat("_EnableToneMap", debugParameters.enableTonemap ? 1.0f : 0.0f);
            m_FinalPassMaterial.SetFloat("_Exposure", debugParameters.exposure);

            var cmd = new CommandBuffer { name = "FinalPass" };

            // Resolve our HDR texture to CameraTarget.
            cmd.Blit(s_CameraColorBuffer, BuiltinRenderTextureType.CameraTarget, m_FinalPassMaterial, 0);
            renderLoop.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

        void NewFrame()
        {
            // update texture caches
            m_cubeReflTexArray.NewFrame();
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------

        void UpdatePunctualLights(VisibleLight[] visibleLights, ref ShadowOutput shadowOutput)
        {
            var lights = new List<PunctualLightData>();
            var shadows = new List<PunctualShadowData>();

            for (int lightIndex = 0; lightIndex < Math.Min(visibleLights.Length, MaxLights); lightIndex++)
            {
                var light = visibleLights[lightIndex];
                if (light.lightType != LightType.Spot && light.lightType != LightType.Point && light.lightType != LightType.Directional)
                    continue;

                var additionalLightData = light.light.GetComponent<AdditionalLightData>();

                var l = new PunctualLightData();

                if (light.lightType == LightType.Directional)
                {
                    l.useDistanceAttenuation = 0.0f;
                    // positionWS store Light direction for directional and is opposite to the forward direction
                    l.positionWS = -light.light.transform.forward;
                    l.invSqrAttenuationRadius = 0.0f;
                }
                else
                {
                    l.useDistanceAttenuation = 1.0f;
                    l.positionWS = light.light.transform.position;
                    l.invSqrAttenuationRadius = 1.0f / (light.range * light.range);
                }

                // Correct intensity calculation (Different from Unity)
                var lightColorR = light.light.intensity * Mathf.GammaToLinearSpace(light.light.color.r);
                var lightColorG = light.light.intensity * Mathf.GammaToLinearSpace(light.light.color.g);
                var lightColorB = light.light.intensity * Mathf.GammaToLinearSpace(light.light.color.b);

                l.color.Set(lightColorR, lightColorG, lightColorB);

                l.forward = light.light.transform.forward; // Note: Light direction is oriented backward (-Z)
                l.up = light.light.transform.up;
                l.right = light.light.transform.right;

                if (light.lightType == LightType.Spot)
                {
                    var spotAngle = light.spotAngle;

                    var innerConePercent = AdditionalLightData.GetInnerSpotPercent01(additionalLightData);
                    var cosSpotOuterHalfAngle = Mathf.Clamp(Mathf.Cos(spotAngle * 0.5f * Mathf.Deg2Rad), 0.0f, 1.0f);
                    var cosSpotInnerHalfAngle = Mathf.Clamp(Mathf.Cos(spotAngle * 0.5f * innerConePercent * Mathf.Deg2Rad), 0.0f, 1.0f); // inner cone

                    var val = Mathf.Max(0.001f, (cosSpotInnerHalfAngle - cosSpotOuterHalfAngle));
                    l.angleScale    = 1.0f / val;
                    l.angleOffset   = -cosSpotOuterHalfAngle * l.angleScale;
                }
                else
                {
                    // 1.0f, 2.0f are neutral value allowing GetAngleAnttenuation in shader code to return 1.0
                    l.angleScale = 1.0f;
                    l.angleOffset = 2.0f;
                }

                l.diffuseScale = AdditionalLightData.GetAffectDiffuse(additionalLightData) ? 1.0f : 0.0f;
                l.specularScale = AdditionalLightData.GetAffectSpecular(additionalLightData) ? 1.0f : 0.0f;
                l.shadowDimmer = AdditionalLightData.GetShadowDimmer(additionalLightData);

                l.IESIndex = -1;
                l.cookieIndex = -1;
                l.shadowIndex = -1;

                // Setup shadow data arrays
                bool hasShadows = shadowOutput.GetShadowSliceCountLightIndex(lightIndex) != 0;
                bool hasNotReachMaxLimit = shadows.Count + (light.lightType == LightType.Point ? 6 : 1) <= MaxShadows;

                if (hasShadows && hasNotReachMaxLimit) // Note  < MaxShadows should be check at shadowOutput creation
                {
                    // When we have a point light, we assumed that there is 6 consecutive PunctualShadowData
                    l.shadowIndex = shadows.Count;

                    for (int sliceIndex = 0; sliceIndex < shadowOutput.GetShadowSliceCountLightIndex(lightIndex); ++sliceIndex)
                    {
                        PunctualShadowData s = new PunctualShadowData();

                        int shadowSliceIndex = shadowOutput.GetShadowSliceIndex(lightIndex, sliceIndex);
                        s.worldToShadow = shadowOutput.shadowSlices[shadowSliceIndex].shadowTransform.transpose; // Transpose for hlsl reading ?

                        if (light.lightType == LightType.Spot)
                        {
                            s.shadowType = ShadowType.Spot;
                        }
                        else if (light.lightType == LightType.Point)
                        {
                            s.shadowType = ShadowType.Point;
                        }
                        else
                        {
                            s.shadowType = ShadowType.Directional;
                        }

                        s.bias = light.light.shadowBias;

                        shadows.Add(s);
                    }
                }

                lights.Add(l);
            }
            s_punctualLightList.SetData(lights.ToArray());
            s_punctualShadowList.SetData(shadows.ToArray());

            Shader.SetGlobalBuffer("_PunctualLightList", s_punctualLightList);
            Shader.SetGlobalInt("_PunctualLightCount", lights.Count);
            Shader.SetGlobalBuffer("_PunctualShadowList", s_punctualShadowList);
        }

        void UpdateReflectionProbes(VisibleReflectionProbe[] activeReflectionProbes)
        {
            var lights = new List<EnvLightData>();

            for (int lightIndex = 0; lightIndex < Math.Min(activeReflectionProbes.Length, MaxProbes); lightIndex++)
            {
                var probe = activeReflectionProbes[lightIndex];

                if (probe.texture == null)
                    continue;

                var l = new EnvLightData();

                // CAUTION: localToWorld is the transform for the widget of the reflection probe. i.e the world position of the point use to do the cubemap capture (mean it include the local offset)
                l.positionWS = probe.localToWorld.GetColumn(3);

                l.envShapeType = EnvShapeType.None;

                // TODO: Support sphere in the interface
                if (probe.boxProjection != 0)
                {
                    l.envShapeType = EnvShapeType.Box;
                }

                // remove scale from the matrix (Scale in this matrix is use to scale the widget)
                l.right = probe.localToWorld.GetColumn(0);
                l.right.Normalize();
                l.up = probe.localToWorld.GetColumn(1);
                l.up.Normalize();
                l.forward = probe.localToWorld.GetColumn(2);
                l.forward.Normalize();

                // Artists prefer to have blend distance inside the volume!
                // So we let the current UI but we assume blendDistance is an inside factor instead
                // Blend distance can't be larger than the max radius
                // probe.bounds.extents is BoxSize / 2
                float maxBlendDist = Mathf.Min(probe.bounds.extents.x, Mathf.Min(probe.bounds.extents.y, probe.bounds.extents.z));
                float blendDistance = Mathf.Min(maxBlendDist, probe.blendDistance);
                l.innerDistance = probe.bounds.extents - new Vector3(blendDistance, blendDistance, blendDistance);

                l.envIndex = m_cubeReflTexArray.FetchSlice(probe.texture);

                l.offsetLS = probe.center; // center is misnamed, it is the offset (in local space) from center of the bounding box to the cubemap capture point
                l.blendDistance = blendDistance;
                lights.Add(l);
            }

            s_envLightList.SetData(lights.ToArray());

            Shader.SetGlobalBuffer("_EnvLightList", s_envLightList);
            Shader.SetGlobalInt("_EnvLightCount", lights.Count);
            Shader.SetGlobalTexture("_EnvTextures", m_cubeReflTexArray.GetTexCache());
        }

        public override void Render(Camera[] cameras, RenderLoop renderLoop)
        {
            if (!m_LitRenderLoop.isInit)
            {
                m_LitRenderLoop.RenderInit(renderLoop);
            }

            // Do anything we need to do upon a new frame.
            NewFrame();

            // Set Frame constant buffer
            // TODO...

            foreach (var camera in cameras)
            {
                // Set camera constant buffer
                // TODO...

                CullingParameters cullingParams;
                if (!CullResults.GetCullingParameters(camera, out cullingParams))
                    continue;

                m_ShadowPass.UpdateCullingParameters (ref cullingParams);

                var cullResults = CullResults.Cull(ref cullingParams, renderLoop);

                renderLoop.SetupCameraProperties(camera);

                InitAndClearBuffer(camera, renderLoop);

                RenderGBuffer(cullResults, camera, renderLoop);

                if (debugParameters.debugViewMaterial != 0)
                {
                    RenderDebugViewMaterial(cullResults, camera, renderLoop);
                }
                else
                {
                    ShadowOutput shadows;
                    m_ShadowPass.Render(renderLoop, cullResults, out shadows);

                    renderLoop.SetupCameraProperties(camera); // Need to recall SetupCameraProperties after m_ShadowPass.Render

                    UpdatePunctualLights(cullResults.visibleLights, ref shadows);
                    UpdateReflectionProbes(cullResults.visibleReflectionProbes);

                    RenderDeferredLighting(camera, renderLoop);

                    RenderForward(cullResults, camera, renderLoop);
                    RenderForwardUnlit(cullResults, camera, renderLoop);

                    FinalPass(renderLoop);
                }

                renderLoop.Submit();
            }

            // Post effects
        }

#if UNITY_EDITOR
        public override UnityEditor.SupportedRenderingFeatures GetSupportedRenderingFeatures()
        {
            var features = new UnityEditor.SupportedRenderingFeatures
            {
                reflectionProbe = UnityEditor.SupportedRenderingFeatures.ReflectionProbe.Rotation
            };

            return features;
        }

#endif
    }
}
