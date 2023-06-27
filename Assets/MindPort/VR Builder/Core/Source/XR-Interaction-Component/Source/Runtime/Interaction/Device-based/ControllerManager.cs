using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Button = UnityEngine.XR.Interaction.Toolkit.InputHelpers.Button;

namespace VRBuilder.XRInteraction
{
    /// <summary>
    /// Handles controller states interactions.
    /// Contains all methods for performing basic math functions.
    /// <list type="ControllerStates">
    /// <item>
    /// <term>Interaction</term>
    /// <description>Used for selecting and interacting with objects.</description>
    /// </item>
    /// <item>
    /// <term>Teleport</term>
    /// <description>Used forteleport interactors and queue teleportations.</description>
    /// </item>
    /// <item>
    /// <term>UI</term>
    /// <description>Used for interacting with Unity.UI elements.</description>
    /// </item>
    /// </list>
    /// </summary>
    [DefaultExecutionOrder(ControllerManagerUpdateOrder)]
    public class ControllerManager : MonoBehaviour
    {
        // Slightly after the default, so that any actions such as release or grab can be processed *before* we switch controllers.
        private const int ControllerManagerUpdateOrder = 10;

        public enum ControllerStates
        {
            /// <summary>
            /// The Interaction state is used to interact with interactables.
            /// </summary>
            Interaction = 0,

            /// <summary>
            /// The Teleport state is used to interact with teleport interactors and queue teleportations.
            /// </summary>
            Teleport = 1,

            /// <summary>
            /// The UI state is used to interact with Unity.UI elements.
            /// </summary>
            UI = 2,

            /// <summary>
            /// Maximum sentinel.
            /// </summary>
            Max = 3
        }

        /// <summary>
        /// A simple state machine which manages the three pieces of content that are used to represent a
        /// controller state within the XR Interaction Toolkit.
        /// </summary>
        public struct InteractorController
        {
            /// <summary>
            /// The game object that this state controls
            /// </summary>
            private GameObject target;

            /// <summary>
            /// The XR Controller instance that is associated with this state
            /// </summary>
            private XRController controller;

            /// <summary>
            /// The Line renderer that is associated with this state
            /// </summary>
            private XRInteractorLineVisual lineRenderer;

            /// <summary>
            /// The interactor instance that is associated with this state
            /// </summary>
            private XRBaseInteractor interactor;

            /// <summary>
            /// When passed a gameObject, this function will scrape the game object for all valid components that we will
            /// interact with by enabling/disabling as the state changes
            /// </summary>
            /// <param name="gameObject">The game object to scrape the various components from</param>
            public void Attach(GameObject gameObject)
            {
                target = gameObject;

                if (target != null)
                {
                    controller = target.GetComponent<XRController>();
                    lineRenderer = target.GetComponent<XRInteractorLineVisual>();
                    interactor = target.GetComponent<XRBaseInteractor>();

                    Leave();
                }
            }

            /// <summary>
            /// Enter this state, performs a set of changes to the associated components to enable things
            /// </summary>
            public void Enter()
            {
                if (lineRenderer != null)
                {
                    lineRenderer.enabled = true;
                }

                if (controller != null)
                {
                    controller.enableInputActions = true;
                }

                if (interactor != null)
                {
                    interactor.enabled = true;
                }
            }

            /// <summary>
            /// Leaves this state, performs a set of changes to the associate components to disable things.
            /// </summary>
            public void Leave()
            {
                if (lineRenderer != null)
                {
                    lineRenderer.enabled = false;
                }

                if (controller != null)
                {
                    controller.enableInputActions = false;
                }

                if (interactor != null)
                {
                    interactor.enabled = false;
                }
            }

            /// <summary>
            /// True if the interactor is either touching or grabbing an interactable.
            /// </summary>
            public bool IsInteractorInteracting()
            {
                if (interactor == null)
                {
                    return false;
                }

                return interactor.interactablesHovered.Any() || interactor.hasSelection;
            }
        }

        /// <summary>
        /// Current status of a controller. there will be two instances of this (for left/right). and this allows
        /// the system to change between different states on each controller independently.
        /// </summary>
        public struct ControllerState
        {
            private ControllerStates currentState;
            private InteractorController[] interactors;

            /// <summary>
            /// Sets up the controller
            /// </summary>
            public void Initialize()
            {
                currentState = ControllerStates.Max;
                interactors = new InteractorController[(int) ControllerStates.Max];
            }

            /// <summary>
            /// Exits from all states that are in the list, basically a reset.
            /// </summary>
            public void ClearAll()
            {
                if (interactors == null)
                {
                    return;
                }

                for (int i = 0; i < (int) ControllerStates.Max; ++i)
                {
                    interactors[i].Leave();
                }
            }

            /// <summary>
            /// Attaches a game object that represents an interactor for a state, to a state.
            /// </summary>
            /// <param name="state">The state that we're attaching the game object to</param>
            /// <param name="parentGamObject">The game object that represents the interactor for that state.</param>
            public void SetGameObject(ControllerStates state, GameObject parentGamObject)
            {
                if (state == ControllerStates.Max || interactors == null)
                {
                    return;
                }

                interactors[(int) state].Attach(parentGamObject);
            }

            /// <summary>
            /// Attempts to set the current state of a controller.
            /// </summary>
            /// <param name="nextState">The state that we wish to transition to</param>
            public void SetState(ControllerStates nextState)
            {
                if (nextState == currentState || nextState == ControllerStates.Max)
                {
                    return;
                }

                if (currentState != ControllerStates.Max)
                {
                    interactors[(int) currentState].Leave();
                }

                currentState = nextState;
                interactors[(int) currentState].Enter();
            }

