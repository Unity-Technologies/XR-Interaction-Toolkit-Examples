using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

    /// <summary>
    /// If this object is not being held, return to a snap zone
    /// </summary>
    public class ReturnToSnapZone : MonoBehaviour {

        [Tooltip("The SnapZone to return to if not being held")]
        public SnapZone ReturnTo;

        [Tooltip("How fast to Lerp Towards the SnapZone")]
        public float LerpSpeed = 15f;

        [Tooltip("How long to wait before starting to Lerp the object back towards the SnapZone. In Seconds.")]
        public float ReturnDelay = 0.1f;
        
        // How long we've been waiting
        float currentDelay = 0;

        Grabbable grab;
        Rigidbody rigid;
        bool useGravityInitial;

        [Tooltip("Initiate snap if distance between the Grabbable and SnapZone is <= SnapDistance")]
        public float SnapDistance = 0.05f;

        void Start() {
            grab = GetComponent<Grabbable>();
            rigid = GetComponent<Rigidbody>();
            useGravityInitial = rigid.useGravity;
        }

        void Update() {

            // Reset the counter if we're holding the item
            if(grab.BeingHeld) {
                currentDelay = 0;
            }

            bool validReturn = grab != null && ReturnTo != null && ReturnTo.HeldItem == null && !grab.BeingHeld;

            // Increment how long we've been waiting
            if (validReturn) {
                currentDelay += Time.deltaTime;
            }

            // Start moving towards the SnapZone
            if(validReturn && currentDelay >= ReturnDelay) {
                moveToSnapZone();
            }
        }

        void moveToSnapZone() {
            
            rigid.useGravity = false;
            
            transform.position = Vector3.MoveTowards(transform.position, ReturnTo.transform.position, Time.deltaTime * LerpSpeed);

            if (Vector3.Distance(transform.position, ReturnTo.transform.position) < SnapDistance) {
                rigid.useGravity = useGravityInitial;
                ReturnTo.GrabGrabbable(grab);
            }
        }
    }
}