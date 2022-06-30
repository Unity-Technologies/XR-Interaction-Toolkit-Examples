using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    public class PlaySoundOnGrab : GrabbableEvents {

        public AudioClip SoundToPlay;

        public override void OnGrab(Grabber grabber) {

            // Play Sound
            if(SoundToPlay) {
                VRUtils.Instance.PlaySpatialClipAt(SoundToPlay, transform.position, 1f, 1f);
            }

            base.OnGrab(grabber);
        }
    }
}