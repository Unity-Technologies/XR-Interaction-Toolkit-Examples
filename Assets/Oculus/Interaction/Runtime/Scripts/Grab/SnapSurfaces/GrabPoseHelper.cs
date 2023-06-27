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
    public static class GrabPoseHelper
    {
        public delegate Pose PoseCalculator(in Pose desiredPose, Transform relativeTo);

        /// <summary>
        /// Finds the best pose comparing the one that requires the minimum rotation
        /// and minimum translation.
        /// </summary>
        /// <param name="desiredPose">Pose to measure from.</param>
        /// <param name="bestPose">Nearest pose to the desired one at the hand grab pose.</param>
        /// <param name="scoringModifier">Modifiers for the score based in rotation and distance.</param>
        /// <param name="relativeTo">The reference transform to apply the calculators to</param>
        /// <param name="minimalTranslationPoseCalculator">Delegate to calculate the nearest, by position, pose at a hand grab pose.</param>
        /// <param name="minimalRotationPoseCalculator">Delegate to calculate the nearest, by rotation, pose at a hand grab pose.</param>
        /// <returns>The score, normalized, of the best pose.</returns>
        public static GrabPoseScore CalculateBestPoseAtSurface(in Pose desiredPose, out Pose bestPose,
            in PoseMeasureParameters scoringModifier, Transform relativeTo,
            PoseCalculator minimalTranslationPoseCalculator, PoseCalculator minimalRotationPoseCalculator)
        {
            if (scoringModifier.PositionRotationWeight == 1f)
            {
                bestPose = minimalRotationPoseCalculator(desiredPose, relativeTo);
                return new GrabPoseScore(desiredPose, bestPose, 1f);
            }

            if (scoringModifier.PositionRotationWeight == 0f)
            {
                bestPose = minimalTranslationPoseCalculator(desiredPose, relativeTo);
                return new GrabPoseScore(desiredPose, bestPose, 0f);
            }

            Pose minimalTranslationPose = minimalTranslationPoseCalculator(desiredPose, relativeTo);
            Pose minimalRotationPose = minimalRotationPoseCalculator(desiredPose, relativeTo);
            bestPose = SelectBestPose(minimalRotationPose, minimalTranslationPose,
                desiredPose, scoringModifier, out GrabPoseScore bestScore);
            return bestScore;

        }

        /// <summary>
        /// Compares two poses to a reference and returns the most similar one
        /// </summary>
        /// <param name="poseA">First Pose to compare with the reference.</param>
        /// <param name="poseB">Second Pose to compare with the reference.</param>
        /// <param name="reference">Reference pose to measure from.</param>
        /// <param name="scoringModifier">Modifiers for the score based in rotation and distance.</param>
        /// <param name="bestScore">Out value with the score of the best pose.</param>
        /// <returns>The most similar pose to reference out of the poses</returns>
        public static Pose SelectBestPose(in Pose poseA, in Pose poseB, in Pose reference,
            PoseMeasureParameters scoringModifier, out GrabPoseScore bestScore)
        {
            GrabPoseScore poseAScore = new GrabPoseScore(reference, poseA,
                scoringModifier.PositionRotationWeight);
            GrabPoseScore poseBScore = new GrabPoseScore(reference, poseB,
                scoringModifier.PositionRotationWeight);

            if (poseAScore.IsBetterThan(poseBScore))
            {
                bestScore = poseAScore;
                return poseA;
            }
            else
            {
                bestScore = poseBScore;
                return poseB;
            }
        }

        /// <summary>
        /// Calculates the score from a point to a set of colliders.
        /// When the point is outside the colliders the further from their surface means the
        /// lower the score.
        /// When the point is inside any of the colliders the score is always higher.
        /// </summary>
        /// <param name="position">Position to measure against the colliders</param>
        /// <param name="colliders">Group of colliders to measure the score</param>
        /// <param name="hitPoint">Output point in the surface or inside the colliders that is near the position</param>
        /// <returns>A GrabPoseScore value containing the score of the position in reference to the colliders</returns>
        public static GrabPoseScore CollidersScore(Vector3 position, Collider[] colliders,
            out Vector3 hitPoint)
        {
            GrabPoseScore bestScore = GrabPoseScore.Max;
            GrabPoseScore score;
            hitPoint = position;
            foreach (Collider collider in colliders)
            {
                bool isPointInsideCollider = Collisions.IsPointWithinCollider(position, collider);
                Vector3 measuringPoint = isPointInsideCollider ? collider.bounds.center
                    : collider.ClosestPoint(position);

                score = new GrabPoseScore(position, measuringPoint,
                    isPointInsideCollider);

                if (score.IsBetterThan(bestScore))
                {
                    hitPoint = isPointInsideCollider ? position : measuringPoint;
                    bestScore = score;
                }
            }

            return bestScore;
        }
    }
}
