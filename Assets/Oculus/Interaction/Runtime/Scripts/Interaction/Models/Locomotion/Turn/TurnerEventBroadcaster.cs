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

using Oculus.Interaction.Input;
using System;
using UnityEngine;

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// This component transforms an Axis value into Locomotion turning events.
    /// The moment at which the events are emitted depends on an Interactor State.
    /// When using Snap turning mode the event is sent once during Select while
    /// on Smooth turning mode it is processed continuously during select.
    /// </summary>
    public class TurnerEventBroadcaster : MonoBehaviour,
        ILocomotionEventBroadcaster
    {
        public enum TurnMode
        {
            Snap,
            Smooth
        }

        [SerializeField, Interface(typeof(IInteractor))]
        [Tooltip("The interactor defines when the Locomotion events are sent based on its Select state")]
        private UnityEngine.Object _interactor;
        private IInteractor Interactor { get; set; }

        [SerializeField, Interface(typeof(IAxis1D))]
        [Tooltip("Axis from -1 to 1 indicating the turning direction and velocity")]
        private UnityEngine.Object _axis;
        private IAxis1D Axis { get; set; }

        [SerializeField]
        [Tooltip("Snap turn fires once during Select, while Smooth fires continuously during Select")]
        private TurnMode _turnMethod;
        /// <summary>
        /// Snap turn fires once during Select, while Smooth fires continuously during Select
        /// </summary>
        public TurnMode TurnMethod
        {
            get
            {
                return _turnMethod;
            }
            set
            {
                _turnMethod = value;
            }
        }

        [SerializeField]
        [Tooltip("Degrees to instantly turn when in Snap turn mode. Note the direction is provided by the axis")]
        private float _snapTurnDegrees = 45f;
        /// <summary>
        /// Degrees to instantly turn when in Snap turn mode. Note the direction is provided by the axis
        /// </summary>
        public float SnapTurnDegrees
        {
            get
            {
                return _snapTurnDegrees;
            }
            set
            {
                _snapTurnDegrees = value;
            }
        }

        [SerializeField]
        [Tooltip("Degrees to continuously rotate during selection when in Smooth turn mode, it is remapped from the Axis value")]
        private AnimationCurve _smoothTurnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 100f);
        /// <summary>
        /// Degrees to continuously rotate during selection when in Smooth turn mode, it is remapped from the Axis value
        /// </summary>
        public AnimationCurve SmoothTurnCurve
        {
            get
            {
                return _smoothTurnCurve;
            }
            set
            {
                _smoothTurnCurve = value;
            }
        }

        [SerializeField]
        [Tooltip("When enabled, snap turn happens on unselect. If false it happens on select")]
        private bool _fireSnapOnUnselect = true;
        /// <summary>
        /// When enabled, snap turn happens on unselect. If false it happens on select
        /// </summary>
        public bool FireSnapOnUnselect
        {
            get
            {
                return _fireSnapOnUnselect;
            }
            set
            {
                _fireSnapOnUnselect = value;
            }
        }

        private UniqueIdentifier _identifier;
        public int Identifier => _identifier.ID;

        private bool _wasSelecting = false;

        protected bool _started;

        protected virtual void Awake()
        {
            _identifier = UniqueIdentifier.Generate();

            Interactor = _interactor as IInteractor;
            Axis = _axis as IAxis1D;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Interactor, nameof(Interactor));
            this.AssertField(Axis, nameof(Axis));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Interactor.WhenStateChanged += HandleStateChanged;
                Interactor.WhenPostprocessed += HandlePostprocessed;
                _wasSelecting = false;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Interactor.WhenStateChanged -= HandleStateChanged;
                Interactor.WhenPostprocessed -= HandlePostprocessed;
            }
        }

        private Action<LocomotionEvent> _whenLocomotionEventRaised = delegate { };
        public event Action<LocomotionEvent> WhenLocomotionPerformed
        {
            add
            {
                _whenLocomotionEventRaised += value;
            }
            remove
            {
                _whenLocomotionEventRaised -= value;
            }
        }

        private void HandleStateChanged(InteractorStateChangeArgs obj)
        {
            if (obj.PreviousState == InteractorState.Select)
            {
                _wasSelecting = _fireSnapOnUnselect;
            }
        }

        private void HandlePostprocessed()
        {
            if (_wasSelecting && _fireSnapOnUnselect)
            {
                _wasSelecting = false;
                if ((Interactor.State == InteractorState.Hover || Interactor.State == InteractorState.Normal)
                    && _turnMethod == TurnMode.Snap)
                {
                    ProcessSnapTurn(Axis.Value());
                }
            }

            if (Interactor.State == InteractorState.Select)
            {
                if (_turnMethod == TurnMode.Smooth)
                {
                    ProcessSmoothTurn(Axis.Value());
                }
                else if (_turnMethod == TurnMode.Snap
                    && !_fireSnapOnUnselect
                    && !_wasSelecting)
                {
                    _wasSelecting = true;
                    ProcessSnapTurn(Axis.Value());
                }
            }

        }

        private void ProcessSnapTurn(float pointerOffset)
        {
            float sign = Mathf.Sign(pointerOffset);
            Quaternion rot = Quaternion.Euler(0f, sign * _snapTurnDegrees, 0f);

            LocomotionEvent locomotionEvent = new LocomotionEvent(
                Identifier, rot, LocomotionEvent.RotationType.Relative);
            _whenLocomotionEventRaised.Invoke(locomotionEvent);
        }

        private void ProcessSmoothTurn(float pointerOffset)
        {
            float sign = Mathf.Sign(pointerOffset);
            float vel = _smoothTurnCurve.Evaluate(Mathf.Abs(pointerOffset));
            Quaternion rot = Quaternion.Euler(0f, sign * vel, 0f);
            LocomotionEvent locomotionEvent = new LocomotionEvent(
                Identifier, rot, LocomotionEvent.RotationType.Velocity);
            _whenLocomotionEventRaised.Invoke(locomotionEvent);
        }

        #region Inject
        public void InjectAllTurnerEventBroadcaster(IInteractor interactor,
            IAxis1D axis)
        {
            InjectInteractor(interactor);
            InjectAxis(axis);
        }

        public void InjectInteractor(IInteractor interactor)
        {
            _interactor = interactor as UnityEngine.Object;
            Interactor = interactor;
        }

        public void InjectAxis(IAxis1D axis)
        {
            _axis = axis as UnityEngine.Object;
            Axis = axis;
        }
        #endregion
    }
}
