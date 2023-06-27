using UnityEngine;
using System;
using System.Linq;

namespace VRBuilder.Core.Utils
{
    /// <summary>
    /// Class that generates a Bezier spline.
    /// </summary>
	public class BezierSpline : MonoBehaviour
	{
		[SerializeField]
		private Vector3[] points;

		[SerializeField]
		private BezierControlPointMode[] modes;

		[SerializeField]
		private bool loop;

        [SerializeField]
        private bool linearVelocity;

        [SerializeField]
        private int curveResolution = 100;

        private bool isLengthDirty = true;
        private float[][] arcLengths;
        private float splineLength;        

        /// <summary>
        /// If true, the spline will form a loop.
        /// </summary>
		public bool Loop
		{
			get
			{
				return loop;
			}
			set
			{
				loop = value;
				if (value == true)
				{
					modes[modes.Length - 1] = modes[0];
					SetControlPoint(0, points[0]);
				}
			}
		}

        /// <summary>
        /// If true, the t parameter will be applied linearly, with some approximation.
        /// </summary>
        public bool LinearVelocity
        {
            get
            {
                return linearVelocity;
            }
            set
            {
                linearVelocity = value;
            }
        }

        /// <summary>
        /// The amount of segments the curve will be divided in for the linear approximation.
        /// </summary>
        public int CurveResolution
        {
            get
            {
                return curveResolution;
            }
            set
            {
                curveResolution = value;
            }
        }

        /// <summary>
        /// Amount of control points in the spline.
        /// </summary>
		public int ControlPointCount
		{
			get
			{
				return points.Length;
			}
		}

        /// <summary>
        /// Returns curve count.
        /// </summary>
        public int CurveCount
        {
            get
            {
                return (points.Length - 1) / 3;
            }
        }

        /// <summary>
        /// Returns the control point at the given index.
        /// </summary>
		public Vector3 GetControlPoint(int index)
		{
			return points[index];
		}

        /// <summary>
        /// Sets the control point at the given index.
        /// </summary>
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
            isLengthDirty = true;
		}

        /// <summary>
        /// Returns control point mode.
        /// </summary>
		public BezierControlPointMode GetControlPointMode(int index)
		{
			return modes[(index + 1) / 3];
		}

        /// <summary>
        /// Sets control point mode.
        /// </summary>
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

        /// <summary>
        /// Returns the point at the given position.
        /// </summary>
        public Vector3 GetPoint(float t)
        {
            int i;

            if (linearVelocity)
            {
                t = Mathf.Clamp01(t);
                GetLinearPosition(ref t, out i);
                i *= 3;
            }
            else
            {
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
            }

            return transform.TransformPoint(Bezier.GetPoint(points[i], points[i + 1], points[i + 2], points[i + 3], t));
        }

        /// <summary>
        /// Returns velocity at the given position.
        /// </summary>
        public Vector3 GetVelocity(float t)
        {
            int i;

            if (linearVelocity)
            {
                t = Mathf.Clamp01(t);
                GetLinearPosition(ref t, out i);
                i *= 3;
            }
            else
            {
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
            }

            return transform.TransformPoint(Bezier.GetFirstDerivative(points[i], points[i + 1], points[i + 2], points[i + 3], t)) - transform.position;
        }

        /// <summary>
        /// Returns direction at the given position.
        /// </summary>
		public Vector3 GetDirection(float t)
        {
            return GetVelocity(t).normalized;
        }

        /// <summary>
        /// Adds a new curve to the spline.
        /// </summary>
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

        /// <summary>
        /// Removes the last curve from the spline.
        /// </summary>
        public void RemoveCurve()
        {
            Array.Resize(ref points, points.Length - 3);
            Array.Resize(ref modes, modes.Length - 1);

            if (loop)
            {
                points[points.Length - 1] = points[0];
                modes[modes.Length - 1] = modes[0];
                EnforceMode(0);
            }
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

        private void GetLinearPosition(ref float t, out int currentCurve)
        {
            if (isLengthDirty || arcLengths == null || arcLengths.Length == 0)
            {
                CalculateArcLengths();
            }

            float distanceOnCurve = t * splineLength;
            currentCurve = 0;

            while (currentCurve < arcLengths.Length && distanceOnCurve - arcLengths[currentCurve].Last() > 0)
            {
                distanceOnCurve -= arcLengths[currentCurve].Last();
                currentCurve++;
            }

            currentCurve = Mathf.Clamp(currentCurve, 0, arcLengths.Length - 1);

            int waypointIndex = Array.IndexOf(arcLengths[currentCurve], arcLengths[currentCurve].Where(wp => wp <= distanceOnCurve).Max());

            float waypointBefore = arcLengths[currentCurve][waypointIndex];

            if(waypointIndex == arcLengths[currentCurve].Length - 1)
            {
                t = 1f;
            }
            else
            {
                float waypointAfter = arcLengths[currentCurve][waypointIndex + 1];
                float waypointDistance = waypointAfter - waypointBefore;
                float partialDistance = distanceOnCurve - waypointBefore;
                t = Mathf.Lerp(waypointIndex / (float)(arcLengths[currentCurve].Length - 1), (waypointIndex + 1) / (float)(arcLengths[currentCurve].Length - 1), waypointDistance == 0 ? 0f : partialDistance / waypointDistance);
            }
        }

        private void CalculateArcLengths()
        {
            Array.Resize(ref arcLengths, CurveCount);
            splineLength = 0;

            for (int i = 0; i < CurveCount; ++i)
            {
                int p = i * 3;
                arcLengths[i] = Bezier.GetArcLength(points[p], points[p + 1], points[p + 2], points[p + 3], CurveResolution).ToArray();
                splineLength += arcLengths[i].Last();
            }

            isLengthDirty = false;
        }

		public void Reset()
		{
			points = new Vector3[] {
			new Vector3(1f, 0f, 0f),
			new Vector3(2f, 0f, 0f),
			new Vector3(3f, 0f, 0f),
			new Vector3(4f, 0f, 0f)
		};

			modes = new BezierControlPointMode[] {
			BezierControlPointMode.Free,
			BezierControlPointMode.Free
		};
		}
	}
}
