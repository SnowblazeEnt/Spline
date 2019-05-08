using System.Collections.Generic;
using UnityEngine;

namespace SnowblazeEntertainment.Tools.Spline
{
	public static class Bezier 
	{
		// public static Vector3 GetPoint(ref BezierCurve curve, float t) 
		// {
		// 	t = Mathf.Clamp01(t);
		// 	float oneMinusT = 1f - t;
		// 	return
		// 		oneMinusT * oneMinusT * curve[0] +
		// 		2f * oneMinusT * t * curve[1] +
		// 		t * t * curve[2];
		// }

		// public static Vector3 GetFirstDerivative(ref BezierCurve curve, float t) 
		// {
		// 	return
		// 		2f * (1f - t) * (curve[1] - curve[0]) +
		// 		2f * t * (curve[2] - curve[1]);
		// }

		public static Vector3 GetPoint(ref BezierCurve curve, float t) 
		{
			t = Mathf.Clamp01(t);
			float OneMinusT = 1f - t;
			return
				OneMinusT * OneMinusT * OneMinusT * curve.points[0] +
				3f * OneMinusT * OneMinusT * t * curve.points[1] +
				3f * OneMinusT * t * t * curve.points[2] +
				t * t * t * curve.points[3];
		}

		public static Vector3 GetFirstDerivative(ref BezierCurve curve, float t) 
		{
			t = Mathf.Clamp01(t);
			float oneMinusT = 1f - t;
			return
				3f * oneMinusT * oneMinusT * (curve.points[1] - curve.points[0]) +
				6f * oneMinusT * t * (curve.points[2] - curve.points[1]) +
				3f * t * t * (curve.points[3] - curve.points[2]);
		}

		public static List<Vector3> GetHull(ref BezierCurve curve, float t)
		{
			List<Vector3> hull = new List<Vector3>();
			hull.Add(curve.points[0]);
			hull.Add(curve.points[1]);
			hull.Add(curve.points[2]);
			hull.Add(curve.points[3]);

			hull.Add(Vector3.Lerp(hull[0], hull[1], t));
			hull.Add(Vector3.Lerp(hull[1], hull[2], t));
			hull.Add(Vector3.Lerp(hull[2], hull[3], t));

			hull.Add(Vector3.Lerp(hull[4], hull[5], t));
			hull.Add(Vector3.Lerp(hull[5], hull[6], t));

			hull.Add(Vector3.Lerp(hull[7], hull[8], t));

			return hull;
		}

		public static void Split(ref BezierCurve curve, float t)
		{
			GetHull(ref curve, t);

		}
	}
}