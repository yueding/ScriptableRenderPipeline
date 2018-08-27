using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // TODO remove every occurrence of ShadowSplitData in function parameters when we'll have scriptable culling
    public static class HDShadowUtils
    {
        static float GetFilterWidthInTexels(LightType lightType)
        {
            // TODO: get current shadow algorithm in use for the lightType and return the filter textel size

            // Just return the PCF5x5 value by default
            return 5f;
        }

        public static void ExtractPunctualLightData(LightType lightType, VisibleLight visibleLight, Vector2 viewportSize, float normalBiasMax, uint faceIndex, out Matrix4x4 view, out Matrix4x4 invViewProjection, out Matrix4x4 projection, out Matrix4x4 deviceProjection, out ShadowSplitData splitData)
        {
            Vector4 lightDir;
            float guardAngle;

            deviceProjection = Matrix4x4.identity;
            view = Matrix4x4.identity;
            projection = Matrix4x4.identity;

            if (lightType == LightType.Spot)
            {
                guardAngle = ShadowUtils.CalcGuardAnglePerspective(visibleLight.light.spotAngle, viewportSize.x, GetFilterWidthInTexels(lightType), normalBiasMax, 180.0f - visibleLight.light.spotAngle);
                ShadowUtils.ExtractSpotLightMatrix(visibleLight, guardAngle, out view, out projection, out deviceProjection, out invViewProjection, out lightDir, out splitData);
            }
            else
            {
                guardAngle = ShadowUtils.CalcGuardAnglePerspective(90.0f, viewportSize.x, GetFilterWidthInTexels(lightType), normalBiasMax, 79.0f);
                ShadowUtils.ExtractPointLightMatrix(visibleLight, faceIndex, guardAngle, out view, out projection, out deviceProjection, out invViewProjection, out lightDir, out splitData);
            }
        }

        public static void ExtractDirectionalLightData(VisibleLight visibleLight, Vector2 viewportSize, uint cascadeIndex, int cascadeCount, float[] cascadeRatios, float nearPlaneOffset, CullResults cullResults, int lightIndex, out Matrix4x4 view, out Matrix4x4 invViewProjection, out Matrix4x4 projection, out Matrix4x4 deviceProjection, out ShadowSplitData splitData)
        {
            Vector4     lightDir;

            ShadowUtils.ExtractDirectionalLightMatrix(visibleLight, cascadeIndex, cascadeCount, cascadeRatios, nearPlaneOffset, (uint)viewportSize.x, (uint)viewportSize.y, out view, out projection, out deviceProjection, out invViewProjection, out lightDir, out splitData, cullResults, lightIndex);
        }

        // Currently area light shadows are not supported
        public static void ExtractAreaLightData(VisibleLight visibleLight, LightTypeExtent lightTypeExtent, out Matrix4x4 view, out Matrix4x4 invViewProjection, out Matrix4x4 projection, out Matrix4x4 deviceProjection, out ShadowSplitData splitData)
        {
            view = Matrix4x4.identity;
            invViewProjection = Matrix4x4.identity;
            deviceProjection = Matrix4x4.identity;
            projection = Matrix4x4.identity;
            splitData = default(ShadowSplitData);
        }
    }
}