using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

    /// <summary>
    /// Events that will highlight an object if it is a valid Grabbable
    /// </summary>
    public class GrabbableHighlight : GrabbableEvents {

        public bool HighlightOnGrabbable = true;
        public bool HighlightOnRemoteGrabbable = true;

        // Item has been grabbed by a Grabber
        public override void OnGrab(Grabber grabber) {
            UnhighlightItem();
        }

        // Fires if this is the closest grabbable but wasn't in the previous frame
        public override void OnBecomesClosestGrabbable(ControllerHand touchingHand) {
            if (HighlightOnGrabbable) {
                HighlightItem();
            }
        }

        public override void OnNoLongerClosestGrabbable(ControllerHand touchingHand) {
            if (HighlightOnGrabbable) {
                UnhighlightItem();
            }
        }

        public override void OnBecomesClosestRemoteGrabbable(ControllerHand touchingHand) {
            if (HighlightOnRemoteGrabbable) {
                HighlightItem();
            }
        }

        public override void OnNoLongerClosestRemoteGrabbable(ControllerHand touchingHand) {
            if (HighlightOnRemoteGrabbable) {
                UnhighlightItem();
            }
        }
        public void HighlightItem() {
            // Enable your highlight here
        }

        public void UnhighlightItem() {
            // Disable your highlight here
        }
    }
}

