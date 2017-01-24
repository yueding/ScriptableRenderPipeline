using UnityEngine;
using System;
using System.Linq;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class UnlitGraphUI : BaseUnlitGUI
    {
        private MaterialProperty[] genericProperties = new MaterialProperty[] { };
        protected override void FindMaterialProperties(MaterialProperty[] props)
        {
            genericProperties = props.Where(p => (p.flags & MaterialProperty.PropFlags.HideInInspector) == 0 & !reservedProperties.Contains(p.name)).ToArray();
        }
        protected override void SetupMaterialKeywords(Material material)
        {
        }

        protected override void ShaderInputGUI()
        {
            EditorGUI.indentLevel++;
            foreach (var prop in genericProperties)
            {
                if ((prop.type & MaterialProperty.PropType.Texture) != 0)
                {
                    m_MaterialEditor.TexturePropertySingleLine(new GUIContent(prop.name), prop);
                }
                else
                {
                    m_MaterialEditor.ShaderProperty(prop, prop.name);
                }
            }
            EditorGUI.indentLevel--;
        }

        protected override void ShaderInputOptionsGUI()
        {
        }

        protected override bool ShouldEmissionBeEnabled(Material material)
        {
            return true;
        }
    }
}