using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

/// <summary>
/// Use this class to map input actions to each controller state (mode)
/// and set up the transitions between controller states (modes).
/// </summary>
[AddComponentMenu("XR/Action Based Controller Manager")]
[DefaultExecutionOrder(kControllerManagerUpdateOrder)]
public class ActionBasedControllerManager : MonoBehaviour
{
    public const int kControllerManagerUpdateOrder = 10;

    public enum StateId
    {
        None,
        Select,
        Teleport,
        Interact,
    }

    [Serializable]
    public class StateEnterEvent : UnityEvent<StateId> { }

    [Serializable]
    public class StateUpdateEvent : UnityEvent { }

    [Serializable]
    public class StateExitEvent : UnityEvent<StateId> { }

    /// <summary>
    /// Use this class to create a controller state and set up its enter, update, and exit events.
    /// </summary>
    [Serializable]
    public class ControllerState
    {
        [SerializeField] 
        [Tooltip("Sets the controller state to be active. " +
                 "For the default states, setting this value to true will automatically update their StateUpdateEvent.")]
        bool m_Enabled;
        /// <summary>
        /// Sets the controller state to be active.
        /// For the default states, setting this value to true will automatically update their <see cref="StateUpdateEvent"/>.
        /// </summary>
        public bool enabled
        {
            get => m_Enabled;
            set => m_Enabled = value;
        }

        [SerializeField]
        [HideInInspector]
        StateId m_Id;
        /// <summary>
        /// Sets the identifier of the <see cref="ControllerState"/> from all the optional Controller States that the <see cref="ActionBasedControllerManager"/> holds.
        /// </summary>
        public StateId id
        {
            get => m_Id;
            set => m_Id = value;
        }

        [SerializeField]
        StateEnterEvent m_OnEnter = new StateEnterEvent();
        /// <summary>
        /// The <see cref="StateEnterEvent"/> that will be invoked when entering the controller state.
        /// </summary>
        public StateEnterEvent onEnter
        {
            get => m_OnEnter;
            set => m_OnEnter = value;
        }

        [SerializeField]
        StateUpdateEvent m_OnUpdate = new StateUpdateEvent();
        /// <summary>
        /// The <see cref="StateUpdateEvent"/> that will be invoked when updating the controller state.
        /// </summary>
        public StateUpdateEvent onUpdate
        {
            get => m_OnUpdate;
            set => m_OnUpdate = value;
        } 
        
        [SerializeField]
        StateExitEvent m_OnExit = new StateExitEvent();
        /// <summary>
        /// The <see cref="StateExitEvent"/> that will be invoked when exiting the controller state.
        /// </summary>
        public StateExitEvent onExit
        {
            get => m_OnExit;
            set => m_OnExit = value;
        }

        public ControllerState(StateId defaultId = StateId.None) => this.id = defaultId;
    }
    
    [Space]
    [Header("Controller GameObjects") ]

    [SerializeField, FormerlySerializedAs("m_BaseControllerGO")]
    [Tooltip("The base controller GameObject, used for changing default settings on its components during state transitions.")]
    GameObject m_BaseControllerGameObject;
    /// <summary>
    /// The base controller <see cref="GameObject"/>, used for changing default settings on its components during state transitions.
    /// </summary>
    public GameObject baseControllerGameObject
    {
        get => m_BaseControllerGameObject;
        set => m_BaseControllerGameObject = value;
    }
    
    [SerializeField, FormerlySerializedAs("m_TeleportControllerGO")]
    [Tooltip("The teleport controller GameObject, used for changing default settings on its components during state transitions.")]
    GameObject m_TeleportControllerGameObject;
    /// <summary>
    /// The teleport controller <see cref="GameObject"/>, used for changing default settings on its components during state transitions.
    /// </summary>
    public GameObject teleportControllerGameObject
    {
        get => m_TeleportControllerGameObject;
        set => m_TeleportControllerGameObject = value;
    }

    [Space]
    [Header("Controller Actions") ]

    // State transition actions
    [SerializeField]
    [Tooltip("The reference to the action of activating the teleport mode for this controller.")]
    InputActionReference m_TeleportModeActivate;
    /// <summary>
    /// The reference to the action of activating the teleport mode for this controller."
    /// </summary>
    public InputActionReference teleportModeActivate
    {
        get => m_TeleportModeActivate;
        set => m_TeleportModeActivate = value;
    }

