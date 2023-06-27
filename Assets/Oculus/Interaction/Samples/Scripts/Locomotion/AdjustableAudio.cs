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
    [RequireComponent(typeof(AudioSource))]
    public class AdjustableAudio : MonoBehaviour
    {
        [SerializeField]
        private AudioSource _audioSource = null;

        [SerializeField]
        private AudioClip _audioClip = null;
        public AudioClip AudioClip
        {
            get
            {
                return _audioClip;
            }
            set
            {
                _audioClip = value;
            }
        }
        [SerializeField, Range(0f,1f)]
        private float _volumeFactor = 1f;
        public float VolumeFactor
        {
            get
            {
                return _volumeFactor;
            }
            set
            {
                _volumeFactor = value;
            }
        }

        [SerializeField]
        private AnimationCurve _volumeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public AnimationCurve VolumeCurve
        {
            get
            {
                return _volumeCurve;
            }
            set
            {
                _volumeCurve = value;
            }
        }

        [SerializeField]
        private AnimationCurve _pitchCurve = AnimationCurve.Linear(0f, 0.5f, 1f, 1.5f);
        public AnimationCurve PitchCurve
        {
            get
            {
                return _pitchCurve;
            }
            set
            {
                _pitchCurve = value;
            }
        }

        protected bool _started;

        #region Editor events
        protected virtual void Reset()
        {
            _audioSource = gameObject.GetComponent<AudioSource>();
            _audioClip = _audioSource.clip;
        }
        #endregion

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_audioSource, nameof(_audioSource));
            this.EndStart(ref _started);
        }

        public void PlayAudio(float volumeT, float pitchT, float pan = 0f)
        {
            if (!_audioSource.isActiveAndEnabled)
            {
                return;
            }

            _audioSource.volume = _volumeCurve.Evaluate(volumeT) * VolumeFactor;
            _audioSource.pitch = _pitchCurve.Evaluate(pitchT);
            _audioSource.panStereo = pan;
            _audioSource.PlayOneShot(_audioClip);
        }

        #region Inject
        public void InjectAllAdjustableAudio(AudioSource audioSource)
        {
            InjectAudioSource(audioSource);
        }

        public void InjectAudioSource(AudioSource audioSource)
        {
            _audioSource = audioSource;
        }

        #endregion
    }
}
