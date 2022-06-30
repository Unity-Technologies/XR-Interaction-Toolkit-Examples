using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BNG {
    public class GrabPointTrigger : MonoBehaviour {

        public enum HandMovement {
            Instant,
            Lerp,
            None
        }

        [Header("Hand Movement")]
        [Tooltip("How to move the hand to the nearest grab point. If set to 'None', the hand model will remain at the controller.")]
        public HandMovement MoveInStyle = HandMovement.Instant;

        [Tooltip("How to move the hand back to the grabber")]
        public HandMovement MoveOutStyle = HandMovement.Instant;

        [Tooltip("How to fast to move the hand if MovementStyle = 'Lerp' or 'Linear'")]
        public float HandSpeed = 20f;

        [Tooltip("If true the hand model will continue to move towards the closest grab point. If false, the hand will only move towards the closest grab point once upon entering the trigger.")]
        public bool LiveUpdateNearestGrabPoint = true;

        [Header("Grabbable Options")]
        [Tooltip("If specified this Grabbable will be grabbed if the user holds down the grab button while this preview is active")]
        public Grabbable GrabObject;

        [Tooltip("If specified this Grabbable must be held for this component to work")]
        public Grabbable OtherGrabbableMustBeHeld;

        [Tooltip("Grab Points to move towards when the grabber is within the Trigger area.")]
        public List<GrabPoint> GrabPoints;

        Grabber currentGrabber;
        Grabbable dummyGrabbable;
        GrabPoint closestPoint;
        Grabber grabberInTrigger;

        void Start() {
            if(dummyGrabbable == null) {
                var go = new GameObject("Dummy Grabbable");
                dummyGrabbable = go.AddComponent<Grabbable>();
                dummyGrabbable.transform.parent = transform;
                dummyGrabbable.transform.localPosition = Vector3.zero;
                dummyGrabbable.transform.localRotation = Quaternion.identity;

                

                // Copy over grab points
                List<Transform> grabs = new List<Transform>();
                for (int x = 0; x < GrabPoints.Count; x++) {
                    GrabPoint g = GrabPoints[x];
                    grabs.Add(g.transform);
                }
                dummyGrabbable.GrabPoints = grabs;
                dummyGrabbable.GrabMechanic = GrabType.Snap;
                dummyGrabbable.ParentHandModel = false;
                dummyGrabbable.CanBeDropped = false;

                // Copy settings over from reference object if available
                if(GrabObject != null) {
                    dummyGrabbable.GrabButton = GrabObject.GrabButton;
                    dummyGrabbable.Grabtype = GrabObject.Grabtype;
                }
            }
        }

        void Update() {

            // holding this object
            if(dummyGrabbable != null && currentGrabber != null) {

                // Do we need to drop this object?
                if(OtherGrabbableMustBeHeld != null && !OtherGrabbableMustBeHeld.BeingHeld) {
                    ReleaseGrabber();
                    return;
                }

                // Is there a new grab point that's closer?
                if(LiveUpdateNearestGrabPoint) {
                    // dummyGrabbable.transform.position = 
                    Transform closestGrab = dummyGrabbable.GetClosestGrabPoint(currentGrabber);

                    if (closestGrab != null) {
                        var newPoint = closestGrab.GetComponent<GrabPoint>();
                        if(newPoint != null && newPoint != closestPoint) {
                            UpdateGrabPoint(newPoint);
                        }
                    }
                }

                // Move the hand
                if (MoveInStyle == HandMovement.Lerp) {
                    currentGrabber.HandsGraphics.localPosition = Vector3.Lerp(currentGrabber.HandsGraphics.localPosition, currentGrabber.handsGraphicsGrabberOffset, Time.deltaTime * HandSpeed);
                    currentGrabber.HandsGraphics.localRotation = Quaternion.Slerp(currentGrabber.HandsGraphics.localRotation, Quaternion.identity, Time.deltaTime * HandSpeed);
                }

                // User pressed grab key while holding object
                if (currentGrabber.GetInputDownForGrabbable(dummyGrabbable)) {

                    if(GrabObject != null) {
                        var prevGrabber = currentGrabber;
                        ReleaseGrabber();
                        prevGrabber.GrabGrabbable(GrabObject);
                    }
                }
            }

            // Are we back in the trigger?
            if(grabberInTrigger != null && !grabberInTrigger.HoldingItem && currentGrabber == null) {
                setGrabber(grabberInTrigger);
            }
        }

        public virtual void UpdateGrabPoint(GrabPoint newPoint) {
            closestPoint = newPoint;

            // Update hand animation
            // dummyGrabbable.CustomHandPose = newPoint.HandPose;
            dummyGrabbable.handPoseType = newPoint.handPoseType;
            dummyGrabbable.SelectedHandPose = newPoint.SelectedHandPose;

            // Move Hand Graphics if they are available
            if (currentGrabber != null && currentGrabber.HandsGraphics != null) {

                if (MoveInStyle != HandMovement.None) {
                    currentGrabber.HandsGraphics.parent = closestPoint.transform;
                }

                // Move hands in place
                if (MoveInStyle == HandMovement.Instant) {
                    currentGrabber.HandsGraphics.localPosition = currentGrabber.handsGraphicsGrabberOffset;
                    currentGrabber.HandsGraphics.localEulerAngles = Vector3.zero;
                }
            }
        }

        void OnTriggerEnter(Collider other) {

            // Object isn't being held, ignore this
            if(OtherGrabbableMustBeHeld != null && !OtherGrabbableMustBeHeld.BeingHeld) {
                return;
            }

            // Already have something in the trigger
            if(grabberInTrigger != null) {
                return;
            }

            // Our component has been disabled
            if(!this.isActiveAndEnabled || dummyGrabbable == null) {
                return;
            }

            Grabber grab = other.GetComponent<Grabber>();
            if (grab != null && !grab.HoldingItem && currentGrabber == null) {

                // Check if any Grab Points have been found
                Transform closestGrab = dummyGrabbable.GetClosestGrabPoint(grab);

                if(closestGrab != null) {
                    closestPoint = closestGrab.GetComponent<GrabPoint>();
                }
                else {
                    closestPoint = null;
                }

                grabberInTrigger = grab;

                // Update Grabber
                if (closestPoint != null) {
                    dummyGrabbable.ActiveGrabPoint = closestPoint;
                    setGrabber(grab);
                }
            }
        }

        void OnTriggerExit(Collider other) {
            Grabber grab = other.GetComponent<Grabber>();

            // No longer inside trigger
            if (grab != null && grab == grabberInTrigger) {
                grabberInTrigger = null;
            }

            // Release grabber if out
            if (grab != null && grab == currentGrabber) {
                ReleaseGrabber();
            }
        }

        void setGrabber(Grabber theGrabber) {
            currentGrabber = theGrabber;

            dummyGrabbable.CanBeDropped = false;
            dummyGrabbable.BeingHeld = true;

            currentGrabber.HeldGrabbable = dummyGrabbable;

            UpdateGrabPoint(closestPoint);
        }

        public virtual void ReleaseGrabber() {

            if (currentGrabber != null) {
                dummyGrabbable.CanBeDropped = true;
                dummyGrabbable.BeingHeld = false;

                currentGrabber.HeldGrabbable = null;

                if (MoveOutStyle == HandMovement.Instant) {
                    currentGrabber.ResetHandGraphics();
                }
                else if (MoveOutStyle == HandMovement.Lerp) {
                    currentGrabber.ResetHandGraphics();
                }

                currentGrabber = null;
            }
        }
    }
}

