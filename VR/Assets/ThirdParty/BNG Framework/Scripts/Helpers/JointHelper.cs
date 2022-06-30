using BNG;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    /// <summary>
    /// Lock joints in place to help with physics handling and parent scaling
    /// </summary>
    public class JointHelper : MonoBehaviour {        

        public bool LockXPosition = false;
        public bool LockYPosition = false;
        public bool LockZPosition = false;

        public bool LockXScale = true;
        public bool LockYScale = true;
        public bool LockZScale = true;

        public bool LockXRotation = false;
        public bool LockYRotation = false;
        public bool LockZRotation = false;

        Vector3 initialPosition;
        Vector3 initialRotation;
        Vector3 initialScale;

        Vector3 currentPosition;
        Vector3 currentScale;
        Vector3 currentRotation;

        void Start() {
            initialPosition = transform.localPosition;
            initialRotation = transform.localEulerAngles;
            initialScale = transform.localScale;
        }

        void lockPosition() {
            if (LockXPosition || LockYPosition || LockZPosition) {
                currentPosition = transform.localPosition;
                transform.localPosition = new Vector3(LockXPosition ? initialPosition.x : currentPosition.x, LockYPosition ? initialPosition.y : currentPosition.y, LockZPosition ? initialPosition.z : currentPosition.z);
            }

            if (LockXScale || LockYScale || LockZScale) {
                currentScale = transform.localScale;
                transform.localScale = new Vector3(LockXScale ? initialScale.x : currentScale.x, LockYScale ? initialScale.y : currentScale.y, LockZScale ? initialScale.z : currentScale.z);
            }

            if (LockXRotation || LockYRotation || LockZRotation) {
                currentRotation = transform.localEulerAngles;
                transform.localEulerAngles = new Vector3(LockXRotation ? initialRotation.x : currentRotation.x, LockYRotation ? initialRotation.y : currentRotation.y, LockZRotation ? initialRotation.z : currentRotation.z);
            }
        }

        void LateUpdate() {
            lockPosition();
        }

        void FixedUpdate() {
            lockPosition();
        }
    }
}

