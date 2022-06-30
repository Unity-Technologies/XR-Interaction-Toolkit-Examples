using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    public class VRTextInput : MonoBehaviour {

        UnityEngine.UI.InputField thisInputField;

        public bool AttachToVRKeyboard = true;

        public bool ActivateKeyboardOnSelect = true;
        public bool DeactivateKeyboardOnDeselect = false;

        public VRKeyboard AttachedKeyboard;

        bool isFocused, wasFocused = false;

        void Awake() {
            thisInputField = GetComponent<UnityEngine.UI.InputField>();
            
            if(thisInputField) {
                AttachedKeyboard.AttachToInputField(thisInputField);
            }
        }

        void Update() {

            isFocused = thisInputField != null && thisInputField.isFocused;

            // Check if our input field is now focused
            if(isFocused == true && wasFocused == false) {
                OnInputSelect();
            }
            else if (isFocused == false && wasFocused == true) {
                OnInputDeselect();
            }

            wasFocused = isFocused;
        }

        public void OnInputSelect() {

            // Enable keyboard if disabled
            if (ActivateKeyboardOnSelect && AttachedKeyboard != null && !AttachedKeyboard.gameObject.activeInHierarchy) {
                AttachedKeyboard.gameObject.SetActive(true);

                AttachedKeyboard.AttachToInputField(thisInputField);
            }
        }

        public void OnInputDeselect() {
            if (DeactivateKeyboardOnDeselect && AttachedKeyboard != null && AttachedKeyboard.gameObject.activeInHierarchy) {
                AttachedKeyboard.gameObject.SetActive(false);
            }
        }
    }
}


