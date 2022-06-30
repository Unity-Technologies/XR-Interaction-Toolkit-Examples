using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    /// <summary>
    /// Rotate this object to point it's transform.forward at an object
    /// </summary>
    public class LookAtTransform : MonoBehaviour {

        /// <summary>
        /// The object to look at
        /// </summary>
        public Transform LookAt;

        /// <summary>
        /// If true will Slerp to the object. If false will use transform.LookAt
        /// </summary>
        public bool UseLerp = true;

        /// <summary>
        /// Slerp speed if UseLerp is true
        /// </summary>
        public float Speed = 20f;

        public bool UseUpdate = false;
        public bool UseLateUpdate = true;

        void Update() {
            if (UseUpdate) {
                lookAt();
            }
        }

        void LateUpdate() {
            if (UseLateUpdate) {
                lookAt();
            }
        }

        void lookAt() {

            if (LookAt != null) {

                if (UseLerp) {
                    Quaternion rot = Quaternion.LookRotation(LookAt.position - transform.position);

                    transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * Speed);
                }
                else {
                    transform.LookAt(LookAt, transform.forward);
                }
            }
        }
    }
}
