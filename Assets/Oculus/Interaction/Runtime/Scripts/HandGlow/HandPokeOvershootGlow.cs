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
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace Oculus.Interaction
{
    /// <summary>
    /// HandPokeOvershootGlow controls the glow properties of the OculusHand material to
    /// get a glow effect when the real hand and the virtual hand are not in the same position in 3d
    /// space. This can only happen when a synthetic hand is used and wrist locking is enabled.
    /// It generates a sphere gradient with the wrist position as the center and a 0.144 units of radius.
    /// </summary>
    public class HandPokeOvershootGlow : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;

        [SerializeField]
        private PokeInteractor _pokeInteractor;

        [SerializeField]
        private HandVisual _handVisual;

        [SerializeField]
        private SkinnedMeshRenderer _handRenderer;

        [SerializeField]
        private MaterialPropertyBlockEditor _materialEditor;

        [SerializeField]
        private Color _glowColor;

        [SerializeField]
        private float _overshootMaxDistance = 0.15f;

        [SerializeField]
        private HandFinger _pokeFinger = HandFinger.Index;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float _maxGradientLength;

        public enum GlowType
        {
            Fill = 30,
            Outline = 31,
            Both = 32
        }

        [SerializeField]
        private GlowType _glowType = GlowType.Outline;

        private IHand Hand;
        private bool _glowEnabled;

        private readonly int _glowFingerIndexID = Shader.PropertyToID("_FingerGlowIndex");
        private readonly int _generateGlowID = Shader.PropertyToID("_GenerateGlow");
        private readonly int _glowColorID = Shader.PropertyToID("_GlowColor");
        private readonly int _glowTypeID = Shader.PropertyToID("_GlowType");
        private readonly int _glowParameterID = Shader.PropertyToID("_GlowParameter");
        private readonly int _glowMaxLengthID = Shader.PropertyToID("_GlowMaxLength");

        protected bool _started = false;

        protected virtual void Awake()
        {
            Hand = _hand as IHand;
        }

        protected virtual void Start()
        {
            _glowEnabled = false;
            this.BeginStart(ref _started);
            Assert.IsNotNull(Hand);
            Assert.IsNotNull(_pokeInteractor);
            Assert.IsNotNull(_materialEditor);
            HandFingerMaskGenerator.GenerateFingerMask(_handRenderer, _handVisual, _materialEditor.MaterialPropertyBlock);
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _pokeInteractor.WhenPostprocessed += UpdateVisual;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _pokeInteractor.WhenPostprocessed -= UpdateVisual;
            }
        }

        private void UpdateOvershoot(float normalizedDistance )
        {
            if (_materialEditor == null) return;
            var _block = _materialEditor.MaterialPropertyBlock;
            _block.SetFloat(_glowParameterID, Mathf.Clamp01(normalizedDistance));
            _block.SetInt(_generateGlowID, 1);
            _block.SetColor(_glowColorID, _glowColor);
            _block.SetInt(_glowTypeID, (int)_glowType);
            _block.SetInt(_glowFingerIndexID, (int)_pokeFinger);
            _block.SetFloat(_glowMaxLengthID, _maxGradientLength);
        }

        private void UpdateVisual()
        {
            if (_pokeInteractor.State == InteractorState.Select)
            {
                _glowEnabled = true;
                Vector3 planeCenter = _pokeInteractor.TouchPoint;
                Vector3 pokeOrigin = _pokeInteractor.Origin;
                float normalizedDistance =
                    Mathf.Clamp01(Vector3.Distance(planeCenter, pokeOrigin) /
                                  _overshootMaxDistance);
                UpdateOvershoot(normalizedDistance);
            }
            else
            {
                if (_glowEnabled)
                {
                    if (_materialEditor == null) return;
                    var _block = _materialEditor.MaterialPropertyBlock;
                    _block.SetInt(_generateGlowID, 0);
                    _glowEnabled = false;
                }
            }
        }

        #region Inject

        public void InjectAllHandPokeOvershootGlow(IHand hand, PokeInteractor pokeInteractor,
            MaterialPropertyBlockEditor materialEditor, Color glowColor, float distanceMultiplier,
            Transform wristTransform, GlowType glowType)
        {
            InjectHand(hand);
            InjectPokeInteractor(pokeInteractor);
            InjectMaterialPropertyBlockEditor(materialEditor);
            InjectGlowColor(glowColor);
            InjectOvershootMaxDistance(distanceMultiplier);
            InjectGlowType(glowType);
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as UnityEngine.Object;
            Hand = hand;
        }

        public void InjectPokeInteractor(PokeInteractor pokeInteractor)
        {
            _pokeInteractor = pokeInteractor;
        }

        public void InjectMaterialPropertyBlockEditor(MaterialPropertyBlockEditor materialEditor)
        {
            _materialEditor = materialEditor;
        }

        public void InjectGlowColor(Color glowColor)
        {
            _glowColor = glowColor;
        }

        public void InjectOvershootMaxDistance(float overshootMaxDistance)
        {
            _overshootMaxDistance = overshootMaxDistance;
        }

        public void InjectGlowType(GlowType glowType)
        {
            _glowType = glowType;
        }

        #endregion
    }
}
