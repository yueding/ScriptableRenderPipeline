using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Flags]
    public enum CaptureSettingsOverrides
    {
        //CubeResolution = 1 << 0,
        //PlanarResolution = 1 << 1,
        ClearColorMode = 1 << 2,
        BackgroundColorHDR = 1 << 3,
        ClearDepth = 1 << 4,
        RenderingPath = 1 << 5,
        VolumeLayerMask = 1 << 6,
        VolumeAnchorOverride = 1 << 7,
        Aperture = 1 << 8,
        ShutterSpeed = 1 << 9,
        Iso = 1 << 10,
        NearClip = 1 << 11,
        FarClip = 1 << 12,
        FieldOfview = 1 << 13,
        CullingMask = 1 << 14,
        UseOcclusionCulling = 1 << 15,
        ShadowDistance = 1 << 16,
    }

    public class CaptureSettings
    {
        public CaptureSettingsOverrides overrides;

        public HDAdditionalCameraData.ClearColorMode clearColorMode = HDAdditionalCameraData.ClearColorMode.Sky;
        public Color backgroundColorHDR = new Color(0.025f, 0.07f, 0.19f, 0.0f);
        public bool clearDepth = true;

        public HDAdditionalCameraData.RenderingPath renderingPath = HDAdditionalCameraData.RenderingPath.Default;
        public LayerMask volumeLayerMask = -1; //= 0xFFFFFFFF which is c++ default
        public Transform volumeAnchorOverride;

        public float aperture = 8f;
        public float shutterSpeed = 1f / 200f;
        public float iso = 400f;

        public float shadowDistance = 100.0f;

        public float farClipPlane = 1000f;
        public float nearClipPlane = 0.3f;
        public float fieldOfview = 60.0f;
        public bool useOcclusionCulling = true;
        public int cullingMask = -1; //= 0xFFFFFFFF which is c++ default
    }
}
