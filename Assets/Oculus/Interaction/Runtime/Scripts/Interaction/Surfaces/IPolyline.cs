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
    /// Defines an sequence of points in 3D space
    /// </summary>
    public interface IPolyline
    {
        /// <summary>
        /// Max number of points that define the polyline
        /// </summary>
        int PointsCount { get; }

        /// <summary>
        /// Calculates the position N vertex of the polyline
        /// </summary>
        /// <param name="index">The N vertex of the polyline been queried.</param>
        /// <returns>The position of the polyline at the index-th point</returns>
        Vector3 PointAtIndex(int index);
    }
}
