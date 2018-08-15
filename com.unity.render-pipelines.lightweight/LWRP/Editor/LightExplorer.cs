using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;
using System.Linq;

namespace UnityEditor
{
	[LightingExplorerExtensionAttribute(typeof(LightweightPipelineAsset))]
	public class LightExplorer : DefaultLightingExplorerExtension
	{
		private static class Styles
        {
	        public static readonly GUIContent On = EditorGUIUtility.TrTextContent("On");
	        public static readonly GUIContent Name = EditorGUIUtility.TrTextContent("Name");
	        public static readonly GUIContent Type = EditorGUIUtility.TrTextContent("Type");
	        public static readonly GUIContent Mode = EditorGUIUtility.TrTextContent("Mode");
	        public static readonly GUIContent Color = EditorGUIUtility.TrTextContent("Color");
	        public static readonly GUIContent Intensity = EditorGUIUtility.TrTextContent("Intensity");
	        public static readonly GUIContent IndirectMultiplier = EditorGUIUtility.TrTextContent("Indirect Multiplier");
	        public static readonly GUIContent ShadowType = EditorGUIUtility.TrTextContent("Shadow Type");
	        public static readonly GUIContent Shape = EditorGUIUtility.TrTextContent("Shape");
	        
	        public static readonly GUIContent HDR = EditorGUIUtility.TrTextContent("HDR");
	        public static readonly GUIContent ShadowDistance = EditorGUIUtility.TrTextContent("Shadow Distance");
	        public static readonly GUIContent NearPlane = EditorGUIUtility.TrTextContent("Near Plane");
	        public static readonly GUIContent FarPlane = EditorGUIUtility.TrTextContent("Far Plane");
	        public static readonly GUIContent Resolution = EditorGUIUtility.TrTextContent("Resolution");
	        
	        
	        public static readonly GUIContent[] LightShapeTitles = { EditorGUIUtility.TrTextContent("Rectangle"), EditorGUIUtility.TrTextContent("Disc") };
	        public static readonly int[] LightShapeValues = { (int)LightType.Rectangle, (int)LightType.Disc };
	        public static readonly GUIContent[] LightmapBakeTypeTitles = { EditorGUIUtility.TrTextContent("Realtime"), EditorGUIUtility.TrTextContent("Mixed"), EditorGUIUtility.TrTextContent("Baked") };
	        public static readonly int[] LightmapBakeTypeValues = { (int)LightmapBakeType.Realtime, (int)LightmapBakeType.Mixed, (int)LightmapBakeType.Baked };
	        
	        public static readonly GUIContent[] ReflectionProbeModeTitles = { EditorGUIUtility.TrTextContent("Baked"), EditorGUIUtility.TrTextContent("Realtime"), EditorGUIUtility.TrTextContent("Custom") };
	        public static readonly int[] ReflectionProbeModeValues = { (int)ReflectionProbeMode.Baked, (int)ReflectionProbeMode.Realtime, (int)ReflectionProbeMode.Custom };
	        public static readonly GUIContent[] ReflectionProbeSizeTitles = { EditorGUIUtility.TrTextContent("16"),
																				EditorGUIUtility.TrTextContent("32"),
																				EditorGUIUtility.TrTextContent("64"),
																				EditorGUIUtility.TrTextContent("128"),
																				EditorGUIUtility.TrTextContent("256"),
																				EditorGUIUtility.TrTextContent("512"),
																				EditorGUIUtility.TrTextContent("1024"),
																				EditorGUIUtility.TrTextContent("2048") };
	        public static readonly int[] ReflectionProbeSizeValues = { 16, 32, 64, 128, 256, 512, 1024, 2048 };
        }
		
		public override LightingExplorerTab[] GetContentTabs()
		{
			return new[]
			{
				new LightingExplorerTab("Light Table", GetLights, GetLightColumns),
				new LightingExplorerTab("Reflection Probes", GetReflectionProbes, GetReflectionProbeColumns),
				new LightingExplorerTab("Light Probes", GetLightProbes, GetLightProbeColumns),
				new LightingExplorerTab("Static Emissives", GetEmissives, GetEmissivesColumns)
			};
		}

