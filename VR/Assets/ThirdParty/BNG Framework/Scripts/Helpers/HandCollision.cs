using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    /// <summary>
    /// Controls collision state of Hand Model.
    /// </summary>
    public class HandCollision : MonoBehaviour {

        /// <summary>
        /// Used to determine if pointing or gripping
        /// </summary>
        public HandController HandControl;

        /// <summary>
        /// Used to determine if Grabbing something
        /// </summary>
        public Grabber HandGrabber;

        /// <summary>
        /// If false we will not check for Hand Collision at all
        /// </summary>
        public bool EnableHandCollision = true;

        /// <summary>
        /// Should we enable hand colliders when pointing
        /// </summary>
        public bool EnableCollisionOnPoint = true;

        /// <summary>
        /// Should we enable hand colliders when making a fist
        /// </summary>
        public bool EnableCollisionOnFist = true;

        /// <summary>
        /// Should we enable hand colliders at all times (still respects EnableCollisionDuringGrab)
        /// </summary>
        public bool EnableCollisionOnAllPoses = false;

        /// <summary>
        /// Set to false to Disable Hand Colliders during grab or remote  grab
        /// </summary>
        public bool EnableCollisionDuringGrab = false;

        public float PointAmount;
        public float GripAmount;
        public bool MakingFist;

        // Colliders to keep track of
        List<Collider> handColliders;

        void Start() {
            handColliders = new List<Collider>();
            var tempColliders = GetComponentsInChildren<Collider>(true);

            // Only accept non-trigger colliders.
            foreach(var c in tempColliders) {
                if(!c.isTrigger) {
                    handColliders.Add(c);
                }
            }
        }

        void Update() {
            if(!EnableHandCollision) {
                return;
            }

            bool grabbing = HandGrabber != null && HandGrabber.HoldingItem;
           
            bool makingFist = HandControl != null && HandControl.GripAmount > 0.9f && (HandControl.PointAmount < 0.1 || HandControl.PointAmount > 1);
            MakingFist = makingFist;
            PointAmount = HandControl.PointAmount;
            GripAmount = HandControl.GripAmount;

            bool pointing = HandControl != null && HandControl.PointAmount > 0.9f && HandControl.GripAmount > 0.9f;

            for (int x = 0; x < handColliders.Count; x++) {
                Collider col = handColliders[x];

                // Immediately disable collider if no collision on grab
                if (EnableCollisionDuringGrab == false && grabbing) {
                    col.enabled = false;
                    continue;
                }

                // Immediately disable collider if we just released an item. 
                // This is so we don't enable the collider right when we are trying to drop something
                if(HandGrabber != null && (Time.time - HandGrabber.LastDropTime < 0.5f )) {
                    col.enabled = false;
                    continue;
                }

                bool enableCollider = false;
                if (EnableCollisionDuringGrab && grabbing) {
                    enableCollider = true;
                }
                else if (EnableCollisionOnPoint && pointing) {
                    enableCollider = true;
                }
                else if (EnableCollisionOnFist && makingFist) {
                    enableCollider = true;
                }
                else if (EnableCollisionOnAllPoses) {
                    enableCollider = true;
                }

                col.enabled = enableCollider;
            }
        }
    }
}