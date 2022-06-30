using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {


    /// <summary>
    /// This is an example of how to spawn ammo depending on the weapon that is equipped in the opposite hand
    /// </summary>
    public class AmmoDispenser : MonoBehaviour {

        /// <summary>
        /// Used to determine if holding a weapon
        /// </summary>
        public Grabber LeftGrabber;

        /// <summary>
        /// Used to determine if holding a weapon
        /// </summary>
        public Grabber RightGrabber;

        /// <summary>
        /// Disable this if weapon not equipped
        /// </summary>
        public GameObject AmmoDispenserObject;

        /// <summary>
        /// Instantiate this if pistol equipped
        /// </summary>
        public GameObject PistolClip;

        /// <summary>
        /// Instantiate this if shotgun equipped
        /// </summary>
        public GameObject ShotgunShell;

        /// <summary>
        /// Instantiate this if shotgun equipped
        /// </summary>
        public GameObject RifleClip;

        /// <summary>
        /// Amount of Pistol Clips currently available
        /// </summary>
        public int CurrentPistolClips = 5;

        public int CurrentRifleClips = 5;

        public int CurrentShotgunShells = 30;

        // Update is called once per frame
        void Update() {
            bool weaponEquipped = false;

            if (grabberHasWeapon(LeftGrabber) || grabberHasWeapon(RightGrabber)) {
                weaponEquipped = true;
            }

            // Only show if we have something equipped
            if(AmmoDispenserObject.activeSelf != weaponEquipped) {
                AmmoDispenserObject.SetActive(weaponEquipped);
            }
        }

        bool grabberHasWeapon(Grabber g) {

            if(g == null || g.HeldGrabbable == null) {
                return false;
            }

            // Holding shotgun, pistol, or rifle
            string grabName = g.HeldGrabbable.transform.name;
            if (grabName.Contains("Shotgun") || grabName.Contains("Pistol") || grabName.Contains("Rifle")) {
                return true;
            }

            return false;
        }

        public GameObject GetAmmo() {

            bool leftGrabberValid = LeftGrabber != null && LeftGrabber.HeldGrabbable != null;
            bool rightGrabberValid = RightGrabber != null && RightGrabber.HeldGrabbable != null;

            // Shotgun
            if (leftGrabberValid && LeftGrabber.HeldGrabbable.transform.name.Contains("Shotgun") && CurrentShotgunShells > 0) {
                CurrentShotgunShells--;
                return ShotgunShell;
            }
            else if (rightGrabberValid && RightGrabber.HeldGrabbable.transform.name.Contains("Shotgun") && CurrentShotgunShells > 0) {
                CurrentShotgunShells--;
                return ShotgunShell;
            }

            // Rifle
            if (leftGrabberValid && LeftGrabber.HeldGrabbable.transform.name.Contains("Rifle") && CurrentRifleClips > 0) {
                CurrentRifleClips--;
                return RifleClip;
            }
            else if (rightGrabberValid && RightGrabber.HeldGrabbable.transform.name.Contains("Rifle") && CurrentRifleClips > 0) {
                CurrentRifleClips--;
                return RifleClip;
            }

            // Pistol
            if (leftGrabberValid && LeftGrabber.HeldGrabbable.transform.name.Contains("Pistol") && CurrentPistolClips > 0) {
                CurrentPistolClips--;
                return PistolClip;
            }
            else if (rightGrabberValid && RightGrabber.HeldGrabbable.transform.name.Contains("Pistol") && CurrentPistolClips > 0) {
                CurrentPistolClips--;
                return PistolClip;
            }

            // Default to nothing
            return null;
        }

        public void GrabAmmo(Grabber grabber) {

            if(GetAmmo() != null) {
                GameObject ammo = Instantiate(GetAmmo(), grabber.transform.position, grabber.transform.rotation) as GameObject;
                Grabbable g = ammo.GetComponent<Grabbable>();

                // Disable rings for performance
                GrabbableRingHelper grh = ammo.GetComponentInChildren<GrabbableRingHelper>();
                if (grh) {
                    Destroy(grh);
                    RingHelper r = ammo.GetComponentInChildren<RingHelper>();
                    Destroy(r.gameObject);
                }


                // Offset to hand
                ammo.transform.parent = grabber.transform;
                ammo.transform.localPosition = -g.GrabPositionOffset;
                ammo.transform.parent = null;

                grabber.GrabGrabbable(g);
            }
        }

        public virtual void AddAmmo(string AmmoName) {
            if(AmmoName.Contains("Shotgun")) {
                CurrentShotgunShells++;
            }
            else if (AmmoName.Contains("Rifle")) {
                CurrentRifleClips--;
            }
            else if (AmmoName.Contains("Pistol")) {
                CurrentPistolClips++;
            }
        }
    }
}

