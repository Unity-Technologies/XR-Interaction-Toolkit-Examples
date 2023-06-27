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

namespace Oculus.Interaction.Body.Input
{
    public class BodyJointsCache
    {
        private const int DIRTY_ARRAY_SIZE = 1 + (Constants.NUM_BODY_JOINTS / 64);

        public int LocalDataVersion { get; private set; } = -1;

        private Pose[] _originalPoses = new Pose[Constants.NUM_BODY_JOINTS];
        private Pose[] _posesFromRoot = new Pose[Constants.NUM_BODY_JOINTS];
        private Pose[] _localPoses = new Pose[Constants.NUM_BODY_JOINTS];
        private Pose[] _worldPoses = new Pose[Constants.NUM_BODY_JOINTS];

        private ReadOnlyBodyJointPoses _posesFromRootCollection;
        private ReadOnlyBodyJointPoses _worldPosesCollection;
        private ReadOnlyBodyJointPoses _localPosesCollection;

        private ulong[] _dirtyJointsFromRoot;
        private ulong[] _dirtyLocalJoints;
        private ulong[] _dirtyWorldJoints;

        private Matrix4x4 _scale;
        private Pose _rootPose;
        private Pose _worldRoot;

        private ISkeletonMapping _mapping;

        public BodyJointsCache()
        {
            LocalDataVersion = -1;

            _dirtyJointsFromRoot = new ulong[DIRTY_ARRAY_SIZE];
            _dirtyLocalJoints = new ulong[DIRTY_ARRAY_SIZE];
            _dirtyWorldJoints = new ulong[DIRTY_ARRAY_SIZE];

            _localPosesCollection = new ReadOnlyBodyJointPoses(_localPoses);
            _worldPosesCollection = new ReadOnlyBodyJointPoses(_worldPoses);
            _posesFromRootCollection = new ReadOnlyBodyJointPoses(_posesFromRoot);
        }

        public void Update(BodyDataAsset data, int dataVersion,
            Transform trackingSpace = null)
        {
            LocalDataVersion = dataVersion;
            _mapping = data.SkeletonMapping;

            for (int i = 0; i < DIRTY_ARRAY_SIZE; ++i)
            {
                _dirtyJointsFromRoot[i] = ulong.MaxValue;
                _dirtyLocalJoints[i] = ulong.MaxValue;
                _dirtyWorldJoints[i] = ulong.MaxValue;
            }
            if (!data.IsDataValid)
            {
                return;
            }

            _scale = Matrix4x4.Scale(Vector3.one * data.RootScale);
            _rootPose = data.Root;
            _worldRoot = _rootPose;

            if (trackingSpace != null)
            {
                _scale *= Matrix4x4.Scale(trackingSpace.lossyScale);
                _worldRoot.position = trackingSpace.TransformPoint(_rootPose.position);
                _worldRoot.rotation = trackingSpace.rotation * _rootPose.rotation;
            }

            for (int i = 0; i < Constants.NUM_BODY_JOINTS; ++i)
            {
                _originalPoses[i] = data.JointPoses[i];
            }
        }

        public bool GetAllLocalPoses(out ReadOnlyBodyJointPoses localJointPoses)
        {
            UpdateAllLocalPoses();
            localJointPoses = _localPosesCollection;
            return _localPosesCollection.Count > 0;
        }

        public bool GetAllPosesFromRoot(out ReadOnlyBodyJointPoses posesFromRoot)
        {
            UpdateAllPosesFromRoot();
            posesFromRoot = _posesFromRootCollection;
            return _posesFromRootCollection.Count > 0;
        }

        public bool GetAllWorldPoses(out ReadOnlyBodyJointPoses worldJointPoses)
        {
            UpdateAllWorldPoses();
            worldJointPoses = _worldPosesCollection;
            return _worldPosesCollection.Count > 0;
        }

        public Pose GetLocalJointPose(BodyJointId jointId)
        {
            UpdateLocalJointPose(jointId);
            return _localPoses[(int)jointId];
        }

        public Pose GetJointPoseFromRoot(BodyJointId jointId)
        {
            UpdateJointPoseFromRoot(jointId);
            return _posesFromRoot[(int)jointId];
        }

        public Pose GetWorldJointPose(BodyJointId jointId)
        {
            UpdateWorldJointPose(jointId);
            return _worldPoses[(int)jointId];
        }

        public Pose GetWorldRootPose()
        {
            return _worldRoot;
        }

        private void UpdateJointPoseFromRoot(BodyJointId jointId)
        {
            if (!CheckJointDirty(jointId, _dirtyJointsFromRoot))
            {
                return;
            }

            Pose originalPose = _originalPoses[(int)jointId];
            Vector3 posFromRoot = Quaternion.Inverse(_rootPose.rotation) *
                (originalPose.position - _rootPose.position);
            Quaternion rotFromRoot = Quaternion.Inverse(_rootPose.rotation) *
                originalPose.rotation;
            _posesFromRoot[(int)jointId] = new Pose(posFromRoot, rotFromRoot);

            SetJointClean(jointId, _dirtyJointsFromRoot);
        }

        private void UpdateLocalJointPose(BodyJointId jointId)
        {
            if (!CheckJointDirty(jointId, _dirtyLocalJoints))
            {
                return;
            }

            if (_mapping.TryGetParentJointId(jointId, out BodyJointId parentId) &&
                parentId >= BodyJointId.Body_Root)
            {
                Pose originalPose = _originalPoses[(int)jointId];
                Pose parentPose = _originalPoses[(int)parentId];

                Vector3 localPos = Quaternion.Inverse(parentPose.rotation) *
                    (originalPose.position - parentPose.position);
                Quaternion localRot = Quaternion.Inverse(parentPose.rotation) *
                    originalPose.rotation;
                _localPoses[(int)jointId] = new Pose(localPos, localRot);
            }
            else
            {
                _localPoses[(int)jointId] = Pose.identity;
            }
            SetJointClean(jointId, _dirtyLocalJoints);
        }

        private void UpdateWorldJointPose(BodyJointId jointId)
        {
            if (!CheckJointDirty(jointId, _dirtyWorldJoints))
            {
                return;
            }

            Pose fromRoot = GetJointPoseFromRoot(jointId);
            fromRoot.position = _scale * fromRoot.position;
            fromRoot.Postmultiply(GetWorldRootPose());
            _worldPoses[(int)jointId] = fromRoot;
            SetJointClean(jointId, _dirtyWorldJoints);
        }

        private void UpdateAllWorldPoses()
        {
            foreach (BodyJointId joint in _mapping.Joints)
            {
                UpdateWorldJointPose(joint);
            }
        }

        private void UpdateAllLocalPoses()
        {
            foreach (BodyJointId joint in _mapping.Joints)
            {
                UpdateLocalJointPose(joint);
            }
        }

        private void UpdateAllPosesFromRoot()
        {
            foreach (BodyJointId joint in _mapping.Joints)
            {
                UpdateJointPoseFromRoot(joint);
            }
        }

        private bool CheckJointDirty(BodyJointId jointId, ulong[] dirtyFlags)
        {
            int outerIdx = (int)jointId / 64;
            int innerIdx = (int)jointId % 64;
            return (dirtyFlags[outerIdx] & (1UL << innerIdx)) != 0;
        }

        private void SetJointClean(BodyJointId jointId, ulong[] dirtyFlags)
        {
            int outerIdx = (int)jointId / 64;
            int innerIdx = (int)jointId % 64;
            dirtyFlags[outerIdx] = dirtyFlags[outerIdx] & ~(1UL << innerIdx);
        }
    }
}