		protected override LightingExplorerTableColumn[] GetLightColumns()
		{
			return new[]
			{
				new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, Styles.On, "m_Enabled", 25), // 0: Enabled
				new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Name, Styles.Name, null, 200), // 1: Name
				new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, Styles.Type, "m_Type", 120), // 2: Type
				new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, Styles.Mode, "m_Lightmapping", 70, (r, prop, dep) =>
				{
					bool areaLight = dep.Length > 1 && (dep[0].enumValueIndex == (int)LightType.Area || dep[0].enumValueIndex == (int)LightType.Disc);

					using (new EditorGUI.DisabledScope(areaLight))
					{
						EditorGUI.BeginChangeCheck();
						int newval = EditorGUI.IntPopup(r, prop.intValue, Styles.LightmapBakeTypeTitles, Styles.LightmapBakeTypeValues);
						if (EditorGUI.EndChangeCheck())
						{
							prop.intValue = newval;
						}
					}
				}, null, null, new int[] { 2 }), // 3: Mode
				new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Color, Styles.Color, "m_Color", 70), // 4: Color
				new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, Styles.Intensity, "m_Intensity", 60), // 5: Intensity
				new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, Styles.IndirectMultiplier, "m_BounceIntensity", 110), // 6: Indirect Multiplier
				new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, Styles.ShadowType, "m_Shadows.m_Type", 100, (r, prop, dep) =>
				{
					bool areaLight = dep.Length > 1 && (dep[0].enumValueIndex == (int)LightType.Rectangle || dep[0].enumValueIndex == (int)LightType.Disc);

					if (areaLight)
					{
						EditorGUI.BeginProperty(r, GUIContent.none, prop);
						EditorGUI.BeginChangeCheck();
						bool shadows = EditorGUI.Toggle(r, prop.intValue != (int)LightShadows.None);

						if (EditorGUI.EndChangeCheck())
						{
							prop.intValue = shadows ? (int)LightShadows.Soft : (int)LightShadows.None;
						}
						EditorGUI.EndProperty();
					}
					else
					{
						EditorGUI.PropertyField(r, prop, GUIContent.none);
					}
				}, null, null, new int[] { 2 }),     // 7: Shadow Type
			};
		}
		
		protected override LightingExplorerTableColumn[] GetReflectionProbeColumns()
		{
			return new[]
			{
				new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, Styles.On, "m_Enabled", 25), // 0: Enabled
				new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Name, Styles.Name, null, 200),  // 1: Name
				new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Int, Styles.Mode, "m_Mode", 70, (r, prop, dep) =>
				{
					EditorGUI.IntPopup(r, prop, Styles.ReflectionProbeModeTitles, Styles.ReflectionProbeModeValues, GUIContent.none);
				}),     // 2: Mode
				new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, Styles.HDR, "m_HDR", 35),  // 3: HDR
				new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, Styles.Resolution, "m_Resolution", 100, (r, prop, dep) =>
				{
					EditorGUI.IntPopup(r, prop, Styles.ReflectionProbeSizeTitles, Styles.ReflectionProbeSizeValues, GUIContent.none);
				}), // 4: Probe Resolution
				new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, Styles.ShadowDistance, "m_ShadowDistance", 100), // 5: Shadow Distance
				new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, Styles.NearPlane, "m_NearClip", 70), // 6: Near Plane
				new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, Styles.FarPlane, "m_FarClip", 70), // 7: Far Plane
			};
		}
		
		protected virtual LightingExplorerTableColumn[] GetLightProbeColumns()
		{
			return new[]
			{
				new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, Styles.On, "m_Enabled", 25), // 0: Enabled
				new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Name, Styles.Name, null, 200), // 1: Name
			};
		}
	}
}