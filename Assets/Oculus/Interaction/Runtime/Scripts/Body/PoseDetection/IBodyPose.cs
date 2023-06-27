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

namespace Oculus.Interaction.Body.PoseDetection
{
    public interface IBodyPose
    {
        /// <summary>
        /// Called each time the body pose is updated with new data
        /// </summary>
        event Action WhenBodyPoseUpdated;

        /// <summary>
        /// The mapping of the skeleton
        /// </summary>
        ISkeletonMapping SkeletonMapping { get; }

        /// <summary>
        /// Attempts to return the pose of the requested body joint,
        /// in local space relative to its parent joint.
        /// </summary>
        bool GetJointPoseLocal(BodyJointId bodyJointId, out Pose pose);

        /// <summary>
        /// Attempts to return the pose of the requested body joint relative
        /// to the root joint <see cref="BodyJointId.Body_Root"/>.
        /// </summary>
        bool GetJointPoseFromRoot(BodyJointId bodyJointId, out Pose pose);
    }
}
