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
using Oculus.Interaction.Body.Input;

namespace Oculus.Interaction.Body
{
    public abstract class SkeletonDebugGizmos : MonoBehaviour
    {
        [System.Flags]
        public enum VisibilityFlags
        {
            Joints = 1 << 0,
            Axes = 1 << 1,
            Bones = 1 << 2,
        }

        [Tooltip("Which components of the skeleton will be visualized.")]
        [SerializeField]
        private VisibilityFlags _visibility =
            VisibilityFlags.Axes | VisibilityFlags.Joints;

        [Tooltip("The joint debug spheres will be drawn with this color.")]
        [SerializeField]
        private Color _jointColor = Color.white;

        [Tooltip("The bone connecting lines will be drawn with this color.")]
        [SerializeField]
        private Color _boneColor = Color.gray;

        [Tooltip("The radius of the joint spheres and the thickness " +
            "of the bone and axis lines.")]
        [SerializeField]
        private float _radius = 0.02f;

        public float Radius
        {
            get => _radius;
            set => _radius = value;
        }

        public VisibilityFlags Visibility
        {
            get => _visibility;
            set => _visibility = value;
        }

        public Color JointColor
        {
            get => _jointColor;
            set => _jointColor = value;
        }

        public Color BoneColor
        {
            get => _boneColor;
            set => _boneColor = value;
        }

        private float LineWidth => _radius / 2f;

        protected abstract bool TryGetWorldJointPose(BodyJointId jointId, out Pose pose);

        protected abstract bool TryGetParentJointId(BodyJointId jointId, out BodyJointId parent);

        protected bool HasNegativeScale => transform.lossyScale.x < 0 ||
                                           transform.lossyScale.y < 0 ||
                                           transform.lossyScale.z < 0;

        protected void Draw(BodyJointId joint, VisibilityFlags visibility)
        {
            if (TryGetWorldJointPose(joint, out Pose pose))
            {
                if (visibility.HasFlag(VisibilityFlags.Axes))
                {
                    DebugGizmos.LineWidth = LineWidth;
                    DebugGizmos.DrawAxis(pose, _radius);
                }
                if (visibility.HasFlag(VisibilityFlags.Joints))
                {
                    DebugGizmos.Color = _jointColor;
                    DebugGizmos.LineWidth = _radius;
                    DebugGizmos.DrawPoint(pose.position);
                }
                if (visibility.HasFlag(VisibilityFlags.Bones) &&
                    TryGetParentJointId(joint, out BodyJointId parent) &&
                    TryGetWorldJointPose(parent, out Pose parentPose))
                {
                    DebugGizmos.Color = _boneColor;
                    DebugGizmos.LineWidth = LineWidth;
                    DebugGizmos.DrawLine(pose.position, parentPose.position);
                }
            }
        }
    }
}