    [SerializeField]
    [Tooltip("The reference to the action of canceling the teleport mode for this controller.")]
    InputActionReference m_TeleportModeCancel;
    /// <summary>
    /// The reference to the action of canceling the teleport mode for this controller."
    /// </summary>
    public InputActionReference teleportModeCancel
    {
        get => m_TeleportModeCancel;
        set => m_TeleportModeCancel = value;
    }

    // Character movement actions
    [SerializeField]
    [Tooltip("The reference to the action of turning the XR rig with this controller.")]
    InputActionReference m_Turn;
    /// <summary>
    /// The reference to the action of turning the XR rig with this controller.
    /// </summary>
    public InputActionReference turn
    {
        get => m_Turn;
        set => m_Turn = value;
    }

    [SerializeField]
    [Tooltip("The reference to the action of moving the XR rig with this controller.")]
    InputActionReference m_Move;
    /// <summary>
    /// The reference to the action of moving the XR rig with this controller.
    /// </summary>
    public InputActionReference move
    {
        get => m_Move;
        set => m_Move = value;
    }

    // Object control actions
    [SerializeField, FormerlySerializedAs("m_TranslateObject")]
    [Tooltip("The reference to the action of translating the selected object of this controller.")]
    InputActionReference m_TranslateAnchor;
    /// <summary>
    /// The reference to the action of translating the selected object of this controller.
    /// </summary>
    public InputActionReference translateAnchor
    {
        get => m_TranslateAnchor;
        set => m_TranslateAnchor = value;
    }

    [SerializeField, FormerlySerializedAs("m_RotateObject")]
    [Tooltip("The reference to the action of rotating the selected object of this controller.")]
    InputActionReference m_RotateAnchor;
    /// <summary>
    /// The reference to the action of rotating the selected object of this controller.
    /// </summary>
    public InputActionReference rotateAnchor
    {
        get => m_RotateAnchor;
        set => m_RotateAnchor = value;
    }

    [Space]
    [Header("Default States") ]

#pragma warning disable IDE0044 // Add readonly modifier -- readonly fields cannot be serialized by Unity
    [SerializeField]
    [Tooltip("The default Select state and events for the controller.")]
    ControllerState m_SelectState = new ControllerState(StateId.Select);
    /// <summary>
    /// (Read Only) The default Select state.
    /// </summary>
    public ControllerState selectState => m_SelectState;

    [SerializeField]
    [Tooltip("The default Teleport state and events for the controller.")]
    ControllerState m_TeleportState = new ControllerState(StateId.Teleport);
    /// <summary>
    /// (Read Only) The default Teleport state.
    /// </summary>
    public ControllerState teleportState => m_TeleportState;

    [SerializeField]
    [Tooltip("The default Interact state and events for the controller.")]
    ControllerState m_InteractState = new ControllerState(StateId.Interact);
    /// <summary>
    /// (Read Only) The default Interact state.
    /// </summary>
    public ControllerState interactState => m_InteractState;
#pragma warning restore IDE0044

    // The list to store and run the default states
    readonly List<ControllerState> m_DefaultStates = new List<ControllerState>();

    // Components of the controller to switch on and off for different states
    XRBaseController m_BaseController;
    XRBaseInteractor m_BaseInteractor;
    XRInteractorLineVisual m_BaseLineVisual;

    XRBaseController m_TeleportController;
    XRBaseInteractor m_TeleportInteractor;
    XRInteractorLineVisual m_TeleportLineVisual;

    protected void OnEnable()
    {
        FindBaseControllerComponents();
        FindTeleportControllerComponents();

        // Add default state events.
        m_SelectState.onEnter.AddListener(OnEnterSelectState);
        m_SelectState.onUpdate.AddListener(OnUpdateSelectState);
        m_SelectState.onExit.AddListener(OnExitSelectState);

        m_TeleportState.onEnter.AddListener(OnEnterTeleportState);
        m_TeleportState.onUpdate.AddListener(OnUpdateTeleportState);
        m_TeleportState.onExit.AddListener(OnExitTeleportState);

        m_InteractState.onEnter.AddListener(OnEnterInteractState);
        m_InteractState.onUpdate.AddListener(OnUpdateInteractState);
        m_InteractState.onExit.AddListener(OnExitInteractState);
    }

