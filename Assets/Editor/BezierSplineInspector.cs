using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SnowblazeEntertainment.Tools.Spline
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(BezierSplineContainer))]
	public class BezierSplineContainerInspector : Editor 
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

		private BezierSplineContainer splineContainer;
		private Transform handleTransform;
		private Quaternion handleRotation;
		private int selectedIndex = -1;

		private SerializedProperty stepWorldUnitsProp;
		private SerializedProperty roadRadiusProp;
		private SerializedProperty borderRadiusProp;
		private SerializedProperty loopProp;
		private SerializedProperty speedCategoryProp;

		private object[] splineContainers;

		private void OnEnable()
		{
			splineContainer = serializedObject.targetObject as BezierSplineContainer;
			splineContainers = serializedObject.targetObjects;

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

			if (selectedIndex >= 0 && selectedIndex < splineContainer.ControlPointCount) 
			{
				DrawSelectedPointInspector();
			}

			if (GUILayout.Button("Add Curve")) 
			{
				Undo.RecordObject(splineContainer, "Add Curve");
				splineContainer.AddCurve();
				EditorUtility.SetDirty(splineContainer);
			}

			serializedObject.ApplyModifiedProperties();
			
			if (EditorGUI.EndChangeCheck())
            {
				//TODO: find intersections
				//MeshCombiner.GenerateRoad(intersectionPoints, targets.Select(x => x as BezierSpline).ToList());
                splineContainer.GenerateLUT();
            }
		}

		private void DrawSelectedPointInspector() 
		{
			EditorGUILayout.Space();
			GUILayout.Label("Selected Point");

			SerializedProperty splineProp = serializedObject.FindProperty("spline");
			SerializedProperty curvesProp = splineProp.FindPropertyRelative("curves").GetArrayElementAtIndex((selectedIndex - 1) / 3);
			SerializedProperty pointsProp = curvesProp.FindPropertyRelative("points").GetArrayElementAtIndex(selectedIndex - ((selectedIndex - 1) / 3) * 3);
			if(EditorGUILayout.PropertyField(pointsProp, new GUIContent("Position")))
			{
				splineContainer.SetControlPoint(selectedIndex, pointsProp.vector3Value);
			}

			SerializedProperty modesProp = splineProp.FindPropertyRelative("modes").GetArrayElementAtIndex((selectedIndex + 1) / 3);
			if(EditorGUILayout.PropertyField(modesProp, new GUIContent("Mode")))
			{
				splineContainer.SetControlPointMode(selectedIndex, (BezierControlPointMode)modesProp.enumValueIndex);
			}
		}

		private void OnSceneGUI() 
		{
			//spline = target as BezierSpline;			
			foreach (var splineContainer in splineContainers)
			{
				this.splineContainer = splineContainer as BezierSplineContainer;
				handleTransform = this.splineContainer.transform;
				handleRotation = UnityEditor.Tools.pivotRotation == PivotRotation.Local ? handleTransform.rotation : Quaternion.identity;
			
				Vector3 p0 = ShowPoint(0);
				for (int i = 1; i < this.splineContainer.ControlPointCount; i += 3) 
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
			Vector3 point = splineContainer.GetPoint(0f);
			Handles.DrawLine(point, point + splineContainer.GetDirection(0f) * directionScale);
			int steps = stepsPerCurve * splineContainer.CurveCount;
			for (int i = 1; i <= steps; i++) 
			{
				point = splineContainer.GetPoint(i / (float)steps);
				Handles.DrawLine(point, point + splineContainer.GetDirection(i / (float)steps) * directionScale);
			}
		}

		private Vector3 ShowPoint(int index) 
		{
			Vector3 point = handleTransform.TransformPoint(splineContainer.GetControlPoint(index));
			float size = HandleUtility.GetHandleSize(point);
			if (index == 0) 
			{
				size *= 2f;
			}
			Handles.color = modeColors[(int)splineContainer.GetControlPointMode(index)];
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
					Undo.RecordObject(splineContainer, "Move Point");
					EditorUtility.SetDirty(splineContainer);
					splineContainer.SetControlPoint(index, handleTransform.InverseTransformPoint(point));
					splineContainer.GenerateLUT();
				}
			}
			return point;
		}

		private void SimulateMovement()
		{
			if(splineContainer.GetSpeed() == 0.0f) return;

			splineContainer.Progress = Time.realtimeSinceStartup % splineContainer.GetSpeed() / splineContainer.GetSpeed();

			Handles.DrawWireCube(splineContainer.SamplePoint(splineContainer.Progress) + Vector3.one * 0.1f,  Vector3.one / 4);
		}
	}
}