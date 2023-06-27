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
    public class ColliderSurface : MonoBehaviour, ISurface, IBounds
    {
        [Tooltip("The Surface will be represented by this collider.")]
        [SerializeField]
        private Collider _collider;

        protected virtual void Start()
        {
            this.AssertField(_collider, nameof(_collider));
        }

        public Transform Transform => transform;

        public Bounds Bounds => _collider.bounds;

        public bool Raycast(in Ray ray, out SurfaceHit hit, float maxDistance)
        {
            hit = new SurfaceHit();

            RaycastHit hitInfo;

            if (_collider.Raycast(ray, out hitInfo, maxDistance))
            {
                hit.Point = hitInfo.point;
                hit.Normal = hitInfo.normal;
                hit.Distance = hitInfo.distance;
                return true;
            }

            return false;
        }

        public bool ClosestSurfacePoint(in Vector3 point, out SurfaceHit hit, float maxDistance = 0)
        {
            Vector3 closest = _collider.ClosestPoint(point);

            Vector3 delta = closest - point;
            if (delta.x == 0f && delta.y == 0f && delta.z == 0f)
            {
                Vector3 direction = _collider.bounds.center - point;
                return Raycast(new Ray(point - direction,
                    direction), out hit, float.MaxValue);
            }

            return Raycast(new Ray(point, delta), out hit, maxDistance);
        }

        #region Inject

        public void InjectAllColliderSurface(Collider collider)
        {
            InjectCollider(collider);
        }

        public void InjectCollider(Collider collider)
        {
            _collider = collider;
        }

        #endregion
    }
}
