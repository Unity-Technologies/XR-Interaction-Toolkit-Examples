using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BNG {

    [CustomEditor(typeof(TrackedDevice))]
    [CanEditMultipleObjects]
    public class TrackedDeviceEditor : Editor {

        TrackedDevice device;

        public override void OnInspectorGUI() {

            device = (TrackedDevice)target;

            EditorGUILayout.HelpBox("This is a simple alternative to the Tracked Pose Driver. This Transform's position will be updated in Update(), FixedUpdate(), and OnBeforeRender(). \n\n Feel free to replace this with the TrackedPoseDriver component from the XR Legacy Input Helpers package or with your own custom tracking solution.", MessageType.Info);

            base.OnInspectorGUI();

            serializedObject.ApplyModifiedProperties();
        }
    }
}

