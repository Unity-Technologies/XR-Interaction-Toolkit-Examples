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

using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction.Surfaces;
using System;
using UnityEngine.Serialization;

namespace Oculus.Interaction
{
    /// <summary>
    /// Defines a near-poke interaction that is driven by a near-distance
    /// proximity computation and a raycast between the position
    /// recorded across two frames against a target surface.
    /// </summary>
    public class PokeInteractor : PointerInteractor<PokeInteractor, PokeInteractable>
    {
        private class SurfaceHitCache
        {
            private readonly struct HitInfo
            {
                public readonly bool IsValid;
                public readonly SurfaceHit Hit;

                public HitInfo(bool isValid, SurfaceHit hit)
                {
                    IsValid = isValid;
                    Hit = hit;
                }
            }

            private Dictionary<PokeInteractable, HitInfo> _surfacePatchHitCache;
            private Dictionary<PokeInteractable, HitInfo> _backingSurfaceHitCache;
            private Vector3 _origin;

            public bool GetPatchHit(PokeInteractable interactable, out SurfaceHit hit)
            {
                if (!_surfacePatchHitCache.ContainsKey(interactable))
                {
                    bool isValid = interactable.SurfacePatch
                        .ClosestSurfacePoint(_origin, out SurfaceHit patchHit);
                    HitInfo info = new HitInfo(isValid, patchHit);
                    _surfacePatchHitCache.Add(interactable, info);
                }

                hit = _surfacePatchHitCache[interactable].Hit;
                return _surfacePatchHitCache[interactable].IsValid;
            }

            public bool GetBackingHit(PokeInteractable interactable, out SurfaceHit hit)
            {
                if (!_backingSurfaceHitCache.ContainsKey(interactable))
                {
                    bool isValid = interactable.SurfacePatch.BackingSurface
                        .ClosestSurfacePoint(_origin, out SurfaceHit backingHit);
                    HitInfo info = new HitInfo(isValid, backingHit);
                    _backingSurfaceHitCache.Add(interactable, info);
                }

                hit = _backingSurfaceHitCache[interactable].Hit;
                return _backingSurfaceHitCache[interactable].IsValid;
            }

            public SurfaceHitCache()
            {
                _surfacePatchHitCache = new Dictionary<PokeInteractable, HitInfo>();
                _backingSurfaceHitCache = new Dictionary<PokeInteractable, HitInfo>();
            }

            public void Reset(Vector3 origin)
            {
                _origin = origin;
                _surfacePatchHitCache.Clear();
                _backingSurfaceHitCache.Clear();
            }
        }

        [SerializeField]
        [Tooltip("The poke origin tracks the provided transform.")]
        private Transform _pointTransform;

        [SerializeField]
        [Tooltip("(Meters, World) The radius of the sphere positioned at the origin.")]
        private float _radius = 0.005f;

        [SerializeField]
        [Tooltip("(Meters, World) A poke unselect fires when the poke origin surpasses this " +
                 "distance above a surface.")]
        private float _touchReleaseThreshold = 0.002f;

        [FormerlySerializedAs("_zThreshold")]
        [SerializeField]
        [Tooltip("(Meters, World) The threshold below which distances to a surface " +
                 "will use tiebreaker score to decide candidate.")]
        private float _equalDistanceThreshold = 0.001f;

        public Vector3 ClosestPoint { get; private set; }

        public Vector3 TouchPoint { get; private set; }
        public Vector3 TouchNormal { get; private set; }

        public float Radius => _radius;

        public Vector3 Origin { get; private set; }

        private Vector3 _previousPokeOrigin;

        private PokeInteractable _previousCandidate = null;
        private PokeInteractable _hitInteractable = null;
        private PokeInteractable _recoilInteractable = null;

        private Vector3 _previousSurfacePointLocal;
        private Vector3 _firstTouchPointLocal;
        private Vector3 _targetTouchPointLocal;
        private Vector3 _easeTouchPointLocal;

        private bool _isRecoiled;
        private bool _isDragging;
        private ProgressCurve _dragEaseCurve;
        private ProgressCurve _pinningResyncCurve;
        private Vector3 _dragCompareSurfacePointLocal;
        private float _maxDistanceFromFirstTouchPoint;

        private float _recoilVelocityExpansion;
        private float _selectMaxDepth;
        private float _reEnterDepth;
        private float _lastUpdateTime;

        private Func<float> _timeProvider;

        private bool _isPassedSurface;
        public bool IsPassedSurface
        {
            get
            {
                return _isPassedSurface;
            }
            set
            {
                bool previousValue = _isPassedSurface;
                _isPassedSurface = value;
                if (value != previousValue)
                {
                    WhenPassedSurfaceChanged.Invoke(value);
                }
            }
        }

