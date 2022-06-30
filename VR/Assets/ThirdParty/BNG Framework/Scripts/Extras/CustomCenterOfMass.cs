using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BNG {
    public class CustomCenterOfMass : MonoBehaviour {

        [Header("Define Center of Mass")]
        [Tooltip("Local coordinates to use as center of mass if 'CenterOfMassTransform' is not specified.")]
        public Vector3 CenterOfMass = Vector3.zero;

        [Tooltip("Use this Transform's local position for the center of mass if specified.")]
        public Transform CenterOfMassTransform;

        [Header("Debug Options")]
        [Tooltip("If true a red sphere will in the editor show where the center of mass will be positioned")]
        public bool ShowGizmo = true;

        Rigidbody rigid;

        // Start is called before the first frame update
        void Start() {
            rigid = GetComponent<Rigidbody>();
            SetCenterOfMass(getThisCenterOfMass());
        }

        public virtual void SetCenterOfMass(Vector3 center) {
            if (rigid) {
                rigid.centerOfMass = center;
            }
        }

        protected virtual Vector3 getThisCenterOfMass() {
            if (CenterOfMassTransform != null) {
                return CenterOfMassTransform.localPosition;
            }
            else {
                return CenterOfMass;
            }
        }

        void OnDrawGizmos() {
            if(ShowGizmo) {
                Gizmos.color = Color.red;
                if(rigid) {
                    Gizmos.DrawSphere(rigid.worldCenterOfMass, 0.02f);
                }
                else {
                    Gizmos.DrawSphere(transform.position + transform.TransformVector(getThisCenterOfMass()), 0.02f);
                }
            }
        }
    }
}

