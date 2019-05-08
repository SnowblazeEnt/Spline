using System;
using UnityEngine;

namespace SnowblazeEntertainment.Tools.Spline
{
    [Serializable]
    public struct BezierCurve
    {
        public Vector3[] points;

        public BezierCurve(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            points = new Vector3[] { p0, p1, p2, p3 };
        }
    }
}