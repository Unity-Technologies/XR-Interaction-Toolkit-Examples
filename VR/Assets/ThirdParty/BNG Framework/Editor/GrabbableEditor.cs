using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BNG {

    [CustomEditor(typeof(Grabbable))]
    [CanEditMultipleObjects]
    public class GrabbableEditor : Editor {

        /// <summary>
        /// Set this to false if you don't want to use the custom editor :)
        /// </summary>
        public bool UseCustomEditor = true;

        Grabbable grabbable;

        SerializedProperty grabButton;
        SerializedProperty grabType;
        SerializedProperty grabPhysics;
        SerializedProperty grabMechanic;
        SerializedProperty grabSpeed;
        SerializedProperty remoteGrabbable;
        SerializedProperty remoteGrabMechanic;
        SerializedProperty remoteGrabDistance;
        SerializedProperty throwForceMultiplier;
        SerializedProperty throwForceMultiplierAngular;
        SerializedProperty breakDistance;
        SerializedProperty hideHandGraphics;
        SerializedProperty parentToHands;
        SerializedProperty parentHandModel;
        SerializedProperty snapHandModel;
        SerializedProperty canBeDropped;
        SerializedProperty CanBeSnappedToSnapZone;
        SerializedProperty ForceDisableKinematicOnDrop;
        SerializedProperty InstantMovement;
        SerializedProperty MakeChildCollidersGrabbable;        
        SerializedProperty CustomHandPose;
        SerializedProperty handPoseType;
        SerializedProperty SelectedHandPose;
        SerializedProperty SecondaryGrabBehavior;
        SerializedProperty OtherGrabbableMustBeGrabbed;
        SerializedProperty SecondaryGrabbable;
        SerializedProperty SecondHandLookSpeed;
        SerializedProperty TwoHandedPosition;
        SerializedProperty TwoHandedPostionLerpAmount;
        SerializedProperty TwoHandedRotation;
        SerializedProperty TwoHandedRotationLerpAmount;
        SerializedProperty TwoHandedDropBehavior;
        SerializedProperty TwoHandedLookVector;
        SerializedProperty CollisionSpring;
        SerializedProperty CollisionSlerp;
        SerializedProperty CollisionLinearMotionX;
        SerializedProperty CollisionLinearMotionY;
        SerializedProperty CollisionLinearMotionZ;
        SerializedProperty CollisionAngularMotionX;
        SerializedProperty CollisionAngularMotionY;
        SerializedProperty CollisionAngularMotionZ;
        SerializedProperty ApplyCorrectiveForce;
        SerializedProperty MoveVelocityForce;
        SerializedProperty MoveAngularVelocityForce;
        SerializedProperty GrabPoints;
        SerializedProperty collisions;

        void OnEnable() {
            grabButton = serializedObject.FindProperty("GrabButton");
            grabType = serializedObject.FindProperty("Grabtype");
            grabPhysics = serializedObject.FindProperty("GrabPhysics");
            grabMechanic = serializedObject.FindProperty("GrabMechanic");
            grabSpeed = serializedObject.FindProperty("GrabSpeed");
            remoteGrabbable = serializedObject.FindProperty("RemoteGrabbable");
            remoteGrabDistance = serializedObject.FindProperty("RemoteGrabDistance");
            remoteGrabMechanic = serializedObject.FindProperty("RemoteGrabMechanic");
            throwForceMultiplier = serializedObject.FindProperty("ThrowForceMultiplier");
            throwForceMultiplierAngular = serializedObject.FindProperty("ThrowForceMultiplierAngular");
            breakDistance = serializedObject.FindProperty("BreakDistance");
            hideHandGraphics = serializedObject.FindProperty("HideHandGraphics");
            parentToHands = serializedObject.FindProperty("ParentToHands");
            parentHandModel = serializedObject.FindProperty("ParentHandModel");
            snapHandModel = serializedObject.FindProperty("SnapHandModel");
            canBeDropped = serializedObject.FindProperty("CanBeDropped");
            CanBeSnappedToSnapZone = serializedObject.FindProperty("CanBeSnappedToSnapZone");
            ForceDisableKinematicOnDrop = serializedObject.FindProperty("ForceDisableKinematicOnDrop");
            InstantMovement = serializedObject.FindProperty("InstantMovement");
            MakeChildCollidersGrabbable = serializedObject.FindProperty("MakeChildCollidersGrabbable");
            handPoseType = serializedObject.FindProperty("handPoseType");
            SelectedHandPose = serializedObject.FindProperty("SelectedHandPose");
            CustomHandPose = serializedObject.FindProperty("CustomHandPose");
            SecondaryGrabBehavior = serializedObject.FindProperty("SecondaryGrabBehavior");
            OtherGrabbableMustBeGrabbed = serializedObject.FindProperty("OtherGrabbableMustBeGrabbed");
            SecondaryGrabbable = serializedObject.FindProperty("SecondaryGrabbable");
            SecondHandLookSpeed = serializedObject.FindProperty("SecondHandLookSpeed");
            TwoHandedPosition = serializedObject.FindProperty("TwoHandedPosition");
            TwoHandedPostionLerpAmount = serializedObject.FindProperty("TwoHandedPostionLerpAmount");
            TwoHandedRotation = serializedObject.FindProperty("TwoHandedRotation");
            TwoHandedRotationLerpAmount = serializedObject.FindProperty("TwoHandedRotationLerpAmount");
            TwoHandedDropBehavior = serializedObject.FindProperty("TwoHandedDropBehavior");
            TwoHandedLookVector = serializedObject.FindProperty("TwoHandedLookVector");
            CollisionSpring = serializedObject.FindProperty("CollisionSpring");
            CollisionSlerp = serializedObject.FindProperty("CollisionSlerp");
            CollisionLinearMotionX = serializedObject.FindProperty("CollisionLinearMotionX");
            CollisionLinearMotionY = serializedObject.FindProperty("CollisionLinearMotionY");
            CollisionLinearMotionZ = serializedObject.FindProperty("CollisionLinearMotionZ");
            CollisionAngularMotionX = serializedObject.FindProperty("CollisionAngularMotionX");
            CollisionAngularMotionY = serializedObject.FindProperty("CollisionAngularMotionY");
            CollisionAngularMotionZ = serializedObject.FindProperty("CollisionAngularMotionZ");
            ApplyCorrectiveForce = serializedObject.FindProperty("ApplyCorrectiveForce");
            MoveVelocityForce = serializedObject.FindProperty("MoveVelocityForce");
            MoveAngularVelocityForce = serializedObject.FindProperty("MoveAngularVelocityForce");
            GrabPoints = serializedObject.FindProperty("GrabPoints");
            collisions = serializedObject.FindProperty("collisions");
        }

        public override void OnInspectorGUI() {

            grabbable = (Grabbable)target;

            // Don't use Custom Editor
            if (UseCustomEditor == false || grabbable.UseCustomInspector == false) {
                base.OnInspectorGUI();
                return;
            }

            // Add warning if scale is non uniform
            if (Mathf.Abs(grabbable.transform.localScale.x - grabbable.transform.localScale.y) > 0.001f) {
                EditorGUILayout.HelpBox("WARNING! Transform scale is non-uniform. It is recommended to keep object's transform scale to 1, 1, 1. \nIf you need to scale your object, scale the graphics as a child object and then resize your colliders to fit. \n\nVRIF can attempt to fix your Transform scale for you by clicking the 'Fix Transform Scale' button below. This will set this object's scale to 1,1,1 and then move your MeshRenderer to a child object with the original scale.", MessageType.Warning);

                if (GUILayout.Button("Fix Transform Scale")) {
                    FixScaling();
                }
            }

            EditorGUILayout.PropertyField(grabButton);
            EditorGUILayout.PropertyField(grabType, new GUIContent("Grab Type"));
            EditorGUILayout.PropertyField(grabMechanic);

            EditorGUILayout.PropertyField(grabPhysics);
            if (grabbable.GrabPhysics == GrabPhysics.PhysicsJoint) {
                EditorGUILayout.PropertyField(CollisionSpring);
                EditorGUILayout.PropertyField(CollisionSlerp);
                EditorGUILayout.PropertyField(CollisionLinearMotionX);
                EditorGUILayout.PropertyField(CollisionLinearMotionY);
                EditorGUILayout.PropertyField(CollisionLinearMotionZ);
                EditorGUILayout.PropertyField(CollisionAngularMotionX);
                EditorGUILayout.PropertyField(CollisionAngularMotionY);
                EditorGUILayout.PropertyField(CollisionAngularMotionZ);
            }

            if (grabbable.GrabPhysics == GrabPhysics.PhysicsJoint || grabbable.GrabPhysics == GrabPhysics.FixedJoint) {
                EditorGUILayout.PropertyField(ApplyCorrectiveForce);
            }

            if (grabbable.GrabPhysics == GrabPhysics.Velocity) {
                EditorGUILayout.PropertyField(MoveVelocityForce);
                EditorGUILayout.PropertyField(MoveAngularVelocityForce);
            }

            EditorGUILayout.PropertyField(throwForceMultiplier);
            EditorGUILayout.PropertyField(throwForceMultiplierAngular);
            EditorGUILayout.PropertyField(remoteGrabbable);
            EditorGUILayout.PropertyField(remoteGrabMechanic);

            if(grabbable.RemoteGrabMechanic == RemoteGrabMovement.Linear) {
                EditorGUILayout.PropertyField(grabSpeed);

            }
            else if (grabbable.RemoteGrabMechanic == RemoteGrabMovement.Flick) {
                //EditorGUILayout.PropertyField(flick);
            }

            EditorGUILayout.PropertyField(remoteGrabDistance);

            EditorGUILayout.PropertyField(hideHandGraphics);
            EditorGUILayout.PropertyField(parentToHands);
            EditorGUILayout.PropertyField(parentHandModel);
            EditorGUILayout.PropertyField(snapHandModel);

            EditorGUILayout.PropertyField(canBeDropped);
            EditorGUILayout.PropertyField(CanBeSnappedToSnapZone);
            EditorGUILayout.PropertyField(ForceDisableKinematicOnDrop);
            EditorGUILayout.PropertyField(InstantMovement);
            EditorGUILayout.PropertyField(MakeChildCollidersGrabbable);
            EditorGUILayout.PropertyField(breakDistance);

            EditorGUILayout.PropertyField(handPoseType);

            if(grabbable.handPoseType == HandPoseType.HandPose) {
                EditorGUILayout.PropertyField(SelectedHandPose);
            }
            else if (grabbable.handPoseType == HandPoseType.AnimatorID) {
                EditorGUILayout.PropertyField(CustomHandPose);
            }
            
            EditorGUILayout.PropertyField(SecondaryGrabBehavior);

            // Two-Handed Settings
            if(grabbable.SecondaryGrabBehavior == OtherGrabBehavior.DualGrab) {

                EditorGUILayout.PropertyField(TwoHandedPosition);
                if(grabbable.TwoHandedPosition == TwoHandedPositionType.Lerp) {
                    EditorGUILayout.PropertyField(TwoHandedPostionLerpAmount);
                }

                EditorGUILayout.PropertyField(TwoHandedRotation);
                if (grabbable.TwoHandedRotation == TwoHandedRotationType.Lerp || grabbable.TwoHandedRotation == TwoHandedRotationType.Slerp) {
                    EditorGUILayout.PropertyField(TwoHandedRotationLerpAmount);
                }
                else if (grabbable.TwoHandedRotation == TwoHandedRotationType.LookAtSecondary) {
                    EditorGUILayout.PropertyField(TwoHandedLookVector);
                    EditorGUILayout.PropertyField(SecondHandLookSpeed);
                }
                                    
                EditorGUILayout.PropertyField(TwoHandedDropBehavior);
                EditorGUILayout.PropertyField(SecondaryGrabbable);
            }

            EditorGUILayout.PropertyField(OtherGrabbableMustBeGrabbed);

            EditorGUILayout.PropertyField(GrabPoints);

            // Grab Point Button
            if(GUILayout.Button("Auto Populate Grab Points")) {
                AutoPopulateGrabPoints();
            }

            // Only show Debug Fields when playing in editor
            if (Application.isPlaying) {
                EditorGUILayout.PropertyField(collisions);
            }
            
            serializedObject.ApplyModifiedProperties();
        }

        public void AutoPopulateGrabPoints() {
            if(grabbable) {
                var newPoints = new List<Transform>();

                foreach(var gp in grabbable.GetComponentsInChildren<GrabPoint>()) {
                    if(gp != null && gp.gameObject.activeInHierarchy) {
                        newPoints.Add(gp.transform);
                    }
                }

                grabbable.GrabPoints = newPoints;
            }
        }

        public void FixScaling() {
            if(grabbable != null) {
                grabbable.gameObject.AddComponent<FixNonUniformScale>();
            }
        }

        [MenuItem("GameObject/VRIF/Grabbable", false, 10)]
        private static void AddGrabbable(MenuCommand menuCommand) {
            // Create and add a new Grabbable Object in the Scene
            GameObject grab = Instantiate(Resources.Load("DefaultGrabbableItem", typeof(GameObject))) as GameObject;
            grab.name = "Grabbable Object";

            GameObjectUtility.SetParentAndAlign(grab, menuCommand.context as GameObject);

            Undo.RegisterCreatedObjectUndo(grab, "Created Grabbable " + grab.name);
            Selection.activeObject = grab;
        }

        [MenuItem("GameObject/VRIF/EventSystem", false, 20)]
        private static void AddEventSystem(MenuCommand menuCommand) {
            // Create and add a new Grabbable Object in the Scene
            GameObject eventSystem = Instantiate(Resources.Load("DefaultEventSystem", typeof(GameObject))) as GameObject;
            eventSystem.name = "EventSystem";

            GameObjectUtility.SetParentAndAlign(eventSystem, menuCommand.context as GameObject);

            Undo.RegisterCreatedObjectUndo(eventSystem, "Created EventSystem " + eventSystem.name);
            Selection.activeObject = eventSystem;
        }
    }
}
