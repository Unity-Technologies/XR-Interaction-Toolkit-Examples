using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

[DefaultExecutionOrder(kControllerManagerUpdateOrder)]
public class ControllerManager : MonoBehaviour
{
    // Slightly after the default, so that any actions such as release or grab can be processed *before* we switch controllers.
    public const int kControllerManagerUpdateOrder = 10;

    [SerializeField]
    [Tooltip("The buttons on the controller that will trigger a transition to the Teleport Controller.")]
    List<InputHelpers.Button> m_ActivationButtons = new List<InputHelpers.Button>();
    /// <summary>
    /// The buttons on the controller that will trigger a transition to the Teleport Controller.
    /// </summary>
    public List<InputHelpers.Button> activationButtons
    {
        get => m_ActivationButtons;
        set => m_ActivationButtons = value;
    }

    [SerializeField]
    [Tooltip("The buttons on the controller that will force a deactivation of the teleport option.")]
    List<InputHelpers.Button> m_DeactivationButtons = new List<InputHelpers.Button>();
    /// <summary>
    /// The buttons on the controller that will trigger a transition to the Teleport Controller.
    /// </summary>
    public List<InputHelpers.Button> deactivationButtons
    {
        get => m_DeactivationButtons;
        set => m_DeactivationButtons = value;
    }

    [SerializeField]
    [Tooltip("The Game Object which represents the left hand for normal interaction purposes.")]
    GameObject m_LeftBaseController;
    /// <summary>
    /// The Game Object which represents the left hand for normal interaction purposes.
    /// </summary>
    public GameObject leftBaseController
    {
        get => m_LeftBaseController;
        set => m_LeftBaseController = value;
    }

    [SerializeField]
    [Tooltip("The Game Object which represents the left hand when teleporting.")]
    GameObject m_LeftTeleportController;
    /// <summary>
    /// The Game Object which represents the left hand when teleporting.
    /// </summary>
    public GameObject leftTeleportController
    {
        get => m_LeftTeleportController;
        set => m_LeftTeleportController = value;
    }

    [SerializeField]
    [Tooltip("The Game Object which represents the right hand for normal interaction purposes.")]
    GameObject m_RightBaseController;
    /// <summary>
    /// The Game Object which represents the right hand for normal interaction purposes.
    /// </summary>
    public GameObject rightBaseController
    {
        get => m_RightBaseController;
        set => m_RightBaseController = value;
    }

    [SerializeField]
    [Tooltip("The Game Object which represents the right hand when teleporting.")]
    GameObject m_RightTeleportController;
    /// <summary>
    /// The Game Object which represents the right hand when teleporting.
    /// </summary>
    public GameObject rightTeleportController
    {
        get => m_RightTeleportController;
        set => m_RightTeleportController = value;
    }

    InputDevice m_RightController;
    InputDevice m_LeftController;

    bool m_LeftTeleportDeactivated;
    bool m_RightTeleportDeactivated;

    ControllerState m_RightControllerState;
    ControllerState m_LeftControllerState;

    /// <summary>
    /// A simple state machine which manages the three pieces of content that are used to represent
    /// A controller state within the XR Interaction Toolkit
    /// </summary>
    struct InteractorController
    {
        /// <summary>
        /// The game object that this state controls
        /// </summary>
        public GameObject m_GO;
        /// <summary>
        /// The XR Controller instance that is associated with this state
        /// </summary>
        public XRController m_XRController;
        /// <summary>
        /// The Line renderer that is associated with this state
        /// </summary>
        public XRInteractorLineVisual m_LineRenderer;
        /// <summary>
        /// The interactor instance that is associated with this state
        /// </summary>
        public XRBaseInteractor m_Interactor;

        /// <summary>
        /// When passed a gameObject, this function will scrape the game object for all valid components that we will
        /// interact with by enabling/disabling as the state changes
        /// </summary>
        /// <param name="gameObject">The game object to scrape the various components from</param>
        public void Attach(GameObject gameObject)
        {
            m_GO = gameObject;
            if (m_GO != null)
            {
                m_XRController = m_GO.GetComponent<XRController>();
                m_LineRenderer = m_GO.GetComponent<XRInteractorLineVisual>();
                m_Interactor = m_GO.GetComponent<XRBaseInteractor>();

                Leave();               
            }
        }

        /// <summary>
        /// Enter this state, performs a set of changes to the associated components to enable things
        /// </summary>
        public void Enter()
        {
            if (m_LineRenderer)
            {
                m_LineRenderer.enabled = true;
            }
            if (m_XRController)
            {
                m_XRController.enableInputActions = true;
            }
            if (m_Interactor)
            {
                m_Interactor.enabled = true;
            }
        }

        /// <summary>
        /// Leaves this state, performs a set of changes to the associate components to disable things.
        /// </summary>
        public void Leave()
        {
            if (m_LineRenderer)
            {
                m_LineRenderer.enabled = false;
            }
            if (m_XRController)
            {
                m_XRController.enableInputActions = false;
            }
            if(m_Interactor)
            {
                m_Interactor.enabled = false;
            }
        }
    }

    /// <summary>
    /// The states that we are currently modeling. 
    /// If you want to add more states, add them here!
    /// </summary>
    public enum ControllerStates
    {
        /// <summary>
        /// the Select state is the "normal" interaction state for selecting and interacting with objects
        /// </summary>
        Select = 0,
        /// <summary>
        /// the Teleport state is used to interact with teleport interactors and queue teleportations.
        /// </summary>
        Teleport = 1,        
        /// <summary>
        /// Maximum sentinel
        /// </summary>
        MAX = 2,
    }

