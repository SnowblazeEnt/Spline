using UnityEditor;
using UnityEngine;

namespace SnowblazeEntertainment.Tools.Spline
{
	[CustomEditor(typeof(BezierCurveContainer))]
	public class BezierCurveContainerInspector : Editor 
	{
		private const int lineSteps = 10;
		private const float directionScale = 0.5f;

		private BezierCurveContainer curveContainer;
		private Transform handleTransform;
		private Quaternion handleRotation;

		private void OnSceneGUI() 
		{
			curveContainer = target as BezierCurveContainer;
			handleTransform = curveContainer.transform;
			handleRotation = UnityEditor.Tools.pivotRotation == PivotRotation.Local ? handleTransform.rotation : Quaternion.identity;
			
			Vector3 p0 = ShowPoint(0);
			Vector3 p1 = ShowPoint(1);
			Vector3 p2 = ShowPoint(2);
			Vector3 p3 = ShowPoint(3);
			
			Handles.color = Color.gray;
			Handles.DrawLine(p0, p1);
			Handles.DrawLine(p2, p3);
			
			ShowDirections();
			Handles.DrawBezier(p0, p3, p1, p2, Color.white, null, 2f);
		}

		private void ShowDirections() 
		{
			Handles.color = Color.green;
			Vector3 point = curveContainer.GetPoint(0f);
			Handles.DrawLine(point, point + curveContainer.GetDirection(0f) * directionScale);
			for (int i = 1; i <= lineSteps; i++) 
			{
				point = curveContainer.GetPoint(i / (float)lineSteps);
				Handles.DrawLine(point, point + curveContainer.GetDirection(i / (float)lineSteps) * directionScale);
			}
		}

		private Vector3 ShowPoint(int index) 
		{
			Vector3 point = handleTransform.TransformPoint(curveContainer.curve.points[index]);
			EditorGUI.BeginChangeCheck();
			point = Handles.DoPositionHandle(point, handleRotation);
			if (EditorGUI.EndChangeCheck()) 
			{
				Undo.RecordObject(curveContainer, "Move Point");
				EditorUtility.SetDirty(curveContainer);
				curveContainer.curve.points[index] = handleTransform.InverseTransformPoint(point);
			}
			return point;
		}
	}
}