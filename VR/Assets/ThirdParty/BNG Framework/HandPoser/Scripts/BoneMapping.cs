using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

    /// <summary>
    /// This class can be used to map finger rotations from one model to another
    /// </summary>
    public class BoneMapping : MonoBehaviour {
        
        [Range(0f, 1f)] public float weight = 1f;

        [System.Serializable]
        public enum Mode {
            FromToRotation,
            MatchRotation
        }

        public BoneObject[] Fingers;

        [Header("Shown for Debug : ")]
        public bool ShowGizmos = true;

        void Update() {
            if (weight <= 0f) {
                return;
            }
            
            for (int x = 0; x < Fingers.Length; x++) {
                BoneObject finger = Fingers[x];

                if (finger == null) {
                    continue;
                }

                for (int i = 0; i < finger.destinationBones.Length - 1; i++) {
                    // Get the relative rotation from the current rotation to the target rotation
                    Quaternion f = Quaternion.Inverse(finger.destinationBones[i].rotation) * finger.targetBones[i].rotation;

                    // Weight blending
                    if (weight < 1f) {
                        f = Quaternion.Slerp(Quaternion.identity, f, weight);
                    }

                    // Append relative rotation
                    finger.destinationBones[i].rotation *= f;

                }
            }
        }

        void OnDrawGizmos() {
            if (!ShowGizmos || Fingers == null) {
                return;
            }

            for (int x = 0; x < Fingers.Length; x++) {
                BoneObject finger = Fingers[x];

                for (int i = 0; i < finger.targetBones.Length; i++) {

                    if(finger.targetBones[i] == null) {
                        continue;
                    }

                    // Target Bones
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(finger.targetBones[i].position, 0.003f);

                    if (i < finger.targetBones.Length - 1) {
                        if(finger.targetBones[i] == null || finger.targetBones[i + 1] == null) {
                            continue;
                        }
                        Gizmos.DrawLine(finger.targetBones[i].position, finger.targetBones[i + 1].position);
                    }
                }

                for (int i = 0; i < finger.destinationBones.Length; i++) {

                    // Reference may have been removed from inspector
                    if (finger.destinationBones[i] == null) {
                        continue;
                    }

                    // Avatar Bones
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(finger.destinationBones[i].position, 0.003f);

                    if (i < finger.destinationBones.Length - 1) {
                        if (finger.destinationBones[i] == null || finger.destinationBones[i + 1] == null) {
                            continue;
                        }
                        Gizmos.DrawLine(finger.destinationBones[i].position, finger.destinationBones[i + 1].position);
                    }
                }
            }
        }
    }

    [System.Serializable]
    public class BoneObject {
        
        public Transform[] targetBones = new Transform[0];
        public Transform[] destinationBones = new Transform[0];

        public Vector3 avatarForwardAxis = Vector3.forward;
        public Vector3 avatarUpAxis = Vector3.up;
        public Vector3 targetForwardAxis = Vector3.forward;
        public Vector3 targetUpAxis = Vector3.up;
    }
}