        public Action<bool> WhenPassedSurfaceChanged = delegate {};
        private SurfaceHitCache _hitCache;

        private Dictionary<PokeInteractable, Matrix4x4> _previousSurfaceTransformMap;
        private float _previousDragCurveProgress;
        private float _previousPinningCurveProgress;

        protected override void Awake()
        {
            base.Awake();
            _timeProvider = () => Time.time;
            _nativeId = 0x506f6b6549746f72;
        }

        protected override void Start()
        {
            base.Start();
            this.AssertField(_pointTransform, nameof(_pointTransform));
            this.AssertField(_timeProvider, nameof(_timeProvider));
            _dragEaseCurve = new ProgressCurve();
            _pinningResyncCurve = new ProgressCurve();
            _hitCache = new SurfaceHitCache();
            _previousSurfaceTransformMap = new Dictionary<PokeInteractable, Matrix4x4>();
        }

        protected override void DoPreprocess()
        {
            base.DoPreprocess();
            _previousPokeOrigin = Origin;
            Origin = _pointTransform.position;
            _hitCache.Reset(Origin);
        }

        protected override void DoPostprocess()
        {
            base.DoPostprocess();
            var interactables = PokeInteractable.Registry.List(this);
            foreach (PokeInteractable interactable in interactables)
            {
                _previousSurfaceTransformMap[interactable] =
                    interactable.SurfacePatch.BackingSurface.Transform.worldToLocalMatrix;
            }
            _lastUpdateTime = _timeProvider();
        }

        protected override bool ComputeShouldSelect()
        {
            if (_recoilInteractable != null)
            {
                float depth = ComputeDepth(_recoilInteractable, Origin);
                _reEnterDepth = Mathf.Min(depth + _recoilInteractable.RecoilAssist.ReEnterDistance, _reEnterDepth);
                _hitInteractable = depth > _reEnterDepth ? _recoilInteractable : null;
            }

            return _hitInteractable != null;
        }

        protected override bool ComputeShouldUnselect()
        {
            return _hitInteractable == null;
        }

        private bool GetBackingHit(PokeInteractable interactable, out SurfaceHit hit)
        {
            return _hitCache.GetBackingHit(interactable, out hit);
        }

        private bool GetPatchHit(PokeInteractable interactable, out SurfaceHit hit)
        {
            return _hitCache.GetPatchHit(interactable, out hit);
        }

        private bool InteractableInRange(PokeInteractable interactable)
        {
            if (!_previousSurfaceTransformMap.ContainsKey(interactable))
            {
                return true; // Cannot determine without previous surface data
            }

            Vector3 previousLocalPokeOrigin = _previousSurfaceTransformMap[interactable]
                .MultiplyPoint(_previousPokeOrigin);
            Vector3 adjustedPokeOrigin = interactable.SurfacePatch.BackingSurface.Transform
                .TransformPoint(previousLocalPokeOrigin);

            float hoverDistance = interactable == Interactable ?
                Mathf.Max(interactable.ExitHoverTangent, interactable.ExitHoverNormal) :
                Mathf.Max(interactable.EnterHoverTangent, interactable.EnterHoverNormal);

            float moveDistance = Vector3.Distance(Origin, adjustedPokeOrigin);
            float maxDistance = moveDistance + Radius + hoverDistance +
                    _equalDistanceThreshold + interactable.CloseDistanceThreshold;

            return interactable.SurfacePatch.ClosestSurfacePoint(Origin, out _, maxDistance);
        }

        protected override void DoHoverUpdate()
        {
            if (_interactable != null &&
                GetBackingHit(_interactable, out SurfaceHit backingHit))
            {
                TouchPoint = backingHit.Point;
                TouchNormal = backingHit.Normal;
            }

            if (_recoilInteractable != null)
            {
                bool withinSurface = SurfaceUpdate(_recoilInteractable);
                if (!withinSurface)
                {
                    _isRecoiled = false;
                    _recoilInteractable = null;
                    _recoilVelocityExpansion = 0;
                    IsPassedSurface = false;
                    return;
                }

                if (ShouldCancel(_recoilInteractable))
                {
                    GeneratePointerEvent(PointerEventType.Cancel, _recoilInteractable);
                    _previousPokeOrigin = Origin;
                    _previousCandidate = null;
                    _hitInteractable = null;
                    _recoilInteractable = null;
                    _recoilVelocityExpansion = 0;
                    IsPassedSurface = false;
                    _isRecoiled = false;
                }
            }
        }

