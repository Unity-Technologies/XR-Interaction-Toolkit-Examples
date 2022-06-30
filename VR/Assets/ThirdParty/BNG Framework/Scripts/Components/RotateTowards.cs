using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    public class RotateTowards : MonoBehaviour {

        public Transform TargetTransform;

        void Update() {
            transform.rotation = Quaternion.LookRotation(Vector3.RotateTowards(transform.right, TargetTransform.position - transform.position, Time.deltaTime * 1f, 0.0f));
        }
    }
}
