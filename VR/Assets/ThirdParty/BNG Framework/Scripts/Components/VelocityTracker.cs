using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

    public class VelocityTracker : MonoBehaviour {

        public enum VelocityTrackingType {
            Device,   // Velocity is retrieved using XR Controller Velocity if it is supported. Will fall back to PerFrame if not.
            PerFrame // Calculate velocity per frame based on prior position / rotation
        }

        [Tooltip("This setting determines how retrieve the velocity. If 'Device' is selected and ControllerHand is specified, then velocity will be retrieved from the connected physical controller. Otherwise velocity is calculated on a per frame basis.")]
        public VelocityTrackingType trackingType = VelocityTrackingType.Device;

        [Tooltip("If ControllerHand is specified as Left or Right then velocity will attempt to be retrieved from the physical controller. If None, velocity will be calculated per frame.")]
        public ControllerHand controllerHand = ControllerHand.None;

        [Tooltip("How many frames to use when averaging retrieving velocity using GetAveragedVelocity / GetAveragedAngularVelocity")]
        public float AverageVelocityCount = 3;

        // Values used to manually track velocity
        private Vector3 _velocity;
        private Vector3 _angularVelocity;

        // Used for manual velocity tracking
        private Vector3 _lastPosition;
        private Quaternion _lastRotation;

        List<Vector3> previousVelocities = new List<Vector3>();
        List<Vector3> previousAngularVelocities = new List<Vector3>();

        // Used in out variables to calculate angleaxis
        float angle;
        Vector3 axis;

        // Used for tracking playspace rotation which may be needed to determine velocity of thrown objects
        GameObject playSpace;

        void Start() {
            playSpace = GameObject.Find("TrackingSpace");
        }

        void FixedUpdate() {
            UpdateVelocities();

            // Save our last position / rotation so we can use it for velocity calculations
            _lastPosition = transform.position;
            _lastRotation = transform.rotation;
        }

        public virtual void UpdateVelocities() {
            UpdateVelocity();
            UpdateAngularVelocity();
        }

        public virtual void UpdateVelocity() {
            // Update velocity based on current and previous position
            _velocity = (transform.position - _lastPosition) / Time.deltaTime;

            // Add Linear Velocity
            previousVelocities.Add(GetVelocity());

            // Shrink list if necessary
            if (previousVelocities.Count > AverageVelocityCount) {
                previousVelocities.RemoveAt(0);
            }
        }

        public virtual void UpdateAngularVelocity() {
            // Update our current angular velocity
            Quaternion deltaRotation = transform.rotation * Quaternion.Inverse(_lastRotation);
            deltaRotation.ToAngleAxis(out angle, out axis);
            angle *= Mathf.Deg2Rad;

            _angularVelocity = axis * angle * (1.0f / Time.deltaTime);

            // Add Angular Velocity
            previousAngularVelocities.Add(GetAngularVelocity());

            // Shrink list if necessary
            if (previousAngularVelocities.Count > AverageVelocityCount) {
                previousAngularVelocities.RemoveAt(0);
            }
        }

        public virtual Vector3 GetVelocity() {

            // Return velocity straight away if set to per frame velocity check. No need to check device.
            if(trackingType == VelocityTrackingType.PerFrame) {
                return _velocity;
            }

            // Try XR Input Velocity First
            Vector3 vel = InputBridge.Instance.GetControllerVelocity(controllerHand);

            // Fall back to tracking velocity on a per frame basis if current velocity is unknown
            if (vel == null || vel == Vector3.zero) {
                return _velocity;
            }
            else {
                // Add the playspace rotation in if necessary
                if(playSpace != null) {
                    return playSpace.transform.rotation* vel;
                }

                return vel;
            }
        }
       
        public virtual Vector3 GetAveragedVelocity() {
            return GetAveragedVector(previousVelocities);
        }

        public virtual Vector3 GetAngularVelocity() {

            // Device Angular Velocity appears to have some issues when being used in the editor. Sticking with per-frame angular Velocity for now as it is more reliable.
            return _angularVelocity;

            // Try XR Input AngularVelocity First
            //Vector3 angularVel = InputBridge.Instance.GetControllerAngularVelocity(controllerHand);

            //// Fall back to tracking velocity on a per frame basis if current velocity is unknown
            //if (angularVel == null || angularVel == Vector3.zero) {
            //    return _angularVelocity;
            //}
            //else {
            //    // Add the playspace rotation in if necessary
            //    if (playSpace != null) {
            //        return playSpace.transform.rotation * angularVel;
            //    }

            //    return angularVel;
            //}
        }

        public virtual Vector3 GetAveragedAngularVelocity() {
            return GetAveragedVector(previousAngularVelocities);
        }

        public virtual Vector3 GetAveragedVector(List<Vector3> vectors) {

            if (vectors != null) {

                int count = vectors.Count;
                float x = 0;
                float y = 0;
                float z = 0;

                for (int i = 0; i < count; i++) {
                    Vector3 v = vectors[i];
                    x += v.x;
                    y += v.y;
                    z += v.z;
                }

                return new Vector3(x / count, y / count, z / count);
            }

            return Vector3.zero;
        }
    }
}

