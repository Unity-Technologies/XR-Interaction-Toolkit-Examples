using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

    public enum RemoteGrabType {
        Trigger,
        Raycast,
        Spherecast
    }

    /// <summary>
    /// Keeps track of Grabbables that are within it's Trigger
    /// </summary>
    public class RemoteGrabber : MonoBehaviour {

        public RemoteGrabType PhysicsCheckType = RemoteGrabType.Trigger;

        public float RaycastLength = 20f;

        public float SphereCastLength = 20f;
        public float SphereCastRadius = 0.05f;

        public LayerMask RemoteGrabLayers = ~0;

        // Grabber we can hand objects off to
        public GrabbablesInTrigger ParentGrabber;

        private Collider _lastColliderHit = null;

        void Start() {
            if(PhysicsCheckType == RemoteGrabType.Trigger && GetComponent<Collider>() == null) {
                Debug.LogWarning("Remote Grabber set to 'Trigger', but no Trigger Collider was found. You may need to add a collider, or switch to a different physics check type.");
            }
        }

        public virtual void Update() {
            if (PhysicsCheckType == RemoteGrabType.Raycast) {

                RaycastHit hit;
                if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, RaycastLength, RemoteGrabLayers)) {
                    ObjectHit(hit.collider);
                }
                else if (_lastColliderHit != null) {
                    RemovePreviousHitObject();
                }
            }
            else if (PhysicsCheckType == RemoteGrabType.Spherecast) {
                RaycastHit hit;
                if (Physics.SphereCast(transform.position, SphereCastRadius, transform.forward, out hit, SphereCastLength)) {
                    ObjectHit(hit.collider);
                }
                else if (_lastColliderHit != null) {
                    RemovePreviousHitObject();
                }
            }
        }

        private void ObjectHit(Collider colliderHit) {
            //  We will let this grabber know we have remote objects available            
            if (ParentGrabber == null) {
                return;
            }

            // Did our last item change?
            if (_lastColliderHit != colliderHit) {
                RemovePreviousHitObject();
            }

            _lastColliderHit = colliderHit;

            if (_lastColliderHit.gameObject.TryGetComponent(out Grabbable grabObject)) {
                ParentGrabber.AddValidRemoteGrabbable(_lastColliderHit, grabObject);
                return;
            }

            // Check for Grabbable Child Object Last
             if (_lastColliderHit.gameObject.TryGetComponent(out GrabbableChild gc)) {
                ParentGrabber.AddValidRemoteGrabbable(_lastColliderHit, gc.ParentGrabbable);
                return;
            }
        }

        public void RemovePreviousHitObject() {
            if (_lastColliderHit == null) return;

            if (_lastColliderHit.TryGetComponent(out Grabbable grabObject)) {
                ParentGrabber.RemoveValidRemoteGrabbable(_lastColliderHit, grabObject);
                return;
            }

            // Check for Grabbable Child Object Last
            if (_lastColliderHit.TryGetComponent(out GrabbableChild gc)) {
                ParentGrabber.RemoveValidRemoteGrabbable(_lastColliderHit, gc.ParentGrabbable);
                return;
            }

            _lastColliderHit = null;
        }

        void OnTriggerEnter(Collider other) {
            
            // Skip check for other PhysicsCheckTypes
            if (ParentGrabber == null || PhysicsCheckType != RemoteGrabType.Trigger) {
                return;
            }
            
            // Ignore Raycast Triggers
            if(other.gameObject.layer == 2) {
                return;
            }

            //  We will let this grabber know we have remote objects available           
            Grabbable grabObject = other.GetComponent<Grabbable>();
            if(grabObject != null && ParentGrabber != null) {
                ParentGrabber.AddValidRemoteGrabbable(other, grabObject);
                return;
            }

            // Check for Grabbable Child Object Last
            GrabbableChild gc = other.GetComponent<GrabbableChild>();
            if (gc != null && ParentGrabber != null) {
                ParentGrabber.AddValidRemoteGrabbable(other, gc.ParentGrabbable);
                return;
            }
        }

        void OnTriggerExit(Collider other) {

            // Skip check for other PhysicsCheckTypes
            if (ParentGrabber == null || PhysicsCheckType != RemoteGrabType.Trigger) {
                return;
            }

            Grabbable grabObject = other.GetComponent<Grabbable>();
            if (grabObject != null && ParentGrabber != null) {
                ParentGrabber.RemoveValidRemoteGrabbable(other, grabObject);
                return;
            }

            // Check for Grabbable Child Object Last
            GrabbableChild gc = other.GetComponent<GrabbableChild>();
            if (gc != null && ParentGrabber != null) {
                ParentGrabber.RemoveValidRemoteGrabbable(other, gc.ParentGrabbable);
                return;
            }
        }

        #region EditorGizmos

        public bool ShowGizmos = true;

#if UNITY_EDITOR
        void OnDrawGizmos() {

            // Don't draw gizmos if this component has been disabled
            if (!this.isActiveAndEnabled) {
                return;
            }

            if (ShowGizmos) {
                if (PhysicsCheckType == RemoteGrabType.Raycast) {
                    Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * RaycastLength, Color.green);
                }
                else if(PhysicsCheckType == RemoteGrabType.Spherecast) {
                    Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * SphereCastLength, Color.green);

                    DrawWireCapsule(transform.position + (transform.forward * SphereCastLength / 2), transform.rotation * Quaternion.Euler(90f, 0, 0), SphereCastRadius, SphereCastLength, Color.green);
                }
            }
        }

        // Draw a Wire Gizmos, similar to a wiresphere, but for capsules
        // https://answers.unity.com/questions/56063/draw-capsule-gizmo.html
        public void DrawWireCapsule(Vector3 position, Quaternion rotation, float radius, float height, Color color) {
            
            UnityEditor.Handles.color = color;
            Matrix4x4 angleMatrix = Matrix4x4.TRS(position, rotation, UnityEditor.Handles.matrix.lossyScale);

            using (new UnityEditor.Handles.DrawingScope(angleMatrix)) {
                var pointOffset = (height - (radius * 2)) / 2;

                // Draw sideways
                UnityEditor.Handles.DrawWireArc(Vector3.up * pointOffset, Vector3.left, Vector3.back, -180, radius);
                UnityEditor.Handles.DrawLine(new Vector3(0, pointOffset, -radius), new Vector3(0, -pointOffset, -radius));
                UnityEditor.Handles.DrawLine(new Vector3(0, pointOffset, radius), new Vector3(0, -pointOffset, radius));
                UnityEditor.Handles.DrawWireArc(Vector3.down * pointOffset, Vector3.left, Vector3.back, 180, radius);

                // Draw frontways
                UnityEditor.Handles.DrawWireArc(Vector3.up * pointOffset, Vector3.back, Vector3.left, 180, radius);
                UnityEditor.Handles.DrawLine(new Vector3(-radius, pointOffset, 0), new Vector3(-radius, -pointOffset, 0));
                UnityEditor.Handles.DrawLine(new Vector3(radius, pointOffset, 0), new Vector3(radius, -pointOffset, 0));
                UnityEditor.Handles.DrawWireArc(Vector3.down * pointOffset, Vector3.back, Vector3.left, -180, radius);

                // Draw center
                UnityEditor.Handles.DrawWireDisc(Vector3.up * pointOffset, Vector3.up, radius);
                UnityEditor.Handles.DrawWireDisc(Vector3.down * pointOffset, Vector3.up, radius);
            }
        }
#endif
        #endregion
    }
}