using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    [System.Serializable]
    public class FingerJoint  {       
        [SerializeField]
        public string TransformName;

        [SerializeField]
        public Vector3 LocalPosition;

        [SerializeField]
        public Quaternion LocalRotation;
    }
}

