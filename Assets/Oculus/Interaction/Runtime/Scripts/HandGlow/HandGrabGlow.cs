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
using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction.Grab;
using Oculus.Interaction.GrabAPI;
using Oculus.Interaction.Input;
using System;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using Oculus.Interaction.HandGrab;

namespace Oculus.Interaction
{
    /// <summary>
    /// HandGrabGlow controls the glow properties of the OculusHand material to get a glow effect
    /// when pinch / palm grabbing objects depending on the per finger pinch / palm strength.
    /// To achive the glow effect, it also generates a custom UV channel and using the joints
    /// in the hand visual  component adds per finger mask information.
    /// </summary>
    public class HandGrabGlow : MonoBehaviour
    {
        #region Inspector

        [SerializeField, Interface(typeof(IHandGrabInteractor), typeof(IInteractor))]
        private UnityEngine.Object _handGrabInteractor;

        [SerializeField]
        private SkinnedMeshRenderer _handRenderer;

        [SerializeField]
        private MaterialPropertyBlockEditor _materialEditor;

        [SerializeField]
        private HandVisual _handVisual;

        [SerializeField]
        private Color _glowColorGrabing;

        [SerializeField]
        private Color _glowColorHover;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float _colorChangeSpeed = 0.5f;

        [SerializeField]
        [Range(0.0f, 0.25f)]
        private float _glowFadeStartTime = 0.2f;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float _glowStrengthChangeSpeed = 0.5f;

        [SerializeField]
        private bool _fadeOut;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        [Tooltip("Recommended from 0.7 to 1.0")]
        private float _gradientLength = 0.85f;

        [SerializeField]
        private GlowType _glowType = GlowType.Outline;

        #endregion

        public enum GlowType
        {
            Fill = 27,
            Outline = 28,
            Both = 29
        }

        enum GlowState
        {
            None,
            Hover,
            Selected,
            SelectedGlowOut,
        }

        private GlowState _state;
        private float _accumulatedSelectedTime;

        enum GrabState
        {
            None,
            Pinch,
            Palm
        }

        private GrabState _grabState;

        private float _glowFadeValue;
        private Color _currentColor;

        private IHandGrabInteractor HandGrabInteractor;
        private IInteractor Interactor;

        private float[] _glowStregth = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };

        #region ShaderPropertyIDs

        private readonly int _generateGlowID = Shader.PropertyToID("_GenerateGlow");
        private readonly int _glowColorID = Shader.PropertyToID("_GlowColor");
        private readonly int _glowTypeID = Shader.PropertyToID("_GlowType");
        private readonly int _glowParameterID = Shader.PropertyToID("_GlowParameter");

        private readonly int[] _fingersGlowIDs =
        {
            Shader.PropertyToID("_ThumbGlowValue"), Shader.PropertyToID("_IndexGlowValue"),
            Shader.PropertyToID("_MiddleGlowValue"), Shader.PropertyToID("_RingGlowValue"),
            Shader.PropertyToID("_PinkyGlowValue"),
        };

        #endregion

        protected bool _started = false;

