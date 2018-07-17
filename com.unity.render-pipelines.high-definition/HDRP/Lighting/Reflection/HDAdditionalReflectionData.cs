using UnityEngine.Serialization;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEngine.Experimental.Rendering
{
    [RequireComponent(typeof(ReflectionProbe), typeof(MeshFilter), typeof(MeshRenderer))]
    public class HDAdditionalReflectionData : MonoBehaviour
    {
        [HideInInspector]
        public float version = 1.0f;

        public ShapeType influenceShape = ShapeType.Box;
        [FormerlySerializedAsAttribute("dimmer")]
        public float multiplier = 1.0f;
        [Range(0.0f, 1.0f)]
        public float weight = 1.0f;
        public float influenceSphereRadius = 3.0f;
        public float sphereReprojectionVolumeRadius = 1.0f;
        public bool useSeparateProjectionVolume = false;
        public Vector3 boxReprojectionVolumeSize = Vector3.one;
        public Vector3 boxReprojectionVolumeCenter = Vector3.zero;
        public float maxSearchDistance = 8.0f;
        public Texture previewCubemap;
        public Vector3 blendDistancePositive = Vector3.zero;
        public Vector3 blendDistanceNegative = Vector3.zero;
        public Vector3 blendNormalDistancePositive = Vector3.zero;
        public Vector3 blendNormalDistanceNegative = Vector3.zero;
        public Vector3 boxSideFadePositive = Vector3.one;
        public Vector3 boxSideFadeNegative = Vector3.one;

        //editor value that need to be saved for easy passing from simplified to advanced and vice et versa
        // /!\ must not be used outside editor code
        [SerializeField]
        private Vector3 editorAdvancedModeBlendDistancePositive = Vector3.zero;
        [SerializeField]
        private Vector3 editorAdvancedModeBlendDistanceNegative = Vector3.zero;
        [SerializeField]
        private float editorSimplifiedModeBlendDistance = 0f;
        [SerializeField]
        private Vector3 editorAdvancedModeBlendNormalDistancePositive = Vector3.zero;
        [SerializeField]
        private Vector3 editorAdvancedModeBlendNormalDistanceNegative = Vector3.zero;
        [SerializeField]
        private float editorSimplifiedModeBlendNormalDistance = 0f;
        [SerializeField]
        private bool editorAdvancedModeEnabled = false;

        public ReflectionProxyVolumeComponent proxyVolumeComponent;

        public Vector3 boxBlendCenterOffset { get { return (blendDistanceNegative - blendDistancePositive) * 0.5f; } }
        public Vector3 boxBlendSizeOffset { get { return -(blendDistancePositive + blendDistanceNegative); } }
        public Vector3 boxBlendNormalCenterOffset { get { return (blendNormalDistanceNegative - blendNormalDistancePositive) * 0.5f; } }
        public Vector3 boxBlendNormalSizeOffset { get { return -(blendNormalDistancePositive + blendNormalDistanceNegative); } }


        public float sphereBlendRadiusOffset { get { return -blendDistancePositive.x; } }
        public float sphereBlendNormalRadiusOffset { get { return -blendNormalDistancePositive.x; } }

        public void CopyTo(HDAdditionalReflectionData data)
        {
            data.proxyVolumeComponent = proxyVolumeComponent;

            data.influenceShape = influenceShape;
            data.multiplier = multiplier;
            data.weight = weight;
            data.influenceSphereRadius = influenceSphereRadius;
            data.sphereReprojectionVolumeRadius = sphereReprojectionVolumeRadius;
            data.useSeparateProjectionVolume = useSeparateProjectionVolume;
            data.boxReprojectionVolumeSize = boxReprojectionVolumeSize;
            data.boxReprojectionVolumeCenter = boxReprojectionVolumeCenter;
            data.maxSearchDistance = maxSearchDistance;
            data.blendDistancePositive = blendDistancePositive;
            data.blendDistanceNegative = blendDistanceNegative;
            data.blendNormalDistancePositive = blendNormalDistancePositive;
            data.blendNormalDistanceNegative = blendNormalDistanceNegative;
            data.boxSideFadePositive = boxSideFadePositive;
            data.boxSideFadeNegative = boxSideFadeNegative;

            data.editorAdvancedModeBlendDistancePositive = editorAdvancedModeBlendDistancePositive;
            data.editorAdvancedModeBlendDistanceNegative = editorAdvancedModeBlendDistanceNegative;
            data.editorSimplifiedModeBlendDistance = editorSimplifiedModeBlendDistance;
            data.editorAdvancedModeBlendNormalDistancePositive = editorAdvancedModeBlendNormalDistancePositive;
            data.editorAdvancedModeBlendNormalDistanceNegative = editorAdvancedModeBlendNormalDistanceNegative;
            data.editorSimplifiedModeBlendNormalDistance = editorSimplifiedModeBlendNormalDistance;
            data.editorAdvancedModeEnabled = editorAdvancedModeEnabled;
        }
    }
}
