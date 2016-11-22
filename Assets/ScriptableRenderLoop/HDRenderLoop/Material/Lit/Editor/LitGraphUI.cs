using System;
using System.Linq;

namespace UnityEditor
{
    class LitGraphUI : BaseLitGUI
    {
        private MaterialProperty[] genericProperties = new MaterialProperty[] { };
        public override void FindInputProperties(MaterialProperty[] props)
        {
            genericProperties = props.Where(p => (p.flags & MaterialProperty.PropFlags.HideInInspector) == 0 & !reservedProperties.Contains(p.name)).ToArray();
        }
        protected override void ShaderInputOptionsGUI()
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
    }
}