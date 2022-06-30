using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

    public class DrawerSound : MonoBehaviour {
        
        public AudioClip DrawerOpenSound;
        public float DrawerOpenValue = 80f;

        public AudioClip DrawerCloseSound;
        public float DrawerCloseValue = 20f;

        bool playedOpenSound = false;
        bool playedCloseSound = false;

        public void OnDrawerUpdate(float drawerValue) {
            
            // Open Sound
            if(drawerValue < DrawerOpenValue && !playedOpenSound && DrawerOpenSound != null) {
                VRUtils.Instance.PlaySpatialClipAt(DrawerOpenSound, transform.position, 1f);
                playedOpenSound = true;
            }
            // Reset Open Sound
            if(drawerValue > DrawerOpenValue) {
                playedOpenSound = false;
            }

            // Close Sound
            if (drawerValue > DrawerCloseValue && !playedCloseSound && DrawerCloseSound != null) {
                VRUtils.Instance.PlaySpatialClipAt(DrawerCloseSound, transform.position, 1f);
                playedCloseSound = true;
            }

            // Reset Close Sound
            if (drawerValue < DrawerCloseValue) {
                playedCloseSound = false;
            }
        }
    }
}

