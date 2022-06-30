using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BNG {

    [CustomEditor(typeof(GrabPointTrigger))]
    [CanEditMultipleObjects]
    public class GrabPointTriggerEditor : Editor {

        GrabPointTrigger grabTrigger;

        public override void OnInspectorGUI() {

            base.OnInspectorGUI();

            grabTrigger = (GrabPointTrigger)target;

            if (grabTrigger.GrabObject != null && GUILayout.Button("Populate Grab Points from " + grabTrigger.GrabObject.transform.name)) {
                AutoPopulateGrabPoints();
            }
        }

        public void AutoPopulateGrabPoints() {
            if (grabTrigger.GrabObject) {
                var newPoints = grabTrigger.GrabObject.GetComponentsInChildren<GrabPoint>().ToList();

                if(newPoints != null) {
                    grabTrigger.GrabPoints = newPoints;
                }
            }
        }
    }
}

