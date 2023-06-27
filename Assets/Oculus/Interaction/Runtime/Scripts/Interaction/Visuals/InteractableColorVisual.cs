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
using System.Collections;
using UnityEngine;

namespace Oculus.Interaction
{
    public class InteractableColorVisual : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IInteractableView))]
        private UnityEngine.Object _interactableView;
        private IInteractableView InteractableView { get; set; }

        [SerializeField]
        private MaterialPropertyBlockEditor _editor;

        [SerializeField]
        private string _colorShaderPropertyName = "_Color";

        [Serializable]
        public class ColorState
        {
            public Color Color = Color.white;
            public AnimationCurve ColorCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            public float ColorTime = 0.1f;
        }

        [SerializeField]
        private ColorState _normalColorState = new ColorState() { Color = Color.white };
        [SerializeField]
        private ColorState _hoverColorState = new ColorState() { Color = Color.blue };
        [SerializeField]
        private ColorState _selectColorState = new ColorState() { Color = Color.green };
        [SerializeField]
        private ColorState _disabledColorState = new ColorState() { Color = Color.grey };

        private Color _currentColor;
        private ColorState _target;
        private int _colorShaderID;
        private Coroutine _routine = null;
        private static readonly YieldInstruction _waiter = new WaitForEndOfFrame();

        protected bool _started = false;

        protected virtual void Awake()
        {
            InteractableView = _interactableView as IInteractableView;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            this.AssertField(InteractableView, nameof(InteractableView));
            this.AssertField(_editor, nameof(_editor));

            _colorShaderID = Shader.PropertyToID(_colorShaderPropertyName);

            UpdateVisual();
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                UpdateVisual();
                InteractableView.WhenStateChanged += UpdateVisualState;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                InteractableView.WhenStateChanged -= UpdateVisualState;
            }
        }

        private void UpdateVisualState(InteractableStateChangeArgs args)
        {
            UpdateVisual();
        }

        protected virtual void UpdateVisual()
        {
            ColorState target = ColorForState(InteractableView.State);
            if (target != _target)
            {
                _target = target;
                CancelRoutine();
                _routine = StartCoroutine(ChangeColor(target));
            }
        }

        private ColorState ColorForState(InteractableState state)
        {
            switch (state)
            {
                case InteractableState.Select:
                    return _selectColorState;
                case InteractableState.Hover:
                    return _hoverColorState;
                case InteractableState.Normal:
                    return _normalColorState;
                case InteractableState.Disabled:
                    return _disabledColorState;
                default:
                    return _normalColorState;
            }
        }

        private IEnumerator ChangeColor(ColorState targetState)
        {
            Color startColor = _currentColor;
            float timer = 0f;
            do
            {
                timer += Time.deltaTime;
                float normalizedTimer = Mathf.Clamp01(timer / targetState.ColorTime);
                float t = targetState.ColorCurve.Evaluate(normalizedTimer);
                SetColor(Color.Lerp(startColor, targetState.Color, t));

                yield return _waiter;
            }
            while (timer <= targetState.ColorTime);
        }

        private void SetColor(Color color)
        {
            _currentColor = color;
            _editor.MaterialPropertyBlock.SetColor(_colorShaderID, color);
        }

        private void CancelRoutine()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
        }

        #region Inject

        public void InjectAllInteractableColorVisual(IInteractableView interactableView,
            MaterialPropertyBlockEditor editor)
        {
            InjectInteractableView(interactableView);
            InjectMaterialPropertyBlockEditor(editor);
        }

        public void InjectInteractableView(IInteractableView interactableview)
        {
            _interactableView = interactableview as UnityEngine.Object;
            InteractableView = interactableview;
        }

        public void InjectMaterialPropertyBlockEditor(MaterialPropertyBlockEditor editor)
        {
            _editor = editor;
        }

        public void InjectOptionalColorShaderPropertyName(string colorShaderPropertyName)
        {
            _colorShaderPropertyName = colorShaderPropertyName;
        }

        public void InjectOptionalNormalColorState(ColorState normalColorState)
        {
            _normalColorState = normalColorState;
        }

        public void InjectOptionalHoverColorState(ColorState hoverColorState)
        {
            _hoverColorState = hoverColorState;
        }

        public void InjectOptionalSelectColorState(ColorState selectColorState)
        {
            _selectColorState = selectColorState;
        }

        #endregion
    }
}
