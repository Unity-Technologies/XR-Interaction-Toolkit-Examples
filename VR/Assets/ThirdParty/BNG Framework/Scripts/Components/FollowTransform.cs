using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    public class FollowTransform : MonoBehaviour {

        public Transform FollowTarget;
        public bool MatchRotation = true;

        void Update() {
            if(FollowTarget) {
                transform.position = FollowTarget.position;

                if(MatchRotation) {
                    transform.rotation = FollowTarget.rotation;
                }
            }
        }
    }
}