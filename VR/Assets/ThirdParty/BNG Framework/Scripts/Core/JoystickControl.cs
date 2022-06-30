using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

    /// <summary>
    /// Helper for joystick type physical inputs
    /// </summary>
    public class JoystickControl : MonoBehaviour {

        [Header("Deadzone")]
        [Tooltip("Any values below this threshold will not be passed to events")]
        public float DeadZone = 0.001f;

        /// <summary>
        /// Minimum angle the Level can be rotated
        /// </summary>
        public float MinDegrees = -45f;

        /// <summary>
        /// Maximum angle the Level can be rotated
        /// </summary>
        public float MaxDegrees = 45f;

        /// <summary>
        /// Current Percentage of joystick on X axis (left / right)
        /// </summary>
        public float LeverPercentageX = 0;

        /// <summary>
        /// Current Percentage of joystick on Y axis (forward / back)
        /// </summary>
        public float LeverPercentageY = 0;

        public Vector2 LeverVector;

        public bool UseSmoothLook = true;
        public float SmoothLookSpeed = 15f;

        /// <summary>
        /// If true, the joystick's rigidbody will be kinematic when not being held. Enable this if you don't want your joystick to interact with physics or if you need moving platform support.
        /// </summary>
        public bool KinematicWhileInactive = false;

        /// <summary>
        /// Event called when Joystick value is changed
        /// </summary>
        public FloatFloatEvent onJoystickChange;

        /// <summary>
        /// Event called when Joystick value is changed
        /// </summary>
        public Vector2Event onJoystickVectorChange;


        Grabbable grab;
        Rigidbody rb;

        // Keep track of Joystick Rotation
        Vector3 currentRotation;
        public float angleX;
        public float angleY;

        void Start() {
            grab = GetComponent<Grabbable>();
            rb = GetComponent<Rigidbody>();
        }

        // Update is called once per frame
        void Update() {

            // Update Kinematic Status.
            if (rb) {
                rb.isKinematic = KinematicWhileInactive && !grab.BeingHeld;
            }

            // Align lever with Grabber
            doJoystickLook();

            // Lock our local position and axis in Update to avoid jitter
            transform.localPosition = Vector3.zero;
            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, 0);

            // Get the modified angle of of the lever. Use this to get percentage based on Min and Max angles.
            currentRotation = transform.localEulerAngles;
            angleX = Mathf.Floor(currentRotation.x);
            angleX = (angleX > 180) ? angleX - 360 : angleX;

            angleY = Mathf.Floor(currentRotation.y);
            angleY = (angleY > 180) ? angleY - 360 : angleY;

            // Cap Angles X
            if (angleX > MaxDegrees) {
                transform.localEulerAngles = new Vector3(MaxDegrees, currentRotation.y, currentRotation.z);
            }
            else if (angleX < MinDegrees) {
                transform.localEulerAngles = new Vector3(MinDegrees, currentRotation.y, currentRotation.z);
            }

            // Cap Angles Z
            if (angleY > MaxDegrees) {
                transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, currentRotation.y, MaxDegrees);
            }
            else if (angleY < MinDegrees) {
                transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, currentRotation.y, MinDegrees);
            }

            // Set percentage of level position
            LeverPercentageX = (angleY - MinDegrees) / (MaxDegrees - MinDegrees) * 100;
            LeverPercentageY = (angleX - MinDegrees) / (MaxDegrees - MinDegrees) * 100;

            // Lever value changed event
            OnJoystickChange(LeverPercentageX, LeverPercentageY);

            // Lever Vector Changed Event
            float xInput = Mathf.Lerp(-1f, 1f, LeverPercentageX / 100);
            float yInput = Mathf.Lerp(-1f, 1f, LeverPercentageY / 100);

            // Reset any values that are inside the deadzone
            if(DeadZone > 0) {
                if(Mathf.Abs(xInput) < DeadZone) {
                    xInput = 0;
                }
                if (Mathf.Abs(yInput) < DeadZone) {
                    yInput = 0;
                }
            }

            LeverVector = new Vector2(xInput, yInput);

            OnJoystickChange(LeverVector);
        }

        void FixedUpdate() {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        void doJoystickLook() {

            // Do Lever Look
            if (grab != null && grab.BeingHeld) {

                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                // Store original rotation to be used with smooth look
                Quaternion originalRot = transform.rotation;

                // Use the Grabber as our look target
                // Convert to local position so we can remove the x axis
                Vector3 localTargetPosition = transform.InverseTransformPoint(grab.GetPrimaryGrabber().transform.position);

                // Convert back to world position 
                Vector3 targetPosition = transform.TransformPoint(localTargetPosition);
                transform.LookAt(targetPosition, transform.up);

                if (UseSmoothLook) {
                    Quaternion newRot = transform.rotation;
                    transform.rotation = originalRot;
                    transform.rotation = Quaternion.Lerp(transform.rotation, newRot, Time.fixedDeltaTime * SmoothLookSpeed);
                }
            }
            else if (grab != null && !grab.BeingHeld && rb.isKinematic) {
                transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.identity, Time.deltaTime * SmoothLookSpeed);

                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        // Callback for lever percentage change
        public virtual void OnJoystickChange(float leverX, float leverY) {
            if (onJoystickChange != null) {
                onJoystickChange.Invoke(leverX, leverY);
            }
        }

        public virtual void OnJoystickChange(Vector2 joystickVector) {
            if (onJoystickVectorChange != null) {
                onJoystickVectorChange.Invoke(joystickVector);
            }
        }
    }
}
