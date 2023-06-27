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
    public static class TransformExtensions
    {
        /// <summary>
        /// Transforms position from world space to local space
        /// </summary>
        public static Vector3 InverseTransformPointUnscaled(this Transform transform, Vector3 position)
        {
            Matrix4x4 worldToLocal = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one).inverse;
            return worldToLocal.MultiplyPoint3x4(position);
        }

        /// <summary>
        /// Transforms position from local space to world space
        /// </summary>
        public static Vector3 TransformPointUnscaled(this Transform transform, Vector3 position)
        {
            Matrix4x4 localToWorld = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            return localToWorld.MultiplyPoint3x4(position);
        }

        /// <summary>
        /// Transform a bounding box from local to world space.
        /// </summary>
        /// <param name="transform">Transfrom that
        /// <paramref name="bounds"/> is local to</param>
        /// <param name="bounds">The bounds to transform, in local space</param>
        /// <returns>The bounding box in world space</returns>
        public static Bounds TransformBounds(this Transform transform, in Bounds bounds)
        {
            Bounds worldBounds = new Bounds();

            Vector3 boundsMin = bounds.min;
            Vector3 boundsMax = bounds.max;
            Vector3 min = transform.position;
            Vector3 max = transform.position;
            Matrix4x4 m = transform.localToWorldMatrix;

            for (int i = 0; i < 3; ++i)
            {
                for (int j = 0; j < 3; ++j)
                {
                    float e = m[i, j] * boundsMin[j];
                    float f = m[i, j] * boundsMax[j];
                    min[i] += (e < f) ? e : f;
                    max[i] += (e < f) ? f : e;
                }
            }

            worldBounds.SetMinMax(min, max);
            return worldBounds;
        }

        public static Transform FindChildRecursive(this Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name.Contains(name))
                    return child;

                var result = child.FindChildRecursive(name);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}
