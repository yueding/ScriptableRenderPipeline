using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering
{
    public static class CoreLightEditorUtilities
    {
        // Don't use Handles.Disc as it break the highlight of the gizmo axis, use our own draw disc function instead for gizmo
        public static void DrawWireDisc(Quaternion q, Vector3 position, Vector3 axis, float radius)
        {
            Matrix4x4 rotation = Matrix4x4.TRS(Vector3.zero, q, Vector3.one);

            //Handles.color = Color.white;
            //Gizmos.color = Color.white;
            float theta = 0.0f;
            float x = radius * Mathf.Cos(theta);
            float y = radius * Mathf.Sin(theta);
            Vector3 pos = rotation * new Vector3(x, y, 0);
            pos += position;
            Vector3 newPos = pos;
            Vector3 lastPos = pos;
            for (theta = 0.1f; theta < 2.0f * Mathf.PI; theta += 0.1f)
            {
                x = radius * Mathf.Cos(theta);
                y = radius * Mathf.Sin(theta);

                newPos = rotation * new Vector3(x, y, 0);
                newPos += position;
                Handles.DrawLine(pos, newPos);
                //Gizmos.DrawLine(pos, newPos);
                pos = newPos;
            }
            Handles.DrawLine(pos, lastPos);
            //Gizmos.DrawLine(pos, lastPos);
        }

        public static void DrawSpotlightGizmo(Light spotlight, float innerSpotPercent, bool selected)
        {
            Color drawColor = Color.yellow;
            Color drawColorBehind = new Color(Color.yellow.r, Color.yellow.g, Color.yellow.b,  0.2f);
            Color drawColorInner = new Color32(255, 180, 100, 150);

            var flatRadiusAtRange = spotlight.range * Mathf.Tan(spotlight.spotAngle * Mathf.Deg2Rad * 0.5f);

            var vectorLineUp = Vector3.Normalize(spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.up * flatRadiusAtRange - spotlight.gameObject.transform.position);
            var vectorLineDown = Vector3.Normalize(spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.up * -flatRadiusAtRange - spotlight.gameObject.transform.position);
            var vectorLineRight = Vector3.Normalize(spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.right * flatRadiusAtRange - spotlight.gameObject.transform.position);
            var vectorLineLeft = Vector3.Normalize(spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.right * -flatRadiusAtRange - spotlight.gameObject.transform.position);

            var rangeDiscDistance = Mathf.Cos(Mathf.Deg2Rad * spotlight.spotAngle / 2) * spotlight.range;
            var rangeDiscRadius = spotlight.range * Mathf.Sin(spotlight.spotAngle * Mathf.Deg2Rad * 0.5f);
            var nearDiscDistance = Mathf.Cos(Mathf.Deg2Rad * spotlight.spotAngle / 2) * spotlight.shadowNearPlane;
            var nearDiscRadius = spotlight.shadowNearPlane * Mathf.Sin(spotlight.spotAngle * Mathf.Deg2Rad * 0.5f);

            // Saving the default colors
            var defColor = Handles.color;
            var defZTest = Handles.zTest;

            // Setting the color to yellow and doing the depth test here
            Handles.color = drawColor;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

            //Draw Range disc
            DrawWireDisc(spotlight.gameObject.transform.rotation, spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * rangeDiscDistance, spotlight.gameObject.transform.forward, rangeDiscRadius);

            //Draw Lines
            Handles.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineUp * spotlight.range);
            Handles.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineDown * spotlight.range);
            Handles.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineRight * spotlight.range);
            Handles.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineLeft * spotlight.range);
            Handles.color = drawColorInner;
            Handles.DrawWireArc(spotlight.gameObject.transform.position, spotlight.gameObject.transform.right, vectorLineUp, spotlight.spotAngle, spotlight.range);
            Handles.DrawWireArc(spotlight.gameObject.transform.position, spotlight.gameObject.transform.up, vectorLineLeft, spotlight.spotAngle, spotlight.range);

            //Draw Near Plane Disc
            if (spotlight.shadows != LightShadows.None)
                DrawWireDisc(spotlight.gameObject.transform.rotation, spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * nearDiscDistance, spotlight.gameObject.transform.forward, nearDiscRadius);

            //Inner Cone
            Handles.color = drawColorInner;
            DrawInnerCone(spotlight, innerSpotPercent);

            // Setting the color to the behind color and doing the depth test here
            Handles.color = drawColorBehind;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;

            DrawWireDisc(spotlight.gameObject.transform.rotation, spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * rangeDiscDistance, spotlight.gameObject.transform.forward, rangeDiscRadius);

            Handles.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineUp * spotlight.range);
            Handles.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineDown * spotlight.range);
            Handles.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineRight * spotlight.range);
            Handles.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineLeft * spotlight.range);
            Handles.DrawWireArc(spotlight.gameObject.transform.position, spotlight.gameObject.transform.right, vectorLineUp, spotlight.spotAngle, spotlight.range);
            Handles.DrawWireArc(spotlight.gameObject.transform.position, spotlight.gameObject.transform.up, vectorLineLeft, spotlight.spotAngle, spotlight.range);
            //Draw Near Plane Disc
            if (spotlight.shadows != LightShadows.None)
                DrawWireDisc(spotlight.gameObject.transform.rotation, spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * nearDiscDistance, spotlight.gameObject.transform.forward, nearDiscRadius);

            //Inner Cone
            DrawInnerCone(spotlight, innerSpotPercent);

            Handles.color = defColor;
            Handles.zTest = defZTest;
        }

        public static float DrawCenterHandle(Quaternion rotation, Vector3 position, float range)
        {
            Vector3 forward = rotation * Vector3.forward;

            // Range handle at the center of the circle
            bool temp = GUI.changed;
            GUI.changed = false;
            range = SizeSlider(position, forward, range);
            if (GUI.changed)
            {
                range = Mathf.Max(0.0F, range);
            }
            GUI.changed |= temp;
            return range;
        }

        public static Vector2 DrawConeHandles(Quaternion rotation, Vector3 position, Vector2 angleAndRange)
        {
            float spotAngle = angleAndRange.x;
            float range = angleAndRange.y;

            Vector3 forward = rotation * Vector3.forward;
            Vector3 up = rotation * Vector3.up;
            Vector3 right = rotation * Vector3.right;

            spotAngle = SizeSliderSpotAngle(position, forward, right, range, spotAngle);
            spotAngle = SizeSliderSpotAngle(position, forward, -right, range, spotAngle);
            spotAngle = SizeSliderSpotAngle(position, forward, up, range, spotAngle);
            spotAngle = SizeSliderSpotAngle(position, forward, -up, range, spotAngle);

            return new Vector2(spotAngle, range);
        }

        static float SizeSlider(Vector3 p, Vector3 d, float t)
        {
            Vector3 position = p + d * t;
            float size = HandleUtility.GetHandleSize(position);
            bool temp = GUI.changed;
            GUI.changed = false;
            position = Handles.Slider(position, d, size * 0.04f, Handles.DotHandleCap, 0f);
            if (GUI.changed)
                t = Vector3.Dot(position - p, d);

            GUI.changed |= temp;
            return t;
        }

        static int s_SliderSpotAngleId;

        static float SizeSliderSpotAngle(Vector3 position, Vector3 forward, Vector3 axis, float range, float spotAngle)
        {
            if (Math.Abs(spotAngle) <= 0.05f && GUIUtility.hotControl != s_SliderSpotAngleId)
                return spotAngle;
            var angledForward = Quaternion.AngleAxis(Mathf.Max(spotAngle, 0.05f) * 0.5f, axis) * forward;
            var centerToLeftOnSphere = (angledForward * range + position) - (position + forward * range);
            bool temp = GUI.changed;
            GUI.changed = false;
            var newMagnitude = Mathf.Max(0f, SizeSlider(position + forward * range, centerToLeftOnSphere.normalized, centerToLeftOnSphere.magnitude));
            if (GUI.changed)
            {
                s_SliderSpotAngleId = GUIUtility.hotControl;
                centerToLeftOnSphere = centerToLeftOnSphere.normalized * newMagnitude;
                angledForward = (centerToLeftOnSphere + (position + forward * range) - position).normalized;
                spotAngle = Mathf.Clamp(Mathf.Acos(Vector3.Dot(forward, angledForward)) * Mathf.Rad2Deg * 2, 0f, 179f);
                if (spotAngle <= 0.05f || float.IsNaN(spotAngle))
                    spotAngle = 0f;
            }
            GUI.changed |= temp;
            return spotAngle;
        }

        // innerSpotPercent - 0 to 1 value (percentage 0 - 100%)
        public static void DrawInnerCone(Light spotlight, float innerSpotPercent)
        {
            var flatRadiusAtRange = spotlight.range * Mathf.Tan(spotlight.spotAngle * innerSpotPercent * Mathf.Deg2Rad * 0.5f);

            var vectorLineUp = Vector3.Normalize(spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.up * flatRadiusAtRange - spotlight.gameObject.transform.position);
            var vectorLineDown = Vector3.Normalize(spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.up * -flatRadiusAtRange - spotlight.gameObject.transform.position);
            var vectorLineRight = Vector3.Normalize(spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.right * flatRadiusAtRange - spotlight.gameObject.transform.position);
            var vectorLineLeft = Vector3.Normalize(spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * spotlight.range + spotlight.gameObject.transform.right * -flatRadiusAtRange - spotlight.gameObject.transform.position);

            //Draw Lines
            Handles.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineUp * spotlight.range);
            Handles.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineDown * spotlight.range);
            Handles.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineRight * spotlight.range);
            Handles.DrawLine(spotlight.gameObject.transform.position, spotlight.gameObject.transform.position + vectorLineLeft * spotlight.range);

            var innerAngle = spotlight.spotAngle * innerSpotPercent;
            if (innerAngle > 0)
            {
                var innerDiscDistance = Mathf.Cos(Mathf.Deg2Rad * innerAngle * 0.5f) * spotlight.range;
                var innerDiscRadius = spotlight.range * Mathf.Sin(innerAngle * Mathf.Deg2Rad * 0.5f);
                //Draw Range disc
                DrawWireDisc(spotlight.gameObject.transform.rotation, spotlight.gameObject.transform.position + spotlight.gameObject.transform.forward * innerDiscDistance, spotlight.gameObject.transform.forward, innerDiscRadius);
            }
        }

        public static void DrawArealightGizmo(Light arealight)
        {
            var RectangleSize = new Vector3(arealight.areaSize.x, arealight.areaSize.y, 0);
            // Remove scale for light, not take into account
            var localToWorldMatrix = Matrix4x4.TRS(arealight.transform.position, arealight.transform.rotation, Vector3.one);
            Gizmos.matrix = localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, RectangleSize);
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawWireSphere(arealight.transform.position, arealight.range);
        }

        public static void DrawPointlightGizmo(Light pointlight, bool selected)
        {
            if (pointlight.shadows != LightShadows.None && selected) Gizmos.DrawWireSphere(pointlight.transform.position, pointlight.shadowNearPlane);
            Gizmos.DrawWireSphere(pointlight.transform.position, pointlight.range);
        }

        // Same as Gizmo.DrawFrustum except that when aspect is below one, fov represent fovX instead of fovY
        // Use to match our light frustum pyramid behavior
        public static void DrawLightPyramidFrustum(Vector3 center, float fov, float maxRange, float minRange, float aspect)
        {
            fov = Mathf.Deg2Rad * fov * 0.5f;
            float tanfov = Mathf.Tan(fov);
            Vector3 farEnd = new Vector3(0, 0, maxRange);
            Vector3 endSizeX;
            Vector3 endSizeY;

            if (aspect >= 1.0f)
            {
                endSizeX = new Vector3(maxRange * tanfov * aspect, 0, 0);
                endSizeY = new Vector3(0, maxRange * tanfov, 0);
            }
            else
            {
                endSizeX = new Vector3(maxRange * tanfov, 0, 0);
                endSizeY = new Vector3(0, maxRange * tanfov / aspect, 0);
            }

            Vector3 s1, s2, s3, s4;
            Vector3 e1 = farEnd + endSizeX + endSizeY;
            Vector3 e2 = farEnd - endSizeX + endSizeY;
            Vector3 e3 = farEnd - endSizeX - endSizeY;
            Vector3 e4 = farEnd + endSizeX - endSizeY;
            if (minRange <= 0.0f)
            {
                s1 = s2 = s3 = s4 = center;
            }
            else
            {
                Vector3 startSizeX;
                Vector3 startSizeY;
                if (aspect >= 1.0f)
                {
                    startSizeX = new Vector3(minRange * tanfov * aspect, 0, 0);
                    startSizeY = new Vector3(0, minRange * tanfov, 0);
                }
                else
                {
                    startSizeY = new Vector3(minRange * tanfov / aspect, 0, 0);
                    startSizeX = new Vector3(0, minRange * tanfov, 0);
                }
                Vector3 startPoint = center;
                s1 =    startPoint + startSizeX + startSizeY;
                s2 =    startPoint - startSizeX + startSizeY;
                s3 =    startPoint - startSizeX - startSizeY;
                s4 =    startPoint + startSizeX - startSizeY;
                Gizmos.DrawLine(s1, s2);
                Gizmos.DrawLine(s2, s3);
                Gizmos.DrawLine(s3, s4);
                Gizmos.DrawLine(s4, s1);
            }

            Gizmos.DrawLine(e1, e2);
            Gizmos.DrawLine(e2, e3);
            Gizmos.DrawLine(e3, e4);
            Gizmos.DrawLine(e4, e1);

            Gizmos.DrawLine(s1, e1);
            Gizmos.DrawLine(s2, e2);
            Gizmos.DrawLine(s3, e3);
            Gizmos.DrawLine(s4, e4);
        }

        public static void DrawLightOrthoFrustum(Vector3 center, float width, float height, float maxRange, float minRange)
        {
            Vector3 farEnd = new Vector3(0, 0, maxRange);
            Vector3 endSizeX = new Vector3(width, 0, 0);
            Vector3 endSizeY = new Vector3(0, height, 0);

            Vector3 s1, s2, s3, s4;
            Vector3 e1 = farEnd + endSizeX + endSizeY;
            Vector3 e2 = farEnd - endSizeX + endSizeY;
            Vector3 e3 = farEnd - endSizeX - endSizeY;
            Vector3 e4 = farEnd + endSizeX - endSizeY;
            if (minRange <= 0.0f)
            {
                s1 = s2 = s3 = s4 = center;
            }
            else
            {
                Vector3 startSizeX = new Vector3(width, 0, 0);
                Vector3 startSizeY = new Vector3(0, height, 0);

                Vector3 startPoint = center;
                s1 =    startPoint + startSizeX + startSizeY;
                s2 =    startPoint - startSizeX + startSizeY;
                s3 =    startPoint - startSizeX - startSizeY;
                s4 =    startPoint + startSizeX - startSizeY;
                Gizmos.DrawLine(s1, s2);
                Gizmos.DrawLine(s2, s3);
                Gizmos.DrawLine(s3, s4);
                Gizmos.DrawLine(s4, s1);
            }

            Gizmos.DrawLine(e1, e2);
            Gizmos.DrawLine(e2, e3);
            Gizmos.DrawLine(e3, e4);
            Gizmos.DrawLine(e4, e1);

            Gizmos.DrawLine(s1, e1);
            Gizmos.DrawLine(s2, e2);
            Gizmos.DrawLine(s3, e3);
            Gizmos.DrawLine(s4, e4);
        }

        public static void DrawDirectionalLightGizmo(Light directionalLight)
        {
            var gizmoSize = 0.2f;
            DrawWireDisc(directionalLight.transform.rotation, directionalLight.transform.position, directionalLight.gameObject.transform.forward, gizmoSize);
            Gizmos.DrawLine(directionalLight.transform.position, directionalLight.transform.position + directionalLight.transform.forward);
            Gizmos.DrawLine(directionalLight.transform.position + directionalLight.transform.up * gizmoSize, directionalLight.transform.position + directionalLight.transform.up * gizmoSize + directionalLight.transform.forward);
            Gizmos.DrawLine(directionalLight.transform.position + directionalLight.transform.up * -gizmoSize, directionalLight.transform.position + directionalLight.transform.up * -gizmoSize + directionalLight.transform.forward);
            Gizmos.DrawLine(directionalLight.transform.position + directionalLight.transform.right * gizmoSize, directionalLight.transform.position + directionalLight.transform.right * gizmoSize + directionalLight.transform.forward);
            Gizmos.DrawLine(directionalLight.transform.position + directionalLight.transform.right * -gizmoSize, directionalLight.transform.position + directionalLight.transform.right * -gizmoSize + directionalLight.transform.forward);
        }
    }
}



//        public static Vector2 DrawSpotligtCenterHandle(Light spotlight)
//        {
//            // Range handle at the center of the circle
//            bool temp = GUI.changed;
//            GUI.changed = false;
//            float actualRange = spotlight.range;
//            Vector3 position = spotlight.transform.position;
//            Vector3 forward = spotlight.transform.rotation * Vector3.forward;
//            float range = spotlight.range;
//            float rangeScale = 1.0f;
//            actualRange = SizeSlider(position, forward, actualRange);
//            if (GUI.changed)
//                range = Mathf.Max(0.0F, actualRange / rangeScale);
//            GUI.changed |= temp;
//
//            return new Vector2(spotlight.spotAngle, range);
//        }
