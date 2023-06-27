/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Oculus.Interaction.Grab.GrabSurfaces
{
    /// <summary>
    /// Defines a series of points that specify paths along with the snapping pose is valid.
    /// The points can be connected to the previous and/or next one, even looping, or completely isolated.
    /// When two or more consecutive points are connected, they define a continuous path. The Tangent can be
    /// used to specify the curve between two points. It is also possible to connect the last point with the
    /// first one to create a closed loop. The rotation at a point in the path is specified by interpolating
    /// the rotation of the starting and ending point of the current segment.
    /// When a point is not connected to any other point, it is considered as a single point in space.
    /// </summary>
    [Serializable]
    public class BezierGrabSurface : MonoBehaviour, IGrabSurface
    {
        [SerializeField]
        private List<BezierControlPoint> _controlPoints = new List<BezierControlPoint>();

        [SerializeField]
        [Tooltip("Transform used as a reference to measure the local data of the grab surface")]
        private Transform _relativeTo;

        public List<BezierControlPoint> ControlPoints => _controlPoints;

        private const float MAX_PLANE_DOT = 0.95f;

        #region editor events
        protected virtual void Reset()
        {
            _relativeTo = this.GetComponentInParent<IRelativeToRef>()?.RelativeTo;
        }
        #endregion

        protected virtual void Start()
        {
            this.AssertCollectionField(ControlPoints, nameof(ControlPoints));
            this.AssertField(_relativeTo, nameof(_relativeTo));
        }

        public GrabPoseScore CalculateBestPoseAtSurface(in Pose targetPose, out Pose bestPose,
            in PoseMeasureParameters scoringModifier, Transform relativeTo)
        {
            Pose testPose = Pose.identity;
            Pose smallestRotationPose = Pose.identity;
            bestPose = targetPose;
            GrabPoseScore bestScore = GrabPoseScore.Max;
            for (int i = 0; i < _controlPoints.Count; i++)
            {
                BezierControlPoint currentControlPoint = _controlPoints[i];
                BezierControlPoint nextControlPoint = _controlPoints[(i + 1) % _controlPoints.Count];

                if (!currentControlPoint.Disconnected
                    && nextControlPoint.Disconnected)
                {
                    continue;
                }

                GrabPoseScore score;
                if ((currentControlPoint.Disconnected && nextControlPoint.Disconnected)
                    || _controlPoints.Count == 1)
                {
                    Pose worldPose = currentControlPoint.GetPose(relativeTo);
                    testPose.CopyFrom(worldPose);
                    score = new GrabPoseScore(targetPose, testPose, scoringModifier.PositionRotationWeight);
                }
                else
                {
                    Pose start = currentControlPoint.GetPose(relativeTo);
                    Pose end = nextControlPoint.GetPose(relativeTo);
                    Vector3 tangent = currentControlPoint.GetTangent(relativeTo);

                    NearestPointInTriangle(targetPose.position, start.position, tangent, end.position, out float positionT);
                    float rotationT = ProgressForRotation(targetPose.rotation, start.rotation, end.rotation);

                    score = GrabPoseHelper.CalculateBestPoseAtSurface(targetPose, out testPose, scoringModifier, relativeTo,
                        (in Pose target, Transform relativeTo) =>
                        {
                            Pose result;
                            result.position = EvaluateBezier(start.position, tangent, end.position, positionT);
                            result.rotation = Quaternion.Slerp(start.rotation, end.rotation, positionT);
                            return result;

                        },
                        (in Pose target, Transform relativeTo) =>
                        {
                            Pose result;
                            result.position = EvaluateBezier(start.position, tangent, end.position, rotationT);
                            result.rotation = Quaternion.Slerp(start.rotation, end.rotation, rotationT);
                            return result;
                        });
                }

                if (score.IsBetterThan(bestScore))
                {
                    bestScore = score;
                    bestPose.CopyFrom(testPose);
                }
            }
            return bestScore;
        }

        public bool CalculateBestPoseAtSurface(Ray targetRay, out Pose bestPose, Transform relativeTo)
        {
            Pose testPose = Pose.identity;
            Pose targetPose = Pose.identity;
            bestPose = Pose.identity;
            bool poseFound = false;
            GrabPoseScore bestScore = GrabPoseScore.Max;
            for (int i = 0; i < _controlPoints.Count; i++)
            {
                BezierControlPoint currentControlPoint = _controlPoints[i];
                BezierControlPoint nextControlPoint = _controlPoints[(i + 1) % _controlPoints.Count];

                if (!currentControlPoint.Disconnected
                    && nextControlPoint.Disconnected)
                {
                    continue;
                }

                if ((currentControlPoint.Disconnected && nextControlPoint.Disconnected)
                    || _controlPoints.Count == 1)
                {
                    Pose worldPose = currentControlPoint.GetPose(relativeTo);
                    Plane plane = new Plane(-targetRay.direction, worldPose.position);
                    if (!plane.Raycast(targetRay, out float enter))
                    {
                        continue;
                    }
                    targetPose.position = targetRay.GetPoint(enter);
                    testPose.CopyFrom(worldPose);
                }
                else
                {
                    Pose start = currentControlPoint.GetPose(relativeTo);
                    Pose end = nextControlPoint.GetPose(relativeTo);
                    Vector3 tangent = currentControlPoint.GetTangent(relativeTo);
                    Plane plane = GenerateRaycastPlane(start.position, tangent, end.position, -targetRay.direction);
                    if (!plane.Raycast(targetRay, out float enter))
                    {
                        continue;
                    }
                    targetPose.position = targetRay.GetPoint(enter);
                    NearestPointInTriangle(targetPose.position, start.position, tangent, end.position, out float t);
                    testPose.position = EvaluateBezier(start.position, tangent, end.position, t);
                    testPose.rotation = Quaternion.Slerp(start.rotation, end.rotation, t);
                }

                GrabPoseScore score = new GrabPoseScore(targetPose.position, testPose.position);
                if (score.IsBetterThan(bestScore))
                {
                    bestScore = score;
                    bestPose.CopyFrom(testPose);
                    poseFound = true;
                }
            }
            return poseFound;
        }

        private Plane GenerateRaycastPlane(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 fallbackDir)
        {
            Vector3 line0 = (p1 - p0).normalized;
            Vector3 line1 = (p2 - p0).normalized;

            Plane plane;
            if (Mathf.Abs(Vector3.Dot(line0, line1)) > MAX_PLANE_DOT)
            {
                plane = new Plane(fallbackDir, (p0 + p2 + p1) / 3f);
            }
            else
            {
                plane = new Plane(p0, p1, p2);
            }
            return plane;
        }

        private float ProgressForRotation(Quaternion targetRotation, Quaternion from, Quaternion to)
        {
            Vector3 targetForward = targetRotation * Vector3.forward;
            Vector3 fromForward = from * Vector3.forward;
            Vector3 toForward = to * Vector3.forward;
            Vector3 axis = Vector3.Cross(fromForward, toForward).normalized;

            float angleFrom = Vector3.SignedAngle(targetForward, fromForward, axis);
            float angleTo = Vector3.SignedAngle(targetForward, toForward, axis);

            if (angleFrom < 0 && angleTo < 0)
            {
                return 1f;
            }
            if (angleFrom > 0 && angleTo > 0)
            {
                return 0f;
            }
            return Mathf.Abs(angleFrom) / Vector3.Angle(fromForward, toForward);
        }

        private Vector3 NearestPointInTriangle(Vector3 point, Vector3 p0, Vector3 p1, Vector3 p2, out float t)
        {
            Vector3 centroid = (p0 + p1 + p2) / 3f;

            Vector3 pointInMedian0 = NearestPointToSegment(point, p0, centroid, out float t0);
            Vector3 pointInMedian1 = NearestPointToSegment(point, centroid, p2, out float t1);

            float median0 = Vector3.Distance(p0, centroid);
            float median2 = Vector3.Distance(p2, centroid);
            float alpha = median2 / (median0 + median2);

            float distance0 = (pointInMedian0 - point).sqrMagnitude;
            float distance1 = (pointInMedian1 - point).sqrMagnitude;
            if (distance0 < distance1)
            {
                t = t0 * alpha;
                return pointInMedian0;
            }
            else
            {
                t = alpha + t1 * (1f - alpha);
                return pointInMedian1;
            }
        }

        private Vector3 NearestPointToSegment(Vector3 point, Vector3 start, Vector3 end, out float progress)
        {
            Vector3 segment = end - start;
            Vector3 projection = Vector3.Project(point - start, segment.normalized);
            Vector3 pointInSegment;
            if (Vector3.Dot(segment, projection) <= 0)
            {
                pointInSegment = start;
                progress = 0;
            }
            else if (projection.sqrMagnitude >= segment.sqrMagnitude)
            {
                pointInSegment = end;
                progress = 1;
            }
            else
            {
                pointInSegment = start + projection;
                progress = projection.magnitude / segment.magnitude;
            }

            return pointInSegment;
        }

        public IGrabSurface CreateDuplicatedSurface(GameObject gameObject)
        {
            BezierGrabSurface surface = gameObject.AddComponent<BezierGrabSurface>();
            surface._controlPoints = new List<BezierControlPoint>(_controlPoints);
            return surface;
        }

        public IGrabSurface CreateMirroredSurface(GameObject gameObject)
        {
            BezierGrabSurface mirroredSurface = gameObject.AddComponent<BezierGrabSurface>();
            mirroredSurface._controlPoints = new List<BezierControlPoint>();
            foreach (BezierControlPoint controlPoint in _controlPoints)
            {
                Pose pose = controlPoint.GetPose(_relativeTo);
                pose.rotation = pose.rotation * Quaternion.Euler(180f, 180f, 0f);
                controlPoint.SetPose(pose, _relativeTo);
                mirroredSurface._controlPoints.Add(controlPoint);

            }
            return mirroredSurface;
        }

        public Pose MirrorPose(in Pose gripPose, Transform relativeTo)
        {
            return gripPose;
        }

        public static Vector3 EvaluateBezier(Vector3 start, Vector3 middle, Vector3 end, float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            return (oneMinusT * oneMinusT * start)
                + (2f * oneMinusT * t * middle)
                + (t * t * end);
        }

        #region Inject
        public void InjectAllBezierSurface(List<BezierControlPoint> controlPoints,
            Transform relativeTo)
        {
            InjectControlPoints(controlPoints);
            InjectRelativeTo(relativeTo);
        }

        public void InjectControlPoints(List<BezierControlPoint> controlPoints)
        {
            _controlPoints = controlPoints;
        }

        public void InjectRelativeTo(Transform relativeTo)
        {
            _relativeTo = relativeTo;
        }
        #endregion
    }

    [Serializable]
    public struct BezierControlPoint
    {
        [SerializeField]
        [FormerlySerializedAs("pose")]
        private Pose _pose;
        [SerializeField]
        [FormerlySerializedAs("tangentPoint")]
        private Vector3 _tangentPoint;
        [SerializeField]
        [FormerlySerializedAs("disconnected")]
        private bool _disconnected;

        public bool Disconnected
        {
            get
            {
                return _disconnected;
            }
            set
            {
                _disconnected = value;
            }
        }

        public Pose GetPose(Transform relativeTo)
        {
            return PoseUtils.GlobalPoseScaled(relativeTo, _pose);
        }

        public void SetPose(in Pose worldSpacePose, Transform relativeTo)
        {
            _pose = PoseUtils.DeltaScaled(relativeTo, worldSpacePose);
        }

        public Vector3 GetTangent(Transform relativeTo)
        {
            return relativeTo.TransformPoint(_tangentPoint);
        }

        public void SetTangent(in Vector3 tangent, Transform relativeTo)
        {
            _tangentPoint = relativeTo.InverseTransformPoint(tangent);
        }

    }
}
