using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.Experimental.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    [CustomEditor(typeof(LightweightPipelineAsset))]
    public class LightweightPipelineAssetEditor : Editor
    {
        bool generalSettingsFoldout = false;
        bool qualitySettingsFoldout = false;
        bool shadowsSettingsFoldout = false;
        internal class Styles
        {
            public static GUIContent generalSettingsLabel = new GUIContent("General");
            public static GUIContent qualityLabel = new GUIContent("Quality");
            public static GUIContent shadowLabel = new GUIContent("Shadows");
            public static GUIContent directionalShadowLabel = new GUIContent("Directional Shadows");
            public static GUIContent localShadowLabel = new GUIContent("Local Shadows");
            public static GUIContent featuresLabel = new GUIContent("Shader Features");

            public static GUIContent renderScaleLabel = new GUIContent("Render Scale", "Scales the camera render target allowing the game to render at a resolution different than native resolution. UI is always rendered at native resolution.");

            public static GUIContent maxPixelLightsLabel = new GUIContent("Pixel Lights",
                    "Controls the amount of pixel lights that run in fragment light loop. Lights are sorted and culled per-object.");

            public static GUIContent requireDepthTexture = new GUIContent("Depth Texture", "If enabled the pipeline will generate camera's depth that can be bound in shaders as _CameraDepthTexture.");

            public static GUIContent requireSoftParticles = new GUIContent("Soft Particles", "If enabled the pipeline will enable SOFT_PARTICLES keyword.\nNeeds Depth Texture to be enabled.");

            public static GUIContent requireOpaqueTexture = new GUIContent("Opaque Texture", "If enabled the pipeline will copy the screen to texture after opaque objects are drawn. For transparent objects this can be bound in shaders as _CameraOpaqueTexture.");

            public static GUIContent opaqueDownsampling = new GUIContent("Opaque Downsampling", "The downsampling method that is used for the opaque texture");
            public static GUIContent hdrContent = new GUIContent("HDR", "Controls the global HDR settings.");
            public static GUIContent msaaContent = new GUIContent("Anti Aliasing (MSAA)", "Controls the global anti aliasing settings.");
            public static GUIContent dynamicBatching = new GUIContent("Dynamic Batching", "If enabled the pipeline will batch drawcalls with few triangles together by copying their vertex buffers into a shared buffer on a per-frame basis.");

            public static GUIContent supportsSoftShadows = new GUIContent("Soft Shadows", "If enabled pipeline will perform shadow filtering. Otherwise all lights that cast shadows will fallback to perform a single shadow sample.\nNeeds either Directional or Punctual Shadows to be enabled in the Capabilities section.");
            public static GUIContent supportsDirectionalShadows = new GUIContent("Directional Shadows", "If enabled shadows will be supported for directional lights.\nNeeds Directional Shadows to be enabled in the Capabilities section.");

            public static GUIContent punctualLightsSupportLabel = EditorGUIUtility.TrTextContent("Punctual Lights", "Support for realtime point and spot lights.");
            public static GUIContent mixedLightingSupportLabel = EditorGUIUtility.TrTextContent("Mixed Lighting", "Support for mixed light mode.");
            

            public static GUIContent shadowDistance = new GUIContent("Distance", "Max shadow rendering distance.");

            public static GUIContent directionalShadowAtlasResolution = new GUIContent("Atlas Resolution",
                    "Resolution of the directional shadow map texture. If cascades are enabled, cascades will be packed into an atlas and this setting controls the max shadows atlas resolution.");

            public static GUIContent shadowCascades = new GUIContent("Cascades",
                    "Number of cascades used in directional lights shadows");

            public static GUIContent shadowCascadeSplit = new GUIContent("Cascades Split",
                    "Percentages to split shadow volume");

            public static GUIContent supportsPunctualShadows = new GUIContent("Punctual Shadows", "If enabled shadows will be supported for spot lights.\n");

            public static GUIContent punctualShadowsAtlasResolution = new GUIContent("Atlas Resolution",
                    "All local lights are packed into a single atlas. This setting controls the atlas size.");

            public static string[] punctualLightsOptions = {"Not Supported", "Per Vertex", "Per Pixel"};
            public static string[] shadowCascadeOptions = {"No Cascades", "Two Cascades", "Four Cascades"};
            public static string[] opaqueDownsamplingOptions = {"None", "2x (Bilinear)", "4x (Box)", "4x (Bilinear)"};

            public static GUIContent XRConfig = new GUIContent("XR Graphics Settings", "SRP will attempt to set this configuration to the VRDevice.");
        }

        AnimBool m_ShowSoftParticles = new AnimBool();
        AnimBool m_ShowOpaqueTextureScale = new AnimBool();

        int k_MaxSupportedPixelLights = 8;
        float k_MinRenderScale = 0.1f;
        float k_MaxRenderScale = 4.0f;
        SerializedProperty m_RenderScale;
        SerializedProperty m_MaxPixelLights;
        SerializedProperty m_RequireDepthTextureProp;
        SerializedProperty m_RequireSoftParticlesProp;
        SerializedProperty m_RequireOpaqueTextureProp;
        SerializedProperty m_OpaqueDownsamplingProp;
        SerializedProperty m_HDR;
        SerializedProperty m_MSAA;
        SerializedProperty m_SupportsDynamicBatching;

        SerializedProperty m_SoftShadowsSupportedProp;
        SerializedProperty m_DirectionalShadowsSupportedProp;
        SerializedProperty m_PunctualLightsSupportProp;
        SerializedProperty m_ShadowDistanceProp;
        SerializedProperty m_DirectionalShadowAtlasResolutionProp;
        SerializedProperty m_ShadowCascadesProp;
        SerializedProperty m_ShadowCascade2SplitProp;
        SerializedProperty m_ShadowCascade4SplitProp;
        SerializedProperty m_PunctualShadowSupportedProp;
        SerializedProperty m_PunctualShadowsAtlasResolutionProp;
        SerializedProperty m_MixedLightingSupportedProp;

        SerializedProperty m_XRConfig;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            UpdateAnimationValues();

            DrawShaderFeaturesSettings();
            DrawGeneralSettings();
            DrawQualitySettings();
            DrawShadowSettings();
            EditorGUILayout.PropertyField(m_XRConfig);

            serializedObject.ApplyModifiedProperties();
        }

        void OnEnable()
        {
            m_RenderScale = serializedObject.FindProperty("m_RenderScale");
            m_MaxPixelLights = serializedObject.FindProperty("m_MaxPixelLights");
            m_RequireDepthTextureProp = serializedObject.FindProperty("m_RequireDepthTexture");
            m_RequireSoftParticlesProp = serializedObject.FindProperty("m_RequireSoftParticles");
            m_RequireOpaqueTextureProp = serializedObject.FindProperty("m_RequireOpaqueTexture");
            m_OpaqueDownsamplingProp = serializedObject.FindProperty("m_OpaqueDownsampling");
            m_HDR = serializedObject.FindProperty("m_SupportsHDR");
            m_MSAA = serializedObject.FindProperty("m_MSAA");
            m_SupportsDynamicBatching = serializedObject.FindProperty("m_SupportsDynamicBatching");
            m_DirectionalShadowsSupportedProp = serializedObject.FindProperty("m_DirectionalShadowsSupported");
            m_PunctualLightsSupportProp = serializedObject.FindProperty("m_PunctualLightsSupport");
            m_ShadowDistanceProp = serializedObject.FindProperty("m_ShadowDistance");
            m_DirectionalShadowAtlasResolutionProp = serializedObject.FindProperty("m_ShadowAtlasResolution");
            m_ShadowCascadesProp = serializedObject.FindProperty("m_ShadowCascades");
            m_ShadowCascade2SplitProp = serializedObject.FindProperty("m_Cascade2Split");
            m_ShadowCascade4SplitProp = serializedObject.FindProperty("m_Cascade4Split");
            m_PunctualShadowSupportedProp = serializedObject.FindProperty("m_LocalShadowsSupported");
            m_PunctualShadowsAtlasResolutionProp = serializedObject.FindProperty("m_LocalShadowsAtlasResolution");
            m_SoftShadowsSupportedProp = serializedObject.FindProperty("m_SoftShadowsSupported");
            m_MixedLightingSupportedProp = serializedObject.FindProperty("m_MixedLightingSupported");

            m_ShowSoftParticles.valueChanged.AddListener(Repaint);
            m_ShowSoftParticles.value = m_RequireSoftParticlesProp.boolValue;
            m_ShowOpaqueTextureScale.valueChanged.AddListener(Repaint);
            m_ShowOpaqueTextureScale.value = m_RequireOpaqueTextureProp.boolValue;
            m_XRConfig = serializedObject.FindProperty("m_SavedXRConfig");
        }

        void OnDisable()
        {
            m_ShowSoftParticles.valueChanged.RemoveListener(Repaint);
            m_ShowOpaqueTextureScale.valueChanged.RemoveListener(Repaint);
        }

        void UpdateAnimationValues()
        {
            m_ShowSoftParticles.target = m_RequireDepthTextureProp.boolValue;
            m_ShowOpaqueTextureScale.target = m_RequireOpaqueTextureProp.boolValue;
        }

        void DrawShaderFeaturesSettings()
        {
            EditorGUILayout.LabelField(Styles.featuresLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_DirectionalShadowsSupportedProp, Styles.supportsDirectionalShadows);
            CoreEditorUtils.DrawPopup(Styles.punctualLightsSupportLabel, m_PunctualLightsSupportProp, Styles.punctualLightsOptions);
            EditorGUILayout.PropertyField(m_PunctualShadowSupportedProp, Styles.supportsPunctualShadows);
            EditorGUI.BeginDisabledGroup(!(m_DirectionalShadowsSupportedProp.boolValue || m_PunctualShadowSupportedProp.boolValue));
                EditorGUILayout.PropertyField(m_SoftShadowsSupportedProp, Styles.supportsSoftShadows);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.PropertyField(m_MixedLightingSupportedProp, Styles.mixedLightingSupportLabel);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        void DrawGeneralSettings()
        {
            generalSettingsFoldout = EditorGUILayout.Foldout(generalSettingsFoldout, Styles.generalSettingsLabel, true);
            if (generalSettingsFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_SupportsDynamicBatching, Styles.dynamicBatching);

                EditorGUILayout.PropertyField(m_RequireDepthTextureProp, Styles.requireDepthTexture);
                EditorGUI.BeginDisabledGroup(!m_RequireDepthTextureProp.boolValue);
                EditorGUILayout.PropertyField(m_RequireSoftParticlesProp, Styles.requireSoftParticles);
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.PropertyField(m_RequireOpaqueTextureProp, Styles.requireOpaqueTexture);
                EditorGUI.indentLevel++;
                EditorGUI.BeginDisabledGroup(!m_RequireOpaqueTextureProp.boolValue);
                EditorGUILayout.PropertyField(m_OpaqueDownsamplingProp, Styles.opaqueDownsampling);
                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
        }

        void DrawQualitySettings()
        {
            qualitySettingsFoldout = EditorGUILayout.Foldout(qualitySettingsFoldout, Styles.qualityLabel, true);
            if (qualitySettingsFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(m_HDR, Styles.hdrContent);
                EditorGUILayout.PropertyField(m_MSAA, Styles.msaaContent);
                m_RenderScale.floatValue = EditorGUILayout.Slider(Styles.renderScaleLabel, m_RenderScale.floatValue, k_MinRenderScale, k_MaxRenderScale);
                m_MaxPixelLights.intValue = EditorGUILayout.IntSlider(Styles.maxPixelLightsLabel, m_MaxPixelLights.intValue, 0, k_MaxSupportedPixelLights);

                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
        }

        void DrawShadowSettings()
        {
            shadowsSettingsFoldout = EditorGUILayout.Foldout(shadowsSettingsFoldout, Styles.shadowLabel, true);
            if (shadowsSettingsFoldout)
            {
                // Directional Shadows
                EditorGUI.BeginDisabledGroup(!m_DirectionalShadowsSupportedProp.boolValue);
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField(Styles.directionalShadowLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_DirectionalShadowAtlasResolutionProp, Styles.directionalShadowAtlasResolution);
                m_ShadowDistanceProp.floatValue = Mathf.Max(0.0f,
                    EditorGUILayout.FloatField(Styles.shadowDistance, m_ShadowDistanceProp.floatValue));
                CoreEditorUtils.DrawPopup(Styles.shadowCascades, m_ShadowCascadesProp, Styles.shadowCascadeOptions);

                ShadowCascades cascades = (ShadowCascades)m_ShadowCascadesProp.intValue;
                if (cascades == ShadowCascades.FOUR_CASCADES)
                    CoreEditorUtils.DrawCascadeSplitGUI<Vector3>(ref m_ShadowCascade4SplitProp);
                else if (cascades == ShadowCascades.TWO_CASCADES)
                    CoreEditorUtils.DrawCascadeSplitGUI<float>(ref m_ShadowCascade2SplitProp);

                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
                EditorGUI.EndDisabledGroup();

                // Local Shadows
                EditorGUI.BeginDisabledGroup(!m_PunctualShadowSupportedProp.boolValue);
                EditorGUILayout.LabelField(Styles.localShadowLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_PunctualShadowsAtlasResolutionProp, Styles.punctualShadowsAtlasResolution);
                EditorGUI.indentLevel--;
                EditorGUI.indentLevel--;
                EditorGUI.EndDisabledGroup();
            }
        }
    }
}
