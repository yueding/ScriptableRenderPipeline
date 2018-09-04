using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Events;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using CED = CoreEditorDrawer<CaptureSettingsUI, SerializedCaptureSettings>;

    partial class CaptureSettingsUI : BaseUI<SerializedFrameSettings>
    {
        const string captureSettingsHeaderContent = "Capture Settings";
        static readonly GUIContent clearColorModeContent = CoreEditorUtils.GetContent("Clear Mode");
        static readonly GUIContent backgroundColorHDRContent = CoreEditorUtils.GetContent("Background Color");
        static readonly GUIContent clearDepthContent = CoreEditorUtils.GetContent("Clear Depth");
        static readonly GUIContent renderingPathContent = CoreEditorUtils.GetContent("Rendering Path");
        static readonly GUIContent volumeLayerMaskContent = CoreEditorUtils.GetContent("Volume Layer Mask");
        static readonly GUIContent volumeAnchorOverrideContent = CoreEditorUtils.GetContent("Volume Anchor Override");
        static readonly GUIContent apertureContent = CoreEditorUtils.GetContent("Aperture");
        static readonly GUIContent shutterSpeedContent = CoreEditorUtils.GetContent("Shutter Speed");
        static readonly GUIContent isoContent = CoreEditorUtils.GetContent("Iso");
        static readonly GUIContent shadowDistanceContent = CoreEditorUtils.GetContent("Shadow Distance");
        static readonly GUIContent farClipPlaneContent = CoreEditorUtils.GetContent("Far Clip Plane");
        static readonly GUIContent nearClipPlaneContent = CoreEditorUtils.GetContent("Near Clip Plane");
        static readonly GUIContent fieldOfviewContent = CoreEditorUtils.GetContent("Field Of View");
        static readonly GUIContent useOcclusionCullingContent = CoreEditorUtils.GetContent("Occlusion Culling");
        static readonly GUIContent cullingMaskContent = CoreEditorUtils.GetContent("Culling Mask");
        
        public static CED.IDrawer SectionCaptureSettings(bool withOverride)
        {
            return CED.FoldoutGroup(
                captureSettingsHeaderContent,
                (s, p, o) => s.isSectionExpandedCaptureSettings,
                FoldoutOption.Indent,
                CED.LabelWidth(250, CED.Action((s, p, o) => Drawer_SectionCaptureSettings(s, p, o, withOverride))));
        }

        public AnimBool isSectionExpandedCaptureSettings { get { return m_AnimBools[0]; } }

        public CaptureSettingsUI()
            : base(1)
        {
        }

        static void Drawer_SectionCaptureSettings(CaptureSettingsUI s, SerializedCaptureSettings p, Editor owner, bool withOverride)
        {
            FrameSettingsUI.DrawProperty(p.clearColorMode, clearColorModeContent, withOverride, a => p.overridesClearColorMode = a, () => p.overridesClearColorMode);
            FrameSettingsUI.DrawProperty(p.backgroundColorHDR, backgroundColorHDRContent, withOverride, a => p.overridesBackgroundColorHDR = a, () => p.overridesBackgroundColorHDR);
            FrameSettingsUI.DrawProperty(p.clearDepth, clearDepthContent, withOverride, a => p.overridesClearDepth = a, () => p.overridesClearDepth);
            FrameSettingsUI.DrawProperty(p.renderingPath, renderingPathContent, withOverride, a => p.overridesRenderingPath = a, () => p.overridesRenderingPath);
            FrameSettingsUI.DrawProperty(p.volumeLayerMask, volumeLayerMaskContent, withOverride, a => p.overridesVolumeLayerMask = a, () => p.overridesVolumeLayerMask);
            FrameSettingsUI.DrawProperty(p.volumeAnchorOverride, volumeAnchorOverrideContent, withOverride, a => p.overridesVolumeAnchorOverride = a, () => p.overridesVolumeAnchorOverride);
            FrameSettingsUI.DrawProperty(p.aperture, apertureContent, withOverride, a => p.overridesAperture = a, () => p.overridesAperture);
            FrameSettingsUI.DrawProperty(p.shutterSpeed, shutterSpeedContent, withOverride, a => p.overridesShutterSpeed = a, () => p.overridesShutterSpeed);
            FrameSettingsUI.DrawProperty(p.iso, isoContent, withOverride, a => p.overridesIso = a, () => p.overridesIso);
            FrameSettingsUI.DrawProperty(p.shadowDistance, shadowDistanceContent, withOverride, a => p.overridesShadowDistance = a, () => p.overridesShadowDistance);
            FrameSettingsUI.DrawProperty(p.farClipPlane, farClipPlaneContent, withOverride, a => p.overridesFarClip = a, () => p.overridesFarClip);
            FrameSettingsUI.DrawProperty(p.nearClipPlane, nearClipPlaneContent, withOverride, a => p.overridesNearClip = a, () => p.overridesNearClip);
            FrameSettingsUI.DrawProperty(p.fieldOfview, fieldOfviewContent, withOverride, a => p.overridesFieldOfview = a, () => p.overridesFieldOfview);
            FrameSettingsUI.DrawProperty(p.useOcclusionCulling, useOcclusionCullingContent, withOverride, a => p.overridesUseOcclusionCulling = a, () => p.overridesUseOcclusionCulling);
            FrameSettingsUI.DrawProperty(p.cullingMask, cullingMaskContent, withOverride, a => p.overridesCullingMask = a, () => p.overridesCullingMask);
        }
    }
}
