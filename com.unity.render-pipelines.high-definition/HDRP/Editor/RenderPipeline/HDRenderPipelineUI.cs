using UnityEngine.Events;
using UnityEditor.AnimatedValues;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<HDRenderPipelineUI, SerializedHDRenderPipelineAsset>;

    class HDRenderPipelineUI : BaseUI<SerializedHDRenderPipelineAsset>
    {
        static HDRenderPipelineUI()
        {
            Inspector = CED.Group(
                    SectionPrimarySettings,
                    CED.space,
                    CED.Select(
                        (s, d, o) => s.renderPipelineSettings,
                        (s, d, o) => d.renderPipelineSettings,
                        RenderPipelineSettingsUI.Inspector
                        ),
                    CED.space,
                    CED.Action(Drawer_TitleDefaultFrameSettings),
                    CED.FoldoutGroup(
                        "Camera",
                        (s, p, o) => s.isSectionExpandedCamera,
                        FoldoutOption.Indent,
                        CED.Select(
                            (s, d, o) => s.defaultFrameSettings,
                            (s, d, o) => d.defaultFrameSettings,
                            FrameSettingsUI.Inspector(withOverride: false)
                            )
                        ),
                    CED.space,
                    CED.FoldoutGroup(
                        "Cube Reflection",
                        (s, p, o) => s.isSectionExpandedCubeReflection,
                        FoldoutOption.Indent,
                        CED.Select(
                            (s, d, o) => s.defaultCubeReflectionCaptureSettings,
                            (s, d, o) => d.defaultCubeReflectionCaptureSettings,
                            CaptureSettingsUI.SectionCaptureSettings(withOverride: false)
                            ),
                        CED.Select(
                            (s, d, o) => s.defaultCubeReflectionFrameSettings,
                            (s, d, o) => d.defaultCubeReflectionFrameSettings,
                            FrameSettingsUI.Inspector(withOverride: false)
                            )
                        ),
                    CED.space,
                    CED.FoldoutGroup(
                        "Planar Reflection",
                        (s, p, o) => s.isSectionExpandedPlanarReflection,
                        FoldoutOption.Indent,
                        CED.Select(
                            (s, d, o) => s.defaultPlanarReflectionCaptureSettings,
                            (s, d, o) => d.defaultPlanarReflectionCaptureSettings,
                            CaptureSettingsUI.SectionCaptureSettings(withOverride: false)
                            ),
                        CED.Select(
                            (s, d, o) => s.defaultPlanarReflectionFrameSettings,
                            (s, d, o) => d.defaultPlanarReflectionFrameSettings,
                            FrameSettingsUI.Inspector(withOverride: false)
                            )
                        )

                    );
        }



        public static readonly CED.IDrawer Inspector;

        public static readonly CED.IDrawer SectionPrimarySettings = CED.Action(Drawer_SectionPrimarySettings);

        public FrameSettingsUI defaultFrameSettings = new FrameSettingsUI();
        public FrameSettingsUI defaultCubeReflectionFrameSettings = new FrameSettingsUI();
        public FrameSettingsUI defaultPlanarReflectionFrameSettings = new FrameSettingsUI();
        public CaptureSettingsUI defaultCubeReflectionCaptureSettings = new CaptureSettingsUI();
        public CaptureSettingsUI defaultPlanarReflectionCaptureSettings = new CaptureSettingsUI();
        public RenderPipelineSettingsUI renderPipelineSettings = new RenderPipelineSettingsUI();


        public AnimBool isSectionExpandedCamera { get { return m_AnimBools[0]; } }
        public AnimBool isSectionExpandedCubeReflection { get { return m_AnimBools[1]; } }
        public AnimBool isSectionExpandedPlanarReflection { get { return m_AnimBools[2]; } }

        public HDRenderPipelineUI()
            : base(3)
        {
        }

        public override void Reset(SerializedHDRenderPipelineAsset data, UnityAction repaint)
        {
            renderPipelineSettings.Reset(data.renderPipelineSettings, repaint);
            defaultFrameSettings.Reset(data.defaultFrameSettings, repaint);
            //defaultCubeReflectionSettings.Reset(data.defaultCubeReflectionSettings, repaint);
            //defaultPlanarReflectionSettings.Reset(data.defaultPlanarReflectionSettings, repaint);
            base.Reset(data, repaint);
        }

        public override void Update()
        {
            renderPipelineSettings.Update();
            defaultFrameSettings.Update();
            base.Update();
        }

        static void Drawer_TitleDefaultFrameSettings(HDRenderPipelineUI s, SerializedHDRenderPipelineAsset d, Editor o)
        {
            EditorGUILayout.LabelField(_.GetContent("Default Frame Settings"), EditorStyles.boldLabel);
        }

        static void Drawer_SectionPrimarySettings(HDRenderPipelineUI s, SerializedHDRenderPipelineAsset d, Editor o)
        {
            EditorGUILayout.PropertyField(d.renderPipelineResources, _.GetContent("Render Pipeline Resources|Set of resources that need to be loaded when creating stand alone"));
            EditorGUILayout.PropertyField(d.diffusionProfileSettings, _.GetContent("Diffusion Profile Settings"));
            EditorGUILayout.PropertyField(d.allowShaderVariantStripping, _.GetContent("Enable Shader Variant Stripping (experimental)"));
        }
    }
}
