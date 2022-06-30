using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

    /// <summary>
    /// An example hand controller that sets animation values depending on Grabber state
    /// </summary>
    public class HandController : MonoBehaviour {

        [Tooltip("HandController parent will be set to this on Start if specified")]
        public Transform HandAnchor;

        [Tooltip("If true, this transform will be parented to HandAnchor and it's position / rotation set to 0,0,0.")]
        public bool ResetHandAnchorPosition = true;

        public Animator HandAnimator;

        [Tooltip("(Optional) If specified, this HandPoser can be used when setting poses retrieved from a grabbed Grabbable.")]
        public HandPoser handPoser;

        [Tooltip("(Optional) If specified, this AutoPoser component can be used when if set on the Grabbable, or if AutoPose is set to true")]
        public AutoPoser autoPoser;

        [Tooltip("If true, this hand will autopose when not holding a Grabbable. AutoPoser must be specified.")]
        public bool AutoPoseWhenNoGrabbable = false;

        /// <summary>
        /// How fast to Lerp the Layer Animations
        /// </summary>
        [Tooltip("How fast to Lerp the Layer Animations")]
        public float HandAnimationSpeed = 20f;

        [Tooltip("Check the state of this grabber to determine animation state. If null, a child Grabber component will be used.")]
        public Grabber grabber;

        [Header("Shown for Debug : ")]
        /// <summary>
        /// 0 = Open Hand, 1 = Full Grip
        /// </summary>
        public float GripAmount;
        private float _prevGrip;

        /// <summary>
        /// 0 = Index Curled in,  1 = Pointing Finger
        /// </summary>
        public float PointAmount;
        private float _prevPoint;

        /// <summary>
        /// 0 = Thumb Down, 1 = Thumbs Up
        /// </summary>
        public float ThumbAmount;
        private float _prevThumb;

        public int PoseId;

        ControllerOffsetHelper offset;
        InputBridge input;
        Rigidbody rigid;
        Transform offsetTransform;

        Vector3 offsetPosition {
            get {
                if(offset) {
                    return offset.OffsetPosition;
                }
                return Vector3.zero;
            }
        }

        Vector3 offsetRotation {
            get {
                if (offset) {
                    return offset.OffsetRotation;
                }
                return Vector3.zero;
            }
        }

        void Start() {

            rigid = GetComponent<Rigidbody>();
            offset = GetComponent<ControllerOffsetHelper>();
            offsetTransform = new GameObject("OffsetHelper").transform;
            offsetTransform.parent = transform;

            if (HandAnchor) {
                transform.parent = HandAnchor;
                offsetTransform.parent = HandAnchor;

                if (ResetHandAnchorPosition) {
                    transform.localPosition = offsetPosition;
                    transform.localEulerAngles = offsetRotation;
                }
            }
            
            if(grabber == null) {
                grabber = GetComponentInChildren<Grabber>();
            }

            // Subscribe to grab / release events
            if(grabber != null) {
                grabber.onAfterGrabEvent.AddListener(OnGrabberGrabbed);
                grabber.onReleaseEvent.AddListener(OnGrabberReleased);
            }

            // Try getting child animator
            SetHandAnimator();

            input = InputBridge.Instance;
        }

        public void Update() {

            CheckForGrabChange();

            // Set Hand state according to InputBridge
            UpdateFromInputs();
            
            UpdateAnimimationStates();
                        
            UpdateHandPoser();
        }

        public GameObject PreviousHeldObject;

        public virtual void CheckForGrabChange() {
            if(grabber != null) {

                // Check for null object but no animator enabled
                if(grabber.HeldGrabbable == null && PreviousHeldObject != null) {                    
                    OnGrabDrop();
                }
                else if(grabber.HeldGrabbable != null && !GameObject.ReferenceEquals(grabber.HeldGrabbable.gameObject, PreviousHeldObject)) {
                    OnGrabChange(grabber.HeldGrabbable.gameObject);
                }
            }
        }

        public virtual void OnGrabChange(GameObject newlyHeldObject) {

            // Update Component state if the held object has changed
            if(grabber != null && grabber.HeldGrabbable != null) {

                // Switch components based on held object properties
                // Animator
                if (grabber.HeldGrabbable.handPoseType == HandPoseType.AnimatorID) {
                    EnableHandAnimator();
                }
                // Auto Poser - Once
                else if (grabber.HeldGrabbable.handPoseType == HandPoseType.AutoPoseOnce) {
                    EnableAutoPoser(false);
                }
                // Auto Poser - Continuous
                else if (grabber.HeldGrabbable.handPoseType == HandPoseType.AutoPoseContinuous) {
                    EnableAutoPoser(true);
                }
                // Hand Poser
                else if (grabber.HeldGrabbable.handPoseType == HandPoseType.HandPose) {
                    // If we have a valid hand pose use it, otherwise fall back to the animator if it is available
                    if (grabber.HeldGrabbable.SelectedHandPose != null) {
                        EnableHandPoser();
                    }
                    else {
                        EnableHandAnimator();
                    }
                    
                }
            }

            PreviousHeldObject = newlyHeldObject;
        }

        /// <summary>
        /// Dropped our held item - nothing currently in our hands
        /// </summary>
        public virtual void OnGrabDrop() {

            // Should we use auto pose when nothing in the hand?
            if(AutoPoseWhenNoGrabbable) {
                EnableAutoPoser(true);
            }
            // Otherwise default to animator if it's available
            else {
                EnableHandAnimator();
                DisableAutoPoser();
            }

            PreviousHeldObject = null;
        }       

        public virtual void SetHandAnimator() {
            if (HandAnimator == null || !HandAnimator.gameObject.activeInHierarchy) {
                HandAnimator = GetComponentInChildren<Animator>();
            }
        }

        /// <summary>
        /// Update GripAmount, PointAmount, and ThumbAmount based raw input from InputBridge
        /// </summary>
        public virtual void UpdateFromInputs() {

            // Grabber may have been deactivated
            if (grabber == null || !grabber.isActiveAndEnabled) {
                grabber = GetComponentInChildren<Grabber>();
                GripAmount = 0;
                PointAmount = 0;
                ThumbAmount = 0;
                return;
            }

            if (grabber.HandSide == ControllerHand.Left) {
                GripAmount = input.LeftGrip;
                PointAmount = 1 - input.LeftTrigger; // Range between 0 and 1. 1 == Finger all the way out
                PointAmount *= InputBridge.Instance.InputSource == XRInputSource.SteamVR ? 0.25F : 0.5F; // Reduce the amount our finger points out if Oculus or XRInput

                // If not near the trigger, point finger all the way out
                if (input.SupportsIndexTouch && input.LeftTriggerNear == false && PointAmount != 0) {
                    PointAmount = 1f;
                }
                // Does not support touch, stick finger out as if pointing if no trigger found
                else if (!input.SupportsIndexTouch && input.LeftTrigger == 0) {
                    PointAmount = 1;
                }

                ThumbAmount = input.LeftThumbNear ? 0 : 1;
            }
            else if (grabber.HandSide == ControllerHand.Right) {
                GripAmount = input.RightGrip;
                PointAmount = 1 - input.RightTrigger; // Range between 0 and 1. 1 == Finger all the way out
                PointAmount *= InputBridge.Instance.InputSource == XRInputSource.SteamVR ? 0.25F : 0.5F; // Reduce the amount our finger points out if Oculus or XRInput

                // If not near the trigger, point finger all the way out
                if (input.SupportsIndexTouch && input.RightTriggerNear == false && PointAmount != 0) {
                    PointAmount = 1f;
                }
                // Does not support touch, stick finger out as if pointing if no trigger found
                else if (!input.SupportsIndexTouch && input.RightTrigger == 0) {
                    PointAmount = 1;
                }

                ThumbAmount = input.RightThumbNear ? 0 : 1;
            }
        }

        public bool DoUpdateAnimationStates = true;
        public bool DoUpdateHandPoser = true;

        public virtual void UpdateAnimimationStates()
        {

            if(DoUpdateAnimationStates == false) {
                return;
            }

            // Enable Animator if it was disabled by the hand poser
            if(IsAnimatorGrabbable() && !HandAnimator.isActiveAndEnabled) {
                EnableHandAnimator();
            }

            // Update Hand Animator info
            if (HandAnimator != null && HandAnimator.isActiveAndEnabled && HandAnimator.runtimeAnimatorController != null) {

                _prevGrip = Mathf.Lerp(_prevGrip, GripAmount, Time.deltaTime * HandAnimationSpeed);
                _prevThumb = Mathf.Lerp(_prevThumb, ThumbAmount, Time.deltaTime * HandAnimationSpeed);
                _prevPoint = Mathf.Lerp(_prevPoint, PointAmount, Time.deltaTime * HandAnimationSpeed);

                // 0 = Hands Open, 1 = Grip closes                        
                HandAnimator.SetFloat("Flex", _prevGrip);

                HandAnimator.SetLayerWeight(1, _prevThumb);

                //// 0 = pointer finger inwards, 1 = pointing out    
                //// Point is played as a blend
                //// Near trigger? Push finger down a bit
                HandAnimator.SetLayerWeight(2, _prevPoint);

                // Should we use a custom hand pose?
                if (grabber != null && grabber.HeldGrabbable != null) {
                    HandAnimator.SetLayerWeight(0, 0);
                    HandAnimator.SetLayerWeight(1, 0);
                    HandAnimator.SetLayerWeight(2, 0);

                    PoseId = (int)grabber.HeldGrabbable.CustomHandPose;

                    if (grabber.HeldGrabbable.ActiveGrabPoint != null) {

                        // Default Grip to 1 when holding an item
                        HandAnimator.SetLayerWeight(0, 1);
                        HandAnimator.SetFloat("Flex", 1);

                        // Get the Min / Max of our finger blends if set by the user
                        // This allows a pose to blend between states
                        // Index Finger
                        setAnimatorBlend(grabber.HeldGrabbable.ActiveGrabPoint.IndexBlendMin, grabber.HeldGrabbable.ActiveGrabPoint.IndexBlendMax, PointAmount, 2);

                        // Thumb
                        setAnimatorBlend(grabber.HeldGrabbable.ActiveGrabPoint.ThumbBlendMin, grabber.HeldGrabbable.ActiveGrabPoint.ThumbBlendMax, ThumbAmount, 1);                       
                    }
                    else {
                        // Force everything to grab if we're holding something
                        if (grabber.HoldingItem) {
                            GripAmount = 1;
                            PointAmount = 0;
                            ThumbAmount = 0;
                        }
                    }

                    HandAnimator.SetInteger("Pose", PoseId);
                    
                }
                else {
                    HandAnimator.SetInteger("Pose", 0);
                }
            }
        }

        void setAnimatorBlend(float min, float max, float input, int animationLayer) {
            HandAnimator.SetLayerWeight(animationLayer, min + (input) * max - min);
        }

        /// <summary>
        /// Returns true if there is a valid animator and the held grabbable is set to use an Animation ID
        /// </summary>
        /// <returns></returns>
        public virtual bool IsAnimatorGrabbable() {
            return HandAnimator != null && grabber != null && grabber.HeldGrabbable != null && grabber.HeldGrabbable.handPoseType == HandPoseType.AnimatorID;
        }

        public virtual void UpdateHandPoser() {

            if (DoUpdateHandPoser == false) {
                return;
            }

            // HandPoser may have changed - check for new component
            if (handPoser == null || !handPoser.isActiveAndEnabled) {
                handPoser = GetComponentInChildren<HandPoser>();
            }                        

            // Bail early if missing any info
            if(handPoser == null || grabber == null || grabber.HeldGrabbable == null || grabber.HeldGrabbable.handPoseType != HandPoseType.HandPose) {
                return;
            }

            // Update hand pose if changed
            if(handPoser.CurrentPose != grabber.HeldGrabbable.SelectedHandPose) {
                UpdateCurrentHandPose();
            }
        }

        public virtual void EnableHandPoser() {
            // Disable the hand animator if we have a valid hand pose to use
            if(handPoser != null) {
                // Just need to make sure animator isn't enabled
                DisableHandAnimator();
            }
        }

        public virtual void EnableAutoPoser(bool continuous) {

            // Check if AutoPoser was set
            if (autoPoser == null || !autoPoser.gameObject.activeInHierarchy) {

                if(handPoser != null) {
                    autoPoser = handPoser.GetComponent<AutoPoser>();
                }
                // Check for active children
                else {
                    autoPoser = GetComponentInChildren<AutoPoser>(false);
                }
            }

            // Do the auto pose
            if (autoPoser != null) {
                autoPoser.UpdateContinuously = continuous;

                if(!continuous) {
                    autoPoser.UpdateAutoPoseOnce();
                }

                DisableHandAnimator();
            }
        }

        public virtual void DisableAutoPoser() {
            if (autoPoser != null) {
                autoPoser.UpdateContinuously = false;
            }
        }

        public virtual void EnableHandAnimator() {
            if (HandAnimator != null && HandAnimator.enabled == false) {
                HandAnimator.enabled = true;
            }

            // If using a hand poser reset the currennt pose so it can be set again later
            if(handPoser != null) {
                handPoser.CurrentPose = null;
            }
        }

        public virtual void DisableHandAnimator() {
            if (HandAnimator != null && HandAnimator.enabled) {
                HandAnimator.enabled = false;
            }
        }

        public virtual void OnGrabberGrabbed(Grabbable grabbed) {
            // Set the Hand Pose on our component
            if (grabbed.SelectedHandPose != null) {
                UpdateCurrentHandPose();
            }
        }

        public virtual void UpdateCurrentHandPose() {
            if(handPoser != null) {
                // Update the pose
                handPoser.CurrentPose = grabber.HeldGrabbable.SelectedHandPose;
                handPoser.OnPoseChanged();
            }
        }

        public virtual void OnGrabberReleased(Grabbable released) {
            OnGrabDrop();
        }
    }
}