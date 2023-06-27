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
using UnityEngine;
using Oculus.Interaction.GrabAPI;
using Oculus.Interaction.Input;

namespace Oculus.Interaction.HandGrab
{
    /// <summary>
    /// Helper class for Hand Grabbing objects.
    /// This class keeps track of the grabbing anchors and updates the target
    /// and movement during a Hand Grab interaction.
    /// </summary>
    public static class HandGrabInteraction
    {
        /// <summary>
        /// Calculates a new target. That is the point of the
        /// interactable at which the HandGrab will happen.
        /// </summary>
        /// <param name="interactable">The interactable to be HandGrabbed</param>
        /// <param name="grabTypes">The supported GrabTypes</param>
        /// <param name="anchorMode">The anchor to use for grabbing</param>
        /// <param name="handGrabResult">The a variable to store the result</param>
        /// <returns>True if a valid pose was found</returns>
        public static bool TryCalculateBestGrab(this IHandGrabInteractor handGrabInteractor,
            IHandGrabInteractable interactable, GrabTypeFlags grabTypes,
            out HandGrabTarget.GrabAnchor anchorMode, ref HandGrabResult handGrabResult)
        {
            grabTypes = grabTypes & interactable.SupportedGrabTypes;
            anchorMode = HandGrabInteractionPoses.GetBestAvailableAnchor(handGrabInteractor, grabTypes);
            Pose handPose = HandGrabInteractionPoses.GetHandAlignmentPoint(handGrabInteractor, interactable.UsesHandPose, anchorMode);

            if (interactable.CalculateBestPose(handPose,
                handGrabInteractor.Hand.Scale, handGrabInteractor.Hand.Handedness, ref handGrabResult))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the current GrabAnchor as a GrabTypeFlag
        /// </summary>
        /// <param name="handGrabInteractor">The interactor whose anchor tor inspect</param>
        /// <returns>A GrabTypeFlags matching the actual Anchor</returns>
        public static GrabTypeFlags CurrentGrabType(this IHandGrabInteractor handGrabInteractor)
        {
            switch (handGrabInteractor.HandGrabTarget.Anchor)
            {
                case HandGrabTarget.GrabAnchor.Pinch: return GrabTypeFlags.Pinch;
                case HandGrabTarget.GrabAnchor.Palm: return GrabTypeFlags.Palm;
                case HandGrabTarget.GrabAnchor.Wrist: return GrabTypeFlags.All;
                default: return GrabTypeFlags.None;
            }
        }

        /// <summary>
        /// Initiates the movement of the Interactable
        /// with the current HandGrabTarget
        /// </summary>
        /// <param name="interactable">The interactable to be HandGrabbed</param>
        public static IMovement GenerateMovement(this IHandGrabInteractor handGrabInteractor, IHandGrabInteractable interactable)
        {
            Pose originPose = handGrabInteractor.GetTargetGrabPose();
            Pose grabPose = handGrabInteractor.GetHandGrabPose();
            return interactable.GenerateMovement(originPose, grabPose);
        }

        /// <summary>
        /// Gets the current pose of the current grabbing point.
        /// </summary>
        /// <returns>The pose in world coordinates</returns>
        public static Pose GetHandGrabPose(this IHandGrabInteractor handGrabInteractor)
        {
            Pose wristPose = HandGrabInteractionPoses.GetPose(handGrabInteractor, HandGrabTarget.GrabAnchor.Wrist);
            return PoseUtils.Multiply(wristPose, handGrabInteractor.WristToGrabPoseOffset);
        }

        /// <summary>
        /// Calculates the GrabPoseScore for an interactable with the
        /// given grab modes.
        /// </summary>
        /// <param name="interactable">The interactable to measure to</param>
        /// <param name="grabTypes">The supported grab types for the grab</param>
        /// <param name="result">Calculating the score requires finding the best grab pose. It is stored here.</param>
        /// <returns>The best GrabPoseScore considering the grabtypes at the interactable</returns>
        public static GrabPoseScore GetPoseScore(this IHandGrabInteractor handGrabInteractor, IHandGrabInteractable interactable,
            GrabTypeFlags grabTypes, ref HandGrabResult result)
        {
            grabTypes = grabTypes & interactable.SupportedGrabTypes;
            HandGrabTarget.GrabAnchor anchorMode = HandGrabInteractionPoses.GetBestAvailableAnchor(handGrabInteractor, grabTypes);
            Pose handPose = HandGrabInteractionPoses.GetHandAlignmentPoint(handGrabInteractor, interactable.UsesHandPose, anchorMode);
            if (interactable.CalculateBestPose(handPose,
                handGrabInteractor.Hand.Scale, handGrabInteractor.Hand.Handedness, ref result))
            {
                return result.Score;
            }
            return GrabPoseScore.Max;
        }

        /// <summary>
        /// Indicates if an interactor can interact with (hover and select) and interactable.
        /// This depends on the handedness and the valid grab types of both elements
        /// </summary>
        /// <param name="handGrabInteractor">The interactor grabbing</param>
        /// <param name="handGrabInteractable">The interactable to be grabbed</param>
        /// <returns>True if the interactor could grab the interactable</returns>
        public static bool CanInteractWith(this IHandGrabInteractor handGrabInteractor, IHandGrabInteractable handGrabInteractable)
        {
            if (!handGrabInteractable.SupportsHandedness(handGrabInteractor.Hand.Handedness))
            {
                return false;
            }

            GrabTypeFlags handGrabTypes = GrabTypeFlags.None;
            if (SupportsPinch(handGrabInteractor, handGrabInteractable))
            {
                handGrabTypes |= GrabTypeFlags.Pinch;
            }
            if (SupportsPalm(handGrabInteractor, handGrabInteractable))
            {
                handGrabTypes |= GrabTypeFlags.Palm;
            }
            return handGrabTypes != GrabTypeFlags.None;
        }

        /// <summary>
        /// Calculates the offset from the Wrist to the actual grabbing point
        /// defined by the current anchor in the interactor HandGrabTarget.
        /// </summary>
        /// <param name="handGrabInteractor">The interactor whose HandGrabTarget to inspect</param>
        /// <returns>The local offset from the wrist to the grab point</returns>
        public static Pose GetGrabOffset(this IHandGrabInteractor handGrabInteractor)
        {
            Pose grabPose = HandGrabInteractionPoses.GetPose(handGrabInteractor, handGrabInteractor.HandGrabTarget.Anchor);
            Pose wristPose = HandGrabInteractionPoses.GetPose(handGrabInteractor, HandGrabTarget.GrabAnchor.Wrist);
            Pose wristToGrabPoseOffset = PoseUtils.Delta(wristPose, grabPose);
            return wristToGrabPoseOffset;

        }

        /// <summary>
        /// Calculates the strenght of the fingers of an interactor trying (or grabbing) an interactable
        /// </summary>
        /// <param name="handGrabInteractor">The interactor grabbing</param>
        /// <param name="handGrabInteractable">The interactable being grabbed</param>
        /// <param name="handGrabTypes">A filter for the grab types to calculate</param>
        /// <returns>The maximum strength for the grabbing fingers, normalized</returns>
        public static float ComputeHandGrabScore(IHandGrabInteractor handGrabInteractor,
            IHandGrabInteractable handGrabInteractable, out GrabTypeFlags handGrabTypes)
        {
            HandGrabAPI api = handGrabInteractor.HandGrabApi;
            handGrabTypes = GrabTypeFlags.None;
            float handGrabScore = 0f;

            if (SupportsPinch(handGrabInteractor, handGrabInteractable))
            {
                float pinchStrength = api.GetHandPinchScore(handGrabInteractable.PinchGrabRules, false);
                if (pinchStrength > handGrabScore)
                {
                    handGrabScore = pinchStrength;
                    handGrabTypes = GrabTypeFlags.Pinch;
                }
            }

            if (SupportsPalm(handGrabInteractor, handGrabInteractable))
            {
                float palmStrength = api.GetHandPalmScore(handGrabInteractable.PalmGrabRules, false);
                if (palmStrength > handGrabScore)
                {
                    handGrabScore = palmStrength;
                    handGrabTypes = GrabTypeFlags.Palm;
                }
            }

            return handGrabScore;
        }

        public static bool ComputeShouldSelect(this IHandGrabInteractor handGrabInteractor,
            IHandGrabInteractable handGrabInteractable, out GrabTypeFlags selectingGrabTypes)
        {
            if (handGrabInteractable == null)
            {
                selectingGrabTypes = GrabTypeFlags.None;
                return false;
            }

            HandGrabAPI api = handGrabInteractor.HandGrabApi;
            selectingGrabTypes = GrabTypeFlags.None;
            if (SupportsPinch(handGrabInteractor, handGrabInteractable) &&
                 api.IsHandSelectPinchFingersChanged(handGrabInteractable.PinchGrabRules))
            {
                selectingGrabTypes |= GrabTypeFlags.Pinch;
            }

            if (SupportsPalm(handGrabInteractor, handGrabInteractable) &&
                 api.IsHandSelectPalmFingersChanged(handGrabInteractable.PalmGrabRules))
            {
                selectingGrabTypes |= GrabTypeFlags.Palm;
            }

            return selectingGrabTypes != GrabTypeFlags.None;
        }

        public static bool ComputeShouldUnselect(this IHandGrabInteractor handGrabInteractor,
            IHandGrabInteractable handGrabInteractable)
        {
            HandGrabAPI api = handGrabInteractor.HandGrabApi;
            HandFingerFlags pinchFingers = api.HandPinchGrabbingFingers();
            HandFingerFlags palmFingers = api.HandPalmGrabbingFingers();

            if (handGrabInteractable.SupportedGrabTypes == GrabTypeFlags.None)
            {
                if (!api.IsSustainingGrab(GrabbingRule.FullGrab, pinchFingers) &&
                    !api.IsSustainingGrab(GrabbingRule.FullGrab, palmFingers))
                {
                    return true;
                }
                return false;
            }

            bool pinchHolding = false;
            bool palmHolding = false;
            bool pinchReleased = false;
            bool palmReleased = false;

            if (SupportsPinch(handGrabInteractor, handGrabInteractable.SupportedGrabTypes))
            {
                pinchHolding = api.IsSustainingGrab(handGrabInteractable.PinchGrabRules, pinchFingers);
                if (api.IsHandUnselectPinchFingersChanged(handGrabInteractable.PinchGrabRules))
                {
                    pinchReleased = true;
                }
            }

            if (SupportsPalm(handGrabInteractor, handGrabInteractable.SupportedGrabTypes))
            {
                palmHolding = api.IsSustainingGrab(handGrabInteractable.PalmGrabRules, palmFingers);
                if (api.IsHandUnselectPalmFingersChanged(handGrabInteractable.PalmGrabRules))
                {
                    palmReleased = true;
                }
            }

            return !pinchHolding && !palmHolding && (pinchReleased || palmReleased);
        }

        public static HandFingerFlags GrabbingFingers(this IHandGrabInteractor handGrabInteractor,
            IHandGrabInteractable handGrabInteractable)
        {
            HandGrabAPI api = handGrabInteractor.HandGrabApi;
            if (handGrabInteractable == null)
            {
                return HandFingerFlags.None;
            }

            HandFingerFlags fingers = HandFingerFlags.None;

            if (SupportsPinch(handGrabInteractor, handGrabInteractable))
            {
                HandFingerFlags pinchingFingers = api.HandPinchGrabbingFingers();
                handGrabInteractable.PinchGrabRules.StripIrrelevant(ref pinchingFingers);
                fingers = fingers | pinchingFingers;
            }

            if (SupportsPalm(handGrabInteractor, handGrabInteractable))
            {
                HandFingerFlags grabbingFingers = api.HandPalmGrabbingFingers();
                handGrabInteractable.PalmGrabRules.StripIrrelevant(ref grabbingFingers);
                fingers = fingers | grabbingFingers;
            }

            return fingers;
        }

        private static bool SupportsPinch(IHandGrabInteractor handGrabInteractor,
            IHandGrabInteractable handGrabInteractable)
        {
            return SupportsPinch(handGrabInteractor, handGrabInteractable.SupportedGrabTypes);
        }

        private static bool SupportsPalm(IHandGrabInteractor handGrabInteractor,
            IHandGrabInteractable handGrabInteractable)
        {
            return SupportsPalm(handGrabInteractor, handGrabInteractable.SupportedGrabTypes);
        }

        private static bool SupportsPinch(IHandGrabInteractor handGrabInteractor,
            GrabTypeFlags grabTypes)
        {
            return (handGrabInteractor.SupportedGrabTypes & GrabTypeFlags.Pinch) != 0 &&
                (grabTypes & GrabTypeFlags.Pinch) != 0;
        }

        private static bool SupportsPalm(IHandGrabInteractor handGrabInteractor,
            GrabTypeFlags grabTypes)
        {
            return (handGrabInteractor.SupportedGrabTypes & GrabTypeFlags.Palm) != 0 &&
                (grabTypes & GrabTypeFlags.Palm) != 0;
        }

        private class HandGrabInteractionPoses
        {
            public static HandGrabTarget.GrabAnchor GetBestAvailableAnchor(IHandGrabInteractor handGrabInteractor,
                GrabTypeFlags grabTypes)
            {
                if (handGrabInteractor.PalmPoint != null
                    && (grabTypes & GrabTypeFlags.Palm) != 0)
                {
                    return HandGrabTarget.GrabAnchor.Palm;
                }
                else if (handGrabInteractor.PinchPoint != null
                    && (grabTypes & GrabTypeFlags.Pinch) != 0)
                {
                    return HandGrabTarget.GrabAnchor.Pinch;
                }
                else
                {
                    return HandGrabTarget.GrabAnchor.Wrist;
                }
            }

            public static Pose GetHandAlignmentPoint(IHandGrabInteractor handGrabInteractor,
                bool forceWrist, HandGrabTarget.GrabAnchor anchorMode)
            {
                return GetPose(handGrabInteractor, forceWrist ? HandGrabTarget.GrabAnchor.Wrist : anchorMode);
            }

            public static Pose GetPose(IHandGrabInteractor handGrabInteractor,
                HandGrabTarget.GrabAnchor anchorMode)
            {
                if (anchorMode == HandGrabTarget.GrabAnchor.Pinch && handGrabInteractor.PinchPoint != null)
                {
                    return handGrabInteractor.PinchPoint.GetPose();
                }
                else if (anchorMode == HandGrabTarget.GrabAnchor.Palm && handGrabInteractor.PalmPoint != null)
                {
                    return handGrabInteractor.PalmPoint.GetPose();
                }

                return handGrabInteractor.WristPoint.GetPose();
            }
        }
    }
}
