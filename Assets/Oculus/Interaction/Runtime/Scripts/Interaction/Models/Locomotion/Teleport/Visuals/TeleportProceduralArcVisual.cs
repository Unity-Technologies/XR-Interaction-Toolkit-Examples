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

using Oculus.Interaction.DistanceReticles;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Oculus.Interaction.Locomotion
{
    public class TeleportProceduralArcVisual : MonoBehaviour
    {
        [SerializeField]
        private TeleportInteractor _interactor;

        [SerializeField]
        private TubeRenderer _tubeRenderer;

        [SerializeField, Optional]
        private PinchPointerVisual _pointer;
        [SerializeField, Optional]
        private Transform _pointerAnchor;

        [SerializeField, Optional, Interface(typeof(IAxis1D))]
        private UnityEngine.Object _progress;
        private IAxis1D Progress;

        [SerializeField, Min(2)]
        private int _arcPointsCount = 30;
        public int ArcPointsCount
        {
            get
            {
                return _arcPointsCount;
            }
            set
            {
                _arcPointsCount = value;
            }
        }

        [SerializeField]
        private Color _noDestinationTint = Color.red;
        public Color NoDestinationTint
        {
            get
            {
                return _noDestinationTint;
            }
            set
            {
                _noDestinationTint = value;
            }
        }

        private TubePoint[] _arcPoints;

        private static float[,] MIDPOINT_FACTOR = new float[,]
        {
            { -0.984807753f, 2.86f },
            { -0.8660254038f, 1.02f },
            { -0.6427876097f, 0.66f },
            { -0.3420201433f, 0.54f },
            { 0f, 0.5f },
            { 0.3420201433f, 0.53f },
            { 0.6427876097f, 0.65f },
            { 0.8660254038f, 1f },
            { 0.9396926208f, 1.45f },
            { 0.984807753f, 2.86f },
            { 0.9961946981f, 5.7f },
            { 0.9975640503f, 7.2f },
            { 0.9986295348f, 9.55f },
            { 0.999390827f, 14.31f },
            { 0.9998476952f, 28.65f }
        };

        private IReticleData _reticleData;

        protected bool _started;

        protected virtual void Awake()
        {
            Progress = _progress as IAxis1D;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_interactor, nameof(_interactor));
            this.AssertField(_tubeRenderer, nameof(_tubeRenderer));
            if (_progress != null)
            {
                this.AssertField(Progress, nameof(Progress));
            }
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _interactor.WhenPostprocessed += HandleInteractorPostProcessed;
                _interactor.WhenStateChanged += HandleInteractorStateChanged;
                _interactor.WhenInteractableSet.Action += HandleInteractableSet;
                _interactor.WhenInteractableUnset.Action += HandleInteractableUnset;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _interactor.WhenPostprocessed -= HandleInteractorPostProcessed;
                _interactor.WhenStateChanged -= HandleInteractorStateChanged;
                _interactor.WhenInteractableSet.Action -= HandleInteractableSet;
                _interactor.WhenInteractableUnset.Action -= HandleInteractableUnset;
            }
        }

        private void HandleInteractableSet(TeleportInteractable interactable)
        {
            if (interactable != null)
            {
                _reticleData = interactable.GetComponent<IReticleData>();
            }
        }

        private void HandleInteractableUnset(TeleportInteractable obj)
        {
            _reticleData = null;
        }

        private void HandleInteractorStateChanged(InteractorStateChangeArgs stateChange)
        {
            if (stateChange.NewState == InteractorState.Disabled)
            {
                _tubeRenderer.Hide();
            }
        }

        private void HandleInteractorPostProcessed()
        {
            if (_interactor.State == InteractorState.Disabled)
            {
                return;
            }

            Color tint = Color.white;
            if (_interactor.Interactable == null
                || !_interactor.Interactable.AllowTeleport)
            {
                tint = _noDestinationTint;
            }

            Vector3 target = _reticleData != null ?
                _reticleData.ProcessHitPoint(_interactor.ArcEnd.Point) :
                _interactor.ArcEnd.Point;

            UpdateVisualArcPoints(_interactor.ArcOrigin, target);

            _tubeRenderer.Tint = tint;
            _tubeRenderer.Progress = Progress != null ? Progress.Value() : 0f;
            _tubeRenderer.RenderTube(_arcPoints, Space.World);

            UpdatePointer(tint);
        }

        private void UpdatePointer(Color tint)
        {
            if (_pointer == null)
            {
                return;
            }
            _pointer.Tint = tint;
            Vector3 pos = _pointerAnchor != null ? _pointerAnchor.position : _interactor.ArcOrigin.position;
            Vector3 fadePos = _interactor.ArcEnd.Point;
            Quaternion rot = Quaternion.LookRotation(fadePos - pos);
            _pointer.SetPositionAndRotation(pos, rot);
        }

        private void UpdateVisualArcPoints(Pose origin, Vector3 target)
        {
            if (_arcPoints == null
                || _arcPoints.Length != ArcPointsCount)
            {
                _arcPoints = new TubePoint[ArcPointsCount];
            }

            float pitchDot = Vector3.Dot(origin.forward, Vector3.up);
            float controlPointFactor = CalculateMidpointFactor(pitchDot);
            float distance = Vector3.ProjectOnPlane(target - origin.position, Vector3.up).magnitude;
            Vector3 midPoint = origin.position + origin.forward * distance * controlPointFactor;

            Vector3 prevPosition = origin.position - origin.forward;
            Vector3 inverseScale = new Vector3(1f / this.transform.lossyScale.x,
                1f / this.transform.lossyScale.y,
                1f / this.transform.lossyScale.z);

            float totalDistance = 0f;
            for (int i = 0; i < ArcPointsCount; i++)
            {
                float t = i / (ArcPointsCount - 1f);
                Vector3 position = EvaluateBezierArc(origin.position, midPoint, target, t);
                Vector3 difference = (position - prevPosition);
                totalDistance += difference.magnitude;

                _arcPoints[i].position = Vector3.Scale(position, inverseScale);
                _arcPoints[i].rotation = Quaternion.LookRotation(difference.normalized);

                prevPosition = position;
            }

            for (int i = 1; i < ArcPointsCount; i++)
            {
                float segmentLenght = (_arcPoints[i - 1].position - _arcPoints[i].position).magnitude;
                _arcPoints[i].relativeLength = _arcPoints[i - 1].relativeLength + (segmentLenght / totalDistance);
            }
        }

        private static Vector3 EvaluateBezierArc(Vector3 start, Vector3 middle, Vector3 end, float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            return (oneMinusT * oneMinusT * start)
                + (2f * oneMinusT * t * middle)
                + (t * t * end);
        }

        private static float CalculateMidpointFactor(float pitchDot)
        {
            int lastSample = MIDPOINT_FACTOR.GetLength(0) - 1;
            for (int i = 0; i < lastSample; i++)
            {
                if (MIDPOINT_FACTOR[i, 0] <= pitchDot
                    && MIDPOINT_FACTOR[i + 1, 0] > pitchDot)
                {
                    return Interpolate(pitchDot, i, i + 1);
                }
            }

            if (MIDPOINT_FACTOR[0, 0] < pitchDot)
            {
                return Interpolate(pitchDot, 0, 1);
            }

            if (MIDPOINT_FACTOR[lastSample, 0] < pitchDot)
            {
                return Interpolate(pitchDot, lastSample - 1, lastSample);
            }

            float Interpolate(float angle, int fromIndex, int toIndex)
            {
                float t = Mathf.InverseLerp(MIDPOINT_FACTOR[fromIndex, 0], MIDPOINT_FACTOR[toIndex, 0], angle);
                return Mathf.LerpUnclamped(MIDPOINT_FACTOR[fromIndex, 1], MIDPOINT_FACTOR[toIndex, 1], t);
            }

            return 0.5f;
        }

        #region Inject

        public void InjectAllTeleportProceduralArcVisual(TeleportInteractor interactor)
        {
            InjectTeleportInteractor(interactor);
        }

        public void InjectTeleportInteractor(TeleportInteractor interactor)
        {
            _interactor = interactor;
        }

        public void InjectOptionalProgress(IAxis1D progress)
        {
            _progress = progress as UnityEngine.Object;
            Progress = progress;
        }


        public void InjectOptionalPointer(PinchPointerVisual pointer)
        {
            _pointer = pointer;
        }

        public void InjectOptionalPointerAnchor(Transform pointerAnchor)
        {
            _pointerAnchor = pointerAnchor;
        }

        #endregion
    }
}
