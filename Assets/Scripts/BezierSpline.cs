using System;

namespace SnowblazeEntertainment.Tools.Spline
{
    [Serializable]
    public struct BezierSpline
    {
        public BezierCurve[] curves;
        public BezierControlPointMode[] modes;
        public float[] lengths;
    }
}