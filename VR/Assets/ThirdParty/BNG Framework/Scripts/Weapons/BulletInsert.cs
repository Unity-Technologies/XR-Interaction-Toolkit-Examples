using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    public class BulletInsert : MonoBehaviour {

        /// <summary>
        /// The weapon we will be adding Bullets to
        /// </summary>
        public RaycastWeapon Weapon;

        /// <summary>
        /// Only transforms that contains this name will be accepted as bullets
        /// </summary>
        public string AcceptBulletName = "Bullet";

        public AudioClip InsertSound;

        void OnTriggerEnter(Collider other) {

            Grabbable grab = other.GetComponent<Grabbable>();
            if (grab != null) {
                if(grab.transform.name.Contains(AcceptBulletName)) {

                    // Weapon is full
                    if(Weapon.GetBulletCount() >= Weapon.MaxInternalAmmo) {
                        return;
                    }

                    // Drop the bullet and add ammo to gun
                    grab.DropItem(false, true);
                    grab.transform.parent = null;
                    GameObject.Destroy(grab.gameObject);

                    // Up Ammo Count
                    GameObject b = new GameObject();
                    b.AddComponent<Bullet>();
                    b.transform.parent = Weapon.transform;

                    // Play Sound
                    if(InsertSound) {
                        VRUtils.Instance.PlaySpatialClipAt(InsertSound, transform.position, 1f, 0.5f);
                    }
                }
            }
        }

    }

}