        protected override PokeInteractable ComputeCandidate()
        {
            if (_recoilInteractable != null)
            {
                return _recoilInteractable;
            }

            if (_hitInteractable != null)
            {
                return _hitInteractable;
            }

            // First, see if we trigger a press on any interactable
            PokeInteractable closestInteractable = ComputeSelectCandidate();
            if (closestInteractable != null)
            {
                // We have found an active hit target, so we return it
                _hitInteractable = closestInteractable;
                _previousCandidate = closestInteractable;
                return _hitInteractable;
            }

            // Otherwise we have no active interactable, so we do a proximity-only check for
            // closest hovered interactable (above the surface)
            closestInteractable = ComputeHoverCandidate();
            _previousCandidate = closestInteractable;

            return closestInteractable;
        }

        protected override int ComputeCandidateTiebreaker(PokeInteractable a, PokeInteractable b)
        {
            int result = base.ComputeCandidateTiebreaker(a, b);
            if (result != 0)
            {
                return result;
            }

            return a.TiebreakerScore.CompareTo(b.TiebreakerScore);
        }

        private PokeInteractable ComputeSelectCandidate()
        {
            PokeInteractable closestInteractable = null;
            float closestNormalDistance = float.MaxValue;
            float closestTangentDistance = float.MaxValue;

            var interactables = PokeInteractable.Registry.List(this);

            // Check the surface first as a movement through this will
            // automatically put us in a "active" state. We expect the raycast
            // to happen only in one direction
            foreach (PokeInteractable interactable in interactables)
            {
                if (!InteractableInRange(interactable) ||
                    !GetBackingHit(interactable, out SurfaceHit backingHit) ||
                    !GetPatchHit(interactable, out SurfaceHit patchHit))
                {
                    continue;
                }

                Matrix4x4 previousSurfaceMatrix =
                    _previousSurfaceTransformMap.ContainsKey(interactable)
                        ? _previousSurfaceTransformMap[interactable]
                        : interactable.SurfacePatch.BackingSurface.Transform.worldToLocalMatrix;

                Vector3 localPokeOrigin = previousSurfaceMatrix.MultiplyPoint(_previousPokeOrigin);
                Vector3 adjustedPokeOrigin =
                    interactable.SurfacePatch.BackingSurface.Transform.TransformPoint(localPokeOrigin);

                if (!PassesEnterHoverDistanceCheck(adjustedPokeOrigin, interactable))
                {
                    continue;
                }

                Vector3 moveDirection = Origin - adjustedPokeOrigin;
                float magnitude = moveDirection.magnitude;
                if (magnitude == 0f)
                {
                    continue;
                }

                moveDirection /= magnitude;
                Ray ray = new Ray(adjustedPokeOrigin, moveDirection);

                Vector3 closestSurfaceNormal = backingHit.Normal;

                // First check that we are moving towards the surface by checking
                // the direction of our position delta with the forward direction of the surface normal.
                // This is to not allow presses from "behind" the surface.

                // Check if we are moving toward the surface
                if (Vector3.Dot(moveDirection, closestSurfaceNormal) < 0f)
                {
                    // Then do a raycast against the surface
                    bool hit = interactable.SurfacePatch.BackingSurface.Raycast(ray, out SurfaceHit surfaceHit);
                    hit = hit && surfaceHit.Distance <= magnitude;

                    if (!hit)
                    {
                        // We may still be touching the surface within our radius
                        float distance = ComputeDistanceAbove(interactable, Origin);
                        if (distance <= 0)
                        {
                            Vector3 closestSurfacePointToOrigin = backingHit.Point;
                            hit = true;
                            surfaceHit = new SurfaceHit()
                            {
                                Point = closestSurfacePointToOrigin,
                                Normal = backingHit.Normal,
                                Distance = distance
                            };
                        }
                    }

                    if (hit)
                    {
                        float tangentDistance =
                            ComputeTangentDistance(interactable, surfaceHit.Point);

                        // Check if our collision lies outside of the max distance in the proximityfield
                        if (tangentDistance >
                           (interactable != _previousCandidate ?
                               interactable.EnterHoverTangent :
                               interactable.ExitHoverTangent))
                        {
                            continue;
                        }

                        // We collided against the surface and now we must rank this
                        // interactable versus others that also pass this test this frame.

                        // First we rank by normal distance traveled,
                        // and secondly by closer proximity
                        float normalDistance = Vector3.Dot(adjustedPokeOrigin - surfaceHit.Point, surfaceHit.Normal);
                        bool normalDistanceEqual = Mathf.Abs(normalDistance - closestNormalDistance) < _equalDistanceThreshold;

                        if (normalDistanceEqual)
                        {
                            if (ComputeCandidateTiebreaker(interactable, closestInteractable) > 0)
                            {
                                closestNormalDistance = normalDistance;
                                closestTangentDistance = tangentDistance;
                                closestInteractable = interactable;
                                continue;
                            }
                        }

                        if (normalDistance > closestNormalDistance + interactable.CloseDistanceThreshold)
                        {
                            continue;
                        }

                        if (closestInteractable == null ||
                            normalDistance < closestNormalDistance - closestInteractable.CloseDistanceThreshold)
                        {
                            closestNormalDistance = normalDistance;
                            closestTangentDistance = tangentDistance;
                            closestInteractable = interactable;
                            continue;
                        }

                        if(tangentDistance < closestTangentDistance)
                        {
                            closestNormalDistance = normalDistance;
                            closestTangentDistance = tangentDistance;
                            closestInteractable = interactable;
                        }
                    }
                }
            }

            if (closestInteractable != null)
            {
                GetBackingHit(closestInteractable, out SurfaceHit backingHitClosest);
                GetPatchHit(closestInteractable, out SurfaceHit patchHitClosest);

                ClosestPoint = patchHitClosest.Point;
                TouchPoint = backingHitClosest.Point;
                TouchNormal = backingHitClosest.Normal;

                // Once we have a select interactable, we need to check for the existence
                // of better future select candidates that are within our close distance threshold
                // If they exist, then this select interactable should be ignored.

                // We again run through all the other interactables
                foreach (PokeInteractable interactable in interactables)
                {
                    if (interactable == closestInteractable)
                    {
                        continue;
                    }

                    if (!InteractableInRange(interactable) ||
                        !GetBackingHit(interactable, out SurfaceHit backingHit) ||
                        !GetPatchHit(interactable, out SurfaceHit patchHit))
                    {
                        continue;
                    }

                    Matrix4x4 previousSurfaceMatrix =
                        _previousSurfaceTransformMap.ContainsKey(interactable)
                            ? _previousSurfaceTransformMap[interactable]
                            : interactable.SurfacePatch.BackingSurface.Transform.worldToLocalMatrix;

                    Vector3 localPokeOrigin =
                        previousSurfaceMatrix.MultiplyPoint(_previousPokeOrigin);
                    Vector3 adjustedPokeOrigin =
                        interactable.SurfacePatch.BackingSurface.Transform.TransformPoint(
                            localPokeOrigin);

                    if (!PassesEnterHoverDistanceCheck(adjustedPokeOrigin, interactable))
                    {
                        continue;
                    }

                    // Check the distance from the backingHit point to the touchpoint
                    // projected onto the interactable normal to see if its not within
                    // the close distance threshold
                    Vector3 backingToTouchPoint = TouchPoint - backingHit.Point;
                    float normalDistance = Vector3.Dot(backingToTouchPoint, backingHit.Normal);

                    if (Mathf.Abs(normalDistance) < _equalDistanceThreshold)
                    {
                        if (ComputeCandidateTiebreaker(closestInteractable, interactable) > 0)
                        {
                            continue;
                        }
                    }

                    if (normalDistance <= 0 || normalDistance > interactable.CloseDistanceThreshold)
                    {
                        continue;
                    }

                    float tangentDistance = ComputeTangentDistance(interactable, TouchPoint);

                    // Compute the tangent distance from the backing hit point
                    // to check if its not within the hover tangent distance
                    if (tangentDistance > interactable.EnterHoverTangent)
                    {
                        continue;
                    }

                    // Check that the tangent distance is not larger than the selected closest dist
                    if (tangentDistance > closestTangentDistance)
                    {
                        continue;
                    }

                    // This is a closer interactable, clear the select interactable
                    return null;
                }
            }

            return closestInteractable;
        }

