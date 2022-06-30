using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

    public class VRKeyboardKey : MonoBehaviour {

        UnityEngine.UI.Button thisButton;
        UnityEngine.UI.Text thisButtonText;

        VRKeyboard vrKeyboard;

        public string Keycode;

        public string KeycodeShift;

        [HideInInspector]
        public bool UseShiftKey = false;

        void Awake() {
            thisButton = GetComponent<UnityEngine.UI.Button>();
            thisButtonText = GetComponentInChildren<UnityEngine.UI.Text>();

            // Assign click event handler
            if (thisButton != null) {                
                thisButton.onClick.AddListener(OnKeyHit);
            }

            vrKeyboard = GetComponentInParent<VRKeyboard>();
        }

        public virtual void ToggleShift() {
            UseShiftKey = !UseShiftKey;

            // Make sure the button exists
            if(thisButtonText == null) {
                return;
            }

            // Update text label
            if(UseShiftKey && !string.IsNullOrEmpty(KeycodeShift)) {
                thisButtonText.text = KeycodeShift;
            }
            else {
                thisButtonText.text = Keycode;
            }
        }

        public virtual void OnKeyHit() {
            OnKeyHit(UseShiftKey && !string.IsNullOrEmpty(KeycodeShift) ? KeycodeShift : Keycode);
        }

        public virtual void OnKeyHit(string key) {
            if(vrKeyboard != null) {
                vrKeyboard.PressKey(key);
            }
            else {
                Debug.Log("Pressed key " + key + ", but no keyboard was found");
            }
        }
    }
}

