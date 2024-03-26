using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets
{
    /// <summary>
    /// Use this class to mediate the controllers and their associated interactors and input actions under different interaction states.
    /// </summary>
    [AddComponentMenu("XR/Action Based Controller Manager")]
    [DefaultExecutionOrder(k_UpdateOrder)]
    public class ActionBasedControllerManager : MonoBehaviour
    {
        /// <summary>
        /// Order when instances of type <see cref="ActionBasedControllerManager"/> are updated.
        /// </summary>
        /// <remarks>
        /// Executes before controller components to ensure input processors can be attached
        /// to input actions and/or bindings before the controller component reads the current
        /// values of the input actions.
        /// </remarks>
        public const int k_UpdateOrder = XRInteractionUpdateOrder.k_Controllers - 1;

        [Space]
        [Header("Interactors")]

        [SerializeField]
        [Tooltip("The GameObject containing the interaction group used for direct and distant manipulation.")]
        XRInteractionGroup m_ManipulationInteractionGroup;

        [SerializeField]
        [Tooltip("The GameObject containing the interactor used for direct manipulation.")]
        XRDirectInteractor m_DirectInteractor;

        [SerializeField]
        [Tooltip("The GameObject containing the interactor used for distant/ray manipulation.")]
        XRRayInteractor m_RayInteractor;

        [SerializeField]
        [Tooltip("The GameObject containing the interactor used for teleportation.")]
        XRRayInteractor m_TeleportInteractor;

        [Space]
        [Header("Controller Actions")]

        [SerializeField]
        [Tooltip("The reference to the action to start the teleport aiming mode for this controller.")]
        InputActionReference m_TeleportModeActivate;

        [SerializeField]
        [Tooltip("The reference to the action to cancel the teleport aiming mode for this controller.")]
        InputActionReference m_TeleportModeCancel;

        [SerializeField]
        [Tooltip("The reference to the action of continuous turning the XR Origin with this controller.")]
        InputActionReference m_Turn;

        [SerializeField]
        [Tooltip("The reference to the action of snap turning the XR Origin with this controller.")]
        InputActionReference m_SnapTurn;

        [SerializeField]
        [Tooltip("The reference to the action of moving the XR Origin with this controller.")]
        InputActionReference m_Move;

        [SerializeField]
        [Tooltip("The reference to the action of scrolling UI with this controller.")]
        InputActionReference m_UIScroll;

        [Space]
        [Header("Locomotion Settings")]

        [SerializeField]
        [Tooltip("If true, continuous movement will be enabled. If false, teleport will enabled.")]
        bool m_SmoothMotionEnabled;
        
        [SerializeField]
        [Tooltip("If true, continuous turn will be enabled. If false, snap turn will be enabled. Note: If smooth motion is enabled and enable strafe is enabled on the continuous move provider, turn will be overriden in favor of strafe.")]
        bool m_SmoothTurnEnabled;

        [Space]
        [Header("UI Settings")]

        [SerializeField]
        [Tooltip("If true, UI scrolling will be enabled.")]
        bool m_UIScrollingEnabled;

        [Space]
        [Header("Mediation Events")]
        [SerializeField]
        [Tooltip("Event fired when the active ray interactor changes between interaction and teleport.")]
        UnityEvent<IXRRayProvider> m_RayInteractorChanged;

        public bool smoothMotionEnabled
        {
            get => m_SmoothMotionEnabled;
            set
            {
                m_SmoothMotionEnabled = value;
                UpdateLocomotionActions();
            }
        }

        public bool smoothTurnEnabled
        {
            get => m_SmoothTurnEnabled;
            set
            {
                m_SmoothTurnEnabled = value;
                UpdateLocomotionActions();
            }
        }

        public bool uiScrollingEnabled
        {
            get => m_UIScrollingEnabled;
            set
            {
                m_UIScrollingEnabled = value;
                UpdateUIActions();
            }
        }

        bool m_StartCalled;
        bool m_PostponedDeactivateTeleport;
        bool m_HoveringScrollableUI;

        const int k_InteractorNotInGroup = -1;

        IEnumerator m_AfterInteractionEventsRoutine;
        HashSet<InputAction> m_LocomotionUsers = new HashSet<InputAction>();

        /// <summary>
        /// Temporary scratch list to populate with the group members of the interaction group.
        /// </summary>
        static readonly List<IXRGroupMember> s_GroupMembers = new List<IXRGroupMember>();

        // For our input mediation, we are enforcing a few rules between direct, ray, and teleportation interaction:
        // 1. If the Teleportation Ray is engaged, the Ray interactor is disabled
        // 2. The interaction group ensures that the Direct and Ray interactors cannot interact at the same time, with the Direct interactor taking priority
        // 3. If the Ray interactor is selecting, all locomotion controls are disabled (teleport ray, move, and turn controls) to prevent input collision
        void SetupInteractorEvents()
        {
            if (m_RayInteractor != null)
            {
                m_RayInteractor.selectEntered.AddListener(OnRaySelectEntered);
                m_RayInteractor.selectExited.AddListener(OnRaySelectExited);
                m_RayInteractor.uiHoverEntered.AddListener(OnUIHoverEntered);
                m_RayInteractor.uiHoverExited.AddListener(OnUIHoverExited);
            }

            var teleportModeActivateAction = GetInputAction(m_TeleportModeActivate);
            if (teleportModeActivateAction != null)
            {
                teleportModeActivateAction.performed += OnStartTeleport;
                teleportModeActivateAction.performed += OnStartLocomotion;
                teleportModeActivateAction.canceled += OnCancelTeleport;
                teleportModeActivateAction.canceled += OnStopLocomotion;
            }

            var teleportModeCancelAction = GetInputAction(m_TeleportModeCancel);
            if (teleportModeCancelAction != null)
            {
                teleportModeCancelAction.performed += OnCancelTeleport;
            }

            var moveAction = GetInputAction(m_Move);
            if (moveAction != null)
            {
                moveAction.started += OnStartLocomotion;
                moveAction.canceled += OnStopLocomotion;
            }

            var turnAction = GetInputAction(m_Turn);
            if (turnAction != null)
            {
                turnAction.started += OnStartLocomotion;
                turnAction.canceled += OnStopLocomotion;
            }

            var snapTurnAction = GetInputAction(m_SnapTurn);
            if (snapTurnAction != null)
            {
                snapTurnAction.started += OnStartLocomotion;
                snapTurnAction.canceled += OnStopLocomotion;
            }
        }

        void TeardownInteractorEvents()
        {
            if (m_RayInteractor != null)
            {
                m_RayInteractor.selectEntered.RemoveListener(OnRaySelectEntered);
                m_RayInteractor.selectExited.RemoveListener(OnRaySelectExited);
            }

            var teleportModeActivateAction = GetInputAction(m_TeleportModeActivate);
            if (teleportModeActivateAction != null)
            {
                teleportModeActivateAction.performed -= OnStartTeleport;
                teleportModeActivateAction.performed -= OnStartLocomotion;
                teleportModeActivateAction.canceled -= OnCancelTeleport;
                teleportModeActivateAction.canceled -= OnStopLocomotion;
            }

            var teleportModeCancelAction = GetInputAction(m_TeleportModeCancel);
            if (teleportModeCancelAction != null)
            {
                teleportModeCancelAction.performed -= OnCancelTeleport;
            }

            var moveAction = GetInputAction(m_Move);
            if (moveAction != null)
            {
                moveAction.started -= OnStartLocomotion;
                moveAction.canceled -= OnStopLocomotion;
            }

            var turnAction = GetInputAction(m_Turn);
            if (turnAction != null)
            {
                turnAction.started -= OnStartLocomotion;
                turnAction.canceled -= OnStopLocomotion;
            }

            var snapTurnAction = GetInputAction(m_SnapTurn);
            if (snapTurnAction != null)
            {
                snapTurnAction.started -= OnStartLocomotion;
                snapTurnAction.canceled -= OnStopLocomotion;
            }
        }

        void OnStartTeleport(InputAction.CallbackContext context)
        {
            m_PostponedDeactivateTeleport = false;

            if (m_TeleportInteractor != null)
                m_TeleportInteractor.gameObject.SetActive(true);

            if (m_RayInteractor != null)
                m_RayInteractor.gameObject.SetActive(false);

            m_RayInteractorChanged?.Invoke(m_TeleportInteractor);
        }

        void OnCancelTeleport(InputAction.CallbackContext context)
        {
            // Do not deactivate the teleport interactor in this callback.
            // We delay turning off the teleport interactor in this callback so that
            // the teleport interactor has a chance to complete the teleport if needed.
            // OnAfterInteractionEvents will handle deactivating its GameObject.
            m_PostponedDeactivateTeleport = true;

            if (m_RayInteractor != null)
                m_RayInteractor.gameObject.SetActive(true);

            m_RayInteractorChanged?.Invoke(m_RayInteractor);

        }

        void OnStartLocomotion(InputAction.CallbackContext context)
        {
            m_LocomotionUsers.Add(context.action);
        }

        void OnStopLocomotion(InputAction.CallbackContext context)
        {
            m_LocomotionUsers.Remove(context.action);

            if (m_LocomotionUsers.Count == 0 && m_HoveringScrollableUI)
            {
                DisableLocomotionActions();
                UpdateUIActions();
            }
        }

        void OnRaySelectEntered(SelectEnterEventArgs args)
        {
            // Disable locomotion and turn actions
            DisableLocomotionActions();
        }

        void OnRaySelectExited(SelectExitEventArgs args)
        {
            // Re-enable the locomotion and turn actions
            UpdateLocomotionActions();
        }

        void OnUIHoverEntered(UIHoverEventArgs args)
        {
            m_HoveringScrollableUI = m_UIScrollingEnabled && args.deviceModel.isScrollable;
            UpdateUIActions();

            // If locomotion is occurring, wait
            if (m_HoveringScrollableUI && m_LocomotionUsers.Count == 0)
            {
                // Disable locomotion and turn actions
                DisableLocomotionActions();
            }
        }

        void OnUIHoverExited(UIHoverEventArgs args)
        {
            m_HoveringScrollableUI = false;
            UpdateUIActions();

            // Re-enable the locomotion and turn actions
            UpdateLocomotionActions();
        }

        protected void Awake()
        {
            m_AfterInteractionEventsRoutine = OnAfterInteractionEvents();
        }

        protected void OnEnable()
        {
            if (m_TeleportInteractor != null)
                m_TeleportInteractor.gameObject.SetActive(false);

            // Allow the locomotion actions to be refreshed when this is re-enabled.
            // See comments in Start for why we wait until Start to enable/disable locomotion actions.
            if (m_StartCalled)
                UpdateLocomotionActions();

            SetupInteractorEvents();

            // Start the coroutine that executes code after the Update phase (during yield null).
            // Since this behavior has an execution order that runs before the XRInteractionManager,
            // we use the coroutine to run after the selection events
            StartCoroutine(m_AfterInteractionEventsRoutine);
        }

        protected void OnDisable()
        {
            TeardownInteractorEvents();

            StopCoroutine(m_AfterInteractionEventsRoutine);
        }

        protected void Start()
        {
            m_StartCalled = true;

            // Ensure the enabled state of locomotion and turn actions are properly set up.
            // Called in Start so it is done after the InputActionManager enables all input actions earlier in OnEnable.
            UpdateLocomotionActions();
            UpdateUIActions();

            if (m_ManipulationInteractionGroup == null)
            {
                Debug.LogError("Missing required Manipulation Interaction Group reference. Use the Inspector window to assign the XR Interaction Group component reference.", this);
                return;
            }

            // Ensure interactors are properly set up in the interaction group by adding
            // them if necessary and ordering Direct before Ray interactor.
            var directInteractorIndex = k_InteractorNotInGroup;
            var rayInteractorIndex = k_InteractorNotInGroup;
            m_ManipulationInteractionGroup.GetGroupMembers(s_GroupMembers);
            for (var i = 0; i < s_GroupMembers.Count; ++i)
            {
                var groupMember = s_GroupMembers[i];
                if (ReferenceEquals(groupMember, m_DirectInteractor))
                    directInteractorIndex = i;
                else if (ReferenceEquals(groupMember, m_RayInteractor))
                    rayInteractorIndex = i;
            }

            if (directInteractorIndex == k_InteractorNotInGroup)
            {
                // Must add Direct interactor to group, and make sure it is ordered before the Ray interactor
                if (rayInteractorIndex == k_InteractorNotInGroup)
                {
                    // Must add Ray interactor to group
                    if (m_DirectInteractor != null)
                        m_ManipulationInteractionGroup.AddGroupMember(m_DirectInteractor);

                    if (m_RayInteractor != null)
                        m_ManipulationInteractionGroup.AddGroupMember(m_RayInteractor);
                }
                else if (m_DirectInteractor != null)
                {
                    m_ManipulationInteractionGroup.MoveGroupMemberTo(m_DirectInteractor, rayInteractorIndex);
                }
            }
            else
            {
                if (rayInteractorIndex == k_InteractorNotInGroup)
                {
                    // Must add Ray interactor to group
                    if (m_RayInteractor != null)
                        m_ManipulationInteractionGroup.AddGroupMember(m_RayInteractor);
                }
                else
                {
                    // Must make sure Direct interactor is ordered before the Ray interactor
                    if (rayInteractorIndex < directInteractorIndex)
                    {
                        m_ManipulationInteractionGroup.MoveGroupMemberTo(m_DirectInteractor, rayInteractorIndex);
                    }
                }
            }
        }

        IEnumerator OnAfterInteractionEvents()
        {
            while (true)
            {
                // Yield so this coroutine is resumed after the teleport interactor
                // has a chance to process its select interaction event during Update.
                yield return null;

                if (m_PostponedDeactivateTeleport)
                {
                    if (m_TeleportInteractor != null)
                        m_TeleportInteractor.gameObject.SetActive(false);

                    m_PostponedDeactivateTeleport = false;
                }
            }
        }

        void UpdateLocomotionActions()
        {
            // Disable/enable Teleport and Turn when Move is enabled/disabled.
            SetEnabled(m_Move, m_SmoothMotionEnabled);
            SetEnabled(m_TeleportModeActivate, !m_SmoothMotionEnabled);
            SetEnabled(m_TeleportModeCancel, !m_SmoothMotionEnabled);

            // Disable ability to turn when using continuous movement
            SetEnabled(m_Turn, !m_SmoothMotionEnabled && m_SmoothTurnEnabled);
            SetEnabled(m_SnapTurn, !m_SmoothMotionEnabled && !m_SmoothTurnEnabled);
        }

        void DisableLocomotionActions()
        {
            DisableAction(m_Move);
            DisableAction(m_TeleportModeActivate);
            DisableAction(m_TeleportModeCancel);
            DisableAction(m_Turn);
            DisableAction(m_SnapTurn);
        }

        void UpdateUIActions()
        {
            SetEnabled(m_UIScroll, m_UIScrollingEnabled && m_HoveringScrollableUI && m_LocomotionUsers.Count == 0);
        }

        static void SetEnabled(InputActionReference actionReference, bool enabled)
        {
            if (enabled)
                EnableAction(actionReference);
            else
                DisableAction(actionReference);
        }

        static void EnableAction(InputActionReference actionReference)
        {
            var action = GetInputAction(actionReference);
            if (action != null && !action.enabled)
                action.Enable();
        }

        static void DisableAction(InputActionReference actionReference)
        {
            var action = GetInputAction(actionReference);
            if (action != null && action.enabled)
                action.Disable();
        }

        static InputAction GetInputAction(InputActionReference actionReference)
        {
#pragma warning disable IDE0031 // Use null propagation -- Do not use for UnityEngine.Object types
            return actionReference != null ? actionReference.action : null;
#pragma warning restore IDE0031
        }
    }
}
