using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class SerializedCaptureSettings
    {
        public SerializedProperty root;

        public SerializedProperty clearColorMode;
        public SerializedProperty backgroundColorHDR;
        public SerializedProperty clearDepth;

        public SerializedProperty renderingPath;
        public SerializedProperty volumeLayerMask;
        public SerializedProperty volumeAnchorOverride;

        public SerializedProperty aperture;
        public SerializedProperty shutterSpeed;
        public SerializedProperty iso;

        public SerializedProperty shadowDistance;

        public SerializedProperty farClipPlane;
        public SerializedProperty nearClipPlane;
        public SerializedProperty fieldOfview;
        public SerializedProperty useOcclusionCulling;
        public SerializedProperty cullingMask;

        private SerializedProperty overrides;
        //public bool overridesCubeResolution
        //{
        //    get { return (overrides.intValue & (int)CaptureSettingsOverrides.CubeResolution) > 0; }
        //    set
        //    {
        //        if (value)
        //            overrides.intValue |= (int)CaptureSettingsOverrides.CubeResolution;
        //        else
        //            overrides.intValue &= ~(int)CaptureSettingsOverrides.CubeResolution;
        //    }
        //}
        //public bool overridesPlanarResolution
        //{
        //    get { return (overrides.intValue & (int)CaptureSettingsOverrides.PlanarResolution) > 0; }
        //    set
        //    {
        //        if (value)
        //            overrides.intValue |= (int)CaptureSettingsOverrides.PlanarResolution;
        //        else
        //            overrides.intValue &= ~(int)CaptureSettingsOverrides.PlanarResolution;
        //    }
        //}
        public bool overridesClearColorMode
        {
            get { return (overrides.intValue & (int)CaptureSettingsOverrides.ClearColorMode) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)CaptureSettingsOverrides.ClearColorMode;
                else
                    overrides.intValue &= ~(int)CaptureSettingsOverrides.ClearColorMode;
            }
        }
        public bool overridesBackgroundColorHDR
        {
            get { return (overrides.intValue & (int)CaptureSettingsOverrides.BackgroundColorHDR) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)CaptureSettingsOverrides.BackgroundColorHDR;
                else
                    overrides.intValue &= ~(int)CaptureSettingsOverrides.BackgroundColorHDR;
            }
        }
        public bool overridesClearDepth
        {
            get { return (overrides.intValue & (int)CaptureSettingsOverrides.ClearDepth) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)CaptureSettingsOverrides.ClearDepth;
                else
                    overrides.intValue &= ~(int)CaptureSettingsOverrides.ClearDepth;
            }
        }
        public bool overridesRenderingPath
        {
            get { return (overrides.intValue & (int)CaptureSettingsOverrides.RenderingPath) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)CaptureSettingsOverrides.RenderingPath;
                else
                    overrides.intValue &= ~(int)CaptureSettingsOverrides.RenderingPath;
            }
        }
        public bool overridesVolumeLayerMask
        {
            get { return (overrides.intValue & (int)CaptureSettingsOverrides.VolumeLayerMask) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)CaptureSettingsOverrides.VolumeLayerMask;
                else
                    overrides.intValue &= ~(int)CaptureSettingsOverrides.VolumeLayerMask;
            }
        }
        public bool overridesVolumeAnchorOverride
        {
            get { return (overrides.intValue & (int)CaptureSettingsOverrides.VolumeAnchorOverride) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)CaptureSettingsOverrides.VolumeAnchorOverride;
                else
                    overrides.intValue &= ~(int)CaptureSettingsOverrides.VolumeAnchorOverride;
            }
        }
        public bool overridesAperture
        {
            get { return (overrides.intValue & (int)CaptureSettingsOverrides.Aperture) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)CaptureSettingsOverrides.Aperture;
                else
                    overrides.intValue &= ~(int)CaptureSettingsOverrides.Aperture;
            }
        }
        public bool overridesShutterSpeed
        {
            get { return (overrides.intValue & (int)CaptureSettingsOverrides.ShutterSpeed) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)CaptureSettingsOverrides.ShutterSpeed;
                else
                    overrides.intValue &= ~(int)CaptureSettingsOverrides.ShutterSpeed;
            }
        }
        public bool overridesIso
        {
            get { return (overrides.intValue & (int)CaptureSettingsOverrides.Iso) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)CaptureSettingsOverrides.Iso;
                else
                    overrides.intValue &= ~(int)CaptureSettingsOverrides.Iso;
            }
        }
        public bool overridesNearClip
        {
            get { return (overrides.intValue & (int)CaptureSettingsOverrides.NearClip) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)CaptureSettingsOverrides.NearClip;
                else
                    overrides.intValue &= ~(int)CaptureSettingsOverrides.NearClip;
            }
        }
        public bool overridesFarClip
        {
            get { return (overrides.intValue & (int)CaptureSettingsOverrides.FarClip) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)CaptureSettingsOverrides.FarClip;
                else
                    overrides.intValue &= ~(int)CaptureSettingsOverrides.FarClip;
            }
        }
        public bool overridesFieldOfview
        {
            get { return (overrides.intValue & (int)CaptureSettingsOverrides.FieldOfview) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)CaptureSettingsOverrides.FieldOfview;
                else
                    overrides.intValue &= ~(int)CaptureSettingsOverrides.FieldOfview;
            }
        }
        public bool overridesCullingMask
        {
            get { return (overrides.intValue & (int)CaptureSettingsOverrides.CullingMask) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)CaptureSettingsOverrides.CullingMask;
                else
                    overrides.intValue &= ~(int)CaptureSettingsOverrides.CullingMask;
            }
        }
        public bool overridesUseOcclusionCulling
        {
            get { return (overrides.intValue & (int)CaptureSettingsOverrides.UseOcclusionCulling) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)CaptureSettingsOverrides.UseOcclusionCulling;
                else
                    overrides.intValue &= ~(int)CaptureSettingsOverrides.UseOcclusionCulling;
            }
        }
        public bool overridesShadowDistance
        {
            get { return (overrides.intValue & (int)CaptureSettingsOverrides.ShadowDistance) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)CaptureSettingsOverrides.ShadowDistance;
                else
                    overrides.intValue &= ~(int)CaptureSettingsOverrides.ShadowDistance;
            }
        }

        public SerializedCaptureSettings(SerializedProperty root)
        {
            this.root = root;

            clearColorMode = root.Find((CaptureSettings d) => d.clearColorMode);
            backgroundColorHDR = root.Find((CaptureSettings d) => d.backgroundColorHDR);
            clearDepth = root.Find((CaptureSettings d) => d.clearDepth);

            renderingPath = root.Find((CaptureSettings d) => d.renderingPath);
            volumeLayerMask = root.Find((CaptureSettings d) => d.volumeLayerMask);
            volumeAnchorOverride = root.Find((CaptureSettings d) => d.volumeAnchorOverride);

            aperture = root.Find((CaptureSettings d) => d.aperture);
            shutterSpeed = root.Find((CaptureSettings d) => d.shutterSpeed);
            iso = root.Find((CaptureSettings d) => d.iso);

            shadowDistance = root.Find((CaptureSettings d) => d.shadowDistance);

            farClipPlane = root.Find((CaptureSettings d) => d.farClipPlane);
            nearClipPlane = root.Find((CaptureSettings d) => d.nearClipPlane);
            fieldOfview = root.Find((CaptureSettings d) => d.fieldOfview);
            useOcclusionCulling = root.Find((CaptureSettings d) => d.useOcclusionCulling);
            cullingMask = root.Find((CaptureSettings d) => d.cullingMask);

            overrides = root.Find((CaptureSettings d) => d.overrides);
        }
    }
}
