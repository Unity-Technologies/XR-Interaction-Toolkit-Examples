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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// HandRayPinchGlow controls the glow properties of the OculusHand material to get a glow effect
    /// when the user is using Pinch Ray
    /// </summary>
    public class HandRayPinchGlow : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;

        [SerializeField]
        private RayInteractor _rayInteractor;

        [SerializeField]
        private MaterialPropertyBlockEditor _materialEditor;

        [SerializeField]
        private Color _glowColor;

        [SerializeField]
        private GlowType _glowType = GlowType.Outline;

        public enum GlowType
        {
            Fill = 17,
            Outline = 18,
            Both = 16
        }

        private IHand Hand;

        private readonly int _generateGlowID = Shader.PropertyToID("_GenerateGlow");
        private readonly int _glowPositionID = Shader.PropertyToID("_GlowPosition");
        private readonly int _glowColorID = Shader.PropertyToID("_GlowColor");
        private readonly int _glowTypeID = Shader.PropertyToID("_GlowType");
        private readonly int _glowParameterID = Shader.PropertyToID("_GlowParameter");
        private readonly int _glowMaxLengthID = Shader.PropertyToID("_GlowMaxLength");

        private bool _glowEnabled;
        protected bool _started = false;

        protected virtual void Awake()
        {
            Hand = _hand as IHand;
        }

        protected virtual void Start()
        {
            _glowEnabled = false;
            this.BeginStart(ref _started);
            this.AssertField(Hand, nameof(Hand));
            this.AssertField(_rayInteractor, nameof(_rayInteractor));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _rayInteractor.WhenPostprocessed += UpdateVisual;
                _rayInteractor.WhenStateChanged += UpdateVisualState;
                UpdateVisual();
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _rayInteractor.WhenPostprocessed -= UpdateVisual;
                _rayInteractor.WhenStateChanged -= UpdateVisualState;
            }
        }

        private void UpdateVisualState(InteractorStateChangeArgs args) => UpdateVisual();

        private void UpdateGlow(Vector3 glowPosition, float pinchStrength, float glowMaxLength)
        {
            if (_materialEditor == null)
            {
                return;
            }

            var block = _materialEditor.MaterialPropertyBlock;
            block.SetInt(_generateGlowID, 1);
            block.SetColor(_glowColorID, _glowColor);
            block.SetFloat(_glowParameterID, pinchStrength);
            block.SetFloat(_glowMaxLengthID, glowMaxLength);

            block.SetInt(_glowTypeID, (int) _glowType);
            block.SetVector(_glowPositionID, glowPosition);
        }

        private void UpdateVisual()
        {
            if (_rayInteractor.State == InteractorState.Disabled)
            {
                if (_glowEnabled)
                {
                    if (_materialEditor == null)
                    {
                        return;
                    }

                    var block = _materialEditor.MaterialPropertyBlock;
                    block.SetInt(_generateGlowID, 0);
                    _glowEnabled = false;
                }
            }
            else
            {
                _glowEnabled = true;
                if (!Hand.GetJointPose(HandJointId.HandThumbTip, out Pose thumbPose))
                {
                    return;
                }

                if (!Hand.GetJointPose(HandJointId.HandIndexTip, out Pose indexPose))
                {
                    return;
                }

                if (!Hand.GetRootPose(out Pose wristPose))
                {
                    return;
                }

                var pinchStrength = Hand.GetFingerPinchStrength(HandFinger.Index);
                Vector3 glowPosition = (thumbPose.position + indexPose.position) / 2.0f;
                float glowPosToWrist = Vector3.Distance(wristPose.position, glowPosition) * 0.9f;
                UpdateGlow(glowPosition, pinchStrength, glowPosToWrist);
            }
        }

        #region Inject

        public void InjectAllHandRayPinchGlow(IHand hand, RayInteractor interactor,
            MaterialPropertyBlockEditor materialEditor, Color color, GlowType glowType)
        {
            InjectHand(hand);
            InjectRayInteractor(interactor);
            InjectMaterialPropertyBlockEditor(materialEditor);
            InjectGlowColor(color);
            InjectGlowType(glowType);
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as UnityEngine.Object;
            Hand = hand;
        }

        public void InjectRayInteractor(RayInteractor interactor)
        {
            _rayInteractor = interactor;
        }

        public void InjectMaterialPropertyBlockEditor(MaterialPropertyBlockEditor materialEditor)
        {
            _materialEditor = materialEditor;
        }

        public void InjectGlowColor(Color color)
        {
            _glowColor = color;
        }

        public void InjectGlowType(GlowType glowType)
        {
            _glowType = glowType;
        }

        #endregion
    }
}
