using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace UnityEngine.XR.Content.Interaction
{
    /// <summary>
    /// Use this class as a central manager to configure locomotion control schemes and configuration preferences.
    /// </summary>
    public class LocomotionManager : MonoBehaviour
    {
        const ContinuousMoveProviderBase.GravityApplicationMode k_DefaultGravityApplicationMode =
            ContinuousMoveProviderBase.GravityApplicationMode.AttemptingMove;
        const ConstrainedMoveProvider.GravityApplicationMode k_DefaultGravityMode =
            ConstrainedMoveProvider.GravityApplicationMode.AttemptingMove;

        /// <summary>
        /// Sets which movement control scheme to use.
        /// </summary>
        /// <seealso cref="leftHandLocomotionType"/>
        /// <seealso cref="rightHandLocomotionType"/>
        public enum LocomotionType
        {
            /// <summary>
            /// Use smooth (continuous) movement control scheme.
            /// </summary>
            MoveAndStrafe,

            /// <summary>
            /// Use teleport movement control scheme.
            /// </summary>
            TeleportAndTurn,
        }

        /// <summary>
        /// Sets which turn style of locomotion to use.
        /// </summary>
        /// <seealso cref="leftHandTurnStyle"/>
        /// <seealso cref="rightHandTurnStyle"/>
        public enum TurnStyle
        {
            /// <summary>
            /// Use snap turning to rotate the direction you are facing by snapping by a specified angle.
            /// </summary>
            Snap,

            /// <summary>
            /// Use continuous turning to smoothly rotate the direction you are facing by a specified speed.
            /// </summary>
            Smooth,
        }

        [SerializeField]
        [Tooltip("Stores the locomotion provider for smooth (continuous) movement.")]
        DynamicMoveProvider m_DynamicMoveProvider;

        /// <summary>
        /// Stores the locomotion provider for smooth (continuous) movement.
        /// </summary>
        /// <seealso cref="DynamicMoveProvider"/>
        public DynamicMoveProvider dynamicMoveProvider
        {
            get => m_DynamicMoveProvider;
            set => m_DynamicMoveProvider = value;
        }

        [SerializeField]
        [Tooltip("Stores the locomotion provider for smooth (continuous) turning.")]
        ContinuousTurnProviderBase m_SmoothTurnProvider;

        /// <summary>
        /// Stores the locomotion provider for smooth (continuous) turning.
        /// </summary>
        /// <seealso cref="ContinuousTurnProviderBase"/>
        public ContinuousTurnProviderBase smoothTurnProvider
        {
            get => m_SmoothTurnProvider;
            set => m_SmoothTurnProvider = value;
        }

        [SerializeField]
        [Tooltip("Stores the locomotion provider for snap turning.")]
        SnapTurnProviderBase m_SnapTurnProvider;

        /// <summary>
        /// Stores the locomotion provider for snap turning.
        /// </summary>
        /// <seealso cref="SnapTurnProviderBase"/>
        public SnapTurnProviderBase snapTurnProvider
        {
            get => m_SnapTurnProvider;
            set => m_SnapTurnProvider = value;
        }

        [SerializeField]
        [Tooltip("Stores the locomotion provider for two-handed grab movement.")]
        TwoHandedGrabMoveProvider m_TwoHandedGrabMoveProvider;

        /// <summary>
        /// Stores the locomotion provider for two-handed grab movement.
        /// </summary>
        /// <seealso cref="TwoHandedGrabMoveProvider"/>
        public TwoHandedGrabMoveProvider twoHandedGrabMoveProvider
        {
            get => m_TwoHandedGrabMoveProvider;
            set => m_TwoHandedGrabMoveProvider = value;
        }

        [SerializeField]
        [Tooltip("Reference to the manager that mediates the left-hand controllers.")]
        ActionBasedControllerManager m_LeftHandManager;

        [SerializeField]
        [Tooltip("Reference to the manager that mediates the right-hand controllers.")]
        ActionBasedControllerManager m_RightHandManager;

        [SerializeField]
        [Tooltip("Controls which movement control scheme to use for the left hand.")]
        LocomotionType m_LeftHandLocomotionType;

        /// <summary>
        /// Controls which movement control scheme to use for the left hand.
        /// </summary>
        /// <seealso cref="LocomotionType"/>
        public LocomotionType leftHandLocomotionType
        {
            get => m_LeftHandLocomotionType;
            set
            {
                SetMoveScheme(value, true);
                m_LeftHandLocomotionType = value;
            }
        }

        [SerializeField]
        [Tooltip("Controls which movement control scheme to use for the right hand.")]
        LocomotionType m_RightHandLocomotionType;

        /// <summary>
        /// Controls which movement control scheme to use for the left hand.
        /// </summary>
        /// <seealso cref="LocomotionType"/>
        public LocomotionType rightHandLocomotionType
        {
            get => m_RightHandLocomotionType;
            set
            {
                SetMoveScheme(value, false);
                m_RightHandLocomotionType = value;
            }
        }

        [SerializeField]
        [Tooltip("Controls which turn style of locomotion to use for the left hand.")]
        TurnStyle m_LeftHandTurnStyle;

        /// <summary>
        /// Controls which turn style of locomotion to use for the left hand.
        /// </summary>
        /// <seealso cref="TurnStyle"/>
        public TurnStyle leftHandTurnStyle
        {
            get => m_LeftHandTurnStyle;
            set
            {
                SetTurnStyle(value, true);
                m_LeftHandTurnStyle = value;
            }
        }

        [SerializeField]
        [Tooltip("Controls which turn style of locomotion to use for the right hand.")]
        TurnStyle m_RightHandTurnStyle;

        /// <summary>
        /// Controls which turn style of locomotion to use for the left hand.
        /// </summary>
        /// <seealso cref="TurnStyle"/>
        public TurnStyle rightHandTurnStyle
        {
            get => m_RightHandTurnStyle;
            set
            {
                SetTurnStyle(value, false);
                m_RightHandTurnStyle = value;
            }
        }

        [SerializeField]
        [Tooltip("Whether to enable the comfort mode that applies the tunneling vignette effect to smooth movement and turn.")]
        bool m_EnableComfortMode;

        public bool enableComfortMode
        {
            get => m_EnableComfortMode;
            set
            {
                m_EnableComfortMode = value;
                if (m_ComfortMode != null)
                    m_ComfortMode.SetActive(m_EnableComfortMode);
            }
        }

        [SerializeField]
        [Tooltip("Stores the GameObject for the comfort mode.")]
        GameObject m_ComfortMode;

        [SerializeField]
        [Tooltip("Whether gravity affects continuous and grab movement when flying is disabled.")]
        bool m_UseGravity;

        /// <summary>
        /// Whether gravity affects continuous and grab movement when flying is disabled.
        /// </summary>
        public bool useGravity
        {
            get => m_UseGravity;
            set
            {
                m_UseGravity = value;
                m_DynamicMoveProvider.useGravity = value;
                m_TwoHandedGrabMoveProvider.useGravity = value;
                m_TwoHandedGrabMoveProvider.leftGrabMoveProvider.useGravity = value;
                m_TwoHandedGrabMoveProvider.rightGrabMoveProvider.useGravity = value;
                if (value)
                {
                    m_DynamicMoveProvider.gravityApplicationMode = k_DefaultGravityApplicationMode;
                    m_TwoHandedGrabMoveProvider.gravityMode = k_DefaultGravityMode;
                    m_TwoHandedGrabMoveProvider.leftGrabMoveProvider.gravityMode = k_DefaultGravityMode;
                    m_TwoHandedGrabMoveProvider.rightGrabMoveProvider.gravityMode = k_DefaultGravityMode;
                }
            }
        }

        [SerializeField]
        [Tooltip("Whether to enable flying for continuous and grab movement. This overrides the use of gravity.")]
        bool m_EnableFly;

        /// <summary>
        /// Whether to enable flying for continuous and grab movement. This overrides the use of gravity.
        /// </summary>
        public bool enableFly
        {
            get => m_EnableFly;
            set
            {
                m_EnableFly = value;
                m_DynamicMoveProvider.enableFly = value;
                m_TwoHandedGrabMoveProvider.enableFreeYMovement = value;
                m_TwoHandedGrabMoveProvider.leftGrabMoveProvider.enableFreeYMovement = value;
                m_TwoHandedGrabMoveProvider.rightGrabMoveProvider.enableFreeYMovement = value;
            }
        }

        [SerializeField]
        [Tooltip("Whether to enable grab movement.")]
        bool m_EnableGrabMovement;

        /// <summary>
        /// Whether to enable grab movement.
        /// </summary>
        public bool enableGrabMovement
        {
            get => m_EnableGrabMovement;
            set
            {
                m_EnableGrabMovement = value;
                m_TwoHandedGrabMoveProvider.enabled = value;
                m_TwoHandedGrabMoveProvider.leftGrabMoveProvider.enabled = value;
                m_TwoHandedGrabMoveProvider.rightGrabMoveProvider.enabled = value;
            }
        }

        void OnEnable()
        {
            SetMoveScheme(m_LeftHandLocomotionType, true);
            SetMoveScheme(m_RightHandLocomotionType, false);
            SetTurnStyle(m_LeftHandTurnStyle, true);
            SetTurnStyle(m_RightHandTurnStyle, false);

            if (m_ComfortMode != null)
                m_ComfortMode.SetActive(m_EnableComfortMode);

            m_DynamicMoveProvider.useGravity = m_UseGravity;
            m_TwoHandedGrabMoveProvider.useGravity = m_UseGravity;
            m_TwoHandedGrabMoveProvider.leftGrabMoveProvider.useGravity = m_UseGravity;
            m_TwoHandedGrabMoveProvider.rightGrabMoveProvider.useGravity = m_UseGravity;
            if (m_UseGravity)
            {
                m_DynamicMoveProvider.gravityApplicationMode = k_DefaultGravityApplicationMode;
                m_TwoHandedGrabMoveProvider.gravityMode = k_DefaultGravityMode;
                m_TwoHandedGrabMoveProvider.leftGrabMoveProvider.gravityMode = k_DefaultGravityMode;
                m_TwoHandedGrabMoveProvider.rightGrabMoveProvider.gravityMode = k_DefaultGravityMode;
            }

            m_DynamicMoveProvider.enableFly = m_EnableFly;
            m_TwoHandedGrabMoveProvider.enableFreeYMovement = m_EnableFly;
            m_TwoHandedGrabMoveProvider.leftGrabMoveProvider.enableFreeYMovement = m_EnableFly;
            m_TwoHandedGrabMoveProvider.rightGrabMoveProvider.enableFreeYMovement = m_EnableFly;

            m_TwoHandedGrabMoveProvider.enabled = m_EnableGrabMovement;
            m_TwoHandedGrabMoveProvider.leftGrabMoveProvider.enabled = m_EnableGrabMovement;
            m_TwoHandedGrabMoveProvider.rightGrabMoveProvider.enabled = m_EnableGrabMovement;
        }

        void SetMoveScheme(LocomotionType scheme, bool leftHand)
        {
            var targetHand = leftHand ? m_LeftHandManager : m_RightHandManager;
            targetHand.smoothMotionEnabled = (scheme == LocomotionType.MoveAndStrafe);
        }

        void SetTurnStyle(TurnStyle style, bool leftHand)
        {
            var targetHand = leftHand ? m_LeftHandManager : m_RightHandManager;
            targetHand.smoothTurnEnabled = (style == TurnStyle.Smooth);
        }
    }
}
