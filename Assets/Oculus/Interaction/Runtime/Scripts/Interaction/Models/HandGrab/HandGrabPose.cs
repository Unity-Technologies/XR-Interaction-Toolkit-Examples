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

using Oculus.Interaction.Grab;
using Oculus.Interaction.Grab.GrabSurfaces;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Oculus.Interaction.HandGrab
{
    /// <summary>
    /// The HandGrabPose defines the local point in an object to which the grip point
    /// of the hand should align. It can also contain information about the final pose
    /// of the hand for perfect alignment as well as a surface that indicates the valid
    /// positions for the point.
    /// </summary>
    public class HandGrabPose : MonoBehaviour
    {
        [SerializeField, Optional, Interface(typeof(IGrabSurface))]
        private UnityEngine.Object _surface = null;
        private IGrabSurface _snapSurface;
        public IGrabSurface SnapSurface
        {
            get => _snapSurface ?? _surface as IGrabSurface;
            private set
            {
                _snapSurface = value;
            }
        }

        [SerializeField]
        [Tooltip("Transform used as a reference to measure the local data of the HandGrabPose")]
        private Transform _relativeTo;

        [SerializeField]
        private bool _usesHandPose = true;

        [SerializeField, Optional]
        [HideInInspector]
        private HandPose _handPose = new HandPose();

        public HandPose HandPose => _usesHandPose ? _handPose : null;

        /// <summary>
        /// Scale of the HandGrabPoint relative to its reference transform.
        /// </summary>
        public float RelativeScale
        {
            get
            {
                return this.transform.lossyScale.x / _relativeTo.lossyScale.x;
            }
        }

        /// <summary>
        /// Pose of the HandGrabPose relative to its reference transform.
        /// </summary>
        public Pose RelativePose
        {
            get
            {
                if (_relativeTo != null)
                {
                    return PoseUtils.DeltaScaled(_relativeTo, transform);
                }
                else
                {
                    return transform.GetPose(Space.Self);
                }
            }
        }

        /// <summary>
        /// Reference transform of the HandGrabPose
        /// </summary>
        public Transform RelativeTo => _relativeTo;

        #region editor events
        protected virtual void Reset()
        {
            _relativeTo = this.GetComponentInParent<IRelativeToRef>()?.RelativeTo;
        }
        #endregion

        public bool UsesHandPose()
        {
            return _usesHandPose;
        }

        public virtual bool CalculateBestPose(Pose userPose,
            Handedness handedness, PoseMeasureParameters scoringModifier,
            Transform relativeTo, ref HandGrabResult result)
        {
            result.HasHandPose = false;
            if (HandPose != null
                && HandPose.Handedness != handedness)
            {
                return false;
            }

            result.Score = CompareNearPoses(userPose,
                scoringModifier, relativeTo, out Pose worldPose);
            result.RelativePose = PoseUtils.Delta(relativeTo, worldPose);
            if (HandPose != null)
            {
                result.HasHandPose = true;
                result.HandPose.CopyFrom(HandPose);
            }

            return true;
        }

        /// <summary>
        /// Finds the most similar pose to the provided pose.
        /// If the HandGrabPose contains a surface it will defer the calculation to it.
        /// </summary>
        /// <param name="worldPoint">The desired pose in world space</param>
        /// <param name="scoringModifier">How much to weight the translational or rotational distance</returns>
        /// <param name="relativeTo">Reference transform used to measure the local parameters</param>
        /// <param name="bestWorldPose">Best pose available that is near the desired one</param>
        /// <returns>The score from the desired worldPoint to the result BestWorldPose</returns>
        private GrabPoseScore CompareNearPoses(in Pose worldPoint,
            PoseMeasureParameters scoringModifier, Transform relativeTo,
            out Pose bestWorldPose)
        {
            GrabPoseScore bestScore;
            if (SnapSurface != null)
            {
                bestScore = SnapSurface.CalculateBestPoseAtSurface(worldPoint, out bestWorldPose, scoringModifier, relativeTo);
            }
            else
            {
                bestWorldPose = PoseUtils.GlobalPoseScaled(relativeTo, this.RelativePose);
                bestScore = new GrabPoseScore(worldPoint, bestWorldPose, scoringModifier.PositionRotationWeight);
            }

            return bestScore;
        }

        #region Inject
        public void InjectAllHandGrabPose(Transform relativeTo)
        {
            InjectRelativeTo(relativeTo);
        }

        public void InjectRelativeTo(Transform relativeTo)
        {
            _relativeTo = relativeTo;
        }

        public void InjectOptionalSurface(IGrabSurface surface)
        {
            _surface = surface as UnityEngine.Object;
            SnapSurface = surface;
        }

        public void InjectOptionalHandPose(HandPose handPose)
        {
            _handPose = handPose;
            _usesHandPose = _handPose != null;
        }

        #endregion

    }
}
