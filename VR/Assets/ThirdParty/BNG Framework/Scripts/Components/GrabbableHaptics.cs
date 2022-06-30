using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BNG {

    /// <summary>
    /// A set of events that allow you to apply haptics to a controller
    /// </summary>
    public class GrabbableHaptics : GrabbableEvents {

        public bool HapticsOnValidPickup = true;
        public bool HapticsOnValidRemotePickup = true;
        public bool HapticsOnCollision = true;
        public bool HapticsOnGrab = true;

        public float VibrateFrequency = 0.3f;
        public float VibrateAmplitude = 0.1f;
        public float VibrateDuration = 0.1f;

        Grabber currentGrabber;

        public override void OnGrab(Grabber grabber) {
            // Store grabber so we can use it if we need to vibrate the controller
            currentGrabber = grabber;

            if(HapticsOnGrab) {
                doHaptics(grabber.HandSide);
            }
        }

        public override void OnRelease() {
            currentGrabber = null;
        }

        // Fires if this is the closest grabbable but wasn't in the previous frame
        public override void OnBecomesClosestGrabbable(ControllerHand touchingHand) {
            
            if (HapticsOnValidPickup) {                
                doHaptics(touchingHand);
            }
        }

        public override void OnBecomesClosestRemoteGrabbable(ControllerHand touchingHand) {
            if (HapticsOnValidRemotePickup) {
                doHaptics(touchingHand);
            }
        }

        void doHaptics(ControllerHand touchingHand) {
            if(input) {
                input.VibrateController(VibrateFrequency, VibrateAmplitude, VibrateDuration, touchingHand);
            }
        }

        private void OnCollisionEnter(Collision collision) {
            // Play Haptic on collision
            if (HapticsOnCollision && currentGrabber != null && input != null) {
                // Only play collision haptics if being held
                if(grab != null && grab.BeingHeld) {
                    input.VibrateController(0.1f, 0.1f, 0.1f, currentGrabber.HandSide);
                }
            }
        }
    }
}

