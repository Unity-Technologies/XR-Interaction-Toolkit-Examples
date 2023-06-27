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

namespace Oculus.Interaction
{
    /// <summary>
    /// An Axis1D that switches between two Axis1D based on an ActiveState
    /// </summary>
    public class Axis1DSwitch : MonoBehaviour, IAxis1D
    {
        [SerializeField, Interface(typeof(IActiveState))]
        private UnityEngine.Object _activeState;
        private IActiveState ActiveState;

        [SerializeField, Interface(typeof(IAxis1D))]
        private UnityEngine.Object _axisWhenActive;

        [SerializeField, Interface(typeof(IAxis1D))]
        private UnityEngine.Object _axisWhenInactive;

        protected IAxis1D AxisWhenActive;
        protected IAxis1D AxisWhenInactive;

        protected IAxis1D Current => ActiveState.Active ? AxisWhenActive : AxisWhenInactive;

        protected virtual void Awake()
        {
            ActiveState = _activeState as IActiveState;
            AxisWhenActive = _axisWhenActive as IAxis1D;
            AxisWhenInactive = _axisWhenInactive as IAxis1D;
        }

        protected virtual void Start()
        {
            this.AssertField(ActiveState, nameof(ActiveState));
            this.AssertField(AxisWhenActive, nameof(AxisWhenActive));
            this.AssertField(AxisWhenInactive, nameof(AxisWhenInactive));
        }

        public float Value()
        {
            return Current.Value();
        }

        #region Inject

        public void InjectAllAxis1DSwitch(IActiveState activeState, IAxis1D axisWhenActive, IAxis1D axisWhenInactive)
        {
            InjectActiveState(activeState);
            InjectAxisWhenActive(axisWhenActive);
            InjectAxisWhenInactive(axisWhenActive);
        }

        public void InjectActiveState(IActiveState activeState)
        {
            _activeState = activeState as UnityEngine.Object;
            ActiveState = activeState;
        }

        private void InjectAxisWhenActive(IAxis1D axisWhenActive)
        {
            AxisWhenActive = axisWhenActive;
            _axisWhenActive = axisWhenActive as UnityEngine.Object;
        }

        private void InjectAxisWhenInactive(IAxis1D axisWhenInactive)
        {
            AxisWhenInactive = axisWhenInactive;
            _axisWhenInactive = axisWhenInactive as UnityEngine.Object;
        }

        #endregion
    }
}