        private bool PassesEnterHoverDistanceCheck(Vector3 position, PokeInteractable interactable)
        {
            if (interactable == _previousCandidate)
            {
                return true;
            }

            float distanceThreshold = 0f;
            if (interactable.MinThresholds.Enabled)
            {
                distanceThreshold = Mathf.Min(interactable.MinThresholds.MinNormal,
                    MinPokeDepth(interactable));
            }

            return ComputeDistanceAbove(interactable, position) > distanceThreshold;
        }

        public float MinPokeDepth(PokeInteractable interactable)
        {
            float minDepth = interactable.ExitHoverNormal;
            foreach (PokeInteractor pokeInteractor in interactable.Interactors)
            {
                // Scalar project the poke interactor's position onto the button base's normal vector
                float normalDistance = ComputeDepth(interactable, pokeInteractor.Origin);
                minDepth = Mathf.Min(normalDistance, minDepth);
            }

            return minDepth;
        }

        private PokeInteractable ComputeHoverCandidate()
        {
            PokeInteractable closestInteractable = null;
            float closestNormalDistance = float.MaxValue;
            float closestTangentDistance = float.MaxValue;

            var interactables = PokeInteractable.Registry.List(this);

            // We check that we're above the surface first as we don't
            // care about hovers that originate below the surface
            foreach (PokeInteractable interactable in interactables)
            {
                if (!InteractableInRange(interactable) ||
                    !GetBackingHit(interactable, out SurfaceHit backingHit) ||
                    !GetPatchHit(interactable, out SurfaceHit patchHit))
                {
                    continue;
                }

                // First check that above EnterHover and within HoverZThreshold
                // Or if above EnterHover last frame and within HoverZThreshold this frame:
                // eg. if EnterHover and HoverZThreshold are equal, still want to hover in one frame
                if (!PassesEnterHoverDistanceCheck(Origin, interactable) &&
                    !PassesEnterHoverDistanceCheck(_previousPokeOrigin, interactable))
                {
                    continue;
                }

                Vector3 closestSurfacePoint = backingHit.Point;
                Vector3 closestSurfaceNormal = backingHit.Normal;

                Vector3 surfaceToPoint = Origin - closestSurfacePoint;
                float magnitude = surfaceToPoint.magnitude;
                if (magnitude != 0f)
                {
                    // Check if our position is above the surface
                    if (Vector3.Dot(surfaceToPoint, closestSurfaceNormal) > 0f)
                    {
                        float normalDistance = ComputeDistanceAbove(interactable, Origin);
                        if (normalDistance >
                            (_previousCandidate != interactable ?
                                interactable.EnterHoverNormal :
                                interactable.ExitHoverNormal))
                        {
                            continue;
                        }

                        float tangentDistance = ComputeTangentDistance(interactable, Origin);
                        if (tangentDistance >
                            (_previousCandidate != interactable ?
                                interactable.EnterHoverTangent :
                                interactable.ExitHoverTangent))
                        {
                            continue;
                        }

                        // We're above the surface so now we must rank this
                        // interactable versus others that also pass this test this frame
                        // but may be at a closer proximity.

                        bool normalDistanceEqual = Mathf.Abs(normalDistance - closestNormalDistance) < _equalDistanceThreshold;

                        // If within the equal distance threshold
                        if (normalDistanceEqual)
                        {
                            // Select this interactable if its tiebreakerscore is highest
                            if (closestInteractable != null && ComputeCandidateTiebreaker(interactable, closestInteractable) > 0)
                            {
                                closestInteractable = interactable;
                                closestNormalDistance = normalDistance;
                                closestTangentDistance = tangentDistance;
                                continue;
                            }
                        }

                        // If normal distance is greater than closest normal distance by over closeDistanceThreshold
                        if (normalDistance > closestNormalDistance + interactable.CloseDistanceThreshold)
                        {
                            continue;
                        }

                        // If normal distance is less than closest normal distance by over closeDistanceThreshold
                        // of the best closest interactable's close distance threshold
                        if(closestInteractable == null || normalDistance < closestNormalDistance -
                            closestInteractable.CloseDistanceThreshold)
                        {
                            closestInteractable = interactable;
                            closestNormalDistance = normalDistance;
                            closestTangentDistance = tangentDistance;
                            continue;
                        }

                        // Normal distance is within closeDistanceThreshold of the closestNormalDistance
                        // Pick the candidate based on tangent distance
                        if (tangentDistance < closestTangentDistance)
                        {
                            closestInteractable = interactable;
                            closestNormalDistance = normalDistance;
                            closestTangentDistance = tangentDistance;
                        }
                    }
                }
            }

            if (closestInteractable != null)
            {
                GetBackingHit(closestInteractable, out SurfaceHit backingHitClosest);
                GetPatchHit(closestInteractable, out SurfaceHit patchHitClosest);

                ClosestPoint = patchHitClosest.Point;
                TouchPoint = backingHitClosest.Point;
                TouchNormal = backingHitClosest.Normal;
            }

            return closestInteractable;
        }

