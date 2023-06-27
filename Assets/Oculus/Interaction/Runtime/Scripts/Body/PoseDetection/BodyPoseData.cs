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

using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction.Body.Input;
using System;
using Oculus.Interaction.Collections;

namespace Oculus.Interaction.Body.PoseDetection
{
    [CreateAssetMenu(menuName = "Oculus/Interaction/SDK/Pose Detection/Body Pose")]
    public class BodyPoseData : ScriptableObject,
        IBodyPose, ISerializationCallbackReceiver
    {
        [System.Serializable]
        private struct JointData
        {
            public BodyJointId JointId;
            public BodyJointId ParentId;
            public Pose PoseFromRoot;
            public Pose LocalPose;
        }

        private class Mapping : ISkeletonMapping
        {
            public EnumerableHashSet<BodyJointId> Joints =
                new EnumerableHashSet<BodyJointId>();

            public Dictionary<BodyJointId, BodyJointId> JointToParent =
                new Dictionary<BodyJointId, BodyJointId>();

            IEnumerableHashSet<BodyJointId> ISkeletonMapping.Joints => Joints;

            bool ISkeletonMapping.TryGetParentJointId(BodyJointId jointId, out BodyJointId parent) =>
                JointToParent.TryGetValue(jointId, out parent);
        }

        public event Action WhenBodyPoseUpdated = delegate { };

        [SerializeField, HideInInspector]
        private List<JointData> _jointData = new List<JointData>();

        private Dictionary<BodyJointId, Pose> _posesFromRoot =
            new Dictionary<BodyJointId, Pose>();

        private Dictionary<BodyJointId, Pose> _localPoses =
            new Dictionary<BodyJointId, Pose>();

        private Mapping _mapping = new Mapping();

        public bool GetJointPoseFromRoot(BodyJointId bodyJointId, out Pose pose) =>
            _posesFromRoot.TryGetValue(bodyJointId, out pose);

        public bool GetJointPoseLocal(BodyJointId bodyJointId, out Pose pose) =>
            _localPoses.TryGetValue(bodyJointId, out pose);

        public ISkeletonMapping SkeletonMapping => _mapping;

        public void SetBodyPose(IBody body)
        {
            _jointData.Clear();
            foreach (var joint in body.SkeletonMapping.Joints)
            {
                if (body.GetJointPoseLocal(joint, out Pose local) &&
                    body.GetJointPoseFromRoot(joint, out Pose fromRoot) &&
                    body.SkeletonMapping.TryGetParentJointId(joint, out BodyJointId parent))
                {
                    _jointData.Add(new JointData()
                    {
                        JointId = joint,
                        ParentId = parent,
                        PoseFromRoot = fromRoot,
                        LocalPose = local,
                    });
                }
            }
            Rebuild();
            WhenBodyPoseUpdated.Invoke();
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            Rebuild();
        }

        private void Rebuild()
        {
            _localPoses.Clear();
            _posesFromRoot.Clear();
            _mapping.Joints.Clear();
            _mapping.JointToParent.Clear();

            for (int i = 0; i < _jointData.Count; ++i)
            {
                _localPoses[_jointData[i].JointId] =
                    _jointData[i].LocalPose;
                _posesFromRoot[_jointData[i].JointId] =
                    _jointData[i].PoseFromRoot;
                _mapping.Joints.Add(
                    _jointData[i].JointId);
                _mapping.JointToParent.Add(
                    _jointData[i].JointId, _jointData[i].ParentId);
            }
        }
    }
}
