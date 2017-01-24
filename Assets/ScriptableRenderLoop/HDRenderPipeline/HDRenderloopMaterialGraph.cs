using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.MaterialGraph;
using UnityEngine.Graphing;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public abstract class AbstractMaterialNodeHDRenderPipeline : AbstractMaterialNode
    {
        protected string GetVariableName(IEdge edge, ConcreteSlotValueType type)
        {
            var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(edge.outputSlot.nodeGuid);
            return ShaderGenerator.AdaptNodeOutput(fromNode, edge.outputSlot.slotId, type);
        }

        protected IEdge GetEdge(int idSlot)
        {
            var slot = FindInputSlot<MaterialSlot>(idSlot);
            if (slot != null)
            {
                var edges = owner.GetEdges(slot.slotReference).ToArray();
                if (edges.Length > 0)
                {
                    return edges[0];
                }
            }
            return null;
        }
    }

    [Serializable]
    [Title("HDRenderPipeline/BakeDiffuseLighting")]
    public class BakeDiffuseLightingNode : AbstractMaterialNodeHDRenderPipeline, IGeneratesBodyCode, IMayRequireNormal, IMayRequireWorldPosition, IMayRequireMeshUV
    {
        private const int PositionWSInput = 0;
        private const int NormalWSInput = 1;
        private const int UVStaticInput = 2;
        private const int UVDynamicInput = 3;
        private const int ColorOuput = 4;

        public BakeDiffuseLightingNode()
        {
            name = GetType().Name;
            UpdateNodeAfterDeserialization();
        }


        public bool RequiresWorldPosition()
        {
            return GetEdge(PositionWSInput) == null;
        }

        public bool RequiresMeshUV(UVChannel uv)
        {
            if (uv == UVChannel.uv0)
            {
                return GetEdge(UVStaticInput) == null;
            }

            if (uv == UVChannel.uv1)
            {
                return GetEdge(UVDynamicInput) == null;
            }
            return false;
        }

        public bool RequiresNormal()
        {
            return GetEdge(NormalWSInput) == null;
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(PositionWSInput, "PositionWS", "PositionWSInput", Graphing.SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            AddSlot(new MaterialSlot(NormalWSInput, "Normal", "NormalWSInput", Graphing.SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            AddSlot(new MaterialSlot(UVStaticInput, "UVStatic", "UVStaticInput", Graphing.SlotType.Input, SlotValueType.Vector2, Vector4.zero));
            AddSlot(new MaterialSlot(UVDynamicInput, "UVDynamic", "UVDynamicInput", Graphing.SlotType.Input, SlotValueType.Vector2, Vector4.zero));

            AddSlot(new MaterialSlot(ColorOuput, "Color", "ColorOuput", Graphing.SlotType.Output, SlotValueType.Vector3, Vector4.zero));
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var positionWSInputEdge = GetEdge(PositionWSInput);
            var normalWSInputEdge = GetEdge(NormalWSInput);
            var UVStaticInputEdge = GetEdge(UVStaticInput);
            var UVDynamicInputEdge = GetEdge(UVDynamicInput);

            var positionWSInputValue = ShaderGeneratorNames.WorldSpacePosition;
            var normalWSInputValue = ShaderGeneratorNames.WorldSpaceNormal;
            var UVStaticInputValue = ShaderGeneratorNames.GetUVName(UVChannel.uv1);
            var UVDynamicInputValue = ShaderGeneratorNames.GetUVName(UVChannel.uv2);

            if (positionWSInputEdge != null)
            {
                positionWSInputValue = GetVariableName(positionWSInputEdge, ConcreteSlotValueType.Vector3);
            }

            if (normalWSInputEdge != null)
            {
                normalWSInputValue = GetVariableName(normalWSInputEdge, ConcreteSlotValueType.Vector3);
            }

            if (UVStaticInputEdge != null)
            {
                UVStaticInputValue = GetVariableName(UVStaticInputEdge, ConcreteSlotValueType.Vector2);
            }

            if (UVDynamicInputEdge != null)
            {
                UVDynamicInputValue = GetVariableName(UVDynamicInputEdge, ConcreteSlotValueType.Vector2);
            }

            var body = string.Format("{0}3 {1} = SampleBakedGI({2}, {3}, {4}.xy, {5}.xy);", precision, GetVariableNameForSlot(ColorOuput), positionWSInputValue, normalWSInputValue, UVStaticInputValue, UVDynamicInputValue);
            visitor.AddShaderChunk(body, false);
        }
    }

    [Serializable]
    [Title("HDRenderPipeline/TangentToWorldDirectionNode")]
    public class TangentToWorldDirectionNode : AbstractMaterialNodeHDRenderPipeline, IGeneratesBodyCode, IMayRequireBitangent, IMayRequireTangent, IMayRequireNormal
    {
        private const int TextureNormal = 0;
        private const int NormalInput = 1;
        private const int TangentInput = 2;
        private const int BitangentInput = 3;
        private const int NormalOutput = 4;

        public TangentToWorldDirectionNode()
        {
            name = GetType().Name;
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview
        {
            get { return true; }
        }

        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var textureNormalSlotEdge = GetEdge(TextureNormal);
            var normalInputSlotEdge = GetEdge(NormalInput);
            var tangentInputSlotEdge = GetEdge(TangentInput);
            var bitangentInputSlotEdge = GetEdge(BitangentInput);

            var textureNormalValue = string.Format("{0}4(UnpackNormal(float3(0.5f), 0.0f)", precision);
            var normalInputSlotValue = ShaderGeneratorNames.WorldSpaceNormal;
            var tangentInputSlotValue = ShaderGeneratorNames.WorldSpaceTangent;
            var bitangentInputSlotValue = ShaderGeneratorNames.WorldSpaceBitangent;

            if (textureNormalSlotEdge != null)
            {
                textureNormalValue = GetVariableName(textureNormalSlotEdge, ConcreteSlotValueType.Vector4);
            }

            if (normalInputSlotEdge != null)
            {
                normalInputSlotValue = GetVariableName(normalInputSlotEdge, ConcreteSlotValueType.Vector3);
            }

            if (tangentInputSlotEdge != null)
            {
                tangentInputSlotValue = GetVariableName(tangentInputSlotEdge, ConcreteSlotValueType.Vector3);
            }

            if (bitangentInputSlotEdge != null)
            {
                bitangentInputSlotValue = GetVariableName(bitangentInputSlotEdge, ConcreteSlotValueType.Vector3);
            }

            var tangentToWorldName = string.Format("tangentToWorld_{0}", GetVariableNameForNode());
            var normalTSName = string.Format("normalTS_{0}", GetVariableNameForNode());

            var body = "";
            body += string.Format("float3 {0} = {1}.xyz;\n", normalTSName, textureNormalValue);
            body += string.Format("float3 {0}[3] = {{ {1}, {2}, {3} }};\n", tangentToWorldName, tangentInputSlotValue, bitangentInputSlotValue, normalInputSlotValue);
            if (generationMode == GenerationMode.Preview)
            {
                body += string.Format("{0}3 {1} = mul({2}, float3x3({3}[0], {3}[1], {3}[2]));\n", precision, GetVariableNameForSlot(NormalOutput), normalTSName, tangentToWorldName);
            }
            else
            {
                body += string.Format("{0}3 {1} = TransformTangentToWorld({2}, {3});\n", precision, GetVariableNameForSlot(NormalOutput), normalTSName, tangentToWorldName);
            }
            
            visitor.AddShaderChunk(body, false);
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(TextureNormal, "TextureNormal", "TextureNormalInput", Graphing.SlotType.Input, SlotValueType.Vector4, Vector4.zero));
            AddSlot(new MaterialSlot(NormalInput, "Normal", "NormalInput", Graphing.SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            AddSlot(new MaterialSlot(TangentInput, "Tangent", "TangentInput", Graphing.SlotType.Input, SlotValueType.Vector3, Vector4.zero));
            AddSlot(new MaterialSlot(BitangentInput, "Bitangent", "BitangentInput", Graphing.SlotType.Input, SlotValueType.Vector3, Vector4.zero));

            AddSlot(new MaterialSlot(NormalOutput, "Normal", "NormalOutput", Graphing.SlotType.Output, SlotValueType.Vector3, Vector4.zero));
        }

        public bool RequiresBitangent()
        {
            return GetEdge(BitangentInput) == null;
        }

        public bool RequiresTangent()
        {
            return GetEdge(TangentInput) == null;
        }

        public bool RequiresNormal()
        {
            return GetEdge(NormalInput) == null;
        }
    }

    [Serializable]
    public abstract class AbstractHDRenderPipelineMasterNode : AbstractMasterNode
    {
        public AbstractHDRenderPipelineMasterNode()
        {
            name = GetType().Name;
            UpdateNodeAfterDeserialization();
        }

        private static void DepthFirstCollectNodesFromNodeAndSlotIDList(List<INode> nodeList, INode node, List<int> slotId)
        {
            foreach (var slot in node.GetInputSlots<ISlot>().Select(x => x.id).Where(x => slotId.Contains(x)))
            {
                foreach (var edge in node.owner.GetEdges(node.GetSlotReference(slot)))
                {
                    var outputNode = node.owner.GetNodeFromGuid(edge.outputSlot.nodeGuid);
                    NodeUtils.DepthFirstCollectNodesFromNode(nodeList, outputNode);
                }
            }
        }

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
                        semantic = attribute.semantic,
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
                    if (slot.semantic == SurfaceDataAttributes.Semantic.BakeDiffuseLighting && !IncludeBuiltInLitData())
                    {
                        continue;
                    }
                    AddSlot(new MaterialSlot(slot.index, slot.displayName, slot.shaderOutputName, Graphing.SlotType.Input, slot.valueType, Vector4.zero));
                }
            }
        }

        private bool Requires(SurfaceDataAttributes.Semantic targetSemantic, Regex filter)
        {
            var fields = GetSurfaceType().GetFields().Concat(typeof(Builtin.BuiltinData).GetFields());
            return fields.Where(f => filter.IsMatch(f.Name)).Any(f =>
            {
                var attributes = (SurfaceDataAttributes[])f.GetCustomAttributes(typeof(SurfaceDataAttributes), false);
                var semantic = attributes.Length > 0 ? attributes[0].semantic : SurfaceDataAttributes.Semantic.None;
                if (semantic == targetSemantic)
                {
                    var slot = GetInputSlots<MaterialSlot>().FirstOrDefault(o => o.shaderOutputName == f.Name);
                    if (slot != null)
                    {
                        return owner.GetEdges(slot.slotReference).Count() == 0;
                    }

                }
                return false;
            });
        }

        public bool RequiresNormal(Regex filter)
        {
            return Requires(SurfaceDataAttributes.Semantic.Normal, filter);
        }

        public bool RequiresTangent(Regex filter)
        {
            return Requires(SurfaceDataAttributes.Semantic.Tangent, filter);
        }

        public bool RequiresMeshUV(int index, Regex filter)
        {
            if (index == 1 || index == 2)
            {
                return Requires(SurfaceDataAttributes.Semantic.BakeDiffuseLighting, filter);
            }
            return false;
        }

        public bool RequiresWorldPosition(Regex filter)
        {
            return Requires(SurfaceDataAttributes.Semantic.BakeDiffuseLighting, filter);
        }

        private class Vayring
        {
            public string attributeName;
            public string semantic;
            public SlotValueType semanticType;
            public string vayringName;
            public SlotValueType type;
            public string vertexCode;
            public string pixelCode;
            public string fragInputTarget;

            public string[] tempAdditionnalDefine; //only for first version purpose : use common define system
        };

        private string GenerateLitDataTemplate(GenerationMode mode, string useSurfaceDataInput, string useSurfaceFragInput, PropertyGenerator propertyGenerator, ShaderGenerator propertyUsagesVisitor, ShaderGenerator shaderFunctionVisitor, string litShareTemplate)
        {
            var activeNodeList = new List<INode>();

            var useDataInputRegex = new Regex(useSurfaceDataInput);
            var needFragInputRegex = new Regex(useSurfaceFragInput);
            var slotIDList = GetInputSlots<MaterialSlot>().Where(s => useDataInputRegex.IsMatch(s.shaderOutputName)).Select(s => s.id).ToList();

            DepthFirstCollectNodesFromNodeAndSlotIDList(activeNodeList, this, slotIDList);
            var vayrings = new List<Vayring>();
            for (int iTexCoord = 0; iTexCoord < 4; ++iTexCoord)
            {
                if (needFragInputRegex.IsMatch("texCoord" + iTexCoord) || RequiresMeshUV(iTexCoord, useDataInputRegex) || activeNodeList.OfType<IMayRequireMeshUV>().Any(x => x.RequiresMeshUV((UVChannel)iTexCoord)))
                {
                    vayrings.Add(new Vayring()
                    {
                        attributeName = "texCoord" + iTexCoord,
                        semantic = "TEXCOORD" + iTexCoord,
                        vayringName = "texCoord" + iTexCoord,
                        type = SlotValueType.Vector2,
                        vertexCode = string.Format("output.texCoord{0} = input.texCoord{0};", iTexCoord),
                        pixelCode = string.Format("float4 {0} = float4(fragInput.texCoord{1}, 0, 0);", ShaderGeneratorNames.GetUVName((UVChannel)iTexCoord), iTexCoord),

                        tempAdditionnalDefine = new string[] { "ATTRIBUTES_NEED_TEXCOORD" + iTexCoord, "VARYINGS_NEED_TEXCOORD" + iTexCoord }
                    });
                }
            }

            bool needBitangent = needFragInputRegex.IsMatch("bitangentWS") || activeNodeList.OfType<IMayRequireBitangent>().Any(x => x.RequiresBitangent());
            if (needBitangent || needFragInputRegex.IsMatch("tangentWS") || RequiresTangent(useDataInputRegex) || activeNodeList.OfType<IMayRequireTangent>().Any(x => x.RequiresTangent()))
            {
                vayrings.Add(new Vayring()
                {
                    attributeName = "tangentOS",
                    semantic = "TANGENT",
                    semanticType = SlotValueType.Vector4,
                    vayringName = "tangentWS",
                    type = SlotValueType.Vector3,
                    vertexCode = "output.tangentWS = TransformObjectToWorldDir(input.tangentOS.xyz);",
                    fragInputTarget = "tangentToWorld[0]",
                    pixelCode = string.Format("float3 {0} = normalize(fragInput.tangentToWorld[0]);", ShaderGeneratorNames.WorldSpaceTangent),

                    tempAdditionnalDefine = new string[] { "ATTRIBUTES_NEED_NORMAL", "ATTRIBUTES_NEED_TANGENT", "VARYINGS_NEED_TANGENT_TO_WORLD" }
                });
            }

            if (needBitangent || needFragInputRegex.IsMatch("normalWS") || RequiresNormal(useDataInputRegex) || activeNodeList.OfType<IMayRequireNormal>().Any(x => x.RequiresNormal()))
            {
                vayrings.Add(new Vayring()
                {
                    attributeName = "normalOS",
                    semantic = "NORMAL",
                    vayringName = "normalWS",
                    type = SlotValueType.Vector3,
                    vertexCode = "output.normalWS = TransformObjectToWorldNormal(input.normalOS);",
                    fragInputTarget = "tangentToWorld[2]",
                    pixelCode = string.Format("float3 {0} = normalize(fragInput.tangentToWorld[2]);", ShaderGeneratorNames.WorldSpaceNormal),

                    tempAdditionnalDefine = new string[] { "ATTRIBUTES_NEED_NORMAL", "ATTRIBUTES_NEED_TANGENT", "VARYINGS_NEED_TANGENT_TO_WORLD" }
                });
            }

            if (needBitangent)
            {
                vayrings.Add(new Vayring()
                {
                    vayringName = "bitangentWS",
                    type = SlotValueType.Vector3,
                    vertexCode = "output.bitangentWS = CreateBitangent(output.normalWS, output.tangentWS, input.tangentOS.w);",
                    fragInputTarget = "tangentToWorld[1]",
                    pixelCode = string.Format("float3 {0} = normalize(fragInput.tangentToWorld[1]);", ShaderGeneratorNames.WorldSpaceBitangent),

                    tempAdditionnalDefine = new string[] { "ATTRIBUTES_NEED_NORMAL", "ATTRIBUTES_NEED_TANGENT", "VARYINGS_NEED_TANGENT_TO_WORLD" }
                });
            }

            bool requireViewDirection = needFragInputRegex.IsMatch("viewDirectionWS") || activeNodeList.OfType<IMayRequireViewDirection>().Any(x => x.RequiresViewDirection());
            if (requireViewDirection || needFragInputRegex.IsMatch("positionWS") || RequiresWorldPosition(useDataInputRegex) || activeNodeList.OfType<IMayRequireWorldPosition>().Any(x => x.RequiresWorldPosition()))
            {
                vayrings.Add(new Vayring()
                {
                    vayringName = "positionWS",
                    type = SlotValueType.Vector3,
                    vertexCode = "output.positionWS = TransformObjectToWorld(input.positionOS);",
                    pixelCode = string.Format("float3 {0} = fragInput.positionWS;", ShaderGeneratorNames.WorldSpacePosition),

                    tempAdditionnalDefine = new string[] { "VARYINGS_NEED_POSITION_WS" }
                });
            }

            if (requireViewDirection)
            {
                vayrings.Add(new Vayring()
                {
                    pixelCode = string.Format("float3 {0} = GetWorldSpaceNormalizeViewDir(fragInput.positionWS);", ShaderGeneratorNames.WorldSpaceViewDirection)
                });
            }

            if (needFragInputRegex.IsMatch("vertexColor") || activeNodeList.OfType<IMayRequireVertexColor>().Any(x => x.RequiresVertexColor()))
            {
                vayrings.Add(new Vayring()
                {
                    attributeName = "vertexColor",
                    semantic = "COLOR",
                    vayringName = "vertexColor",
                    type = SlotValueType.Vector4,
                    vertexCode = "output.vertexColor = input.vertexColor;",
                    pixelCode = string.Format("float4 {0} = fragInput.color;", ShaderGeneratorNames.VertexColor),

                    tempAdditionnalDefine = new string[] { "ATTRIBUTES_NEED_COLOR", "VARYINGS_NEED_COLOR" }
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
                vertexShaderBodyVisitor.AddShaderChunk(vayring.vertexCode, false);
                pixelShaderInitVisitor.AddShaderChunk(vayring.pixelCode, false);

                if (vayring.type != SlotValueType.Dynamic)
                {
                    var typeSize = _fnTypeToSize(vayring.type);
                    if (!string.IsNullOrEmpty(vayring.attributeName))
                    {
                        var semanticType = vayring.semanticType != SlotValueType.Dynamic ? vayring.semanticType : vayring.type;
                        var semanticSize = _fnTypeToSize(semanticType);
                        vertexAttributeVisitor.AddShaderChunk(string.Format("float{0} {1} : {2};", semanticSize, vayring.attributeName, vayring.semantic), true);
                    }

                    vayringVisitor.AddShaderChunk(string.Format("float{0} {1};", typeSize, vayring.vayringName), false);

                    for (int channel = 0; channel < typeSize; ++channel)
                    {
                        var packed = string.Format("interpolators[{0}][{1}]", currentIndex, currentChannel);
                        var source = string.Format("{0}[{1}]", vayring.vayringName, channel);
                        var target = string.Format("{0}[{1}]", string.IsNullOrEmpty(vayring.fragInputTarget) ? vayring.vayringName : vayring.fragInputTarget, channel);
                        packInterpolatorVisitor.AddShaderChunk(string.Format("output.{0} = input.{1};", packed, source), false);
                        unpackInterpolatorVisitor.AddShaderChunk(string.Format("output.{0} = input.{1};", target, packed), false);

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

            var pixelShaderBodyVisitor = new ShaderGenerator[] { new ShaderGenerator(), new ShaderGenerator() };
            foreach (var node in activeNodeList)
            {
                if (node is IGeneratesBodyCode)
                    (node as IGeneratesBodyCode).GenerateNodeCode(pixelShaderBodyVisitor[0], mode);
            }

            foreach (var slot in GetInputSlots<MaterialSlot>())
            {
                if (!slotIDList.Contains(slot.id))
                    continue;

                var slotOutputName = slot.shaderOutputName;
                var surfaceField = GetSurfaceType().GetFields().FirstOrDefault(o => o.Name == slotOutputName);
                var builtinField = typeof(Builtin.BuiltinData).GetFields().FirstOrDefault(o => o.Name == slotOutputName);
                var currentField = surfaceField != null ? surfaceField : builtinField;

                string variableName = null;
                int visitorIndex = 0;
                var egdes = owner.GetEdges(slot.slotReference).ToArray();
                if (egdes.Length == 1)
                {
                    var outputRef = egdes[0].outputSlot;
                    var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(outputRef.nodeGuid);
                    if (fromNode != null)
                    {
                        variableName = fromNode.GetVariableNameForSlot(outputRef.slotId);
                    }
                }
                else if (egdes.Length == 0)
                {
                    var attributes = (SurfaceDataAttributes[])currentField.GetCustomAttributes(typeof(SurfaceDataAttributes), false);
                    var semantic = attributes.Length > 0 ? attributes[0].semantic : SurfaceDataAttributes.Semantic.None;
                    switch(semantic)
                    {
                        case SurfaceDataAttributes.Semantic.AmbientOcclusion:
                        case SurfaceDataAttributes.Semantic.Opacity:
                            variableName = "1.0f";
                            break;
                        case SurfaceDataAttributes.Semantic.Normal:
                            variableName = ShaderGeneratorNames.WorldSpaceNormal;
                            break;
                        case SurfaceDataAttributes.Semantic.Tangent:
                            variableName = ShaderGeneratorNames.WorldSpaceTangent;
                            break;
                        case SurfaceDataAttributes.Semantic.BakeDiffuseLighting:
                            variableName = string.Format("SampleBakedGI({0}, surfaceData.normalWS, {1}, {2})", ShaderGeneratorNames.WorldSpacePosition, ShaderGeneratorNames.GetUVName(UVChannel.uv1), ShaderGeneratorNames.GetUVName(UVChannel.uv2));
                            visitorIndex = 1; //it depends of surfaceData.normalWS, do it last
                            break;
                        default: break;
                    }
                }
                else
                {
                    Debug.LogError("Unexpected graph : multiples edges connected to the same slot");
                }

                if (!string.IsNullOrEmpty(variableName))
                {
                    pixelShaderBodyVisitor[visitorIndex].AddShaderChunk(string.Format("{0}.{1} = {2};", surfaceField != null ? "surfaceData" : "builtinData", slotOutputName, variableName), false);
                }
            }

            var type =  GetMatchingMaterialID();
            var typeString = type.ToString();

            var fieldsSurface = GetSurfaceType().GetFields();
            var materialIdField = fieldsSurface.FirstOrDefault(o => o.FieldType.IsEnum);
            if (materialIdField != null)
            {
                var enumValue = Enum.ToObject(materialIdField.FieldType, GetMatchingMaterialID()).ToString();
                var define = string.Format("{0}_{1}", materialIdField.Name, UnityEditor.Experimental.Rendering.ShaderGeneratorHelper.InsertUnderscore(enumValue));
                define = define.ToUpper();
                pixelShaderBodyVisitor[1].AddShaderChunk(string.Format("surfaceData.{0} = {1};", materialIdField.Name, define), false);
            }

            var resultShader = litShareTemplate.Replace("${VaryingAttributes}", vayringVisitor.GetShaderString(1));
            resultShader = resultShader.Replace("${PixelShaderInitialize}", pixelShaderInitVisitor.GetShaderString(1));
            resultShader = resultShader.Replace("${PixelShaderBody}", pixelShaderBodyVisitor.Select(o => o.GetShaderString(1)).Aggregate((a, b) => a + b));
            resultShader = resultShader.Replace("${VertexAttributes}", vertexAttributeVisitor.GetShaderString(1));
            resultShader = resultShader.Replace("${PackedVaryingAttributes}", packedVarying.GetShaderString(1));
            resultShader = resultShader.Replace("${PackingVaryingCode}", packInterpolatorVisitor.GetShaderString(1));
            resultShader = resultShader.Replace("${UnpackVaryingCode}", unpackInterpolatorVisitor.GetShaderString(1));
            resultShader = resultShader.Replace("${VertexShaderBody}", vertexShaderBodyVisitor.GetShaderString(1));

            /* temporary behavior : use common define system */
            var tempAdditionnalDefine = new ShaderGenerator();
            foreach (var define in vayrings.Where(o => o.tempAdditionnalDefine != null).SelectMany(o => o.tempAdditionnalDefine).Distinct())
            {
                tempAdditionnalDefine.AddShaderChunk(string.Format("#define {0}", define), false);
            }
            resultShader = resultShader.Replace("${TemporaryAdditionnalDefines}", tempAdditionnalDefine.GetShaderString(1));
            
            return resultShader;
        }

        public override string GetSubShader(GenerationMode mode, PropertyGenerator shaderPropertiesVisitor)
        {
            //TODOPAUL
            throw new NotImplementedException();
        }

        public override string GetFullShader(GenerationMode mode, out List<PropertyGenerator.TextureInfo> configuredTextures)
        {
            configuredTextures = new List<PropertyGenerator.TextureInfo>();

            var templateText = GetTemplateText();
            var templatePassText = GetTemplatePassText();

            var shaderPropertiesVisitor = new PropertyGenerator();
            var propertyUsagesVisitor = new ShaderGenerator();
            var shaderFunctionVisitor = new ShaderGenerator();
            var templateToShader = new Dictionary<string, string>();

            var findLitShareTemplate = new System.Text.RegularExpressions.Regex("#{TemplatePass.*}");
            var findUseDataInput = new System.Text.RegularExpressions.Regex("useSurfaceData:{(.*?)}");
            var findNeedFragInput = new System.Text.RegularExpressions.Regex("useFragInput:{(.*?)}");
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

                        var generatedShader = GenerateLitDataTemplate(mode, useUseDataInputRegex, needFragInputRegex, shaderPropertiesVisitor, propertyUsagesVisitor, shaderFunctionVisitor, templatePassText);
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
            resultShader = Regex.Replace(resultShader, @"\r\n|\n\r|\n|\r", Environment.NewLine);
            //Test
            System.IO.File.WriteAllText("C:/Unity/Git_ScriptableRenderLoop/ScriptableRenderLoop/Assets/UnityShaderEditor/Editor/Testing/IntegrationTests/Graphs/Test.shader", resultShader);
            return resultShader;
        }

        protected abstract Type GetSurfaceType();
        protected abstract int GetMatchingMaterialID();
        protected abstract string GetTemplateText();
        protected abstract string GetTemplatePassText();
        protected abstract bool IncludeBuiltInLitData();
    }

    [Serializable]
    public abstract class LitNode : AbstractHDRenderPipelineMasterNode
    {
        protected override sealed Type GetSurfaceType()
        {
            return typeof(Lit.SurfaceData);
        }

        protected sealed override bool IncludeBuiltInLitData()
        {
            return true;
        }

        protected sealed override string GetTemplateText()
        {
            var templatePath = "Assets/ScriptableRenderLoop/HDRenderPipeline/Material/Lit/Lit.template";
            if (!System.IO.File.Exists(templatePath))
                return "";
            return System.IO.File.ReadAllText(templatePath);
        }

        protected sealed override string GetTemplatePassText()
        {
            var templatePathPass = "Assets/ScriptableRenderLoop/HDRenderPipeline/Material/Lit/LitSharePass.template";
            if (!System.IO.File.Exists(templatePathPass))
                return "";
            return System.IO.File.ReadAllText(templatePathPass);
        }
    }

    [Serializable]
    [Title("HDRenderPipeline/Lit/Standard")]
    public class StandardtLitNode : LitNode
    {
        protected override int GetMatchingMaterialID()
        {
            return (int)Lit.MaterialId.LitStandard;
        }
    }

    [Serializable]
    [Title("HDRenderPipeline/Lit/SubsurfaceScattering")]
    public class SubsurfaceScatteringLitNode : LitNode
    {
        protected override int GetMatchingMaterialID()
        {
            return (int)Lit.MaterialId.LitSSS;
        }
    }

    [Serializable]
    [Title("HDRenderPipeline/Lit/SubsurfaceClearCoat")]
    public class SubsurfaceClearCoatLitNode : LitNode
    {
        protected override int GetMatchingMaterialID()
        {
            return (int)Lit.MaterialId.LitClearCoat;
        }
    }

    [Serializable]
    [Title("HDRenderPipeline/Lit/SpecularColor")]
    public class SpecularColorLitNode : LitNode
    {
        protected override int GetMatchingMaterialID()
        {
            return (int)Lit.MaterialId.LitSpecular;
        }
    }

    [Serializable]
    [Title("HDRenderPipeline/Unlit")]
    public class UnlitNode : AbstractHDRenderPipelineMasterNode
    {
        protected override Type GetSurfaceType()
        {
            return typeof(Unlit.SurfaceData);
        }

        protected override int GetMatchingMaterialID()
        {
            return -1;
        }

        protected sealed override bool IncludeBuiltInLitData()
        {
            return false;
        }

        protected sealed override string GetTemplateText()
        {
            var templatePath = "Assets/ScriptableRenderLoop/HDRenderPipeline/Material/Unlit/Unlit.template";
            if (!System.IO.File.Exists(templatePath))
                return "";
            return System.IO.File.ReadAllText(templatePath);
        }

        protected sealed override string GetTemplatePassText()
        {
            var templatePathPass = "Assets/ScriptableRenderLoop/HDRenderPipeline/Material/Lit/LitSharePass.template";
            if (!System.IO.File.Exists(templatePathPass))
                return "";
            return System.IO.File.ReadAllText(templatePathPass);
        }
    }
}