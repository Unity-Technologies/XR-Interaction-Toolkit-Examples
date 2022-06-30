using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    public class JointBreaker : MonoBehaviour {

        public float BreakDistance = 0.25f;

        public float JointDistance;

        /// <summary>
        /// Should we destroy the attached joint if BreakDistance is reached? Set to false if you only wish to execture an event
        /// </summary>
        public bool DestroyJointOnBreak = true;

        public GrabberEvent OnBreakEvent;

        Joint theJoint;

        Vector3 startPos;

        bool brokeJoint = false;

        // Start is called before the first frame update
        void Start() {
            startPos = transform.localPosition;
            theJoint = GetComponent<Joint>();
        }

        // Update is called once per frame
        void Update() {
            JointDistance = Vector3.Distance(transform.localPosition, startPos);

            if(!brokeJoint && JointDistance > BreakDistance) {
                BreakJoint();
            }
        }

        public void BreakJoint() {

            if(DestroyJointOnBreak &&  theJoint) {
                Destroy(theJoint);
            }

            if (OnBreakEvent != null) {

                var heldGrabbable = GetComponent<Grabbable>();
                if(heldGrabbable && heldGrabbable.GetPrimaryGrabber() ) {
                    brokeJoint = true;
                    OnBreakEvent.Invoke(heldGrabbable.GetPrimaryGrabber());
                }
            }
        }
    }
}

