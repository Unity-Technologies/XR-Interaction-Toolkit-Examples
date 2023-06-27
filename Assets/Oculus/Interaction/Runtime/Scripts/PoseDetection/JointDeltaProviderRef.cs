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
    /// JointDeltaProviderRef is a utility component that delegates all of its IJointDeltaProvider implementation
    /// to the provided JointDeltaProvider object.
    /// </summary>
    public class JointDeltaProviderRef : MonoBehaviour, IJointDeltaProvider
    {
        [SerializeField, Interface(typeof(IJointDeltaProvider))]
        private UnityEngine.Object _jointDeltaProvider;

        public IJointDeltaProvider JointDeltaProvider { get; private set; }

        protected virtual void Awake()
        {
            JointDeltaProvider = _jointDeltaProvider as IJointDeltaProvider;
        }

        protected virtual void Start()
        {
            this.AssertField(JointDeltaProvider, nameof(JointDeltaProvider));
        }

        public bool GetPositionDelta(HandJointId joint, out Vector3 delta)
        {
            return JointDeltaProvider.GetPositionDelta(joint, out delta);
        }

        public bool GetRotationDelta(HandJointId joint, out Quaternion delta)
        {
            return JointDeltaProvider.GetRotationDelta(joint, out delta);
        }

        public void RegisterConfig(JointDeltaConfig config)
        {
            JointDeltaProvider.RegisterConfig(config);
        }

        public void UnRegisterConfig(JointDeltaConfig config)
        {
            JointDeltaProvider.UnRegisterConfig(config);
        }

        #region Inject
        public void InjectAllJointDeltaProviderRef(IJointDeltaProvider jointDeltaProvider)
        {
            InjectJointDeltaProvider(jointDeltaProvider);
        }

        public void InjectJointDeltaProvider(IJointDeltaProvider jointDeltaProvider)
        {
            _jointDeltaProvider = jointDeltaProvider as UnityEngine.Object;
            JointDeltaProvider = jointDeltaProvider;
        }
        #endregion
    }
}
