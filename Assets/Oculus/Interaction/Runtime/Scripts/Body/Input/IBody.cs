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
using System;

namespace Oculus.Interaction.Body.Input
{
    public interface IBody
    {
        /// <summary>
        /// The mapping for this body, which allows querying of the supported joint set
        /// of the skeleton as well as joint parent/child relationship.
        /// </summary>
        ISkeletonMapping SkeletonMapping { get; }

        /// <summary>
        /// Is the body connected and tracked
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// The body is connected and tracked, and the root pose's tracking data is marked as
        /// high confidence.
        /// If this is true, then it implies that <see cref="IsConnected"/> is also true,
        /// so they don't need to be checked in addition to this.
        /// </summary>
        bool IsHighConfidence { get; }

        /// <summary>
        /// True if the body is currently tracked, thus tracking poses are available for the body
        /// root and joints.
        /// </summary>
        bool IsTrackedDataValid { get; }

        /// <summary>
        /// The scale of the body skeleton applied to world joint poses.
        /// </summary>
        float Scale { get; }

        /// <summary>
        /// Gets the root pose of the body, in world space.
        /// Will return true if a pose was available; false otherwise.
        /// Confidence level of the pose is exposed via <see cref="IsHighConfidence"/>.
        /// </summary>
        bool GetRootPose(out Pose pose);

        /// <summary>
        /// Attempts to calculate the pose of the requested body joint, in world space.
        /// Returns false if the skeleton is not yet initialized, or there is no valid
        /// tracking data. Scale is applied to this joint position.
        /// </summary>
        bool GetJointPose(BodyJointId bodyJointId, out Pose pose);

        /// <summary>
        /// Attempts to calculate the pose of the requested body joint, in local space
        /// to its parent joint. Returns false if the skeleton is not yet initialized,
        /// or there is no valid tracking data. Scale is not applied.
        /// </summary>
        bool GetJointPoseLocal(BodyJointId bodyJointId, out Pose pose);

        /// <summary>
        /// Attempts to calculate the pose of the requested hand joint relative to the root.
        /// Returns false if the skeleton is not yet initialized, or there is no valid
        /// tracking data. Scale is not applied.
        /// </summary>
        bool GetJointPoseFromRoot(BodyJointId bodyJointId, out Pose pose);

        /// <summary>
        /// Incremented every time the source tracking or state data changes.
        /// </summary>
        int CurrentDataVersion { get; }

        /// <summary>
        /// Called each time the body is updated with new data
        /// </summary>
        event Action WhenBodyUpdated;
    }
}
