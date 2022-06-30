using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BNG {


    /// <summary>
    /// Like a jetpack, but for your hands.
    /// </summary>
    public class HandJet : GrabbableEvents {

        [Tooltip("Movement Speed to apply if using a CharacterController, or Force to apply if using a Rigidbody controller.")]
        public float JetForce = 10f;

        [Tooltip("Enabled while jetting")]
        public ParticleSystem JetFX;
        
        [Tooltip("If true the player will float in the air when not jetting. (Works for Rigidbody player only)")]
        public bool DisableGravityWhileHeld = true;

        CharacterController characterController;
        SmoothLocomotion smoothLocomotion;
        PlayerGravity playerGravity;
        Rigidbody playerRigid;

        AudioSource audioSource;

        void Start() {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if(player) {
                characterController = player.GetComponentInChildren<CharacterController>();
                playerGravity = player.GetComponentInChildren<PlayerGravity>();
                smoothLocomotion = player.GetComponentInChildren<SmoothLocomotion>();
                playerRigid = player.GetComponent<Rigidbody>();
            }
            else {
                Debug.Log("No player object found.");
            }

            audioSource = GetComponent<AudioSource>();
        }

        public override void OnTrigger(float triggerValue) {

            if(triggerValue > 0.25f) {
                doJet(triggerValue);
            }
            else {
                stopJet();
            }

            base.OnTrigger(triggerValue);
        }

        // Apply force to player in Fixed Update
        public void FixedUpdate() {
            if(grab != null && grab.BeingHeld && addRigidForce != null && addRigidForce != Vector3.zero) {
                if(playerRigid) {
                    playerRigid.AddForce(addRigidForce, ForceMode.Force);
                }
            }
        }

        public override void OnGrab(Grabber grabber) {
            // disable gravity
            if(DisableGravityWhileHeld) {
                ChangeGravity(false);
            }
            
        }

        public void ChangeGravity(bool gravityOn) {
            if(playerGravity) {
                playerGravity.ToggleGravity(gravityOn);
            }
        }

        public override void OnRelease() {
            stopJet();

            // re-enforce gravity
            if (DisableGravityWhileHeld) {
                ChangeGravity(true);
            }
        }

        Vector3 moveDirection;
        Vector3 addRigidForce;

        void doJet(float triggerValue) {
            moveDirection = transform.forward * JetForce;

            // Use smooth loco method if available
            if (smoothLocomotion) {
                if(smoothLocomotion.ControllerType == PlayerControllerType.CharacterController) {
                    smoothLocomotion.MoveCharacter(moveDirection * Time.deltaTime * triggerValue);
                }
                else if (smoothLocomotion.ControllerType == PlayerControllerType.Rigidbody) {
                    //smoothLocomotion.MoveRigidPlayer(moveDirection * triggerValue);
                    
                    // Handle this in LFixedUpdate
                    // Rigidbody rb = smoothLocomotion.GetComponent<Rigidbody>();

                    // moveDirection += new Vector3(0, smoothLocomotion.RigidBodyGravity, 0);

                    addRigidForce = moveDirection * triggerValue;
                    // rb.AddRelativeForce(moveDirection * triggerValue, fm);

                    // smoothLocomotion.MoveCharacter(moveDirection * Time.deltaTime * triggerValue);
                }

            }
            // Fall back to character controller
            else if (characterController) {
                characterController.Move(moveDirection * Time.deltaTime * triggerValue);
            }

            // Gravity is always off while jetting
            ChangeGravity(false);            

            // Sound
            if (!audioSource.isPlaying) {
                audioSource.pitch = Time.timeScale;
                audioSource.Play();
            }

            // Particle FX
            if(JetFX != null && !JetFX.isPlaying) {
                JetFX.Play();
            }

            //Haptics
            if(input && thisGrabber != null) {
                input.VibrateController(0.1f, 0.5f, 0.2f, thisGrabber.HandSide);
            }
        }

        void stopJet() {

            if (audioSource.isPlaying) {
                audioSource.Stop();
            }

            if (JetFX != null && JetFX.isPlaying) {
                JetFX.Stop();
            }

            if (DisableGravityWhileHeld == false) {                
                ChangeGravity(true);
            }

            addRigidForce = Vector3.zero;
        }

        public override void OnTriggerUp() {
            stopJet();
            base.OnTriggerUp();
        }
    }
}

