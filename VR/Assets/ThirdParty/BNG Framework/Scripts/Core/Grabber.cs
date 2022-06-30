using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BNG {

    /// <summary>
    /// A trigger collider that handles grabbing grabbables.
    /// </summary>
    [RequireComponent(typeof(GrabbablesInTrigger))]
    public class Grabber : MonoBehaviour {

        [Header("Hand Side")]
        /// <summary>
        /// Which controller side. None if not attached to a controller.
        /// </summary>
        [Tooltip("Which controller side. None if not attached to a controller.")]
        public ControllerHand HandSide = ControllerHand.Left;

        [Header("Grab Settings")]
        /// <summary>
        /// Which controller side. None if not attached to a controller.
        /// </summary>
        [Tooltip("The default hold type for all Grabbables. A Grabbable can manually override this default.")]
        public HoldType DefaultHoldType = HoldType.HoldDown;

        /// <summary>
        /// The default grab button for all Grabbables. A Grabbable can manually override this default.
        /// </summary>
        [Tooltip("The default grab button for all Grabbables. A Grabbable can manually override this default.")]
        public GrabButton DefaultGrabButton = GrabButton.Grip;

        [Header("Hold / Release")]
        /// <summary>
        /// 0-1 determine how much to consider a grip.
        /// Example : 0.75 is holding the grip down 3/4 of the way
        /// </summary>
        [Tooltip("0-1 determine how much to consider a grip. Example : 0.75 is holding the grip down 3/4 of the way.")]
        [Range(0.0f, 1f)]
        public float GripAmount = 0.9f;

        /// <summary>
        /// How much grip considered to release an ob ect (0-1)
        /// </summary>
        [Tooltip("How much grip considered to release an object (0-1). Example : 0.75 is holding the grip down 3/4 of the way")]
        [Range(0.0f, 1f)]
        public float ReleaseGripAmount = 0.5f;

        /// <summary>
        /// How many seconds to check for grab input while Grip is held down. After grip is held down for this long, grip will need to be repressed in order to pick up an object.
        /// </summary>
        [Tooltip("How many seconds to check for grab input while Grip is held down. After grip is held down for this long, grip will need to be repressed in order to pick up an object.")]
        public float GrabCheckSeconds = 0.5f;
        float currentGrabTime;

        [Header("Equip on Start")]
        /// <summary>
        /// Assign a Grabbable here if you want to auto equip it on Start
        /// </summary>
        [Tooltip("Assign a Grabbable here if you want to auto equip it on Start")]
        public Grabbable EquipGrabbableOnStart;

        [Header("Hand Graphics")]
        /// <summary>
        /// Root transform that holds hands models. We may want to hide these while holding certain objects.
        /// </summary>
        [Tooltip("Root transform that holds hands models. We may want to hide these while holding certain objects, or parent this object to the grabbable so they follow the object perfectly.")]
        public Transform HandsGraphics;

        Transform handsGraphicsParent;
        Vector3 handsGraphicsPosition;
        Quaternion handsGraphicsRotation;

        [Header("Shown for Debug :")]
        /// <summary>
        /// The Grabbable we are currently holding. Null if not holding anything.
        /// </summary>
        [Tooltip("The Grabbable we are currently holding. Null if not holding anything.")]
        public Grabbable HeldGrabbable;

        /// <summary>
        /// Same as holding down grip if set to true. Should not have same value as ForceRelease.
        /// </summary>
        [Tooltip("Same as holding down grip if set to true. Should not have same value as ForceRelease.")]
        public bool ForceGrab = false;

        /// <summary>
        /// Force the release of grip
        /// </summary>
        [Tooltip("Force the release of grip if set to true. Should not have same value as ForceGrab.")]
        public bool ForceRelease = false;

        [Tooltip("Time.time when we last dropped a Grabbable")]
        public float LastDropTime;

        Grabbable previousClosest;
        Grabbable previousClosestRemote;

        /// <summary>
        /// Are we currently holding any valid items?
        /// </summary>
        public bool HoldingItem
        {
            get { return HeldGrabbable != null; }
        }

        /// <summary>
        /// Are we currently pulling a remote grabbable towards us?
        /// </summary>
        public bool RemoteGrabbingItem {
            get { return flyingGrabbable != null; }
        }

        /// <summary>
        /// Keep track of all grabbables in trigger
        /// </summary>
        GrabbablesInTrigger grabsInTrigger;
        public GrabbablesInTrigger GrabsInTrigger {
            get {
                return grabsInTrigger;
            }
        }

        /// <summary>
        /// Returns the Grabbable object if we are currenly remote grabbing an object
        /// </summary>
        public Grabbable RemoteGrabbingGrabbable {
            get {
                return flyingGrabbable;
            }
        }
        Grabbable flyingGrabbable;

        // How long the object has been flying at our hand
        float flyingTime = 0;        

        // Offset Hand Models are from Grabber
        public Vector3 handsGraphicsGrabberOffset { get; private set; }
        public Vector3 handsGraphicsGrabberOffsetRotation { get; private set; }                

        [HideInInspector]
        public Vector3 PreviousPosition;

        /// <summary>
        /// Can be used to position hands independently from model
        /// </summary>
        [HideInInspector]
        public Transform DummyTransform; 

        Rigidbody rb;
        InputBridge input;
        ConfigurableJoint joint;

        // Is this a fresh grab / has the control been depressed
        [HideInInspector]
        public bool FreshGrip = true;

        [Header("Grabber Events")]
        [Tooltip("Called immediately before a Grabbable object is officially grabbed")]
        public GrabbableEvent onGrabEvent;

        [Tooltip("Called immediately after a Grabbable object is grabbed. Use this if you need the Grabbable object to be setup before accessing it")]
        public GrabbableEvent onAfterGrabEvent;

        [Tooltip("Called immediately before droppping an item")]
        public GrabbableEvent onReleaseEvent;

        // For tracking velocity
        [HideInInspector]
        public VelocityTracker velocityTracker;

        void Start() {
            rb = GetComponent<Rigidbody>();
            grabsInTrigger = GetComponent<GrabbablesInTrigger>();
            joint = GetComponent<ConfigurableJoint>();
            input = InputBridge.Instance;

            // Setup defaults
            if (joint == null) {
                joint = gameObject.AddComponent<ConfigurableJoint>();
                joint.rotationDriveMode = RotationDriveMode.Slerp;

                JointDrive slerpDrive = joint.slerpDrive;
                slerpDrive.positionSpring = 600;

                JointDrive xDrive = joint.xDrive;
                xDrive.positionSpring = 2500;
                JointDrive yDrive = joint.yDrive;
                yDrive.positionSpring = 2500;
                JointDrive zDrive = joint.zDrive;
                zDrive.positionSpring = 2500;
            }

            if(HandsGraphics) {
                handsGraphicsParent = HandsGraphics.transform.parent;
                handsGraphicsPosition = HandsGraphics.transform.localPosition;
                handsGraphicsRotation = HandsGraphics.transform.localRotation;

                handsGraphicsGrabberOffset = transform.InverseTransformPoint(HandsGraphics.position);
                handsGraphicsGrabberOffsetRotation = transform.localEulerAngles;
            }

            // Make Collision Dynamic so we don't miss any collisions
            if (rb && rb.isKinematic) {
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }

            // Should we auto equip an item
            if(EquipGrabbableOnStart != null) {
                GrabGrabbable(EquipGrabbableOnStart);
            }

            // Velocity Tracking
            if(velocityTracker == null) {
                velocityTracker = GetComponent<VelocityTracker>();
            }

            // Add Velocity Tracker if one was not provided
            if (velocityTracker == null) {
                velocityTracker = gameObject.AddComponent<VelocityTracker>();
                velocityTracker.controllerHand = HandSide;
            }
        }
        
        void Update() {

            // Keep track of how long an object has been trying to fly to our hand
            if(flyingGrabbable != null) {
                flyingTime += Time.deltaTime;
                
                // Only allow an object to fly towards us
                float maxFlyingGrabbableTime = 5;
                if(flyingTime > maxFlyingGrabbableTime) {
                    resetFlyingGrabbable();
                }
            }           

            // Make sure grab is valid
            updateFreshGrabStatus();            

            // Fire off updates
            checkGrabbableEvents();

            // Check for input to grab or release item
            if ((HoldingItem == false && InputCheckGrab()) || ForceGrab) {
                TryGrab();               
            }
            else if(((HoldingItem || RemoteGrabbingItem) && inputCheckRelease()) || ForceRelease) {                
                TryRelease();
            }
        }

        void updateFreshGrabStatus() {
            // Update Fresh Grab status
            if (getGrabInput(GrabButton.Grip) <= ReleaseGripAmount) {
                // We release grab, so this is considered fresh
                FreshGrip = true;
                currentGrabTime = 0;
            }

            // Increment fresh grab time
            if (getGrabInput(GrabButton.Grip) > GripAmount) {
                currentGrabTime += Time.deltaTime;
            }

            // Not considered a valid grab if holding down for too long
            if (currentGrabTime > GrabCheckSeconds) {
                FreshGrip = false;
            }
        }

        void checkGrabbableEvents() {

            // Bail if nothing in our trigger area
            if(grabsInTrigger == null) {
                return;
            }

            // If last closest was this one let event know and remove validator  
            if (previousClosest != grabsInTrigger.ClosestGrabbable) {
                if (previousClosest != null) {

                    // Fire Off Events
                    GrabbableEvents[] ge = previousClosest.GetComponents<GrabbableEvents>();
                    if (ge != null) {
                        for (int x = 0; x < ge.Length; x++) {
                            ge[x].OnNoLongerClosestGrabbable(HandSide);
                            ge[x].OnNoLongerClosestGrabbable(this);
                        }
                    }
                    previousClosest.RemoveValidGrabber(this);
                }

                // Update closest Grabbable
                if (grabsInTrigger.ClosestGrabbable != null && !HoldingItem) {

                    // Fire Off Events
                    GrabbableEvents[] ge = grabsInTrigger.ClosestGrabbable.GetComponents<GrabbableEvents>();
                    if (ge != null) {
                        for (int x = 0; x < ge.Length; x++) {
                            ge[x].OnBecomesClosestGrabbable(HandSide);
                            ge[x].OnBecomesClosestGrabbable(this);
                        }
                    }
                    grabsInTrigger.ClosestGrabbable.AddValidGrabber(this);
                }
            }

            if (grabsInTrigger.ClosestGrabbable != null && !HoldingItem) {
                grabsInTrigger.ClosestGrabbable.AddValidGrabber(this);
            }

            // Remote Grabbable Events
            // If last closest was this one, unhighlight object            
            if (previousClosestRemote != grabsInTrigger.ClosestRemoteGrabbable) {
                if (previousClosestRemote != null) {
                    // Fire Off Events
                    GrabbableEvents[] ge = previousClosestRemote.GetComponents<GrabbableEvents>();
                    if (ge != null) {
                        for (int x = 0; x < ge.Length; x++) {
                            ge[x].OnNoLongerClosestRemoteGrabbable(HandSide);
                            ge[x].OnNoLongerClosestRemoteGrabbable(this);
                        }

                    }
                    previousClosestRemote.RemoveValidGrabber(this);
                }

                // Update closest remote Grabbable
                if (grabsInTrigger.ClosestRemoteGrabbable != null && !HoldingItem) {

                    // Fire Off Events 
                    GrabbableEvents[] ge = grabsInTrigger.ClosestRemoteGrabbable.GetComponents<GrabbableEvents>();
                    if (ge != null) {
                        for (int x = 0; x < ge.Length; x++) {
                            ge[x].OnBecomesClosestRemoteGrabbable(HandSide);
                            ge[x].OnBecomesClosestRemoteGrabbable(this);
                        }
                    }

                    grabsInTrigger.ClosestRemoteGrabbable.AddValidGrabber(this);
                }
            }

            // Set this as previous closest
            previousClosest = grabsInTrigger.ClosestGrabbable;
            previousClosestRemote = grabsInTrigger.ClosestRemoteGrabbable;
        }

        // See if we are inputting controls to grab an item
        public virtual bool InputCheckGrab() {

            // Nothing nearby to grab
            Grabbable closest = getClosestOrRemote();

            return GetInputDownForGrabbable(closest);           
        }

        public virtual bool GetInputDownForGrabbable(Grabbable grabObject) {

            if(grabObject == null) {
                return false;
            }

            // Check Hold Controls
            HoldType closestHoldType = getHoldType(grabObject);
            GrabButton closestGrabButton = GetGrabButton(grabObject);

            // Hold to grab controls
            if (closestHoldType == HoldType.HoldDown) {
                bool grabInput = getGrabInput(closestGrabButton) >= GripAmount;

                if (closestGrabButton == GrabButton.Grip && !FreshGrip) {
                    return false;
                }

                //if(HandSide == ControllerHand.Left && InputBridge.Instance.LeftTrigger > 0.9f) {
                //    Debug.Log("Trigger Down");
                //}

                return grabInput;
            }
            // Check Toggle Controls
            else if (closestHoldType == HoldType.Toggle) {
                return getToggleInput(closestGrabButton);
            }

            return false;
        }
        
        HoldType getHoldType(Grabbable grab) {
            HoldType closestHoldType = grab.Grabtype;

            // Inherit from Grabber
            if (closestHoldType == HoldType.Inherit) {
                closestHoldType = DefaultHoldType;
            }

            // Inherit isn't a value in itself. Use "hold down" instead and warn the user
            if (closestHoldType == HoldType.Inherit) {
                closestHoldType = HoldType.HoldDown;
                Debug.LogWarning("Inherit found on both Grabber and Grabbable. Consider updating the Grabber's DefaultHoldType");
            }

            return closestHoldType;
        }
        
        public virtual GrabButton GetGrabButton(Grabbable grab) {
            GrabButton grabButton = grab.GrabButton;

            // Inherit from Grabber
            if (grabButton == GrabButton.Inherit) {
                grabButton = DefaultGrabButton;
            }

            // Inherit isn't a value in itself. Use "Grip" instead and warn the user
            if (grabButton == GrabButton.Inherit) {
                grabButton = GrabButton.Grip;
                Debug.LogWarning("Inherit found on both Grabber and Grabbable. Consider updating the Grabber's DefaultHoldType");
            }

            return grabButton;
        }


        Grabbable getClosestOrRemote() {
            if (grabsInTrigger.ClosestGrabbable != null) {
                return grabsInTrigger.ClosestGrabbable;
            }
            else if (grabsInTrigger.ClosestRemoteGrabbable != null) {
                return grabsInTrigger.ClosestRemoteGrabbable;
            }

            return null;
        }

        // Release conditions are a little different than grab
        bool inputCheckRelease() {

            var grabbingGrabbable = RemoteGrabbingItem ? flyingGrabbable : HeldGrabbable;

            // Can't release anything we're not holding
            if (grabbingGrabbable == null) {
                return false;
            }

            // Check Hold Controls
            HoldType closestHoldType = getHoldType(grabbingGrabbable);
            GrabButton closestGrabButton = GetGrabButton(grabbingGrabbable);

            if (closestHoldType == HoldType.HoldDown) {
                return getGrabInput(closestGrabButton) <= ReleaseGripAmount;
            }
            // Check for toggle controls
            else if (closestHoldType == HoldType.Toggle) {
                return getToggleInput(closestGrabButton);
            }

            return false;
        }

        float getGrabInput(GrabButton btn) {
            float gripValue = 0;

            if(input == null) {
                return 0;
            }

            // Left Hand
            if (HandSide == ControllerHand.Left) {
                if (btn == GrabButton.Grip) {
                    gripValue = input.LeftGrip;
                }
                else if (btn == GrabButton.Trigger) {
                    gripValue = input.LeftTrigger;
                }
            }
            // Right Hand
            else if (HandSide == ControllerHand.Right) {
                if (btn == GrabButton.Grip) {
                    gripValue = input.RightGrip;
                }
                else if (btn == GrabButton.Trigger) {
                    gripValue = input.RightTrigger;
                }
            }

            return gripValue;
        }

        bool getToggleInput(GrabButton btn) {

            if (input == null) {
                return false;
            }

            // Left Hand
            if (HandSide == ControllerHand.Left) {
                if (btn == GrabButton.Grip) {
                    return input.LeftGripDown;
                }
                else if (btn == GrabButton.Trigger) {
                    return input.LeftTriggerDown;
                }
            }
            // Right Hand
            else if (HandSide == ControllerHand.Right) {
                if (btn == GrabButton.Grip) {
                    return input.RightGripDown;
                }
                else if (btn == GrabButton.Trigger) {
                    return input.RightTriggerDown;
                }
            }

            return false;
        }               

        public virtual bool TryGrab() {
            // Already holding something
            if (HeldGrabbable != null) {
                return false;
            }            

            // Activate Nearby Grabbable
            if (grabsInTrigger.ClosestGrabbable != null) {
                GrabGrabbable(grabsInTrigger.ClosestGrabbable);                
                
                return true;
            }
            // If no immediate grabbable, see if remote is available to pull
            else if(grabsInTrigger.ClosestRemoteGrabbable != null && flyingGrabbable == null) {
                flyingGrabbable = grabsInTrigger.ClosestRemoteGrabbable;
                flyingGrabbable.GrabRemoteItem(this);
            }

            return false;
        }

        // Assign new held Item, then grab the item into our hand / controller
        public virtual void GrabGrabbable(Grabbable item) {

            // We are trying to grab something else
            if(flyingGrabbable != null && item != flyingGrabbable) {
                return;
            }

            // Make sure we aren't flying an object at us still
            resetFlyingGrabbable();

            // Drop whatever we were holding
            if (HeldGrabbable != null && HeldGrabbable) {
                TryRelease();
            }

            // Assign new grabbable
            HeldGrabbable = item;

            // Just grabbed something, no longer fresh.
            FreshGrip = false;

            // Fire off Grabber 'before' grab event
            onGrabEvent?.Invoke(item);

            // Let item know it's been grabbed
            item.GrabItem(this);

            // Fire off Grabber 'after' grab event
            onAfterGrabEvent?.Invoke(item);
        }

        // Dropped whatever was in hand
        public virtual void DidDrop() {

            // Fire off Grabber Release event
            if (onReleaseEvent != null && HeldGrabbable != null) {
                onReleaseEvent.Invoke(HeldGrabbable);
            }

            HeldGrabbable = null;

            transform.localEulerAngles = Vector3.zero;

            LastDropTime = Time.time;

            resetFlyingGrabbable();

            ResetHandGraphics();
        }

        public virtual void HideHandGraphics() {
            if (HandsGraphics != null) {
                HandsGraphics.gameObject.SetActive(false);
            }
        }

        public virtual void ResetHandGraphics() {
            if(HandsGraphics != null) {
                // Make visible again
                HandsGraphics.gameObject.SetActive(true);

                // Move parent back to where it was originally
                HandsGraphics.transform.parent = handsGraphicsParent;
                HandsGraphics.transform.localPosition = handsGraphicsPosition;
                HandsGraphics.transform.localRotation = handsGraphicsRotation;
            }
        }

        public virtual void TryRelease() {
            if (HeldGrabbable != null && HeldGrabbable.CanBeDropped) {
                HeldGrabbable.DropItem(this);
            }

            // No longer try to bring flying grabbable to us
            resetFlyingGrabbable();
        }

        void resetFlyingGrabbable() {
            // No longer flying at us
            if (flyingGrabbable != null) {
                flyingGrabbable.ResetGrabbing();
                flyingGrabbable = null;
                flyingTime = 0;
            }
        }       

        public virtual Vector3 GetGrabberAveragedVelocity() {
            return velocityTracker.GetAveragedVelocity();
        }

        public virtual Vector3 GetGrabberAveragedAngularVelocity() {
            return velocityTracker.GetAveragedAngularVelocity();
        }
    }
}