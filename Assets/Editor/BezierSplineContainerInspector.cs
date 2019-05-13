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
		private SerializedProperty t1Prop;
		private SerializedProperty t2Prop;
		private SerializedProperty widthCurveProp;

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
			t1Prop = serializedObject.FindProperty("t1");
			t2Prop = serializedObject.FindProperty("t2");
			widthCurveProp = serializedObject.FindProperty("widthCurve");
		}

		public override void OnInspectorGUI() 
		{
			splineContainer = target as BezierSplineContainer;

			EditorGUI.BeginChangeCheck();
			float stepWorldUnits = EditorGUILayout.FloatField("Step world units", stepWorldUnitsProp.floatValue);
			if (EditorGUI.EndChangeCheck()) 
			{
				Undo.RecordObject(splineContainer, "StepWorldUnits");
				EditorUtility.SetDirty(splineContainer);
				splineContainer.StepWorldUnits = stepWorldUnits;
			}

			EditorGUI.BeginChangeCheck();
			float roadRadius = EditorGUILayout.FloatField("Road radius", roadRadiusProp.floatValue);
			if (EditorGUI.EndChangeCheck()) 
			{
				Undo.RecordObject(splineContainer, "RoadRadius");
				EditorUtility.SetDirty(splineContainer);
				splineContainer.RoadRadius = roadRadius;
				splineContainer.GenerateLUT();
			}

			EditorGUI.BeginChangeCheck();
			float borderRadius = EditorGUILayout.FloatField("Border radius", borderRadiusProp.floatValue);
			if (EditorGUI.EndChangeCheck()) 
			{
				Undo.RecordObject(splineContainer, "BorderRadius");
				EditorUtility.SetDirty(splineContainer);
				splineContainer.BorderRadius = borderRadius;
				splineContainer.GenerateLUT();
			}

			EditorGUI.BeginChangeCheck();
			bool loop = EditorGUILayout.Toggle("Loop", splineContainer.Loop);
			if (EditorGUI.EndChangeCheck()) 
			{
				Undo.RecordObject(splineContainer, "Toggle Loop");
				EditorUtility.SetDirty(splineContainer);
				splineContainer.Loop = loop;
				splineContainer.GenerateLUT();
			}

			EditorGUI.BeginChangeCheck();
			float t1 = t1Prop.floatValue;
			float t2 = t2Prop.floatValue;
			EditorGUILayout.MinMaxSlider(ref t1, ref t2, 0.0f, 1.0f);
			if(EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(splineContainer, "T1/T2");
				EditorUtility.SetDirty(splineContainer);
				splineContainer.T1 = t1;
				splineContainer.T2 = t2;
			}

			EditorGUI.BeginChangeCheck();
			AnimationCurve curve = EditorGUILayout.CurveField("Road width", widthCurveProp.animationCurveValue, Color.red, new Rect(0,0,1,2));
			if(EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(splineContainer, "WidthCurve");
				EditorUtility.SetDirty(splineContainer);
				splineContainer.WidthCurve = curve;
				splineContainer.GenerateLUT();
			}

			// EditorGUI.BeginChangeCheck();
			// float t1 = EditorGUILayout.FloatField("T1", t1Prop.floatValue);
			// if (EditorGUI.EndChangeCheck()) 
			// {
			// 	Undo.RecordObject(splineContainer, "T1");
			// 	EditorUtility.SetDirty(splineContainer);
			// 	splineContainer.T1 = t1;
			// }

			// EditorGUI.BeginChangeCheck();
			// float t2 = EditorGUILayout.FloatField("T2", t2Prop.floatValue);
			// if (EditorGUI.EndChangeCheck()) 
			// {
			// 	Undo.RecordObject(splineContainer, "T2");
			// 	EditorUtility.SetDirty(splineContainer);
			// 	splineContainer.T2 = t2;
			// }

			EditorGUI.BeginChangeCheck();
			SpeedCategory speedCategory = (SpeedCategory)EditorGUILayout.EnumPopup("Speed category", (SpeedCategory)speedCategoryProp.enumValueIndex);
			if (EditorGUI.EndChangeCheck()) 
			{
				Undo.RecordObject(splineContainer, "SpeedCategory");
				EditorUtility.SetDirty(splineContainer);
				splineContainer.SpeedCategory = speedCategory;
			}

			if (selectedIndex >= 0 && selectedIndex < splineContainer.ControlPointCount) {
				DrawSelectedPointInspector();
			}
			if (GUILayout.Button("Add Curve")) {
				Undo.RecordObject(splineContainer, "Add Curve");
				splineContainer.AddCurve();
				splineContainer.GenerateLUT();
				EditorUtility.SetDirty(splineContainer);
			}

			// serializedObject.Update();
            // EditorGUI.BeginChangeCheck();

			// EditorGUILayout.PropertyField(stepWorldUnitsProp);
			// EditorGUILayout.PropertyField(roadRadiusProp);
			// EditorGUILayout.PropertyField(borderRadiusProp);
			// EditorGUILayout.PropertyField(loopProp);
			// EditorGUILayout.Space();
			// EditorGUILayout.PropertyField(speedCategoryProp);

			// if (selectedIndex >= 0 && selectedIndex < splineContainer.ControlPointCount) 
			// {
			// 	DrawSelectedPointInspector();
			// }

			// if (GUILayout.Button("Add Curve")) 
			// {
			// 	Undo.RecordObject(splineContainer, "Add Curve");
			// 	splineContainer.AddCurve();
			// 	EditorUtility.SetDirty(splineContainer);
			// }

			// serializedObject.ApplyModifiedProperties();
			
			// if (EditorGUI.EndChangeCheck())
            // {
			// 	//TODO: find intersections
			// 	//MeshCombiner.GenerateRoad(intersectionPoints, targets.Select(x => x as BezierSpline).ToList());
            //     splineContainer.GenerateLUT();
            // }
		}

		private void DrawSelectedPointInspector() 
		{
			EditorGUILayout.Space();
			GUILayout.Label("Selected Point");
			EditorGUI.BeginChangeCheck();
			Vector3 point = EditorGUILayout.Vector3Field("Position", splineContainer.GetControlPoint(selectedIndex));
			if (EditorGUI.EndChangeCheck()) {
				Undo.RecordObject(splineContainer, "Move Point");
				EditorUtility.SetDirty(splineContainer);
				splineContainer.SetControlPoint(selectedIndex, point);
				splineContainer.GenerateLUT();
			}
			EditorGUI.BeginChangeCheck();
			BezierControlPointMode mode = (BezierControlPointMode)EditorGUILayout.EnumPopup("Mode", splineContainer.GetControlPointMode(selectedIndex));
			if (EditorGUI.EndChangeCheck()) {
				Undo.RecordObject(splineContainer, "Change Point Mode");
				splineContainer.SetControlPointMode(selectedIndex, mode);
				splineContainer.GenerateLUT();
				EditorUtility.SetDirty(splineContainer);
			}

			// EditorGUILayout.Space();
			// GUILayout.Label("Selected Point");

			// SerializedProperty splineProp = serializedObject.FindProperty("spline");
			// SerializedProperty curvesProp = splineProp.FindPropertyRelative("curves").GetArrayElementAtIndex((selectedIndex - 1) / 3);
			// SerializedProperty pointsProp = curvesProp.FindPropertyRelative("points").GetArrayElementAtIndex(selectedIndex - ((selectedIndex - 1) / 3) * 3);

            // EditorGUI.BeginChangeCheck();
			// EditorGUILayout.PropertyField(pointsProp, new GUIContent("Position"));
			// if(EditorGUI.EndChangeCheck())
			// {
			// 	splineContainer.SetControlPoint(selectedIndex, pointsProp.vector3Value);
			// }

			// SerializedProperty modesProp = splineProp.FindPropertyRelative("modes").GetArrayElementAtIndex((selectedIndex + 1) / 3);
			
            // EditorGUI.BeginChangeCheck();
			// EditorGUILayout.PropertyField(modesProp, new GUIContent("Mode"));
			// if(EditorGUI.EndChangeCheck())
			// {
			// 	splineContainer.SetControlPointMode(selectedIndex, (BezierControlPointMode)modesProp.enumValueIndex);
			// }
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

				ShowStartAndEnd();

				SimulateMovement();

				// foreach(var curve in this.splineContainer.Spline.curves)
				// {
				// 	BezierCurve cu = curve;
				// 	Bounds bound = Bezier.GetBoundingBox(ref cu);
				// 	Handles.DrawWireCube(bound.center + this.splineContainer.transform.localPosition, bound.size);
				// }
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

		private void ShowStartAndEnd()
		{
			Handles.color = Color.yellow;
			Handles.DrawWireCube(splineContainer.SamplePoint(0.0f) + Vector3.one * 0.1f,  Vector3.one / 4);
			Handles.color = Color.red;
			Handles.DrawWireCube(splineContainer.SamplePoint(1.0f) + Vector3.one * 0.1f,  Vector3.one / 4);
			Handles.color = Color.green;
		}

		private void SimulateMovement()
		{
			if(splineContainer.GetSpeed() == 0.0f) return;

			splineContainer.Progress = Time.realtimeSinceStartup % splineContainer.GetSpeed() / splineContainer.GetSpeed();

			Handles.DrawWireCube(splineContainer.SamplePoint(splineContainer.Progress) + Vector3.one * 0.1f,  Vector3.one / 4);
		}
	}
}