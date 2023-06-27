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

namespace Oculus.Interaction.HandGrab
{
    /// <summary>
    /// All the relevant data needed for a snapping position.
    /// This includes the Interactor and the surface (if any) around
    /// which the pose is valid.
    /// </summary>
    public class HandGrabTarget
    {
        public enum GrabAnchor
        {
            None,
            Wrist,
            Pinch,
            Palm
        }

        private Transform _relativeTo;
        private HandGrabResult _handGrabResult = new HandGrabResult();

        public HandPose HandPose => _handGrabResult.HasHandPose ? _handGrabResult.HandPose : null;

        public Pose GetWorldPoseDisplaced(in Pose offset)
        {
            Pose displaced = PoseUtils.Multiply(_handGrabResult.RelativePose, offset);
            return PoseUtils.GlobalPose(_relativeTo, displaced);
        }

        public HandAlignType HandAlignment { get; private set; } = HandAlignType.None;
        public GrabAnchor Anchor { get; private set; } = GrabAnchor.None;

        public void Set(Transform relativeTo, HandAlignType handAlignment, GrabAnchor anchor, HandGrabResult handGrabResult)
        {
            Anchor = anchor;
            HandAlignment = handAlignment;
            _relativeTo = relativeTo;
            _handGrabResult.CopyFrom(handGrabResult);
        }
    }
}
