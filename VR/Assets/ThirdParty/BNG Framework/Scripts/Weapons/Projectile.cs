using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace BNG {

    /// <summary>
    /// An object than do damage and play hit FX
    /// </summary>
    public class Projectile : MonoBehaviour {

        public GameObject HitFXPrefab;
        private bool _checkRaycast;
        public float Damage = 25;

        /// <summary>
        /// Add force to rigidbody on impact
        /// </summary>
        public float AddRigidForce = 5;

        public LayerMask ValidLayers;

        public bool StickToObject = false;

        /// <summary>
        /// Minimum Z velocity required to register as an impact
        /// </summary>
        public float MinForceHit = 0.02f;

        [Tooltip("Unity Event called when the projectile damages something")]
        public UnityEvent onDealtDamageEvent;

        private void OnCollisionEnter(Collision collision) {
            OnCollisionEvent(collision);
        }

        public virtual void OnCollisionEvent(Collision collision) {
            // Ignore Triggers
            if (collision.collider.isTrigger) {
                return;
            }

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb && MinForceHit != 0) {
                float zVel = System.Math.Abs(transform.InverseTransformDirection(rb.velocity).z);

                // Minimum Force not achieved
                if (zVel < MinForceHit) {
                    return;
                }
            }

            Vector3 hitPosition = collision.contacts[0].point;
            Vector3 normal = collision.contacts[0].normal;
            Quaternion hitNormal = Quaternion.FromToRotation(Vector3.forward, normal);

            // FX - Particles, Decals, etc.
            DoHitFX(hitPosition, hitNormal, collision.collider);

            // Damage if possible
            Damageable d = collision.collider.GetComponent<Damageable>();
            if (d) {
                d.DealDamage(Damage, hitPosition, normal, true, gameObject, collision.collider.gameObject);

                if (onDealtDamageEvent != null) {
                    onDealtDamageEvent.Invoke();
                }
            }

            if (StickToObject) {
                // tryStickToObject
            }
            else {
                // Done with this projectile
                Destroy(this.gameObject);
            }
        }

        public virtual void DoHitFX(Vector3 pos, Quaternion rot, Collider col) {

            // Create FX at impact point / rotation
            if(HitFXPrefab) {
                GameObject impact = Instantiate(HitFXPrefab, pos, rot) as GameObject;
                BulletHole hole = impact.GetComponent<BulletHole>();
                if (hole) {
                    hole.TryAttachTo(col);
                }
            }

            // push object if rigidbody
            Rigidbody hitRigid = col.attachedRigidbody;
            if (hitRigid != null) {
                hitRigid.AddForceAtPosition(transform.forward * AddRigidForce, pos, ForceMode.VelocityChange);
            }
        }

        /// <summary>
        /// A projectile can be converted into a raycast if time reverts to full speed (or more)
        /// </summary>
        public virtual void MarkAsRaycastBullet() {
            _checkRaycast = true;
            StartCoroutine(CheckForRaycast());
        }
        
        public virtual void DoRayCastProjectile() {

            // Raycast to hit
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, 25f, ValidLayers, QueryTriggerInteraction.Ignore)) {
                Quaternion decalRotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);
                DoHitFX(hit.point, decalRotation, hit.collider);
            }

            _checkRaycast = false;

            // Done with this projectile
            Destroy(this.gameObject);
        }

        IEnumerator CheckForRaycast() {
            while(this.gameObject.activeSelf && _checkRaycast) {
                // Switch to raycast
                if (Time.timeScale >= 1) {
                    DoRayCastProjectile();
                }

                yield return new WaitForEndOfFrame();
            }
        }
    }
}

