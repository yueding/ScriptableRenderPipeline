using UnityEditor.AnimatedValues;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using CED = CoreEditorDrawer<LightLoopSettingsUI, SerializedLightLoopSettings>;

    class LightLoopSettingsUI : BaseUI<SerializedLightLoopSettings>
    {
        const string lightLoopSettingsHeaderContent = "Light Loop Settings";
        static readonly GUIContent tileAndClusterContent = CoreEditorUtils.GetContent("Enable Tile And Cluster");
        static readonly GUIContent fptlForForwardOpaqueContent = CoreEditorUtils.GetContent("Enable FPTL For Forward Opaque");
        static readonly GUIContent bigTilePrepassContent = CoreEditorUtils.GetContent("Enable Big Tile Prepass");
        static readonly GUIContent computeLightEvaluationContent = CoreEditorUtils.GetContent("Enable Compute Light Evaluation");
        static readonly GUIContent computeLightVariantsContent = CoreEditorUtils.GetContent("Enable Compute Light Variants");
        static readonly GUIContent computeMaterialVariantsContent = CoreEditorUtils.GetContent("Enable Compute Material Variants");

        public static CED.IDrawer SectionLightLoopSettings(bool withOverride)
        {
            return CED.FoldoutGroup(
                lightLoopSettingsHeaderContent,
                (s, p, o) => s.isSectionExpandedLightLoopSettings,
                FoldoutOption.Indent,
                CED.LabelWidth(250, CED.Action((s, p, o) => Drawer_SectionLightLoopSettings(s, p, o, withOverride))));
        }

        public AnimBool isSectionExpandedLightLoopSettings { get { return m_AnimBools[0]; } }
        public AnimBool isSectionExpandedEnableTileAndCluster { get { return m_AnimBools[1]; } }
        public AnimBool isSectionExpandedComputeLightEvaluation { get { return m_AnimBools[2]; } }

        public LightLoopSettingsUI()
            : base(3)
        {
        }

        public override void Update()
        {
            isSectionExpandedEnableTileAndCluster.target = data.enableTileAndCluster.boolValue;
            isSectionExpandedComputeLightEvaluation.target = data.enableComputeLightEvaluation.boolValue;
            base.Update();
        }

        static void Drawer_SectionLightLoopSettings(LightLoopSettingsUI s, SerializedLightLoopSettings p, Editor owner, bool withOverride)
        {
            // Uncomment if you re-enable LIGHTLOOP_SINGLE_PASS multi_compile in lit*.shader
            //FrameSettingsUI.DrawProperty(p.enableTileAndCluster, tileAndClusterContent, withOverride, a => p.overridesTileAndCluster = a, () => p.overridesTileAndCluster);
            //EditorGUI.indentLevel++;

            GUILayout.BeginVertical();
            if (s.isSectionExpandedEnableTileAndCluster.target)
            {
                FrameSettingsUI.DrawProperty(p.enableFptlForForwardOpaque, fptlForForwardOpaqueContent, withOverride, a => p.overridesFptlForForwardOpaque = a, () => p.overridesFptlForForwardOpaque);
                FrameSettingsUI.DrawProperty(p.enableBigTilePrepass, bigTilePrepassContent, withOverride, a => p.overridesBigTilePrepass = a, () => p.overridesBigTilePrepass);
                FrameSettingsUI.DrawProperty(p.enableComputeLightEvaluation, computeLightEvaluationContent, withOverride, a => p.overridesComputeLightEvaluation = a, () => p.overridesComputeLightEvaluation);
                GUILayout.BeginVertical();
                if (s.isSectionExpandedComputeLightEvaluation.target)
                {
                    EditorGUI.indentLevel++;
                    FrameSettingsUI.DrawProperty(p.enableComputeLightVariants, computeLightVariantsContent, withOverride, a => p.overridesComputeLightVariants = a, () => p.overridesComputeLightVariants);
                    FrameSettingsUI.DrawProperty(p.enableComputeMaterialVariants, computeMaterialVariantsContent, withOverride, a => p.overridesComputeMaterialVariants  = a, () => p.overridesComputeMaterialVariants);
                    EditorGUI.indentLevel--;
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();

            //EditorGUI.indentLevel--;
        }
    }
}
