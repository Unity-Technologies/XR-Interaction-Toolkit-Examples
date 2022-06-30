using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BNG {

    [CustomEditor(typeof(Lever))]
    public class LeverEditor : Editor {

        SerializedProperty minimumXRotation;
        SerializedProperty maximumXRotation;

        void OnEnable() {
            minimumXRotation = serializedObject.FindProperty("MinimumXRotation");
            maximumXRotation = serializedObject.FindProperty("MaximumXRotation");
        }

        public override void OnInspectorGUI() {

            Lever lever = (Lever)target;

            float initialMin = lever.MinimumXRotation;
            float initialMax = lever.MaximumXRotation;

            GUILayout.Label("Min / Max Angle Settings ", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(minimumXRotation);
            EditorGUILayout.PropertyField(maximumXRotation);

            GUILayout.BeginHorizontal();

            EditorGUILayout.MinMaxSlider(ref lever.MinimumXRotation, ref lever.MaximumXRotation, -180, 180);

            GUILayout.EndHorizontal();

            GUILayout.Label("Starting Value", EditorStyles.boldLabel);
            EditorGUILayout.Slider(serializedObject.FindProperty("InitialXRotation"), lever.MinimumXRotation, lever.MaximumXRotation);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("SwitchTolerance"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SwitchOnSound"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SwitchOffSound"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("UseSmoothLook"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SmoothLookSpeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("AllowPhysicsForces"));
            
            if(lever.ReturnToCenter == true && lever.AllowPhysicsForces) {
                EditorGUILayout.HelpBox("Make sure 'AllowPhysicsForces' is set to FALSE for ReturnToCenter to work properly.", MessageType.Warning);
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ReturnToCenter"));


            EditorGUILayout.PropertyField(serializedObject.FindProperty("ReturnLookSpeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SnapToGrabber"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("DropLeverOnActivation"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("LeverPercentage"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ShowEditorGizmos"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onLeverDown"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onLeverUp"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onLeverChange"));

            serializedObject.ApplyModifiedProperties();
            
            if (!Application.isPlaying) {
                // Make Sure initial x rotation is within range
                if(lever.InitialXRotation < lever.MinimumXRotation) {
                    lever.InitialXRotation = lever.MinimumXRotation;
                }
                else if (lever.InitialXRotation > lever.MaximumXRotation) {
                    lever.InitialXRotation = lever.MaximumXRotation;
                }
            }

            bool hingeChanged = lever.MinimumXRotation != initialMin || lever.MaximumXRotation != initialMax;
            if (hingeChanged) {
                // Update hingeJoint if it's available
                HingeJoint hinge = lever.GetComponent<HingeJoint>();
                if (hinge != null) {
                    JointLimits jl = hinge.limits;
                    jl.min = lever.MinimumXRotation;
                    jl.max = lever.MaximumXRotation;
                    hinge.limits = jl;
                }
            }
            else {
                // Check that the hinge wasn't changed
                HingeJoint hinge = lever.GetComponent<HingeJoint>();
                if (hinge != null) {
                    JointLimits jl = hinge.limits;
                    if(lever.MinimumXRotation != jl.min) {
                        lever.MinimumXRotation = jl.min;
                    }
                    if(lever.MaximumXRotation != jl.max) {
                        lever.MaximumXRotation = jl.max;
                    }
                }
            }
        }
    }
}

