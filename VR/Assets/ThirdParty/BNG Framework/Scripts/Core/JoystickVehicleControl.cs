using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    /// <summary>
    /// This component is similar to the JoystickControl, but is designed to be used on fast moving Rigidbodies
    /// </summary>
    public class JoystickVehicleControl : MonoBehaviour {

        [Header("Grab Object")]
        public Grabbable JoystickGrabbable;

        [Header("Movement Speed")]
        [Tooltip("Set to True to Lerp towards the held hand. Set to False for Instant movement")]
        public bool UseSmoothLook = true;
        public float SmoothLookSpeed = 15f;

        [Header("Hinge X")]
        public Transform HingeXTransform;
        public float MinXAngle = -45f;
        public float MaxXAngle = 45f;

        [Header("Hinge Y")]
        public Transform HingeYTransform;
        public float MinYAngle = -45f;
        public float MaxYAngle = 45f;

        [Header("Return To Center")]
        [Tooltip("How fast to return to center if nothing is holding the Joystick. Set to 0 if you do not wish to Return to Center")]
        public float ReturnToCenterSpeed = 5f;

        [Header("Deadzone")]
        [Tooltip("Any values below this threshold will not be passed to events")]
        public float DeadZone = 0.001f;

        /// <summary>
        /// Event called when Joystick value is changed
        /// </summary>
        public FloatFloatEvent onJoystickChange;

        /// <summary>
        /// Event called when Joystick value is changed
        /// </summary>
        public Vector2Event onJoystickVectorChange;

        [Header("Shown for Debug : ")]
        /// <summary>
        /// Current Percentage of joystick on X axis (left / right)
        /// </summary>
        public float LeverPercentageX = 0;

        /// <summary>
        /// Current Percentage of joystick on Y axis (forward / back)
        /// </summary>
        public float LeverPercentageY = 0;

        public Vector2 LeverVector;
        public float angleX;
        public float angleY;

        Quaternion originalRot = Quaternion.identity;

        void Update() {
            if(JoystickGrabbable != null) {
                if(JoystickGrabbable.BeingHeld) {
                    Transform lookAt = JoystickGrabbable.GetPrimaryGrabber().transform;

                    // Look towards the Grabber
                    if (HingeXTransform) {
                        originalRot = HingeXTransform.rotation;

                        HingeXTransform.LookAt(lookAt, Vector3.left);

                        angleX = HingeXTransform.localEulerAngles.x;
                        if (angleX > 180) {
                            angleX -= 360;
                        }

                        HingeXTransform.localEulerAngles = new Vector3(Mathf.Clamp(angleX, MinXAngle, MaxXAngle), 0, 0);

                        if (UseSmoothLook) {
                            Quaternion newRot = HingeXTransform.rotation;
                            HingeXTransform.rotation = originalRot;
                            HingeXTransform.rotation = Quaternion.Lerp(HingeXTransform.rotation, newRot, Time.deltaTime * SmoothLookSpeed);
                        }
                    }
                    if (HingeYTransform) {

                        originalRot = HingeYTransform.rotation;

                        HingeYTransform.LookAt(lookAt, Vector3.left);

                        angleY = HingeYTransform.localEulerAngles.y;
                        if (angleY > 180) {
                            angleY -= 360;
                        }

                        HingeYTransform.localEulerAngles = new Vector3(0, Mathf.Clamp(angleY, MinYAngle, MaxYAngle), 0);

                        if (UseSmoothLook) {
                            Quaternion newRot = HingeYTransform.rotation;
                            HingeYTransform.rotation = originalRot;
                            HingeYTransform.rotation = Quaternion.Lerp(HingeYTransform.rotation, newRot, Time.deltaTime * SmoothLookSpeed);
                        }
                    }
                }
                // Return to center if not being held
                else if (ReturnToCenterSpeed > 0) {
                    if (HingeXTransform) {
                        HingeXTransform.localRotation = Quaternion.Lerp(HingeXTransform.localRotation, Quaternion.identity, Time.deltaTime * ReturnToCenterSpeed);
                    }
                    if (HingeYTransform) {
                        HingeYTransform.localRotation = Quaternion.Lerp(HingeYTransform.localRotation, Quaternion.identity, Time.deltaTime * ReturnToCenterSpeed);
                    }
                }

                CallJoystickEvents();
            }
        }

        public virtual void CallJoystickEvents() {
            // Call events
            angleX = HingeXTransform.localEulerAngles.x;
            if (angleX > 180) {
                angleX -= 360;
            }

            angleY = HingeYTransform.localEulerAngles.y;
            if (angleY > 180) {
                angleY -= 360;
            }

            LeverPercentageY = (angleX - MinXAngle) / (MaxXAngle - MinXAngle) * 100;
            LeverPercentageX = (angleY - MinYAngle) / (MaxYAngle - MinYAngle) * 100;

            OnJoystickChange(LeverPercentageX, LeverPercentageY);

            // Lever Vector Changed Event
            float xInput = Mathf.Lerp(-1f, 1f, LeverPercentageX / 100);
            float yInput = Mathf.Lerp(-1f, 1f, LeverPercentageY / 100);

            // Reset any values that are inside the deadzone
            if (DeadZone > 0) {
                if (Mathf.Abs(xInput) < DeadZone) {
                    xInput = 0;
                }
                if (Mathf.Abs(yInput) < DeadZone) {
                    yInput = 0;
                }
            }

            LeverVector = new Vector2(xInput, yInput);

            OnJoystickChange(LeverVector);
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

