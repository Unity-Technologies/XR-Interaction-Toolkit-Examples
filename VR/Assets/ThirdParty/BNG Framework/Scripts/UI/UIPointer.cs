using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BNG {

    /// <summary>
    /// Point a line  at our GazePointer
    /// </summary>
    public class UIPointer : MonoBehaviour {

        [Tooltip("The controller side this pointer is on")]
        public ControllerHand ControllerSide = ControllerHand.Right;

        [Tooltip("If true this object will update the VRUISystem's Left or Right Transform property")]
        public bool AutoUpdateUITransforms = true;

        public GameObject cursor;
        private GameObject _cursor;

        [Tooltip("If true the cursor and LineRenderer will be Hidden. Otherwise it will still be show at a fixed length")]
        public bool HidePointerIfNoObjectsFound = true;

        [Tooltip("How long the line / cursor should extend if no objects are found to point at")]
        public float FixedPointerLength = 0.5f;

        [Tooltip("If true the cursor object will scale based on how far away the pointer is from the origin. A cursor far away will have a larger cusor than one up close.")]
        public bool CursorScaling = true;

        [Tooltip("Minimum scale of the Cursor object if CursorScaling is enabled")]
        public float CursorMinScale = 0.6f;
        public float CursorMaxScale = 6.0f;
        
        private Vector3 _cursorInitialLocalScale;

        /// <summary>
        /// 0.5 = Line Goes Half Way. 1 = Line reaches end.
        /// </summary>
        [Tooltip("Example : 0.5 = Line Goes Half Way. 1 = Line reaches end.")]
        public float LineDistanceModifier = 0.8f;

        /// <summary>
        /// Calls Events
        /// </summary>
        VRUISystem uiSystem;
        PointerEvents selectedPointerEvents;
        PointerEventData data;

        [Tooltip("LineRenderer to use when showing a valid UI Canvas. Leave null to attempt a GetComponent<> on this object.")]
        public LineRenderer lineRenderer;

        void Awake() {

            if(cursor) {
                _cursor = GameObject.Instantiate(cursor);
                _cursor.transform.SetParent(transform);
                _cursorInitialLocalScale = transform.localScale;
            }

            // If no Line Renderer was specified in the editor, check this Transform
            if (lineRenderer == null) {
                lineRenderer = GetComponent<LineRenderer>();
            }

            uiSystem = VRUISystem.Instance;           
        }

        void OnEnable() {
            // Automatically update VR System with our transforms
            if (AutoUpdateUITransforms && ControllerSide == ControllerHand.Left) {
                uiSystem.LeftPointerTransform = this.transform;
            }
            else if (AutoUpdateUITransforms && ControllerSide == ControllerHand.Right) {
                uiSystem.RightPointerTransform = this.transform;
            }

            uiSystem.UpdateControllerHand(ControllerSide);
        }

        public void Update() {
            data = uiSystem.EventData;

            // Can bail early if not looking at anything
            if (data == null || data.pointerCurrentRaycast.gameObject == null) {
                
                HidePointer();
                                                
                return;
            }

            // Set position of the cursor
            if (_cursor != null ) {

                bool lookingAtUI = data.pointerCurrentRaycast.module.GetType() == typeof(GraphicRaycaster);
                selectedPointerEvents = data.pointerCurrentRaycast.gameObject.GetComponent<PointerEvents>();
                bool lookingAtPhysicalObject = selectedPointerEvents != null;

                // Are we too far away from the Physics object now?
                if(lookingAtPhysicalObject) {
                    if (data.pointerCurrentRaycast.distance > selectedPointerEvents.MaxDistance) {
                        HidePointer();
                        return;
                    }
                }
                
                // Can bail immediately if not looking at a UI object or an Object with PointerEvents on it
                if(!lookingAtUI && !lookingAtPhysicalObject) {
                    HidePointer();
                    return;
                }

                // Set as local position
                float distance = Vector3.Distance(transform.position, data.pointerCurrentRaycast.worldPosition);
                _cursor.transform.localPosition = new Vector3(0, 0, distance - 0.0001f);                
                _cursor.transform.rotation = Quaternion.FromToRotation(Vector3.forward, data.pointerCurrentRaycast.worldNormal);

                // Scale cursor based on distance from main camera
                float cameraDist = Vector3.Distance(Camera.main.transform.position, _cursor.transform.position);
                _cursor.transform.localScale = _cursorInitialLocalScale * Mathf.Clamp(cameraDist, CursorMinScale, CursorMaxScale);

                _cursor.SetActive(data.pointerCurrentRaycast.distance > 0);
            }

            // Update linerenderer
            if (lineRenderer) {
                lineRenderer.useWorldSpace = false;
                lineRenderer.SetPosition(0, Vector3.zero);
                lineRenderer.SetPosition(1, new Vector3(0, 0 , Vector3.Distance(transform.position, data.pointerCurrentRaycast.worldPosition) * LineDistanceModifier));
                lineRenderer.enabled = data.pointerCurrentRaycast.distance > 0;
            }
        }

        public virtual void HidePointer() {
            // Hide the line and cursor
            if(HidePointerIfNoObjectsFound) {
                _cursor.SetActive(false);
                lineRenderer.enabled = false;
            }
            // Show a fixed length line
            else {
                if (_cursor) {
                    _cursor.SetActive(false);
                }

                // Set length to fixed amount
                if (lineRenderer) {
                    lineRenderer.useWorldSpace = false;
                    lineRenderer.SetPosition(0, Vector3.zero);
                    lineRenderer.SetPosition(1, new Vector3(0, 0, FixedPointerLength * LineDistanceModifier));
                    lineRenderer.enabled = true;
                }
            }
        }
    }
}

