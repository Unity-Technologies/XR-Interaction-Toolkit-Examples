using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace BNG {

    [CustomEditor(typeof(HandPoser))]
    [CanEditMultipleObjects]
    public class HandPoserEditor : Editor {

        SerializedProperty currentPose;
        SerializedProperty animationSpeed;

        // Auto Pose properties
        SerializedProperty openHandPose;
        SerializedProperty closedHandPose;
        SerializedProperty autoUpdateAutoPose;
        SerializedProperty fingerTipRadius;
        SerializedProperty fingerTipOffsets;

        SerializedProperty WristJoint;
        SerializedProperty ThumbJoints;
        SerializedProperty IndexJoints;
        SerializedProperty MiddleJoints;
        SerializedProperty RingJoints;
        SerializedProperty PinkyJoints;
        SerializedProperty OtherJoints;

        HandPoser poser;
        void OnEnable() {
            currentPose = serializedObject.FindProperty("CurrentPose");
            animationSpeed = serializedObject.FindProperty("AnimationSpeed");
            openHandPose = serializedObject.FindProperty("OpenHandPose");
            closedHandPose = serializedObject.FindProperty("ClosedHandPose");
            autoUpdateAutoPose = serializedObject.FindProperty("UpdateContinuously");
            fingerTipRadius = serializedObject.FindProperty("FingerTipRadius");
            fingerTipOffsets = serializedObject.FindProperty("FingerTipOffsets");

            WristJoint = serializedObject.FindProperty("WristJoint");
            ThumbJoints = serializedObject.FindProperty("ThumbJoints");
            IndexJoints = serializedObject.FindProperty("IndexJoints");
            MiddleJoints = serializedObject.FindProperty("MiddleJoints");
            RingJoints = serializedObject.FindProperty("RingJoints");
            PinkyJoints = serializedObject.FindProperty("PinkyJoints");
            OtherJoints = serializedObject.FindProperty("OtherJoints");
        }

        bool showTransformProps;

        public override void OnInspectorGUI() {
            poser = (HandPoser)target;

            serializedObject.Update();

            GUILayout.Label("Hand Pose", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(currentPose);

            // Show option to auto assign if nothing has been set yet
            bool notYetSpecified = (poser.ThumbJoints == null || poser.ThumbJoints != null && poser.ThumbJoints.Count == 0) &&
                (poser.IndexJoints == null  || poser.IndexJoints  != null && poser.IndexJoints.Count == 0) &&
                (poser.MiddleJoints == null || poser.MiddleJoints != null && poser.MiddleJoints.Count == 0) &&
                (poser.RingJoints == null   || poser.RingJoints   != null && poser.RingJoints.Count == 0) &&
                (poser.PinkyJoints == null  || poser.PinkyJoints  != null && poser.PinkyJoints.Count == 0) &&
                (poser.OtherJoints == null  || poser.OtherJoints  != null && poser.OtherJoints.Count == 0);

            if (notYetSpecified ) {
                EditorGUILayout.HelpBox("No Bone Transforms have been assigned, would you like to auto assign them now?", MessageType.Warning);
                if (GUILayout.Button("Auto Assign")) {
                    AutoAssignBones();
                    // Toggle props
                    showTransformProps = true;
                }
            }

            // Show Transform Properties
            showTransformProps = EditorGUILayout.Foldout(showTransformProps, "Transform Definitions");
            if (showTransformProps) {

                EditorGUILayout.Separator();

                EditorGUI.indentLevel++;

                if (GUILayout.Button("Auto Assign")) {
                    AutoAssignBones();
                }

                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(ThumbJoints);

                EditorGUILayout.PropertyField(IndexJoints);

                EditorGUILayout.PropertyField(MiddleJoints);

                EditorGUILayout.PropertyField(RingJoints);

                EditorGUILayout.PropertyField(PinkyJoints);

                EditorGUILayout.PropertyField(OtherJoints);

                EditorGUILayout.PropertyField(WristJoint);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Separator();

            EditorGUILayout.PropertyField(animationSpeed);

            GUILayout.BeginHorizontal();

            var initialColor = GUI.backgroundColor;
            //GUI.backgroundColor = Color.green;

            if (GUILayout.Button("Save Pose...")) {
                SaveHandPoseAs(poser);
            }

            // Reset color
            GUI.backgroundColor = initialColor;

            GUILayout.EndHorizontal();

            EditorGUILayout.Separator();

            // Gizmos
            serializedObject.ApplyModifiedProperties();
        }

        public void AutoAssignBones() {
            // Reset bones
            poser.WristJoint = null;
            poser.ThumbJoints = new List<Transform>();
            poser.IndexJoints = new List<Transform>();
            poser.MiddleJoints = new List<Transform>();
            poser.RingJoints= new List<Transform>();
            poser.PinkyJoints = new List<Transform>();
            poser.OtherJoints = new List<Transform>();
            bool wristFound = false;

            Transform[] children = poser.GetComponentsInChildren<Transform>();

            int childCount = children.Length;
            for (int x = 0; x < childCount; x++) {
                Transform child = children[x];
                if(child == null || child.name == null || child == poser.transform) {
                    continue;
                }

                // Ignore this bone
                if(ShouldIgnoreJoint(child)) {
                    continue;
                }

                string formattedName = child.name.ToLower();

                // Assign Bones to appropriate containers
                if (formattedName.Contains("thumb")) {
                    poser.ThumbJoints.Add(child);
                }
                else if (formattedName.Contains("index")) {
                    poser.IndexJoints.Add(child);
                }
                else if (formattedName.Contains("middle")) {
                    poser.MiddleJoints.Add(child);
                }
                else if (formattedName.Contains("ring")) {
                    poser.RingJoints.Add(child);
                }
                else if (formattedName.Contains("pinky")) {
                    poser.PinkyJoints.Add(child);
                }
                else if(wristFound == false && formattedName.Contains("wrist") || (formattedName.EndsWith("hand") && child.childCount > 3)) {
                    poser.WristJoint = child;
                    wristFound = true;
                }
                else  {
                    poser.OtherJoints.Add(child);
                }
            }
        }

        public virtual bool ShouldIgnoreJoint(Transform theJoint) {
            if(!theJoint.gameObject.activeSelf) {
                return true; ;
            }

            // Don't store the handsmodel location. This is only for rendering
            string loweredName = theJoint.name.ToLower();
            if (loweredName == "handsmodel" || loweredName == "lhand" || loweredName == "rhand" || loweredName.StartsWith("tip_collider") || loweredName.StartsWith("hands_col")) {
                return true;
            }

            return false;
        }

        public void SaveHandPoseAs(HandPoser poser) {
            // Open Window
            HandPoseSaveAs window = (HandPoseSaveAs)EditorWindow.GetWindow(typeof(HandPoseSaveAs));
            window.ShowWindow(poser);
        }

        //public void LoadHandPose(string poseName) {

        //    var pose = Resources.Load<HandPose>(poseName);

        //    if (pose != null) {
        //        // Update position / rotations
        //        foreach (var f in pose.Joints.OtherJoints) {
        //            Transform thisT = FindChildTransformByName(f.TransformName);
        //            if (thisT) {
        //                thisT.localPosition = f.LocalPosition;
        //                thisT.localRotation = f.LocalRotation;
        //            }
        //        }

        //        poser.CurrentPose = pose;
        //        poser.PoseName = poseName;
        //    }
        //}

        public Transform FindChildTransformByName(string transformName) {

            Transform[] children = poser.GetComponentsInChildren<Transform>();

            int childCount = children.Length;
            for (int x = 0; x < childCount; x++) {
                Transform child = children[x];
                if (child != null && child.name == transformName) {
                    return child;
                }
            }

            return poser.transform.Find(transformName);
        }
    }
}

