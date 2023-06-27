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
using UnityEngine;

namespace Oculus.Interaction
{
    public class ActiveStateFingerVisual : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IActiveState))]
        private UnityEngine.Object _activeState;
        private IActiveState ActiveState;

        [SerializeField]
        private HandFingerFlags _fingersMask;
        public HandFingerFlags FingersMask
        {
            get
            {
                return _fingersMask;
            }
            set
            {
                _fingersMask = value;
            }
        }

        [SerializeField]
        private MaterialPropertyBlockEditor _handMaterialPropertyBlockEditor;

        [SerializeField]
        private float _glowLerpSpeed = 2f;
        public float GlowLerpSpeed
        {
            get
            {
                return _glowLerpSpeed;
            }
            set
            {
                _glowLerpSpeed = value;
            }
        }

        [SerializeField]
        private Color _fingerGlowColor;
        public Color FingerGlowColor
        {
            get
            {
                return _fingerGlowColor;
            }
            set
            {
                _fingerGlowColor = value;
            }
        }

        private readonly int[] _handShaderGlowPropertyIds = new int[]
        {
                Shader.PropertyToID("_ThumbGlowValue"),
                Shader.PropertyToID("_IndexGlowValue"),
                Shader.PropertyToID("_MiddleGlowValue"),
                Shader.PropertyToID("_RingGlowValue"),
                Shader.PropertyToID("_PinkyGlowValue"),
        };

        private readonly int _fingerGlowColorPropertyId = Shader.PropertyToID("_FingerGlowColor");

        private bool _prevActive = false;
        protected bool _started;

        protected virtual void Awake()
        {
            ActiveState = _activeState as IActiveState;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(ActiveState, nameof(ActiveState));
            this.AssertField(_handMaterialPropertyBlockEditor, nameof(_handMaterialPropertyBlockEditor));
            this.EndStart(ref _started);
        }

        private void Update()
        {
            if (_prevActive == ActiveState.Active)
            {
                return;
            }
            StopAllCoroutines();
            _prevActive = ActiveState.Active;
            _handMaterialPropertyBlockEditor.MaterialPropertyBlock.SetColor(_fingerGlowColorPropertyId, _fingerGlowColor);

            float glowValue = ActiveState.Active ? 1f : 0f;
            for (int i = 0; i < Constants.NUM_FINGERS; ++i)
            {
                if (!_fingersMask.HasFlag((HandFingerFlags)(1 << i)))
                {
                    continue;
                }
                StartCoroutine(UpdateGlowValue(i, glowValue));
            }

            _handMaterialPropertyBlockEditor.UpdateMaterialPropertyBlock();
        }

        private IEnumerator UpdateGlowValue(int fingerIndex, float targetGlow)
        {
            float startGlow = _handMaterialPropertyBlockEditor.MaterialPropertyBlock.GetFloat(_handShaderGlowPropertyIds[fingerIndex]);
            float startTime = Time.time;
            float currentGlow = startGlow;
            do
            {
                currentGlow = Mathf.MoveTowards(startGlow, targetGlow, _glowLerpSpeed * (Time.time - startTime));
                _handMaterialPropertyBlockEditor.MaterialPropertyBlock.SetFloat(_handShaderGlowPropertyIds[fingerIndex], currentGlow);
                yield return null;
            } while (currentGlow != targetGlow);
        }

        #region Inject

        public void InjectAllActiveStateFingerVisual(IActiveState activeState,
            MaterialPropertyBlockEditor handMaterialPropertyBlockEditor)
        {
            InjectActiveState(activeState);
            InjectHandMaterialPropertyBlockEditor(handMaterialPropertyBlockEditor);
        }

        public void InjectActiveState(IActiveState activeState)
        {
            ActiveState = activeState;
            _activeState = activeState as UnityEngine.Object;
        }

        public void InjectHandMaterialPropertyBlockEditor(MaterialPropertyBlockEditor handMaterialPropertyBlockEditor)
        {
            _handMaterialPropertyBlockEditor = handMaterialPropertyBlockEditor;
        }

        #endregion
    }
}
