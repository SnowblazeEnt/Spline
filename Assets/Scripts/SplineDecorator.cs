using UnityEngine;

namespace SnowblazeEntertainment.Tools.Spline
{
	public class SplineDecorator : MonoBehaviour 
	{
		public BezierSplineContainer splineContainer;

		public int frequency;

		public bool lookForward;

		public Transform[] items;

		private void Awake() 
		{
			if (frequency <= 0 || items == null || items.Length == 0) 
			{
				return;
			}
			float stepSize = frequency * items.Length;
			if (splineContainer.Loop || stepSize == 1) 
			{
				stepSize = 1f / stepSize;
			}
			else 
			{
				stepSize = 1f / (stepSize - 1);
			}
			for (int p = 0, f = 0; f < frequency; f++) 
			{
				for (int i = 0; i < items.Length; i++, p++) 
				{
					Transform item = Instantiate(items[i]) as Transform;
					Vector3 position = splineContainer.GetPoint(p * stepSize);
					item.transform.localPosition = position;
					if (lookForward) 
					{
						item.transform.LookAt(position + splineContainer.GetDirection(p * stepSize));
					}
					item.transform.parent = transform;
				}
			}
		}
	}
}