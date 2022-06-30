using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    [System.Serializable]
    public class HandPoseDefinition {
        [SerializeField]
        [Header("Wrist")]
        public FingerJoint WristJoint;

        [SerializeField]
        [Header("Thumb")]
        public List<FingerJoint> ThumbJoints;

        [SerializeField]
        [Header("Index")]
        public List<FingerJoint> IndexJoints;

        [SerializeField]
        [Header("Middle")]
        public List<FingerJoint> MiddleJoints;

        [SerializeField]
        [Header("Ring")]
        public List<FingerJoint> RingJoints;

        [SerializeField]
        [Header("Pinky")]
        public List<FingerJoint> PinkyJoints;

        [SerializeField]
        [Header("Other")]
        public List<FingerJoint> OtherJoints;
    }
}

