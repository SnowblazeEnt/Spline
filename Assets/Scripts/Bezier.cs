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
			//t = Mathf.Clamp01(t);
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
			hull = GetHull(ref splitCurvesFirst[1], (t2 - t1) / (1.0f - t1));
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

			return new SplitBezierCurveData() { curves = splitCurvesSecond, t1 = t1, t2 = t2 };
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
				//result[i].Sort((a,b) => a.CompareTo(b));
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

		public static Bounds GetBoundingBox(BezierCurve curve)
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

		public static bool BoundingBoxesOverlap(Bounds left, Bounds right)
		{
			for (int i = 0; i < 3; i++)
			{
				var l = left.center[i];
				var t = right.center[i];
				var d = (left.size[i] + right.size[i]) / 2.0f;

				if(d == 0.0f) continue;

				if(Mathf.Abs(l - t) >= d)
				{
					return false;
				}
			}

			return true;

			//return left.Intersects(right);
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

		public static float GetAngle(Vector3 p1, Vector3 p2, Vector3 p3)
		{
			float dx1 = p2.x - p1.x;
        	float dy1 = p2.y - p1.y;
        	float dx2 = p3.x - p1.x;
        	float dy2 = p3.y - p1.y;
        	float cross = dx1 * dy2 - dy1 * dx2;
        	float dot = dx1 * dx2 + dy1 * dy2;
      		return Mathf.Atan2(cross, dot);
		}

		public static Vector3 GetNormal(ref BezierCurve curve, float time)
		{
			Vector3 r1 = GetFirstDerivative(ref curve, time);
			Vector3 r2 = GetFirstDerivative(ref curve, time + 0.01f);
			float q1 = Mathf.Sqrt(r1.x * r1.x + r1.y * r1.y + r1.z * r1.z);
			float q2 = Mathf.Sqrt(r2.x * r2.x + r2.y * r2.y + r2.z * r2.z);

			r1.x /= q1;
			r1.y /= q1;
			r1.z /= q1;
			r2.x /= q2;
			r2.y /= q2;
			r2.z /= q2;
			
			Vector3 cross = new Vector3();
			cross.x = r2.y * r1.z - r2.z * r1.y;
			cross.y = r2.z * r1.x - r2.x * r1.z;
			cross.z = r2.x * r1.y - r2.y * r1.x;

			float m = Mathf.Sqrt(cross.x * cross.x + cross.y * cross.y + cross.z * cross.z);
			cross.x /= m;
			cross.y /= m;
			cross.z /= m;
			
			float[] rotationMatrix = new float[]
			{
				cross.x * cross.x,
				cross.x * cross.y - cross.z,
				cross.x * cross.z + cross.y,
				cross.x * cross.y + cross.z,
				cross.y * cross.y,
				cross.y * cross.z - cross.x,
				cross.x * cross.z - cross.y,
				cross.y * cross.z + cross.x,
				cross.z * cross.z
			};
			
			Vector3 normal = new Vector3();
			normal.x = rotationMatrix[0] * r1.x + rotationMatrix[1] * r1.y + rotationMatrix[2] * r1.z;
			normal.y = rotationMatrix[3] * r1.x + rotationMatrix[4] * r1.y + rotationMatrix[5] * r1.z;
			normal.z = rotationMatrix[6] * r1.x + rotationMatrix[7] * r1.y + rotationMatrix[8] * r1.z;

			return normal;
		}

		public static bool IsSimple(ref BezierCurve curve)
		{
			float angle1 = GetAngle(curve.points[0], curve.points[3], curve.points[1]);
			float angle2 = GetAngle(curve.points[0], curve.points[3], curve.points[2]);
			if((angle1 > 0 && angle2 < 0) || (angle1 < 0 && angle2 > 0)) return false;

			Vector3 normal1 = GetNormal(ref curve, 0.0f);
			Vector3 normal2 = GetNormal(ref curve, 1.0f);

      		float s = normal1.x * normal2.x + normal1.y * normal2.y + normal1.z * normal2.z;
			
			float angle = Mathf.Abs(Mathf.Acos(s));
			
      		return angle < Mathf.PI / 3.0f;
		}

		public static List<BezierCurve> Reduce(ref BezierCurve curve)
		{
			float t2 = 0.0f;
			float step = 0.01f;
			List<SplitBezierCurveData> pass1 = new List<SplitBezierCurveData>();
			List<BezierCurve> pass2 = new List<BezierCurve>();

      		// first pass: split on extrema
			var extrema = GetExtrema(ref curve).SelectMany(x => x).ToList();
      		List<float> extremaSorted = extrema.ToList();
			extremaSorted.Sort((a,b) => a.CompareTo(b));
			//extremaSorted = extremaSorted.Where((x, id) => extrema.IndexOf(x) == id).ToList();
			
			if (extremaSorted.IndexOf(0.0f) == -1) 
			{
				extremaSorted.Insert(0, 0.0f);
			}
			if (extremaSorted.IndexOf(1.0f) == -1) 
			{
				extremaSorted.Add(1.0f);
			}

			float t1 = extremaSorted[0];

			for (int i = 1; i < extremaSorted.Count; i++) 
			{
				t2 = extremaSorted[i];
				SplitBezierCurveData split = Split(ref curve, t1, t2, true);
				pass1.Add(split);
				t1 = t2;
			}

			return pass1.SelectMany(x => x.curves).ToList();

			// second pass: further reduce these segments to simple segments
			foreach (var split in pass1)
			{
				t1 = 0.0f;
				t2 = 0.0f;
				while (t2 <= 1.0f) 
				{
					for (t2 = t1 + step; t2 <= 1 + step; t2 += step) 
					{
						SplitBezierCurveData splitData = Split(ref split.curves[0], t1, t2, true);
						if (!IsSimple(ref splitData.curves[0])) 
						{
							t2 -= step;
							if (Mathf.Abs(t1 - t2) < step) 
							{
								// we can never form a reduction
								return new List<BezierCurve>();
							}
							splitData = Split(ref split.curves[0], t1, t2, true);
							splitData.t1 = Map(t1, 0.0f, 1.0f, splitData.t1, splitData.t2);
							splitData.t2 = Map(t2, 0.0f, 1.0f, splitData.t1, splitData.t2);
							pass2.Add(splitData.curves[0]);
							t1 = t2;
							break;
						}
					}
				}
				if(t1 < 1.0f)
				{
					SplitBezierCurveData splitData = Split(ref split.curves[0], t1, 1.0f, true);
					splitData.t1 = Map(t1, 0.0f, 1.0f, splitData.t1, splitData.t2);
					splitData.t2 = split.t2;
					pass2.Add(splitData.curves[0]);
				}
			}

			return pass2;
		}

		public static List<Vector3> SelfIntersects(ref BezierCurve curve)
		{
			List<BezierCurve> curves = Reduce(ref curve);
			List<Vector3> intersections = new List<Vector3>();

			for (int i = 0; i < curves.Count - 2; i++)
			{
				IEnumerable<BezierCurve> left = curves.Skip(i).Take(1);
				IEnumerable<BezierCurve> right = curves.Skip(i + 2);
				var result = CurvesIntersect(left, right);
				intersections.AddRange(result);
			}

			return intersections;
		}

		private static List<Vector3> CurvesIntersect(IEnumerable<BezierCurve> left, IEnumerable<BezierCurve> right)
		{
			List<BezierCurve[]> pairs = new List<BezierCurve[]>();
			foreach (var l in left)
			{
				foreach (var r in right)
				{
					if(BoundingBoxesOverlap(GetBoundingBox(l), GetBoundingBox(r)))
					{
						pairs.Add(new BezierCurve[] {l, r});
					}
				}
			}

			List<Vector3> intersections = new List<Vector3>();
			foreach (var pair in pairs)
			{
				var result = IteratePair(pair[0], pair[1]);
				if(result.Count > 0)
				{
					intersections.AddRange(result);
				}
			}

			return intersections;
		}

		private static List<Vector3> IteratePair(BezierCurve left, BezierCurve right)
		{
			Bounds leftBounds = GetBoundingBox(left);
			Bounds rightBounds = GetBoundingBox(right);
			int r = 100000;
			float threshold = 0.5f;

			if(leftBounds.size.x + leftBounds.size.y < threshold && rightBounds.size.x + rightBounds.size.y < threshold)
			{
				//TODO: fix this!!!
				return new List<Vector3>() { (leftBounds.center + rightBounds.center) / 2 } ;
			}

			SplitBezierCurveData splitLeft = Split(ref left, 0.5f);
			SplitBezierCurveData splitRight = Split(ref right, 0.5f);

			List<BezierCurve[]> pairs = new List<BezierCurve[]>()
			{
				new BezierCurve[] { splitLeft.curves[0], splitRight.curves[0] },
				new BezierCurve[] { splitLeft.curves[0], splitRight.curves[1] },
				new BezierCurve[] { splitLeft.curves[1], splitRight.curves[1] },
				new BezierCurve[] { splitLeft.curves[1], splitRight.curves[0] },
			};
			
			pairs = pairs.Where(x => BoundingBoxesOverlap(GetBoundingBox(x[0]), GetBoundingBox(x[1]))).ToList();

			List<Vector3> results = new List<Vector3>();

			if(pairs.Count == 0) return results;

			foreach (var pair in pairs)
			{
				results.AddRange(IteratePair(pair[0], pair[1]));
			}

			//results = results.Where((x,i) => results.IndexOf(x) == i).ToList();

			return results;
		}

		private static float Map(float value, float ds, float de, float ts, float te)
		{
			return ts + (te - ts) * ((value - ds) / (de - ds));
		}

		//Extensions
		public static List<Vector3> Intersets(this BezierCurve curve)
		{
			return SelfIntersects(ref curve);
		}

		public static List<Vector3> Intersets(this BezierCurve curve, ref BezierCurve otherCurve)
		{
			return CurvesIntersect(Reduce(ref curve), Reduce(ref otherCurve));
		}
	}
}