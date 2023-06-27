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

namespace Oculus.Interaction.Input
{
    public static class OVRControllerUtility
    {
        public static float GetPinchAmount(OVRInput.Controller ovrController)
        {
            float pinchAmount = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, ovrController);
            return pinchAmount;
        }

        public static float GetIndexCurl(OVRInput.Controller ovrController)
        {
            float indexCurl;
            if (SupportsAnalogIndex(ovrController))
            {
                indexCurl = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTriggerCurl, ovrController);
            }
            else
            {
                // Fallback to binary capsense
                bool isPointing = !OVRInput.Get(OVRInput.NearTouch.PrimaryIndexTrigger, ovrController)
                    && OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, ovrController) == 0f;

                indexCurl = isPointing ? 0 : 1;
            }
            return indexCurl;
        }

        public static float GetIndexSlide(OVRInput.Controller ovrController)
        {
            float indexSlide;
            if (SupportsAnalogIndex(ovrController))
            {
                indexSlide = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTriggerSlide, ovrController);
            }
            else
            {
                indexSlide = 0;
            }
            return indexSlide;
        }

        private static bool SupportsAnalogIndex(OVRInput.Controller ovrController)
        {
            bool isTouchController = (ovrController == OVRInput.Controller.LTouch) || (ovrController == OVRInput.Controller.RTouch);
            if (!isTouchController)
            {
                return false;
            }

            OVRInput.Hand ovrHandedness = (ovrController == OVRInput.Controller.LTouch) ? OVRInput.Hand.HandLeft : OVRInput.Hand.HandRight;
            OVRInput.InteractionProfile ovrProfile = OVRInput.GetCurrentInteractionProfile(ovrHandedness);
            return ovrProfile == OVRInput.InteractionProfile.TouchPro;
        }
    }
}
