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

namespace Oculus.Interaction
{
    /// <summary>
    /// A SnapPoseDelegate can be used to provide custom snap pose logic
    /// given a set of elements that are either being tracked or are snapped.
    /// </summary>
    public interface ISnapPoseDelegate
    {
        /// <summary>
        /// Indicates that a new element is tracking.
        /// </summary>
        /// <param name="id">The element id to track.</param>
        /// <param name="pose">The pose of the element.</param>
        void TrackElement(int id, Pose p);
        /// <summary>
        /// Indicates that an element is no longer being tracked.
        /// </summary>
        /// <param name="id">The element id to stop tracking.</param>
        void UntrackElement(int id);
        /// <summary>
        /// Indicates that the tracked element should snap.
        /// </summary>
        /// <param name="id">The element id to snap.</param>
        /// <param name="pose">The pose of the element.</param>
        void SnapElement(int id, Pose pose);
        /// <summary>
        /// Indicates that the element should no longer snap.
        /// </summary>
        /// <param name="id">The element id to stop snapping.</param>
        void UnsnapElement(int id);
        /// <summary>
        /// Indicates that a tracked element pose has updated.
        /// </summary>
        /// <param name="id">The element id.</param>
        /// <param name="pose">The new element pose.</param>
        void MoveTrackedElement(int id, Pose p);
        /// <summary>
        /// The target snap pose for a queried element id.
        /// </summary>
        /// <param name="id">The element id.</param>
        /// <param name="pose">The target pose.</param>
        /// <returns>True if the element has a pose to snap to.</returns>
        bool SnapPoseForElement(int id, Pose pose, out Pose result);
    }
}
