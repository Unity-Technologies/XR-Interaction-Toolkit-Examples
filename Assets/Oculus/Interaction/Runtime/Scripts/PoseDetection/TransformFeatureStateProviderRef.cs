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
    /// TransformFeatureStateProviderRef is a utility component that delegates all of its ITransformFeatureStateProvider implementation
    /// to the provided TransformFeatureStateProvider object.
    /// </summary>
    public class TransformFeatureStateProviderRef : MonoBehaviour, ITransformFeatureStateProvider
    {
        [SerializeField, Interface(typeof(ITransformFeatureStateProvider))]
        private UnityEngine.Object _transformFeatureStateProvider;

        public ITransformFeatureStateProvider TransformFeatureStateProvider { get; private set; }

        protected virtual void Awake()
        {
            TransformFeatureStateProvider = _transformFeatureStateProvider as ITransformFeatureStateProvider;
        }

        protected virtual void Start()
        {
            this.AssertField(TransformFeatureStateProvider, nameof(TransformFeatureStateProvider));
        }

        public bool IsStateActive(TransformConfig config, TransformFeature feature, FeatureStateActiveMode mode,
            string stateId)
        {
            return TransformFeatureStateProvider.IsStateActive(config, feature, mode, stateId);
        }

        public bool GetCurrentState(TransformConfig config, TransformFeature transformFeature,
            out string currentState)
        {
            return TransformFeatureStateProvider.GetCurrentState(config, transformFeature, out currentState);
        }

        public void RegisterConfig(TransformConfig transformConfig)
        {
            TransformFeatureStateProvider.RegisterConfig(transformConfig);
        }

        public void UnRegisterConfig(TransformConfig transformConfig)
        {
            TransformFeatureStateProvider.UnRegisterConfig(transformConfig);
        }

        public void GetFeatureVectorAndWristPos(TransformConfig config, TransformFeature transformFeature,
            bool isHandVector, ref Vector3? featureVec, ref Vector3? wristPos)
        {
            TransformFeatureStateProvider.GetFeatureVectorAndWristPos(config, transformFeature, isHandVector, ref featureVec, ref wristPos);
        }

        #region Inject
        public void InjectAllTransformFeatureStateProviderRef(ITransformFeatureStateProvider transformFeatureStateProvider)
        {
            InjectTransformFeatureStateProvider(transformFeatureStateProvider);
        }

        public void InjectTransformFeatureStateProvider(ITransformFeatureStateProvider transformFeatureStateProvider)
        {
            _transformFeatureStateProvider = transformFeatureStateProvider as UnityEngine.Object;
            TransformFeatureStateProvider = transformFeatureStateProvider;
        }
        #endregion
    }
}
