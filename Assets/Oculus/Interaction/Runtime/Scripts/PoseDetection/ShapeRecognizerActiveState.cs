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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction.PoseDetection
{
    public class ShapeRecognizerActiveState : MonoBehaviour, IActiveState
    {
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;
        public IHand Hand { get; private set; }

        [SerializeField, Interface(typeof(IFingerFeatureStateProvider))]
        private UnityEngine.Object _fingerFeatureStateProvider;

        protected IFingerFeatureStateProvider FingerFeatureStateProvider;

        [SerializeField]
        private ShapeRecognizer[] _shapes;
        public IReadOnlyList<ShapeRecognizer> Shapes => _shapes;
        public Handedness Handedness => Hand.Handedness;

        struct FingerFeatureStateUsage
        {
            public HandFinger handFinger;
            public ShapeRecognizer.FingerFeatureConfig config;
        }

        private List<FingerFeatureStateUsage> _allFingerStates = new List<FingerFeatureStateUsage>();

        // keeps track of native state
        private bool _nativeActive = false;

        protected virtual void Awake()
        {
            Hand = _hand as IHand;
            FingerFeatureStateProvider = _fingerFeatureStateProvider as IFingerFeatureStateProvider;
        }

        protected virtual void Start()
        {
            this.AssertField(Hand, nameof(Hand));
            this.AssertField(FingerFeatureStateProvider, nameof(FingerFeatureStateProvider));
            this.AssertCollectionField(_shapes, nameof(_shapes));

            _allFingerStates = FlattenUsedFeatures();

            // Warm up the proactive evaluation
            InitStateProvider();
        }

        private void InitStateProvider()
        {
            foreach (FingerFeatureStateUsage state in _allFingerStates)
            {
                FingerFeatureStateProvider.GetCurrentState(state.handFinger, state.config.Feature, out _);
            }
        }

        private List<FingerFeatureStateUsage> FlattenUsedFeatures()
        {
            var fingerFeatureStateUsages = new List<FingerFeatureStateUsage>();
            foreach (var sr in _shapes)
            {
                int configCount = 0;
                for (var fingerIdx = 0; fingerIdx < Constants.NUM_FINGERS; ++fingerIdx)
                {
                    var handFinger = (HandFinger)fingerIdx;
                    foreach (var config in sr.GetFingerFeatureConfigs(handFinger))
                    {
                        ++configCount;
                        fingerFeatureStateUsages.Add(new FingerFeatureStateUsage()
                        {
                            handFinger = handFinger, config = config
                        });
                    }
                }

                // If this assertion is hit, open the ScriptableObject in the Unity Inspector
                // and ensure that it has at least one valid condition.
                Assert.IsTrue(configCount > 0, $"Shape {sr.ShapeName} has no valid conditions.");
            }

            return fingerFeatureStateUsages;
        }

        public bool Active
        {
            get
            {
                if (!isActiveAndEnabled || _allFingerStates.Count == 0)
                {
                    return (_nativeActive = false);
                }

                foreach (FingerFeatureStateUsage stateUsage in _allFingerStates)
                {
                    if (!FingerFeatureStateProvider.IsStateActive(stateUsage.handFinger,
                        stateUsage.config.Feature, stateUsage.config.Mode, stateUsage.config.State))
                    {
                        return (_nativeActive = false);
                    }
                }

                if (!_nativeActive)
                {
                    // Activate native component
                    int result = NativeMethods.isdk_NativeComponent_Activate(0x48506f7365446574);
                    this.AssertIsTrue(result == NativeMethods.IsdkSuccess, "Unable to Activate native recognizer!");
                }

                return (_nativeActive = true);
            }
        }

        #region Inject
        public void InjectAllShapeRecognizerActiveState(IHand hand,
            IFingerFeatureStateProvider fingerFeatureStateProvider,
            ShapeRecognizer[] shapes)
        {
            InjectHand(hand);
            InjectFingerFeatureStateProvider(fingerFeatureStateProvider);
            InjectShapes(shapes);
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as UnityEngine.Object;
            Hand = hand;
        }

        public void InjectFingerFeatureStateProvider(IFingerFeatureStateProvider fingerFeatureStateProvider)
        {
            _fingerFeatureStateProvider = fingerFeatureStateProvider as UnityEngine.Object;
            FingerFeatureStateProvider = fingerFeatureStateProvider;
        }

        public void InjectShapes(ShapeRecognizer[] shapes)
        {
            _shapes = shapes;
        }
        #endregion
    }
}