            /// <summary>
            /// True if the interactor from given <paramref name="controller"/> is either touching or grabbing an interactable.
            /// </summary>
            /// <returns></returns>
            public bool IsControllerInteracting(ControllerStates controller)
            {
                if (controller == ControllerStates.Max)
                {
                    return false;
                }

                return interactors[(int) controller].IsInteractorInteracting();
            }
        }

        [Header("XR Interaction Controllers")]
        [SerializeField]
        [Tooltip("The Game Object which represents the right hand for normal interaction purposes.")]
        private GameObject rightBaseController = null;

        [SerializeField] [Tooltip("The Game Object which represents the left hand for normal interaction purposes.")]
        private GameObject leftBaseController = null;

        [Header("Teleportation Controllers")]
        [SerializeField]
        [Tooltip("The Game Object which represents the right hand when teleporting.")]
        private GameObject rightTeleportController = null;

        [SerializeField] [Tooltip("The Game Object which represents the left hand when teleporting.")]
        private GameObject leftTeleportController = null;

        [SerializeField]
        [Tooltip("The buttons on the controller that will trigger a transition to the Teleport Controller.")]
        private Button teleportButton = default;

        [SerializeField]
        [Tooltip("The buttons on the controller that will force a deactivation of the teleport option.")]
        private Button cancelTeleportButton = default;

        [Header("UI Interaction Controllers")]
        [SerializeField]
        [Tooltip("The Game Object which represents the right hand when teleporting.")]
        private GameObject rightUIController = null;

        [SerializeField] [Tooltip("The Game Object which represents the left hand when teleporting.")]
        private GameObject leftUIController = null;

        [SerializeField]
        [Tooltip("The buttons on the controller that will force a deactivation of the teleport option.")]
        private Button UIButton = default;

        /// <summary>
        /// The buttons on the controller that will trigger a transition to the Teleport Controller.
        /// </summary>
        public Button TeleportButton
        {
            get => teleportButton;
            set => teleportButton = value;
        }

        /// <summary>
        /// The buttons on the controller that will trigger a transition to the Teleport Controller.
        /// </summary>
        public Button CancelTeleportButton
        {
            get => cancelTeleportButton;
            set => cancelTeleportButton = value;
        }

        /// <summary>
        /// The Game Object which represents the left hand for normal interaction purposes.
        /// </summary>
        public GameObject LeftBaseController
        {
            get => leftBaseController;
            set => leftBaseController = value;
        }

        /// <summary>
        /// The Game Object which represents the left hand when teleporting.
        /// </summary>
        public GameObject LeftTeleportController
        {
            get => leftTeleportController;
            set => leftTeleportController = value;
        }

        /// <summary>
        /// The Game Object which represents the right hand for normal interaction purposes.
        /// </summary>
        public GameObject RightBaseController
        {
            get => rightBaseController;
            set => rightBaseController = value;
        }

        /// <summary>
        /// The Game Object which represents the right hand when teleporting.
        /// </summary>
        public GameObject RightTeleportController
        {
            get => rightTeleportController;
            set => rightTeleportController = value;
        }

        private InputDevice rightController;
        private InputDevice leftController;
        private ControllerState rightControllerState;
        private ControllerState leftControllerState;

        private void OnEnable()
        {
            rightControllerState.Initialize();
            leftControllerState.Initialize();

            rightControllerState.SetGameObject(ControllerStates.Interaction, rightBaseController);
            rightControllerState.SetGameObject(ControllerStates.Teleport, rightTeleportController);
            rightControllerState.SetGameObject(ControllerStates.UI, rightUIController);

            leftControllerState.SetGameObject(ControllerStates.Interaction, leftBaseController);
            leftControllerState.SetGameObject(ControllerStates.Teleport, leftTeleportController);
            leftControllerState.SetGameObject(ControllerStates.UI, leftUIController);

            leftControllerState.ClearAll();
            rightControllerState.ClearAll();

            InputDevices.deviceConnected += RegisterDevices;
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevices(devices);

            foreach (InputDevice device in devices)
            {
                RegisterDevices(device);
            }
        }

        private void OnDisable()
        {
            InputDevices.deviceConnected -= RegisterDevices;
        }

        private void Update()
        {
            ProcessController(leftController, ref leftControllerState);
            ProcessController(rightController, ref rightControllerState);
        }

        private void RegisterDevices(InputDevice connectedDevice)
        {
            if (connectedDevice.isValid)
            {
                if ((connectedDevice.characteristics & InputDeviceCharacteristics.Left) != 0)
                {
                    leftController = connectedDevice;
                    leftControllerState.ClearAll();
                    leftControllerState.SetState(ControllerStates.Interaction);
                }
                else if ((connectedDevice.characteristics & InputDeviceCharacteristics.Right) != 0)
                {
                    rightController = connectedDevice;
                    rightControllerState.ClearAll();
                    rightControllerState.SetState(ControllerStates.Interaction);
                }
            }
        }

        private void ProcessController(InputDevice controller, ref ControllerState controllerState)
        {
            if (controller.isValid == false || controllerState.IsControllerInteracting(ControllerStates.Interaction))
            {
                return;
            }

            controller.IsPressed(teleportButton, out bool activateTeleportationMode);
            controller.IsPressed(cancelTeleportButton, out bool cancelTeleportationMode);
            controller.IsPressed(UIButton, out bool activateUIMode);
            ControllerStates nextState = ControllerStates.Interaction;

            if (activateUIMode)
            {
                nextState = ControllerStates.UI;
            }
            else if (activateTeleportationMode && cancelTeleportationMode == false)
            {
                nextState = ControllerStates.Teleport;
            }

            controllerState.SetState(nextState);
        }
    }
}