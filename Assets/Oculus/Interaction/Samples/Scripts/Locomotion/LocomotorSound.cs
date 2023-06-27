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

namespace Oculus.Interaction.Locomotion
{
    public class LocomotorSound : MonoBehaviour
    {
        [SerializeField]
        private PlayerLocomotor _locomotor;

        [SerializeField]
        private AdjustableAudio _translationSound;
        [SerializeField]
        private AdjustableAudio _translationDeniedSound;
        [SerializeField]
        private AdjustableAudio _snapTurnSound;

        [SerializeField]
        private AnimationCurve _translationCurve = AnimationCurve.EaseInOut(0f, 0f, 2f, 1f);
        [SerializeField]
        private AnimationCurve _rotationCurve = AnimationCurve.EaseInOut(0f, 0f, 180f, 1f);
        [SerializeField]
        private float _pitchVariance = 0.05f;

        protected bool _started;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_locomotor, nameof(_locomotor));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _locomotor.WhenLocomotionEventHandled += HandleLocomotionEvent;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _locomotor.WhenLocomotionEventHandled -= HandleLocomotionEvent;
            }
        }

        private void HandleLocomotionEvent(LocomotionEvent locomotionEvent, Pose delta)
        {
            if (locomotionEvent.Translation > LocomotionEvent.TranslationType.Velocity)
            {
                PlayTranslationSound(delta.position.magnitude);
            }
            if (locomotionEvent.Rotation > LocomotionEvent.RotationType.Velocity)
            {
                PlayRotationSound(delta.rotation.y * delta.rotation.w);
            }
            if (locomotionEvent.Translation == LocomotionEvent.TranslationType.None
                && locomotionEvent.Rotation == LocomotionEvent.RotationType.None)
            {
                PlayDenialSound(delta.position.magnitude);
            }
        }

        private void PlayTranslationSound(float translationDistance)
        {
            float t = _translationCurve.Evaluate(translationDistance);
            float pitch = t + Random.Range(-_pitchVariance, _pitchVariance);
            _translationSound.PlayAudio(t, pitch);
        }

        private void PlayDenialSound(float translationDistance)
        {
            float t = _translationCurve.Evaluate(translationDistance);
            float pitch = t + Random.Range(-_pitchVariance, _pitchVariance);
            _translationDeniedSound.PlayAudio(t, pitch);
        }

        private void PlayRotationSound(float rotationLength)
        {
            float t = _rotationCurve.Evaluate(Mathf.Abs(rotationLength));
            float pitch = t + Random.Range(-_pitchVariance, _pitchVariance);
            _snapTurnSound.PlayAudio(t, pitch, rotationLength);
        }

        #region Inject

        public void InjectAllLocomotorSound(PlayerLocomotor locomotor)
        {
            InjectPlayerLocomotor(locomotor);
        }

        public void InjectPlayerLocomotor(PlayerLocomotor locomotor)
        {
            _locomotor = locomotor;
        }
        #endregion
    }
}
