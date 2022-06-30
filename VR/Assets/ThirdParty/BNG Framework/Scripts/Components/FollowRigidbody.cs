using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    public class FollowRigidbody : MonoBehaviour {

        public Transform FollowTransform;
        Rigidbody rigid;

        void Start() {
            rigid = GetComponent<Rigidbody>();
        }

        void FixedUpdate() {
            rigid.MovePosition(FollowTransform.transform.position);
        }
    }
}

