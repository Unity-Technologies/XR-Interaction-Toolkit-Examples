using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BNG {

    /// <summary>
    /// An example Grabbable that adds lots of particles and changes audio pitch on collision.
    /// Press X to activate while in hand
    /// </summary>
    public class LaserSword : GrabbableEvents {

        Grabbable grabbable;

        // Enable this when toggled on
        public Transform BladeTransform;
        public Transform RaycastTransform;
        public LayerMask LaserCollision;
        public ParticleSystem CollisionParticle;        

        public bool BladeEnabled = false;

        bool SaberSwitchOn = false;

        public float LaserLength = 1f;
        public float LaserActivateSpeed = 10f;

        public AudioSource CollisionAudio;
        public bool Colliding = false;

        // Start is called before the first frame update
        void Start() {
            grabbable = GetComponent<Grabbable>();

            if(CollisionParticle != null) {
                CollisionParticle.Stop();
            }            
        }

        void Update() {

            // Toggle Saber
            if (grabbable.BeingHeld && input.BButtonDown) {
                SaberSwitchOn = !SaberSwitchOn;
            }

            // Sheath / Unsheath
            if (BladeEnabled || SaberSwitchOn) {
                BladeTransform.localScale = Vector3.Lerp(BladeTransform.localScale, Vector3.one, Time.deltaTime * LaserActivateSpeed);
            }
            else {
                BladeTransform.localScale = Vector3.Lerp(BladeTransform.localScale, new Vector3(1, 0, 1), Time.deltaTime * LaserActivateSpeed);
            }

            BladeTransform.gameObject.SetActive(BladeTransform.localScale.y >= 0.01);

            checkCollision();

            // Raise pitch on collision
            if(Colliding) {
                CollisionAudio.pitch = 2f;
            }
            else {
                CollisionAudio.pitch = 1f;
            }
        }

        public override void OnTrigger(float triggerValue) {

            BladeEnabled = triggerValue > 0.2f;

            base.OnTrigger(triggerValue);
        }

        void checkCollision() {

            Colliding = false;

            if (BladeEnabled == false && !SaberSwitchOn) {
                CollisionParticle.Pause();
                return;
            }

            RaycastHit hit;
            Physics.Raycast(RaycastTransform.position, RaycastTransform.up, out hit, LaserLength, LaserCollision, QueryTriggerInteraction.Ignore);

            if(hit.collider != null) {
                if (CollisionParticle != null) {

                    float distance = Vector3.Distance(hit.point, RaycastTransform.transform.position);
                    float percentage = distance / LaserLength;
                    BladeTransform.localScale = new Vector3(BladeTransform.localScale.x, percentage, BladeTransform.localScale.z);

                    // Allow collision particle to play
                    CollisionParticle.transform.parent.position = hit.point;
                    CollisionParticle.transform.parent.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                    if(!CollisionParticle.isPlaying) {
                        CollisionParticle.Play();
                    }

                    // Haptics
                    input.VibrateController(0.2f, 0.1f, 0.1f, thisGrabber.HandSide);

                    Colliding = true;
                }
            }
            else {
                if (CollisionParticle != null) {
                    CollisionParticle.Pause();
                }
            }
        }

        void OnDrawGizmosSelected() {
            if (RaycastTransform != null) {
                // Draws a blue line from this transform to the target
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(RaycastTransform.position, RaycastTransform.position + RaycastTransform.up * LaserLength);
            }
        }
    }
}

