using UnityEditor;
using UnityEngine;

using ColorMapEditorType = OVRPassthroughLayer.ColorMapEditorType;

[CustomEditor(typeof(OVRPassthroughLayer))]
public class OVRPassthroughLayerEditor : Editor
{
	public override void OnInspectorGUI()
	{
		OVRPassthroughLayer layer = (OVRPassthroughLayer)target;

		layer.projectionSurfaceType = (OVRPassthroughLayer.ProjectionSurfaceType)EditorGUILayout.EnumPopup(
			new GUIContent("Projection Surface", "The type of projection surface for this Passthrough layer"),
			layer.projectionSurfaceType);

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Compositing", EditorStyles.boldLabel);
		layer.overlayType = (OVROverlay.OverlayType)EditorGUILayout.EnumPopup(new GUIContent("Placement", "Whether this overlay should layer behind the scene or in front of it"), layer.overlayType);
		layer.compositionDepth = EditorGUILayout.IntField(new GUIContent("Composition Depth", "Depth value used to sort layers in the scene, smaller value appears in front"), layer.compositionDepth);

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Style", EditorStyles.boldLabel);

		layer.textureOpacity = EditorGUILayout.Slider("Opacity", layer.textureOpacity, 0, 1);

		EditorGUILayout.Space();

		layer.edgeRenderingEnabled = EditorGUILayout.Toggle(
			new GUIContent("Edge Rendering", "Highlight salient edges in the camera images in a specific color"),
			layer.edgeRenderingEnabled);
		layer.edgeColor = EditorGUILayout.ColorField("Edge Color", layer.edgeColor);

		EditorGUILayout.Space();

		System.Func<System.Enum, bool> hideCustomColorMapOption = option => (ColorMapEditorType)option != ColorMapEditorType.Custom;
		layer.colorMapEditorType = (ColorMapEditorType)EditorGUILayout.EnumPopup(
			new GUIContent("Color Map"),
			layer.colorMapEditorType,
			hideCustomColorMapOption,
			false);

		if (layer.colorMapEditorType == ColorMapEditorType.Controls)
		{
			layer.colorMapEditorContrast = EditorGUILayout.Slider("Contrast", layer.colorMapEditorContrast, -1, 1);
			layer.colorMapEditorBrightness = EditorGUILayout.Slider("Brightness", layer.colorMapEditorBrightness, -1, 1);
			layer.colorMapEditorPosterize = EditorGUILayout.Slider("Posterize", layer.colorMapEditorPosterize, 0, 1);
			layer.colorMapEditorGradient = EditorGUILayout.GradientField("Colorize", layer.colorMapEditorGradient);
		}

		if (GUI.changed)
		{
			EditorUtility.SetDirty(layer);
		}
	}
}
