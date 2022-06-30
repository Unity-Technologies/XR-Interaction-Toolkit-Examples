using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BNG {
    public class AmmoDisplay : MonoBehaviour {

        public RaycastWeapon Weapon;
        public Text AmmoLabel;

        void OnGUI() {
            string loadedShot = Weapon.BulletInChamber ? "1" : "0";
            AmmoLabel.text = loadedShot + " / " + Weapon.GetBulletCount();
        }
    }
}

