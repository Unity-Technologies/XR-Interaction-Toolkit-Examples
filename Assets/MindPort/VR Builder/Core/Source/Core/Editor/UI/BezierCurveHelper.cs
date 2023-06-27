// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VRBuilder.Editor.UI
{
    internal static class BezierCurveHelper
    {
        /// <summary>
        /// Calculates a Bezier curve using "De Casteljau's algorithm".
        /// </summary>
        /// <param name="startPosition">Starting position.</param>
        /// <param name="controlPointOne">First control point position.</param>
        /// <param name="controlPointTwo">Second control point position.</param>
        /// <param name="endPosition">End position.</param>
        /// <param name="curveSegmentCount">Number of lines the Bezier curve is made of.</param>
        /// <returns>An array containing all calculated points. The length is <paramref name="curveSegmentCount"/> + 1.</returns>
        public static Vector2[] CalculateDeCastejauCurve(Vector2 startPosition, Vector2 controlPointOne, Vector2 controlPointTwo, Vector2 endPosition, int curveSegmentCount)
        {
            Vector2[] points = new Vector2[curveSegmentCount + 1];
            points[0] = startPosition;
            points[curveSegmentCount] = endPosition;

            float resolution = 1.0f / curveSegmentCount;

            for (int i = 1; i < curveSegmentCount; i++)
            {
                float timeStep = i * resolution;

                Vector2 q0 = Vector2.Lerp(startPosition, controlPointOne, timeStep);
                Vector2 q1 = Vector2.Lerp(controlPointOne, controlPointTwo, timeStep);
                Vector2 q2 = Vector2.Lerp(controlPointTwo, endPosition, timeStep);

                Vector2 r0 = Vector2.Lerp(q0, q1, timeStep);
                Vector2 r1 = Vector2.Lerp(q1, q2, timeStep);

                points[i] = Vector2.Lerp(r0, r1, timeStep);
            }

            return points;
        }

        /// <summary>
        /// Calculates control points that try to make the curve avoid the given bounding boxes.
        /// </summary>
        /// <param name="startPosition">Position where the curve starts.</param>
        /// <param name="endPosition">Position where the curve ends.</param>
        /// <param name="startBoundingBox">Bounding box of the start element which should be avoided.</param>
        /// <param name="endBoundingBox">Bounding box of the destination element which should be avoided.</param>
        /// <returns>An array containing both control points in the right order.</returns>
        public static Vector2[] CalculateControlPointsForTransition(Vector2 startPosition, Vector2 endPosition,  Rect startBoundingBox, Rect endBoundingBox)
        {
            float maxBoundingBoxWidth = Mathf.Max(startBoundingBox.width, endBoundingBox.width);

            // Control points for the regular "s" curve which does not need to avoid crossing the elements' boundaries.
            Vector2 controlPointOne = new Vector2(startPosition.x + Mathf.Max(maxBoundingBoxWidth / 2f, Mathf.Abs(startPosition.x - endPosition.x)), startPosition.y);
            Vector2 controlPointTwo = new Vector2(endPosition.x - Mathf.Max(maxBoundingBoxWidth / 2f, Mathf.Abs(startPosition.x - endPosition.x)), endPosition.y);

            // If the destination point is positioned right of the start element center, return result.
            if (endPosition.x > startBoundingBox.xMax)
            {
                return new Vector2[] { controlPointOne, controlPointTwo };
            }

            // Otherwise, we need to take care of the boundaries.
            float fromBaseX = Mathf.Max(startPosition.x, endBoundingBox.xMax);
            float toBaseX = Mathf.Min(endPosition.x, startBoundingBox.xMin);
            controlPointOne.x = fromBaseX + Mathf.Max(startBoundingBox.height, endBoundingBox.height, Mathf.Abs(fromBaseX - endPosition.x));
            controlPointTwo.x = toBaseX - Mathf.Max(startBoundingBox.height, endBoundingBox.height, Mathf.Abs(toBaseX - startPosition.x));

            float elementBoundaryOffset = Mathf.Max(startBoundingBox.height * 0.667f, endBoundingBox.height * 0.667f);

            // If the destination point is below the center of the start element,
            if (endPosition.y > startBoundingBox.center.y)
            {
                // Draw an inverted "s" curve which avoids crossing the elements' boundaries from the start to the destination point.
                controlPointOne.y = startBoundingBox.yMax + elementBoundaryOffset;
                controlPointTwo.y = Mathf.Max(controlPointOne.y, endBoundingBox.yMin - elementBoundaryOffset);

                // But as long as the destination element's top line is above the the second control point,
                // Draw an "o" curve going around the elements,
                // Because there is no space for the inverted "s" curve.
                if (endBoundingBox.yMin <= controlPointOne.y)
                {
                    float lowestPoint = Mathf.Max(startBoundingBox.yMax, endBoundingBox.yMax);
                    controlPointOne.y = lowestPoint + Mathf.Max(elementBoundaryOffset, 125f);
                    controlPointTwo.y = controlPointOne.y;
                }
            }
            // Else if the destination point is above the center of the start element,
            else
            {
                // Draw a "s" curve which avoids crossing the elements' boundaries from the start to the destination point.
                controlPointOne.y = startBoundingBox.y - elementBoundaryOffset;
                controlPointTwo.y = endBoundingBox.yMax + elementBoundaryOffset;

                // But as long as the destination element's bottom line is below the the second control point,
                // Draw an "o" curve going around the elements,
                // Because there is no space for the "s" curve.
                if (endBoundingBox.yMax >= controlPointOne.y)
                {
                    controlPointOne.y = endBoundingBox.y - elementBoundaryOffset;
                    controlPointTwo.y = controlPointOne.y;
                }
            }

            return new Vector2[] { controlPointOne, controlPointTwo };
        }

        /// <summary>
        /// Returns a bounding box calculated from all given polyline <paramref name="points"/>.
        /// </summary>
        public static Rect CalculateBoundingBoxForPolyline(IList<Vector2> points)
        {
            float xMin = points.Min(point => point.x);
            float xMax = points.Max(point => point.x);
            float yMin = points.Min(point => point.y);
            float yMax = points.Max(point => point.y);

            return new Rect(xMin, yMin, Mathf.Abs(xMax - xMin), Mathf.Abs(yMax - yMin));
        }
    }
}
