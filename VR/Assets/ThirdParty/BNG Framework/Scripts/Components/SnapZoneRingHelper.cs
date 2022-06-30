using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BNG {
    public class SnapZoneRingHelper : MonoBehaviour {

        /// <summary>
        /// The Snap Zone to respond to. Change size and color of ring if there is a valid grabbable within
        /// </summary>
        public SnapZone Snap;

        public Color RestingColor = Color.gray;
        public Color ValidSnapColor = Color.white;
       

        /// <summary>
        /// Scale in Dynamic Pixels Per Unit
        /// </summary>
        public float RestingScale = 1000f;

        /// <summary>
        /// Scale in Dynamic Pixels Per Unit
        /// </summary>
        public float ValidSnapScale = 800f;

        CanvasScaler ringCanvas;
        Text ringText;
        GrabbablesInTrigger nearbyGrabbables;

        bool validSnap = false;

        public float ScaleSpeed = 50f;

        void Start() {
            ringCanvas = GetComponent<CanvasScaler>();
            ringText = GetComponent<Text>();
            nearbyGrabbables = Snap.GetComponent<GrabbablesInTrigger>();
        }

        // Update is called once per frame
        void Update() {

            validSnap = checkIsValidSnap();

            // Scale
            float lerpTo = validSnap ? ValidSnapScale : RestingScale;
            ringCanvas.dynamicPixelsPerUnit = Mathf.Lerp(ringCanvas.dynamicPixelsPerUnit, lerpTo, Time.deltaTime * ScaleSpeed);

            // Color
            ringText.color = validSnap ? ValidSnapColor : RestingColor;
        }

        bool checkIsValidSnap() {
            if(nearbyGrabbables != null) {

                // Invalid if we are already  holding something
                if(Snap.HeldItem != null) {
                    return false;
                }

                // Can snap if there is a held object inside our trigger
                if (Snap.ClosestGrabbable != null) {
                    return true;
                }
            }

            return false;
        }
    }
}

