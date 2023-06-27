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

namespace Oculus.Interaction.DistanceReticles
{
    public abstract class DistantInteractionLineVisual : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IDistanceInteractor))]
        private UnityEngine.Object _distanceInteractor;
        public IDistanceInteractor DistanceInteractor { get; protected set; }

        [SerializeField]
        private float _visualOffset = 0.07f;
        public float VisualOffset
        {
            get
            {
                return _visualOffset;
            }
            set
            {
                _visualOffset = value;
            }
        }

        private Vector3[] _linePoints;

        [SerializeField]
        private bool _visibleDuringNormal;
        private IReticleData _target;

        [SerializeField]
        private int _numLinePoints = 20;
        protected int NumLinePoints => _numLinePoints;

        [SerializeField]
        private float _targetlessLength = 0.5f;
        protected float TargetlessLength => _targetlessLength;

        protected bool _started;
        private bool _shouldDrawLine;
        private DummyPointReticle _dummyTarget = new DummyPointReticle();

        private void Awake()
        {
            DistanceInteractor = _distanceInteractor as IDistanceInteractor;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(DistanceInteractor, nameof(DistanceInteractor));
            _linePoints = new Vector3[NumLinePoints];
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                DistanceInteractor.WhenStateChanged += HandleStateChanged;
                DistanceInteractor.WhenPostprocessed += HandlePostProcessed;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                DistanceInteractor.WhenStateChanged -= HandleStateChanged;
                DistanceInteractor.WhenPostprocessed -= HandlePostProcessed;
            }
        }

        private void HandleStateChanged(InteractorStateChangeArgs args)
        {
            switch (args.NewState)
            {
                case InteractorState.Normal:
                    if (args.PreviousState != InteractorState.Disabled)
                    {
                        InteractableUnset();
                    }

                    break;
                case InteractorState.Hover:
                    if (args.PreviousState == InteractorState.Normal)
                    {
                        InteractableSet(DistanceInteractor.DistanceInteractable);
                    }
                    break;
            }

            if (args.NewState == InteractorState.Select
                || args.NewState == InteractorState.Disabled
                || args.PreviousState == InteractorState.Disabled)
            {
                _shouldDrawLine = false;
            }
            else if (args.NewState == InteractorState.Hover)
            {
                _shouldDrawLine = true;
            }
            else if (args.NewState == InteractorState.Normal)
            {
                _shouldDrawLine = _visibleDuringNormal;
            }
        }

        private void HandlePostProcessed()
        {
            if (_shouldDrawLine)
            {
                UpdateLine();
            }
            else
            {
                HideLine();
            }
        }

        protected virtual void InteractableSet(IRelativeToRef interactable)
        {
            Component component = interactable as Component;
            if (component == null)
            {
                _target = null;
                return;
            }

            if (!component.TryGetComponent(out _target))
            {
                _dummyTarget.Target = interactable.RelativeTo;
                _target = _dummyTarget;
            }
        }

        protected virtual void InteractableUnset()
        {
            _target = null;
        }

        private void UpdateLine()
        {
            Vector3 direction = DistanceInteractor.Origin.forward;
            Vector3 origin = DistanceInteractor.Origin.position;
            Vector3 start = origin + direction * VisualOffset;
            Vector3 end = TargetHit(DistanceInteractor.HitPoint);
            Vector3 middle = start + direction * Vector3.Distance(start, end) * 0.5f;

            for (int i = 0; i < NumLinePoints; i++)
            {
                float t = i / (NumLinePoints - 1f);
                Vector3 point = EvaluateBezier(start, middle, end, t);
                _linePoints[i] = point;
            }

            RenderLine(_linePoints);
        }

        protected abstract void RenderLine(Vector3[] linePoints);
        protected abstract void HideLine();

        protected Vector3 TargetHit(Vector3 hitPoint)
        {
            if (_target != null)
            {
                return _target.ProcessHitPoint(hitPoint);
            }

            return DistanceInteractor.Origin.position
                + DistanceInteractor.Origin.forward * _targetlessLength;
        }

        protected static Vector3 EvaluateBezier(Vector3 start, Vector3 middle, Vector3 end, float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            return (oneMinusT * oneMinusT * start)
                + (2f * oneMinusT * t * middle)
                + (t * t * end);
        }

        private class DummyPointReticle : IReticleData
        {
            public Transform Target { get; set; }

            public Vector3 ProcessHitPoint(Vector3 hitPoint)
            {
                return Target.position;
            }
        }

        #region Inject

        public void InjectAllDistantInteractionLineVisual(IDistanceInteractor interactor)
        {
            InjectDistanceInteractor(interactor);
        }

        public void InjectDistanceInteractor(IDistanceInteractor interactor)
        {
            _distanceInteractor = interactor as UnityEngine.Object;
            DistanceInteractor = interactor;
        }

        #endregion
    }
}