        protected override void InteractableSelected(PokeInteractable interactable)
        {
            if (interactable != null && GetBackingHit(interactable, out SurfaceHit backingHit))
            {
                _previousSurfacePointLocal =
                _firstTouchPointLocal =
                _easeTouchPointLocal =
                _targetTouchPointLocal =
                interactable.SurfacePatch.BackingSurface.Transform.InverseTransformPoint(TouchPoint);

                Vector3 lateralComparePoint = backingHit.Point;
                _dragCompareSurfacePointLocal = interactable.SurfacePatch.BackingSurface.Transform.InverseTransformPoint(lateralComparePoint);
                _dragEaseCurve.Copy(interactable.DragThresholds.DragEaseCurve);
                _pinningResyncCurve.Copy(interactable.PositionPinning.ResyncCurve);
                _isDragging = false;
                _isRecoiled = false;

                _maxDistanceFromFirstTouchPoint = 0;
                _selectMaxDepth = 0;
            }

            IsPassedSurface = true;
            base.InteractableSelected(interactable);
        }

        protected override void HandleDisabled()
        {
            _hitInteractable = null;
            base.HandleDisabled();
        }

        protected override Pose ComputePointerPose()
        {
            if (Interactable == null)
            {
                return Pose.identity;
            }

            if (!Interactable.ClosestBackingSurfaceHit(TouchPoint, out SurfaceHit hit))
            {
                return Pose.identity;
            }

            return new Pose(TouchPoint, Quaternion.LookRotation(hit.Normal));
        }

