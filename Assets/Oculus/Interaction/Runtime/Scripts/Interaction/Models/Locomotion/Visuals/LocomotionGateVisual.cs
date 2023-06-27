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

using UnityEngine;

namespace Oculus.Interaction.Locomotion
{
    public class LocomotionGateVisual : MonoBehaviour
    {
        [SerializeField]
        private LocomotionGate _locomotionGate;
        [SerializeField]
        private Renderer _ring;
        [SerializeField]
        private TubeRenderer _ringTube;
        [SerializeField]
        private Transform _origin;
        [SerializeField]
        private float _radius;
        public float Radius
        {
            get
            {
                return _radius;
            }
            set
            {
                _radius = value;
            }
        }

        [SerializeField]
        private float _fadeGap = 0.1f;
        public float FadeGap
        {
            get
            {
                return _fadeGap;
            }
            set
            {
                _fadeGap = value;
            }
        }

        private const float _degreesPerSegment = 1f;
        private static readonly Quaternion _rotationCorrection = Quaternion.Euler(-90f, 0f, 0f);

        private TubePoint[] _teleportUpPoints;
        private TubePoint[] _teleportDownPoints;
        private TubePoint[] _turningPoints;

        private LocomotionGate.LocomotionMode _prevLocomotionMode = LocomotionGate.LocomotionMode.None;
        protected bool _started;

        private Vector2 TurnLimits
        {
            get
            {
                return new Vector2(
                    _locomotionGate.PalmUpToTurnThresholds.x,
                    _locomotionGate.TurnToPalmDownToThresholds.y);
            }
        }

        private Vector2 TeleportUpLimits
        {
            get
            {
                return new Vector2(
                    _locomotionGate.WristLimit,
                    _locomotionGate.PalmUpToTurnThresholds.y);
            }
        }

        private Vector2 TeleportDownLimits
        {
            get
            {
                return new Vector2(
                    _locomotionGate.TurnToPalmDownToThresholds.x,
                    _locomotionGate.WristLimit + 360f);
            }
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_locomotionGate, nameof(_locomotionGate));
            this.AssertField(_origin, nameof(_origin));
            this.AssertField(_ring, nameof(_ring));
            this.AssertField(_ringTube, nameof(_ringTube));

            Show(false);
            InitializeSegments();

            this.EndStart(ref _started);
        }

        protected virtual void LateUpdate()
        {
            if (_locomotionGate.ActiveMode == LocomotionGate.LocomotionMode.None)
            {
                Show(false);
            }
            else
            {
                Show(true);
                UpdatePosition();
                bool redraw = _prevLocomotionMode != _locomotionGate.ActiveMode;
                UpdateActiveSegment(redraw);
            }
            _prevLocomotionMode = _locomotionGate.ActiveMode;
        }

        private void Show(bool show)
        {
            _ring.enabled = show;
        }

        private void UpdateActiveSegment(bool redraw)
        {
            float circleLength = 2 * Mathf.PI * _radius;
            if (_locomotionGate.ActiveMode == LocomotionGate.LocomotionMode.Turn)
            {

                Vector2 limit = TurnLimits;
                float pointerPosStart = (_locomotionGate.CurrentAngle - limit.x) / 360f;
                float pointerPosEnd = (limit.y - _locomotionGate.CurrentAngle) / 360f;

                float gap = -_fadeGap - _ringTube.Feather;
                _ringTube.StartFadeThresold = circleLength * pointerPosStart + gap;
                _ringTube.EndFadeThresold = circleLength * pointerPosEnd + gap;
                _ringTube.InvertThreshold = true;
                if (redraw)
                {
                    _ringTube.RenderTube(_turningPoints);
                }
            }
            else if (_locomotionGate.ActiveMode == LocomotionGate.LocomotionMode.TeleportUp)
            {
                Vector2 limit = TeleportUpLimits;
                float pointerPos = (_locomotionGate.CurrentAngle - limit.x) / 360f;

                _ringTube.StartFadeThresold = circleLength * pointerPos + _fadeGap;
                _ringTube.EndFadeThresold = -100f;
                _ringTube.InvertThreshold = false;
                if (redraw)
                {
                    _ringTube.RenderTube(_teleportUpPoints);
                }
            }
            else if (_locomotionGate.ActiveMode == LocomotionGate.LocomotionMode.TeleportDown)
            {
                Vector2 limit = TeleportDownLimits;
                float pointerPos = (limit.y - _locomotionGate.CurrentAngle) / 360f;

                _ringTube.StartFadeThresold = -100f;
                _ringTube.EndFadeThresold = circleLength * pointerPos + _fadeGap;
                _ringTube.InvertThreshold = false;
                if (redraw)
                {
                    _ringTube.RenderTube(_teleportDownPoints);
                }
            }

            if (!redraw)
            {
                _ringTube.RedrawFadeThresholds();
            }
        }

        private void UpdatePosition()
        {
            float sign = _locomotionGate.Hand.Handedness == Input.Handedness.Right ? -1 : 1;
            Pose stabilizationPose = _locomotionGate.StabilizationPose;
            Vector3 projected = stabilizationPose.position
                + Vector3.Project(_origin.position - stabilizationPose.position, stabilizationPose.forward);
            Vector3 originDirection = -(projected - _origin.position).normalized;
            Vector3 position = _origin.position - originDirection * (_radius * this.transform.lossyScale.x);
            Quaternion offset = Quaternion.FromToRotation(_locomotionGate.WristDirection, originDirection);
            Quaternion rotation = Quaternion.LookRotation(sign * stabilizationPose.forward, offset * Vector3.up);

            this.transform.SetPositionAndRotation(position, rotation);
        }

        private void InitializeSegments()
        {
            _turningPoints = InitializeSegment(TurnLimits);
            _teleportUpPoints = InitializeSegment(TeleportUpLimits);
            _teleportDownPoints = InitializeSegment(TeleportDownLimits);
        }

        private TubePoint[] InitializeSegment(Vector2 minMax)
        {
            float lowLimit = minMax.x;
            float upLimit = minMax.y;
            int segments = Mathf.RoundToInt((upLimit - lowLimit) / _degreesPerSegment);
            TubePoint[] tubePoints = new TubePoint[segments];
            float segmentLenght = 1f / segments;
            for (int i = 0; i < segments; i++)
            {
                Quaternion rotation = Quaternion.AngleAxis(-i * _degreesPerSegment - lowLimit, Vector3.forward);
                tubePoints[i] = new TubePoint()
                {
                    position = rotation * Vector3.left * _radius,
                    rotation = rotation * _rotationCorrection,
                    relativeLength = i * segmentLenght
                };
            }
            return tubePoints;
        }

        #region Inject
        public void InjectAllLocomotionGateVisual(LocomotionGate locomotionGate,
            Transform origin, Renderer ring, TubeRenderer ringTube)
        {
            InjectLocomotionGate(locomotionGate);
            InjectOrigin(origin);
            InjectRing(ring);
            InjectRingTube(ringTube);
        }

        public void InjectLocomotionGate(LocomotionGate locomotionGate)
        {
            _locomotionGate = locomotionGate;
        }

        public void InjectOrigin(Transform origin)
        {
            _origin = origin;
        }

        public void InjectRing(Renderer ring)
        {
            _ring = ring;
        }

        public void InjectRingTube(TubeRenderer ringTube)
        {
            _ringTube = ringTube;
        }

        #endregion
    }
}
