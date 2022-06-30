using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

    // This will rotate a transform along with a users headset. Useful for keeping an object aligned with the camera, independent of the player capsule collider.
    public class RotateWithHMD : MonoBehaviour {

        [Tooltip("The Transform to rotate along with")]
        public Transform FollowTransform;

        /// <summary>
        /// The Character Capsule to  rotate along with
        /// </summary>
        [Tooltip("The Character Capsule to  rotate along with")]
        public CharacterController Character;

        /// <summary>
        /// Offset to apply in local space to the hmdTransform
        /// </summary>
        public Vector3 Offset = new Vector3(0, -0.25f, 0);

        public float RotateSpeed = 5f;

        public float MovementSmoothing = 0;

        private Vector3 velocity = Vector3.zero;

        [Tooltip("If true this transform will be parented to the characterController. Set this to true if you want the position and rotation to align with the character controller without delay.")]
        public bool ParentToCharacter = false;

        Transform originalParent;

        /// <summary>
        /// This object will be used as a reference to follow
        /// </summary>
        Transform followTransform;

        Transform camTransform;

        void Start() {
            originalParent = transform.parent;
            followTransform = new GameObject().transform;
            followTransform.name = "RotateReferenceObject";
            followTransform.position = transform.position;
            followTransform.rotation = transform.rotation;

            // Parent the object to our character and let the hierarchy take care of positioning
            if (ParentToCharacter) {
                transform.parent = Character.transform;
            }

            // Set our reference transform to the Character object if it is available
            if(FollowTransform) {
                followTransform.parent = FollowTransform;
            }
            else if (Character) {
                followTransform.parent = Character.transform;
            }
            else {
                followTransform.parent = originalParent;
            }
        }

        void LateUpdate() {
            UpdatePosition();
        }
        void UpdatePosition() {

            // Find Main Camera Object if it changed or not yet been fou nd
            // Use the transform with the "MainCamera" tag, instead of Camera.main, as the Camera component could be disabled when using dual eye cameras.
            if (camTransform == null && GameObject.FindGameObjectWithTag("MainCamera") != null) {
                camTransform = GameObject.FindGameObjectWithTag("MainCamera").transform;
                followTransform.position = camTransform.position;
                followTransform.localEulerAngles = Vector3.zero;
            }

            // No main camera available
            if (camTransform == null) {
                return;
            }

            // Offset from Character's body if available
            Vector3 worldOffset = Vector3.zero;
            if(FollowTransform) {
                worldOffset = FollowTransform.position - FollowTransform.TransformVector(Offset);
            }
            else if (Character) {
                worldOffset = Character.transform.position - Character.transform.TransformVector(Offset);
            } 

            Vector3 moveToPosition = new Vector3(worldOffset.x, camTransform.position.y - Offset.y, worldOffset.z);
            transform.position = Vector3.SmoothDamp(transform.position, moveToPosition, ref velocity, MovementSmoothing);
            transform.rotation = Quaternion.Lerp(transform.rotation, followTransform.rotation, Time.deltaTime * RotateSpeed);
        }
    }
}
