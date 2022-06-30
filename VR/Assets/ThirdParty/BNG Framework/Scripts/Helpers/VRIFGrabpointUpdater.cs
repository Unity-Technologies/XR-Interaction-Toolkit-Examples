using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

    /// <summary>
    /// This component is meant to help you autmatically update your grab points, as the default hand model positions have changed since VRIF v1.7
    /// </summary>
    public class VRIFGrabpointUpdater : MonoBehaviour {

        [Header("Right Hand Model")]
        [Tooltip("This is the local position of the hand model that was defined in your previous xr rig, as well as what was used in the previewer.")]
        public Vector3 PriorModelOffsetRightPosition = new Vector3(-0.024f, 0.051f, 0.001f);

        [Tooltip("This is the local position of the NEW hand model that is currently defined in your xr rig, as well as what is used in the previewer.")]
        public Vector3 NewModelOffsetRightPosition   = new Vector3(-0.006f, 0, -0.04f);

        [Tooltip("This is the local rotation of the hand model that was defined in your previous xr rig, as well as what was used in the previewer.")]
        public Vector3 PriorModelOffsetRightRotation = new Vector3(-12.041f, 13f, -90f);

        [Tooltip("This is the local rotation of the NEW hand model that is currently defined in your xr rig, as well as what is used in the previewer.")]
        public Vector3 NewModelOffsetRightRotation   = new Vector3(-6, 0.43f, -90f);

        [Header("Left Hand Model")]
        [Tooltip("This is the local position of the hand model that was defined in your previous xr rig, as well as what was used in the previewer.")]
        public Vector3 PriorModelOffsetLeftPosition = new Vector3(0.024f, 0.051f, 0.001f);

        [Tooltip("This is the local position of the NEW hand model that is currently defined in your xr rig, as well as what is used in the previewer.")]
        public Vector3 NewModelOffsetLeftPosition = new Vector3(0.006f, 0, -0.04f);

        [Tooltip("This is the local rotation of the hand model that was defined in your previous xr rig, as well as what was used in the previewer.")]
        public Vector3 PriorModelOffsetLeftRotation = new Vector3(-12.041f, -13f, 90f);

        [Tooltip("This is the local rotation of the NEW hand model that is currently defined in your xr rig, as well as what is used in the previewer.")]
        public Vector3 NewModelOffsetLeftRotation = new Vector3(-6, -0.43f, 90);

        void Start() {
            ApplyGrabPointUpdate();
        }

        public void ApplyGrabPointUpdate() {
            GrabPoint[] points = GetComponentsInChildren<GrabPoint>();

            foreach(var gp in points) {

                // Both Hands - use Right Offset for both
                if (gp.RightHandIsValid && gp.LeftHandIsValid) {
                    gp.transform.localPosition = gp.transform.localPosition + (PriorModelOffsetRightPosition - NewModelOffsetRightPosition);
                    gp.transform.localRotation *= Quaternion.Euler(PriorModelOffsetRightRotation) * Quaternion.Inverse(Quaternion.Euler(NewModelOffsetRightRotation));
                }
                // Right Hand only
                else if (gp.RightHandIsValid) {
                    gp.transform.localPosition = gp.transform.localPosition + (PriorModelOffsetRightPosition - NewModelOffsetRightPosition);
                    gp.transform.localRotation *= Quaternion.Euler(PriorModelOffsetRightRotation) * Quaternion.Inverse(Quaternion.Euler(NewModelOffsetRightRotation));
                }
                // Left Hand only
                else if(gp.LeftHandIsValid) {
                    gp.transform.localPosition = gp.transform.localPosition + (PriorModelOffsetLeftPosition - NewModelOffsetLeftPosition);
                    gp.transform.localRotation *= Quaternion.Euler(PriorModelOffsetLeftRotation) * Quaternion.Inverse(Quaternion.Euler(NewModelOffsetLeftRotation));
                }
            }
        }
    }
}


