using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    public class DetachableLimb : MonoBehaviour {

        public Transform ShrinkBone;

        public GameObject ReplaceGrabbableWith;

        public GameObject EnableOnDetach;

        public void DoDismemberment(Grabber grabbedBy) {

            if (ReplaceGrabbableWith && grabbedBy != null) {
                if (grabbedBy.HeldGrabbable) {
                    grabbedBy.HeldGrabbable.DropItem(grabbedBy, true, true);
                }

                if (ReplaceGrabbableWith) {
                    ReplaceGrabbableWith.SetActive(true);
                    Grabbable g = ReplaceGrabbableWith.GetComponent<Grabbable>();
                    g.transform.parent = null;
                    g.transform.localScale = Vector3.one;
                    g.UpdateOriginalParent();

                    grabbedBy.GrabGrabbable(g);
                }
            }

            if (ShrinkBone) {
                ShrinkBone.localScale = Vector3.zero;
                ShrinkBone.gameObject.SetActive(false);
            }

            if (EnableOnDetach) {
                EnableOnDetach.SetActive(true);
            }
        }

        public void ReverseDismemberment() {
            if (ShrinkBone) {
                ShrinkBone.gameObject.SetActive(true);
                ShrinkBone.localScale = Vector3.one;
            }

            if (EnableOnDetach) {
                EnableOnDetach.SetActive(false);
            }
        }
    }
}