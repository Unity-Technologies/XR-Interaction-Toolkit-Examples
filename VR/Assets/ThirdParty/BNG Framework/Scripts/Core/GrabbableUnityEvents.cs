using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace BNG {
    public class GrabbableUnityEvents : GrabbableEvents {

        public GrabberEvent onGrab;
        public UnityEvent onRelease;
        public UnityEvent onBecomesClosestGrabbable;
        public UnityEvent onNoLongerClosestGrabbable;
        public UnityEvent onBecomesClosestRemoteGrabbable;
        public UnityEvent onNoLongerClosestRemoteGrabbable;
        public FloatEvent onGrip;
        public FloatEvent onTrigger;
        public UnityEvent onTriggerDown;
        public UnityEvent onTriggerUp;
        public UnityEvent onButton1;
        public UnityEvent onButton1Down;
        public UnityEvent onButton1Up;
        public UnityEvent onButton2;
        public UnityEvent onButton2Down;
        public UnityEvent onButton2Up;
        public UnityEvent onSnapZoneEnter;
        public UnityEvent onSnapZoneExit;

        /// <summary>
        /// Item has been grabbed by a Grabber
        /// </summary>
        /// <param name="grabber"></param>
        public override void OnGrab(Grabber grabber) {
            base.OnGrab(grabber);

            if(onGrab != null) {
                onGrab.Invoke(grabber);
            }
        }

        /// <summary>
        /// Has been dropped from the Grabber
        /// </summary>
        public override void OnRelease() {
            base.OnRelease();

            if (onRelease != null) {
                onRelease.Invoke();
            }
        }

        /// <summary>
        /// Called if this is the closest grabbable but wasn't in the previous frame 
        /// </summary>
        /// <param name="touchingHand"></param>
        public override void OnBecomesClosestGrabbable(ControllerHand touchingHand) {
            base.OnBecomesClosestGrabbable(touchingHand);

            if (onBecomesClosestGrabbable != null) {
                onBecomesClosestGrabbable.Invoke();
            }
        }

        /// <summary>
        /// No longer closest grabbable. May need to disable highlight, ring, etc.
        /// </summary>
        /// <param name="touchingHand"></param>
        public override void OnNoLongerClosestGrabbable(ControllerHand touchingHand) {
            base.OnNoLongerClosestGrabbable(touchingHand);

            if (onNoLongerClosestGrabbable != null) {
                onNoLongerClosestGrabbable.Invoke();
            }
        }

        /// <summary>
        /// Fires if this is the closest remote grabbable but wasn't in the previous frame
        /// </summary>
        /// <param name="touchingHand"></param>
        public override void OnBecomesClosestRemoteGrabbable(ControllerHand touchingHand) {
            base.OnBecomesClosestRemoteGrabbable(touchingHand);

            if (onBecomesClosestRemoteGrabbable != null) {
                onBecomesClosestRemoteGrabbable.Invoke();
            }
        }

        /// <summary>
        /// Fires if this was the closest remote grabbable last frame, but not this frame
        /// </summary>
        /// <param name="touchingHand"></param>
        public override void OnNoLongerClosestRemoteGrabbable(ControllerHand touchingHand) {
            base.OnNoLongerClosestRemoteGrabbable(touchingHand);

            if (onNoLongerClosestRemoteGrabbable != null) {
                onNoLongerClosestRemoteGrabbable.Invoke();
            }
        }

        /// <summary>
        /// Amount of Grip (0-1). Only fired if object is being held.
        /// </summary>
        /// <param name="gripValue">0 - 1 Open / Closed</param>
        public override void OnGrip(float gripValue) {
            base.OnGrip(gripValue);

            if(onGrip != null) {
                onGrip.Invoke(gripValue);
            }
        }

        /// <summary>
        /// Amount of Trigger being held down on the grabbed items controller. Only fired if object is being held.
        /// </summary>
        /// <param name="triggerValue">0 - 1 Open / Closed</param>
        public override void OnTrigger(float triggerValue) {
            base.OnTrigger(triggerValue);

            if (onTrigger != null) {
                onTrigger.Invoke(triggerValue);
            }
        }

        /// <summary>
        /// Fires if trigger was pressed down on this controller this frame. Only fired if object is being held.
        /// </summary>
        public override void OnTriggerDown() {
            base.OnTriggerDown();

            if (onTriggerDown != null) {
                onTriggerDown.Invoke();
            }
        }

        /// <summary>
        /// Fires if trigger was released on this controller this frame. Only fired if object is being held.
        /// </summary>
        public override void OnTriggerUp() {
            base.OnTriggerUp();

            if (onTriggerUp != null) {
                onTriggerUp.Invoke();
            }
        }

        /// <summary>
        /// Button 1 is being held down this frame but not last
        /// Oculus : Button 1 = "A" if held in Right controller."X" if held in Left Controller
        /// </summary>
        public override void OnButton1() {
            base.OnButton1();

            if (onButton1 != null) {
                onButton1.Invoke();
            }
        }

        /// <summary>
        /// Button 1 Pressed down this frame
        /// Oculus : Button 1 = "A" if held in Right controller."X" if held in Left Controller
        /// </summary>
        public override void OnButton1Down() {
            base.OnButton1Down();

            if (onButton1Down != null) {
                onButton1Down.Invoke();
            }
        }

        /// <summary>
        /// Button 1 Released this frame
        /// Oculus : Button 1 = "A" if held in Right controller."X" if held in Left Controller
        /// </summary>
        public override void OnButton1Up() {
            base.OnButton1Up();

            if (onButton1Up != null) {
                onButton1Up.Invoke();
            }
        }

        /// <summary>
        /// Button 2 is being held down this frame but not last
        /// Oculus : Button 2 = "B" if held in Right controller."Y" if held in Left Controller
        /// </summary>
        public override void OnButton2() {
            base.OnButton2();

            if (onButton2 != null) {
                onButton2.Invoke();
            }
        }

        /// <summary>
        /// Button 2 Pressed down this frame
        /// Oculus : Button 2 = "B" if held in Right controller."Y" if held in Left Controller
        /// </summary>
        public override void OnButton2Down() {
            base.OnButton2Down();

            if (onButton2Down != null) {
                onButton2Down.Invoke();
            }
        }

        /// <summary>
        /// Button 2 Released this frame
        /// Oculus : Button 2 = "B" if held in Right controller."Y" if held in Left Controller
        /// </summary>
        public override void OnButton2Up() {
            base.OnButton2Up();

            if (onButton2Up != null) {
                onButton2Up.Invoke();
            }
        }

        /// <summary>
        /// Button 2 Released this frame
        /// Oculus : Button 2 = "B" if held in Right controller."Y" if held in Left Controller
        /// </summary>
        public override void OnSnapZoneEnter() {
            base.OnSnapZoneEnter();

            if (onSnapZoneEnter != null) {
                onSnapZoneEnter.Invoke();
            }
        }

        /// <summary>
        /// Button 2 Released this frame
        /// Oculus : Button 2 = "B" if held in Right controller."Y" if held in Left Controller
        /// </summary>
        public override void OnSnapZoneExit() {
            base.OnSnapZoneExit();

            if (onSnapZoneExit != null) {
                onSnapZoneExit.Invoke();
            }
        }
    }
}