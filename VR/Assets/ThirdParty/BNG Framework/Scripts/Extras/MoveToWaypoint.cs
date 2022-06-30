using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BNG {
    public class MoveToWaypoint : MonoBehaviour {

        public bool IsActive = true;
        public Waypoint Destination;

        public float MovementSpeed = 1f;

        public bool ReachedDestination = false;

        [Tooltip("Delay in seconds to way before starting movement towards Destination")]
        public float StartDelay = 0f;
        bool reachedDelay = false;
        float delayedTime = 0;

        Vector3 previousPosition;
        public Vector3 PositionDifference;

        public bool MoveInUpdate = true;
        public bool MoveInFixedUpdate = true;

        Rigidbody rigid;

        // Start is called before the first frame update
        void Start() {
            rigid = GetComponent<Rigidbody>();
            rigid.isKinematic = true;
        }

        void Update() {
            // Update delay status
            if(!reachedDelay) {
                delayedTime += Time.deltaTime;
                if (delayedTime >= StartDelay) {
                    reachedDelay = true;
                }
            }

            if(MoveInUpdate) {
                movePlatform(Time.deltaTime);
            }

            PositionDifference = transform.position - previousPosition;

            previousPosition = transform.position;
        }

        void FixedUpdate() {
            if (MoveInFixedUpdate) {
                movePlatform(Time.fixedDeltaTime);
            }
        }

        void movePlatform(float timeDelta) {
            if (IsActive && !ReachedDestination && reachedDelay && Destination != null) {
                Vector3 direction = Destination.transform.position - transform.position;
                rigid.MovePosition(transform.position + (direction.normalized * MovementSpeed * timeDelta));

                // Update ReachedDestination 
                float dist = Vector3.Distance(transform.position, Destination.transform.position);
                if (Vector3.Distance(transform.position, Destination.transform.position) < 0.02f) {
                    ReachedDestination = true;

                    resetDelayStatus();

                    // Is there a new Destination?
                    if (Destination.Destination != null) {
                        Destination = Destination.Destination;
                        ReachedDestination = false;
                    }
                }
            }
        }

        void resetDelayStatus() {
            reachedDelay = false;
            delayedTime = 0;
        }
    }
}
