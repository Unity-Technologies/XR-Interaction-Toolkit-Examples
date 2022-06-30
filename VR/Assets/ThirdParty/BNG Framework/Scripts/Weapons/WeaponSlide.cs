using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

    /// <summary>
    /// Weapon slide on a pistol. Charges weapon and ejects casings.
    /// </summary>
    public class WeaponSlide : MonoBehaviour {

        /// <summary>
        /// Minimum distance slide will travel on Z axis
        /// </summary>
        public float MinLocalZ = -0.03f;

        /// <summary>
        /// Max distance slide will travel on Z axis
        /// </summary>
        public float MaxLocalZ = 0;

        // Keep track of which way we are sliding
        bool slidingBack = true;

        /// <summary>
        /// Is the Slide locked back due to last shot
        /// </summary>
        public bool LockedBack = false;

        /// <summary>
        /// Sound to play when slide is released back into position
        /// </summary>
        public AudioClip SlideReleaseSound;

        /// <summary>
        /// Sound to play after last shot has fired and slide is forced back
        /// </summary>
        public AudioClip LockedBackSound;

        /// <summary>
        /// When true, the slide will be set to 0 mass when not being held. This fixes jitter caused by the slide having a configurable joint attached to the weapon
        /// </summary>
        public bool ZeroMassWhenNotHeld = true;

        RaycastWeapon parentWeapon;
        Grabbable parentGrabbable;
        Vector3 initialLocalPos;
        Grabbable thisGrabbable;
        AudioSource audioSource;
        Rigidbody rigid;
        float initialMass;

        /// <summary>
        /// Lock the slide position in place
        /// </summary>
        Vector3 _lockPosition;
        /// <summary>
        /// If true then the slides position is locked in Update and cannot be moved
        /// </summary>
        bool lockSlidePosition;

        void Start() {
            initialLocalPos = transform.localPosition;
            audioSource = GetComponent<AudioSource>();
            parentWeapon = transform.parent.GetComponent<RaycastWeapon>();
            parentGrabbable = transform.parent.GetComponent<Grabbable>();
            thisGrabbable = GetComponent<Grabbable>();
            rigid = GetComponent<Rigidbody>();
            initialMass = rigid.mass;

            if (parentWeapon != null) {
                Physics.IgnoreCollision(GetComponent<Collider>(), parentWeapon.GetComponent<Collider>());
            }
        }

        public virtual void OnEnable() {
            // Lock the slide in place when teleporting or snap turning
            PlayerTeleport.OnBeforeTeleport += LockSlidePosition;
            PlayerTeleport.OnAfterTeleport += UnlockSlidePosition;

            //PlayerRotation.OnBeforeRotate += LockSlidePosition;
            //PlayerRotation.OnAfterRotate += UnlockSlidePosition;
        }

        public virtual void OnDisable() {
            PlayerTeleport.OnBeforeTeleport -= LockSlidePosition;
            PlayerTeleport.OnAfterTeleport -= UnlockSlidePosition;

            //PlayerRotation.OnBeforeRotate += LockSlidePosition;
            //PlayerRotation.OnAfterRotate += UnlockSlidePosition;
        }        

        // Update is called once per frame
        void Update() {

            // If our slide is currently locked just set it and return early
            if(lockSlidePosition) {
                transform.localPosition = _lockPosition;
                return;
            }

            float localZ = transform.localPosition.z;

            if (LockedBack) {
                transform.localPosition = new Vector3(initialLocalPos.x, initialLocalPos.y, MinLocalZ);

                // Not locking back if hand is holding this
                if (thisGrabbable != null && thisGrabbable.BeingHeld) {
                    UnlockBack();
                }
            }

            if (!LockedBack) {
                // Clamp values
                if (localZ <= MinLocalZ) {
                    transform.localPosition = new Vector3(initialLocalPos.x, initialLocalPos.y, MinLocalZ);
                    if (slidingBack) {
                        onSlideBack();
                    }
                }
                else if (localZ >= MaxLocalZ) {
                    transform.localPosition = new Vector3(initialLocalPos.x, initialLocalPos.y, MaxLocalZ);

                    // Moving forward
                    if (!slidingBack) {
                        onSlideForward();
                    }
                }
            }
        }

        void FixedUpdate() {
            // Change mass of slider rigidbody. This prevents stuttering when the object is not held and the slide is back
            if (ZeroMassWhenNotHeld && parentGrabbable.BeingHeld && rigid) {
                rigid.mass = initialMass;
            }
            else if (ZeroMassWhenNotHeld && rigid) {
                // Set mass to very low to prevent stuttering when not held
                rigid.mass = 0.0001f;
            }           
        }

        public virtual void LockBack() {

            if (!LockedBack) {
                if (thisGrabbable.BeingHeld || parentGrabbable.BeingHeld) {
                    VRUtils.Instance.PlaySpatialClipAt(LockedBackSound, transform.position, 1f, 0.8f);
                }

                LockedBack = true;
            }
        }

        public virtual void UnlockBack() {

            if (LockedBack) {
                if (thisGrabbable.BeingHeld || parentGrabbable.BeingHeld) {
                    VRUtils.Instance.PlaySpatialClipAt(SlideReleaseSound, transform.position, 1f, 0.9f);
                }

                LockedBack = false;

                // This is considered a charge
                if (parentWeapon != null) {
                    parentWeapon.OnWeaponCharged(false);
                }
            }
        }

        void onSlideBack() {

            if (thisGrabbable.BeingHeld || parentGrabbable.BeingHeld) {
                playSoundInterval(0, 0.2f, 0.9f);
            }

            if (parentWeapon != null) {
                parentWeapon.OnWeaponCharged(true);
            }

            slidingBack = false;
        }

        void onSlideForward() {

            if (thisGrabbable.BeingHeld || parentGrabbable.BeingHeld) {
                playSoundInterval(0.2f, 0.35f, 1f);
            }

            slidingBack = true;
        }

        public virtual void LockSlidePosition() {
            // Lock the slide position if we aren't holding the object
            if (parentGrabbable.BeingHeld && !thisGrabbable.BeingHeld && !lockSlidePosition) {
                _lockPosition = transform.localPosition;
                lockSlidePosition = true;
            }
        }

        public virtual void UnlockSlidePosition() {
            if (lockSlidePosition) {
                StartCoroutine(UnlockSlideRoutine());
            }
        }

        public IEnumerator UnlockSlideRoutine() {
            yield return new WaitForSeconds(0.2f);
            lockSlidePosition = false;
        }

        void playSoundInterval(float fromSeconds, float toSeconds, float volume) {
            if (audioSource) {

                if (audioSource.isPlaying) {
                    audioSource.Stop();
                }

                audioSource.pitch = Time.timeScale;
                audioSource.time = fromSeconds;
                audioSource.volume = volume;
                audioSource.Play();
                audioSource.SetScheduledEndTime(AudioSettings.dspTime + (toSeconds - fromSeconds));
            }
        }
    }
}
