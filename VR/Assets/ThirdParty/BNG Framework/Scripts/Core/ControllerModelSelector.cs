using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    public class ControllerModelSelector : MonoBehaviour {

        int disableIndex = 0;
        
        void OnEnable() {
            // Subscribe to input events
            InputBridge.OnControllerFound += UpdateControllerModel;
        }

        public void UpdateControllerModel() {
            // XRInput controller/device names are not currently specific enough to distinquish between Quest 1, 2, and Rift devices
            string controllerName = InputBridge.Instance.GetControllerName().ToLower();
            if (controllerName.Contains("quest 2")) {
                EnableChildController(0);
            }
            else if (controllerName.Contains("quest")) {
                EnableChildController(1);
            }
            else if (controllerName.Contains("rift")) {
                EnableChildController(2);
            }
        }

        public void EnableChildController(int childIndex) {
            // Always disable the first child if we've found a controller
            transform.GetChild(disableIndex).gameObject.SetActive(false);

            // Now enable the new child
            transform.GetChild(childIndex).gameObject.SetActive(true);

            // Disable this item next time we need to enable another controller
            disableIndex = childIndex;
        }

        void OnDisable() {
            if (isQuitting) {
                return;
            }

            // Unsubscribe from input events
            InputBridge.OnControllerFound -= UpdateControllerModel;
        }

        bool isQuitting = false;
        void OnApplicationQuit() {
            isQuitting = true;
        }
    }
}

