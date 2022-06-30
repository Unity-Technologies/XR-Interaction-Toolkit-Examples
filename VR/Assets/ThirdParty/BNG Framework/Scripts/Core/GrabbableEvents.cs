using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace BNG {

    /// <summary>
    /// Override this class to respond to various events that happen to this Grabbable
    /// </summary>
    [RequireComponent(typeof(Grabbable))]
    public abstract class GrabbableEvents : MonoBehaviour {

        protected Grabbable grab;
        protected Grabber thisGrabber;

        protected InputBridge input;

        protected virtual void Awake() {
            grab = GetComponent<Grabbable>();
            input = InputBridge.Instance;
        }

        /// <summary>
        /// Item has been grabbed by a Grabber
        /// </summary>
        /// <param name="grabber"></param>
        public virtual void OnGrab(Grabber grabber) {
            thisGrabber = grabber;
        }
        
        /// <summary>
        /// Has been dropped from the Grabber
        /// </summary>
        public virtual void OnRelease() {
           
        }

        /// <summary>
        /// Called if this is the closest grabbable but wasn't in the previous frame 
        /// </summary>
        /// <param name="touchingHand"></param>
        public virtual void OnBecomesClosestGrabbable(ControllerHand touchingHand) {
            
        }

        /// <summary>
        /// Called if this is the closest grabbable but wasn't in the previous frame 
        /// </summary>
        /// <param name="touchingGrabber"></param>
        public virtual void OnBecomesClosestGrabbable(Grabber touchingGrabber) {

        }

        /// <summary>
        /// No longer closest grabbable. May need to disable highlight, ring, etc.
        /// </summary>
        /// <param name="touchingHand"></param>
        public virtual void OnNoLongerClosestGrabbable(ControllerHand touchingHand) {
            
        }

        /// <summary>
        /// No longer closest grabbable. May need to disable highlight, ring, etc.
        /// </summary>
        /// <param name="touchingGrabber"></param>
        public virtual void OnNoLongerClosestGrabbable(Grabber touchingGrabber) {

        }

        /// <summary>
        /// Fires if this is the closest remote grabbable but wasn't in the previous frame
        /// </summary>
        /// <param name="touchingHand"></param>
        public virtual void OnBecomesClosestRemoteGrabbable(ControllerHand touchingHand) {
            
        }

        /// <summary>
        /// Fires if this is the closest remote grabbable but wasn't in the previous frame
        /// </summary>
        /// <param name="theGrabber">The Grabber that this object is valid for</param>
        public virtual void OnBecomesClosestRemoteGrabbable(Grabber theGrabber) {

        }

        /// <summary>
        /// Fires if this was the closest remote grabbable last frame, but not this frame
        /// </summary>
        /// <param name="touchingHand"></param>
        public virtual void OnNoLongerClosestRemoteGrabbable(ControllerHand touchingHand) {
            
        }

        /// <summary>
        /// Fires if this was the closest remote grabbable last frame, but not this frame
        /// </summary>
        /// <param name="theGrabber">The Grabber this object used to be associated with</param>
        public virtual void OnNoLongerClosestRemoteGrabbable(Grabber theGrabber) {

        }

        /// <summary>
        /// Amount of Grip (0-1). Only fired if object is being held.
        /// </summary>
        /// <param name="gripValue">0 - 1 Open / Closed</param>
        public virtual void OnGrip(float gripValue) {
            
        }

        /// <summary>
        /// Amount of Trigger being held down on the grabbed items controller. Only fired if object is being held.
        /// </summary>
        /// <param name="triggerValue">0 - 1 Open / Closed</param>
        public virtual void OnTrigger(float triggerValue) {
            
        }

        /// <summary>
        /// Fires if trigger was pressed down on this controller this frame, but was not pressed last frame. Only fired if object is being held.
        /// </summary>
        public virtual void OnTriggerDown() {
            
        }

        /// <summary>
        /// Fires if Trigger is not held down this frame
        /// </summary>
        public virtual void OnTriggerUp() {
           
        }

        /// <summary>
        /// Button 1 is being held down this frame but not last
        /// Oculus : Button 1 = "A" if held in Right controller."X" if held in Left Controller
        /// </summary>
        public virtual void OnButton1() {
            
        }

        /// <summary>
        /// Button 1 Pressed down this frame
        /// Oculus : Button 1 = "A" if held in Right controller."X" if held in Left Controller
        /// </summary>
        public virtual void OnButton1Down() {

        }

        /// <summary>
        /// Button 1 Released this frame
        /// Oculus : Button 1 = "A" if held in Right controller."X" if held in Left Controller
        /// </summary>
        public virtual void OnButton1Up() {
            
        }


        /// <summary>
        /// Button 2 is being held down this frame but not last
        /// Oculus : Button 2 = "B" if held in Right controller."Y" if held in Left Controller
        /// </summary>
        public virtual void OnButton2() {
            
        }

        /// <summary>
        /// Button 2 Pressed down this frame
        /// Oculus : Button 2 = "B" if held in Right controller."Y" if held in Left Controller
        /// </summary>
        public virtual void OnButton2Down() {
           
        }

        /// <summary>
        /// Button 2 Released this frame
        /// Oculus : Button 2 = "B" if held in Right controller."Y" if held in Left Controller
        /// </summary>
        public virtual void OnButton2Up() {

        }

        /// <summary>
        /// Grabbable has been successfully inserted into a SnapZone
        /// </summary>
        public virtual void OnSnapZoneEnter() {

        }

        /// <summary>
        /// Grabbable has been removed from a SnapZone
        /// </summary>
        public virtual void OnSnapZoneExit() {

        }
    }

    /// <summary>
    /// A UnityEvent with a float as a parameter
    /// </summary>
    [System.Serializable]
    public class FloatEvent : UnityEvent<float> { }

    /// <summary>
    /// A UnityEvent with a 2 floats as parameters
    /// </summary>
    [System.Serializable]
    public class FloatFloatEvent : UnityEvent<float, float> { }

    /// <summary>
    /// A UnityEvent with a Grabber as the parameter
    /// </summary>
    [System.Serializable]
    public class GrabberEvent : UnityEvent<Grabber> { }

    /// <summary>
    /// A UnityEvent with a Grabbable as the parameter
    /// </summary>
    [System.Serializable]
    public class GrabbableEvent : UnityEvent<Grabbable> { }

    /// <summary>
    /// A UnityEvent with a RaycastHit as the parameter
    /// </summary>
    [System.Serializable]
    public class RaycastHitEvent : UnityEvent<RaycastHit> { }

    /// <summary>
    /// A UnityEvent with a Vector2 as a parameter
    /// </summary>
    [System.Serializable]
    public class Vector2Event : UnityEvent<Vector2> { }

    /// <summary>
    /// A UnityEvent with a Vector3 as a parameter
    /// </summary>
    [System.Serializable]
    public class Vector3Event : UnityEvent<Vector3> { }

    /// <summary>
    /// A UnityEvent with a Vector3 as a parameter
    /// </summary>
    [System.Serializable]
    public class PointerEventDataEvent : UnityEvent<UnityEngine.EventSystems.PointerEventData> { }
}