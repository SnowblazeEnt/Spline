using System.Collections.Generic;
using System.Linq;
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

		public static SplitBezierCurveData Split(ref BezierCurve curve, float t)
		{
			List<Vector3> hull = GetHull(ref curve, t);
			BezierCurve[] splitCurves = new BezierCurve[] 
			{
				new BezierCurve(hull[0], hull[4], hull[7], hull[9]),
				new BezierCurve(hull[9], hull[8], hull[6], hull[3]),
			};
			return new SplitBezierCurveData() { curves = splitCurves };
		}

		public static SplitBezierCurveData Split(ref BezierCurve curve, float t1, float t2, bool midCurve)
		{
			List<Vector3> hull = GetHull(ref curve, t1);
			BezierCurve[] splitCurvesFirst = new BezierCurve[] 
			{
				new BezierCurve(hull[0], hull[4], hull[7], hull[9]),
				new BezierCurve(hull[9], hull[8], hull[6], hull[3]),
			};
			hull = GetHull(ref splitCurvesFirst[1], t2);
			BezierCurve[] splitCurvesSecond;
			if(midCurve)
			{
				splitCurvesSecond = new BezierCurve[] 
				{
					new BezierCurve(hull[0], hull[4], hull[7], hull[9]),
				};
			}
			else
			{
				splitCurvesSecond = new BezierCurve[] 
				{
					splitCurvesFirst[0],
					new BezierCurve(hull[9], hull[8], hull[6], hull[3]),
				};
			}

			return new SplitBezierCurveData() { curves = splitCurvesSecond };
		}

		public static List<List<float>> GetExtrema(ref BezierCurve curve)
		{
			List<List<float>> result = new List<List<float>>();

			IEnumerable<float> p;
			List<List<Vector3>> derivedPoints = Derive(ref curve);

			//3 for 3 dimensions (x,y,z)
			for (int i = 0; i < 3; i++)
			{
				IEnumerable<float> tmp;
				p = derivedPoints[0].Select(x => x[i]);
				tmp = DerivedRoots(p);
				p = derivedPoints[1].Select(x => x[i]);
				tmp = tmp.Concat(DerivedRoots(p));
				result.Add(tmp.Where(x => x >= 0.0f && x <= 1.0f).ToList());
				result[i].Sort((a,b) => a.CompareTo(b));
			}

			return result;
		}

		public static List<List<Vector3>> Derive(ref BezierCurve curve)
		{
			List<List<Vector3>> derivedPoints = new List<List<Vector3>>();

			List<Vector3> points = curve.points.ToList();

			for (int d = points.Count, c = d - 1; d > 1; d--, c--)
			{
				List<Vector3> list = new List<Vector3>();
				for (int j = 0; j < c; j++)
				{
					list.Add(c * (points[j + 1] - points[j]));
				}
				derivedPoints.Add(list);
				points = list;
				//d = points.Count;
			}

			return derivedPoints;
		}

		public static IEnumerable<float> DerivedRoots(IEnumerable<float> p)
		{
			//Quadratic roots
			if(p.Count() == 3)
			{
				float a = p.ElementAt(0);
				float b = p.ElementAt(1);
				float c = p.ElementAt(2);
				float d = a - 2 * b + c;

				if(d != 0)
				{
					float m1 = -Mathf.Sqrt(b * b - a * c);
					float m2 = -a + b;
					float v1 = -(m1 + m2) / d;
					float v2 = -(-m1 + m2) / d;

					return new List<float>() { v1, v2 };
				}
				else if(b != c && d == 0)
				{
					return new List<float>() { (2 * b - c) / (2 * (b -c )) };
				}
			}

			//Linear roots
			if(p.Count() == 2)
			{
				float a = p.ElementAt(0);
				float b = p.ElementAt(1);

				if(a != b)
				{
					return new List<float>() { a / (a - b) };
				}
			}

			return new List<float>();
		}

		public static Bounds GetBoundingBox(ref BezierCurve curve)
		{
			List<List<float>> extrema = GetExtrema(ref curve);
			List<Vector2> minMax = new List<Vector2>();
			for (int i = 0; i < 3; i++)
			{
				minMax.Add(GetMinMax(ref curve, i, extrema[i]));
			}

			Vector3 center;
			center.x = (minMax[0].x + minMax[0].y) / 2;
			center.y = (minMax[1].x + minMax[1].y) / 2;
			center.z = (minMax[2].x + minMax[2].y) / 2;

			Vector3 size;
			size.x = minMax[0].y - minMax[0].x;
			size.y = minMax[1].y - minMax[1].x;
			size.z = minMax[2].y - minMax[2].x;

			return new Bounds(center, size);
		}

		public static Vector2 GetMinMax(ref BezierCurve curve, int d, List<float> extrema)
		{
			float max = int.MinValue;
			float min = int.MaxValue;
			float t;
			Vector3 c;

			if(!extrema.Any(x => x == 0))
			{
				extrema.Insert(0, 0);
			}

			if(!extrema.Any(x => x == 1))
			{
				extrema.Add(1);
			}

			for (int i = 0; i < extrema.Count; i++)
			{
				t = extrema[i];
				c = GetPoint(ref curve, t);
				if(c[d] < min)
				{
					min = c[d];
				}
				if(c[d] > max)
				{
					max = c[d];
				}
			}

			return new Vector2(min, max);
		}
	}
}