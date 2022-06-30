using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BNG {

    [CustomEditor(typeof(VRIFGrabpointUpdater))]
    [CanEditMultipleObjects]
    public class VRIFGrabpointUpdaterEditor : Editor {

        VRIFGrabpointUpdater updater;

        void OnEnable() {
           
        }

        public override void OnInspectorGUI() {

            updater = (VRIFGrabpointUpdater)target;

            EditorGUILayout.HelpBox("This component will help you autmatically update your grab points, as the default hand model positions have changed since VRIF v1.7. Click the 'Updated Pose' button to update the grab points in the editor. You can preview the changes in play mode before applying the changes in the editor.", MessageType.Info);

            if (GUILayout.Button("Update Pose")) {
                updater.ApplyGrabPointUpdate();
            }

            EditorGUILayout.Separator();

            serializedObject.ApplyModifiedProperties();

            base.OnInspectorGUI();
        }
    }
}