        // The distance above a surface along the closest normal.
        // Returns 0 for where the sphere touches the surface along the normal.
        private float ComputeDistanceAbove(PokeInteractable interactable, Vector3 point)
        {
            interactable.ClosestBackingSurfaceHit(point, out SurfaceHit hit);
            Vector3 surfaceToPoint = point - hit.Point;
            return Vector3.Dot(surfaceToPoint, hit.Normal) - _radius;
        }

        // The distance below a surface along the closest normal. Always positive.
        public float ComputeDepth(PokeInteractable interactable, Vector3 point)
        {
            return Mathf.Max(0f, -ComputeDistanceAbove(interactable, point));
        }

        // The distance from the closest point as computed by the proximity field and surface.
        // Returns the distance to the point without taking into account the surface normal.
        private float ComputeDistanceFrom(PokeInteractable interactable, Vector3 point)
        {
            interactable.ClosestSurfacePatchHit(point, out SurfaceHit hit);
            Vector3 surfaceToPoint = point - hit.Point;
            return surfaceToPoint.magnitude - _radius;
        }

        private float ComputeTangentDistance(PokeInteractable interactable, Vector3 point)
        {
            interactable.ClosestSurfacePatchHit(point, out SurfaceHit patchHit);
            interactable.ClosestBackingSurfaceHit(point, out SurfaceHit backingHit);
            Vector3 proximityToPoint = point - patchHit.Point;
            Vector3 projOnNormal = Vector3.Dot(proximityToPoint, backingHit.Normal) *
                backingHit.Normal;
            Vector3 lateralVec = proximityToPoint - projOnNormal;
            return lateralVec.magnitude - _radius;
        }

