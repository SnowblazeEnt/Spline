using UnityEngine;

namespace SnowblazeEntertainment.Tools.Spline
{
	public class BezierCurveContainer : MonoBehaviour
	{
		public BezierCurve curve;
		
		public Vector3 GetPoint(float t) 
		{
			return transform.TransformPoint(Bezier.GetPoint(ref curve, t));
		}
		
		public Vector3 GetVelocity(float t) 
		{
			return transform.TransformPoint(Bezier.GetFirstDerivative(ref curve, t)) - transform.position;
		}
		
		public Vector3 GetDirection(float t) 
		{
			return GetVelocity(t).normalized;
		}
		
		public void Reset() 
		{
			curve = new BezierCurve
			(
				new Vector3(1.0f, 0.0f, 0.0f),
				new Vector3(2.0f, 0.0f, 0.0f),
				new Vector3(3.0f, 0.0f, 0.0f),
				new Vector3(4.0f, 0.0f, 0.0f)
			);
		}
	}
}