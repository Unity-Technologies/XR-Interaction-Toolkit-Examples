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

using System;
using UnityEngine;
using Oculus.Interaction.Body.Input;
using System.Collections.Generic;

namespace Oculus.Interaction.Body.PoseDetection
{
    /// <summary>
    /// Exposes an <see cref="IBodyPose"/> from an <see cref="IBody"/>
    /// </summary>
    public class PoseFromBody : MonoBehaviour, IBodyPose
    {
        public event Action WhenBodyPoseUpdated = delegate { };

        [Tooltip("The IBodyPose will be derived from this IBody.")]
        [SerializeField, Interface(typeof(IBody))]
        private UnityEngine.Object _body;
        private IBody Body;

        [Tooltip("If true, this component will track the provided IBody as " +
            "its data is updated. If false, you must call " +
            nameof(UpdatePose) + " to update joint data.")]
        [SerializeField]
        private bool _autoUpdate = true;

        /// <summary>
        /// If true, this component will track the provided IBody as
        /// its data is updated. If false, you must call
        /// <see cref="UpdatePose"/> to update joint data.
        /// </summary>
        public bool AutoUpdate
        {
            get => _autoUpdate;
            set => _autoUpdate = value;
        }

        protected bool _started = false;

        private Dictionary<BodyJointId, Pose> _jointPosesLocal;
        private Dictionary<BodyJointId, Pose> _jointPosesFromRoot;

        public ISkeletonMapping SkeletonMapping => Body.SkeletonMapping;

        public bool GetJointPoseLocal(BodyJointId bodyJointId, out Pose pose) =>
            _jointPosesLocal.TryGetValue(bodyJointId, out pose);

        public bool GetJointPoseFromRoot(BodyJointId bodyJointId, out Pose pose) =>
            _jointPosesFromRoot.TryGetValue(bodyJointId, out pose);

        protected virtual void Awake()
        {
            _jointPosesLocal = new Dictionary<BodyJointId, Pose>();
            _jointPosesFromRoot = new Dictionary<BodyJointId, Pose>();
            Body = _body as IBody;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Body, nameof(Body));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Body.WhenBodyUpdated += Body_WhenBodyUpdated;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Body.WhenBodyUpdated -= Body_WhenBodyUpdated;
            }
        }

        private void Body_WhenBodyUpdated()
        {
            if (_autoUpdate)
            {
                UpdatePose();
            }
        }

        public void UpdatePose()
        {
            _jointPosesLocal.Clear();
            _jointPosesFromRoot.Clear();

            foreach (var joint in Body.SkeletonMapping.Joints)
            {
                if (Body.GetJointPoseLocal(joint,
                    out Pose localPose))
                {
                    _jointPosesLocal[joint] = localPose;
                }
                if (Body.GetJointPoseFromRoot(joint,
                    out Pose poseFromRoot))
                {
                    _jointPosesFromRoot[joint] = poseFromRoot;
                }
            }

            WhenBodyPoseUpdated.Invoke();
        }

        #region Inject

        public void InjectAllPoseFromBody(IBody body)
        {
            InjectBody(body);
        }

        public void InjectBody(IBody body)
        {
            _body = body as UnityEngine.Object;
            Body = body;
        }

        #endregion
    }
}
