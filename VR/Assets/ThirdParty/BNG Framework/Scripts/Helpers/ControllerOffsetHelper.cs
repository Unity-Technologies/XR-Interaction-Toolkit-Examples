using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

namespace BNG {
    public class ControllerOffsetHelper : MonoBehaviour {

        public ControllerHand ControllerHand = ControllerHand.Right;

        [Header("Shown for Debug :")]
        [Tooltip("The model of controller found")]
        [SerializeField]
        private string thisControllerModel;

        [SerializeField]
        private ControllerOffset thisOffset;

        [Tooltip("The position offset is defined within this script and loaded once the controller is found.")]
        public Vector3 OffsetPosition;
        [Tooltip("The rotation offset is defined within this script and loaded once the controller is found.")]
        public Vector3 OffsetRotation;


        public List<ControllerOffset> ControllerOffsets;

        void Start() {
            if(ControllerOffsets == null) {
                ControllerOffsets = new List<ControllerOffset>();
            }

            StartCoroutine(checkForController());
        }

        IEnumerator checkForController() {

            while(string.IsNullOrEmpty(thisControllerModel)) {

                thisControllerModel = InputBridge.Instance.GetControllerName();

                yield return new WaitForEndOfFrame();
            }

            OnControllerFound();
        }

        public virtual void OnControllerFound() {
            // Debug.Log("Controller found : " + thisControllerModel);

            DefineControllerOffsets();

            thisOffset = GetControllerOffset(thisControllerModel);

            if(thisOffset != null) {
                if(ControllerHand == ControllerHand.Left) {
                    OffsetPosition = thisOffset.LeftControllerPositionOffset;
                    OffsetRotation = thisOffset.LeftControllerRotationOffset;

                    transform.localPosition += OffsetPosition;
                    transform.localEulerAngles += OffsetRotation;
                }
                else if (ControllerHand == ControllerHand.Right) {
                    OffsetPosition = thisOffset.RightControllerPositionOffset;
                    OffsetRotation = thisOffset.RightControlleRotationOffset;

                    transform.localPosition += OffsetPosition;
                    transform.localEulerAngles += OffsetRotation;
                }
            }
        }

        public virtual ControllerOffset GetControllerOffset(string controllerName) {

            var offset = ControllerOffsets.FirstOrDefault(x => thisControllerModel.StartsWith(x.ControllerName));

            // This is an OpenXR controller - fallback to a generic offset
            if(offset == null && controllerName.EndsWith("OpenXR")) {
                return GetOpenXROffset();
            }

            return offset;
        }

        public virtual void DefineControllerOffsets() {
            ControllerOffsets = new List<ControllerOffset>();

            // Sample OpenVR Offsets :
            // Oculus Touch OpenVR :  "OpenVR Controller(Oculus Quest (Right Controller)) - Right"
            // HTC Vive Wand : "OpenVR Controller(Vive Controller MV) - Right"
            // Vive Cosmos : "OpenVR Controller(vive_cosmos_controller) - Right"
            // Valve Knuckles OpenVR : "OpenVR Controller(Knuckles Right) -Right"
            // Windows WMR - HP 1440 - VR 1000 : "OpenVR Controller(WindowsMR: 0x045E/0x065B/0/2) - Right"

            ControllerOffsets.Add(new ControllerOffset() {
                ControllerName = "Oculus Touch Controller OpenXR",
                LeftControllerPositionOffset = new Vector3(0.002f, -0.02f, 0.04f),
                RightControllerPositionOffset = new Vector3(-0.002f, -0.02f, 0.04f),
                LeftControllerRotationOffset = new Vector3(60.0f, 0.0f, 0.0f),
                RightControlleRotationOffset = new Vector3(60.0f, 0.0f, 0.0f)
            });

            ControllerOffsets.Add(new ControllerOffset() {
                ControllerName = "Index Controller OpenXR",
                LeftControllerPositionOffset = new Vector3(0.002f, -0.02f, 0.04f),
                RightControllerPositionOffset = new Vector3(-0.002f, -0.02f, 0.04f),
                LeftControllerRotationOffset = new Vector3(60.0f, 0.0f, 0.0f),
                RightControlleRotationOffset = new Vector3(60.0f, 0.0f, 0.0f)
            });

            // Oculus Touch on Oculus SDK is at correct orientation by default
            // Example  : "Oculus Touch Controller - Right"
            ControllerOffsets.Add(new ControllerOffset() { 
                ControllerName = "Oculus Touch Controller",
            });

            // Oculus Quest Example : 
            ControllerOffsets.Add(new ControllerOffset() {
                ControllerName = "OpenVR Controller(Oculus Quest",
                LeftControllerPositionOffset = new Vector3(0.0075f, -0.005f, -0.0525f),
                RightControllerPositionOffset = new Vector3(-0.0075f, -0.005f, -0.0525f),
                LeftControllerRotationOffset = new Vector3(40.0f, 0.0f, 0.0f),
                RightControlleRotationOffset = new Vector3(40.0f, 0.0f, 0.0f)
            });

            // Default all other OpenVR Controllers to about a 40 degree angle
            ControllerOffsets.Add(new ControllerOffset() {
                ControllerName = "OpenVR Controller",
                LeftControllerPositionOffset = new Vector3(0.0075f, -0.005f, -0.0525f),
                RightControllerPositionOffset = new Vector3(-0.0075f, -0.005f, -0.0525f),
                LeftControllerRotationOffset = new Vector3(40.0f, 0.0f, 0.0f),
                RightControlleRotationOffset = new Vector3(40.0f, 0.0f, 0.0f)
            });
        }

        /// <summary>
        /// Returns a generic offset for OpenXR controllers not defined in DefineControllerOffsets(). 
        /// All OpenXR controllers appear to have about a 60 degree rotation in Unity, for example.
        /// Override this method if you need to specify a different offset (or none at all)
        /// </summary>
        /// <returns></returns>
        public virtual ControllerOffset GetOpenXROffset() {
            return new ControllerOffset() {
                ControllerName = "Controller OpenXR",
                LeftControllerPositionOffset = new Vector3(0.002f, -0.02f, 0.04f),
                RightControllerPositionOffset = new Vector3(-0.002f, -0.02f, 0.04f),
                LeftControllerRotationOffset = new Vector3(60.0f, 0.0f, 0.0f),
                RightControlleRotationOffset = new Vector3(60.0f, 0.0f, 0.0f)
            };
        }
    }

    public class ControllerOffset {
        public string ControllerName { get; set; }
        public Vector3 LeftControllerPositionOffset { get; set; }
        public Vector3 RightControllerPositionOffset { get; set; }
        public Vector3 LeftControllerRotationOffset { get; set; }
        public Vector3 RightControlleRotationOffset { get; set; }
    }
}

