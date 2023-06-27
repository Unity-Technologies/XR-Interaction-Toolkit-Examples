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

namespace Oculus.Interaction.Grab.GrabSurfaces
{
    public class ColliderGrabSurface : MonoBehaviour, IGrabSurface
    {
        [SerializeField]
        private Collider _collider;

        protected virtual void Start()
        {
            this.AssertField(_collider, nameof(_collider));
        }

        private Vector3 NearestPointInSurface(Vector3 targetPosition)
        {
            if (_collider.bounds.Contains(targetPosition))
            {
                targetPosition = _collider.ClosestPointOnBounds(targetPosition);
            }
            return _collider.ClosestPoint(targetPosition);
        }

        public GrabPoseScore CalculateBestPoseAtSurface(in Pose targetPose, out Pose bestPose,
            in PoseMeasureParameters scoringModifier, Transform relativeTo)
        {
            Vector3 surfacePoint = NearestPointInSurface(targetPose.position);
            bestPose = new Pose(surfacePoint, targetPose.rotation);
            return new GrabPoseScore(surfacePoint, targetPose.position);
        }

        public bool CalculateBestPoseAtSurface(Ray targetRay, out Pose bestPose, Transform relativeTo)
        {
            if (_collider.Raycast(targetRay, out RaycastHit hit, Mathf.Infinity))
            {
                bestPose.position = hit.point;
                bestPose.rotation = relativeTo.rotation;
                return true;
            }
            bestPose = Pose.identity;
            return false;
        }


        public Pose MirrorPose(in Pose gripPose, Transform relativeTo)
        {
            return gripPose;
        }

        public IGrabSurface CreateMirroredSurface(GameObject gameObject)
        {
            return CreateDuplicatedSurface(gameObject);
        }

        public IGrabSurface CreateDuplicatedSurface(GameObject gameObject)
        {
            ColliderGrabSurface colliderSurface = gameObject.AddComponent<ColliderGrabSurface>();
            colliderSurface.InjectAllColliderGrabSurface(_collider);
            return colliderSurface;
        }

        #region Inject
        public void InjectAllColliderGrabSurface(Collider collider)
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
