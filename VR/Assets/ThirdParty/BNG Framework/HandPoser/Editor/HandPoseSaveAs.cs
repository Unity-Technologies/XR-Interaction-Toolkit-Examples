using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BNG {
    public class HandPoseSaveAs : EditorWindow {

        public string PoseName = "HandPose";

        

        HandPoser inspectedPoser;
        GUIStyle rt;

        // [MenuItem("VRIF/HandPose Save As...")]
        public void ShowWindow(HandPoser poser) {
            EditorWindow.GetWindow(typeof(HandPoseSaveAs));

            inspectedPoser = poser;

            if(inspectedPoser != null && inspectedPoser.CurrentPose != null) {
                PoseName = inspectedPoser.CurrentPose.name;
            }
            else {
                PoseName = "Default";
            }

            const int width = 480;
            const int height = 220;

            var x = (Screen.currentResolution.width - width) / 2;
            var y = (Screen.currentResolution.height - height) / 2;

            GetWindow<HandPoseSaveAs>("Save HandPose As...").position = new Rect(x, y, width, height);
        }

        public void Save() {

            inspectedPoser.SavePoseAsScriptablObject(PoseName);

            // Close the window
            GetWindow<HandPoseSaveAs>().Close();

            // Update the inspector object's properties
            HandPose newPose = Resources.Load<HandPose>(PoseName);
            if (newPose) {
                inspectedPoser.CurrentPose = newPose;
            }

            // Highlight the newly created Objects
            // Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(assetsDirectory + fileName);
        }

        

        public void SaveAs() {

            GetWindow<HandPoseSaveAs>().Close();

            string path = EditorUtility.SaveFilePanelInProject("Save Hand Pose", "HandPose", "asset", "Please enter a file name to save the hand pose");
            if (path.Length != 0) {
                var poseObject = inspectedPoser.GetHandPoseScriptableObject();

                // Creates the file in the folder path
                AssetDatabase.CreateAsset(poseObject, path);
                AssetDatabase.SaveAssets();

                // As we are saving to the asset folder, tell Unity to scan for modified or new assets
                AssetDatabase.Refresh();

                // Set as active in the hierarchy
                // Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(path);
                inspectedPoser.CurrentPose = poseObject;
            }
        }


        void OnGUI() {

            GUI.changed = false;

            if (rt == null) {
                rt = new GUIStyle(EditorStyles.label);
                rt.richText = true;
            }

            EditorGUILayout.HelpBox("Hand poses are stored in a Resources directory. \nThis allows them to be loaded at runtime using Resources.Load().", MessageType.Info);

            EditorGUILayout.Separator();

            GUILayout.Label("Save Handpose As...", EditorStyles.boldLabel);

            EditorGUILayout.Separator();

            GUILayout.Label("Name : ", EditorStyles.boldLabel);
            PoseName = EditorGUILayout.TextField(PoseName, EditorStyles.textField);

            if(GUI.changed) {
                // Value has changed
            }

            string formattedName = PoseName;
            if(!string.IsNullOrEmpty(formattedName)) {
                formattedName += ".asset";
            }

            EditorGUILayout.Separator();

            if (inspectedPoser != null) {
                GUILayout.Label("<b>Will save to : </b>", rt);
                GUILayout.Label("<i>" + inspectedPoser.ResourcePath + formattedName + "</i>", rt);
            }

            EditorGUILayout.Separator();

            // Button area
            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(" Save as... ", EditorStyles.miniButton)) {
                SaveAs();
            }

            var initialColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button(" Save ", EditorStyles.miniButton)) {
                Save();
            }

            // Reset color
            GUI.backgroundColor = initialColor;

            GUILayout.EndHorizontal();
            //GUILayout.Label("Width : " + GetWindow<HandPoseSaveAs>().position.width, rt);
            //GUILayout.Label("Height : " + GetWindow<HandPoseSaveAs>().position.height, rt);
        }
    }
}

