using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace UnityEngine.XR.Content.Interaction
{
    /// <summary>
    /// Use this class to present locomotion control schemes and configuration preferences,
    /// and respond to player input in the UI to set them.
    /// </summary>
    /// <seealso cref="LocomotionManager"/>
    public class LocomotionSetup : MonoBehaviour
    {
        const float k_MaxMoveSpeed = 5.0f;
        const float k_MinMoveSpeed = 0.5f;
        const float k_MaxTurnSpeed = 180f;
        const float k_MaxSnapTurnAmount = 90f;
        const float k_MaxGrabMoveRatio = 4f;
        const float k_MinGrabMoveRatio = 0.5f;

        const string k_SpeedFormat = "###.0";
        const string k_DegreeFormat = "###";
        const string k_GrabMoveRatioFormat = "###.0";
        const string k_MoveSpeedUnitLabel = " m/s";
        const string k_TurnSpeedUnitLabel = "°/s";
        const string k_SnapTurnAmountLabel = "°";
        const string k_GrabMoveRatioLabel = " : 1.0";

        const string k_GravityLabel = "Rig Gravity";

        [SerializeField]
        [Tooltip("Stores the behavior that will be used to configure locomotion control schemes and configuration preferences.")]
        LocomotionManager m_Manager;

        [SerializeField]
        [Tooltip("Stores the GameObject reference used to turn on and off the movement direction toggle in the 3D UI for the left hand.")]
        GameObject m_LeftHandMovementDirectionSelection;

        [SerializeField]
        [Tooltip("Stores the GameObject reference used to turn on and off the movement direction toggle in the 3D UI for the right hand.")]
        GameObject m_RightHandMovementDirectionSelection;

        [SerializeField]
        [Tooltip("Stores the GameObject reference used to turn on and off the turn style toggle in the 3D UI for the left hand.")]
        GameObject m_LeftHandTurnStyleSelection;

        [SerializeField]
        [Tooltip("Stores the GameObject reference used to turn on and off the turn style toggle in the 3D UI for the right hand.")]
        GameObject m_RightHandTurnStyleSelection;

        [SerializeField]
        [Tooltip("Stores the toggle lever used to choose the locomotion type between move/strafe and teleport/turn for the left hand.")]
        XRLever m_LeftHandLocomotionTypeToggle;

        [SerializeField]
        [Tooltip("Stores the toggle lever used to choose the locomotion type between move/strafe and teleport/turn for the right hand.")]
        XRLever m_RightHandLocomotionTypeToggle;

        [SerializeField]
        [Tooltip("Stores the toggle lever used to choose the movement direction between head-relative and hand-relative for the left hand.")]
        XRLever m_LeftHandMovementDirectionToggle;

        [SerializeField]
        [Tooltip("Stores the toggle lever used to choose the movement direction between head-relative and hand-relative for the right hand.")]
        XRLever m_RightHandMovementDirectionToggle;

        [SerializeField]
        [Tooltip("Stores the toggle lever used to choose the turn style between continuous and snap for the left hand.")]
        XRLever m_LeftHandTurnStyleToggle;

        [SerializeField]
        [Tooltip("Stores the toggle lever used to choose the turn style between continuous and snap for the right hand.")]
        XRLever m_RightHandTurnStyleToggle;

        [SerializeField]
        [Tooltip("Stores the Slider used to set the move speed of continuous movement.")]
        XRSlider m_MoveSpeedSlider;

        [SerializeField]
        [Tooltip("Stores the button toggle used to enable strafing movement.")]
        XRPushButton m_StrafeToggle;

        [SerializeField]
        [Tooltip("Stores the button toggle used to enable comfort mode.")]
        XRPushButton m_ComfortModeToggle;

        [SerializeField]
        [Tooltip("Stores the button toggle used to enable gravity.")]
        XRPushButton m_GravityToggle;

        [SerializeField]
        [Tooltip("Stores the button toggle used to enable flying.")]
        XRPushButton m_FlyToggle;

        [SerializeField]
        [Tooltip("Stores the knob used to set turn speed.")]
        XRKnob m_TurnSpeedKnob;

        [SerializeField]
        [Tooltip("Stores the button toggle used to enable instant turn-around.")]
        XRPushButton m_TurnAroundToggle;

        [SerializeField]
        [Tooltip("Stores the knob used to set snap turn around.")]
        XRKnob m_SnapTurnKnob;

        [SerializeField]
        [Tooltip("Stores the button toggle used to enable grab movement.")]
        XRPushButton m_GrabMoveToggle;

        [SerializeField]
        [Tooltip("Stores the Slider used to set the move ratio for grab movement.")]
        XRSlider m_MoveRatioSlider;

        [SerializeField]
        [Tooltip("Stores the button toggle used to enable grab scaling.")]
        XRPushButton m_ScalingToggle;

        [SerializeField]
        [Tooltip("The label that shows the current movement speed value.")]
        TextMeshPro m_MoveSpeedLabel;

        [SerializeField]
        [Tooltip("The label that shows the current turn speed value.")]
        TextMeshPro m_TurnSpeedLabel;

        [SerializeField]
        [Tooltip("The label that shows the current snap turn value.")]
        TextMeshPro m_SnapTurnLabel;

        [SerializeField]
        [Tooltip("The label that shows the current grab move ratio value.")]
        TextMeshPro m_MoveRatioLabel;

        [SerializeField]
        [Tooltip("The label that shows the current strafe toggle value.")]
        TextMeshPro m_StrafeLabel;

        [SerializeField]
        [Tooltip("The label that shows the current gravity toggle value.")]
        TextMeshPro m_GravityLabel;

        [SerializeField]
        [Tooltip("The label that shows the current turn around toggle value.")]
        TextMeshPro m_TurnAroundLabel;

        [SerializeField]
        [Tooltip("The label that shows the current fly toggle value.")]
        TextMeshPro m_FlyLabel;

        [SerializeField]
        [Tooltip("The label that shows the current grab move toggle value.")]
        TextMeshPro m_GrabMoveLabel;

        [SerializeField]
        [Tooltip("The label that shows the current scaling toggle value.")]
        TextMeshPro m_ScalingLabel;

        void ConnectControlEvents()
        {
            m_LeftHandLocomotionTypeToggle.onLeverActivate.AddListener(EnableLeftHandMoveAndStrafe);
            m_LeftHandLocomotionTypeToggle.onLeverDeactivate.AddListener(EnableLeftHandTeleportAndTurn);
            m_RightHandLocomotionTypeToggle.onLeverActivate.AddListener(EnableRightHandMoveAndStrafe);
            m_RightHandLocomotionTypeToggle.onLeverDeactivate.AddListener(EnableRightHandTeleportAndTurn);

            m_LeftHandMovementDirectionToggle.onLeverActivate.AddListener(SetLeftMovementDirectionHeadRelative);
            m_LeftHandMovementDirectionToggle.onLeverDeactivate.AddListener(SetLeftMovementDirectionHandRelative);
            m_RightHandMovementDirectionToggle.onLeverActivate.AddListener(SetRightMovementDirectionHeadRelative);
            m_RightHandMovementDirectionToggle.onLeverDeactivate.AddListener(SetRightMovementDirectionHandRelative);

            m_LeftHandTurnStyleToggle.onLeverActivate.AddListener(EnableLeftHandContinuousTurn);
            m_LeftHandTurnStyleToggle.onLeverDeactivate.AddListener(EnableLeftHandSnapTurn);
            m_RightHandTurnStyleToggle.onLeverActivate.AddListener(EnableRightHandContinuousTurn);
            m_RightHandTurnStyleToggle.onLeverDeactivate.AddListener(EnableRightHandSnapTurn);

            m_MoveSpeedSlider.onValueChange.AddListener(SetMoveSpeed);
            m_StrafeToggle.onPress.AddListener(EnableStrafe);
            m_StrafeToggle.onRelease.AddListener(DisableStrafe);
            m_ComfortModeToggle.onPress.AddListener(EnableComfort);
            m_ComfortModeToggle.onRelease.AddListener(DisableComfort);
            m_GravityToggle.onPress.AddListener(EnableGravity);
            m_GravityToggle.onRelease.AddListener(DisableGravity);
            m_FlyToggle.onPress.AddListener(EnableFly);
            m_FlyToggle.onRelease.AddListener(DisableFly);

            m_TurnSpeedKnob.onValueChange.AddListener(SetTurnSpeed);
            m_TurnAroundToggle.onPress.AddListener(EnableTurnAround);
            m_TurnAroundToggle.onRelease.AddListener(DisableTurnAround);
            m_SnapTurnKnob.onValueChange.AddListener(SetSnapTurnAmount);

            m_GrabMoveToggle.onPress.AddListener(EnableGrabMove);
            m_GrabMoveToggle.onRelease.AddListener(DisableGrabMove);
            m_MoveRatioSlider.onValueChange.AddListener(SetGrabMoveRatio);
            m_ScalingToggle.onPress.AddListener(EnableScaling);
            m_ScalingToggle.onRelease.AddListener(DisableScaling);
        }

        void DisconnectControlEvents()
        {
            m_LeftHandLocomotionTypeToggle.onLeverActivate.RemoveListener(EnableLeftHandMoveAndStrafe);
            m_LeftHandLocomotionTypeToggle.onLeverDeactivate.RemoveListener(EnableLeftHandTeleportAndTurn);
            m_RightHandLocomotionTypeToggle.onLeverActivate.RemoveListener(EnableRightHandMoveAndStrafe);
            m_RightHandLocomotionTypeToggle.onLeverDeactivate.RemoveListener(EnableRightHandTeleportAndTurn);

            m_LeftHandMovementDirectionToggle.onLeverActivate.RemoveListener(SetLeftMovementDirectionHeadRelative);
            m_LeftHandMovementDirectionToggle.onLeverDeactivate.RemoveListener(SetLeftMovementDirectionHandRelative);
            m_RightHandMovementDirectionToggle.onLeverActivate.RemoveListener(SetRightMovementDirectionHeadRelative);
            m_RightHandMovementDirectionToggle.onLeverDeactivate.RemoveListener(SetRightMovementDirectionHandRelative);

            m_LeftHandTurnStyleToggle.onLeverActivate.RemoveListener(EnableLeftHandContinuousTurn);
            m_LeftHandTurnStyleToggle.onLeverDeactivate.RemoveListener(EnableLeftHandSnapTurn);
            m_RightHandTurnStyleToggle.onLeverActivate.RemoveListener(EnableRightHandContinuousTurn);
            m_RightHandTurnStyleToggle.onLeverDeactivate.RemoveListener(EnableRightHandSnapTurn);

            m_MoveSpeedSlider.onValueChange.RemoveListener(SetMoveSpeed);
            m_StrafeToggle.onPress.RemoveListener(EnableStrafe);
            m_StrafeToggle.onRelease.RemoveListener(DisableStrafe);
            m_ComfortModeToggle.onPress.RemoveListener(EnableComfort);
            m_ComfortModeToggle.onRelease.RemoveListener(DisableComfort);
            m_GravityToggle.onPress.RemoveListener(EnableGravity);
            m_GravityToggle.onRelease.RemoveListener(DisableGravity);
            m_FlyToggle.onPress.RemoveListener(EnableFly);
            m_FlyToggle.onRelease.RemoveListener(DisableFly);

            m_TurnSpeedKnob.onValueChange.RemoveListener(SetTurnSpeed);
            m_TurnAroundToggle.onPress.RemoveListener(EnableTurnAround);
            m_TurnAroundToggle.onRelease.RemoveListener(DisableTurnAround);
            m_SnapTurnKnob.onValueChange.RemoveListener(SetSnapTurnAmount);

            m_GrabMoveToggle.onPress.RemoveListener(EnableGrabMove);
            m_GrabMoveToggle.onRelease.RemoveListener(DisableGrabMove);
            m_MoveRatioSlider.onValueChange.RemoveListener(SetGrabMoveRatio);
            m_ScalingToggle.onPress.RemoveListener(EnableScaling);
            m_ScalingToggle.onRelease.RemoveListener(DisableScaling);
        }

        void InitializeControls()
        {
            var isLeftHandMoveAndStrafe = m_Manager.leftHandLocomotionType == LocomotionManager.LocomotionType.MoveAndStrafe;
            var isRightHandMoveAndStrafe = m_Manager.rightHandLocomotionType == LocomotionManager.LocomotionType.MoveAndStrafe;
            m_LeftHandLocomotionTypeToggle.value = isLeftHandMoveAndStrafe;
            m_RightHandLocomotionTypeToggle.value = isRightHandMoveAndStrafe;

            m_LeftHandTurnStyleSelection.SetActive(!isLeftHandMoveAndStrafe);
            m_RightHandTurnStyleSelection.SetActive(!isRightHandMoveAndStrafe);

            m_LeftHandTurnStyleToggle.value = (m_Manager.leftHandTurnStyle == LocomotionManager.TurnStyle.Smooth);
            m_RightHandTurnStyleToggle.value = (m_Manager.rightHandTurnStyle == LocomotionManager.TurnStyle.Smooth);

            m_MoveSpeedSlider.value = Mathf.InverseLerp(k_MinMoveSpeed, k_MaxMoveSpeed, m_Manager.dynamicMoveProvider.moveSpeed);
            m_StrafeToggle.toggleValue = m_Manager.dynamicMoveProvider.enableStrafe;
            m_ComfortModeToggle.toggleValue = (m_Manager.enableComfortMode);
            m_GravityToggle.toggleValue = m_Manager.useGravity;
            m_FlyToggle.toggleValue = m_Manager.enableFly;

            m_TurnSpeedKnob.value = m_Manager.smoothTurnProvider.turnSpeed / k_MaxTurnSpeed;
            m_TurnAroundToggle.toggleValue = m_Manager.snapTurnProvider.enableTurnAround;
            m_SnapTurnKnob.value = m_Manager.snapTurnProvider.turnAmount / k_MaxSnapTurnAmount;

            m_GrabMoveToggle.toggleValue = m_Manager.enableGrabMovement;
            m_MoveRatioSlider.value = Mathf.InverseLerp(k_MinGrabMoveRatio, k_MaxGrabMoveRatio, m_Manager.twoHandedGrabMoveProvider.moveFactor);
            m_ScalingToggle.toggleValue = m_Manager.enableGrabMovement && m_Manager.twoHandedGrabMoveProvider.enableScaling;

            m_MoveSpeedLabel.text = $"{m_Manager.dynamicMoveProvider.moveSpeed.ToString(k_SpeedFormat)}{k_MoveSpeedUnitLabel}";
            m_TurnSpeedLabel.text = $"{m_Manager.smoothTurnProvider.turnSpeed.ToString(k_DegreeFormat)}{k_TurnSpeedUnitLabel}";
            m_SnapTurnLabel.text = $"{m_Manager.snapTurnProvider.turnAmount.ToString(k_DegreeFormat)}{k_TurnSpeedUnitLabel}";

            m_StrafeLabel.text = $"Strafe\n{(m_Manager.dynamicMoveProvider.enableStrafe ? "Enabled" : "Disabled")}";
            m_GravityLabel.text = $"{k_GravityLabel}\n{(m_Manager.useGravity ? "Enabled" : "Disabled")}";
            m_TurnAroundLabel.text = $"Turn Around \n{(m_Manager.snapTurnProvider.enableTurnAround ? "Enabled" : "Disabled")}";
            m_FlyLabel.text = $"Fly\n{(m_Manager.enableFly ? "Enabled" : "Disabled")}";

            m_GrabMoveLabel.text = $"Grab Move\n{(m_Manager.enableGrabMovement ? "Enabled" : "Disabled")}";
            m_MoveRatioLabel.text = $"{m_Manager.twoHandedGrabMoveProvider.moveFactor.ToString(k_GrabMoveRatioFormat)}{k_GrabMoveRatioLabel}";
            m_ScalingLabel.text = $"Scaling\n{(m_Manager.enableGrabMovement && m_Manager.twoHandedGrabMoveProvider.enableScaling ? "Enabled" : "Disabled")}";
        }

        protected void OnEnable()
        {
            if (!ValidateManager())
                return;

            ConnectControlEvents();
            InitializeControls();
        }

        protected void OnDisable()
        {
            DisconnectControlEvents();
        }

        bool ValidateManager()
        {
            if (m_Manager == null)
            {
                m_Manager = FindObjectOfType<LocomotionManager>(true);
                if (m_Manager == null)
                {
                    Debug.LogError($"Reference to the {nameof(LocomotionManager)} is not set or the object has been destroyed," +
                        " configuring locomotion settings from the menu will not be possible." +
                        " Ensure the value has been set in the Inspector.", this);
                    return false;
                }
            }

            if (m_Manager.dynamicMoveProvider == null)
            {
                Debug.LogError($"Reference to the {nameof(LocomotionManager.dynamicMoveProvider)} is not set or the object has been destroyed," +
                    " configuring locomotion settings from the menu will not be possible." +
                    $" Ensure the value has been set in the Inspector on {m_Manager}.", this);
                return false;
            }

            if (m_Manager.smoothTurnProvider == null)
            {
                Debug.LogError($"Reference to the {nameof(LocomotionManager.smoothTurnProvider)} is not set or the object has been destroyed," +
                    " configuring locomotion settings from the menu will not be possible." +
                    $" Ensure the value has been set in the Inspector on {m_Manager}.", this);
                return false;
            }

            if (m_Manager.snapTurnProvider == null)
            {
                Debug.LogError($"Reference to the {nameof(LocomotionManager.snapTurnProvider)} is not set or the object has been destroyed," +
                    " configuring locomotion settings from the menu will not be possible." +
                    $" Ensure the value has been set in the Inspector on {m_Manager}.", this);
                return false;
            }

            if (m_Manager.twoHandedGrabMoveProvider == null)
            {
                Debug.LogError($"Reference to the {nameof(LocomotionManager.twoHandedGrabMoveProvider)} is not set or the object has been destroyed," +
                               " configuring locomotion settings from the menu will not be possible." +
                               $" Ensure the value has been set in the Inspector on {m_Manager}.", this);
                return false;
            }

            return true;
        }

        void EnableLeftHandMoveAndStrafe()
        {
            m_Manager.leftHandLocomotionType = LocomotionManager.LocomotionType.MoveAndStrafe;
            m_LeftHandMovementDirectionSelection.SetActive(true);
            m_LeftHandTurnStyleSelection.SetActive(false);
        }

        void EnableRightHandMoveAndStrafe()
        {
            m_Manager.rightHandLocomotionType = LocomotionManager.LocomotionType.MoveAndStrafe;
            m_RightHandMovementDirectionSelection.SetActive(true);
            m_RightHandTurnStyleSelection.SetActive(false);
        }

        void EnableLeftHandTeleportAndTurn()
        {
            m_Manager.leftHandLocomotionType = LocomotionManager.LocomotionType.TeleportAndTurn;
            m_LeftHandMovementDirectionSelection.SetActive(false);
            m_LeftHandTurnStyleSelection.SetActive(true);
        }

        void EnableRightHandTeleportAndTurn()
        {
            m_Manager.rightHandLocomotionType = LocomotionManager.LocomotionType.TeleportAndTurn;
            m_RightHandMovementDirectionSelection.SetActive(false);
            m_RightHandTurnStyleSelection.SetActive(true);
        }

        void EnableLeftHandContinuousTurn()
        {
            m_Manager.leftHandTurnStyle = LocomotionManager.TurnStyle.Smooth;
        }

        void EnableRightHandContinuousTurn()
        {
            m_Manager.rightHandTurnStyle = LocomotionManager.TurnStyle.Smooth;
        }

        void EnableLeftHandSnapTurn()
        {
            m_Manager.leftHandTurnStyle = LocomotionManager.TurnStyle.Snap;
        }

        void EnableRightHandSnapTurn()
        {
            m_Manager.rightHandTurnStyle = LocomotionManager.TurnStyle.Snap;
        }

        void SetLeftMovementDirectionHeadRelative()
        {
            m_Manager.dynamicMoveProvider.leftHandMovementDirection = DynamicMoveProvider.MovementDirection.HeadRelative;
        }

        void SetLeftMovementDirectionHandRelative()
        {
            m_Manager.dynamicMoveProvider.leftHandMovementDirection = DynamicMoveProvider.MovementDirection.HandRelative;
        }

        void SetRightMovementDirectionHeadRelative()
        {
            m_Manager.dynamicMoveProvider.rightHandMovementDirection = DynamicMoveProvider.MovementDirection.HeadRelative;
        }

        void SetRightMovementDirectionHandRelative()
        {
            m_Manager.dynamicMoveProvider.rightHandMovementDirection = DynamicMoveProvider.MovementDirection.HandRelative;
        }

        void SetMoveSpeed(float sliderValue)
        {
            m_Manager.dynamicMoveProvider.moveSpeed = Mathf.Lerp(k_MinMoveSpeed, k_MaxMoveSpeed, sliderValue);
            m_MoveSpeedLabel.text = $"{m_Manager.dynamicMoveProvider.moveSpeed.ToString(k_SpeedFormat)}{k_MoveSpeedUnitLabel}";
        }

        void EnableStrafe()
        {
            m_Manager.dynamicMoveProvider.enableStrafe = true;
            m_StrafeLabel.text = $"Strafe\n{(m_Manager.dynamicMoveProvider.enableStrafe ? "Enabled" : "Disabled")}";
        }

        void DisableStrafe()
        {
            m_Manager.dynamicMoveProvider.enableStrafe = false;
            m_StrafeLabel.text = $"Strafe\n{(m_Manager.dynamicMoveProvider.enableStrafe ? "Enabled" : "Disabled")}";
        }

        void EnableComfort()
        {
            m_Manager.enableComfortMode = true;
        }

        void DisableComfort()
        {
            m_Manager.enableComfortMode = false;
        }

        void EnableGravity()
        {
            m_Manager.useGravity = true;
            m_GravityLabel.text = $"{k_GravityLabel}\n{(m_Manager.useGravity ? "Enabled" : "Disabled")}";
        }

        void DisableGravity()
        {
            m_Manager.useGravity = false;
            m_GravityLabel.text = $"{k_GravityLabel}\n{(m_Manager.useGravity ? "Enabled" : "Disabled")}";
        }

        void EnableFly()
        {
            m_Manager.enableFly = true;
            m_FlyLabel.text = $"Fly\n{(m_Manager.enableFly ? "Enabled" : "Disabled")}";
        }

        void DisableFly()
        {
            m_Manager.enableFly = false;
            m_FlyLabel.text = $"Fly\n{(m_Manager.enableFly ? "Enabled" : "Disabled")}";
        }

        void SetTurnSpeed(float knobValue)
        {
            m_Manager.smoothTurnProvider.turnSpeed = Mathf.Lerp(m_TurnSpeedKnob.minAngle, m_TurnSpeedKnob.maxAngle, knobValue);
            m_TurnSpeedLabel.text = $"{m_Manager.smoothTurnProvider.turnSpeed.ToString(k_DegreeFormat)}{k_TurnSpeedUnitLabel}";
        }

        void EnableTurnAround()
        {
            m_Manager.snapTurnProvider.enableTurnAround = true;
            m_TurnAroundLabel.text = $"Turn Around \n{(m_Manager.snapTurnProvider.enableTurnAround ? "Enabled" : "Disabled")}";
        }

        void DisableTurnAround()
        {
            m_Manager.snapTurnProvider.enableTurnAround = false;
            m_TurnAroundLabel.text = $"Turn Around \n{(m_Manager.snapTurnProvider.enableTurnAround ? "Enabled" : "Disabled")}";
        }

        void SetSnapTurnAmount(float newAmount)
        {
            m_Manager.snapTurnProvider.turnAmount = Mathf.Lerp(m_SnapTurnKnob.minAngle, m_SnapTurnKnob.maxAngle, newAmount);
            m_SnapTurnLabel.text = $"{m_Manager.snapTurnProvider.turnAmount.ToString(k_DegreeFormat)}{k_SnapTurnAmountLabel}";
        }

        void EnableGrabMove()
        {
            m_Manager.enableGrabMovement = true;
            m_GrabMoveLabel.text = $"Grab Move\n{(m_Manager.enableGrabMovement ? "Enabled" : "Disabled")}";
        }

        void DisableGrabMove()
        {
            m_ScalingToggle.toggleValue = false;
            DisableScaling();
            m_Manager.enableGrabMovement = false;
            m_GrabMoveLabel.text = $"Grab Move\n{(m_Manager.enableGrabMovement ? "Enabled" : "Disabled")}";
        }

        void SetGrabMoveRatio(float sliderValue)
        {
            var moveRatio = Mathf.Lerp(k_MinGrabMoveRatio, k_MaxGrabMoveRatio, sliderValue);
            var twoHandedGrabMoveProvider = m_Manager.twoHandedGrabMoveProvider;
            twoHandedGrabMoveProvider.moveFactor = moveRatio;
            twoHandedGrabMoveProvider.leftGrabMoveProvider.moveFactor = moveRatio;
            twoHandedGrabMoveProvider.rightGrabMoveProvider.moveFactor = moveRatio;
            m_MoveRatioLabel.text = $"{twoHandedGrabMoveProvider.moveFactor.ToString(k_GrabMoveRatioFormat)}{k_GrabMoveRatioLabel}";
        }

        void EnableScaling()
        {
            m_GrabMoveToggle.toggleValue = true;
            EnableGrabMove();
            m_Manager.twoHandedGrabMoveProvider.enableScaling = true;
            m_ScalingLabel.text = $"Scaling\n{(m_Manager.enableGrabMovement && m_Manager.twoHandedGrabMoveProvider.enableScaling ? "Enabled" : "Disabled")}";
        }

        void DisableScaling()
        {
            m_Manager.twoHandedGrabMoveProvider.enableScaling = false;
            m_ScalingLabel.text = $"Scaling\n{(m_Manager.enableGrabMovement && m_Manager.twoHandedGrabMoveProvider.enableScaling ? "Enabled" : "Disabled")}";
        }
    }
}