    protected void OnDisable()
    {
        // Remove default state events.
        m_SelectState.onEnter.RemoveListener(OnEnterSelectState);
        m_SelectState.onUpdate.RemoveListener(OnUpdateSelectState);
        m_SelectState.onExit.RemoveListener(OnExitSelectState);

        m_TeleportState.onEnter.RemoveListener(OnEnterTeleportState);
        m_TeleportState.onUpdate.RemoveListener(OnUpdateTeleportState);
        m_TeleportState.onExit.RemoveListener(OnExitTeleportState);

        m_InteractState.onEnter.RemoveListener(OnEnterInteractState);
        m_InteractState.onUpdate.RemoveListener(OnUpdateInteractState);
        m_InteractState.onExit.RemoveListener(OnExitInteractState);
    }

    // Start is called before the first frame update
    protected void Start()
    {
        // Add states to the list
        m_DefaultStates.Add(m_SelectState);
        m_DefaultStates.Add(m_TeleportState);
        m_DefaultStates.Add(m_InteractState);

        // Initialize to start in m_SelectState
        TransitionState(null, m_SelectState);
    }

    // Update is called once per frame
    protected void Update()
    {
        foreach (var state in m_DefaultStates)
        {
            if (state.enabled)
            {
                state.onUpdate.Invoke();
                return;
            }
        }
    }

    void TransitionState(ControllerState fromState, ControllerState toState)
    {
        if (fromState != null)
        {
            fromState.enabled = false;
            fromState.onExit.Invoke(toState?.id ?? StateId.None);
        }

        if (toState != null)
        {
            toState.onEnter.Invoke(fromState?.id ?? StateId.None);
            toState.enabled = true;
        }
    }

    void FindBaseControllerComponents()
    {
        if (m_BaseControllerGameObject == null)
        {
            Debug.LogWarning("Missing reference to Base Controller GameObject.", this);
            return;
        }

        if (m_BaseController == null)
        {
            m_BaseController = m_BaseControllerGameObject.GetComponent<XRBaseController>();
            if (m_BaseController == null)
                Debug.LogWarning($"Cannot find any {nameof(XRBaseController)} component on the Base Controller GameObject.", this);
        }

        if (m_BaseInteractor == null)
        {
            m_BaseInteractor = m_BaseControllerGameObject.GetComponent<XRBaseInteractor>();
            if (m_BaseInteractor == null)
                Debug.LogWarning($"Cannot find any {nameof(XRBaseInteractor)} component on the Base Controller GameObject.", this);
        }

        // Only check the line visual component for RayInteractor, since DirectInteractor does not use the line visual component
        if (m_BaseInteractor is XRRayInteractor && m_BaseLineVisual == null)
        {
            m_BaseLineVisual = m_BaseControllerGameObject.GetComponent<XRInteractorLineVisual>();
            if (m_BaseLineVisual == null)
                Debug.LogWarning($"Cannot find any {nameof(XRInteractorLineVisual)} component on the Base Controller GameObject.", this);
        }
    }

    void FindTeleportControllerComponents()
    {
        if (m_TeleportControllerGameObject == null)
        {
            Debug.LogWarning("Missing reference to the Teleport Controller GameObject.", this);
            return;
        }

        if (m_TeleportController == null)
        {
            m_TeleportController = m_TeleportControllerGameObject.GetComponent<XRBaseController>();
            if (m_TeleportController == null)
                Debug.LogWarning($"Cannot find {nameof(XRBaseController)} component on the Teleport Controller GameObject.", this);
        }

        if (m_TeleportLineVisual == null)
        {
            m_TeleportLineVisual = m_TeleportControllerGameObject.GetComponent<XRInteractorLineVisual>();
            if (m_TeleportLineVisual == null)
                Debug.LogWarning($"Cannot find {nameof(XRInteractorLineVisual)} component on the Teleport Controller GameObject.", this);
        }

        if (m_TeleportInteractor == null)
        {
            m_TeleportInteractor = m_TeleportControllerGameObject.GetComponent<XRRayInteractor>();
            if (m_TeleportInteractor == null)
                Debug.LogWarning($"Cannot find {nameof(XRRayInteractor)} component on the Teleport Controller GameObject.", this);
        }
    }

    /// <summary>
    /// Find and configure the components on the base controller.
    /// </summary>
    /// <param name="enable"> Set it true to enable the base controller, false to disable it. </param>
    void SetBaseController(bool enable)
    {
        FindBaseControllerComponents();

        if (m_BaseController != null)
            m_BaseController.enableInputActions = enable;
        
        if (m_BaseInteractor != null)
            m_BaseInteractor.enabled = enable;
        
        if (m_BaseInteractor is XRRayInteractor && m_BaseLineVisual != null)
            m_BaseLineVisual.enabled = enable;
    }

