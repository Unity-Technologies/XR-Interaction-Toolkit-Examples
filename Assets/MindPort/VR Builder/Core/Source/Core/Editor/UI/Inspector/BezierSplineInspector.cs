using UnityEditor;
using UnityEngine;
using VRBuilder.Core.Utils;
using VRBuilder.Editor.UI;

namespace VRBuilder.Editor.Core.UI
{
    /// <summary>
    /// Editor for <see cref="BezierSpline"/>.
    /// </summary>
	[CustomEditor(typeof(BezierSpline))]
	public class BezierSplineInspector : UnityEditor.Editor
	{
		private const int stepsPerCurve = 10;
		private const float directionScale = 0.5f;
		private const float handleSize = 0.04f;
		private const float pickSize = 0.06f;

        private static Color[] modeColors = {
			new Color32(231,64,255, 255),
			new Color32(255,238,74, 255),
			new Color32(120,241,200, 255),
	};

		private static Color lineColor = Color.white;
		private static Color handleColor = new Color32(191, 191, 191, 255);
		private static Color tangentColor = new Color32(102,150,255, 255);

		private BezierSpline spline;
		private Transform handleTransform;
		private Quaternion handleRotation;
		private int selectedIndex = -1;

		public override void OnInspectorGUI()
		{
			spline = target as BezierSpline;
			EditorGUI.BeginChangeCheck();
			bool loop = EditorGUILayout.Toggle("Loop", spline.Loop);            
			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(spline, "Toggle Loop");
				EditorUtility.SetDirty(spline);
				spline.Loop = loop;
			}

            spline.LinearVelocity = EditorGUILayout.Toggle("Approximate Linear Velocity", spline.LinearVelocity);

            if(spline.LinearVelocity)
            {
                spline.CurveResolution = EditorGUILayout.IntField("Granularity of Approximation", Mathf.Clamp(spline.CurveResolution, 2, spline.CurveResolution));
            }

            GUILayout.Label("Control Points", BuilderEditorStyles.Header);

            for(int point = 0; point < spline.ControlPointCount; ++point)
            {
                if (point == spline.ControlPointCount - 1 && loop)
                {
                    continue;
                }

                DrawInspectorForPoint(point);
            }

			if (GUILayout.Button("Add Curve"))
			{
				Undo.RecordObject(spline, "Add Curve");
				spline.AddCurve();
				EditorUtility.SetDirty(spline);
			}

            EditorGUI.BeginDisabledGroup(spline.ControlPointCount <= 4);
            if(GUILayout.Button("Remove Curve"))
            {
                Undo.RecordObject(spline, "Remove Curve");
                spline.RemoveCurve();
                EditorUtility.SetDirty(spline);
            }
            EditorGUI.EndDisabledGroup();
		}

		private void DrawInspectorForPoint(int index)
		{
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontStyle = FontStyle.Bold;

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.stretchWidth = false;

            string label;

            if(index == 0 || index % 3 == 2)
            {
                GUILayout.Label($"Point {(index + 1) / 3}", labelStyle);
            }

            if (index % 3 == 0)
            {
                label = "Anchor";
            }
            else if(index % 3 == 1)
            {
                label = "Handle Out";
            }
            else
            {
                label = "Handle In";
            }

            if (index == 0 || index % 3 == 2)
            {
                EditorGUI.BeginChangeCheck();
                BezierControlPointMode mode = (BezierControlPointMode)EditorGUILayout.EnumPopup("Mode", spline.GetControlPointMode(index));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(spline, "Change Point Mode");
                    spline.SetControlPointMode(index, mode);
                    EditorUtility.SetDirty(spline);
                }
            }

            EditorGUILayout.BeginHorizontal();            
            EditorGUI.BeginDisabledGroup(index == selectedIndex);
            if(GUILayout.Button(label, buttonStyle))
            {
                selectedIndex = index;
                Repaint();
                SceneView.RepaintAll();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace();

			EditorGUI.BeginChangeCheck();
			Vector3 point = EditorGUILayout.Vector3Field(string.Empty, spline.GetControlPoint(index));
			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(spline, "Move Point");
				EditorUtility.SetDirty(spline);
				spline.SetControlPoint(index, point);
			}
            EditorGUILayout.EndHorizontal();
		}

		private void OnSceneGUI()
		{
			spline = target as BezierSpline;
			handleTransform = spline.transform;
			handleRotation = Tools.pivotRotation == PivotRotation.Local ?
				handleTransform.rotation : Quaternion.identity;

			Vector3 p0 = ShowPoint(0);
			for (int i = 1; i < spline.ControlPointCount; i += 3)
			{
				Vector3 p1 = ShowPoint(i);
				Vector3 p2 = ShowPoint(i + 1);
				Vector3 p3 = ShowPoint(i + 2);

				Handles.color = handleColor;
				Handles.DrawLine(p0, p1);
				Handles.DrawLine(p2, p3);

				Handles.DrawBezier(p0, p3, p1, p2, lineColor, null, 2f);
				p0 = p3;
			}
			ShowDirections();
		}

		private void ShowDirections()
		{
			Handles.color = tangentColor;
			Vector3 point = spline.GetPoint(0f);
			Handles.DrawLine(point, point + spline.GetDirection(0f) * directionScale);
			int steps = stepsPerCurve * spline.CurveCount;
			for (int i = 1; i <= steps; i++)
			{
				point = spline.GetPoint(i / (float)steps);
				Handles.DrawLine(point, point + spline.GetDirection(i / (float)steps) * directionScale);
			}
		}

		private Vector3 ShowPoint(int index)
		{
			Vector3 point = handleTransform.TransformPoint(spline.GetControlPoint(index));
			float size = HandleUtility.GetHandleSize(point);
			if (index == 0)
			{
				size *= 2f;
			}
			Handles.color = modeColors[(int)spline.GetControlPointMode(index)];
			if (Handles.Button(point, handleRotation, size * handleSize, size * pickSize, Handles.DotHandleCap))
			{
				selectedIndex = index;
				Repaint();
			}
			if (selectedIndex == index)
			{
				EditorGUI.BeginChangeCheck();
				point = Handles.DoPositionHandle(point, handleRotation);
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(spline, "Move Point");
					EditorUtility.SetDirty(spline);
					spline.SetControlPoint(index, handleTransform.InverseTransformPoint(point));
				}
			}
			return point;
		}
	}
}
