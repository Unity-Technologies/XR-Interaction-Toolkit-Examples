using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEngine.XR.Content.Interaction.Analytics
{
    using static XrcAnalyticsUtils;

    /// <summary>
    /// Class that connects the Setup station scene objects with their respective analytics events.
    /// </summary>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    class XrcSetupStationAnalytics : MonoBehaviour
    {
        [Header("Left Hand Substation")]
        [SerializeField]
        XRLever m_LeftHandLocomotionType;

        [SerializeField]
        XRLever m_LeftHandMovementDirection;

        [SerializeField]
        XRLever m_LeftHandTurnStyle;

        [Header("Locomotion Settings Substation")]
        [SerializeField]
        XRSlider m_MoveSpeed;

        [SerializeField]
        XRPushButton m_StrafeEnabled;

        [SerializeField]
        XRPushButton m_ComfortMode;

        [SerializeField]
        XRPushButton m_RigGravityEnabled;

        [SerializeField]
        XRPushButton m_FlyDisabled;

        [SerializeField]
        XRKnob m_TurnSpeed;

        [SerializeField]
        XRPushButton m_TurnAroundEnabled;

        [SerializeField]
        XRKnob m_SnapTurn;

        [SerializeField]
        XRPushButton m_GrabMoveDisabled;

        [SerializeField]
        XRSlider m_MoveRatio;

        [SerializeField]
        XRPushButton m_ScalingDisabled;

        [Header("Right Hand Substation")]
        [SerializeField]
        XRLever m_RightHandLocomotionType;

        [SerializeField]
        XRLever m_RightHandMovementDirection;

        [SerializeField]
        XRLever m_RightHandTurnStyle;

        void Awake()
        {
            // left hand
            Register(m_LeftHandLocomotionType, new LeftHandLocomotionTypeInteraction());
            Register(m_LeftHandMovementDirection, new LeftMovementDirectionInteraction());
            Register(m_LeftHandTurnStyle, new LeftHandTurnStyleInteraction());

            // global settings
            Register(m_MoveSpeed, new MoveSpeedInteraction());
            Register(m_StrafeEnabled, new StrafeEnabledInteraction());
            Register(m_ComfortMode, new ComfortModeInteraction());
            Register(m_RigGravityEnabled, new RigGravityEnabledInteraction());
            Register(m_FlyDisabled, new FlyDisabledInteraction());
            Register(m_TurnSpeed, new TurnSppedInteraction());
            Register(m_TurnAroundEnabled, new TurnAroundEnabledInteraction());
            Register(m_SnapTurn, new SnapTurnInteraction());
            Register(m_GrabMoveDisabled, new GrabMoveDisabledInteraction());
            Register(m_MoveRatio, new MoveRatioInteraction());
            Register(m_ScalingDisabled, new ScalingDisabledInteraction());

            // right hand
            Register(m_RightHandLocomotionType, new RightHandLocomotionTypeInteraction());
            Register(m_RightHandMovementDirection, new RightHandMovementDirectionInteraction());
            Register(m_RightHandTurnStyle, new RightHandTurnStyleInteraction());
        }
    }
}
