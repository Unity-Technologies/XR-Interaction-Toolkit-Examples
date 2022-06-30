using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BNG {
    public class EditorHandle : MonoBehaviour {

        public bool ShowTransformName = false;

        public float Radius = 0.000875f;

        public Color BaseColor = new Color(255f, 255f, 255f, 0.1f);

#if UNITY_EDITOR
        void OnEnable() {
            this.gameObject.hideFlags = HideFlags.None;
            this.gameObject.GetComponent<EditorHandle>().hideFlags = HideFlags.HideInInspector;
        }

        void OnDrawGizmos() {

            var outlineColor = new Color(BaseColor.r, BaseColor.g, BaseColor.b, BaseColor.a - 0.025f);
            var innerColor = new Color(BaseColor.r, BaseColor.g, BaseColor.b, BaseColor.a);
            var sphereColor = new Color(BaseColor.r, BaseColor.g, BaseColor.b, 0.01f);

            Vector3 normal = transform.position - UnityEditor.Handles.inverseMatrix.MultiplyPoint(Camera.current.transform.position);
            float sqrMagnitude = normal.sqrMagnitude;
            float num1 = Radius * Radius / sqrMagnitude;
            float num2 = Mathf.Sqrt(Radius - num1);

            UnityEditor.Handles.color = outlineColor;
            UnityEditor.Handles.DrawWireDisc(transform.position - Radius * normal / sqrMagnitude, normal, num2 / 2);

            UnityEditor.Handles.color = innerColor;

            if(UnityEditor.Selection.activeGameObject == gameObject) {
                UnityEditor.Handles.color = Color.yellow;
            }

            UnityEditor.Handles.DrawSolidDisc(transform.position - Radius * normal / sqrMagnitude, normal, num2 / 2.25f);


            if (ShowTransformName) {
                UnityEditor.Handles.Label(transform.position + new Vector3(0, -0.003f, 0), transform.name);
            }

            Gizmos.color = new Color(255f, 255f, 255f, 0.02f);
            Gizmos.DrawSphere(transform.position, 0.003f);
        }
#endif
    }
}

