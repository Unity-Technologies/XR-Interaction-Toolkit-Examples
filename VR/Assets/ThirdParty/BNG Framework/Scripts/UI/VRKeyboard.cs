using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace BNG {
    public class VRKeyboard : MonoBehaviour {

        public UnityEngine.UI.InputField AttachedInputField;

        public bool UseShift = false;

        [Header("Sound FX")]
        public AudioClip KeyPressSound;

        List<VRKeyboardKey> KeyboardKeys;

        void Awake() {
            KeyboardKeys = transform.GetComponentsInChildren<VRKeyboardKey>().ToList();
        }

        public void PressKey(string key) {

            if(AttachedInputField != null) {
                UpdateInputField(key);
            }
            else {
                Debug.Log("Pressed Key : " + key);
            }
        }

        public void UpdateInputField(string key) {
            string currentText = AttachedInputField.text;
            int caretPosition = AttachedInputField.caretPosition;
            int textLength = currentText.Length;
            bool caretAtEnd = AttachedInputField.isFocused == false || caretPosition == textLength;
            
            // Formatted key based on short names
            string formattedKey = key;
            if (key.ToLower() == "space") {
                formattedKey = " ";
            }
            
            // Find KeyCode Sequence
            if (formattedKey.ToLower() == "backspace") {
                // At beginning of line - nothing to back into
                if(caretPosition == 0) {
                    PlayClickSound(); // Still play the click sound
                    return;
                }

                currentText = currentText.Remove(caretPosition - 1, 1);

                if(!caretAtEnd) {
                    MoveCaretBack();
                }
            }
            else if (formattedKey.ToLower() == "enter") {
                // Debug.Log("Pressed Enter");
                // UnityEngine.EventSystems.ExecuteEvents.Execute(AttachedInputField.gameObject, null, UnityEngine.EventSystems.ExecuteEvents.submitHandler);
            }
            else if (formattedKey.ToLower() == "shift") {
                ToggleShift();
            }
            else {
                // Simply append the text to the end
                if(caretAtEnd) {
                    currentText += formattedKey;
                    MoveCaretUp();
                }
                else {
                    // Otherwise we need to figure out how to insert the text and where
                    string preText = "";
                    if (caretPosition > 0) {
                        preText = currentText.Substring(0, caretPosition);
                    }
                    MoveCaretUp();

                    string postText = currentText.Substring(caretPosition, textLength - preText.Length);

                    currentText = preText + formattedKey + postText;
                }
            }

            // Apply the text change
            AttachedInputField.text = currentText;

            PlayClickSound();

            // Keep Input Focused
            if (!AttachedInputField.isFocused) {
                AttachedInputField.Select();
            }
        }

        public virtual void PlayClickSound() {
            if(KeyPressSound != null) {
                VRUtils.Instance.PlaySpatialClipAt(KeyPressSound, transform.position, 1f, 0.5f);
            }
        }

        public void MoveCaretUp() {
            StartCoroutine(IncreaseInputFieldCareteRoutine());
        }

        public void MoveCaretBack() {
            StartCoroutine(DecreaseInputFieldCareteRoutine());
        }

        public void ToggleShift() {
            UseShift = !UseShift;

            foreach(var key in KeyboardKeys) {
                if(key != null) {
                    key.ToggleShift();
                }
            }
        }

        IEnumerator IncreaseInputFieldCareteRoutine() {
            yield return new WaitForEndOfFrame();
            AttachedInputField.caretPosition = AttachedInputField.caretPosition + 1;
            AttachedInputField.ForceLabelUpdate();
        }

        IEnumerator DecreaseInputFieldCareteRoutine() {
            yield return new WaitForEndOfFrame();
            AttachedInputField.caretPosition = AttachedInputField.caretPosition - 1;
            AttachedInputField.ForceLabelUpdate();
        }

        public void AttachToInputField(UnityEngine.UI.InputField inputField) {
            AttachedInputField = inputField;
        }
    }
}

