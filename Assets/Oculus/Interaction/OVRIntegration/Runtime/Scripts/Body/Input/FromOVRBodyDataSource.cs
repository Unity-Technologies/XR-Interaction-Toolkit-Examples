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
using Oculus.Interaction.Input;

namespace Oculus.Interaction.Body.Input
{
    using IOVRSkeletonDataProvider = OVRSkeleton.IOVRSkeletonDataProvider;

    public class FromOVRBodyDataSource : DataSource<BodyDataAsset>
    {
        [Header("OVR Data Source")]
        [SerializeField, Interface(typeof(IOVRSkeletonDataProvider))]
        private UnityEngine.Object _dataProvider;
        private IOVRSkeletonDataProvider DataProvider;

        [SerializeField, Interface(typeof(IOVRCameraRigRef))]
        private UnityEngine.Object _cameraRigRef;
        private IOVRCameraRigRef CameraRigRef;

        [SerializeField]
        private bool _processLateUpdates = false;

        protected override BodyDataAsset DataAsset => _bodyDataAsset;

        private readonly BodyDataAsset _bodyDataAsset = new BodyDataAsset();

        private readonly OVRSkeletonMapping _mapping = new OVRSkeletonMapping();

        protected void Awake()
        {
            CameraRigRef = _cameraRigRef as IOVRCameraRigRef;
            DataProvider = _dataProvider as IOVRSkeletonDataProvider;
        }

        protected override void Start()
        {
            base.Start();
            this.AssertField(DataProvider, nameof(DataProvider));
            this.AssertField(CameraRigRef, nameof(CameraRigRef));

            _bodyDataAsset.SkeletonMapping = _mapping;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_started)
            {
                CameraRigRef.WhenInputDataDirtied += HandleInputDataDirtied;
            }
        }

        protected override void OnDisable()
        {
            if (_started)
            {
                CameraRigRef.WhenInputDataDirtied -= HandleInputDataDirtied;
            }

            base.OnDisable();
        }

        private void HandleInputDataDirtied(bool isLateUpdate)
        {
            if (isLateUpdate && !_processLateUpdates)
            {
                return;
            }
            MarkInputDataRequiresUpdate();
        }

        protected override void UpdateData()
        {
            var data = DataProvider.GetSkeletonPoseData();
            if (!data.IsDataValid)
            {
                return;
            }

            _bodyDataAsset.SkeletonMapping = _mapping;
            _bodyDataAsset.IsDataHighConfidence = data.IsDataHighConfidence;
            _bodyDataAsset.IsDataValid = data.IsDataValid;
            _bodyDataAsset.SkeletonChangedCount = data.SkeletonChangedCount;
            _bodyDataAsset.RootScale = data.RootScale;

            _bodyDataAsset.Root = new Pose()
            {
                position = data.RootPose.Position.FromFlippedZVector3f(),
                rotation = data.RootPose.Orientation.FromFlippedZQuatf()
            };

            foreach (var jointId in _mapping.Joints)
            {
                Pose pose = default(Pose);
                if (_mapping.TryGetSourceJointId(jointId, out OVRPlugin.BoneId boneId))
                {
                    int index = (int)boneId;
                    pose = new Pose()
                    {
                        rotation = float.IsNaN(data.BoneRotations[index].w)
                            ? default(Quaternion)
                            : data.BoneRotations[index].FromFlippedZQuatf(),
                        position = data.BoneTranslations[index].FromFlippedZVector3f()
                    };

                }
                _bodyDataAsset.JointPoses[(int)jointId] = pose;
            }
        }
    }
}
