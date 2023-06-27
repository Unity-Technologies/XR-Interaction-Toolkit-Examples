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
    public interface ISurface
    {
        /// <summary>
        /// A transform for the surface
        /// </summary>
        Transform Transform { get; }

        /// <summary>
        /// Raycast to the surface with an optional maximum distance value
        /// </summary>
        /// <param name="ray">The ray to cast</param>
        /// <param name="hit">The returned hit data</param>
        /// <param name="maxDistance">If greater than zero, maximum distance of raycast</param>
        /// <returns>true if surface was hit</returns>
        bool Raycast(in Ray ray, out SurfaceHit hit, float maxDistance = 0);

        /// <summary>
        /// Find nearest point to surface
        /// </summary>
        /// <param name="ray">Point to check</param>
        /// <param name="hit">The returned hit data</param>
        /// <param name="maxDistance">If greater than zero, maximum distance of check</param>
        /// <returns>true if nearest point was found</returns>
        bool ClosestSurfacePoint(in Vector3 point, out SurfaceHit hit, float maxDistance = 0);
    }

    public struct SurfaceHit
    {
        /// <summary>
        /// The position of the surface hit
        /// </summary>
        public Vector3 Point { get; set; }

        /// <summary>
        /// The normal of the surface hit
        /// </summary>
        public Vector3 Normal { get; set; }

        /// <summary>
        /// The distance of the surface hit from the ray origin
        /// </summary>
        public float Distance { get; set; }
    }
}
