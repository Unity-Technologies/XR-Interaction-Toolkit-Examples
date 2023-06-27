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

namespace Oculus.Interaction.Surfaces
{
    public interface ICylinderClipper
    {
        /// <summary>
        /// Get the segment defining a portion of a cylinder surface
        /// </summary>
        /// <param name="segment">The segment defining the clip area
        /// of a cylinder</param>
        /// <returns>True if clipping should be performed</returns>
        public bool GetCylinderSegment(out CylinderSegment segment);
    }

    /// <summary>
    /// Uses a bounding box to perform clipping
    /// </summary>
    public interface IBoundsClipper
    {
        /// <summary>
        /// Get the bounding box in the local space of a
        /// provided transform.
        /// </summary>
        /// <param name="localTo">The transform the bounds
        /// will be local to</param>
        /// <param name="bounds">The bounding box in local space of
        /// <paramref name="localTo"/></param>
        /// <returns>True if clipping should be performed</returns>
        public bool GetLocalBounds(Transform localTo, out Bounds bounds);
    }
}
