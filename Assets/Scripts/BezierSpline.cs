using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace SnowblazeEntertainment.Tools.Spline
{
	public class BezierSpline : MonoBehaviour 
	{
		[SerializeField]
		private Vector3[] points;
		[SerializeField]
		private BezierControlPointMode[] modes;
		[SerializeField]
		private float[] lengths;

		[SerializeField]
		private bool loop;
		[SerializeField]
		private float stepWorldUnits = 0.005f;
		[SerializeField]
		private float roadRadius = 1.0f;
		[SerializeField]
		private float borderRadius = 0.1f;

		[SerializeField]
		private SpeedCategory speedCategory;

		[SerializeField]
		private List<Vector3> lut;
#if UNITY_EDITOR
		public float Progress { get; set; }
#endif


		public bool Loop { get { return loop; } }
		public int ControlPointCount { get { return points.Length; } }
		public int CurveCount { get { return (points.Length - 1) / 3; } }

		public Vector3 GetControlPoint(int index) 
		{
			return points[index];
		}

		public void SetControlPoint(int index, Vector3 point) 
		{
			if (index % 3 == 0) 
			{
				Vector3 delta = point - points[index];
				if (loop) 
				{
					if (index == 0) 
					{
						points[1] += delta;
						points[points.Length - 2] += delta;
						points[points.Length - 1] = point;
					}
					else if (index == points.Length - 1) 
					{
						points[0] = point;
						points[1] += delta;
						points[index - 1] += delta;
					}
					else 
					{
						points[index - 1] += delta;
						points[index + 1] += delta;
					}
				}
				else 
				{
					if (index > 0) 
					{
						points[index - 1] += delta;
					}
					if (index + 1 < points.Length) 
					{
						points[index + 1] += delta;
					}
				}
			}
			points[index] = point;
			EnforceMode(index);
		}

		public BezierControlPointMode GetControlPointMode(int index) 
		{
			return modes[(index + 1) / 3];
		}

		public void SetControlPointMode(int index, BezierControlPointMode mode) 
		{
			int modeIndex = (index + 1) / 3;
			modes[modeIndex] = mode;
			if (loop) 
			{
				if (modeIndex == 0) 
				{
					modes[modes.Length - 1] = mode;
				}
				else if (modeIndex == modes.Length - 1) 
				{
					modes[0] = mode;
				}
			}
			EnforceMode(index);
		}

		private void EnforceMode(int index)
		{
			int modeIndex = (index + 1) / 3;
			BezierControlPointMode mode = modes[modeIndex];
			if (mode == BezierControlPointMode.Free || !loop && (modeIndex == 0 || modeIndex == modes.Length - 1)) 
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
					fixedIndex = points.Length - 2;
				}
				enforcedIndex = middleIndex + 1;
				if (enforcedIndex >= points.Length) 
				{
					enforcedIndex = 1;
				}
			}
			else 
			{
				fixedIndex = middleIndex + 1;
				if (fixedIndex >= points.Length) 
				{
					fixedIndex = 1;
				}
				enforcedIndex = middleIndex - 1;
				if (enforcedIndex < 0) 
				{
					enforcedIndex = points.Length - 2;
				}
			}

			Vector3 middle = points[middleIndex];
			Vector3 enforcedTangent = middle - points[fixedIndex];
			if (mode == BezierControlPointMode.Aligned) 
			{
				enforcedTangent = enforcedTangent.normalized * Vector3.Distance(middle, points[enforcedIndex]);
			}
			points[enforcedIndex] = middle + enforcedTangent;
		}

		public Vector3 GetPoint(float t) 
		{
			int i;
			if (t >= 1f) 
			{
				t = 1f;
				i = points.Length - 4;
			}
			else 
			{
				t = Mathf.Clamp01(t) * CurveCount;
				i = (int)t;
				t -= i;
				i *= 3;
			}
			return transform.TransformPoint(Bezier.GetPoint(points[i], points[i + 1], points[i + 2], points[i + 3], t));
		}
		
		public Vector3 GetVelocity(float t) 
		{
			int i;
			if (t >= 1f) 
			{
				t = 1f;
				i = points.Length - 4;
			}
			else 
			{
				t = Mathf.Clamp01(t) * CurveCount;
				i = (int)t;
				t -= i;
				i *= 3;
			}
			return transform.TransformPoint(Bezier.GetFirstDerivative(points[i], points[i + 1], points[i + 2], points[i + 3], t)) - transform.position;
		}
		
		public Vector3 GetDirection(float t) 
		{
			return GetVelocity(t).normalized;
		}

		public void AddCurve() 
		{
			Vector3 point = points[points.Length - 1];
			Array.Resize(ref points, points.Length + 3);
			point.x += 1f;
			points[points.Length - 3] = point;
			point.x += 1f;
			points[points.Length - 2] = point;
			point.x += 1f;
			points[points.Length - 1] = point;

			Array.Resize(ref modes, modes.Length + 1);
			modes[modes.Length - 1] = modes[modes.Length - 2];
			EnforceMode(points.Length - 4);

			if (loop) 
			{
				points[points.Length - 1] = points[0];
				modes[modes.Length - 1] = modes[0];
				EnforceMode(0);
			}
		}
		
		public void Reset()
		{
			points = new Vector3[] 
			{
				new Vector3(1f, 0f, 0f),
				new Vector3(2f, 0f, 0f),
				new Vector3(3f, 0f, 0f),
				new Vector3(4f, 0f, 0f)
			};
			modes = new BezierControlPointMode[] 
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

			float step = stepWorldUnits / lengths.Sum();

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

			//DrawSpline(lut);

			this.lut = lut;
		}

		private void RecalculateLengths()
		{
			Vector3 previousPoint = transform.TransformPoint(Bezier.GetPoint(points[0], points[1], points[2], points[3], 0.0f));
			lengths = new float[CurveCount];
			for (float i = 0.0f; i <= 1.0f; i += 0.01f)
			{
				int t;
				float y = i;
				if (y >= 1f) 
				{
					y = 1f;
					t = points.Length - 4;
				}
				else 
				{
					y = Mathf.Clamp01(y) * CurveCount;
					t = (int)y;
					y -= t;
					t *= 3;
				}
				Vector3 currentPoint = transform.TransformPoint(Bezier.GetPoint(points[t], points[t + 1], points[t + 2], points[t + 3], y));
				lengths[t / 3] += (currentPoint - previousPoint).magnitude;
				previousPoint = currentPoint;
			}

			//totalLength = lengths.Sum();
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
			int index = Mathf.RoundToInt(Mathf.Lerp(0, lut.Count - 1, time));

			return lut[index];
		}

		public float GetSpeed()
		{
			return (float)speedCategory;
		}
	}
}