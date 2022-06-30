using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BNG {

    [CustomEditor(typeof(GrabPoint))]
    [CanEditMultipleObjects]
    public class GrabPointEditor : Editor {

        public GameObject LeftHandPreview;
        bool showingLeftHand = false;

        public GameObject RightHandPreview;
        bool showingRightHand = false;

        // Define a texture and GUIContent
        private Texture buttonLeftTexture;
        private Texture buttonLeftTextureSelected;
        private GUIContent buttonLeftContent;

        private Texture buttonRightTexture;
        private Texture buttonRightTextureSelected;

        private GUIContent buttonRightContent;

        GrabPoint grabPoint;

        SerializedProperty handPoseType;
        SerializedProperty SelectedHandPose;
        SerializedProperty HandPose;
        SerializedProperty LeftHandIsValid;
        SerializedProperty RightHandIsValid;
        SerializedProperty HandPosition;
        SerializedProperty MaxDegreeDifferenceAllowed;
        SerializedProperty IndexBlendMin;
        SerializedProperty IndexBlendMax;
        SerializedProperty ThumbBlendMin;
        SerializedProperty ThumbBlendMax;
        SerializedProperty ShowAngleGizmo;

        void OnEnable() {
            handPoseType = serializedObject.FindProperty("handPoseType");
            SelectedHandPose = serializedObject.FindProperty("SelectedHandPose");
            HandPose = serializedObject.FindProperty("HandPose");
            LeftHandIsValid = serializedObject.FindProperty("LeftHandIsValid");
            RightHandIsValid = serializedObject.FindProperty("RightHandIsValid");
            HandPosition = serializedObject.FindProperty("HandPosition");
            MaxDegreeDifferenceAllowed = serializedObject.FindProperty("MaxDegreeDifferenceAllowed");
            IndexBlendMin = serializedObject.FindProperty("IndexBlendMin");
            IndexBlendMax = serializedObject.FindProperty("IndexBlendMax");
            ThumbBlendMin = serializedObject.FindProperty("ThumbBlendMin");
            ThumbBlendMax = serializedObject.FindProperty("ThumbBlendMax");
            ShowAngleGizmo = serializedObject.FindProperty("ShowAngleGizmo");
        }

        HandPoseType previousType;

        

        public override void OnInspectorGUI() {

            grabPoint = (GrabPoint)target;
            bool inPrefabMode = false;
#if UNITY_EDITOR
            inPrefabMode = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null;
#endif

            // Double check that there wasn't an object left in the scene
            checkForExistingPreview(); 

            // Check for change in handpose type
            if(grabPoint.handPoseType != previousType) {
                OnHandPoseTypeChange();
            }

            // Load the texture resource
            if (buttonLeftTexture == null) {
                buttonLeftTexture = (Texture)Resources.Load("handIcon", typeof(Texture));
                buttonLeftTextureSelected = (Texture)Resources.Load("handIconSelected", typeof(Texture));
                buttonRightTexture = (Texture)Resources.Load("handIconRight", typeof(Texture));
                buttonRightTextureSelected = (Texture)Resources.Load("handIconSelectedRight", typeof(Texture));
            }


            GUILayout.Label("Toggle Hand Preview : ", EditorStyles.boldLabel);

            if(inPrefabMode) {
                GUILayout.Label("(Some preview features disabled in prefab mode)", EditorStyles.largeLabel);
            }

            GUILayout.BeginHorizontal();

            // Show / Hide Left Hand
            if (showingLeftHand) {

                // Define a GUIContent which uses the texture
                buttonLeftContent = new GUIContent(buttonLeftTextureSelected);

                if (!grabPoint.LeftHandIsValid || GUILayout.Button(buttonLeftContent)) {
                    GameObject.DestroyImmediate(LeftHandPreview);
                    showingLeftHand = false;
                }
            }
            else {
                buttonLeftContent = new GUIContent(buttonLeftTexture);

                if (grabPoint.LeftHandIsValid && GUILayout.Button(buttonLeftContent)) {
                    // Create and add the Editor preview
                    CreateLeftHandPreview();
                    
                }
            }

            // Show / Hide Right Hand
            if (showingRightHand) {

                // Define a GUIContent which uses the texture
                buttonRightContent = new GUIContent(buttonRightTextureSelected);

                if (!grabPoint.RightHandIsValid || GUILayout.Button(buttonRightContent)) {
                    GameObject.DestroyImmediate(RightHandPreview);
                    showingRightHand = false;
                }
            }
            else {
                buttonRightContent = new GUIContent(buttonRightTexture);

                if (grabPoint.RightHandIsValid && GUILayout.Button(buttonRightContent)) {
                    CreateRightHandPreview();
                }
            }

            GUILayout.EndHorizontal();

            updateEditorAnimation();

            EditorGUILayout.PropertyField(LeftHandIsValid);
            EditorGUILayout.PropertyField(RightHandIsValid);

            EditorGUILayout.PropertyField(handPoseType);

            if(grabPoint.handPoseType == HandPoseType.HandPose) {
                EditorGUILayout.PropertyField(SelectedHandPose);

                GUILayout.BeginHorizontal();

                EditorGUILayout.LabelField("");
                EditorGUILayout.Space(0, true);

                if (GUILayout.Button("Edit Pose...")) {
                    EditHandPose();
                }
                GUILayout.EndHorizontal();

            }
            else if (grabPoint.handPoseType == HandPoseType.AnimatorID) {
                EditorGUILayout.PropertyField(HandPose);
            }
            
            //EditorGUILayout.PropertyField(HandPosition);
            EditorGUILayout.PropertyField(MaxDegreeDifferenceAllowed);
            EditorGUILayout.PropertyField(IndexBlendMin);
            EditorGUILayout.PropertyField(IndexBlendMax);
            EditorGUILayout.PropertyField(ThumbBlendMin);
            EditorGUILayout.PropertyField(ThumbBlendMax);
            EditorGUILayout.PropertyField(ShowAngleGizmo);

            serializedObject.ApplyModifiedProperties();
            // base.OnInspectorGUI();
        }

        public void OnHandPoseTypeChange() {
            if(grabPoint.handPoseType == HandPoseType.HandPose) {
                UpdateHandPosePreview();
            }
            previousType = grabPoint.handPoseType;
        }

        public void CreateRightHandPreview() {
            // Create and add the Editor preview
            RightHandPreview = Instantiate(Resources.Load("RightHandModelsEditorPreview", typeof(GameObject))) as GameObject;
            RightHandPreview.transform.name = "RightHandModelsEditorPreview";
            RightHandPreview.transform.parent = grabPoint.transform;
            RightHandPreview.transform.localPosition = Vector3.zero;
            RightHandPreview.transform.localEulerAngles = Vector3.zero;
            RightHandPreview.gameObject.hideFlags = HideFlags.HideAndDontSave;
            //RightHandPreview.gameObject.hideFlags = HideFlags.DontSave;

#if UNITY_EDITOR
            if (grabPoint != null) {
                grabPoint.UpdatePreviews();
            }
#endif
            showingRightHand = true;
        }

        public void CreateLeftHandPreview() {
            LeftHandPreview = Instantiate(Resources.Load("LeftHandModelsEditorPreview", typeof(GameObject))) as GameObject;
            LeftHandPreview.transform.name = "LeftHandModelsEditorPreview";
            LeftHandPreview.transform.parent = grabPoint.transform;
            LeftHandPreview.transform.localPosition = Vector3.zero;
            LeftHandPreview.transform.localEulerAngles = Vector3.zero;
            LeftHandPreview.gameObject.hideFlags = HideFlags.HideAndDontSave;
            // LeftHandPreview.gameObject.hideFlags = HideFlags.DontSave;

#if UNITY_EDITOR
            if (grabPoint != null) {
                grabPoint.UpdatePreviews();
            }
#endif
            showingLeftHand = true;
        }

        public void EditHandPose() {
            // Select the Hand Object
            if(grabPoint.RightHandIsValid) {
                if(!showingRightHand) {
                    CreateRightHandPreview();
                }

                RightHandPreview.gameObject.hideFlags = HideFlags.DontSave;
                HandPoser hp = RightHandPreview.gameObject.GetComponentInChildren<HandPoser>();
                if(hp != null) {
                    Selection.activeGameObject = hp.gameObject;
                }
            }
            else if (grabPoint.LeftHandIsValid) {
                if (!showingLeftHand) {
                    CreateLeftHandPreview();
                }

                LeftHandPreview.gameObject.hideFlags = HideFlags.DontSave;
                HandPoser hp = LeftHandPreview.gameObject.GetComponentInChildren<HandPoser>();
                if (hp != null) {
                    Selection.activeGameObject = hp.gameObject;
                }
            }
            else {
                Debug.Log("No HandPoser component was found on hand preview prefab. You may need to add one to 'Resources/RightHandModelsEditorPreview'.");
            }
        }

        void updateEditorAnimation() {

            if (LeftHandPreview) {
                var anim = LeftHandPreview.GetComponentInChildren<Animator>();
                updateAnimator(anim, (int)grabPoint.HandPose);
            }

            if (RightHandPreview) {
                var anim = RightHandPreview.GetComponentInChildren<Animator>();

                updateAnimator(anim, (int)grabPoint.HandPose);
            }
        }

        public void UpdateHandPosePreview() {
            if (LeftHandPreview) {
                var hp = LeftHandPreview.GetComponentInChildren<HandPoser>();
                if(hp) {
                    // Trigger a change
                    hp.OnPoseChanged();
                }
            }
            if (RightHandPreview) {
                var hp = RightHandPreview.GetComponentInChildren<HandPoser>();
                if (hp) {
                    hp.OnPoseChanged();
                }
            }
        }

        void updateAnimator(Animator anim, int handPose) {
            if (anim != null && anim.isActiveAndEnabled && anim.gameObject.activeSelf) {

                // Do Fist Pose
                if (handPose == 0) {

                    // 0 = Hands Open, 1 = Grip closes                        
                    anim.SetFloat("Flex", 1);

                    anim.SetLayerWeight(0, 1);
                    anim.SetLayerWeight(1, 0);
                    anim.SetLayerWeight(2, 0);
                }
                else {
                    anim.SetLayerWeight(0, 0);
                    anim.SetLayerWeight(1, 0);
                    anim.SetLayerWeight(2, 0);
                }

                anim.SetInteger("Pose", handPose);
                anim.Update(Time.deltaTime);

#if UNITY_EDITOR
                // Only set dirty if not in prefab mode
                if(UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() == null) {
                    UnityEditor.EditorUtility.SetDirty(anim.gameObject);
                }
#endif
            }
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/VRIF/GrabPoint", false, 11)]
        private static void AddGrabbable(MenuCommand menuCommand) {
            // Create and add a new Grabbable Object in the Scene
            GameObject grabPoint = Instantiate(Resources.Load("DefaultGrabPointItem", typeof(GameObject))) as GameObject;
            grabPoint.name = "GrabPoint";

            GameObjectUtility.SetParentAndAlign(grabPoint, menuCommand.context as GameObject);

            Undo.RegisterCreatedObjectUndo(grabPoint, "Created GrabPoint " + grabPoint.name);
            Selection.activeObject = grabPoint;
        }
#endif
        void checkForExistingPreview() {
            if (LeftHandPreview == null && !showingLeftHand) {
                Transform lt = grabPoint.transform.Find("LeftHandModelsEditorPreview");
                if (lt) {
                    LeftHandPreview = lt.gameObject;
                    showingLeftHand = true;
                }
            }

            if (RightHandPreview == null && !showingRightHand) {
                Transform rt = grabPoint.transform.Find("RightHandModelsEditorPreview");
                if (rt) {
                    RightHandPreview = rt.gameObject;
                    showingRightHand = true;
                }
            }
        }
    }
}

