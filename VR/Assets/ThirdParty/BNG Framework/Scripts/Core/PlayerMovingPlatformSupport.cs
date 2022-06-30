using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BNG {
    public class PlayerMovingPlatformSupport : MonoBehaviour {

        [Header("Ground checks : ")]
        [Tooltip("Raycast against these layers to check if player is on a moving platform")]
        public LayerMask GroundedLayers;

        // The object currently below us
        protected RaycastHit groundHit;

        // Use smooth movement if available
        SmoothLocomotion smoothLocomotion;

        // Move characterController with platform if smoothlocomotion is not available
        CharacterController characterController;

        private Transform _initialCharacterParent;

        public float DistanceFromGround;

        /// <summary>
        /// The platform we are currently on top of, if any
        /// </summary>
        public MovingPlatform CurrentPlatform;

        // Were we on the platform last frame
        bool wasOnPlatform;
        bool requiresReparent; // Should we reparent the player after we hop off?

        // Cache last object we raycasted so we can save a lookup
        private GameObject _lastHitObject;

        void Start() {
            smoothLocomotion = GetComponentInChildren<SmoothLocomotion>();
            characterController = GetComponentInChildren<CharacterController>();

            _initialCharacterParent = transform.parent;
        }

        void Update() {
            CheckMovingPlatform();
        }

        void FixedUpdate() {
            UpdateDistanceFromGround();
        }

        public virtual void CheckMovingPlatform() {
            bool onMovingPlatform = false;

            if (groundHit.collider != null && DistanceFromGround < 0.01f) {
                UpdateCurrentPlatform();

                if (CurrentPlatform) {
                    onMovingPlatform = true;

                    // This is another potential method of moving the character instead of parenting it
                    if (CurrentPlatform.MovementMethod == MovingPlatformMethod.PositionDifference && CurrentPlatform != null && CurrentPlatform.PositionDelta != Vector3.zero) {
                        if (smoothLocomotion) {
                            if(smoothLocomotion.ControllerType == PlayerControllerType.Rigidbody) {
                                //smoothLocomotion.GetComponent<Rigidbody>().velocity = CurrentPlatform.GetComponent<Rigidbody>().velocity;
                            }
                            else {
                                smoothLocomotion.MoveCharacter(CurrentPlatform.PositionDelta);
                            }
                            
                        }
                        else if (characterController) {
                            characterController.Move(CurrentPlatform.PositionDelta);
                        }
                    }

                    // For now we can parent the characterController object to move it along. Rigidbodies may want to change friction materials or alter the player's velocity
                    if (CurrentPlatform.MovementMethod == MovingPlatformMethod.ParentToPlatform) {
                        if(characterController != null) {
                            if (onMovingPlatform) {
                                characterController.transform.parent = groundHit.collider.transform;
                                requiresReparent = true;
                            }
                        }
                        else if (smoothLocomotion != null && smoothLocomotion.ControllerType == PlayerControllerType.Rigidbody) {
                            if (onMovingPlatform) {
                                transform.parent = groundHit.collider.transform;
                                requiresReparent = true;
                            }
                        }
                    }
                }
            }
            else {
                // Reset our platform if no longer on one
                if(CurrentPlatform != null) {
                    CurrentPlatform = null;
                }
            }

            // Check if we need to reparent the character after hopping off a platform
            if(!onMovingPlatform && wasOnPlatform && requiresReparent) {
                if(characterController) {
                    characterController.transform.parent = _initialCharacterParent;
                }
                else {
                    transform.parent = _initialCharacterParent;
                }
            }

            wasOnPlatform = onMovingPlatform;
        }

        public virtual void UpdateCurrentPlatform() {
            
            // Only update the last platform if our last collider has changed
            if(_lastHitObject != groundHit.collider.gameObject) {

                _lastHitObject = groundHit.collider.gameObject;

                CurrentPlatform = _lastHitObject.GetComponent<MovingPlatform>();
            }
        }

        public virtual void UpdateDistanceFromGround() {

            if (characterController) {
                if (Physics.Raycast(characterController.transform.position, -characterController.transform.up, out groundHit, 20, GroundedLayers, QueryTriggerInteraction.Ignore)) {
                    DistanceFromGround = Vector3.Distance(characterController.transform.position, groundHit.point);
                    DistanceFromGround += characterController.center.y;
                    DistanceFromGround -= (characterController.height * 0.5f) + characterController.skinWidth;

                    // Round to nearest thousandth
                    DistanceFromGround = (float)Math.Round(DistanceFromGround * 1000f) / 1000f;
                }
                else {
                    DistanceFromGround = 9999f;
                }
            }
            // No CharacterController found. Update Distance based on current transform position
            else {
                if (Physics.Raycast(transform.position, -transform.up, out groundHit, 20, GroundedLayers, QueryTriggerInteraction.Ignore)) {
                    DistanceFromGround = Vector3.Distance(transform.position, groundHit.point);
                    // Round to nearest thousandth
                    DistanceFromGround = (float)Math.Round(DistanceFromGround * 1000f) / 1000f;
                }
                else {
                    DistanceFromGround = 9999f;
                }
            }
        }
    }
}

