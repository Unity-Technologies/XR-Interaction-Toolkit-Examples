using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRBuilder.XRInteraction
{
    /// <summary>
    /// Use this class to map input actions to each controller state (mode)
    /// and set up the transitions between controller states (modes).
    /// </summary>
    [AddComponentMenu("XR/Action Based Controller Manager")]
    [DefaultExecutionOrder(ControllerManagerUpdateOrder)]
    public class ActionBasedControllerManager : MonoBehaviour
    {
        private const int ControllerManagerUpdateOrder = 10;

        /// <summary>
        /// Reference to an interaction state.
        /// </summary>
        public enum StateID
        {
            None,
            Select,
            Teleport,
            Interact,
            UI
        }

        [Serializable]
        public class StateEnterEvent : UnityEvent<StateID>
        {
        }

        [Serializable]
        public class StateUpdateEvent : UnityEvent
        {
        }

        [Serializable]
        public class StateExitEvent : UnityEvent<StateID>
        {
        }

        /// <summary>
        /// Use this class to create a controller state and set up its enter, update, and exit events.
        /// </summary>
        [Serializable]
        public class ControllerState
        {
            [SerializeField]
            [Tooltip("Sets the controller state to be active. " +
                "For the default states, setting this value to true will automatically update their StateUpdateEvent.")]
            private bool enabled;

            /// <summary>
            /// Sets the controller state to be active.
            /// For the default states, setting this value to true will automatically update their <see cref="StateUpdateEvent"/>.
            /// </summary>
            public bool Enabled
            {
                get => enabled;
                set => enabled = value;
            }

            [SerializeField]
            [HideInInspector]
            private StateID m_ID;

            /// <summary>
            /// Sets the identifier of the <see cref="ControllerState"/> from all the optional Controller States that the <see cref="ActionBasedControllerManager"/> holds.
            /// </summary>
            public StateID ID
            {
                get => m_ID;
                set => m_ID = value;
            }

            [SerializeField]
            private StateEnterEvent onEnter = new StateEnterEvent();

            /// <summary>
            /// The <see cref="StateEnterEvent"/> that will be invoked when entering the controller state.
            /// </summary>
            public StateEnterEvent OnEnter
            {
                get => onEnter;
                set => onEnter = value;
            }

            [SerializeField]
            private StateUpdateEvent onUpdate = new StateUpdateEvent();

            /// <summary>
            /// The <see cref="StateUpdateEvent"/> that will be invoked when updating the controller state.
            /// </summary>
            public StateUpdateEvent OnUpdate
            {
                get => onUpdate;
                set => onUpdate = value;
            }

            [SerializeField]
            private StateExitEvent onExit = new StateExitEvent();

            /// <summary>
            /// The <see cref="StateExitEvent"/> that will be invoked when exiting the controller state.
            /// </summary>
            public StateExitEvent OnExit
            {
                get => onExit;
                set => onExit = value;
            }

            public ControllerState(StateID defaultId = StateID.None) => ID = defaultId;
        }

        [Space]
        [Header("Controller GameObjects")]

        [SerializeField]
        [Tooltip("The base controller GameObject, used for changing default settings on its components during state transitions.")]
        private GameObject baseController;

        /// <summary>
        /// The base controller <see cref="GameObject"/>, used for changing default settings on its components during state transitions.
        /// </summary>
        public GameObject BaseController
        {
            get => baseController;
            set => baseController = value;
        }

        [SerializeField]
        [Tooltip("The teleport controller GameObject, used for changing default settings on its components during state transitions.")]
        private GameObject teleportController;

        /// <summary>
        /// The teleport controller <see cref="GameObject"/>, used for changing default settings on its components during state transitions.
        /// </summary>
        public GameObject TeleportController
        {
            get => teleportController;
            set => teleportController = value;
        }

        [SerializeField]
        [Tooltip("The UI controller GameObject, used for changing default settings on its components during state transitions.")]
        private GameObject uiController;

        /// <summary>
        /// The UI controller <see cref="GameObject"/>, used for changing default settings on its components during state transitions.
        /// </summary>
        public GameObject UIController
        {
            get => uiController;
            set => uiController = value;
        }

        [Space]
        [Header("Controller Actions")]

        // State transition actions
        [SerializeField]
        [Tooltip("The reference to the action of activating the teleport mode for this controller.")]
        private InputActionReference teleportModeActivate;

        /// <summary>
        /// The reference to the action of activating the teleport mode for this controller."
        /// </summary>
        public InputActionReference TeleportModeActivate
        {
            get => teleportModeActivate;
            set => teleportModeActivate = value;
        }

        [SerializeField]
        [Tooltip("The reference to the action of canceling the teleport mode for this controller.")]
        private InputActionReference teleportModeCancel;

        /// <summary>
        /// The reference to the action of canceling the teleport mode for this controller."
        /// </summary>
        public InputActionReference TeleportModeCancel
        {
            get => teleportModeCancel;
            set => teleportModeCancel = value;
        }

        [SerializeField]
        [Tooltip("The reference to the action of activating the teleport mode for this controller.")]
        private InputActionReference uiModeActivate;

        /// <summary>
        /// The reference to the action of activating the teleport mode for this controller."
        /// </summary>
        public InputActionReference UIModeActivate
        {
            get => uiModeActivate;
            set => uiModeActivate = value;
        }

        // Character movement actions
        [SerializeField]
        [Tooltip("The reference to the action of turning the XR rig with this controller.")]
        private InputActionReference turn;

        /// <summary>
        /// The reference to the action of turning the XR rig with this controller.
        /// </summary>
        public InputActionReference Turn
        {
            get => turn;
            set => turn = value;
        }

        [SerializeField]
        [Tooltip("The reference to the action of moving the XR rig with this controller.")]
        private InputActionReference move;

        /// <summary>
        /// The reference to the action of moving the XR rig with this controller.
        /// </summary>
        public InputActionReference Move
        {
            get => move;
            set => move = value;
        }

        // Object control actions
        [SerializeField]
        [Tooltip("The reference to the action of translating the selected object of this controller.")]
        private InputActionReference translateAnchor;

        /// <summary>
        /// The reference to the action of translating the selected object of this controller.
        /// </summary>
        public InputActionReference TranslateAnchor
        {
            get => translateAnchor;
            set => translateAnchor = value;
        }

        [SerializeField]
        [Tooltip("The reference to the action of rotating the selected object of this controller.")]
        private InputActionReference rotateAnchor;

        /// <summary>
        /// The reference to the action of rotating the selected object of this controller.
        /// </summary>
        public InputActionReference RotateAnchor
        {
            get => rotateAnchor;
            set => rotateAnchor = value;
        }

        [Space]
        [Header("Default States")]

#pragma warning disable IDE0044 // Add readonly modifier -- readonly fields cannot be serialized by Unity
        [SerializeField]
        [Tooltip("The default Select state and events for the controller.")]
        private ControllerState selectState = new ControllerState(StateID.Select);

        /// <summary>
        /// (Read Only) The default Select state.
        /// </summary>
        public ControllerState SelectState => selectState;

        [SerializeField]
        [Tooltip("The default Teleport state and events for the controller.")]
        private ControllerState teleportState = new ControllerState(StateID.Teleport);

        /// <summary>
        /// (Read Only) The default Teleport state.
        /// </summary>
        public ControllerState TeleportState => teleportState;

        [SerializeField]
        [Tooltip("The default Interact state and events for the controller.")]
        private ControllerState interactState = new ControllerState(StateID.Interact);

        /// <summary>
        /// (Read Only) The default Interact state.
        /// </summary>
        public ControllerState InteractState => interactState;

        [SerializeField]
        [Tooltip("The default Interact state and events for the controller.")]
        private ControllerState uiState = new ControllerState(StateID.UI);

        /// <summary>
        /// (Read Only) The default Interact state.
        /// </summary>
        public ControllerState UIState => uiState;
#pragma warning restore IDE0044

        // The list to store and run the default states
        private readonly List<ControllerState> defaultStates = new List<ControllerState>();

        // Components of the controller to switch on and off for different states
        private XRBaseController baseXRController;
        private XRBaseInteractor baseXRInteractor;
        private XRInteractorLineVisual baseXRLineVisual;

        private XRBaseController teleportXRController;
        private XRBaseInteractor teleportXRInteractor;
        private XRInteractorLineVisual teleportLineVisual;

        private XRBaseController uiXRController;
        private XRBaseInteractor uiXRInteractor;
        private XRInteractorLineVisual uiLineVisual;

        protected void OnEnable()
        {
            FindBaseControllerComponents();
            FindTeleportControllerComponents();
            FindUIControllerComponents();

            // Add default state events.
            selectState.OnEnter.AddListener(OnEnterSelectState);
            selectState.OnUpdate.AddListener(OnUpdateSelectState);
            selectState.OnExit.AddListener(OnExitSelectState);

            teleportState.OnEnter.AddListener(OnEnterTeleportState);
            teleportState.OnUpdate.AddListener(OnUpdateTeleportState);
            teleportState.OnExit.AddListener(OnExitTeleportState);

            interactState.OnEnter.AddListener(OnEnterInteractState);
            interactState.OnUpdate.AddListener(OnUpdateInteractState);
            interactState.OnExit.AddListener(OnExitInteractState);

            uiState.OnEnter.AddListener(OnEnterUIState);
            uiState.OnUpdate.AddListener(OnUpdateUIState);
            uiState.OnExit.AddListener(OnExitUIState);
        }

        protected void OnDisable()
        {
            // Remove default state events.
            selectState.OnEnter.RemoveListener(OnEnterSelectState);
            selectState.OnUpdate.RemoveListener(OnUpdateSelectState);
            selectState.OnExit.RemoveListener(OnExitSelectState);

            teleportState.OnEnter.RemoveListener(OnEnterTeleportState);
            teleportState.OnUpdate.RemoveListener(OnUpdateTeleportState);
            teleportState.OnExit.RemoveListener(OnExitTeleportState);

            interactState.OnEnter.RemoveListener(OnEnterInteractState);
            interactState.OnUpdate.RemoveListener(OnUpdateInteractState);
            interactState.OnExit.RemoveListener(OnExitInteractState);

            uiState.OnEnter.RemoveListener(OnEnterUIState);
            uiState.OnUpdate.RemoveListener(OnUpdateUIState);
            uiState.OnExit.RemoveListener(OnExitUIState);
        }

        protected void Start()
        {
            // Add states to the list
            defaultStates.Add(selectState);
            defaultStates.Add(teleportState);
            defaultStates.Add(interactState);
            defaultStates.Add(uiState);

            // Initialize to start in m_SelectState
            TransitionState(null, selectState);
        }

        protected void Update()
        {
            foreach (ControllerState state in defaultStates)
            {
                if (state.Enabled)
                {
                    state.OnUpdate.Invoke();
                    return;
                }
            }
        }

        private void TransitionState(ControllerState fromState, ControllerState toState)
        {
            if (fromState != null)
            {
                fromState.Enabled = false;
                fromState.OnExit.Invoke(toState?.ID ?? StateID.None);
            }

            if (toState != null)
            {
                toState.OnEnter.Invoke(fromState?.ID ?? StateID.None);
                toState.Enabled = true;
            }
        }

        private void FindBaseControllerComponents()
        {
            if (baseController == null)
            {
                Debug.LogWarning("Missing reference to Base Controller GameObject.", this);
                return;
            }

            if (baseXRController == null)
            {
                baseXRController = baseController.GetComponent<XRBaseController>();

                if (baseXRController == null)
                {
                    Debug.LogWarning($"Cannot find any {nameof(XRBaseController)} component on the Base Controller GameObject.", this);
                }
            }

            if (baseXRInteractor == null)
            {
                baseXRInteractor = baseController.GetComponent<XRBaseInteractor>();

                if (baseXRInteractor == null)
                {
                    Debug.LogWarning($"Cannot find any {nameof(XRBaseInteractor)} component on the Base Controller GameObject.", this);
                }
            }

            // Only check the line visual component for RayInteractor, since DirectInteractor does not use the line visual component
            if (baseXRInteractor is XRRayInteractor && baseXRLineVisual == null)
            {
                baseXRLineVisual = baseController.GetComponent<XRInteractorLineVisual>();

                if (baseXRLineVisual == null)
                {
                    Debug.LogWarning($"Cannot find any {nameof(XRInteractorLineVisual)} component on the Base Controller GameObject.", this);
                }
            }
        }

        private void FindTeleportControllerComponents()
        {
            if (teleportController == null)
            {
                Debug.LogWarning("Missing reference to the Teleport Controller GameObject.", this);
                return;
            }

            if (teleportXRController == null)
            {
                teleportXRController = teleportController.GetComponent<XRBaseController>();

                if (teleportXRController == null)
                {
                    Debug.LogWarning($"Cannot find {nameof(XRBaseController)} component on the Teleport Controller GameObject.", this);
                }
            }

            if (teleportLineVisual == null)
            {
                teleportLineVisual = teleportController.GetComponent<XRInteractorLineVisual>();

                if (teleportLineVisual == null)
                {
                    Debug.LogWarning($"Cannot find {nameof(XRInteractorLineVisual)} component on the Teleport Controller GameObject.", this);
                }
            }

            if (teleportXRInteractor == null)
            {
                teleportXRInteractor = teleportController.GetComponent<XRRayInteractor>();

                if (teleportXRInteractor == null)
                {
                    Debug.LogWarning($"Cannot find {nameof(XRRayInteractor)} component on the Teleport Controller GameObject.", this);
                }
            }
        }

        private void FindUIControllerComponents()
        {
            if (uiController == null)
            {
                Debug.LogWarning("Missing reference to the UI Controller GameObject.", this);
                return;
            }

            if (uiXRController == null)
            {
                uiXRController = uiController.GetComponent<XRBaseController>();

                if (uiXRController == null)
                {
                    Debug.LogWarning($"Cannot find {nameof(XRBaseController)} component on the UI Controller GameObject.", this);
                }
            }

            if (uiLineVisual == null)
            {
                uiLineVisual = uiController.GetComponent<XRInteractorLineVisual>();

                if (uiLineVisual == null)
                {
                    Debug.LogWarning($"Cannot find {nameof(XRInteractorLineVisual)} component on the UI Controller GameObject.", this);
                }
            }

            if (uiXRInteractor == null)
            {
                uiXRInteractor = uiController.GetComponent<XRRayInteractor>();

                if (uiXRInteractor == null)
                {
                    Debug.LogWarning($"Cannot find {nameof(XRRayInteractor)} component on the UI Controller GameObject.", this);
                }
            }
        }

        /// <summary>
        /// Find and configure the components on the base controller.
        /// </summary>
        /// <param name="enable"> Set it true to enable the base controller, false to disable it. </param>
        private void SetBaseController(bool enable)
        {
            FindBaseControllerComponents();

            if (baseXRController != null)
            {
                baseXRController.enableInputActions = enable;
            }

            if (baseXRInteractor != null)
            {
                baseXRInteractor.enabled = enable;
            }

            if (baseXRInteractor is XRRayInteractor && baseXRLineVisual != null)
            {
                baseXRLineVisual.enabled = enable;
            }
        }

        /// <summary>
        /// Find and configure the components on the teleport controller.
        /// </summary>
        /// <param name="enable"> Set it true to enable the teleport controller, false to disable it. </param>
        private void SetTeleportController(bool enable)
        {
            FindTeleportControllerComponents();

            if (teleportLineVisual != null)
            {
                teleportLineVisual.enabled = enable;
            }

            if (teleportXRController != null)
            {
                teleportXRController.enableInputActions = enable;
            }

            if (teleportXRInteractor != null)
            {
                teleportXRInteractor.enabled = enable;
            }
        }

        /// <summary>
        /// Find and configure the components on the UI controller.
        /// </summary>
        /// <param name="enable"> Set it true to enable the UI controller, false to disable it. </param>
        private void SetUIController(bool enable)
        {
            FindUIControllerComponents();

            if (uiLineVisual != null)
            {
                uiLineVisual.enabled = enable;
            }

            if (uiXRController != null)
            {
                uiXRController.enableInputActions = enable;
            }

            if (uiXRInteractor != null)
            {
                uiXRInteractor.enabled = enable;
            }
        }

        private void OnEnterSelectState(StateID previousStateId)
        {
            // Change controller and enable actions depending on the previous state
            switch (previousStateId)
            {
                case StateID.None:
                    // Enable transitions to Teleport state 
                    EnableAction(teleportModeActivate);
                    EnableAction(teleportModeCancel);

                    // Enable turn and move actions
                    EnableAction(turn);
                    EnableAction(move);

                    // Enable base controller components
                    SetBaseController(true);
                    break;
                case StateID.Select:
                    break;
                case StateID.Teleport:
                case StateID.UI:
                    EnableAction(turn);
                    EnableAction(move);
                    SetBaseController(true);
                    break;
                case StateID.Interact:
                    EnableAction(turn);
                    EnableAction(move);
                    break;
                default:
                    Debug.Assert(false, $"Unhandled case when entering Select from {previousStateId}.");
                    break;
            }
        }

        private void OnExitSelectState(StateID nextStateId)
        {
            // Change controller and disable actions depending on the next state
            switch (nextStateId)
            {
                case StateID.None:
                case StateID.Select:
                    break;
                case StateID.Teleport:
                case StateID.UI:
                    DisableAction(turn);
                    DisableAction(move);
                    SetBaseController(false);
                    break;
                case StateID.Interact:
                    DisableAction(turn);
                    DisableAction(move);
                    break;
                default:
                    Debug.Assert(false, $"Unhandled case when exiting Select to {nextStateId}.");
                    break;
            }
        }

        private void OnEnterTeleportState(StateID previousStateId) => SetTeleportController(true);

        private void OnExitTeleportState(StateID nextStateId) => SetTeleportController(false);

        private void OnEnterInteractState(StateID previousStateId)
        {
            // Enable object control actions
            EnableAction(translateAnchor);
            EnableAction(rotateAnchor);
        }

        private void OnExitInteractState(StateID nextStateId)
        {
            // Disable object control actions
            DisableAction(translateAnchor);
            DisableAction(rotateAnchor);
        }

        private void OnEnterUIState(StateID previousStateId) => SetUIController(true);

        private void OnExitUIState(StateID nextStateId) => SetUIController(false);

        /// <summary>
        /// This method is automatically called each frame to handle initiating transitions out of the Select state.
        /// </summary>
        private void OnUpdateSelectState()
        {
            if (IsInteractorInteracting())
            {
                return;
            }
            
            // Transition from Select state to Teleport state when the user triggers the "Teleport Mode Activate" action but not the "Cancel Teleport" action
            InputAction teleportModeAction = GetInputAction(teleportModeActivate);
            InputAction cancelTeleportModeAction = GetInputAction(teleportModeCancel);
            InputAction uiModeAction = GetInputAction(uiModeActivate);

            bool isUIModeTriggered = uiModeAction != null && uiModeAction.triggered;
            bool isTriggerTeleportMode = teleportModeAction != null && teleportModeAction.triggered;
            bool shouldCancelTeleport = cancelTeleportModeAction != null && cancelTeleportModeAction.triggered;

            if (isUIModeTriggered)
            {
                TransitionState(selectState, uiState);
                return;
            }
            else if (isTriggerTeleportMode && shouldCancelTeleport == false)
            {
                TransitionState(selectState, teleportState);
                return;
            }

            // Transition from Select state to Interact state when the interactor has a selectTarget
            FindBaseControllerComponents();

            if (baseXRInteractor.hasSelection)
            {
                TransitionState(selectState, interactState);
            }
        }

        /// <summary>
        /// Updated every frame to handle the transition to m_SelectState state.
        /// </summary>
        private void OnUpdateTeleportState()
        {
            // Transition from Teleport state to Select state when we release the Teleport trigger or cancel Teleport mode
            InputAction teleportModeAction = GetInputAction(teleportModeActivate);
            InputAction cancelTeleportModeAction = GetInputAction(teleportModeCancel);

            bool shouldCancelTeleport = cancelTeleportModeAction != null && cancelTeleportModeAction.triggered;
            bool isTeleportModeReleased = teleportModeAction != null && teleportModeAction.phase == InputActionPhase.Waiting;

            if (shouldCancelTeleport || isTeleportModeReleased)
            {
                TransitionState(teleportState, selectState);
            }
        }

        private void OnUpdateInteractState()
        {
            // Transition from Interact state to Select state when the base interactor no longer has a select target
            if (baseXRInteractor.hasSelection == false)
            {
                TransitionState(interactState, selectState);
            }
        }

        private void OnUpdateUIState()
        {
            // Transition from UI state to Select state when we release the UI trigger
            InputAction isUIModeTriggered = GetInputAction(uiModeActivate);

            bool isButtonReleased = isUIModeTriggered != null && isUIModeTriggered.phase == InputActionPhase.Waiting;

            if (isButtonReleased)
            {
                TransitionState(uiState, selectState);
            }
        }

        private void EnableAction(InputActionReference actionReference)
        {
            InputAction action = GetInputAction(actionReference);

            if (action != null && !action.enabled)
            {
                action.Enable();
            }
        }

        private void DisableAction(InputActionReference actionReference)
        {
            InputAction action = GetInputAction(actionReference);

            if (action != null && action.enabled)
            {
                action.Disable();
            }
        }
        
        private bool IsInteractorInteracting()
        {
            if (baseXRInteractor == null)
            {
                return false;
            }

            return baseXRInteractor.interactablesHovered.Any() || baseXRInteractor.hasSelection;
        }

        private InputAction GetInputAction(InputActionReference actionReference)
        {
            return actionReference != null ? actionReference.action : null;
        }
    }
}