    /// <summary>
    /// Current status of a controller. there will be two instances of this (for left/right). and this allows
    /// the system to change between different states on each controller independently.
    /// </summary>
    struct ControllerState
    {
        ControllerStates m_State;
        InteractorController[] m_Interactors;

        /// <summary>
        /// Sets up the controller
        /// </summary>
        public void Initialize()
        {
            m_State = ControllerStates.MAX;
            m_Interactors = new InteractorController[(int)ControllerStates.MAX];
        }

        /// <summary>
        /// Exits from all states that are in the list, basically a reset.
        /// </summary>
        public void ClearAll()
        {
            m_State = ControllerStates.MAX;

            if (m_Interactors == null)
                return;

            for (var i = 0; i < (int)ControllerStates.MAX; ++i)
            {
                m_Interactors[i].Leave();
            }
        }

        /// <summary>
        /// Attaches a game object that represents an interactor for a state, to a state.
        /// </summary>
        /// <param name="state">The state that we're attaching the game object to</param>
        /// <param name="parentGamObject">The game object that represents the interactor for that state.</param>
        public void SetGameObject(ControllerStates state, GameObject parentGamObject)
        {
            if (state == ControllerStates.MAX || m_Interactors == null)
                return;

            m_Interactors[(int)state].Attach(parentGamObject);
        }

        /// <summary>
        /// Attempts to set the current state of a controller.
        /// </summary>
        /// <param name="nextState">The state that we wish to transition to</param>
        public void SetState(ControllerStates nextState)
        {
            if (nextState == m_State || nextState == ControllerStates.MAX)
            {
                return;
            }

            if (m_State != ControllerStates.MAX)
            {
                m_Interactors[(int)m_State].Leave();                    
            }

            m_State = nextState;           
            m_Interactors[(int)m_State].Enter();
        }
    }

    protected void OnEnable()
    {
        m_LeftTeleportDeactivated = false;
        m_RightTeleportDeactivated = false;

        m_RightControllerState.Initialize();
        m_LeftControllerState.Initialize();

        m_RightControllerState.SetGameObject(ControllerStates.Select, m_RightBaseController);
        m_RightControllerState.SetGameObject(ControllerStates.Teleport, m_RightTeleportController);

        m_LeftControllerState.SetGameObject(ControllerStates.Select, m_LeftBaseController);
        m_LeftControllerState.SetGameObject(ControllerStates.Teleport, m_LeftTeleportController);

        m_LeftControllerState.ClearAll();
        m_RightControllerState.ClearAll();

        InputDevices.deviceConnected += RegisterDevices;
        var devices = new List<InputDevice>();
        InputDevices.GetDevices(devices);
        foreach (var device in devices)
            RegisterDevices(device);
    }

    protected void OnDisable()
    {
        InputDevices.deviceConnected -= RegisterDevices;
    }

    void RegisterDevices(InputDevice connectedDevice)
    {
        if (connectedDevice.isValid)
        {
#if UNITY_2019_3_OR_NEWER
            if((connectedDevice.characteristics & InputDeviceCharacteristics.Left) != 0)
#else
            if (connectedDevice.role == InputDeviceRole.LeftHanded)
#endif
            {
                m_LeftController = connectedDevice;
                m_LeftControllerState.ClearAll();
                m_LeftControllerState.SetState(ControllerStates.Select);
            }
#if UNITY_2019_3_OR_NEWER
            else if ((connectedDevice.characteristics & InputDeviceCharacteristics.Right) != 0)
#else
            else if (connectedDevice.role == InputDeviceRole.RightHanded)
#endif
            {
                m_RightController = connectedDevice;          
                m_RightControllerState.ClearAll();
                m_RightControllerState.SetState(ControllerStates.Select);                                
            }
        }
    }

    protected void Update()
    {
        if (m_LeftController.isValid)
        {
            var activated = false;
            foreach (var button in m_ActivationButtons)
            {
                m_LeftController.IsPressed(button, out var value);
                activated |= value;
            }

            foreach (var button in m_DeactivationButtons)
            {
                m_LeftController.IsPressed(button, out var value);
                m_LeftTeleportDeactivated |= value;
            }

            // if we're pressing the activation buttons, we transition to Teleport
            if (activated && !m_LeftTeleportDeactivated)
            {
                m_LeftControllerState.SetState(ControllerStates.Teleport);
            }
            // otherwise we're in normal state. 
            else
            {
                m_LeftControllerState.SetState(ControllerStates.Select);

                if(!activated)
                    m_LeftTeleportDeactivated = false;
            }
        }

        if (m_RightController.isValid)
        {
            var activated = false;
            foreach (var button in m_ActivationButtons)
            {
                m_RightController.IsPressed(button, out var value);
                activated |= value;
            }

            foreach (var button in m_DeactivationButtons)
            {
                m_RightController.IsPressed(button, out var value);
                m_RightTeleportDeactivated |= value;
            }

            if (activated && !m_RightTeleportDeactivated)
            {
                m_RightControllerState.SetState(ControllerStates.Teleport);
            }
            else
            {
                m_RightControllerState.SetState(ControllerStates.Select);

                if (!activated)
                    m_RightTeleportDeactivated = false;
            }
        }
    }
}
