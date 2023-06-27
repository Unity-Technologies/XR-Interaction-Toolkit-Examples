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
using Oculus.Interaction.Input;
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction.HandGrab
{
    /// <summary>
    /// Utility class used by the grab interactors to find the best matching
    /// pose from a provided list of HandGrabPoses in an object.
    /// </summary>
    public class GrabPoseFinder
    {
        public enum FindResult
        {
            NotFound,
            NotCompatible,
            Found,
        }

        /// <summary>
        /// List of HandGrabPoses that can move the provided _relativeTo Transform
        /// </summary>
        private List<HandGrabPose> _handGrabPoses;
        /// <summary>
        /// Reference transform used to convert the local HandGrabPoses data into
        /// worldspace.
        /// </summary>
        private Transform _relativeTo;
        private InterpolationCache _interpolationCache = new InterpolationCache();

        public bool UsesHandPose
        {
            get
            {
                return _handGrabPoses.Count > 0 && _handGrabPoses[0].HandPose != null;
            }
        }

        public GrabPoseFinder(List<HandGrabPose> handGrabPoses, Transform relativeTo)
        {
            _handGrabPoses = handGrabPoses;
            _relativeTo = relativeTo;
        }

        public bool SupportsHandedness(Handedness handedness)
        {
            if (!UsesHandPose)
            {
                return true;
            }

            return _handGrabPoses[0].HandPose.Handedness == handedness;
        }

        /// <summary>
        /// Finds the best valid hand-pose at this HandGrabInteractable.
        /// Remember that a HandGrabPoses can actually have a whole surface the user can snap to.
        /// </summary>
        /// <param name="userPose">Pose to compare to the snap point in world coordinates.</param>
        /// <param name="handScale">The scale of the tracked hand.</param>
        /// <param name="handedness">The handedness of the tracked hand.</param>
        /// <param name="scoringModifier">Parameters indicating how to score the different poses.</param>
        /// <param name="result">The resultant best pose found.</param>
        /// <returns>True if a good pose was found</returns>
        public FindResult FindBestPose(Pose userPose, float handScale, Handedness handedness,
            PoseMeasureParameters scoringModifier,
            ref HandGrabResult result)
        {
            if (_handGrabPoses.Count == 1)
            {
                if (_handGrabPoses[0].CalculateBestPose(userPose,
                    handedness, scoringModifier, _relativeTo, ref result))
                {
                    return FindResult.Found;
                }
                return FindResult.NotCompatible;
            }
            else if (_handGrabPoses.Count > 1)
            {
                if (CalculateBestScaleInterpolatedPose(userPose,
                    handedness, handScale, scoringModifier, ref result))
                {
                    return FindResult.Found;
                }
                return FindResult.NotCompatible;
            }
            return FindResult.NotFound;
        }

        private bool CalculateBestScaleInterpolatedPose(Pose userPose, Handedness handedness,
            float handScale, PoseMeasureParameters scoringModifier, ref HandGrabResult result)
        {
            result.HasHandPose = false;

            float scale = handScale / _relativeTo.lossyScale.x;
            bool rangeFound = FindInterpolationRange(scale, _handGrabPoses,
                out HandGrabPose under, out HandGrabPose over, out float t);

            if (!rangeFound)
            {
                return false;
            }

            bool underFound;
            bool overFound;

            if (t < 0)
            {
                underFound = under.CalculateBestPose(userPose, handedness,
                    scoringModifier, _relativeTo, ref _interpolationCache.underResult);
                Pose adjustedPose = _relativeTo.GlobalPose(_interpolationCache.underResult.RelativePose);
                overFound = over.CalculateBestPose(adjustedPose, handedness,
                    PoseMeasureParameters.DEFAULT, _relativeTo, ref _interpolationCache.overResult);
            }
            else if (t > 1)
            {
                overFound = over.CalculateBestPose(userPose, handedness,
                    scoringModifier, _relativeTo, ref _interpolationCache.overResult);
                Pose adjustedPose = _relativeTo.GlobalPose(_interpolationCache.overResult.RelativePose);
                underFound = under.CalculateBestPose(adjustedPose, handedness,
                    PoseMeasureParameters.DEFAULT, _relativeTo, ref _interpolationCache.underResult);
            }
            else
            {
                underFound = under.CalculateBestPose(userPose, handedness,
                    scoringModifier, _relativeTo, ref _interpolationCache.underResult);
                overFound = over.CalculateBestPose(userPose, handedness,
                    scoringModifier, _relativeTo, ref _interpolationCache.overResult);
            }

            if (_interpolationCache.underResult.HasHandPose
                && _interpolationCache.overResult.HasHandPose)
            {
                result.HasHandPose = true;
                result.HandPose.CopyFrom(_interpolationCache.underResult.HandPose);
                HandPose.Lerp(_interpolationCache.underResult.HandPose,
                    _interpolationCache.overResult.HandPose, t, ref result.HandPose);
                PoseUtils.Lerp(_interpolationCache.underResult.RelativePose,
                    _interpolationCache.overResult.RelativePose, t, ref result.RelativePose);
            }
            else if (_interpolationCache.underResult.HasHandPose)
            {
                result.HasHandPose = true;
                result.HandPose.CopyFrom(_interpolationCache.underResult.HandPose);
                result.RelativePose.CopyFrom(_interpolationCache.underResult.RelativePose);
            }
            else if (_interpolationCache.overResult.HasHandPose)
            {
                result.HasHandPose = true;
                result.HandPose.CopyFrom(_interpolationCache.overResult.HandPose);
                result.RelativePose.CopyFrom(_interpolationCache.overResult.RelativePose);
            }

            if (underFound && overFound)
            {
                result.Score = GrabPoseScore.Lerp(
                    _interpolationCache.underResult.Score,
                    _interpolationCache.overResult.Score, t);
                return true;
            }

            if (underFound)
            {
                result.Score = _interpolationCache.underResult.Score;
                return true;
            }

            if (overFound)
            {
                result.Score = _interpolationCache.overResult.Score;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Finds the two nearest HandGrabPose to interpolate from given a scale.
        /// The result can require an unclamped interpolation (t can be bigger than 1 or smaller than 0).
        /// </summary>
        /// <param name="relativeHandScale">The user scale relative to the target</param>
        /// <param name="grabPoses">The list of grab poses to interpolate from</param>
        /// <param name="from">The HandGrabInteractable with nearest scale recorder that is smaller than the provided one</param>
        /// <param name="to">The HandGrabInteractable with nearest scale recorder that is bigger than the provided one</param>
        /// <param name="t">The progress between from and to variables at which the desired scale resides</param>
        /// <returns>The HandGrabPose near under and over the scale, and the interpolation factor between them.</returns>
        public static bool FindInterpolationRange(float relativeHandScale,
            List<HandGrabPose> grabPoses, out HandGrabPose from, out HandGrabPose to, out float t)
        {
            if (grabPoses.Count == 0)
            {
                from = to = null;
                t = 0;
                return false;
            }
            if (grabPoses.Count == 1)
            {
                t = 0;
                from = to = grabPoses[0];
                return true;
            }

            from = FindPreviousScaledGrabPose(grabPoses, relativeHandScale);
            to = FindNextScaledGrabPose(grabPoses, relativeHandScale);

            if (from == null && to == null)
            {
                t = 0;
                return false;
            }

            if (to == null)
            {
                to = from;
                from = FindPreviousScaledGrabPose(grabPoses, from.RelativeScale, notEqual: true);
            }

            if (from == null)
            {
                from = to;
                to = FindNextScaledGrabPose(grabPoses, to.RelativeScale, notEqual: true);
            }
            float denom = to.RelativeScale - from.RelativeScale;
            if (denom == 0f)
            {
                t = 0f;
            }
            else
            {
                t = (relativeHandScale - from.RelativeScale) / denom;
            }
            return true;
        }

        private static HandGrabPose FindPreviousScaledGrabPose(List<HandGrabPose> grabPoses,
            float upLimit, bool notEqual = false)
        {
            float lowLimit = float.NegativeInfinity;
            HandGrabPose foundGrabPose = null;
            foreach (HandGrabPose grabPose in grabPoses)
            {
                float correctedScale = grabPose.RelativeScale;
                if (((!notEqual && correctedScale <= upLimit)
                    || (notEqual && correctedScale < upLimit))
                    && correctedScale > lowLimit)
                {
                    lowLimit = correctedScale;
                    foundGrabPose = grabPose;
                }
            }
            return foundGrabPose;
        }

        private static HandGrabPose FindNextScaledGrabPose(List<HandGrabPose> grabPoses,
            float lowLimit, bool notEqual = false)
        {
            float upLimit = float.PositiveInfinity;
            HandGrabPose foundGrabPose = null;
            foreach (HandGrabPose grabPose in grabPoses)
            {
                float correctedScale = grabPose.RelativeScale;
                if (((!notEqual && correctedScale >= lowLimit)
                    || (notEqual && correctedScale > lowLimit))
                    && correctedScale < upLimit)
                {
                    upLimit = correctedScale;
                    foundGrabPose = grabPose;
                }
            }
            return foundGrabPose;
        }

        private class InterpolationCache
        {
            public HandGrabResult underResult = new HandGrabResult();
            public HandGrabResult overResult = new HandGrabResult();
        }
    }
}
