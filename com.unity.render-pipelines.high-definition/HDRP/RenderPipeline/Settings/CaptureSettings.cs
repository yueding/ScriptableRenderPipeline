using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class CaptureSettings
    {
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
