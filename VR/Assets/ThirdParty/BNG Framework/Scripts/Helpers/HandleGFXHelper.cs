using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

    /// <summary>
    /// Constrain the rotation based on localEulaerAngles
    /// </summary>
    public class HandleGFXHelper : MonoBehaviour {

        public Transform LookAt;

        /// <summary>
        /// If the handle is Grabbable we can use this to look at a Grabber when equipped
        /// </summary>
        public Grabbable HandleGrabbable;

        public float Speed = 5f;

        public float LocalYMin = 215f;
        public float LocalYMax = 270f;

        Vector3 initialRot;

        void Start() {
            initialRot = transform.localEulerAngles;
        }

        void Update() {

            // Something is holding the handle. Point at the Grabber
            if(HandleGrabbable != null && HandleGrabbable.BeingHeld) {
                Quaternion rot = Quaternion.LookRotation(HandleGrabbable.GetPrimaryGrabber().transform.position - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 10);

                Vector3 currentPos = transform.localEulerAngles;
                float constrainedY = Mathf.Clamp(currentPos.y, LocalYMin, LocalYMax);

                transform.localEulerAngles = new Vector3(initialRot.x, constrainedY, initialRot.z);
            }
            // Point at the LookAt Transform
            else {
                Quaternion rot = Quaternion.LookRotation(LookAt.position - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * Speed);

                Vector3 currentPos = transform.localEulerAngles;
                float constrainedY = Mathf.Clamp(currentPos.y, LocalYMin, LocalYMax);

                transform.localEulerAngles = new Vector3(initialRot.x, constrainedY, initialRot.z);
            }
        }
    }
}