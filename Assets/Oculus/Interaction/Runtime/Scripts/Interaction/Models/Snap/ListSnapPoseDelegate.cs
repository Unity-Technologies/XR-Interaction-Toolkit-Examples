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

namespace Oculus.Interaction
{
    public class ListSnapPoseDelegate : MonoBehaviour, ISnapPoseDelegate
    {
        private HashSet<int> _snappedIds;
        private ListLayout _layout;
        private ListLayoutEase _layoutEase;

        [SerializeField]
        private float _defaultSize = 1f;

        protected virtual void Start()
        {
            _snappedIds = new HashSet<int>();
            _layout = new ListLayout();
            _layoutEase = new ListLayoutEase(_layout);
            _layoutEase.UpdateTime(Time.timeSinceLevelLoad);
        }

        protected virtual void Update()
        {
            _layoutEase.UpdateTime(Time.timeSinceLevelLoad);
        }

        protected virtual float SizeForId(int id)
        {
            return _defaultSize;
        }

        protected virtual float FloatForPose(Pose pose)
        {
            return transform.InverseTransformPoint(pose.position).x;
        }

        protected virtual Pose PoseForFloat(float position)
        {
            return new Pose(transform.TransformPoint(new Vector3(position, 0, 0)), transform.rotation);
        }

        public void TrackElement(int id, Pose p)
        {
            _layout.AddElement(id, SizeForId(id), FloatForPose(p));
        }

        public void UntrackElement(int id)
        {
            _layout.RemoveElement(id);
        }

        public void SnapElement(int id, Pose pose)
        {
            _snappedIds.Add(id);
        }

        public void UnsnapElement(int id)
        {
            _snappedIds.Remove(id);
        }

        public void MoveTrackedElement(int id, Pose p)
        {
            _layout.MoveElement(id, FloatForPose(p));
        }

        public bool SnapPoseForElement(int id, Pose pose, out Pose result)
        {
            if (_snappedIds.Contains(id))
            {
                result = PoseForFloat(_layoutEase.GetPosition(id));
            }
            else
            {
                result = PoseForFloat(_layout.GetTargetPosition(id, FloatForPose(pose), SizeForId(id)));
            }

            return true;
        }

        public float Size => _layout.Size;
    }
}
