using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<ShadowInitParametersUI, SerializedShadowInitParameters>;

    class ShadowInitParametersUI : BaseUI<SerializedShadowInitParameters>
    {
        public static readonly CED.IDrawer SectionAtlas = CED.Action(Drawer_FieldShadowSize);

        public ShadowInitParametersUI()
            : base(0)
        {
        }

        static void Drawer_FieldShadowSize(ShadowInitParametersUI s, SerializedShadowInitParameters d, Editor o)
        {
            EditorGUILayout.LabelField(_.GetContent("Shadow"), EditorStyles.boldLabel);
            
            ++EditorGUI.indentLevel;
            EditorGUILayout.LabelField(_.GetContent("Shadow Atlas"), EditorStyles.boldLabel);
            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(d.shadowAtlasWidth, _.GetContent("Atlas Width"));
            EditorGUILayout.PropertyField(d.shadowAtlasHeight, _.GetContent("Atlas Height"));
            EditorGUILayout.PropertyField(d.shadowMap16Bit, _.GetContent("16-bit Shadow Maps"));
            --EditorGUI.indentLevel;

            EditorGUILayout.Space();

            EditorGUILayout.LabelField(_.GetContent("Shadow Map Budget"), EditorStyles.boldLabel);
            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(d.maxPointLightShadows, _.GetContent("Max Point Light Shadows"));
            EditorGUILayout.PropertyField(d.maxSpotLightShadows, _.GetContent("Max Spot Light Shadows"));
            EditorGUILayout.PropertyField(d.maxDirectionalLightShadows, _.GetContent("Max Directional Light Shadows"));

            EditorGUILayout.PropertyField(d.maxShadowRequests, _.GetContent("Max Shadow Requests|Max shadow requests (SR) per frame, 1 point light = 6 SR, 1 spot light = 1 SR and the directional is 4 SR"));
            --EditorGUI.indentLevel;
            
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(_.GetContent("Shadow Algorithms"), EditorStyles.boldLabel);
            ++EditorGUI.indentLevel;
            EditorGUILayout.PropertyField(d.punctualShadowAlgorithm, _.GetContent("Punctual Shadow Algorithm"));
            EditorGUILayout.PropertyField(d.directionalShadowAlgorithm, _.GetContent("Directional Shadow Algorithm"));
            --EditorGUI.indentLevel;

            // Clamp negative values
            d.shadowAtlasHeight.intValue = Mathf.Max(0, d.shadowAtlasHeight.intValue);
            d.shadowAtlasWidth.intValue = Mathf.Max(0, d.shadowAtlasWidth.intValue);
            d.maxPointLightShadows.intValue = Mathf.Max(0, d.maxPointLightShadows.intValue);
            d.maxSpotLightShadows.intValue = Mathf.Max(0, d.maxSpotLightShadows.intValue);
            d.maxDirectionalLightShadows.intValue = Mathf.Max(0, d.maxDirectionalLightShadows.intValue);

            --EditorGUI.indentLevel;
        }
    }
}
