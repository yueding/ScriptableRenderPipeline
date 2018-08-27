using System;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Experimental.Rendering.LightweightPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.LightweightPipeline
{
    [CanEditMultipleObjects]
    [CustomEditorForRenderPipeline(typeof(Light), typeof(LightweightPipelineAsset))]
    class LightweightLightEditor : LightEditor
    {
        AnimBool m_AnimSpotOptions = new AnimBool();
        AnimBool m_AnimPointOptions = new AnimBool();
        AnimBool m_AnimDirOptions = new AnimBool();
        AnimBool m_AnimAreaOptions = new AnimBool();
        AnimBool m_AnimRuntimeOptions = new AnimBool();
        AnimBool m_AnimShadowOptions = new AnimBool();
        AnimBool m_AnimShadowAngleOptions = new AnimBool();
        AnimBool m_AnimShadowRadiusOptions = new AnimBool();
        AnimBool m_AnimLightBounceIntensity = new AnimBool();

        class Styles
        {
            public readonly GUIContent Cookie = new GUIContent("Cookie", "Specifies the Texture mask to cast shadows, create silhouettes, or patterned illumination for the light.");
            public readonly GUIContent CookieSize = new GUIContent("Cookie Size", "Controls the size of the cookie mask currently assigned to the light.");

            public readonly GUIStyle invisibleButton = "InvisibleButton";

            public readonly GUIContent BakingWarning = new GUIContent("Light mode is currently overridden to Realtime mode. Enable Baked Global Illumination to use Mixed or Baked light modes.");
            public readonly GUIContent CookieWarning = new GUIContent("Cookie textures for spot lights should be set to clamp, not repeat, to avoid artifacts.");
            public readonly GUIContent DisabledLightWarning = new GUIContent("Lighting has been disabled in at least one Scene view. Any changes applied to lights in the Scene will not be updated in these views until Lighting has been enabled again.");

            public readonly GUIContent ShadowsNotSupportedWarning = new GUIContent("Realtime shadows for point lights are not supported. Either disable shadows or set the light mode to Baked.");
        }

        static Styles s_Styles;

        public bool typeIsSame { get { return !settings.lightType.hasMultipleDifferentValues; } }
        public bool shadowTypeIsSame { get { return !settings.shadowsType.hasMultipleDifferentValues; } }
        public bool lightmappingTypeIsSame { get { return !settings.lightmapping.hasMultipleDifferentValues; } }
        public Light lightProperty { get { return target as Light; } }

        public bool spotOptionsValue { get { return typeIsSame && lightProperty.type == LightType.Spot; } }
        public bool pointOptionsValue { get { return typeIsSame && lightProperty.type == LightType.Point; } }
        public bool dirOptionsValue { get { return typeIsSame && lightProperty.type == LightType.Directional; } }
        public bool areaOptionsValue { get { return typeIsSame && lightProperty.type == LightType.Rectangle; } }

        // Point light realtime shadows not supported
        public bool runtimeOptionsValue { get { return typeIsSame && (lightProperty.type != LightType.Rectangle && lightProperty.type != LightType.Point && !settings.isCompletelyBaked); } }
        public bool bakedShadowRadius { get { return typeIsSame && (lightProperty.type == LightType.Point || lightProperty.type == LightType.Spot) && settings.isBakedOrMixed; } }
        public bool bakedShadowAngle { get { return typeIsSame && lightProperty.type == LightType.Directional && settings.isBakedOrMixed; } }
        public bool shadowOptionsValue { get { return shadowTypeIsSame && lightProperty.shadows != LightShadows.None; } }

        public bool bakingWarningValue { get { return !UnityEditor.Lightmapping.bakedGI && lightmappingTypeIsSame && settings.isBakedOrMixed; } }
        public bool showLightBounceIntensity { get { return true; } }
        public bool cookieWarningValue
        {
            get
            {
                return typeIsSame && lightProperty.type == LightType.Spot &&
                    !settings.cookieProp.hasMultipleDifferentValues && settings.cookie && settings.cookie.wrapMode != TextureWrapMode.Clamp;
            }
        }

        public bool isShadowEnabled { get { return settings.shadowsType.intValue != 0; } }

        public bool realtimeShadowsWarningValue
        {
            get
            {
                return typeIsSame && lightProperty.type == LightType.Point &&
                    shadowTypeIsSame && isShadowEnabled &&
                    lightmappingTypeIsSame && !settings.isCompletelyBaked;
            }
        }

        protected override void OnEnable()
        {
            settings.OnEnable();
            UpdateShowOptions(true);
        }

        public override void OnInspectorGUI()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            settings.Update();

            // Update AnimBool options. For properties changed they will be smoothly interpolated.
            UpdateShowOptions(false);

            settings.DrawLightType();

            EditorGUILayout.Space();

            // When we are switching between two light types that don't show the range (directional and area lights)
            // we want the fade group to stay hidden.
            using (var group = new EditorGUILayout.FadeGroupScope(1.0f - m_AnimDirOptions.faded))
                if (group.visible)
                    settings.DrawRange(m_AnimAreaOptions.target);

            // Spot angle
            using (var group = new EditorGUILayout.FadeGroupScope(m_AnimSpotOptions.faded))
                if (group.visible)
                    settings.DrawInnerAndOuterSpotAngle();

            // Area width & height
            using (var group = new EditorGUILayout.FadeGroupScope(m_AnimAreaOptions.faded))
                if (group.visible)
                    settings.DrawArea();

            settings.DrawColor();

            EditorGUILayout.Space();

            using (var group = new EditorGUILayout.FadeGroupScope(1.0f - m_AnimAreaOptions.faded))
                if (group.visible)
                    settings.DrawLightmapping();

            settings.DrawIntensity();

            using (var group = new EditorGUILayout.FadeGroupScope(m_AnimLightBounceIntensity.faded))
                if (group.visible)
                    settings.DrawBounceIntensity();

            ShadowsGUI();

            /* Tim: Disable cookie for v1 to save on shader combinations
            using (var group = new EditorGUILayout.FadeGroupScope(animShowRuntimeOptions.faded))
                if (group.visible)
                    DrawCookie();

            // Cookie size also requires directional light
            using (var group = new EditorGUILayout.FadeGroupScope(animShowRuntimeOptions.faded * animShowDirOptions.faded))
                if (group.visible)
                    DrawCookieSize();
           */

            settings.DrawRenderMode();
            settings.DrawCullingMask();

            EditorGUILayout.Space();

            if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.m_SceneLighting == false)
                EditorGUILayout.HelpBox(s_Styles.DisabledLightWarning.text, MessageType.Warning);

            serializedObject.ApplyModifiedProperties();
        }

        void SetOptions(AnimBool animBool, bool initialize, bool targetValue)
        {
            if (initialize)
            {
                animBool.value = targetValue;
                animBool.valueChanged.AddListener(Repaint);
            }
            else
            {
                animBool.target = targetValue;
            }
        }

        void UpdateShowOptions(bool initialize)
        {
            SetOptions(m_AnimSpotOptions, initialize, spotOptionsValue);
            SetOptions(m_AnimPointOptions, initialize, pointOptionsValue);
            SetOptions(m_AnimDirOptions, initialize, dirOptionsValue);
            SetOptions(m_AnimAreaOptions, initialize, areaOptionsValue);
            SetOptions(m_AnimShadowOptions, initialize, shadowOptionsValue);
            SetOptions(m_AnimRuntimeOptions, initialize, runtimeOptionsValue);
            SetOptions(m_AnimShadowAngleOptions, initialize, bakedShadowAngle);
            SetOptions(m_AnimShadowRadiusOptions, initialize, bakedShadowRadius);
            SetOptions(m_AnimLightBounceIntensity, initialize, showLightBounceIntensity);
        }

        void DrawCookie()
        {
            EditorGUILayout.PropertyField(settings.cookieProp, s_Styles.Cookie);

            if (cookieWarningValue)
            {
                // warn on spotlights if the cookie is set to repeat
                EditorGUILayout.HelpBox(s_Styles.CookieWarning.text, MessageType.Warning);
            }
        }

        void DrawCookieSize()
        {
            EditorGUILayout.PropertyField(settings.cookieSize, s_Styles.CookieSize);
        }

        void ShadowsGUI()
        {
            // Shadows drop-down. Area lights can only be baked and always have shadows.
            float show = 1.0f - m_AnimAreaOptions.faded;
            using (new EditorGUILayout.FadeGroupScope(show))
                settings.DrawShadowsType();

            EditorGUI.indentLevel += 1;
            show *= m_AnimShadowOptions.faded;
            // Baked Shadow radius
            using (var group = new EditorGUILayout.FadeGroupScope(show * m_AnimShadowRadiusOptions.faded))
                if (group.visible)
                    settings.DrawBakedShadowRadius();

            // Baked Shadow angle
            using (var group = new EditorGUILayout.FadeGroupScope(show * m_AnimShadowAngleOptions.faded))
                if (group.visible)
                    settings.DrawBakedShadowAngle();

            // Runtime shadows - shadow strength, resolution, bias
            using (var group = new EditorGUILayout.FadeGroupScope(show * m_AnimRuntimeOptions.faded))
                if (group.visible)
                    settings.DrawRuntimeShadow();
            EditorGUI.indentLevel -= 1;

            if (bakingWarningValue)
                EditorGUILayout.HelpBox(s_Styles.BakingWarning.text, MessageType.Warning);

            if (realtimeShadowsWarningValue)
                EditorGUILayout.HelpBox(s_Styles.ShadowsNotSupportedWarning.text, MessageType.Warning);

            EditorGUILayout.Space();
        }

        int m_HandleHotControl = 0;
        bool m_ShowOuterLabel = true;
        bool m_ShowRange = false;

        protected override void OnSceneGUI()
        {
            Light light = target as Light;

            if (!(GraphicsSettings.renderPipelineAsset is LightweightPipelineAsset))
                return;

            if( light.type == LightType.Spot )
            {
                Vector2 angleAndRange = new Vector2(light.spotAngle, light.range);
                Vector2 innerAngleAndRange = new Vector2(light.innerSpotAngle, light.range);
                Handles.color = Color.white;
                EditorGUI.BeginChangeCheck();
                angleAndRange = CoreLightEditorUtilities.DrawConeHandles(light.transform.rotation, light.transform.position, angleAndRange);
                if (EditorGUI.EndChangeCheck())
                {
                    m_HandleHotControl = GUIUtility.hotControl;
                    m_ShowOuterLabel = true;
                }

                EditorGUI.BeginChangeCheck();
                innerAngleAndRange = CoreLightEditorUtilities.DrawConeHandles(light.transform.rotation, light.transform.position, innerAngleAndRange);
                if (EditorGUI.EndChangeCheck())
                {
                    m_HandleHotControl = GUIUtility.hotControl;
                    m_ShowOuterLabel = false;
                }

                float range = light.range;
                EditorGUI.BeginChangeCheck();
                range = CoreLightEditorUtilities.DrawCenterHandle(light.transform.rotation, light.transform.position, range);
                if (EditorGUI.EndChangeCheck())
                {
                    m_HandleHotControl = GUIUtility.hotControl;
                    m_ShowRange = true;
                }

                var innerProcentage = (light.innerSpotAngle / light.spotAngle);
                Handles.color = Color.yellow;
                CoreLightEditorUtilities.DrawSpotlightGizmo(light, innerProcentage, true);

                Vector3 labelPosition = (light.transform.position + light.transform.forward * light.range);

                if (GUIUtility.hotControl != 0 && GUIUtility.hotControl == m_HandleHotControl)
                {
                    string labelText = "";
                    if (m_ShowRange)
                        labelText = (light.range).ToString("0.00");
                    else if (m_ShowOuterLabel)
                        labelText = (light.spotAngle).ToString("0.00");
                    else
                        labelText = (light.innerSpotAngle).ToString("0.00");

                    var style = new GUIStyle(GUI.skin.label);
                    var offsetFromHandle = 10;
                    style.contentOffset = new Vector2(0, -(style.font.lineHeight + HandleUtility.GetHandleSize(labelPosition) * 0.03f + offsetFromHandle));
                    Handles.Label(labelPosition, labelText, style);
                }

                if (GUI.changed)
                {
                    light.spotAngle = angleAndRange.x;

                    light.innerSpotAngle = innerAngleAndRange.x;
                    light.range = Math.Max(range, 0.01f);
                }
            }

            if (EditorGUIUtility.hotControl == 0 && EditorGUIUtility.hotControl != m_HandleHotControl)
            {
                m_HandleHotControl = 0;
                m_ShowOuterLabel = true;
                m_ShowRange = false;
            }
        }
    }
}
