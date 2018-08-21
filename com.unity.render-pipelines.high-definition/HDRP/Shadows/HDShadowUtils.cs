using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public static class HDShadowUtils
    {
        // Some helper functions to encapsulate ShadowUtilities light extraction functions

        public static void ExtractSpotLightProjectionMatrix(VisibleLight visibleLight, float guardAngle, out Matrix4x4 view, out Matrix4x4 deviceProjection)
        {
            Matrix4x4 proj, vpInverse;
            Vector4 lightDir;
            ShadowSplitData splitData;

            ShadowUtils.ExtractSpotLightMatrix(visibleLight, guardAngle, out view, out proj, out deviceProjection, out vpInverse, out lightDir, out splitData);
        }
        
        public static void ExtractPointLightProjectionMatrix(VisibleLight visibleLight, uint faceIdx, float guardAngle, out Matrix4x4 view, out Matrix4x4 deviceProjection)
        {
            Matrix4x4 proj, vpInverse;
            Vector4 lightDir;
            ShadowSplitData splitData;

            ShadowUtils.ExtractPointLightMatrix(visibleLight, faceIdx, guardAngle, out view, out proj, out deviceProjection, out vpInverse, out lightDir, out splitData);
        }
        
        static float GetFilterWidthInTexels(LightType lightType)
        {
            // TODO: get current shadow algorithm in use for the lightType and return the filter textel size

            // Just return the PCF5x5 value by default
            return 5f;
        }

        // TODO: see if we really need VisibleLight here
        public static void ExtractLightMatrices(LightTypeExtent lightTypeExtent, LightType lightType, VisibleLight visibleLight, Vector2 viewportSize, float normalBiasMax, out Matrix4x4 view, out Matrix4x4 deviceProjection)
        {
            float guardAngle;

            deviceProjection = Matrix4x4.identity;
            view = Matrix4x4.identity;

            switch (lightTypeExtent)
            {
                case LightTypeExtent.Punctual:
                {
                    switch (lightType)
                    {
                        case LightType.Directional:
                            return ;
                        case LightType.Spot:
                            guardAngle = ShadowUtils.CalcGuardAnglePerspective(visibleLight.light.spotAngle, viewportSize.x,  GetFilterWidthInTexels(lightType), normalBiasMax, 180.0f - visibleLight.light.spotAngle);
                            ExtractSpotLightProjectionMatrix(visibleLight, guardAngle, out view, out deviceProjection);
                            return;
                        case LightType.Point:
                            guardAngle = ShadowUtils.CalcGuardAnglePerspective(90.0f, viewportSize.x, GetFilterWidthInTexels(lightType), normalBiasMax, 79.0f);
                            ExtractPointLightProjectionMatrix(visibleLight, 0, guardAngle, out view, out deviceProjection);
                            return;
                    }
                    return;
                }

                // Shadow not currently supported for area lights
                case LightTypeExtent.Line:
                    return;
                case LightTypeExtent.Rectangle:
                    return;
            }
        }
    }
}