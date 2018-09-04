using System;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Events;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<FrameSettingsUI, SerializedFrameSettings>;

    partial class FrameSettingsUI
    {
        internal static CED.IDrawer Inspector(bool withOverride = true, bool withXR = true)
        {
            return CED.Group(
                    SectionRenderingPasses(withOverride),
                    SectionRenderingSettings(withOverride),
                    CED.FadeGroup(
                        (s, d, o, i) => new AnimBool(withXR),
                        FadeOption.None,
                        SectionXRSettings(withOverride)),
                    SectionLightingSettings(withOverride),
                    CED.Select(
                        (s, d, o) => s.lightLoopSettings,
                        (s, d, o) => d.lightLoopSettings,
                        LightLoopSettingsUI.SectionLightLoopSettings(withOverride)
                        )
                    );
        }

        public static CED.IDrawer SectionRenderingPasses(bool withOverride)
        {
            return CED.FoldoutGroup(
                renderingPassesHeaderContent,
                (s, p, o) => s.isSectionExpandedRenderingPasses,
                FoldoutOption.Indent,
                CED.LabelWidth(200, CED.Action((s, p, o) => Drawer_SectionRenderingPasses(s, p, o, withOverride)))
                );
        }


        public static CED.IDrawer SectionRenderingSettings(bool withOverride)
        {
            return CED.FoldoutGroup(
                renderingSettingsHeaderContent,
                (s, p, o) => s.isSectionExpandedRenderingSettings,
                FoldoutOption.Indent,
                CED.LabelWidth(300,
                    CED.Action((s, p, o) => Drawer_FieldForwardRenderingOnly(s, p, o, withOverride)),
                    CED.FadeGroup(
                        (s, d, o, i) => s.isSectionExpandedUseForwardOnly,
                        FadeOption.None,
                        CED.Action((s, p, o) => Drawer_FieldUseDepthPrepassWithDefferedRendering(s, p, o, withOverride))
                        ),
                    CED.Action((s, p, o) => Drawer_SectionOtherRenderingSettings(s, p, o, withOverride))
                    )
                );
        }

        public static CED.IDrawer SectionXRSettings(bool withOverride)
        {
            return CED.FadeGroup(
                (s, d, o, i) => s.isSectionExpandedXRSupported,
                FadeOption.None,
                CED.FoldoutGroup(
                    xrSettingsHeaderContent,
                    (s, p, o) => s.isSectionExpandedXRSettings,
                    FoldoutOption.Indent,
                    CED.LabelWidth(200, CED.Action((s, p, o) => Drawer_FieldStereoEnabled(s, p, o, withOverride)))));
        }

        public static CED.IDrawer SectionLightingSettings(bool withOverride)
        {
            return CED.FoldoutGroup(
                lightSettingsHeaderContent,
                (s, p, o) => s.isSectionExpandedLightingSettings,
                FoldoutOption.Indent,
                CED.LabelWidth(250, CED.Action((s, p, o) => Drawer_SectionLightingSettings(s, p, o, withOverride))));
        }

        static void Drawer_SectionRenderingPasses(FrameSettingsUI s, SerializedFrameSettings p, Editor owner, bool withOverride)
        {
            DrawProperty(p.enableTransparentPrepass, transparentPrepassContent, withOverride, a => p.overridesTransparentPrepass = a, () => p.overridesTransparentPrepass);
            DrawProperty(p.enableTransparentPostpass, transparentPostpassContent, withOverride, a => p.overridesTransparentPostpass = a, () => p.overridesTransparentPostpass);
            DrawProperty(p.enableMotionVectors, motionVectorContent, withOverride, a => p.overridesMotionVectors = a, () => p.overridesMotionVectors);
            DrawProperty(p.enableObjectMotionVectors, objectMotionVectorsContent, withOverride, a => p.overridesObjectMotionVectors = a, () => p.overridesObjectMotionVectors);
            DrawProperty(p.enableDecals, decalsContent, withOverride, a => p.overridesDecals = a, () => p.overridesDecals);
            DrawProperty(p.enableRoughRefraction, roughRefractionContent, withOverride, a => p.overridesRoughRefraction = a, () => p.overridesRoughRefraction);
            DrawProperty(p.enableDistortion, distortionContent, withOverride, a => p.overridesDistortion = a, () => p.overridesDistortion);
            DrawProperty(p.enablePostprocess, postprocessContent, withOverride, a => p.overridesPostprocess = a, () => p.overridesPostprocess);
        }

        static void Drawer_FieldForwardRenderingOnly(FrameSettingsUI s, SerializedFrameSettings p, Editor owner, bool withOverride)
        {
            DrawProperty(p.enableForwardRenderingOnly, forwardRenderingOnlyContent, withOverride, a => p.overridesForwardRenderingOnly = a, () => p.overridesForwardRenderingOnly);
        }

        static void Drawer_FieldUseDepthPrepassWithDefferedRendering(FrameSettingsUI s, SerializedFrameSettings p, Editor owner, bool withOverride)
        {
            DrawProperty(p.enableDepthPrepassWithDeferredRendering, depthPrepassWithDeferredRenderingContent, withOverride, a => p.overridesDepthPrepassWithDeferredRendering = a, () => p.overridesDepthPrepassWithDeferredRendering);
        }

        static void Drawer_SectionOtherRenderingSettings(FrameSettingsUI s, SerializedFrameSettings p, Editor owner, bool withOverride)
        {
            DrawProperty(p.enableAsyncCompute, asyncComputeContent, withOverride, a => p.overridesAsyncCompute = a, () => p.overridesAsyncCompute);

            DrawProperty(p.enableOpaqueObjects, opaqueObjectsContent, withOverride, a => p.overridesOpaqueObjects = a, () => p.overridesOpaqueObjects);
            DrawProperty(p.enableTransparentObjects, transparentObjectsContent, withOverride, a => p.overridesTransparentObjects = a, () => p.overridesTransparentObjects);

            // Hide for now as not supported
            //DrawProperty(p.enableMSAA, msaaContent, withOverride, a => p.overridesMSAA = a, () => p.overridesMSAA);
        }

        static void Drawer_FieldStereoEnabled(FrameSettingsUI s, SerializedFrameSettings p, Editor owner, bool withOverride)
        {
            DrawProperty(p.enableStereo, stereoContent, withOverride, a => p.overridesStereo = a, () => p.overridesStereo);
        }

        static void Drawer_SectionLightingSettings(FrameSettingsUI s, SerializedFrameSettings p, Editor owner, bool withOverride)
        {
            DrawProperty(p.enableShadow, shadowContent, withOverride, a => p.overridesShadow = a, () => p.overridesShadow);
            DrawProperty(p.enableContactShadow, contactShadowContent, withOverride, a => p.overridesContactShadow = a, () => p.overridesContactShadow);
            DrawProperty(p.enableShadowMask, shadowMaskContent, withOverride, a => p.overridesShadowMask = a, () => p.overridesShadowMask);
            DrawProperty(p.enableSSR, ssrContent, withOverride, a => p.overridesSSR = a, () => p.overridesSSR);
            DrawProperty(p.enableSSAO, ssaoContent, withOverride, a => p.overridesSSAO = a, () => p.overridesSSAO);
            DrawProperty(p.enableSubsurfaceScattering, subsurfaceScatteringContent, withOverride, a => p.overridesSubsurfaceScattering = a, () => p.overridesSubsurfaceScattering);
            DrawProperty(p.enableTransmission, transmissionContent, withOverride, a => p.overridesTransmission = a, () => p.overridesTransmission);
            DrawProperty(p.enableAtmosphericScattering, atmosphericScatteringContent, withOverride, a => p.overridesAtmosphericScaterring = a, () => p.overridesAtmosphericScaterring);
            DrawProperty(p.enableVolumetric, volumetricContent, withOverride, a => p.overridesVolumetrics = a, () => p.overridesVolumetrics);
            DrawProperty(p.enableLightLayers, lightLayerContent, withOverride, a => p.overridesLightLayers = a, () => p.overridesLightLayers);
        }

        internal static void DrawProperty(SerializedProperty p, GUIContent c, bool withOverride, Action<bool> setter, Func<bool> getter)
        {
            if(withOverride)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool originalValue = getter();
                    bool modifiedValue = originalValue;

                    var overrideRect = GUILayoutUtility.GetRect(17f, 17f, GUILayout.ExpandWidth(false));
                    overrideRect.yMin += 4f;
                    modifiedValue = GUI.Toggle(overrideRect, originalValue, CoreEditorUtils.GetContent("|Override this setting component."), CoreEditorStyles.smallTickbox);

                    if(originalValue ^ modifiedValue)
                    {
                        setter(modifiedValue);
                    }

                    using (new EditorGUI.DisabledScope(!modifiedValue))
                    {
                        EditorGUILayout.PropertyField(p, c);
                    }
                }
            }
            else
                EditorGUILayout.PropertyField(p, c);
        }
    }
}
