using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BNG {

    [CustomEditor(typeof(InputBridge))]
    public class InputBridgeEditor : Editor {

        InputBridge inputBridge;

        SerializedProperty inputSource;
        SerializedProperty actionSet;
        SerializedProperty trackingOrigin;
        SerializedProperty ThumbstickDeadzoneX;
        SerializedProperty ThumbstickDeadzoneY;

        bool steamVRSupport = false;
        bool ovrSupport = false;

        void OnEnable() {
            inputSource = serializedObject.FindProperty("InputSource");
            actionSet = serializedObject.FindProperty("actionSet");
            trackingOrigin = serializedObject.FindProperty("TrackingOrigin");
            ThumbstickDeadzoneX = serializedObject.FindProperty("ThumbstickDeadzoneX");
            ThumbstickDeadzoneY = serializedObject.FindProperty("ThumbstickDeadzoneY");
        }

        public override void OnInspectorGUI() {

            inputBridge = (InputBridge)target;

            setupRichText();

            EditorGUILayout.PropertyField(inputSource);

            // Show Action Set if using Unity Input
            // if (inputBridge.InputSource == XRInputSource.UnityInput) {
                EditorGUILayout.PropertyField(actionSet);
            // }

#if STEAM_VR_SDK
            steamVRSupport = true;
#endif

#if OCULUS_INTEGRATION
            ovrSupport = true;
#endif

            // Show warning message / button if SteamVR integration isn't enabled
            if (inputBridge.InputSource == XRInputSource.SteamVR && steamVRSupport == false) {
                EditorGUILayout.HelpBox("Input Source set to 'SteamVR', but SteamVR integration has not been enabled. Would you like to enable it now?", MessageType.Warning);
                if (GUILayout.Button("Open Integrations Window")) {
                    IntegrationsEditor.ShowWindow();
                }
            }
            // Show warning message / button if Oculus integration isn't enabled
            else if (inputBridge.InputSource == XRInputSource.OVRInput && ovrSupport == false) {
                EditorGUILayout.HelpBox("Input Source set to 'OVRInput', but Oculus Integration has not been enabled. Would you like to enable it now?", MessageType.Warning);
                if (GUILayout.Button("Open Integrations Window")) {
                    IntegrationsEditor.ShowWindow();
                }
            }

            // Tracking Origin
            EditorGUILayout.PropertyField(trackingOrigin);

            // Deadzone
            EditorGUILayout.PropertyField(ThumbstickDeadzoneX);
            EditorGUILayout.PropertyField(ThumbstickDeadzoneY);

            EditorGUILayout.Separator();

            // Currently showing all
            if(inputBridge.ShowInputDebugger) {

                // Button to Hide all
                if (GUILayout.Button("Hide Input Debugger")) {
                    inputBridge.ShowInputDebugger = false;
                }

                // Show each item
                EditorGUILayout.Separator();

                EditorGUILayout.LabelField("Input Debugger : ", bold);

                DrawLabel("A Button", inputBridge.AButton);
                DrawLabel("B Button", inputBridge.BButton);
                DrawLabel("X Button", inputBridge.XButton);
                DrawLabel("Y Button", inputBridge.YButton);
                DrawLabel("Start Button", inputBridge.StartButton);
                DrawLabel("Back Button", inputBridge.BackButton);
                
                DrawLabelFloat("Left Trigger", inputBridge.LeftTrigger);
                DrawLabelFloat("Right Trigger", inputBridge.RightTrigger);


                DrawLabelFloat("Left Grip", inputBridge.LeftGrip);
                DrawLabelFloat("Right Grip", inputBridge.RightGrip);

                DrawLabel("Left Trigger Near", inputBridge.LeftTriggerNear);
                DrawLabel("Right Trigger Near", inputBridge.RightTriggerNear);

                DrawLabel("Left Thumb Near", inputBridge.LeftThumbNear);
                DrawLabel("Right Thumb Near", inputBridge.RightThumbNear);

                DrawLabelVector2("Left Thumbstick  ", inputBridge.LeftThumbstickAxis);
                DrawLabelVector2("Right Thumbstick", inputBridge.RightThumbstickAxis);

                // Thumb / Index Touching

                DrawLabelVector2("Left TouchPad Axis  ", inputBridge.LeftTouchPadAxis);
                DrawLabelVector2("Right TouchPad Axis", inputBridge.RightTouchPadAxis);

                EditorGUILayout.Separator();

                EditorGUILayout.LabelField("Device Properties : ", bold);

                DrawLabel("HMD Active", inputBridge.HMDActive);

                string deviceName = inputBridge.GetHMDName();
                if (!string.IsNullOrEmpty(deviceName)) {
                    EditorGUILayout.LabelField("Device Name : <color=green><b>" + deviceName + "</b></color>", rt);
                }
                else {
                    EditorGUILayout.LabelField("Device Name : <color=gray><b>(Undetected)</b></color>", rt);
                }

                string controllerName = inputBridge.GetControllerName();
                if(!string.IsNullOrEmpty(controllerName)) {
                    EditorGUILayout.LabelField("Controller Name : <color=green><b>" + controllerName + "</b></color>", rt);
                }
                else {
                    EditorGUILayout.LabelField("Controller Name : <color=gray><b>(Undetected)</b></color>", rt);
                }
            }
            // Hiding input debugger
            else {
                if (GUILayout.Button("Show Input Debugger")) {
                    inputBridge.ShowInputDebugger = true;
                }
            }
            
            // Apply any changes
            serializedObject.ApplyModifiedProperties();
        }

        GUIStyle rt;
        GUIStyle bold;


        public void DrawLabelFloat(string labelName, float value) {

            if (value > 0) {
                EditorGUILayout.LabelField(labelName + " : <color=green> <b>" + value + "</b></color>", rt);
            }
            else {
                EditorGUILayout.LabelField(labelName + " : <color=gray> <b>" + value + "</b></color>", rt);
            }
        }

        public void DrawLabelVector2(string labelName, Vector2 value) {
            if (value.magnitude > 0) {
                EditorGUILayout.LabelField(string.Format("{0}   X :  <color=green><b>{1}</b></color>      Y :  <color=green><b>{2}</b></color>", labelName, value.x, value.y), rt);
            }
            else {
                EditorGUILayout.LabelField(string.Format("{0}   X :  <color=gray><b>{1}</b></color>      Y :  <color=gray><b>{2}</b></color>", labelName, value.x, value.y), rt);
            }
        }

        public void DrawLabel(string labelName, float value) {
            DrawLabel(labelName, value > 0);
        }

        public void DrawLabel(string labelName, bool active) {

            // Active
            if (active) {
                labelName += " : <color=green> <b>True</b></color>";
            }
            else {
                labelName += " : <color=gray> <b>False</b></color>";
            }

            EditorGUILayout.LabelField(labelName, rt);
        }

        public void setupRichText() {
            // Set up our default, rich text label
            if (rt == null) {
                rt = new GUIStyle(EditorStyles.label);
            }
            rt.richText = true;
            rt.fontStyle = FontStyle.Normal;


            if (bold == null) {
                bold = new GUIStyle(EditorStyles.label);
            }

            bold.fontStyle = FontStyle.Bold;
        }
    }
}

