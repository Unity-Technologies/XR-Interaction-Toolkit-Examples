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

using Oculus.Interaction.Locomotion;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Oculus.Interaction.Samples
{
    public class LocomotionTutorialProgressTracker : MonoBehaviour
    {
        [SerializeField]
        private Image[] _dots;
        [SerializeField]
        private Sprite _pendingSprite;
        [SerializeField]
        private Sprite _currentSprite;
        [SerializeField]
        private Sprite _completedSprite;

        [SerializeField]
        private List<LocomotionEvent.TranslationType> _consumeTranslationEvents = new List<LocomotionEvent.TranslationType>();
        [SerializeField]
        private List<LocomotionEvent.RotationType> _consumeRotationEvents = new List<LocomotionEvent.RotationType>();

        [SerializeField, Interface(typeof(ILocomotionEventHandler))]
        private UnityEngine.Object _locomotionHandler;
        private ILocomotionEventHandler LocomotionHandler;

        public UnityEvent WhenCompleted;

        protected bool _started;

        private int _currentProgress = 0;
        private int _totalProgress = 0;

        protected virtual void Awake()
        {
            LocomotionHandler = _locomotionHandler as ILocomotionEventHandler;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertCollectionField(_dots, nameof(_dots));
            this.AssertCollectionItems(_consumeTranslationEvents, nameof(_consumeTranslationEvents));
            this.AssertCollectionItems(_consumeRotationEvents, nameof(_consumeRotationEvents));
            this.AssertField(_pendingSprite, nameof(_pendingSprite));
            this.AssertField(_currentSprite, nameof(_currentSprite));
            this.AssertField(_completedSprite, nameof(_completedSprite));
            this.AssertField(LocomotionHandler, nameof(LocomotionHandler));

            _totalProgress = _dots.Length;
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                LocomotionHandler.WhenLocomotionEventHandled += LocomotionEventHandled;
                ResetProgress();
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                LocomotionHandler.WhenLocomotionEventHandled -= LocomotionEventHandled;
            }
        }


        private void LocomotionEventHandled(LocomotionEvent arg1, Pose arg2)
        {
            if (_consumeRotationEvents.Contains(arg1.Rotation)
                || _consumeTranslationEvents.Contains(arg1.Translation))
            {
                Progress();
            }
        }

        private void Progress()
        {
            _currentProgress++;
            if (_currentProgress <= _totalProgress)
            {
                _dots[_currentProgress - 1].sprite = _completedSprite;
            }
            if (_currentProgress < _totalProgress)
            {
                _dots[_currentProgress].sprite = _currentSprite;
            }

            if (_currentProgress >= _totalProgress)
            {
                WhenCompleted.Invoke();
            }
        }

        private void ResetProgress()
        {
            _currentProgress = 0;
            for (int i = 0; i < _dots.Length; i++)
            {
                _dots[i].sprite = i == 0 ? _currentSprite : _pendingSprite;
            }
        }

        #region Inject
        public void InjectAllLocomotionTutorialProgressTracker(Image[] dots,
            Sprite pendingSprite, Sprite currentSprite, Sprite completedSprite,
            List<LocomotionEvent.TranslationType> consumeTranslationEvents,
            List<LocomotionEvent.RotationType> consumeRotationEvents,
            ILocomotionEventHandler locomotionHandler)
        {
            InjectDots(dots);
            InjectPendingSprite(pendingSprite);
            InjectCurrentSprite(currentSprite);
            InjectCompletedSprite(completedSprite);
            InjectConsumeTranslationEvents(consumeTranslationEvents);
            InjectConsumeRotationEvents(consumeRotationEvents);
            InjectLocomotionHandler(locomotionHandler);
        }

        public void InjectDots(Image[] dots)
        {
            _dots = dots;
        }

        public void InjectPendingSprite(Sprite pendingSprite)
        {
            _pendingSprite = pendingSprite;
        }

        public void InjectCurrentSprite(Sprite currentSprite)
        {
            _currentSprite = currentSprite;
        }

        public void InjectCompletedSprite(Sprite completedSprite)
        {
            _completedSprite = completedSprite;
        }

        public void InjectConsumeTranslationEvents(List<LocomotionEvent.TranslationType> consumeTranslationEvents)
        {
            _consumeTranslationEvents = consumeTranslationEvents;
        }

        public void InjectConsumeRotationEvents(List<LocomotionEvent.RotationType> consumeRotationEvents)
        {
            _consumeRotationEvents = consumeRotationEvents;
        }

        public void InjectLocomotionHandler(ILocomotionEventHandler locomotionHandler)
        {
            _locomotionHandler = locomotionHandler as UnityEngine.Object;
            LocomotionHandler = locomotionHandler;
        }

        #endregion
    }
}
