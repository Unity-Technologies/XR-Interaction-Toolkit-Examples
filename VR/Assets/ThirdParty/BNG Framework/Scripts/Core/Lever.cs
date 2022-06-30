using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace BNG {

    /// <summary>
    /// Helper class to interact with physical levers
    /// </summary>
    public class Lever : MonoBehaviour {

        [Header("Rotation Limits")]
        [Tooltip("Minimum X value in Local Euler Angles")]
        public float MinimumXRotation = -45f;

        [Tooltip("Maximum X value in Local Euler Angles")]
        public float MaximumXRotation = 45f;

        [Header("Initial Rotation")]
        public float InitialXRotation = 0f;

        [Header("Audio")]
        public AudioClip SwitchOnSound;
        public AudioClip SwitchOffSound;

        /// <summary>
        /// Tolerance before considering a switch flipped On or Off
        /// Ex : 1.25 Tolerance means switch can be 98.25% up and considered switched on
        /// </summary>
        [Header("Tolerance")]
        [Tooltip("Tolerance before considering a switch flipped On or Off. Ex : 1.25 Tolerance means switch can be 98.25% up and considered switched on, or 1.25% down to be considered switched off.")]
        public float SwitchTolerance = 1.25f;

        [Header("Smooth Look")]
        [Tooltip("If true the lever will lerp towards the Grabber. If false the lever will instantly point to the grabber")]
        public bool UseSmoothLook = true;

        [Tooltip("The speed at which to Lerp towards the Grabber if UseSmoothLook is enabled")]
        public float SmoothLookSpeed = 15f;

        [Header("Moving Platform Support")]
        /// <summary>
        /// If false, the lever's rigidbody will be kinematic when not being held. Disable this if you don't want your lever to interact with physics or if you need moving platform support.
        /// </summary>
        [Tooltip("If false, the lever's rigidbody will be kinematic when not being held. Disable this if you don't want your lever to interact with physics or if you need moving platform support.")]
        public bool AllowPhysicsForces = true;

        [Header("Return to Center (Must be Kinematic)")]
        /// <summary>
        /// If ReturnToCenter true and KinematicWhileInactive true then the lever will smooth look back to center when not being held
        /// </summary>
        [Tooltip("If ReturnToCenter true and KinematicWhileInactive true then the lever will smooth look back to center when not being held")]
        public bool ReturnToCenter = true;

        /// <summary>
        /// How fast to return to center if not being held
        /// </summary>
        [Tooltip("How fast to return to center if not being held")]
        public float ReturnLookSpeed = 5f;

        [Header("Snap Settings")]
        /// <summary>
        /// If true the lever will look directly at the Grabber and not factor in an initial offset
        /// </summary>
        [Tooltip("If true the lever will look directly at the Grabber and not factor in an initial offset")]
        public bool SnapToGrabber = false;

        [Header("Misc")]
        [Tooltip("If true, the Lever will be dropped once switched on or off")]
        public bool DropLeverOnActivation = false;

        [Header("Shown for Debug")]
        /// <summary>
        /// Current position of the lever as expressed as a percentage 1-100
        /// </summary>
        [Tooltip("Current position of the lever as expressed as a percentage 1-100")]
        public float LeverPercentage;

        [Tooltip("If true will show an angle helper in editor mode (Gizmos must be enabled)")]
        public bool ShowEditorGizmos = true;

        [Header("Events")]
        /// <summary>
        /// Called when lever was up, but is now in the down position
        /// </summary>
        [Tooltip("Called when lever was up, but is now in the down position")]
        public UnityEvent onLeverDown;

        /// <summary>
        /// Called when lever was down, but is now in the up position
        /// </summary>
        [Tooltip("Called when lever was down, but is now in the up position")]
        public UnityEvent onLeverUp;

        /// <summary>
        /// Called if the lever changes position at all
        /// </summary>
        [Tooltip("Called if the lever changes position at all")]
        public FloatEvent onLeverChange;

        Grabbable grab;
        Rigidbody rb;
        AudioSource audioSource;
        bool switchedOn;

        ConfigurableJoint configJoint;
        HingeJoint hingedJoint;

        private Vector3 _lastLocalAngle;

        void Start() {
            grab = GetComponent<Grabbable>();
            rb = GetComponent<Rigidbody>();
            hingedJoint = GetComponent<HingeJoint>();
            configJoint = GetComponent<ConfigurableJoint>();

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && (SwitchOnSound != null || SwitchOffSound != null)) {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        void Awake() {
            transform.localEulerAngles = new Vector3(InitialXRotation, 0, 0);
        }

        void Update() {

            // Update Kinematic Status.
            if (rb) {
                rb.isKinematic = AllowPhysicsForces == false && !grab.BeingHeld;
            }

            // Make sure grab offset is reset when not being held
            if (!grab.BeingHeld) {
                initialOffset = Quaternion.identity;
            }

            // Get the modified angle of of the lever. Use this to get percentage based on Min and Max angles.
            Vector3 currentRotation = transform.localEulerAngles;
            float angle = Mathf.Round(currentRotation.x);
            angle = (angle > 180) ? angle - 360 : angle;

            // Set percentage of level position
            LeverPercentage = GetAnglePercentage(angle);

            // Lever value changed event
            OnLeverChange(LeverPercentage);

            // Up / Down Events
            if ((LeverPercentage + SwitchTolerance) > 99 && !switchedOn) {
                OnLeverUp();
            }
            else if ((LeverPercentage - SwitchTolerance) < 1 && switchedOn) {
                OnLeverDown();
            }

            _lastLocalAngle = transform.localEulerAngles;
        }       

        public virtual float GetAnglePercentage(float currentAngle) {
            if (hingedJoint) {
                return (currentAngle - hingedJoint.limits.min) / (hingedJoint.limits.max - hingedJoint.limits.min) * 100;
            }

            if (configJoint) {
                return currentAngle / configJoint.linearLimit.limit * 100;
            }

            return 0;
        }

        void FixedUpdate() {

            // Align lever with Grabber
            doLeverLook();
        }

        Quaternion initialOffset = Quaternion.identity;

        void doLeverLook() {
            // Do Lever Look
            if (grab != null && grab.BeingHeld) {
                // Use the grabber as our look target. 
                Transform target = grab.GetPrimaryGrabber().transform;

                // Store original rotation to be used with smooth look
                Quaternion originalRot = transform.rotation;

                // Convert to local position so we can remove the x axis
                Vector3 localTargetPosition = transform.InverseTransformPoint(target.position);

                // Remove local X axis as this would cause the lever to rotate incorrectly
                localTargetPosition.x = 0f;

                // Convert back to world position 
                Vector3 targetPosition = transform.TransformPoint(localTargetPosition);
                transform.LookAt(targetPosition, transform.up);

                // Get the initial hand offset so our Lever doesn't jump to the grabber when we first grab it
                if (initialOffset == Quaternion.identity) {
                    initialOffset = originalRot * Quaternion.Inverse(transform.rotation);
                }

                if (!SnapToGrabber) {
                    transform.rotation = transform.rotation * initialOffset;
                }

                if (UseSmoothLook) {
                    Quaternion newRot = transform.rotation;
                    transform.rotation = originalRot;
                    transform.rotation = Quaternion.Lerp(transform.rotation, newRot, Time.deltaTime * SmoothLookSpeed);
                }
            }
            else if (grab != null && !grab.BeingHeld) {
                if (ReturnToCenter && AllowPhysicsForces == false) {
                    transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.identity, Time.deltaTime * ReturnLookSpeed);
                }
            }
        }

        /// <summary>
        /// Sets the X local euler angle of the lever. Angle is capped by MinimumXRotation / MaximumXRotation
        /// </summary>
        /// <param name="angle"></param>
        public virtual void SetLeverAngle(float angle) {
            transform.localEulerAngles = new Vector3(Mathf.Clamp(angle, MinimumXRotation, MaximumXRotation), 0, 0);
        }

        // Callback for lever percentage change
        public virtual void OnLeverChange(float percentage) {
            if (onLeverChange != null) {
                onLeverChange.Invoke(percentage);
            }
        }

        /// <summary>
        /// Lever Moved to down position
        /// </summary>
        public virtual void OnLeverDown() {

            if (SwitchOffSound != null) {
                audioSource.clip = SwitchOffSound;
                audioSource.Play();
            }

            if (onLeverDown != null) {
                onLeverDown.Invoke();
            }

            switchedOn = false;

            if (DropLeverOnActivation && grab != null) {
                grab.DropItem(false, false);
            }
        }

        /// <summary>
        /// Lever moved to up position
        /// </summary>
        public virtual void OnLeverUp() {

            if (SwitchOnSound != null) {
                audioSource.clip = SwitchOnSound;
                audioSource.Play();
            }

            // Fire event
            if (onLeverUp != null) {
                onLeverUp.Invoke();
            }

            switchedOn = true;

            if(DropLeverOnActivation && grab != null) {
                grab.DropItem(false, false);
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected() {

            if (ShowEditorGizmos && !Application.isPlaying) {

                Vector3 _origin = transform.position;
                float rotationDifference = MaximumXRotation - MinimumXRotation;

                float lineLength = 0.1f;
                float arcLength = 0.1f;

                //This is the color of the lines
                UnityEditor.Handles.color = Color.cyan;

                // Min / Max positions in World space
                Vector3 minPosition = _origin + Quaternion.AngleAxis(MinimumXRotation, transform.right) * transform.forward * lineLength;
                Vector3 maxPosition = _origin + Quaternion.AngleAxis(MaximumXRotation, transform.right) * transform.forward * lineLength;

                //Draw the min / max angle lines
                UnityEditor.Handles.DrawLine(_origin, minPosition);
                UnityEditor.Handles.DrawLine(_origin, maxPosition);

                // Draw starting position line
                Debug.DrawLine(transform.position, _origin + Quaternion.AngleAxis(InitialXRotation, transform.right) * transform.forward * lineLength, Color.magenta);

                // Fix for exactly 180
                if(rotationDifference == 180) {
                    minPosition = _origin + Quaternion.AngleAxis(MinimumXRotation + 0.01f, transform.right) * transform.forward * lineLength;
                }

                // Draw the arc
                Vector3 _cross = Vector3.Cross(minPosition - _origin, maxPosition - _origin);
                if(rotationDifference > 180) {
                    _cross = Vector3.Cross(maxPosition - _origin, minPosition - _origin);
                }

                UnityEditor.Handles.color = new Color(0, 255, 255, 0.1f);
                UnityEditor.Handles.DrawSolidArc(_origin, _cross, minPosition - _origin, rotationDifference, arcLength);
            }
        }
#endif
    }
}
