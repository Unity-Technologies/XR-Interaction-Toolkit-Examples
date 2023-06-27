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

namespace Oculus.Interaction.Grab
{
    /// <summary>
    /// Stores the score of a grab.
    /// Score is measured on distance from the desired grab point to an ideal pose.
    /// Grab points might prefer to weight more the translational or the rotational distance,
    /// for that end 2 scalars and a weight are saved and then compared against other scores
    /// to decide which one is higher.
    /// </summary>
    public struct GrabPoseScore
    {
        private float _translationScore;
        private float _rotationScore;
        private float _rotationWeight;

        public static readonly GrabPoseScore Max = new GrabPoseScore(float.PositiveInfinity, float.PositiveInfinity, 0);

        /// <summary>
        /// Creates a GrabPoseScore with the given values.
        /// </summary>
        /// <param name="translationScore">The translational score</param>
        /// <param name="rotationScore">The rotational score</param>
        /// <param name="rotationWeight">The weight indicating preference
        /// between translational (0) or rotational (1) scores</param>
        public GrabPoseScore(float translationScore, float rotationScore, float rotationWeight)
        {
            _translationScore = translationScore;
            _rotationScore = rotationScore;
            _rotationWeight = rotationWeight;
        }

        /// <summary>
        /// Calculates and creates a purely positional score.
        /// This score has a rotational weight of 0.
        /// </summary>
        /// <param name="fromPoint">Desired point of measure</param>
        /// <param name="toPoint">Ideal point of measure</param>
        /// <param name="isInside">If true, the translation is stored as a negative scalar</param>
        public GrabPoseScore(Vector3 fromPoint, Vector3 toPoint, bool isInside = false)
        {
            _translationScore = PositionalScore(fromPoint, toPoint);
            _rotationScore = 0f;
            _rotationWeight = 0f;
            if (isInside)
            {
                _translationScore = -Mathf.Abs(_translationScore);
            }
        }

        /// <summary>
        /// Calculates and creates a score from a desired pose
        /// to a reference pose.
        /// </summary>
        /// <param name="poseA">The desired pose</param>
        /// <param name="poseB">The reference pose</param>
        /// <param name="rotationWeight">The weight indicating preference
        /// between translational (0) or rotational (1) scores</param>
        public GrabPoseScore(in Pose poseA, in Pose poseB, float rotationWeight)
        {
            _translationScore = PositionalScore(poseA.position, poseB.position);
            _rotationScore = RotationalScore(poseA.rotation, poseB.rotation);
            _rotationWeight = rotationWeight;
        }

        public bool IsValid()
        {
            return _translationScore != float.PositiveInfinity
                && _rotationScore != float.PositiveInfinity;
        }

        private float Score(float maxDistance)
        {
            return Mathf.Lerp(_translationScore, _rotationScore * maxDistance, _rotationWeight);
        }

        private static float PositionalScore(in Vector3 from, in Vector3 to)
        {
            return (from - to).sqrMagnitude;
        }

        /// <summary>
        /// Get how similar two rotations are.
        /// Since the Quaternion.Dot is bugged in unity. We compare the
        /// dot products of the forward and up vectors of the rotations.
        /// </summary>
        /// <param name="from">The first rotation.</param>
        /// <param name="to">The second rotation.</param>
        /// <returns>1 for opposite rotations, 0 for equal rotations.</returns>
        private static float RotationalScore(in Quaternion from, in Quaternion to)
        {
            float forwardDifference = Vector3.Dot(from * Vector3.forward, to * Vector3.forward) * 0.5f + 0.5f;
            float upDifference = Vector3.Dot(from * Vector3.up, to * Vector3.up) * 0.5f + 0.5f;
            return 1f - (forwardDifference * upDifference);
        }

        /// <summary>
        /// Linearly interpolate two scores by individually interpolating
        /// its components.
        /// </summary>
        /// <param name="from">Originb score</param>
        /// <param name="to">Target score</param>
        /// <param name="t">Interpolation factor</param>
        /// <returns></returns>
        public static GrabPoseScore Lerp(in GrabPoseScore from, in GrabPoseScore to, float t)
        {
            return new GrabPoseScore(
                Mathf.Lerp(from._translationScore, to._translationScore, t),
                Mathf.Lerp(from._rotationScore, to._rotationScore, t),
                Mathf.Lerp(from._rotationWeight, to._rotationWeight, t));
        }

        /// <summary>
        /// Calculates if the GrabPoseScore is better than other GrabPoseScore by
        /// comparing the Translational and Rotational scores based on their weights.
        /// </summary>
        /// <param name="referenceScore">The score to compare against</param>
        /// <returns>True of this score is better than the provided one, false otherwise</returns>
        public bool IsBetterThan(GrabPoseScore referenceScore)
        {
            if (_translationScore == float.PositiveInfinity)
            {
                return false;
            }
            if (referenceScore._translationScore == float.PositiveInfinity)
            {
                return true;
            }

            float maxTranslation = Mathf.Max(_translationScore, referenceScore._translationScore);
            float testScoreValue = Score(maxTranslation);
            float referenceScoreValue = referenceScore.Score(maxTranslation);

            return (testScoreValue < 0 && referenceScoreValue > 0)
                   || (testScoreValue < 0 && referenceScoreValue < 0 && testScoreValue > referenceScoreValue)
                   || (testScoreValue > 0 && referenceScoreValue > 0 && testScoreValue < referenceScoreValue);
        }
    }
}