        // Returns if poke origin is still considered to be within the surface.
        protected virtual bool SurfaceUpdate(PokeInteractable interactable)
        {
            if (interactable == null)
            {
                return false;
            }

            if (!GetBackingHit(interactable, out SurfaceHit backingHit))
            {
                return false;
            }

            // Unselect if the interactor is above the surface by at least _touchReleaseThreshold
            if (ComputeDistanceAbove(interactable, Origin) > _touchReleaseThreshold)
            {
                return false;
            }

            bool wasRecoiled = _isRecoiled;
            _isRecoiled = _hitInteractable == null && _recoilInteractable != null;

            Vector3 closestSurfacePoint = backingHit.Point;

            Vector3 positionOnSurfaceLocal =
                interactable.SurfacePatch.BackingSurface.Transform.InverseTransformPoint(closestSurfacePoint);

            if (interactable.DragThresholds.Enabled)
            {
                float worldDepthDelta = Mathf.Abs(ComputeDepth(interactable, Origin) -
                                              ComputeDepth(interactable, _previousPokeOrigin));
                Vector3 positionDeltaLocal = positionOnSurfaceLocal - _previousSurfacePointLocal;
                Vector3 positionDeltaWorld =
                    interactable.SurfacePatch.BackingSurface.Transform.TransformVector(positionDeltaLocal);

                bool isZMotion = worldDepthDelta > positionDeltaWorld.magnitude &&
                                 worldDepthDelta > interactable.DragThresholds.DragNormal;

                if (isZMotion)
                {
                    _dragCompareSurfacePointLocal = positionOnSurfaceLocal;
                }

                if (!_isDragging)
                {
                    if (!isZMotion)
                    {
                        Vector3 surfaceDeltaLocal =
                            positionOnSurfaceLocal - _dragCompareSurfacePointLocal;
                        Vector3 surfaceDeltaWorld =
                            interactable.SurfacePatch.BackingSurface.Transform.TransformVector(surfaceDeltaLocal);
                        if (surfaceDeltaWorld.magnitude >
                            interactable.DragThresholds.DragTangent)
                        {
                            _isDragging = true;
                            _dragEaseCurve.Start();
                            _previousDragCurveProgress = 0;
                            _targetTouchPointLocal = positionOnSurfaceLocal;
                        }
                    }
                }
                else
                {
                    if (isZMotion)
                    {
                        _isDragging = false;
                    }
                    else
                    {
                        _targetTouchPointLocal = positionOnSurfaceLocal;
                    }
                }
            }
            else
            {
                _targetTouchPointLocal = positionOnSurfaceLocal;
            }

            Vector3 pinnedTouchPointLocal = _targetTouchPointLocal;
            if (interactable.PositionPinning.Enabled)
            {
                if (!_isRecoiled)
                {
                    Vector3 deltaFromCaptureLocal = pinnedTouchPointLocal - _firstTouchPointLocal;
                    Vector3 deltaFromCaptureWorld =
                        interactable.SurfacePatch.BackingSurface.Transform.TransformVector(deltaFromCaptureLocal);
                    _maxDistanceFromFirstTouchPoint = Mathf.Max(deltaFromCaptureWorld.magnitude, _maxDistanceFromFirstTouchPoint);

                    float deltaAsPercent = 1;
                    if (interactable.PositionPinning.MaxPinDistance != 0f)
                    {
                        deltaAsPercent = Mathf.Clamp01(_maxDistanceFromFirstTouchPoint / interactable.PositionPinning.MaxPinDistance);
                        deltaAsPercent = interactable.PositionPinning.PinningEaseCurve.Evaluate(deltaAsPercent);
                    }

                    pinnedTouchPointLocal = _firstTouchPointLocal + deltaFromCaptureLocal * deltaAsPercent;
                }
                else
                {
                    if (!wasRecoiled) // Entered recoil, begin resync
                    {
                        _pinningResyncCurve.Start();
                        _previousPinningCurveProgress = 0;
                    }

                    // Apply Pinning Resync Curve
                    float pinningCurveProgress = _pinningResyncCurve.Progress();
                    if (pinningCurveProgress != 1f)
                    {
                        float deltaProgress = pinningCurveProgress - _previousPinningCurveProgress;
                        Vector3 delta = pinnedTouchPointLocal - _easeTouchPointLocal;
                        pinnedTouchPointLocal = _easeTouchPointLocal + deltaProgress / (1f - _previousPinningCurveProgress) * delta;
                        _previousPinningCurveProgress = pinningCurveProgress;
                    }
                }
            }

            // Apply Drag Curve
            float dragCurveProgress = _dragEaseCurve.Progress();
            if (dragCurveProgress != 1f)
            {
                float deltaProgress = dragCurveProgress - _previousDragCurveProgress;
                Vector3 delta = pinnedTouchPointLocal - _easeTouchPointLocal;
                _easeTouchPointLocal += deltaProgress / (1f - _previousDragCurveProgress) * delta;
                _previousDragCurveProgress = dragCurveProgress;
            }
            else
            {
                _easeTouchPointLocal = pinnedTouchPointLocal;
            }

            TouchPoint =
                interactable.SurfacePatch.BackingSurface.Transform.TransformPoint(_easeTouchPointLocal);
            interactable.ClosestBackingSurfaceHit(TouchPoint, out SurfaceHit hit);
            TouchNormal = hit.Normal;

            _previousSurfacePointLocal = positionOnSurfaceLocal;

            return true;
        }

        protected virtual bool ShouldCancel(PokeInteractable interactable)
        {
            if ((interactable.CancelSelectNormal > 0.0f &&
                ComputeDepth(interactable, Origin) >
                interactable.CancelSelectNormal) ||
                (interactable.CancelSelectTangent > 0.0f &&
                ComputeTangentDistance(interactable, Origin) >
                interactable.CancelSelectTangent))
            {
                return true;
            }

            return false;
        }

