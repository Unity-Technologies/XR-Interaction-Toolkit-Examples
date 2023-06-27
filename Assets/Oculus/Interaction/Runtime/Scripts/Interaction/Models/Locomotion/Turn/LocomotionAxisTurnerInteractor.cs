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
using UnityEngine;

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// LocomotionAxisTurner transforms Axis2D value into Axis1D value.
    /// When the value becomes bigger than the deadzone, the interactor goes into Hover and Select state.
    /// </summary>
    public class LocomotionAxisTurnerInteractor : Interactor<LocomotionAxisTurnerInteractor, LocomotionAxisTurnerInteractable>
        , IAxis1D
    {
        [SerializeField, Interface(typeof(IAxis2D))]
        [Tooltip("Input 2D Axis from which the Horizontal axis will be extracted")]
        private UnityEngine.Object _axis2D;
        /// <summary>
        /// Input 2D Axis from which the Horizontal axis will be extracted
        /// </summary>
        private IAxis2D Axis2D;

        [SerializeField, Range(0f, 1f)]
        [Tooltip("The Axis.x absolute value must be bigger than this to go into Hover and Select states")]
        private float _deadZone = 0.5f;
        /// <summary>
        /// The Axis.x absolute value must be bigger than this to go into Hover and Select states
        /// </summary>
        public float DeadZone
        {
            get
            {
                return _deadZone;
            }
            set
            {
                _deadZone = value;
            }
        }

        private float _horizontalAxisValue;

        public override bool ShouldHover => Mathf.Abs(_horizontalAxisValue) > _deadZone;
        public override bool ShouldUnhover => !ShouldHover;

        protected override bool ComputeShouldSelect()
        {
            return ShouldHover;
        }

        protected override bool ComputeShouldUnselect()
        {
            return ShouldUnhover;
        }

        protected override void Awake()
        {
            base.Awake();
            Axis2D = _axis2D as IAxis2D;
        }

        protected override void OnDisable()
        {
            if (_started)
            {
                _horizontalAxisValue = 0f;
            }
            base.OnDisable();
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(_axis2D, nameof(_axis2D));
            this.EndStart(ref _started);
        }

        protected override void DoPreprocess()
        {
            base.DoPreprocess();
            _horizontalAxisValue = Axis2D.Value().x;
        }

        protected override LocomotionAxisTurnerInteractable ComputeCandidate()
        {
            return null;
        }

        public float Value()
        {
            return _horizontalAxisValue;
        }

        #region Inject

        public void InjectAllLocomotionAxisTurner(IAxis2D axis2D)
        {
            InjectAxis2D(axis2D);
        }

        public void InjectAxis2D(IAxis2D axis2D)
        {
            _axis2D = axis2D as UnityEngine.Object;
            Axis2D = axis2D;
        }

        #endregion
    }

    /// <summary>
    /// LocomotionAxisTurnerInteractor does not require and Interactable.
    /// This class is left empty and in a differently named file
    /// so it cannot be used as a Component in the inspector.
    /// </summary>
    public class LocomotionAxisTurnerInteractable : Interactable<LocomotionAxisTurnerInteractor, LocomotionAxisTurnerInteractable> { }
}
