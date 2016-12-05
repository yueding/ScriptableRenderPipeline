using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.MaterialGraph;
using UnityEngine.Graphing;

namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    [Serializable]
    public abstract class AbstractHDRenderLoopMasterNode : AbstractMasterNode, IMayRequireNormal, IMayRequireTangent
    {
        public AbstractHDRenderLoopMasterNode()
        {
            name = GetType().Name;
            UpdateNodeAfterDeserialization();
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
                    AddSlot(new MaterialSlot(slot.index, slot.displayName, slot.shaderOutputName, Graphing.SlotType.Input, slot.valueType, Vector4.zero));
                }
            }
        }

        private bool Requires(SurfaceDataAttributes.Semantic targetSemantic)
        {
            var fields = GetSurfaceType().GetFields().Concat(typeof(Builtin.BuiltinData).GetFields());
            return fields.Any(f =>
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

        public bool RequiresNormal()
        {
            return Requires(SurfaceDataAttributes.Semantic.Normal);
        }

        public bool RequiresTangent()
        {
            return Requires(SurfaceDataAttributes.Semantic.Tangent);
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
        };

        private string GenerateLitDataTemplate(GenerationMode mode, string useSurfaceDataInput, string useSurfaceFragInput, PropertyGenerator propertyGenerator, ShaderGenerator propertyUsagesVisitor, ShaderGenerator shaderFunctionVisitor, string litShareTemplate)
        {
            var activeNodeList = new List<INode>();

            var useDataInputRegex = new Regex(useSurfaceDataInput);
            var needFragInputRegex = new Regex(useSurfaceFragInput);
            var slotIDList = GetInputSlots<MaterialSlot>().Where(s => useDataInputRegex.IsMatch(s.shaderOutputName)).Select(s => s.id).ToList();

            NodeUtils.DepthFirstCollectNodesFromNodeSlotList(activeNodeList, this, slotIDList);
            var vayrings = new List<Vayring>();
            for (int iTexCoord = 0; iTexCoord < 4; ++iTexCoord)
            {
                if (needFragInputRegex.IsMatch("texCoord" + iTexCoord) || (iTexCoord == 0 && activeNodeList.OfType<IMayRequireMeshUV>().Any(x => x.RequiresMeshUV())))
                {
                    vayrings.Add(new Vayring()
                    {
                        attributeName = "texCoord" + iTexCoord,
                        semantic = "TEXCOORD" + iTexCoord,
                        vayringName = "texCoord" + iTexCoord,
                        type = SlotValueType.Vector2,
                        vertexCode = string.Format("output.texCoord{0} = input.texCoord{0};", iTexCoord),
                        pixelCode = string.Format("float4 {0} = float4(fragInput.texCoord{1}, 0, 0);", ShaderGeneratorNames.UV0.Replace("0", iTexCoord.ToString()), iTexCoord)
                    });
                }
            }

            bool needBitangent = needFragInputRegex.IsMatch("bitangentWS");
            if (needBitangent || needFragInputRegex.IsMatch("tangentWS") || activeNodeList.OfType<IMayRequireTangent>().Any(x => x.RequiresTangent()))
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
                    pixelCode = string.Format("float3 {0} = normalize(fragInput.tangentToWorld[0]);", ShaderGeneratorNames.WorldSpaceTangent)
                });
            }

            if (needBitangent || needFragInputRegex.IsMatch("normalWS") || activeNodeList.OfType<IMayRequireNormal>().Any(x => x.RequiresNormal()))
            {
                vayrings.Add(new Vayring()
                {
                    attributeName = "normalOS",
                    semantic = "NORMAL",
                    vayringName = "normalWS",
                    type = SlotValueType.Vector3,
                    vertexCode = "output.normalWS = TransformObjectToWorldNormal(input.normalOS);",
                    fragInputTarget = "tangentToWorld[2]",
                    pixelCode = string.Format("float3 {0} = normalize(fragInput.tangentToWorld[2]);", ShaderGeneratorNames.WorldSpaceNormal)
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
                    pixelCode = string.Format("float3 {0} = normalize(fragInput.tangentToWorld[1]);", "worldSpaceBitangent")
                });
            }

            bool requireViewDirection = needFragInputRegex.IsMatch("viewDirectionWS") || activeNodeList.OfType<IMayRequireViewDirection>().Any(x => x.RequiresViewDirection());
            if (requireViewDirection || needFragInputRegex.IsMatch("positionWS") || activeNodeList.OfType<IMayRequireWorldPosition>().Any(x => x.RequiresWorldPosition()))
            {
                vayrings.Add(new Vayring()
                {
                    vayringName = "positionWS",
                    type = SlotValueType.Vector3,
                    vertexCode = "output.positionWS = TransformObjectToWorld(input.positionOS);",
                    pixelCode = string.Format("float3 {0} = fragInput.positionWS;", ShaderGeneratorNames.WorldSpacePosition)
                });
            }

            if (requireViewDirection)
            {
                vayrings.Add(new Vayring()
                {
                    pixelCode = string.Format("float3 {0} = GetWorldSpaceNormalizeViewDir(fragInput.positionWS);", ShaderGeneratorNames.WorldSpaceViewDirection)
                });
            }

            if (needFragInputRegex.IsMatch("vertexColor"))
            {
                vayrings.Add(new Vayring()
                {
                    attributeName = "vertexColor",
                    semantic = "COLOR",
                    vayringName = "vertexColor",
                    type = SlotValueType.Vector4,
                    vertexCode = "output.vertexColor = input.vertexColor;",
                    pixelCode = string.Format("float4 {0} = fragInput.vertexColor;", "vertexColor")
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

                var slotOutputName = slot.shaderOutputName;
                var surfaceField = GetSurfaceType().GetFields().FirstOrDefault(o => o.Name == slotOutputName);
                var builtinField = typeof(Builtin.BuiltinData).GetFields().FirstOrDefault(o => o.Name == slotOutputName);
                var currentField = surfaceField != null ? surfaceField : builtinField;

                string variableName = null;
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
                        default: break;
                    }
                }
                else
                {
                    Debug.LogError("Unexpected graph : multiples edges connected to the same slot");
                }

                if (!string.IsNullOrEmpty(variableName))
                {
                    pixelShaderBodyVisitor.AddShaderChunk(string.Format("{0}.{1} = {2};", surfaceField != null ? "surfaceData" : "builtinData", slotOutputName, variableName), false);
                }
            }

            var type =  GetMatchingMaterialID();
            var typeString = type.ToString();

            var fieldsSurface = GetSurfaceType().GetFields();
            var materialIdField = fieldsSurface.FirstOrDefault(o => o.FieldType.IsEnum);
            if (materialIdField != null)
            {
                var enumValue = Enum.ToObject(materialIdField.FieldType, GetMatchingMaterialID()).ToString();
                var define = string.Format("{0}_{1}", materialIdField.Name, ShaderGeneratorHelper.InsertUnderscore(enumValue));
                define = define.ToUpper();
                pixelShaderBodyVisitor.AddShaderChunk(string.Format("surfaceData.{0} = {1};", materialIdField.Name, define), false);
            }

            var resultShader = litShareTemplate.Replace("${VaryingAttributes}", vayringVisitor.GetShaderString(1));
            resultShader = resultShader.Replace("${PixelShaderInitialize}", pixelShaderInitVisitor.GetShaderString(1));
            resultShader = resultShader.Replace("${PixelShaderBody}", pixelShaderBodyVisitor.GetShaderString(1));
            resultShader = resultShader.Replace("${VertexAttributes}", vertexAttributeVisitor.GetShaderString(1));
            resultShader = resultShader.Replace("${PackedVaryingAttributes}", packedVarying.GetShaderString(1));
            resultShader = resultShader.Replace("${PackingVaryingCode}", packInterpolatorVisitor.GetShaderString(1));
            resultShader = resultShader.Replace("${UnpackVaryingCode}", unpackInterpolatorVisitor.GetShaderString(1));
            resultShader = resultShader.Replace("${VertexShaderBody}", vertexShaderBodyVisitor.GetShaderString(1));
            return resultShader;
        }

        public override string GetShader(GenerationMode mode, out List<PropertyGenerator.TextureInfo> configuredTextures)
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
            return Regex.Replace(resultShader, @"\r\n|\n\r|\n|\r", Environment.NewLine);
        }

        protected abstract Type GetSurfaceType();
        protected abstract int GetMatchingMaterialID();
        protected abstract string GetTemplateText();
        protected abstract string GetTemplatePassText();
    }

    [Serializable]
    public abstract class LitNode : AbstractHDRenderLoopMasterNode
    {
        protected override sealed Type GetSurfaceType()
        {
            return typeof(Lit.SurfaceData);
        }

        protected sealed override string GetTemplateText()
        {
            var templatePath = "Assets/ScriptableRenderLoop/HDRenderLoop/Material/Lit/Lit.template";
            if (!System.IO.File.Exists(templatePath))
                return "";
            return System.IO.File.ReadAllText(templatePath);
        }

        protected sealed override string GetTemplatePassText()
        {
            var templatePathPass = "Assets/ScriptableRenderLoop/HDRenderLoop/Material/Lit/LitSharePass.template";
            if (!System.IO.File.Exists(templatePathPass))
                return "";
            return System.IO.File.ReadAllText(templatePathPass);
        }
    }

    [Serializable]
    [Title("HDRenderLoop/Lit/Standard")]
    public class StandardtLitNode : LitNode
    {
        protected override int GetMatchingMaterialID()
        {
            return (int)Lit.MaterialId.LitStandard;
        }
    }

    [Serializable]
    [Title("HDRenderLoop/Lit/SubsurfaceScattering")]
    public class SubsurfaceScatteringLitNode : LitNode
    {
        protected override int GetMatchingMaterialID()
        {
            return (int)Lit.MaterialId.LitSSS;
        }
    }

    [Serializable]
    [Title("HDRenderLoop/Lit/SubsurfaceClearCoat")]
    public class SubsurfaceClearCoatLitNode : LitNode
    {
        protected override int GetMatchingMaterialID()
        {
            return (int)Lit.MaterialId.LitClearCoat;
        }
    }

    [Serializable]
    [Title("HDRenderLoop/Lit/SpecularColor")]
    public class SpecularColorLitNode : LitNode
    {
        protected override int GetMatchingMaterialID()
        {
            return (int)Lit.MaterialId.LitSpecular;
        }
    }

    [Serializable]
    [Title("HDRenderLoop/Unlit")]
    public class UnlitNode : AbstractHDRenderLoopMasterNode
    {
        protected override Type GetSurfaceType()
        {
            return typeof(Unlit.SurfaceData);
        }

        protected override int GetMatchingMaterialID()
        {
            return -1;
        }

        protected sealed override string GetTemplateText()
        {
            var templatePath = "Assets/ScriptableRenderLoop/HDRenderLoop/Material/Unlit/Unlit.template";
            if (!System.IO.File.Exists(templatePath))
                return "";
            return System.IO.File.ReadAllText(templatePath);
        }

        protected sealed override string GetTemplatePassText()
        {
            var templatePathPass = "Assets/ScriptableRenderLoop/HDRenderLoop/Material/Lit/LitSharePass.template";
            if (!System.IO.File.Exists(templatePathPass))
                return "";
            return System.IO.File.ReadAllText(templatePathPass);
        }
    }
}