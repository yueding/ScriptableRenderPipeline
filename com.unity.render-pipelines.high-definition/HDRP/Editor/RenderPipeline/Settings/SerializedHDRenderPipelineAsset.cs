using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class SerializedHDRenderPipelineAsset
    {
        public SerializedObject serializedObject;

        public SerializedProperty renderPipelineResources;
        public SerializedProperty diffusionProfileSettings;
        public SerializedProperty allowShaderVariantStripping;

        public SerializedRenderPipelineSettings renderPipelineSettings;
        public SerializedFrameSettings defaultFrameSettings;
        public SerializedFrameSettings defaultCubeReflectionSettings;
        public SerializedFrameSettings defaultPlanarReflectionSettings;

        public enum FrameSettings
        {
            Camera,
            CubeReflection,
            PlanarReflection
        }
        public FrameSettings currentlyEdited = FrameSettings.Camera;

        public SerializedHDRenderPipelineAsset(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;

            renderPipelineResources = serializedObject.FindProperty("m_RenderPipelineResources");
            diffusionProfileSettings = serializedObject.Find((HDRenderPipelineAsset s) => s.diffusionProfileSettings);
            allowShaderVariantStripping = serializedObject.Find((HDRenderPipelineAsset s) => s.allowShaderVariantStripping);

            renderPipelineSettings = new SerializedRenderPipelineSettings(serializedObject.Find((HDRenderPipelineAsset a) => a.renderPipelineSettings));
            defaultFrameSettings = new SerializedFrameSettings(serializedObject.FindProperty("m_FrameSettings"));
            defaultCubeReflectionSettings = new SerializedFrameSettings(serializedObject.FindProperty("m_CubeReflectionSettings"));
            defaultPlanarReflectionSettings = new SerializedFrameSettings(serializedObject.FindProperty("m_PlanarReflectionSettings"));
        }

        public void Update()
        {
            serializedObject.Update();
        }

        public void Apply()
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}
