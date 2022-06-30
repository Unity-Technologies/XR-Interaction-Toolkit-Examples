using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    public class Tooltip : MonoBehaviour {

        /// <summary>
        /// Offset from Object we are providing tip to
        /// </summary>
        public Vector3 TipOffset = new Vector3(1.5f, 0.2f, 0);

        /// <summary>
        /// If true Y axis will be in World Coordinates. False for local coords.
        /// </summary>
        public bool UseWorldYAxis = true;

        /// <summary>
        /// Hide the tooltip if Camera is farther away than this. In meters.
        /// </summary>
        public float MaxViewDistance = 10f;

        /// <summary>
        /// Hide this if farther than MaxViewDistance
        /// </summary>
        Transform childTransform;

        public Transform DrawLineTo;
        LineToTransform lineTo;
        Transform lookAt;
        
        void Start() {
            lookAt = Camera.main.transform;
            lineTo = GetComponentInChildren<LineToTransform>();

            childTransform = transform.GetChild(0);

            if (DrawLineTo && lineTo) {
                lineTo.ConnectTo = DrawLineTo;
            }
        }

        void Update() {
            UpdateTooltipPosition();
        }

        public virtual void UpdateTooltipPosition() {
            if (lookAt) {
                transform.LookAt(Camera.main.transform);
            }
            else if (Camera.main != null) {
                lookAt = Camera.main.transform;
            }
            else if (Camera.main == null) {
                return;
            }

            transform.parent = DrawLineTo;
            transform.localPosition = TipOffset;

            if (UseWorldYAxis) {
                transform.localPosition = new Vector3(transform.localPosition.x, 0, transform.localPosition.z);
                transform.position += new Vector3(0, TipOffset.y, 0);
            }

            if (childTransform) {
                childTransform.gameObject.SetActive(Vector3.Distance(transform.position, Camera.main.transform.position) <= MaxViewDistance);
            }
        }
    }
}
