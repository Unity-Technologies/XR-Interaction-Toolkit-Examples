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
    public class Axis2DActiveState : MonoBehaviour, IActiveState
    {
        public enum CheckComponent
        {
            Any = 0,
            X = 1,
            Y = 2,
            All = 3
        }

        public enum ComparisonMode
        {
            GreaterThan = 0,
            LessThan = 1
        }

        [SerializeField, Interface(typeof(IAxis2D))]
        private UnityEngine.Object _inputAxis;
        private IAxis2D InputAxis { get; set; }

        [SerializeField]
        private CheckComponent _checkAxis = CheckComponent.Y;
        public CheckComponent CheckAxis
        {
            get
            {
                return _checkAxis;
            }
            set
            {
                _checkAxis = value;
            }
        }

        [SerializeField]
        private ComparisonMode _comparison = ComparisonMode.GreaterThan;
        public ComparisonMode Comparison
        {
            get
            {
                return _comparison;
            }
            set
            {
                _comparison = value;
            }
        }

        [SerializeField]
        private bool _absoluteValues;
        public bool AbsoluteValues
        {
            get
            {
                return _absoluteValues;
            }
            set
            {
                _absoluteValues = value;
            }
        }

        [SerializeField]
        private Vector2 _thresold = new Vector2(0f, 0.5f);

        public bool Active { get; private set; } = false;

        protected bool _started;

        protected virtual void Awake()
        {
            InputAxis = _inputAxis as IAxis2D;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(InputAxis, nameof(InputAxis));
            this.EndStart(ref _started);
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Active = false;
            }
        }

        protected virtual void Update()
        {
            HandleValueUpdated(InputAxis.Value());
        }

        private void HandleValueUpdated(Vector2 value)
        {
            if (AbsoluteValues)
            {
                value.x = Mathf.Abs(value.x);
                value.y = Mathf.Abs(value.y);
            }

            Active = Comparison == ComparisonMode.GreaterThan ?
                CheckGreaterThan(value) : CheckLessThan(value);
        }

        private bool CheckGreaterThan(Vector2 value)
        {
            return CheckAxis == CheckComponent.X ? value.x > _thresold.x :
                CheckAxis == CheckComponent.Y ? value.y > _thresold.y :
                CheckAxis == CheckComponent.Any ? value.y > _thresold.y || value.x > _thresold.x :
                CheckAxis == CheckComponent.All ? value.y > _thresold.y && value.x > _thresold.x : false;
        }

        private bool CheckLessThan(Vector2 value)
        {
            return CheckAxis == CheckComponent.X ? value.x < _thresold.x :
                CheckAxis == CheckComponent.Y ? value.y < _thresold.y :
                CheckAxis == CheckComponent.Any ? value.y < _thresold.y || value.x < _thresold.x :
                CheckAxis == CheckComponent.All ? value.y < _thresold.y && value.x < _thresold.x : false;
        }
    }
}