        protected virtual bool ShouldRecoil(PokeInteractable interactable)
        {
            if (!interactable.RecoilAssist.Enabled)
            {
                return false;
            }

            float depth = ComputeDepth(interactable, Origin);
            float deltaTime = _timeProvider() - _lastUpdateTime;
            float recoilExitDistance = interactable.RecoilAssist.ExitDistance;

            if (interactable.RecoilAssist.UseVelocityExpansion)
            {
                Vector3 frameDeltaWorld = Origin - _previousPokeOrigin;
                float normalVelocity = Mathf.Max(0, Vector3.Dot(frameDeltaWorld, -TouchNormal));
                normalVelocity = deltaTime > 0 ? normalVelocity / deltaTime : 0f;

                float adjustment = Mathf.InverseLerp(
                    interactable.RecoilAssist.VelocityExpansionMinSpeed,
                    interactable.RecoilAssist.VelocityExpansionMaxSpeed,
                    normalVelocity);

                float targetRecoilVelocityExpansion = Mathf.Clamp01(adjustment) *
                    interactable.RecoilAssist.VelocityExpansionDistance;

                if (targetRecoilVelocityExpansion > _recoilVelocityExpansion)
                {
                    _recoilVelocityExpansion = targetRecoilVelocityExpansion;
                }
                else
                {
                    float decayRate = interactable.RecoilAssist.VelocityExpansionDecayRate * deltaTime;
                    _recoilVelocityExpansion = Math.Max(targetRecoilVelocityExpansion,
                        _recoilVelocityExpansion - decayRate);
                }

                recoilExitDistance += _recoilVelocityExpansion;
            }

            if (depth > _selectMaxDepth)
            {
                _selectMaxDepth = depth;
            }
            else
            {
                if (interactable.RecoilAssist.UseDynamicDecay)
                {
                    // 1. Compute normalRatio to determine the portion of movement normal to the touch
                    //      This is the 'dynamic' part.
                    // 2. Determine a decayFactor as a function of normalRatio.
                    //      Typically higher normalRatio would mean less decay, while more tangent movement gives more decay
                    //      The goal here is to prevent premature recoil while scrolling, since your finger will naturally make an arc
                    // 3. Adjust _selectMaxDepth towards the touch depth proportional to decayFactor
                    //      This is intended to make it so you have to recoil more/faster before we unselect the touch.

                    Vector3 frameDeltaWorld = Origin - _previousPokeOrigin;
                    Vector3 normalDeltaWorld = Vector3.Project(frameDeltaWorld, TouchNormal);
                    float normalRatio = frameDeltaWorld.sqrMagnitude > 0.0000001f ? normalDeltaWorld.magnitude / frameDeltaWorld.magnitude : 1f;
                    float decayFactor = interactable.RecoilAssist.DynamicDecayCurve.Evaluate(normalRatio);
                    _selectMaxDepth = Mathf.Lerp(_selectMaxDepth, depth, decayFactor * deltaTime);
                }

                if (depth < _selectMaxDepth - recoilExitDistance)
                {
                    _reEnterDepth = depth + interactable.RecoilAssist.ReEnterDistance;
                    return true;
                }
            }

            return false;
        }

        protected override void DoSelectUpdate()
        {
            bool withinSurface = SurfaceUpdate(_selectedInteractable);
            if (!withinSurface)
            {
                _hitInteractable = null;
                IsPassedSurface = _recoilInteractable != null;
                return;
            }

            if (ShouldCancel(_selectedInteractable))
            {
                GeneratePointerEvent(PointerEventType.Cancel, _selectedInteractable);
                _previousPokeOrigin = Origin;
                _previousCandidate = null;
                _hitInteractable = null;
                _recoilInteractable = null;
                _recoilVelocityExpansion = 0;
                IsPassedSurface = false;
                _isRecoiled = false;
                return;
            }

            if (ShouldRecoil(_selectedInteractable))
            {
                _hitInteractable = null;
                _recoilInteractable = _selectedInteractable;
                _selectMaxDepth = 0;
            }
        }

        #region Inject

        public void InjectAllPokeInteractor(Transform pointTransform, float radius = 0.005f)
        {
            InjectPointTransform(pointTransform);
            InjectRadius(radius);
        }

        public void InjectPointTransform(Transform pointTransform)
        {
            _pointTransform = pointTransform;
        }

        public void InjectRadius(float radius)
        {
            _radius = radius;
        }

        public void InjectOptionalTouchReleaseThreshold(float touchReleaseThreshold)
        {
            _touchReleaseThreshold = touchReleaseThreshold;
        }

        public void InjectOptionalEqualDistanceThreshold(float equalDistanceThreshold)
        {
            _equalDistanceThreshold = equalDistanceThreshold;
        }

        public void InjectOptionalTimeProvider(Func<float> timeProvider)
        {
            _timeProvider = timeProvider;
        }

        #endregion
    }
}
