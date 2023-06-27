using UnityEditor;
using UnityEngine;
using VRBuilder.XRInteraction;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRBuilder.Editor.XRInteraction
{
    /// <summary>
    /// Drawer class for <see cref="RayInteractor"/>.
    /// </summary>
    [CustomEditor(typeof(RayInteractor))]
    [CanEditMultipleObjects]
    internal class RayInteractorEditor : UnityEditor.Editor
    {
        private SerializedProperty interactionManager;
        private SerializedProperty interactionLayerMask;
        private SerializedProperty attachTransform;
        private SerializedProperty startingSelectedInteractable;
        private SerializedProperty selectActionTrigger;
        private SerializedProperty hideControllerOnSelect;

        private SerializedProperty playAudioClipOnSelectEntered;
        private SerializedProperty audioClipForOnSelectEntered;
        private SerializedProperty playAudioClipOnSelectExited;
        private SerializedProperty audioClipForOnSelectExited;
        private SerializedProperty playAudioClipOnHoverEntered;
        private SerializedProperty audioClipForOnHoverEntered;
        private SerializedProperty playAudioClipOnHoverExited;
        private SerializedProperty audioClipForOnHoverExited;

        private SerializedProperty playHapticsOnSelectEntered;
        private SerializedProperty hapticSelectEnterIntensity;
        private SerializedProperty hapticSelectEnterDuration;
        private SerializedProperty playHapticsOnHoverEntered;
        private SerializedProperty hapticHoverEnterIntensity;
        private SerializedProperty hapticHoverEnterDuration;
        private SerializedProperty playHapticsOnSelectExited;
        private SerializedProperty hapticSelectExitIntensity;
        private SerializedProperty hapticSelectExitDuration;
        private SerializedProperty playHapticsOnHoverExited;
        private SerializedProperty hapticHoverExitIntensity;
        private SerializedProperty hapticHoverExitDuration;

        private SerializedProperty maxRaycastDistance;
        private SerializedProperty hitDetectionType;
        private SerializedProperty sphereCastRadius;        
        private SerializedProperty raycastMask;
        private SerializedProperty raycastTriggerInteraction;
        private SerializedProperty hoverToSelect;
        private SerializedProperty hoverTimeToSelect;
        private SerializedProperty enableUIInteraction;

        private SerializedProperty lineType;
        private SerializedProperty endPointDistance;
        private SerializedProperty endPointHeight;
        private SerializedProperty controlPointDistance;
        private SerializedProperty controlPointHeight;
        private SerializedProperty sampleFrequency;

        private SerializedProperty velocity;
        private SerializedProperty acceleration;
        private SerializedProperty additionalFlightTime;
        private SerializedProperty referenceFrame;

        private SerializedProperty onHoverEntered;
        private SerializedProperty onHoverExited;
        private SerializedProperty onSelectEntered;
        private SerializedProperty onSelectExited;
        
        private SerializedProperty keepSelectedTargetValid;
        private SerializedProperty allowAnchorControl;
        private SerializedProperty useForceGrab;
        private SerializedProperty anchorRotateSpeed;
        private SerializedProperty anchorTranslateSpeed;

        private static class Tooltips
        {
            public static readonly GUIContent InteractionManager = new GUIContent("Interaction Manager", "Manager to handle all interaction management (will find one if empty).");
            public static readonly GUIContent InteractionLayerMask = new GUIContent("Interaction Layer Mask", "Only interactables with this Layer Mask will respond to this interactor.");
            public static readonly GUIContent AttachTransform = new GUIContent("Attach Transform", "Attach Transform to use for this Interactor.  Will create empty GameObject if none set.");
            public static readonly GUIContent StartingSelectedInteractable = new GUIContent("Starting Selected Interactable", "Interactable that will be selected upon start.");
            public static readonly GUIContent SelectActionTrigger = new GUIContent("Select Action Trigger", "Choose how the select action is triggered by current state, state transitions, toggle when the select button is pressed, or [Sticky] toggle on when the select button is pressed and off the second time the select button is depressed.");
            public static readonly GUIContent HideControllerOnSelect = new GUIContent("Hide Controller On Select", "Hide controller on select.");

            public static readonly GUIContent PlayAudioClipOnSelectEntered = new GUIContent("On Select Entered", "Play an audio clip when the Select state is entered.");
            public static readonly GUIContent AudioClipForOnSelectEntered = new GUIContent("AudioClip To Play", "The audio clip to play when the Select state is entered.");
            public static readonly GUIContent PlayAudioClipOnSelectExited = new GUIContent("On Select Exited", "Play an audio clip when the Select state is exited.");
            public static readonly GUIContent AudioClipForOnSelectExited = new GUIContent("AudioClip To Play", "The audio clip to play when the Select state is exited.");
            public static readonly GUIContent PlayAudioClipOnHoverEntered = new GUIContent("On Hover Entered", "Play an audio clip when the Hover state is entered.");
            public static readonly GUIContent AudioClipForOnHoverEntered = new GUIContent("AudioClip To Play", "The audio clip to play when the Hover state is entered.");
            public static readonly GUIContent PlayAudioClipOnHoverExited = new GUIContent("On Hover Exited", "Play an audio clip when the Hover state is exited.");
            public static readonly GUIContent AudioClipForOnHoverExited = new GUIContent("AudioClip To Play", "The audio clip to play when the Hover state is exited.");

            public static readonly GUIContent PlayHapticsOnSelectEntered = new GUIContent("On Select Entered", "Play haptics when the Select state is entered.");
            public static readonly GUIContent HapticSelectEnterIntensity = new GUIContent("Haptic Intensity", "Haptics intensity to play when the Select state is entered.");
            public static readonly GUIContent HapticSelectEnterDuration = new GUIContent("Duration", "Haptics duration (in seconds) to play when the Select state is entered.");
            public static readonly GUIContent PlayHapticsOnHoverEntered = new GUIContent("On Hover Entered", "Play haptics when the Hover State is entered.");
            public static readonly GUIContent HapticHoverEnterIntensity = new GUIContent("Haptic Intensity", "Haptics intensity to play when the Hover state is entered.");
            public static readonly GUIContent HapticHoverEnterDuration = new GUIContent("Duration", "Haptics duration (in seconds) to play when the Hover state is entered.");
            public static readonly GUIContent PlayHapticsOnSelectExited = new GUIContent("On Select Exited", "Play haptics when the Select state is exited.");
            public static readonly GUIContent HapticSelectExitIntensity = new GUIContent("Haptic Intensity", "Haptics intensity to play when the Select state is exited.");
            public static readonly GUIContent HapticSelectExitDuration = new GUIContent("Duration", "Haptics duration (in seconds) to play when the Select state is exited.");
            public static readonly GUIContent PlayHapticsOnHoverExited = new GUIContent("On Hover Exited", "Play haptics when the Hover state is exited.");
            public static readonly GUIContent HapticHoverExitIntensity = new GUIContent("Haptic Intensity", "Haptics intensity to play when the Hover state is exited.");
            public static readonly GUIContent HapticHoverExitDuration = new GUIContent("Duration", "Haptics duration (in seconds) to play when the Hover state is exited.");

            public static readonly GUIContent MaxRaycastDistance = new GUIContent("Max Raycast Distance", "Max distance of ray cast. Increase this value will let you reach further.");
            public static readonly GUIContent SphereCastRadius = new GUIContent("Sphere Cast Radius", "Radius of this Interactor's ray, used for sphere casting.");
            public static readonly GUIContent RaycastMask = new GUIContent("Raycast Mask", "Layer mask used for limiting raycast targets.");
            public static readonly GUIContent RaycastTriggerInteraction = new GUIContent("Raycast Trigger Interaction", "Type of interaction with trigger colliders via raycast.");
            public static readonly GUIContent HoverToSelect = new GUIContent("Hover To Select", "If true, this interactor will simulate a Select event if hovered over an Interactable for some amount of time. Selection will be exited when the Interactor is no longer hovering over the Interactable.");
            public static readonly GUIContent EoverTimeToSelect = new GUIContent("Hover Time To Select", "Number of seconds for which this interactor must hover over an object to select it.");
            public static readonly GUIContent EnableUIInteraction = new GUIContent("Enable Interaction with UI GameObjects", "If checked, this interactor will be able to affect UI.");
            public static readonly GUIContent LineType = new GUIContent("Line Type", "Line type of the ray cast.");
            public static readonly GUIContent EndPointDistance = new GUIContent("End Point Distance", "Increase this value distance will make the end of curve further from the start point.");
            public static readonly GUIContent ControlPointDistance = new GUIContent("Control Point Distance", "Increase this value will make the peak of the curve further from the start point.");
            public static readonly GUIContent EndPointHeight = new GUIContent("End Point Height", "Decrease this value will make the end of the curve drop lower relative to the start point.");
            public static readonly GUIContent ControlPointHeight = new GUIContent("Control Point Height", "Increase this value will make the peak of the curve higher relative to the start point.");
            public static readonly GUIContent SampleFrequency = new GUIContent("Sample Frequency", "Gets or sets the number of sample points of the curve, should be at least 3, the higher the better quality.");
            public static readonly GUIContent Velocity = new GUIContent("Velocity", "Initial velocity of the projectile. Increase this value will make the curve reach further.");
            public static readonly GUIContent Acceleration = new GUIContent("Acceleration", "Gravity of the projectile in the reference frame.");
            public static readonly GUIContent AdditionalFlightTime = new GUIContent("Additional Flight Time", "Additional flight time after the projectile lands at the same height of the start point in the tracking space. Increase this value will make the end point drop lower in height.");
            public static readonly GUIContent ReferenceFrame = new GUIContent("Reference Frame", "The reference frame of the projectile. If not set it will try to find the XRRig GameObject, and if that does not exist it will use its own Transform.");
            public static readonly GUIContent HitDetectionType = new GUIContent("Hit Detection Type", "The type of hit detection used to hit interactable objects.");

            public static readonly GUIContent KeepSelectedTargetValid = new GUIContent("Keep Selected Target Valid", "Keep selecting the target when not pointing to it after initially selecting it. It is recommended to set this value to true for grabbing objects, false for teleportation interactables.");
            public static readonly GUIContent AllowAnchorControl = new GUIContent("Anchor Control", "Allows the user to move the attach anchor point using the joystick.");
            public static readonly GUIContent ForceGrab = new GUIContent("Force Grab", "Force grab moves the object to your hand rather than interacting with it at a distance.");
            public static readonly GUIContent AnchorRotateSpeed = new GUIContent("Rotate Speed", "Speed that the anchor is rotated.");
            public static readonly GUIContent AnchorTranslateSpeed = new GUIContent("Translate Speed", "Speed that the anchor is translated.");

            public static readonly string StartingInteractableWarning = "A Starting Selected Interactable will be instantly deselected unless the Interactor's Toggle Select Mode is set to 'Toggle' or 'Sticky'.";
            public static readonly string MissingRequiredController = "XR Ray Interactor requires the GameObject to have an XR Controller component. Add one to ensure this component can respond to user input.";
        }

        private void OnEnable()
        {
            interactionManager = serializedObject.FindProperty("m_InteractionManager");
            interactionLayerMask = serializedObject.FindProperty("m_InteractionLayers");
            attachTransform = serializedObject.FindProperty("m_AttachTransform");
            startingSelectedInteractable = serializedObject.FindProperty("m_StartingSelectedInteractable");
            selectActionTrigger = serializedObject.FindProperty("m_SelectActionTrigger");
            hideControllerOnSelect = serializedObject.FindProperty("m_HideControllerOnSelect");

            maxRaycastDistance = serializedObject.FindProperty("m_MaxRaycastDistance");
            sphereCastRadius = serializedObject.FindProperty("m_SphereCastRadius");
            hitDetectionType = serializedObject.FindProperty("m_HitDetectionType");
            raycastMask = serializedObject.FindProperty("m_RaycastMask");
            raycastTriggerInteraction = serializedObject.FindProperty("m_RaycastTriggerInteraction");
            hoverToSelect = serializedObject.FindProperty("m_HoverToSelect");
            hoverTimeToSelect = serializedObject.FindProperty("m_HoverTimeToSelect");
            enableUIInteraction = serializedObject.FindProperty("m_EnableUIInteraction");

            lineType = serializedObject.FindProperty("m_LineType");
            endPointDistance = serializedObject.FindProperty("m_EndPointDistance");
            endPointHeight = serializedObject.FindProperty("m_EndPointHeight");
            controlPointDistance = serializedObject.FindProperty("m_ControlPointDistance");
            controlPointHeight = serializedObject.FindProperty("m_ControlPointHeight");
            sampleFrequency = serializedObject.FindProperty("m_SampleFrequency");

            referenceFrame = serializedObject.FindProperty("m_ReferenceFrame");
            velocity = serializedObject.FindProperty("m_Velocity");
            acceleration = serializedObject.FindProperty("m_Acceleration");
            additionalFlightTime = serializedObject.FindProperty("m_AdditionalFlightTime");
            
            keepSelectedTargetValid = serializedObject.FindProperty("m_KeepSelectedTargetValid");
            allowAnchorControl = serializedObject.FindProperty("m_AllowAnchorControl");
            useForceGrab = serializedObject.FindProperty("m_UseForceGrab");
            anchorRotateSpeed = serializedObject.FindProperty("m_RotateSpeed");
            anchorTranslateSpeed = serializedObject.FindProperty("m_TranslateSpeed");

            playAudioClipOnSelectEntered = serializedObject.FindProperty("m_PlayAudioClipOnSelectEntered");
            audioClipForOnSelectEntered = serializedObject.FindProperty("m_AudioClipForOnSelectEntered");
            playAudioClipOnSelectExited = serializedObject.FindProperty("m_PlayAudioClipOnSelectExited");
            audioClipForOnSelectExited = serializedObject.FindProperty("m_AudioClipForOnSelectExited");
            playAudioClipOnHoverEntered = serializedObject.FindProperty("m_PlayAudioClipOnHoverEntered");
            audioClipForOnHoverEntered = serializedObject.FindProperty("m_AudioClipForOnHoverEntered");
            playAudioClipOnHoverExited = serializedObject.FindProperty("m_PlayAudioClipOnHoverExited");
            audioClipForOnHoverExited = serializedObject.FindProperty("m_AudioClipForOnHoverExited");
            
            playHapticsOnSelectEntered = serializedObject.FindProperty("m_PlayHapticsOnSelectEntered");
            hapticSelectEnterIntensity = serializedObject.FindProperty("m_HapticSelectEnterIntensity");
            hapticSelectEnterDuration = serializedObject.FindProperty("m_HapticSelectEnterDuration");
            playHapticsOnHoverEntered = serializedObject.FindProperty("m_PlayHapticsOnHoverEntered");
            hapticHoverEnterIntensity = serializedObject.FindProperty("m_HapticHoverEnterIntensity");
            hapticHoverEnterDuration = serializedObject.FindProperty("m_HapticHoverEnterDuration");
            playHapticsOnSelectExited = serializedObject.FindProperty("m_PlayHapticsOnSelectExited");
            hapticSelectExitIntensity = serializedObject.FindProperty("m_HapticSelectExitIntensity");
            hapticSelectExitDuration = serializedObject.FindProperty("m_HapticSelectExitDuration");
            playHapticsOnHoverExited = serializedObject.FindProperty("m_PlayHapticsOnHoverExited");
            hapticHoverExitIntensity = serializedObject.FindProperty("m_HapticHoverExitIntensity");
            hapticHoverExitDuration = serializedObject.FindProperty("m_HapticHoverExitDuration");
            
            onHoverEntered = serializedObject.FindProperty("m_OnHoverEntered");
            onHoverExited = serializedObject.FindProperty("m_OnHoverExited");
            onSelectEntered = serializedObject.FindProperty("m_OnSelectEntered");
            onSelectExited = serializedObject.FindProperty("m_OnSelectExited");
        }

        public override void OnInspectorGUI()
        { 
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(EditorGUIUtility.TrTempContent("Script"), MonoScript.FromMonoBehaviour((RayInteractor)target), typeof(RayInteractor), false);
            EditorGUI.EndDisabledGroup();

            foreach (Object targetObject in serializedObject.targetObjects)
            {
                RayInteractor interactor = (RayInteractor)targetObject;
                
                if (interactor.GetComponent<XRController>() == null && interactor.GetComponent<ActionBasedController>() == null)
                {
                    EditorGUILayout.HelpBox(Tooltips.MissingRequiredController, MessageType.Warning, true);
                    break;
                }
            }

            EditorGUILayout.PropertyField(interactionManager, Tooltips.InteractionManager);
            EditorGUILayout.PropertyField(interactionLayerMask, Tooltips.InteractionLayerMask);

            EditorGUILayout.Space();
            
            EditorGUILayout.PropertyField(enableUIInteraction, Tooltips.EnableUIInteraction);
            
            EditorGUILayout.PropertyField(useForceGrab, Tooltips.ForceGrab);
            EditorGUILayout.PropertyField(allowAnchorControl, Tooltips.AllowAnchorControl);
            
            if (allowAnchorControl.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(anchorRotateSpeed, Tooltips.AnchorRotateSpeed);
                EditorGUILayout.PropertyField(anchorTranslateSpeed, Tooltips.AnchorTranslateSpeed);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(attachTransform, Tooltips.AttachTransform);

            EditorGUILayout.Space();

            lineType.isExpanded = EditorGUILayout.Foldout(lineType.isExpanded, EditorGUIUtility.TrTempContent("Raycast Configuration"), true);
            
            if (lineType.isExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(lineType, Tooltips.LineType);
                EditorGUI.indentLevel++;
                
                switch (lineType.enumValueIndex)
                {
                    case (int)XRRayInteractor.LineType.StraightLine:
                        EditorGUILayout.PropertyField(maxRaycastDistance, Tooltips.MaxRaycastDistance);
                        break;
                    case (int)XRRayInteractor.LineType.ProjectileCurve:
                        EditorGUILayout.PropertyField(referenceFrame, Tooltips.ReferenceFrame);
                        EditorGUILayout.PropertyField(velocity, Tooltips.Velocity);
                        EditorGUILayout.PropertyField(acceleration, Tooltips.Acceleration);
                        EditorGUILayout.PropertyField(additionalFlightTime, Tooltips.AdditionalFlightTime);
                        EditorGUILayout.PropertyField(sampleFrequency, Tooltips.SampleFrequency);
                        break;
                    case (int)XRRayInteractor.LineType.BezierCurve:
                        EditorGUILayout.PropertyField(endPointDistance, Tooltips.EndPointDistance);
                        EditorGUILayout.PropertyField(endPointHeight, Tooltips.EndPointHeight);
                        EditorGUILayout.PropertyField(controlPointDistance, Tooltips.ControlPointDistance);
                        EditorGUILayout.PropertyField(controlPointHeight, Tooltips.ControlPointHeight);
                        EditorGUILayout.PropertyField(sampleFrequency, Tooltips.SampleFrequency);
                        break;
                }
                EditorGUI.indentLevel--;

                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(raycastMask, Tooltips.RaycastMask);
                EditorGUILayout.PropertyField(raycastTriggerInteraction, Tooltips.RaycastTriggerInteraction);
                EditorGUILayout.PropertyField(hitDetectionType, Tooltips.HitDetectionType);
                
                if (hitDetectionType.enumValueIndex == (int)XRRayInteractor.HitDetectionType.SphereCast)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(sphereCastRadius, Tooltips.SphereCastRadius);
                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            selectActionTrigger.isExpanded = EditorGUILayout.Foldout(selectActionTrigger.isExpanded, EditorGUIUtility.TrTempContent("Selection Configuration"), true);
            
            if (selectActionTrigger.isExpanded)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(selectActionTrigger, Tooltips.SelectActionTrigger);
                if (startingSelectedInteractable.objectReferenceValue != null && (selectActionTrigger.enumValueIndex == 2 || selectActionTrigger.enumValueIndex == 3))
                {
                    EditorGUILayout.HelpBox(Tooltips.StartingInteractableWarning, MessageType.Warning, true);
                }

                EditorGUILayout.PropertyField(keepSelectedTargetValid, Tooltips.KeepSelectedTargetValid);
                EditorGUILayout.PropertyField(hideControllerOnSelect, Tooltips.HideControllerOnSelect);
                EditorGUILayout.PropertyField(hoverToSelect, Tooltips.HoverToSelect);
                
                if (hoverToSelect.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(hoverTimeToSelect, Tooltips.EoverTimeToSelect);
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.PropertyField(startingSelectedInteractable, Tooltips.StartingSelectedInteractable);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            playAudioClipOnSelectEntered.isExpanded = EditorGUILayout.Foldout(playAudioClipOnSelectEntered.isExpanded, EditorGUIUtility.TrTempContent("Audio Events"), true);
            
            if (playAudioClipOnSelectEntered.isExpanded)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(playAudioClipOnSelectEntered, Tooltips.PlayAudioClipOnSelectEntered);
                
                if (playAudioClipOnSelectEntered.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(audioClipForOnSelectEntered, Tooltips.AudioClipForOnSelectEntered);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(playAudioClipOnSelectExited, Tooltips.PlayAudioClipOnSelectExited);
                
                if (playAudioClipOnSelectExited.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(audioClipForOnSelectExited, Tooltips.AudioClipForOnSelectExited);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(playAudioClipOnHoverEntered, Tooltips.PlayAudioClipOnHoverEntered);
                
                if (playAudioClipOnHoverEntered.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(audioClipForOnHoverEntered, Tooltips.AudioClipForOnHoverEntered);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(playAudioClipOnHoverExited, Tooltips.PlayAudioClipOnHoverExited);
                
                if (playAudioClipOnHoverExited.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(audioClipForOnHoverExited, Tooltips.AudioClipForOnHoverExited);
                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            playHapticsOnSelectEntered.isExpanded = EditorGUILayout.Foldout(playHapticsOnSelectEntered.isExpanded, EditorGUIUtility.TrTempContent("Haptic Events"), true);
            
            if (playHapticsOnSelectEntered.isExpanded)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(playHapticsOnSelectEntered, Tooltips.PlayHapticsOnSelectEntered);
                
                if (playHapticsOnSelectEntered.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(hapticSelectEnterIntensity, Tooltips.HapticSelectEnterIntensity);
                    EditorGUILayout.PropertyField(hapticSelectEnterDuration, Tooltips.HapticSelectEnterDuration);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(playHapticsOnSelectExited, Tooltips.PlayHapticsOnSelectExited);
                
                if (playHapticsOnSelectExited.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(hapticSelectExitIntensity, Tooltips.HapticSelectExitIntensity);
                    EditorGUILayout.PropertyField(hapticSelectExitDuration, Tooltips.HapticSelectExitDuration);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(playHapticsOnHoverEntered, Tooltips.PlayHapticsOnHoverEntered);
                
                if (playHapticsOnHoverEntered.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(hapticHoverEnterIntensity, Tooltips.HapticHoverEnterIntensity);
                    EditorGUILayout.PropertyField(hapticHoverEnterDuration, Tooltips.HapticHoverEnterDuration);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(playHapticsOnHoverExited, Tooltips.PlayHapticsOnHoverExited);
                
                if (playHapticsOnHoverExited.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(hapticHoverExitIntensity, Tooltips.HapticHoverExitIntensity);
                    EditorGUILayout.PropertyField(hapticHoverExitDuration, Tooltips.HapticHoverExitDuration);
                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            onHoverEntered.isExpanded = EditorGUILayout.Foldout(onHoverEntered.isExpanded, EditorGUIUtility.TrTempContent("Interactor Events"), true);
            
            if (onHoverEntered.isExpanded)
            {
                EditorGUILayout.PropertyField(onHoverEntered);
                EditorGUILayout.PropertyField(onHoverExited);
                EditorGUILayout.PropertyField(onSelectEntered);
                EditorGUILayout.PropertyField(onSelectExited);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}