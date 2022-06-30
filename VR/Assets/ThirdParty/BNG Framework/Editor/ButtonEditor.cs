using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BNG {

    [CustomEditor(typeof(Button))]
    public class ButtonEditor : Editor {


        public override void OnInspectorGUI() {

            Button button = (Button)target;

            GUILayout.Label("Show Button Position : ", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Preview Button Up")) {
                button.transform.localPosition = new Vector3(button.transform.localPosition.x, button.MaxLocalY, button.transform.localPosition.z);
            }

            if (GUILayout.Button("Preview Button Down")) {
                button.transform.localPosition = new Vector3(button.transform.localPosition.x, button.MinLocalY, button.transform.localPosition.z);
            }

            GUILayout.EndHorizontal();

            // Make sure local position is always capped properly
            // Cap values
            if (button.transform.localPosition.y < button.MinLocalY) {
                button.transform.localPosition = button.GetButtonDownPosition();
            }
            else if (button.transform.localPosition.y > button.MaxLocalY) {
                button.transform.localPosition = button.GetButtonUpPosition();
            }

            base.OnInspectorGUI();
        }
    }
}

