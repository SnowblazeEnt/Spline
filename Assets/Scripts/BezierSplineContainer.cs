using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace SnowblazeEntertainment.Tools.Spline
{
	public class BezierSplineContainer : MonoBehaviour 
	{
		[SerializeField]
		private BezierSpline spline;

		[SerializeField]
		private bool loop;
		[SerializeField]
		private float stepWorldUnits = 0.005f;
		[SerializeField]
		private float roadRadius = 1.0f;
		[SerializeField]
		private float borderRadius = 0.1f;
		[SerializeField]
        private float t1;
		[SerializeField]
        private float t2;

		[SerializeField]
		private SpeedCategory speedCategory;

		[SerializeField]
		private List<Vector3> lut;
#if UNITY_EDITOR
		public float Progress { get; set; }
#endif


		public float StepWorldUnits 
		{ 
			get 
			{ 
				return stepWorldUnits; 
			}
			set
			{
				stepWorldUnits = value;
			} 
		}
		public bool Loop 
		{ 
			get
			{
				return loop;
			}
			set
			{
				loop = value;
			}
		}
		public float RoadRadius
		{
			get
			{
				return roadRadius;
			}
			set
			{
				roadRadius = value;
			}
		}
		public float BorderRadius
		{
			get
			{
				return borderRadius;
			}
			set
			{
				borderRadius = value;
			}
		}
		public SpeedCategory SpeedCategory
		{
			get
			{
				return speedCategory;
			}
			set
			{
				speedCategory = value;
			}
		}
		public float T1
		{
			get
			{
				return t1;
			}
			set
			{
				t1 = value;
			}
		}
		public float T2
		{
			get
			{
				return t2;
			}
			set
			{
				t2 = value;
			}
		}
		public int ControlPointCount { get { return spline.curves.Length * 3 + 1; } }
		public int CurveCount { get { return spline.curves.Length; } }
		public BezierSpline Spline {get { return spline;}}

		public Vector3 GetControlPoint(int index) 
		{
			return spline.curves[(index - 1) / 3].points[index - ((index - 1) / 3) * 3];
		}

		public void SetControlPoint(int index, Vector3 point) 
		{
			if (index % 3 == 0) 
			{
				Vector3 delta = point - GetControlPoint(index);
				if (loop) 
				{
						BezierCurve curve;
						curve = spline.curves[index / 3 - 1];
						curve.points[2] += delta;
						curve.points[3] = point;
						spline.curves[index / 3 - 1] = curve;
						curve = spline.curves[index / 3];
						curve.points[0] = point;
						curve.points[1] += delta;
						spline.curves[index / 3] = curve;
				}
				else 
				{
					if (index > 0) 
					{
						BezierCurve curve = spline.curves[index / 3 - 1];
						curve.points[2] += delta;
						curve.points[3] = point;
						spline.curves[index / 3 - 1] = curve;
					}
					if (index + 1 < ControlPointCount) 
					{
						BezierCurve curve = spline.curves[index / 3];
						curve.points[0] = point;
						curve.points[1] += delta;
						spline.curves[index / 3] = curve;
					}
				}
			}
			else
			{
				BezierCurve curve = spline.curves[index / 3];
				curve.points[index % 3] = point;
				spline.curves[index / 3] = curve;
			}
			EnforceMode(index);
		}

		public BezierControlPointMode GetControlPointMode(int index) 
		{
			return spline.modes[(index + 1) / 3];
		}

		public void SetControlPointMode(int index, BezierControlPointMode mode) 
		{
			int modeIndex = (index + 1) / 3;
			spline.modes[modeIndex] = mode;
			if (loop) 
			{
				if (modeIndex == 0) 
				{
					spline.modes[spline.modes.Length - 1] = mode;
				}
				else if (modeIndex == spline.modes.Length - 1) 
				{
					spline.modes[0] = mode;
				}
			}
			EnforceMode(index);
		}

		private void EnforceMode(int index)
		{
			int modeIndex = (index + 1) / 3;
			BezierControlPointMode mode = spline.modes[modeIndex];
			if (mode == BezierControlPointMode.Free || !loop && (modeIndex == 0 || modeIndex == spline.modes.Length - 1)) 
			{
				return;
			}

			int middleIndex = modeIndex * 3;
			int fixedIndex, enforcedIndex;
			if (index <= middleIndex) 
			{
				fixedIndex = middleIndex - 1;
				if (fixedIndex < 0) 
				{
					fixedIndex = ControlPointCount - 2;
				}
				enforcedIndex = middleIndex + 1;
				if (enforcedIndex >= ControlPointCount) 
				{
					enforcedIndex = 1;
				}
			}
			else 
			{
				fixedIndex = middleIndex + 1;
				if (fixedIndex >= ControlPointCount) 
				{
					fixedIndex = 1;
				}
				enforcedIndex = middleIndex - 1;
				if (enforcedIndex < 0) 
				{
					enforcedIndex = ControlPointCount - 2;
				}
			}

			Vector3 middle = GetControlPoint(middleIndex);
			Vector3 enforcedTangent = middle - GetControlPoint(fixedIndex);
			if (mode == BezierControlPointMode.Aligned) 
			{
				enforcedTangent = enforcedTangent.normalized * Vector3.Distance(middle, GetControlPoint(enforcedIndex));
			}

			if (index <= middleIndex) 
			{
				BezierCurve curve = spline.curves[(enforcedIndex - 1) / 3];
				curve.points[0] = middle;
				curve.points[1] = middle + enforcedTangent;
				spline.curves[(enforcedIndex - 1) / 3] = curve;
			}
			else
			{
				BezierCurve curve = spline.curves[(enforcedIndex - 1) / 3];
				curve.points[2] = middle + enforcedTangent;
				curve.points[3] = middle;
				spline.curves[(enforcedIndex - 1) / 3] = curve;
			}
		}

		public Vector3 GetPoint(float t) 
		{
			int i;
			if (t >= 1f) 
			{
				t = 1f;
				i = CurveCount - 1;
			}
			else 
			{
				t = Mathf.Clamp01(t) * CurveCount;
				i = (int)t;
				t -= i;
			}
			return transform.TransformPoint(Bezier.GetPoint(ref spline.curves[i], t));
		}
		
		public Vector3 GetVelocity(float t) 
		{
			int i;
			if (t >= 1f) 
			{
				t = 1f;
				i = CurveCount - 1;
			}
			else 
			{
				t = Mathf.Clamp01(t) * CurveCount;
				i = (int)t;
				t -= i;
			}
			return transform.TransformPoint(Bezier.GetFirstDerivative(ref spline.curves[i], t)) - transform.position;
		}
		
		public Vector3 GetDirection(float t) 
		{
			return GetVelocity(t).normalized;
		}

		public void AddCurve() 
		{
			Vector3 point = GetControlPoint(ControlPointCount - 1);
			Array.Resize(ref spline.curves, spline.curves.Length + 1);
			spline.curves[CurveCount - 1] = new BezierCurve(point, point + Vector3.right * 1.0f, point + Vector3.right * 2.0f, point + Vector3.right * 3.0f);

			Array.Resize(ref spline.modes, spline.modes.Length + 1);
			spline.modes[spline.modes.Length - 1] = spline.modes[spline.modes.Length - 2];
			EnforceMode(ControlPointCount - 4);

			if (loop) 
			{
				SetControlPoint(ControlPointCount - 1, GetControlPoint(0));
				spline.modes[spline.modes.Length - 1] = spline.modes[0];
				EnforceMode(0);
			}
		}
		
		public void Reset()
		{
			spline.curves = new BezierCurve[]
			{
				new BezierCurve
				(
					new Vector3(1f, 0f, 0f),
					new Vector3(2f, 0f, 0f),
					new Vector3(3f, 0f, 0f),
					new Vector3(4f, 0f, 0f)
				),
			};
			spline.modes = new BezierControlPointMode[] 
			{
				BezierControlPointMode.Free,
				BezierControlPointMode.Free
			};
		}

		public void GenerateLUT()
		{
			RecalculateLengths();

			List<Vector3> tempLUT = new List<Vector3>();
			List<Vector3> lut = new List<Vector3>();

			float step = stepWorldUnits / spline.lengths.Sum();

			for (float i = 0.0f; i < 1.0f; i += step)
			{
				tempLUT.Add(GetPoint(i));
			}

			tempLUT.Add(GetPoint(1.0f));

			lut.Add(tempLUT.First());
			Vector3 prevPos = lut.First();
			float stepSquared = stepWorldUnits * stepWorldUnits;

			for (int i = 0; i < tempLUT.Count; i++)
			{
				if((tempLUT[i] - prevPos).sqrMagnitude - stepSquared < 0.0f) continue;

				Vector3 direction = (tempLUT[i--] - prevPos).normalized;
				lut.Add(prevPos + direction * stepWorldUnits);

				prevPos = lut.Last();
			}

			if(lut.Last() != tempLUT.Last())
				lut.Add(tempLUT.Last());

			GenerateMesh(lut);
			GenerateBorders(lut);

			this.lut = lut;
		}

		private void RecalculateLengths()
		{
			Vector3 previousPoint = transform.TransformPoint(Bezier.GetPoint(ref spline.curves[0], 0.0f));
			spline.lengths = new float[CurveCount];
			for (float i = 0.0f; i <= 1.0f; i += 0.01f)
			{
				int t;
				float y = i;
				if (y >= 1f) 
				{
					y = 1f;
					t = CurveCount - 1;
				}
				else 
				{
					y = Mathf.Clamp01(y) * CurveCount;
					t = (int)y;
					y -= t;
				}
				Vector3 currentPoint = transform.TransformPoint(Bezier.GetPoint(ref spline.curves[t], y));
				spline.lengths[t] += (currentPoint - previousPoint).magnitude;
				previousPoint = currentPoint;
			}
		}

		private void DrawSpline(List<Vector3> lut)
		{
			foreach (var item in transform.GetComponentsInChildren<MeshRenderer>())
			{
				DestroyImmediate(item.gameObject);
			}

			for(int i = 0; i < lut.Count; i++)
			{
				GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
				//go.transform.localScale = Vector3.one * 0.01f;
				go.name = i.ToString();
				go.transform.SetParent(transform);
				go.transform.position = lut[i];
			}
		}

		private void GenerateMesh(List<Vector3> lut)
		{
			List<Vector3> vertices = new List<Vector3>();
			List<int> triangles = new List<int>();
			List<Vector2> uvs = new List<Vector2>();

			Vector3 cross = Vector3.Cross(Vector3.up, (lut[1] - lut[0]).normalized);
			vertices.Add(lut[0] - cross * roadRadius);
			vertices.Add(lut[0] + cross * roadRadius);

			for (int i = 1; i < lut.Count - 1; i++)
			{
				cross = Vector3.Cross(Vector3.up, (lut[i + 1] - lut[i - 1]).normalized);
				vertices.Add(lut[i] - cross * roadRadius);
				vertices.Add(lut[i] + cross * roadRadius);
			}

			cross = Vector3.Cross(Vector3.up, (lut[lut.Count - 1] - lut[lut.Count - 2]).normalized);
			vertices.Add(lut[lut.Count - 1] - cross * roadRadius);
			vertices.Add(lut[lut.Count - 1] + cross * roadRadius);

			triangles.Add(2);
			triangles.Add(1);
			triangles.Add(0);
			triangles.Add(3);
			triangles.Add(1);
			triangles.Add(2);

			for (int i = 6; i < (vertices.Count - 2) * 3; i++)
			{
				triangles.Add(triangles[i - 6] + 2);
			}

			for (int i = 0; i < vertices.Count; i++)
			{
				uvs.Add(new Vector2(i % 2 == 0 ? 1 : 0, i / 2));
			}

			Mesh mesh = new Mesh();
			mesh.vertices = vertices.ToArray();
			mesh.triangles = triangles.ToArray();
			mesh.uv = uvs.ToArray();
			mesh.RecalculateNormals();

			Transform roadTransform = transform.Find("Road");
			if(roadTransform != null)
			{
				DestroyImmediate(roadTransform.gameObject);
			}

			GameObject roadGO = new GameObject("Road");
			roadGO.transform.SetParent(transform);
			MeshRenderer meshRenderer = roadGO.AddComponent<MeshRenderer>();
			meshRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Road.mat");
			MeshFilter meshFilter = roadGO.AddComponent<MeshFilter>();
			meshFilter.mesh = mesh;
		}

		private void GenerateBorders(List<Vector3> lut)
		{
			List<Vector3> vertices = new List<Vector3>();
			List<int> triangles = new List<int>();
			List<Vector2> uvs = new List<Vector2>();

			Vector3 cross = Vector3.Cross(Vector3.up, (lut[1] - lut[0]).normalized);
			vertices.Add(lut[0] - cross * (roadRadius + borderRadius));
			vertices.Add(lut[0] - cross * roadRadius);

			for (int i = 1; i < lut.Count - 1; i++)
			{
				cross = Vector3.Cross(Vector3.up, (lut[i + 1] - lut[i - 1]).normalized);
				vertices.Add(lut[i] - cross * (roadRadius + borderRadius));
				vertices.Add(lut[i] - cross * roadRadius);
			}

			cross = Vector3.Cross(Vector3.up, (lut[lut.Count - 1] - lut[lut.Count - 2]).normalized);
			vertices.Add(lut[lut.Count - 1] - cross * (roadRadius + borderRadius));
			vertices.Add(lut[lut.Count - 1] - cross * roadRadius);

			triangles.Add(2);
			triangles.Add(1);
			triangles.Add(0);
			triangles.Add(3);
			triangles.Add(1);
			triangles.Add(2);

			for (int i = 6; i < (vertices.Count - 2) * 3; i++)
			{
				triangles.Add(triangles[i - 6] + 2);
			}

			for (int i = 0; i < vertices.Count; i++)
			{
				uvs.Add(new Vector2(i % 2 == 0 ? 1 : 0, i / 2));
			}

			Mesh mesh = new Mesh();
			mesh.vertices = vertices.ToArray();
			mesh.triangles = triangles.ToArray();
			mesh.uv = uvs.ToArray();
			mesh.RecalculateNormals();

			Transform roadTransform = transform.Find("LeftBorder");
			if(roadTransform != null)
			{
				DestroyImmediate(roadTransform.gameObject);
			}

			GameObject roadGO = new GameObject("LeftBorder");
			roadGO.transform.SetParent(transform);
			MeshRenderer meshRenderer = roadGO.AddComponent<MeshRenderer>();
			meshRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Border.mat");
			MeshFilter meshFilter = roadGO.AddComponent<MeshFilter>();
			meshFilter.mesh = mesh;

			vertices.Clear();
			triangles.Clear();
			uvs.Clear();

			cross = Vector3.Cross(Vector3.up, (lut[1] - lut[0]).normalized);
			vertices.Add(lut[0] + cross * roadRadius);
			vertices.Add(lut[0] + cross * (roadRadius + borderRadius));

			for (int i = 1; i < lut.Count - 1; i++)
			{
				cross = Vector3.Cross(Vector3.up, (lut[i + 1] - lut[i - 1]).normalized);
				vertices.Add(lut[i] + cross * roadRadius);
				vertices.Add(lut[i] + cross * (roadRadius + borderRadius));
			}

			cross = Vector3.Cross(Vector3.up, (lut[lut.Count - 1] - lut[lut.Count - 2]).normalized);
			vertices.Add(lut[lut.Count - 1] + cross * roadRadius);
			vertices.Add(lut[lut.Count - 1] + cross * (roadRadius + borderRadius));

			triangles.Add(2);
			triangles.Add(1);
			triangles.Add(0);
			triangles.Add(3);
			triangles.Add(1);
			triangles.Add(2);

			for (int i = 6; i < (vertices.Count - 2) * 3; i++)
			{
				triangles.Add(triangles[i - 6] + 2);
			}

			for (int i = 0; i < vertices.Count; i++)
			{
				uvs.Add(new Vector2(i % 2 == 0 ? 1 : 0, i / 2));
			}

			mesh = new Mesh();
			mesh.vertices = vertices.ToArray();
			mesh.triangles = triangles.ToArray();
			mesh.uv = uvs.ToArray();
			mesh.RecalculateNormals();

			roadTransform = transform.Find("RightBorder");
			if(roadTransform != null)
			{
				DestroyImmediate(roadTransform.gameObject);
			}

			roadGO = new GameObject("RightBorder");
			roadGO.transform.SetParent(transform);
			meshRenderer = roadGO.AddComponent<MeshRenderer>();
			meshRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Border.mat");
			meshFilter = roadGO.AddComponent<MeshFilter>();
			meshFilter.mesh = mesh;
		}

		public Vector3 SamplePoint(float time)
		{
			float mappedTime = Mathf.Lerp(t1, t2, time);
			int index = Mathf.RoundToInt(Mathf.Lerp(0, lut.Count - 1, mappedTime));

			return lut[index];
		}

		public float GetSpeed()
		{
			return (float)speedCategory;
		}

		
	}
}