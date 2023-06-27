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

using Oculus.Interaction.Surfaces;
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// Uses a Surface to determine the closest pose to snap to.
    /// </summary>
    public class SurfaceSnapPoseDelegate : MonoBehaviour, ISnapPoseDelegate
    {
        [SerializeField, Interface(typeof(ISurface))]
        private UnityEngine.Object _surface;
        protected ISurface Surface;

        private Dictionary<int, Pose> _snappedPoses;

        protected virtual void Awake()
        {
            Surface = _surface as ISurface;
            _snappedPoses = new Dictionary<int, Pose>();
        }

        protected virtual void Start()
        {
            this.AssertField(Surface, nameof(Surface));
        }

        public void TrackElement(int id, Pose p)
        { }

        public void UntrackElement(int id)
        { }

        private bool ComputeWorldSurfacePose(Pose pose, out Pose result)
        {
            if (Surface.ClosestSurfacePoint(pose.position, out SurfaceHit hit))
            {
                result = new Pose(hit.Point,
                    Quaternion.LookRotation(hit.Normal, pose.up));
                return true;
            }

            result = pose;
            return false;
        }

        private bool ComputeLocalSurfacePose(Pose pose, out Pose result)
        {
            if (ComputeWorldSurfacePose(pose, out Pose closestPoseWorld))
            {
                result = new Pose(
                    Surface.Transform.InverseTransformPoint(closestPoseWorld.position),
                    Quaternion.Inverse(closestPoseWorld.rotation) * Surface.Transform.rotation);
                return true;
            }

            result = pose;
            return false;
        }

        public void SnapElement(int id, Pose pose)
        {
            if (ComputeLocalSurfacePose(pose, out Pose result))
            {
                _snappedPoses.Add(id, result);
            }
            else
            {
                _snappedPoses.Add(id, pose);
            }
        }

        public void UnsnapElement(int id)
        {
            _snappedPoses.Remove(id);
        }

        public void MoveTrackedElement(int id, Pose p)
        {
        }

        public bool SnapPoseForElement(int id, Pose pose, out Pose result)
        {
            if (_snappedPoses.TryGetValue(id, out Pose localPose))
            {
                result = new Pose(Surface.Transform.TransformPoint(localPose.position),
                    Surface.Transform.rotation * localPose.rotation);
                return true;
            }

            return ComputeWorldSurfacePose(pose, out result);
        }
    }
}