    /// <summary>
    /// Find and configure the components on the teleport controller.
    /// </summary>
    /// <param name="enable"> Set it true to enable the teleport controller, false to disable it. </param>
    void SetTeleportController(bool enable)
    {
        FindTeleportControllerComponents();

        if (m_TeleportLineVisual != null) 
            m_TeleportLineVisual.enabled = enable;
        
        if (m_TeleportController != null)
            m_TeleportController.enableInputActions = enable;
        
        if (m_TeleportInteractor != null)
            m_TeleportInteractor.enabled = enable;
    }

    void OnEnterSelectState(StateId previousStateId)
    {
        // Change controller and enable actions depending on the previous state
        switch (previousStateId)
        {
            case StateId.None: 
                // Enable transitions to Teleport state 
                EnableAction(m_TeleportModeActivate);
                EnableAction(m_TeleportModeCancel);

                // Enable turn and move actions
                EnableAction(m_Turn);
                EnableAction(m_Move);

                // Enable base controller components
                SetBaseController(true);
                break;
            case StateId.Select:
                break;
            case StateId.Teleport:
                EnableAction(m_Turn);
                EnableAction(m_Move);
                SetBaseController(true);
                break;
            case StateId.Interact:
                EnableAction(m_Turn);
                EnableAction(m_Move);
                break;
            default:
                Debug.Assert(false, $"Unhandled case when entering Select from {previousStateId}.");
                break;
        }
    }

    void OnExitSelectState(StateId nextStateId)
    {
        // Change controller and disable actions depending on the next state
        switch (nextStateId)
        {
            case StateId.None:
                break;
            case StateId.Select:
                break;
            case StateId.Teleport:
                DisableAction(m_Turn);
                DisableAction(m_Move);
                SetBaseController(false);
                break;
            case StateId.Interact:
                DisableAction(m_Turn);
                DisableAction(m_Move);
                break;
            default:
                Debug.Assert(false, $"Unhandled case when exiting Select to {nextStateId}.");
                break;
        }
    }

    void OnEnterTeleportState(StateId previousStateId) => SetTeleportController(true); 

    void OnExitTeleportState(StateId nextStateId) => SetTeleportController(false);

    void OnEnterInteractState(StateId previousStateId)
    {
        // Enable object control actions
        EnableAction(m_TranslateAnchor);
        EnableAction(m_RotateAnchor);
    }

    void OnExitInteractState(StateId nextStateId)
    {
        // Disable object control actions
        DisableAction(m_TranslateAnchor);
        DisableAction(m_RotateAnchor);
    }

    /// <summary>
    /// This method is automatically called each frame to handle initiating transitions out of the Select state.
    /// </summary>
    void OnUpdateSelectState()
    {
        // Transition from Select state to Teleport state when the user triggers the "Teleport Mode Activate" action but not the "Cancel Teleport" action
        var teleportModeAction = GetInputAction(m_TeleportModeActivate);
        var cancelTeleportModeAction = GetInputAction(m_TeleportModeCancel);

        var triggerTeleportMode = teleportModeAction != null && teleportModeAction.triggered;
        var cancelTeleport = cancelTeleportModeAction != null && cancelTeleportModeAction.triggered;

        if (triggerTeleportMode && !cancelTeleport)
        {
            TransitionState(m_SelectState, m_TeleportState);
            return;
        }

        // Transition from Select state to Interact state when the interactor has a selectTarget
        FindBaseControllerComponents();

        if (m_BaseInteractor.selectTarget != null)
            TransitionState(m_SelectState, m_InteractState);
    }

    /// <summary>
    /// Updated every frame to handle the transition to m_SelectState state.
    /// </summary>
    void OnUpdateTeleportState()
    {
        // Transition from Teleport state to Select state when we release the Teleport trigger or cancel Teleport mode

        var teleportModeAction = GetInputAction(m_TeleportModeActivate);
        var cancelTeleportModeAction = GetInputAction(m_TeleportModeCancel);

        var cancelTeleport = cancelTeleportModeAction != null && cancelTeleportModeAction.triggered;
        var releasedTeleport = teleportModeAction != null && teleportModeAction.phase == InputActionPhase.Waiting;

        if (cancelTeleport || releasedTeleport) 
            TransitionState(m_TeleportState, m_SelectState);
    }

    void OnUpdateInteractState()
    {
        // Transition from Interact state to Select state when the base interactor no longer has a select target
        if (m_BaseInteractor.selectTarget == null)
            TransitionState(m_InteractState, m_SelectState);
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
