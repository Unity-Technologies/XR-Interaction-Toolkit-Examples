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
using Oculus.Interaction.PoseDetection;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// FingerFeatureStateProviderRef is a utility component that delegates all of its IFingerFeatureStateProvider implementation
    /// to the provided FingerFeatureStateProvider object.
    /// </summary>
    public class FingerFeatureStateProviderRef : MonoBehaviour, IFingerFeatureStateProvider
    {
        [SerializeField, Interface(typeof(IFingerFeatureStateProvider))]
        private UnityEngine.Object _fingerFeatureStateProvider;

        public IFingerFeatureStateProvider FingerFeatureStateProvider { get; private set; }

        protected virtual void Awake()
        {
            FingerFeatureStateProvider = _fingerFeatureStateProvider as IFingerFeatureStateProvider;
        }

        protected virtual void Start()
        {
            this.AssertField(FingerFeatureStateProvider, nameof(FingerFeatureStateProvider));
        }

        public bool GetCurrentState(HandFinger finger, FingerFeature fingerFeature, out string currentState)
        {
            return FingerFeatureStateProvider.GetCurrentState(finger, fingerFeature, out currentState);
        }

        public bool IsStateActive(HandFinger finger, FingerFeature feature, FeatureStateActiveMode mode,
            string stateId)
        {
            return FingerFeatureStateProvider.IsStateActive(finger, feature, mode, stateId);
        }

        public float? GetFeatureValue(HandFinger finger, FingerFeature fingerFeature)
        {
            return FingerFeatureStateProvider.GetFeatureValue(finger, fingerFeature);
        }

        #region Inject
        public void InjectAllFingerFeatureStateProviderRef(IFingerFeatureStateProvider fingerFeatureStateProvider)
        {
            InjectFingerFeatureStateProvider(fingerFeatureStateProvider);
        }

        public void InjectFingerFeatureStateProvider(IFingerFeatureStateProvider fingerFeatureStateProvider)
        {
            _fingerFeatureStateProvider = fingerFeatureStateProvider as UnityEngine.Object;
            FingerFeatureStateProvider = fingerFeatureStateProvider;
        }
        #endregion

    }
}
