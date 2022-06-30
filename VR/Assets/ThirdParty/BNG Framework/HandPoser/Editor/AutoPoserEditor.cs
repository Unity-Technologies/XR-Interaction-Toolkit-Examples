using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace BNG {

    [CustomEditor(typeof(AutoPoser))]
    [CanEditMultipleObjects]
    public class AutoPoserEditor : Editor {

       
        // Auto Pose properties
        SerializedProperty openHandPose;
        SerializedProperty closedHandPose;
        SerializedProperty autoUpdateAutoPose;
        SerializedProperty idleHandPose;
        
        SerializedProperty fingerTipRadius;
        SerializedProperty collisionLayerMask;
        SerializedProperty showGizmos;

        SerializedProperty gizmoType;
        SerializedProperty gizmoColor;

        SerializedProperty thumbCollider;
        SerializedProperty indexFingerCollider;
        SerializedProperty middleFingerCollider;
        SerializedProperty ringFingerCollider;
        SerializedProperty pinkyFingerCollider;

        AutoPoser poser;
        bool showGizmoProps;

        bool showColliderOffsets;

        void OnEnable() {
            openHandPose = serializedObject.FindProperty("OpenHandPose");
            closedHandPose = serializedObject.FindProperty("ClosedHandPose");
            autoUpdateAutoPose = serializedObject.FindProperty("UpdateContinuously");
            idleHandPose = serializedObject.FindProperty("IdleHandPose");
            fingerTipRadius = serializedObject.FindProperty("FingerTipRadius");
            collisionLayerMask = serializedObject.FindProperty("CollisionLayerMask");
            showGizmos = serializedObject.FindProperty("ShowGizmos");
            gizmoType = serializedObject.FindProperty("GizmoType");
            gizmoColor = serializedObject.FindProperty("GizmoColor");
            thumbCollider = serializedObject.FindProperty("ThumbCollider");
            indexFingerCollider = serializedObject.FindProperty("IndexFingerCollider");
            middleFingerCollider = serializedObject.FindProperty("MiddleFingerCollider");
            ringFingerCollider = serializedObject.FindProperty("RingFingerCollider");
            pinkyFingerCollider = serializedObject.FindProperty("PinkyFingerCollider");
        }


        public override void OnInspectorGUI() {
            poser = (AutoPoser)target;

            serializedObject.Update();

            GUILayout.Label("Auto Pose", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(openHandPose);
            EditorGUILayout.PropertyField(closedHandPose);
            EditorGUILayout.PropertyField(idleHandPose);
            EditorGUILayout.PropertyField(collisionLayerMask);
            EditorGUILayout.PropertyField(fingerTipRadius);

            showColliderOffsets = EditorGUILayout.Foldout(showColliderOffsets, "Finger Tip Offsets");
            if (showColliderOffsets) {
                EditorGUILayout.HelpBox("You can manually adjust each fingertip's position / scale by specifying a FingerTipCollider object below. Press the 'Auto Add Tip Colliders' to create and populate the objects for you.", MessageType.Info);

                if (GUILayout.Button("Auto Setup Finger Tip Colliders")) {
                    AutoAddFingerTipColliders(poser);
                }

                EditorGUILayout.PropertyField(thumbCollider);
                EditorGUILayout.PropertyField(indexFingerCollider);
                EditorGUILayout.PropertyField(middleFingerCollider);
                EditorGUILayout.PropertyField(ringFingerCollider);
                EditorGUILayout.PropertyField(pinkyFingerCollider);
            }

            EditorGUILayout.Separator();

            EditorGUILayout.PropertyField(autoUpdateAutoPose);


            EditorGUILayout.Separator();

            if (GUILayout.Button("Auto Pose")) {
                poser.UpdateAutoPose(false);
            }

            // GUILayout.Label("Editor Gizmos", EditorStyles.boldLabel);
            showGizmoProps = EditorGUILayout.Foldout(showGizmoProps, "Editor Gizmos");
            if (showGizmoProps) {
                EditorGUILayout.PropertyField(showGizmos);
                EditorGUILayout.PropertyField(gizmoType);
                EditorGUILayout.PropertyField(gizmoColor);
            }

            serializedObject.ApplyModifiedProperties();
        }

        public void AutoAddFingerTipColliders(AutoPoser poser) {
            poser.ThumbCollider = GetOrAddTipCollider(poser.InspectedPose.GetThumbTip(), "tip_collider_t");
            poser.IndexFingerCollider = GetOrAddTipCollider(poser.InspectedPose.GetIndexTip(), "tip_collider_i");
            poser.MiddleFingerCollider = GetOrAddTipCollider(poser.InspectedPose.GetMiddleTip(), "tip_collider_m");
            poser.RingFingerCollider = GetOrAddTipCollider(poser.InspectedPose.GetRingTip(), "tip_collider_r");
            poser.PinkyFingerCollider = GetOrAddTipCollider(poser.InspectedPose.GetPinkyTip(), "tip_collider_p");
        }

        public FingerTipCollider GetOrAddTipCollider(Transform tipTransform, string tipName) {

            if (tipTransform != null) {


                // Check for existing and return that if available
                FingerTipCollider col = tipTransform.GetComponentInChildren<FingerTipCollider>();
                if(col != null) {
                    return col;
                }

                // Otherwise create a new one and parent it to the tip of the finger
                GameObject tipCollider = new GameObject(tipName);
                col = tipCollider.AddComponent<FingerTipCollider>();

                tipCollider.transform.parent = tipTransform;
                tipCollider.transform.localPosition = Vector3.zero;
                tipCollider.transform.localEulerAngles = Vector3.zero;
                tipCollider.transform.localScale = Vector3.one;

                return col;

            }

            return null;
        }
    }
}

