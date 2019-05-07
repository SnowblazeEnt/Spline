using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SnowblazeEntertainment.Tools.Spline
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(BezierSpline))]
	public class BezierSplineInspector : Editor 
	{
		private const int stepsPerCurve = 10;
		private const float directionScale = 0.5f;
		private const float handleSize = 0.04f;
		private const float pickSize = 0.06f;

		private static Color[] modeColors = 
		{
			Color.white,
			Color.yellow,
			Color.cyan
		};

		private BezierSpline spline;
		private Transform handleTransform;
		private Quaternion handleRotation;
		private int selectedIndex = -1;

		private SerializedProperty stepWorldUnitsProp;
		private SerializedProperty roadRadiusProp;
		private SerializedProperty borderRadiusProp;
		private SerializedProperty loopProp;
		private SerializedProperty speedCategoryProp;

		private object[] splines;

		private void OnEnable()
		{
			spline = serializedObject.targetObject as BezierSpline;
			splines = serializedObject.targetObjects;

			stepWorldUnitsProp = serializedObject.FindProperty("stepWorldUnits");
			roadRadiusProp = serializedObject.FindProperty("roadRadius");
			borderRadiusProp = serializedObject.FindProperty("borderRadius");
			loopProp = serializedObject.FindProperty("loop");
			speedCategoryProp = serializedObject.FindProperty("speedCategory");
		}

		public override void OnInspectorGUI() 
		{
			serializedObject.Update();
            EditorGUI.BeginChangeCheck();

			EditorGUILayout.PropertyField(stepWorldUnitsProp);
			EditorGUILayout.PropertyField(roadRadiusProp);
			EditorGUILayout.PropertyField(borderRadiusProp);
			EditorGUILayout.PropertyField(loopProp);
			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(speedCategoryProp);

			if (selectedIndex >= 0 && selectedIndex < spline.ControlPointCount) 
			{
				DrawSelectedPointInspector();
			}

			if (GUILayout.Button("Add Curve")) 
			{
				Undo.RecordObject(spline, "Add Curve");
				spline.AddCurve();
				EditorUtility.SetDirty(spline);
			}

			serializedObject.ApplyModifiedProperties();
			
			if (EditorGUI.EndChangeCheck())
            {
                spline.GenerateLUT();
            }
		}

		private void DrawSelectedPointInspector() 
		{
			EditorGUILayout.Space();
			GUILayout.Label("Selected Point");

			if(EditorGUILayout.PropertyField(serializedObject.FindProperty("points").GetArrayElementAtIndex(selectedIndex), new GUIContent("Position")))
			{
				spline.SetControlPoint(selectedIndex, serializedObject.FindProperty("points").GetArrayElementAtIndex(selectedIndex).vector3Value);
			}

			if(EditorGUILayout.PropertyField(serializedObject.FindProperty("modes").GetArrayElementAtIndex((selectedIndex + 1) / 3), new GUIContent("Mode")))
			{
				spline.SetControlPointMode(selectedIndex, (BezierControlPointMode)serializedObject.FindProperty("points").GetArrayElementAtIndex((selectedIndex + 1) / 3).enumValueIndex);
			}
		}

		private void OnSceneGUI() 
		{
			//spline = target as BezierSpline;			
			foreach (var spline in splines)
			{
				this.spline = spline as BezierSpline;
				handleTransform = this.spline.transform;
				handleRotation = UnityEditor.Tools.pivotRotation == PivotRotation.Local ? handleTransform.rotation : Quaternion.identity;
			
				Vector3 p0 = ShowPoint(0);
				for (int i = 1; i < this.spline.ControlPointCount; i += 3) 
				{
					Vector3 p1 = ShowPoint(i);
					Vector3 p2 = ShowPoint(i + 1);
					Vector3 p3 = ShowPoint(i + 2);
					
					Handles.color = Color.gray;
					Handles.DrawLine(p0, p1);
					Handles.DrawLine(p2, p3);
					
					Handles.DrawBezier(p0, p3, p1, p2, Color.blue, null, 2f);
					p0 = p3;
				}
				ShowDirections();

				SimulateMovement();
			}
		}

		private void ShowDirections() 
		{
			Handles.color = Color.green;
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
					spline.GenerateLUT();
				}
			}
			return point;
		}

		private void SimulateMovement()
		{
			if(spline.GetSpeed() == 0.0f) return;

			spline.Progress = Time.realtimeSinceStartup % spline.GetSpeed() / spline.GetSpeed();

			Handles.DrawWireCube(spline.SamplePoint(spline.Progress) + Vector3.one * 0.1f,  Vector3.one / 4);
		}
	}
}