        protected virtual void Awake()
        {
            _glowFadeValue = 1.0f;
            _state = GlowState.None;
            _grabState = GrabState.None;
            HandGrabInteractor = _handGrabInteractor as IHandGrabInteractor;
            Interactor = _handGrabInteractor as IInteractor;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            Assert.IsNotNull(_materialEditor);
            Assert.IsNotNull(HandGrabInteractor);
            Assert.IsNotNull(Interactor);
            Assert.IsNotNull(_handVisual);
            HandFingerMaskGenerator.GenerateFingerMask(_handRenderer, _handVisual,
                _materialEditor.MaterialPropertyBlock);

            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Interactor.WhenPostprocessed += UpdateVisual;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Interactor.WhenPostprocessed -= UpdateVisual;
            }
        }

        private void SetMaterialPropertyBlockValues()
        {
            MaterialPropertyBlock block = _materialEditor.MaterialPropertyBlock;
            if (block == null) return;
            block.SetInt(_generateGlowID, 1);
            block.SetColor(_glowColorID, _currentColor);
            if (_glowType == GlowType.Fill || _glowType == GlowType.Both)
            {
                float glowParam = _gradientLength;
                if (_fadeOut)
                {
                    glowParam *= _glowFadeValue;
                }

                block.SetFloat(_glowParameterID, glowParam);
            }
            else
            {
                block.SetFloat(_glowParameterID, _glowFadeValue);
            }

            for (int i = 0; i < _fingersGlowIDs.Length; i++)
            {
                block.SetFloat(_fingersGlowIDs[i], Mathf.Clamp01(_glowStregth[i]));
            }

            block.SetInt(_glowTypeID, (int) _glowType);
        }

        private void UpdateFingerGlowStrength(int fingerIndex, float strength)
        {
            float glowStrength =
                Mathf.Lerp(_glowStregth[fingerIndex], strength, _glowStrengthChangeSpeed);
            _glowStregth[fingerIndex] = glowStrength;
        }

        private bool FingerOptionalOrRequired(GrabbingRule rules, HandFinger finger)
        {
            return rules[finger] == FingerRequirement.Optional ||
                   rules[finger] == FingerRequirement.Required;
        }

        private void UpdateGlowStrength()
        {
            float maxStrength = 0.0f;
            for (int i = 1; i < Input.Constants.NUM_FINGERS; i++)
            {
                Input.HandFinger finger = (Input.HandFinger)i;
                bool isPinchOptionalOrRequired =
                    FingerOptionalOrRequired(HandGrabInteractor.TargetInteractable.PinchGrabRules, finger);
                float pinchStrength = TargetSupportsPinch() && isPinchOptionalOrRequired
                    ? HandGrabInteractor.HandGrabApi.GetFingerPinchStrength(finger)
                    : 0.0f;
                bool isPalmOptionalOrRequired =
                    FingerOptionalOrRequired(HandGrabInteractor.TargetInteractable.PalmGrabRules, finger);
                float palmStrength = TargetSupportsPalm() && isPalmOptionalOrRequired
                    ? HandGrabInteractor.HandGrabApi.GetFingerPalmStrength(finger)
                    : 0.0f;
                float strength = Mathf.Max(pinchStrength, palmStrength);
                UpdateFingerGlowStrength(i, strength);
                maxStrength = Mathf.Max(pinchStrength, maxStrength);
            }

            //Set thumb strength
            bool isPalmOptionalOrRequiredThumb =
                FingerOptionalOrRequired(HandGrabInteractor.TargetInteractable.PalmGrabRules,
                    HandFinger.Thumb);
            var thumbStrength = TargetSupportsPalm() && isPalmOptionalOrRequiredThumb
                ? HandGrabInteractor.HandGrabApi.GetFingerPalmStrength(HandFinger.Thumb)
                : 0.0f;
            //Not getting the pinch thumb strength because it might not give the correct value
            UpdateFingerGlowStrength(0, Mathf.Max(thumbStrength, maxStrength));
        }

        private void UpdateGlowState()
        {
            if (Interactor.State == InteractorState.Hover)
            {
                _state = GlowState.Hover;
            }
            else if (Interactor.State == InteractorState.Select)
            {
                if (_state == GlowState.Hover || _state == GlowState.None)
                {
                    _accumulatedSelectedTime = 0.0f;
                    _state = GlowState.Selected;
                }
                else if (_state == GlowState.Selected)
                {
                    _accumulatedSelectedTime += Time.deltaTime;
                    if (_fadeOut && _accumulatedSelectedTime >= _glowFadeStartTime)
                    {
                        _state = GlowState.SelectedGlowOut;
                    }
                }
            }
            else
            {
                _state = GlowState.None;
            }
        }

        private void UpdateGlowColorAndFade()
        {
            if (_state == GlowState.Hover)
            {
                _glowFadeValue = 1.0f;
                _currentColor = Color.Lerp(_currentColor,
                    _fadeOut ? _glowColorGrabing : _glowColorHover, _colorChangeSpeed);
            }
            else if (_state == GlowState.Selected)
            {
                if (_fadeOut)
                {
                    _glowFadeValue = Mathf.Lerp(_glowFadeValue, 0.5f, 0.8f);
                    _currentColor = _glowColorGrabing;
                }
                else
                {
                    _glowFadeValue = 1.0f;
                    _currentColor = Color.Lerp(_currentColor, _glowColorGrabing, _colorChangeSpeed);
                }
            }
            else if (_state == GlowState.SelectedGlowOut)
            {
                _glowFadeValue = Mathf.Lerp(_glowFadeValue, 1.15f, 0.3f);
                _currentColor = _glowColorGrabing;
            }
            else
            {
                _glowFadeValue = Mathf.Lerp(_glowFadeValue, 0.0f, 0.15f);
            }
        }

        private bool TargetSupportsPinch()
        {
            if (HandGrabInteractor.TargetInteractable == null) return false;

            return (HandGrabInteractor.SupportedGrabTypes &
                    HandGrabInteractor.TargetInteractable.SupportedGrabTypes &
                    GrabTypeFlags.Pinch) != 0;
        }

        private bool TargetSupportsPalm()
        {
            if (HandGrabInteractor.TargetInteractable == null) return false;

            return (HandGrabInteractor.SupportedGrabTypes &
                    HandGrabInteractor.TargetInteractable.SupportedGrabTypes &
                    GrabTypeFlags.Palm) != 0;
        }

        private void UpdateGrabState()
        {
            if (HandGrabInteractor.TargetInteractable == null)
            {
                _grabState = GrabState.None;
                return;
            }

            GrabbingRule pinchGrabRules = HandGrabInteractor.TargetInteractable.PinchGrabRules;
            bool pinchGestureActive = HandGrabInteractor.HandGrabApi.IsHandPinchGrabbing(pinchGrabRules);
            if (TargetSupportsPinch() && pinchGestureActive)
            {
                if (_grabState == GrabState.None || _grabState == GrabState.Pinch)
                {
                    _grabState = GrabState.Pinch;
                    return;
                }
            }


            GrabbingRule palmGrabRules = HandGrabInteractor.TargetInteractable.PalmGrabRules;
            bool palmGestureActive = HandGrabInteractor.HandGrabApi.IsHandPalmGrabbing(palmGrabRules);
            if (TargetSupportsPalm() && palmGestureActive)
            {
                if (_grabState == GrabState.None || _grabState == GrabState.Palm)
                {
                    _grabState = GrabState.Palm;
                    return;
                }
            }

            _grabState = GrabState.None;
        }

        private void ClearGlow()
        {
            var block = _materialEditor.MaterialPropertyBlock;
            foreach (var fingerID in _fingersGlowIDs) block.SetFloat(fingerID, 0.0f);
            block.SetInt(_generateGlowID, 0);
        }

        private void UpdateVisual()
        {
            GlowState prevGlowState = _state;
            UpdateGrabState();
            UpdateGlowState();
            if (prevGlowState != _state && _state == GlowState.None)
            {
                ClearGlow();
            }
            else if (_state != GlowState.None)
            {
                UpdateGlowStrength();
                UpdateGlowColorAndFade();
                SetMaterialPropertyBlockValues();
            }
        }

        #region Inject

        public void InjectAllHandGrabGlow(IHandGrabInteractor handGrabInteractor,
            SkinnedMeshRenderer handRenderer, MaterialPropertyBlockEditor materialEditor,
            HandVisual handVisual, Color grabbingColor, Color hoverColor, float colorChangeSpeed,
            float fadeStartTime, float glowStrengthChangeSpeed, bool fadeOut,
            float gradientLength, GlowType glowType)
        {
            InjectHandGrabInteractor(handGrabInteractor);
            InjectHandRenderer(handRenderer);
            InjectMaterialPropertyBlockEditor(materialEditor);
            InjectHandVisual(handVisual);
            InjectGlowColors(grabbingColor, hoverColor);
            InjectVisualChangeSpeed(colorChangeSpeed, fadeStartTime,
                glowStrengthChangeSpeed);
            InjectFadeOut(fadeOut);
            InjectGradientLength(gradientLength);
            InjectGlowType(glowType);
        }

        public void InjectHandGrabInteractor(IHandGrabInteractor handGrabInteractor)
        {
            _handGrabInteractor = handGrabInteractor as UnityEngine.Object;
            Interactor = handGrabInteractor as IInteractor;
            HandGrabInteractor = handGrabInteractor;
        }

        public void InjectHandRenderer(SkinnedMeshRenderer handRenderer)
        {
            _handRenderer = handRenderer;
        }

        public void InjectMaterialPropertyBlockEditor(MaterialPropertyBlockEditor materialEditor)
        {
            _materialEditor = materialEditor;
        }

        public void InjectHandVisual(HandVisual handVisual)
        {
            _handVisual = handVisual;
        }

        public void InjectGlowColors(Color grabbingColor, Color hoverColor)
        {
            _glowColorGrabing = grabbingColor;
            _glowColorHover = hoverColor;
        }

        public void InjectVisualChangeSpeed(float colorChangeSpeed, float fadeStartTime, float glowStrengthChangeSpeed)
        {
            _colorChangeSpeed = colorChangeSpeed;
            _glowFadeStartTime = fadeStartTime;
            _glowStrengthChangeSpeed = glowStrengthChangeSpeed;
        }

        public void InjectFadeOut(bool fadeOut)
        {
            _fadeOut = fadeOut;
        }

        /// <param name="gradientLength">Clamped 0.0 to 1.0</param>
        public void InjectGradientLength(float gradientLength)
        {
            _gradientLength = Mathf.Clamp01(gradientLength);
        }

        public void InjectGlowType(GlowType glowType)
        {
            _glowType = glowType;
        }

        #endregion
    }
}
