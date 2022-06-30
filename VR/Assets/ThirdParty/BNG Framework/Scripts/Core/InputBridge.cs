#define VRIF
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_2018_4_OR_NEWER
using UnityEngine.XR;
#endif
#if STEAM_VR_SDK
using Valve.VR;
#endif

namespace BNG {

    #region Enums
    public enum ControllerHand {
        Left,
        Right,
        None
    }

    /// <summary>
    /// Controller Options available to bind buttons to via Inspector. You can use GetControllerBindingValue() to determine if that button has been pressed.
    /// </summary>
    public enum ControllerBinding {
        None,
        AButton,
        AButtonDown,
        BButton,
        BButtonDown,
        XButton,
        XButtonDown,
        YButton,
        YButtonDown,
        LeftTrigger,
        LeftTriggerDown,
        LeftGrip,
        LeftGripDown,
        LeftThumbstick,
        LeftThumbstickDown,
        RightTrigger,
        RightTriggerDown,
        RightGrip,
        RightGripDown,
        RightThumbstick,
        RightThumbstickDown,
        StartButton,
        StartButtonDown,
        BackButton,
        BackButtonDown
    }

    /// <summary>
    /// Controller Options available to bind buttons to via Inspector. Input is relative to the controller holding it.
    /// Ex : Button 1 = Button A if held in Right controller, Button X if held in Left.
    /// </summary>
    public enum GrabbedControllerBinding {
        None,
        Button1, // Button A, X
        Button1Down,
        Button2, // Button B, Y
        Button2Down,
        Trigger,
        TriggerDown,
        Grip,
        GripDown
    }

    public enum InputAxis {
        None,
        LeftThumbStickAxis,
        LeftTouchPadAxis,
        RightThumbStickAxis,
        RightTouchPadAxis
    }

    public enum ControllerType {
        None,
        Unknown,
        OculusTouch,
        Wand,
        Knuckles
    }

    public enum HandControl {
        LeftGrip,
        RightGrip,
        LeftTrigger,
        RightTrigger,
        None
    }

    public enum GrabButton {
        Grip,
        Trigger,
        Inherit
    }

    public enum HoldType {
        HoldDown, // Hold down the grab button
        Toggle,   // Click the grab button down to switch between hold and release
        Inherit   // Inherit from Grabber
    }    

    public enum XRInputSource {
        XRInput,
        OVRInput,
        SteamVR,
        Pico,
        UnityInput
    }

    public enum SDKProvider {
        Unknown,
        OculusSDK,
        OpenVR
    }

#endregion

    /// <summary>
    /// A proxy for handling input from various input providers such as OVRInput, XRInput, and SteamVR. 
    /// </summary>
    public class InputBridge : MonoBehaviour {

        #region Singleton
        /// <summary>
        /// Instance of our Singleton
        /// </summary>
        public static InputBridge Instance {
            get {
                if (_instance == null) {
                    _instance = FindObjectOfType<InputBridge>();
                    if (_instance == null) {
                        _instance = new GameObject("InputBridge").AddComponent<InputBridge>();
                    }
                }
                return _instance;
            }
        }
        private static InputBridge _instance;

#endregion

        #region Input Properties
        [Header("Input Settings")]
        [SerializeField]        
        public XRInputSource InputSource = XRInputSource.XRInput;

        [SerializeField]
        [Tooltip("Specify an InputActionSet for when using the Unity Input system. These actions will be enabled on load.")]
        public UnityEngine.InputSystem.InputActionAsset actionSet;

        [Header("Tracking Origin")]
        [SerializeField]
        [Tooltip("Set the TrackingOriginModeFlags on device connect. Should typically be set to 'Floor'")]
        public TrackingOriginModeFlags TrackingOrigin = TrackingOriginModeFlags.Floor;

        [Header("Thumbstick Deadzone")]

        /// <summary>
        /// Thumbstick X must be greater than this amount to be considered valid
        /// </summary>
        [Tooltip("Thumbstick X must be greater than this amount to be considered valid")]
        public float ThumbstickDeadzoneX = 0.001f;

        /// <summary>
        /// Thumbstick Y must be greater than this amount to be considered valid
        /// </summary>
        [Tooltip("Thumbstick Y must be greater than this amount to be considered valid")]
        public float ThumbstickDeadzoneY = 0.001f;

#endregion

        #region Raw Inputs

        [Header("Grip")]
        /// <summary>
        /// How far Left Grip is Held down. Values : 0 - 1 (Fully Open / Closed)
        /// </summary>
        public float LeftGrip = 0;

        /// <summary>
        /// Left Grip was pressed down this frame, but not last
        /// </summary>
        public bool LeftGripDown = false;

        /// <summary>
        /// How far Right Grip is Held down. Values : 0 - 1 (Fully Open / Closed)
        /// </summary>
        public float RightGrip = 0;

        /// <summary>
        /// Right Grip was pressed down this frame, but not last
        /// </summary>
        public bool RightGripDown = false;

        [Header("Trigger")]
        /// <summary>
        /// How far Left Trigger is Held down. Values : 0 - 1 (Fully Open / Closed)
        /// </summary>
        public float LeftTrigger = 0;
        public bool LeftTriggerNear = false;
        public bool LeftTriggerUp = false;

        /// <summary>
        /// Returns true if Left Trigger was held down this frame but not the last
        /// </summary>
        public bool LeftTriggerDown = false;

        /// <summary>
        /// How far Left Trigger is Held down. Values : 0 - 1 (Fully Open / Closed)
        /// </summary>
        public float RightTrigger = 0;

        /// <summary>
        /// Returns true if Right Trigger is all the way up this frame but not last
        /// </summary>
        public bool RightTriggerUp = false;

        /// <summary>
        /// Returns true if Right Trigger was held down this frame but not the last
        /// </summary>
        public bool RightTriggerDown = false;
        public bool RightTriggerNear = false;

        public bool LeftThumbNear = false;
        public bool RightThumbNear = false;

        [Header("Thumbstick")]
        /// <summary>
        /// Pressed down this frame, but not last
        /// </summary>
        public bool LeftThumbstickDown = false;

        /// <summary>
        /// Released this frame but not last
        /// </summary>
        public bool LeftThumbstickUp = false;

        /// <summary>
        /// Pressed down this frame, but not last
        /// </summary>
        public bool RightThumbstickDown = false;

        /// <summary>
        /// Released this frame but not last
        /// </summary>
        public bool RightThumbstickUp = false;

        /// <summary>
        /// Currently Held Down
        /// </summary>
        public bool LeftThumbstick = false;
        public bool RightThumbstick = false;

        [Header("Buttons")]
        /// <summary>
        /// Is the A button currently being held down
        /// </summary>
        public bool AButton = false;

        /// <summary>
        /// Returns true if the A Button was pressed down this frame but not last
        /// </summary>
        [Tooltip("Returns true if the A Button was pressed down this frame but not last")]
        public bool AButtonDown = false;

        // A Button Up this frame but down the last
        public bool AButtonUp = false;

        /// <summary>
        /// Is the B button currently being held down
        /// </summary>
        public bool BButton = false;

        /// <summary>
        /// Returns true if the B Button was pressed down this frame but not last
        /// </summary>
        [Tooltip("Returns true if the B Button was pressed down this frame but not last")]
        public bool BButtonDown = false;

        // B Button Up this frame but down the last
        public bool BButtonUp = false;

        public bool XButton = false;

        /// <summary>
        /// Returns true if the X Button was pressed down this frame but not last
        /// </summary>
        [Tooltip("Returns true if the X Button was pressed down this frame but not last")]
        public bool XButtonDown = false;

        // X Button Up this frame but down the last
        public bool XButtonUp = false;

        public bool YButton = false;
        /// <summary>
        /// Returns true if the Y Button was pressed down this frame but not last
        /// </summary>
        public bool YButtonDown = false;
        public bool YButtonUp = false;

        public bool StartButton = false;
        public bool StartButtonDown = false;
        public bool BackButton = false;
        public bool BackButtonDown = false;

        [Header("Axis")]
        public Vector2 LeftThumbstickAxis;
        public Vector2 RightThumbstickAxis;

        public Vector2 LeftTouchPadAxis;
        public Vector2 RightTouchPadAxis;


        #endregion

        #region Device Properties
        /// <summary>
        ///  What threshold constitutes a "down" event.
        ///  For example, pushing the trigger down 20% (0.2) of the way considered starting a trigger down event
        /// This is used in XRInput
        /// </summary>
        public float DownThreshold {
            get {
                return _downThreshold;
            }
        }
        private float _downThreshold = 0.2f;

        bool SteamVRSupport = false;

        [Header("HMD / Hardware")]
        public ControllerType ConnectedControllerType;

        [Tooltip("Is there an HMD present and in use.")]
        public bool HMDActive = false;

        public SDKProvider LoadedSDK { get; private set; }

        public bool IsOculusDevice { get; private set; }

        public bool IsOculusQuest { get; private set; }

        public bool IsHTCDevice { get; private set; }

        public bool IsPicoDevice { get; private set; }

        public bool IsValveIndexController { get; private set; }

        /// <summary>
        /// Returns true if the controller has both a Touchpad and a Joystick. Currently only the Valve Index has both.
        /// </summary>
        [Tooltip("Returns true if the controller has both a Touchpad and a Joystick. Currently on the Valve Index has both.")]
        public bool SupportsBothTouchPadAndJoystick;

        /// <summary>
        /// Returns true if the controllers support the 'indexTouch' (or 'near trigger') XR input mapping. Currently only Oculus devices on the Oculus SDK support index touch. OpenVR is not supported.
        /// </summary>
        [Tooltip("Returns true if the controllers support the 'indexTouch' XR input mapping. Currently only Oculus devices on the Oculus SDK support thumb touch. OpenVR is not supported.")]
        public bool SupportsIndexTouch;

        /// <summary>
        /// Returns true if the controllers support the 'ThumbTouch' (or near thumbstick) XR input mapping. Currently only Oculus devices on the Oculus SDK support index touch. OpenVR is not supported.
        /// </summary>
        [Tooltip("Returns true if the controllers support the 'ThumbTouch' (or near thumbstick) XR input mapping. Currently only Oculus devices on the Oculus SDK support thumb touch. OpenVR is not supported.")]
        public bool SupportsThumbTouch;

#if UNITY_2019_3_OR_NEWER
        static List<InputDevice> devices = new List<InputDevice>();
#endif

        #endregion

        #region Events
        // Events
        /// <summary>
        /// Called after update loop.
        /// </summary>
        public delegate void InputsUpdatedAction();
        public static event InputsUpdatedAction OnInputsUpdated;

        /// <summary>
        /// Called once a controller has been successfully detected
        /// </summary>
        public delegate void ControllerFoundAction();
        public static event ControllerFoundAction OnControllerFound;

        #endregion

        #region Unity Input Actions
        UnityEngine.InputSystem.InputAction leftGrip;
        UnityEngine.InputSystem.InputAction leftTrigger;
        UnityEngine.InputSystem.InputAction leftTriggerNear;
        UnityEngine.InputSystem.InputAction rightGrip;
        UnityEngine.InputSystem.InputAction rightTrigger;
        UnityEngine.InputSystem.InputAction rightTriggerNear;
        UnityEngine.InputSystem.InputAction leftThumbstick;
        UnityEngine.InputSystem.InputAction leftThumbstickDown;
        UnityEngine.InputSystem.InputAction leftThumbNear;        
        UnityEngine.InputSystem.InputAction leftTouchpad;
        UnityEngine.InputSystem.InputAction leftTouchpadDown;
        UnityEngine.InputSystem.InputAction rightThumbstick;
        UnityEngine.InputSystem.InputAction rightThumbstickDown;
        UnityEngine.InputSystem.InputAction rightThumbNear;
        UnityEngine.InputSystem.InputAction rightTouchpad;
        UnityEngine.InputSystem.InputAction rightTouchpadDown;
        UnityEngine.InputSystem.InputAction aButton;
        UnityEngine.InputSystem.InputAction bButton;
        UnityEngine.InputSystem.InputAction xButton;
        UnityEngine.InputSystem.InputAction yButton;

        UnityEngine.InputSystem.InputAction startButton;
        UnityEngine.InputSystem.InputAction backButton;


        #endregion

        #region Input Debugging
        // Used for showing a custom inspector
        [HideInInspector]
        public bool ShowInputDebugger = false;
        #endregion

        private void Awake() {
            // Destroy any duplicate instances that may have been created
            if (_instance != null && _instance != this) {
                Destroy(this);
                return;
            }

            _instance = this;

            // Update all device properties
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevices(devices);

            setDeviceProperties();
        }

        void Start() {

            SetTrackingOriginMode(TrackingOrigin);

#if STEAM_VR_SDK
            SteamVRSupport = true;

            // Warn that input source has not been set, even though the SteamVR SDK is present.
            if(InputSource != XRInputSource.SteamVR) {
                Debug.Log("SteamVR SDK detected, but not set as source on InputBridge. Recommend switching input Source from " + InputSource.ToString() + " to SteamVR.");
            }

            // Set the default action set if not provided
            SteamVR_ActivateActionSetOnLoad VRIFLoader = FindObjectOfType<SteamVR_ActivateActionSetOnLoad>();
            if (VRIFLoader == null) {
                Debug.Log("SteamVR_ActivateActionSetOnLoad component not found - adding VRIF custom actions default.");
                VRIFLoader = gameObject.AddComponent<SteamVR_ActivateActionSetOnLoad>();
                VRIFLoader.actionSet = SteamVR_Actions.VRIF;
            }

            SteamVR.Initialize();
#endif
        }

        void OnEnable() {
#if UNITY_2019_3_OR_NEWER
            InputDevices.deviceConfigChanged += onDeviceChanged;
            InputDevices.deviceConnected += onDeviceChanged;
            InputDevices.deviceDisconnected += onDeviceChanged;
#endif
            CreateUnityInputActions();
            EnableActions();
        }        

        void OnDisable() {
#if UNITY_2019_3_OR_NEWER
            InputDevices.deviceConfigChanged -= onDeviceChanged;
            InputDevices.deviceConnected -= onDeviceChanged;
            InputDevices.deviceDisconnected -= onDeviceChanged;
#endif
            DisableActions();
        }        

        void Update() {
            UpdateDeviceActive();
            UpdateInputs();
        }

        public virtual void UpdateInputs() {
            // SteamVR uses an action system. Only update if HMD is reported as Active
            if (InputSource == XRInputSource.SteamVR && SteamVRSupport && HMDActive) {
                UpdateSteamInput();
            }
            // Use OVRInput to get more Oculus Specific inputs, such as "Near Touch"
            else if (InputSource == XRInputSource.OVRInput) {
                UpdateOVRInput();
            }
            // Use XRInput
            else if(InputSource == XRInputSource.XRInput) {
                UpdateXRInput();
            }
            // New Unity Input System
            else if(InputSource == XRInputSource.UnityInput) {
                UpdateUnityInput();
            }
            // Pico
            else if(InputSource == XRInputSource.Pico) {
                UpdatePicoInput();
            }

            // Call events
            OnInputsUpdated?.Invoke();
        }

        #region SteamVR Action Input
        public virtual void UpdateSteamInput() {
#if STEAM_VR_SDK

            LeftThumbstickAxis = ApplyDeadZones(SteamVR_Actions.vRIF_LeftThumbstickAxis.axis, ThumbstickDeadzoneX, ThumbstickDeadzoneY);
            RightThumbstickAxis = ApplyDeadZones(SteamVR_Actions.vRIF_RightThumbstickAxis.axis, ThumbstickDeadzoneX, ThumbstickDeadzoneY);

            var prevBool = LeftThumbstick;
            LeftThumbstick = SteamVR_Actions.vRIF_LeftThumbstickDown.state;
            // LeftThumbstickDown = SteamVR_Actions.vRIF_LeftThumbstickDown.stateDown;
            LeftThumbstickDown = prevBool == false && LeftThumbstick == true;
            LeftThumbstickUp = prevBool == true && LeftThumbstick == false;

            prevBool = RightThumbstick;
            RightThumbstick = SteamVR_Actions.vRIF_RightThumbstickDown.state;
            // RightThumbstickDown = SteamVR_Actions.vRIF_RightThumbstickDown.stateDown;
            RightThumbstickDown = prevBool == false && RightThumbstick == true;
            RightThumbstickUp = prevBool == true && RightThumbstick == false;
            
            LeftThumbNear = SteamVR_Actions.vRIF_LeftThumbstickNear.state;
            //LeftThumbNear = SteamVR_Actions.vRIF_LeftTrackpadNear.state;
            RightThumbNear = SteamVR_Actions.vRIF_RightThumbstickNear.state;
            //RightThumbNear = SteamVR_Actions.vRIF_RightTrackpadNear.state;

            var prevVal = LeftGrip;
            LeftGrip = LeftGrip = correctValue(SteamVR_Actions.vRIF_LeftGrip.axis);
            LeftGripDown = prevVal < _downThreshold && LeftGrip >= _downThreshold;

            prevVal = RightGrip;
            RightGrip = correctValue(SteamVR_Actions.vRIF_RightGrip.axis);
            RightGripDown = prevVal < _downThreshold && RightGrip >= _downThreshold;

            prevVal = LeftTrigger;
            LeftTrigger = correctValue(SteamVR_Actions.vRIF_LeftTrigger.axis);
            LeftTriggerDown = prevVal < _downThreshold && LeftTrigger >= _downThreshold;
            LeftTriggerUp = prevVal > _downThreshold && LeftTrigger < _downThreshold;
            LeftTriggerNear = SteamVR_Actions.vRIF_LeftTriggerNear.state;

            prevVal = RightTrigger;
            RightTrigger = correctValue(SteamVR_Actions.vRIF_RightTrigger.axis);
            RightTriggerDown = prevVal < _downThreshold && RightTrigger >= _downThreshold;
            RightTriggerUp = prevVal > _downThreshold && RightTrigger < _downThreshold;
            RightTriggerNear = SteamVR_Actions.vRIF_RightTriggerNear.state;

            AButton = SteamVR_Actions.vRIF_AButton.state;
            AButtonDown = SteamVR_Actions.vRIF_AButton.stateDown;
            AButtonUp = SteamVR_Actions.vRIF_AButton.stateUp;
            BButton = SteamVR_Actions.vRIF_BButton.state;
            BButtonDown = SteamVR_Actions.vRIF_BButton.stateDown;
            BButtonUp = SteamVR_Actions.vRIF_AButton.stateUp;
            XButton = SteamVR_Actions.vRIF_XButton.state;
            XButtonDown = SteamVR_Actions.vRIF_XButton.stateDown;
            XButtonUp = SteamVR_Actions.vRIF_XButton.stateUp;
            YButton = SteamVR_Actions.vRIF_YButton.state;
            YButtonDown = SteamVR_Actions.vRIF_YButton.stateDown;
            YButtonUp = SteamVR_Actions.vRIF_YButton.stateUp;

            //prevBool = StartButton;
            //StartButton = SteamVR_Actions.vRIF_StartButton.state;
            //StartButtonDown = prevBool == false && StartButton == true;

            //prevBool = BackButton;
            //BackButton = SteamVR_Actions.vRIF_BackButton.state;
            //BackButtonDown = prevBool == false && BackButton == true;
#endif
        }

        #endregion

        #region XR Input
#if UNITY_2019_3_OR_NEWER
        #region XRInputVariables
        InputDevice primaryLeftController;
        InputDevice primaryRightController;
        InputFeatureUsage<Vector2> thumbstickAxis;
        InputFeatureUsage<Vector2> thumbstickAxisSecondary;
        InputFeatureUsage<bool> thumbstickAxisClick;
#endregion
#endif

        public virtual void UpdateXRInput() {
#if UNITY_2019_3_OR_NEWER
            // Refresh XR devices
            InputDevices.GetDevices(devices);

            // Left XR Controller
            primaryLeftController = GetLeftController();

            // Right XR Controller
            primaryRightController = GetRightController();

            // For most cases thumbstick is on the primary2DAxis
            // However, if the Controller has both a touchpad and a controller on it (i.e. Valve Index Knuckles) then the thumbstick axis is actually on the secondary axis, not the primary axis
            thumbstickAxis = SupportsBothTouchPadAndJoystick ? CommonUsages.secondary2DAxis : CommonUsages.primary2DAxis;
            thumbstickAxisSecondary = SupportsBothTouchPadAndJoystick ? CommonUsages.primary2DAxis : CommonUsages.secondary2DAxis;
            thumbstickAxisClick = SupportsBothTouchPadAndJoystick ? CommonUsages.secondary2DAxisClick : CommonUsages.primary2DAxisClick;

            var prevBool = LeftThumbstick;
            LeftThumbstick = getFeatureUsage(primaryLeftController, thumbstickAxisClick);
            LeftThumbstickDown = prevBool == false && LeftThumbstick == true;
            LeftThumbstickUp = prevBool == true && LeftThumbstick == false;

            prevBool = RightThumbstick;
            RightThumbstick = getFeatureUsage(primaryRightController, thumbstickAxisClick);
            RightThumbstickDown = prevBool == false && RightThumbstick == true;
            RightThumbstickUp = prevBool == true && RightThumbstick == false;

            LeftTouchPadAxis = ApplyDeadZones(getFeatureUsage(primaryLeftController, thumbstickAxisSecondary), ThumbstickDeadzoneX, ThumbstickDeadzoneY);
            LeftThumbstickAxis = ApplyDeadZones(getFeatureUsage(primaryLeftController, thumbstickAxis), ThumbstickDeadzoneX, ThumbstickDeadzoneY);

            RightTouchPadAxis = ApplyDeadZones(getFeatureUsage(primaryRightController, thumbstickAxisSecondary), ThumbstickDeadzoneX, ThumbstickDeadzoneY);
            RightThumbstickAxis = ApplyDeadZones(getFeatureUsage(primaryRightController, thumbstickAxis), ThumbstickDeadzoneX, ThumbstickDeadzoneY);
            
            // Store copy of previous value so we can determine if we need to call OnDownEvent
            var prevVal = LeftGrip;
            LeftGrip = correctValue(getFeatureUsage(primaryLeftController, CommonUsages.grip));
            LeftGripDown = prevVal < _downThreshold && LeftGrip >= _downThreshold;

            prevVal = RightGrip;
            RightGrip = correctValue(getFeatureUsage(primaryRightController, CommonUsages.grip));
            RightGripDown = prevVal < _downThreshold && RightGrip >= _downThreshold;

            prevVal = LeftTrigger;
            LeftTrigger = correctValue(getFeatureUsage(primaryLeftController, CommonUsages.trigger));
            LeftTriggerUp = prevVal > _downThreshold && LeftTrigger < _downThreshold;
            LeftTriggerDown = prevVal < _downThreshold && LeftTrigger >= _downThreshold;            

            prevVal = RightTrigger;
            RightTrigger = correctValue(getFeatureUsage(primaryRightController, CommonUsages.trigger));
            RightTriggerUp = prevVal > _downThreshold && RightTrigger < _downThreshold;
            RightTriggerDown = prevVal < _downThreshold && RightTrigger >= _downThreshold;

            // While OculusUsages.indexTouch is recommended, only CommonUsages.indexTouch is currently providing proper values on certain platforms
            // OculusUsages.indexTouch is returning proper values in Oculus XR plugin >= v1.6.0
            // Oculus Desktop / Android packages require CommonUsages, not OculusUsages ¯\_(ツ)_/¯
#pragma warning disable 0618

            LeftTriggerNear = getFeatureUsage(primaryLeftController, CommonUsages.indexTouch) > 0;

            // Check Oculus Usage if not found with CommonUsages
            if (!LeftTriggerNear) {
#if OCULUS_XR_PLUGIN
                LeftTriggerNear = getFeatureUsage(primaryLeftController, Unity.XR.Oculus.OculusUsages.indexTouch);
#endif
            }

            // Fallback to checking UnityInput if available
            if (!LeftTriggerNear && leftTriggerNear != null && correctValue(leftTriggerNear.ReadValue<float>()) > 0) {
                LeftTriggerNear = true;
            };

            LeftThumbNear = getFeatureUsage(primaryLeftController, CommonUsages.thumbTouch) > 0 || 
                getFeatureUsage(primaryLeftController, CommonUsages.primaryTouch) || 
                getFeatureUsage(primaryLeftController, CommonUsages.secondaryTouch) ||
                getFeatureUsage(primaryLeftController, CommonUsages.primary2DAxisTouch);

            if (!LeftThumbNear) {
#if OCULUS_XR_PLUGIN
                LeftThumbNear = getFeatureUsage(primaryLeftController, Unity.XR.Oculus.OculusUsages.thumbTouch);
#endif
            }

            RightTriggerNear = getFeatureUsage(primaryRightController, CommonUsages.indexTouch) > 0;

            // Try Oculus Usages if not found with Common
            if (!RightTriggerNear) {
#if OCULUS_XR_PLUGIN
                RightTriggerNear = getFeatureUsage(primaryRightController, Unity.XR.Oculus.OculusUsages.indexTouch);
#endif
            }

            // Fallback to checking UnityInput for trigger near if available
            if (!RightTriggerNear && rightTriggerNear != null && correctValue(rightTriggerNear.ReadValue<float>()) > 0) {
                RightTriggerNear = true;
            };

            RightThumbNear = getFeatureUsage(primaryRightController, CommonUsages.thumbTouch) > 0 ||
                getFeatureUsage(primaryRightController, CommonUsages.primaryTouch) ||
                getFeatureUsage(primaryRightController, CommonUsages.secondaryTouch) ||
                getFeatureUsage(primaryRightController, CommonUsages.primary2DAxisTouch);

            if (!RightThumbNear) {
#if OCULUS_XR_PLUGIN
                RightThumbNear = getFeatureUsage(primaryRightController, Unity.XR.Oculus.OculusUsages.thumbTouch);
#endif
            }

#pragma warning restore 0618
            prevBool = AButton;
            AButton = getFeatureUsage(primaryRightController, CommonUsages.primaryButton);
            AButtonDown = prevBool == false && AButton == true;
            AButtonUp = prevBool == true && AButton == false;

            prevBool = BButton;
            BButton = getFeatureUsage(primaryRightController, CommonUsages.secondaryButton);
            BButtonDown = prevBool == false && BButton == true;
            BButtonUp = prevBool == true && BButton == false;

            prevBool = XButton;
            XButton = getFeatureUsage(primaryLeftController, CommonUsages.primaryButton);
            XButtonDown = prevBool == false && XButton == true;
            XButtonUp = prevBool == true && XButton == false;

            prevBool = YButton;
            YButton = getFeatureUsage(primaryLeftController, CommonUsages.secondaryButton);
            YButtonDown = prevBool == false && YButton == true;
            YButtonUp = prevBool == true && YButton == false;

            prevBool = StartButton;
            StartButton = getFeatureUsage(primaryRightController, CommonUsages.menuButton);
            StartButtonDown = prevBool == false && StartButton == true;

            prevBool = BackButton;
            BackButton = getFeatureUsage(primaryLeftController, CommonUsages.menuButton);
            BackButtonDown = prevBool == false && BackButton == true;
#endif
        }
        #endregion

        #region Unity Input
        public virtual void UpdateUnityInput() {
           
            var prevVal = LeftGrip;
            LeftGrip = correctValue(leftGrip.ReadValue<float>());
            LeftGripDown = prevVal < _downThreshold && LeftGrip >= _downThreshold;

            prevVal = LeftTrigger;
            LeftTrigger = correctValue(leftTrigger.ReadValue<float>());
            LeftTriggerDown = prevVal < _downThreshold && LeftTrigger >= _downThreshold;
            LeftTriggerNear = correctValue(leftTriggerNear.ReadValue<float>()) > 0;
            LeftTriggerUp = prevVal > _downThreshold && LeftTrigger < _downThreshold;

            prevVal = RightGrip;
            RightGrip = correctValue(rightGrip.ReadValue<float>());
            RightGripDown = prevVal < _downThreshold && RightGrip >= _downThreshold;

            prevVal = RightTrigger;
            RightTrigger = correctValue(rightTrigger.ReadValue<float>());
            RightTriggerDown = prevVal < _downThreshold && RightTrigger >= _downThreshold;
            RightTriggerNear = correctValue(rightTriggerNear.ReadValue<float>()) > 0;            
            RightTriggerUp = prevVal > _downThreshold && RightTrigger < _downThreshold;

            LeftThumbstickAxis = leftThumbstick.ReadValue<Vector2>();
            var prevBool = LeftThumbstick;
            LeftThumbstick = correctValue(leftThumbstickDown.ReadValue<float>()) > 0;
            LeftThumbstickDown = prevBool == false && LeftThumbstick == true;
            LeftThumbstickUp = prevBool == true && LeftThumbstick == false;
            LeftThumbNear = correctValue(leftThumbNear.ReadValue<float>()) > 0;
            LeftTouchPadAxis = leftTouchpad.ReadValue<Vector2>();

            RightThumbstickAxis = rightThumbstick.ReadValue<Vector2>();
            prevBool = RightThumbstick;
            RightThumbstick = correctValue(rightThumbstickDown.ReadValue<float>()) > 0;
            RightThumbstickDown = prevBool == false && RightThumbstick == true;
            RightThumbstickUp = prevBool == true && RightThumbstick == false;
            RightThumbNear = correctValue(rightThumbNear.ReadValue<float>()) > 0;
            RightTouchPadAxis = rightTouchpad.ReadValue<Vector2>();

            prevBool = AButton;
            AButton = correctValue(aButton.ReadValue<float>()) > 0;
            AButtonDown = prevBool == false && AButton == true;
            AButtonUp = prevBool == true && AButton == false;

            prevBool = BButton;
            BButton = correctValue(bButton.ReadValue<float>()) > 0;
            BButtonDown = prevBool == false && BButton == true;
            BButtonUp = prevBool == true && BButton == false;

            prevBool = XButton;
            XButton = correctValue(xButton.ReadValue<float>()) > 0;
            XButtonDown = prevBool == false && XButton == true;
            XButtonUp = prevBool == true && XButton == false;

            prevBool = YButton;
            YButton = correctValue(yButton.ReadValue<float>()) > 0;
            YButtonDown = prevBool == false && YButton == true;
            YButtonUp = prevBool == true && YButton == false;

            prevBool = StartButton;
            StartButton = correctValue(startButton.ReadValue<float>()) > 0;
            StartButtonDown = prevBool == false && StartButton == true;

            prevBool = BackButton;
            BackButton = correctValue(backButton.ReadValue<float>()) > 0;
            BackButtonDown = prevBool == false && BackButton == true;
        }

        public virtual void CreateUnityInputActions() {
            leftGrip = CreateInputAction("leftGrip", "<XRController>{LeftHand}/{grip}", true);
            leftTrigger = CreateInputAction("leftTrigger", "<XRController>{LeftHand}/{trigger}", true);
            leftTriggerNear = CreateInputAction("leftTriggerNear", "<XRController>{LeftHand}/triggerTouched", true);

            rightGrip = CreateInputAction("rightGrip", "<XRController>{RightHand}/{grip}", true);
            rightTrigger = CreateInputAction("rightTrigger", "<XRController>{RightHand}/{trigger}", true);
            rightTriggerNear = CreateInputAction("rightTriggerNear", "<XRController>{RightHand}/triggerTouched", true);

            leftThumbstick = CreateInputAction("leftThumbstick", "<XRController>{LeftHand}/{primary2DAxis}", true);
            leftThumbstickDown = CreateInputAction("leftThumbstickDown", "<XRController>{LeftHand}/{primary2DAxisClick}", false);
            leftThumbNear = CreateInputAction("leftThumbNear", "<XRController>{LeftHand}/thumbstickTouched", true);
            leftTouchpad = CreateInputAction("leftTouchpad", "<XRController>{LeftHand}/{secondary2DAxis}", true);
            leftTouchpadDown = CreateInputAction("leftTouchpadDown", "<XRController>{LeftHand}/{secondary2DAxisClick}", false);

            rightThumbstick = CreateInputAction("rightThumbstick", "<XRController>{RightHand}/{primary2DAxis}", true);
            rightThumbstickDown = CreateInputAction("rightThumbstickDown", "<XRController>{RightHand}/{primary2DAxisClick}", false);
            rightThumbNear = CreateInputAction("rightThumbNear", "<XRController>{RightHand}/thumbstickTouched", true);
            rightTouchpad = CreateInputAction("rightTouchpad", "<XRController>{RightHand}/{secondary2DAxis}", true);
            rightTouchpadDown = CreateInputAction("rightTouchpadDown", "<XRController>{RightHand}/{secondary2DAxisClick}", false);

            aButton = CreateInputAction("aButton", "<XRController>{RightHand}/{primaryButton}", false);
            bButton = CreateInputAction("bButton", "<XRController>{RightHand}/{secondaryButton}", false);
            xButton = CreateInputAction("xButton", "<XRController>{LeftHand}/{primaryButton}", false);
            yButton = CreateInputAction("yButton", "<XRController>{LeftHand}/{secondaryButton}", false);

            startButton = CreateInputAction("startButton", "<XRController>{RightHand}/{menu}", false);
            backButton = CreateInputAction("backButton", "<XRController>{LeftHand}/{menu}", false);
        }

        public virtual void EnableActions() {
            if (actionSet != null) {
                foreach (var map in actionSet.actionMaps) {
                    foreach (var action in map) {
                        action.Enable();
                    }
                }
            }
        }

        public virtual void DisableActions() {
            if (actionSet != null) {
                foreach (var map in actionSet.actionMaps) {
                    foreach (var action in map) {
                        action.Disable();
                    }
                }
            }
        }

        public UnityEngine.InputSystem.InputAction CreateInputAction(string actionName, string binding, bool valueType) {
            var act = new UnityEngine.InputSystem.InputAction(actionName,
                valueType ? UnityEngine.InputSystem.InputActionType.Value : UnityEngine.InputSystem.InputActionType.Button,
                binding);

            // Automatically enable this binding
            act.Enable();

            return act;
        }

        #endregion

        #region OVR Input
        public virtual void UpdateOVRInput() {
#if OCULUS_INTEGRATION
                LeftThumbstickAxis = ApplyDeadZones(OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick), ThumbstickDeadzoneX, ThumbstickDeadzoneY);
                RightThumbstickAxis = ApplyDeadZones(OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick), ThumbstickDeadzoneX, ThumbstickDeadzoneY);

                LeftGrip = correctValue(OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.LTouch));
                LeftGripDown = OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch);

                RightGrip = correctValue(OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch));
                RightGripDown = OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch);

                LeftTrigger = correctValue(OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch));
                LeftTriggerUp = OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
                LeftTriggerDown = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch);

                RightTrigger = correctValue(OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch));
                RightTriggerUp = OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
                RightTriggerDown = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);

                LeftTriggerNear = OVRInput.Get(OVRInput.NearTouch.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
                LeftThumbNear = OVRInput.Get(OVRInput.NearTouch.PrimaryThumbButtons, OVRInput.Controller.LTouch);

                RightTriggerNear = OVRInput.Get(OVRInput.NearTouch.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
                RightThumbNear = OVRInput.Get(OVRInput.NearTouch.PrimaryThumbButtons, OVRInput.Controller.RTouch);

                AButton = OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.RTouch);
                AButtonDown = OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch);
                AButtonUp = OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTouch);

                BButton = OVRInput.Get(OVRInput.Button.Two);
                BButtonDown = OVRInput.GetDown(OVRInput.Button.Two);
                BButtonUp = OVRInput.GetUp(OVRInput.Button.Two);

                XButton = OVRInput.Get(OVRInput.Button.Three);
                XButtonDown = OVRInput.GetDown(OVRInput.Button.Three);
                XButtonUp = OVRInput.GetUp(OVRInput.Button.Three);

                YButton = OVRInput.Get(OVRInput.Button.Four);
                YButtonDown = OVRInput.GetDown(OVRInput.Button.Four);
                YButtonUp = OVRInput.GetUp(OVRInput.Button.Four);

                StartButton = OVRInput.Get(OVRInput.Button.Start);
                StartButtonDown = OVRInput.GetDown(OVRInput.Button.Start);

                BackButton = OVRInput.Get(OVRInput.Button.Back);
                BackButtonDown = OVRInput.GetDown(OVRInput.Button.Back);

                LeftThumbstickDown = OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);
                LeftThumbstickUp = OVRInput.GetUp(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);

                RightThumbstickDown = OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch);
                RightThumbstickUp = OVRInput.GetUp(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch);

                LeftThumbstick = OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);
                RightThumbstick = OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch);
#endif

            // Call events
            OnInputsUpdated?.Invoke();
        }

        #endregion

        #region Pico Input

        public virtual void UpdatePicoInput() {
#if PICO_SDK
            int rightHand = 1;
            int leftHand = 0;
            var prevBool = LeftThumbstick;
            var prevVal = LeftGrip;

            LeftThumbstickAxis = ApplyDeadZones(Pvr_UnitySDKAPI.Controller.UPvr_GetAxis2D(leftHand), ThumbstickDeadzoneX, ThumbstickDeadzoneY);
            RightThumbstickAxis = ApplyDeadZones(Pvr_UnitySDKAPI.Controller.UPvr_GetAxis2D(rightHand), ThumbstickDeadzoneX, ThumbstickDeadzoneY);

            prevBool = LeftThumbstick;
            LeftThumbstick = Pvr_UnitySDKAPI.Controller.UPvr_GetKey(leftHand, Pvr_UnitySDKAPI.Pvr_KeyCode.TOUCHPAD);
            LeftThumbstickDown = Pvr_UnitySDKAPI.Controller.UPvr_GetKeyDown(leftHand, Pvr_UnitySDKAPI.Pvr_KeyCode.TOUCHPAD);
            LeftThumbstickUp = prevBool == true && LeftThumbstick == false;

            prevBool = RightThumbstick;
            RightThumbstick = Pvr_UnitySDKAPI.Controller.UPvr_GetKey(rightHand, Pvr_UnitySDKAPI.Pvr_KeyCode.TOUCHPAD);
            RightThumbstickDown = Pvr_UnitySDKAPI.Controller.UPvr_GetKeyDown(rightHand, Pvr_UnitySDKAPI.Pvr_KeyCode.TOUCHPAD);
            RightThumbstickUp = prevBool == true && RightThumbstick == false;

            LeftThumbNear = Pvr_UnitySDKAPI.Controller.UPvr_IsTouching(leftHand);
            RightThumbNear = Pvr_UnitySDKAPI.Controller.UPvr_IsTouching(rightHand);

            prevVal = LeftGrip;
            LeftGrip = Pvr_UnitySDKAPI.Controller.UPvr_GetKey(leftHand, Pvr_UnitySDKAPI.Pvr_KeyCode.Left) ? 1f : 0;
            LeftGripDown = Pvr_UnitySDKAPI.Controller.UPvr_GetKeyDown(leftHand, Pvr_UnitySDKAPI.Pvr_KeyCode.Left);

            prevVal = RightGrip;
            RightGrip = Pvr_UnitySDKAPI.Controller.UPvr_GetKey(rightHand, Pvr_UnitySDKAPI.Pvr_KeyCode.Right) ? 1f : 0;
            RightGripDown = Pvr_UnitySDKAPI.Controller.UPvr_GetKeyDown(rightHand, Pvr_UnitySDKAPI.Pvr_KeyCode.Right);

            prevVal = LeftTrigger;
            LeftTrigger = Pvr_UnitySDKAPI.Controller.UPvr_GetControllerTriggerValue(leftHand) / 255f;                        
            LeftTriggerUp = prevVal > _downThreshold && LeftTrigger < _downThreshold;
            LeftTriggerDown = prevVal < _downThreshold && LeftTrigger >= _downThreshold;

            prevVal = RightTrigger;
            RightTrigger = Pvr_UnitySDKAPI.Controller.UPvr_GetControllerTriggerValue(rightHand) / 255f;
            RightTriggerUp = prevVal > _downThreshold && RightTrigger < _downThreshold;
            RightTriggerDown = prevVal < _downThreshold && RightTrigger >= _downThreshold;

            prevBool = AButton;
            AButton = Pvr_UnitySDKAPI.Controller.UPvr_GetKey(rightHand, Pvr_UnitySDKAPI.Pvr_KeyCode.A);
            AButtonDown = Pvr_UnitySDKAPI.Controller.UPvr_GetKeyDown(rightHand, Pvr_UnitySDKAPI.Pvr_KeyCode.A);
            AButtonUp = prevBool == true && AButton == false;

            prevBool = BButton;
            BButton = Pvr_UnitySDKAPI.Controller.UPvr_GetKey(rightHand, Pvr_UnitySDKAPI.Pvr_KeyCode.B);
            BButtonDown = Pvr_UnitySDKAPI.Controller.UPvr_GetKeyDown(rightHand, Pvr_UnitySDKAPI.Pvr_KeyCode.B);
            BButtonUp = prevBool == true && BButton == false;

            prevBool = XButton;
            XButton = Pvr_UnitySDKAPI.Controller.UPvr_GetKey(leftHand, Pvr_UnitySDKAPI.Pvr_KeyCode.X);
            XButtonDown = Pvr_UnitySDKAPI.Controller.UPvr_GetKeyDown(leftHand, Pvr_UnitySDKAPI.Pvr_KeyCode.X);
            XButtonUp = prevBool == true && XButton == false;

            prevBool = YButton;
            YButton = Pvr_UnitySDKAPI.Controller.UPvr_GetKey(leftHand, Pvr_UnitySDKAPI.Pvr_KeyCode.Y);
            YButtonDown = prevBool == false && YButton == true;
            // Alternatively :
            // YButtonDown = Pvr_UnitySDKAPI.Controller.UPvr_GetKeyDown(leftHand, Pvr_UnitySDKAPI.Pvr_KeyCode.Y);
            YButtonUp = prevBool == true && YButton == false;
#endif
        }

        #endregion

        public virtual void UpdateDeviceActive() {

            InputDevice hmd = GetHMD();

            // Can bail early
            if (hmd.isValid == false) {
                HMDActive = false;
                return;
            }

            // Make sure the device supports the presence feature
            bool userPresent = false;
            bool presenceFeatureSupported = hmd.TryGetFeatureValue(CommonUsages.userPresence, out userPresent);
            if(presenceFeatureSupported) {
                HMDActive = userPresent;
            }
            else {
                HMDActive = XRSettings.isDeviceActive;
            }
        }

        /// <summary>
        /// Round to nearest thousandth. This can alleviate some floating point precision errors found when using certain inputs.
        /// </summary>
        /// <param name="inputValue"></param>
        /// <returns></returns>
        float correctValue(float inputValue) {
            return (float)System.Math.Round(inputValue * 1000f) / 1000f;
        }


        /// <summary>
        /// Returns true if the given binding is pressed
        /// </summary>
        public bool GetControllerBindingValue(ControllerBinding val) {
            if (val == ControllerBinding.AButton && AButton) { return true; }
            if (val == ControllerBinding.AButtonDown && AButtonDown) { return true; }
            if (val == ControllerBinding.BButton && BButton) { return true; }
            if (val == ControllerBinding.BButtonDown && BButtonDown) { return true; }
            if (val == ControllerBinding.XButton && XButton) { return true; }
            if (val == ControllerBinding.XButtonDown && XButtonDown) { return true; }
            if (val == ControllerBinding.YButton && YButton) { return true; }
            if (val == ControllerBinding.YButtonDown && YButtonDown) { return true; }
            if (val == ControllerBinding.LeftTrigger && LeftTrigger > _downThreshold) { return true; }
            if (val == ControllerBinding.LeftTriggerDown && LeftTriggerDown) { return true; }
            if (val == ControllerBinding.LeftGrip && LeftGrip > _downThreshold) { return true; }
            if (val == ControllerBinding.LeftGripDown && LeftGripDown) { return true; }
            if (val == ControllerBinding.LeftThumbstick && LeftThumbstick) { return true; }
            if (val == ControllerBinding.LeftThumbstickDown && LeftThumbstickDown) { return true; }
            if (val == ControllerBinding.RightTrigger && RightTrigger > _downThreshold) { return true; }
            if (val == ControllerBinding.RightTriggerDown && RightTriggerDown) { return true; }
            if (val == ControllerBinding.RightGrip && RightGrip > _downThreshold) { return true; }
            if (val == ControllerBinding.RightGripDown && RightGripDown) { return true; }
            if (val == ControllerBinding.RightThumbstick && RightThumbstick) { return true; }
            if (val == ControllerBinding.RightThumbstickDown && RightThumbstickDown) { return true; }
            if (val == ControllerBinding.StartButton && StartButton) { return true; }
            if (val == ControllerBinding.StartButtonDown && StartButtonDown) { return true; }
            if (val == ControllerBinding.BackButton && BackButton) { return true; }
            if (val == ControllerBinding.BackButtonDown && BackButtonDown) { return true; }

            return false;
        }

        public bool GetGrabbedControllerBinding(GrabbedControllerBinding val, ControllerHand hand) {
            if(hand == ControllerHand.Right) {
                if (val == GrabbedControllerBinding.Button1 && AButton) { return true; }
                if (val == GrabbedControllerBinding.Button1Down && AButtonDown) { return true; }
                if (val == GrabbedControllerBinding.Button2 && BButton) { return true; }
                if (val == GrabbedControllerBinding.Button2Down && BButtonDown) { return true; }
                if (val == GrabbedControllerBinding.Grip && RightGrip > _downThreshold) { return true; }
                if (val == GrabbedControllerBinding.GripDown && RightGripDown) { return true; }
                if (val == GrabbedControllerBinding.Trigger && RightTrigger > _downThreshold) { return true; }
                if (val == GrabbedControllerBinding.TriggerDown && RightTriggerDown) { return true; }
            }
            else if (hand == ControllerHand.Left) {
                if (val == GrabbedControllerBinding.Button1 && XButton) { return true; }
                if (val == GrabbedControllerBinding.Button1Down && XButtonDown) { return true; }
                if (val == GrabbedControllerBinding.Button2 && YButton) { return true; }
                if (val == GrabbedControllerBinding.Button2Down && YButtonDown) { return true; }
                if (val == GrabbedControllerBinding.Grip && LeftGrip > _downThreshold) { return true; }
                if (val == GrabbedControllerBinding.GripDown && LeftGripDown) { return true; }
                if (val == GrabbedControllerBinding.Trigger && LeftTrigger > _downThreshold) { return true; }
                if (val == GrabbedControllerBinding.TriggerDown && LeftTriggerDown) { return true; }
            }

            return false;
        }

        public Vector2 GetInputAxisValue(InputAxis val) {
            if (val == InputAxis.LeftThumbStickAxis) { return LeftThumbstickAxis; }
            if (val == InputAxis.RightThumbStickAxis) { return RightThumbstickAxis; }
            if (val == InputAxis.LeftTouchPadAxis) { return LeftTouchPadAxis; }
            if (val == InputAxis.RightTouchPadAxis) { return RightTouchPadAxis; }

            return Vector3.zero;
        }

        Vector2 ApplyDeadZones(Vector2 pos, float deadZoneX, float deadZoneY) {

            if (Mathf.Abs(pos.x) < deadZoneX) {
                pos.x = 0f;
            }

            if (Mathf.Abs(pos.y) < deadZoneY) {
                pos.y = 0f;
            }

            return pos;
        }

        // Called when an input device has changed (connect / disconnect, etc.)
        void onDeviceChanged(InputDevice inputDevice) {

            setDeviceProperties();

            SetTrackingOriginMode(TrackingOrigin);
        }

        void setDeviceProperties() {

            // Update device properties such as device name, controller properties, etc.
            // We only want to update this information if a device has changed in order to skip unnecessary checks every frame
            IsOculusDevice = GetIsOculusDevice();
            IsOculusQuest = GetIsOculusQuest();
            IsHTCDevice = GetIsHTCDevice();
            IsPicoDevice = GetIsPicoDevice();
            IsValveIndexController = GetIsValveIndexController();

            // Set the SDK we are using
            LoadedSDK = GetLoadedSDK();

            // Get specific device support
            SupportsIndexTouch = GetSupportsIndexTouch();
            SupportsThumbTouch = GetSupportsThumbTouch();

            // Currently only the Valve Index has both a touchpad and a joystick on the same controller
            SupportsBothTouchPadAndJoystick = IsValveIndexController;

            // Update Controller Type
            ConnectedControllerType = GetControllerType();

            // Call any events
            if(!string.IsNullOrEmpty(InputBridge.Instance.GetControllerName())) {
                OnControllerFound?.Invoke();
            }
        }

        /// <summary>
        /// Returns true if the controllers support the 'indexTouch' XR input mapping.Currently only Oculus devices on the Oculus SDK support index touch. OpenVR is not supported.
        /// </summary>
        /// <returns></returns>
        public virtual bool GetSupportsIndexTouch() {
            //if(IsOculusDevice && LoadedSDK == SDKProvider.OculusSDK) {
            //    return true;
            //}

            return true;
        }

        public virtual SDKProvider GetLoadedSDK() {

            // Can exit early if no device name has been picked up yet
            if (XRSettings.loadedDeviceName == null) {
                return SDKProvider.Unknown;
            }

            string deviceName = XRSettings.loadedDeviceName.ToLower();

            // Example : "oculus display"
            if (deviceName.StartsWith("oculus")) {
                return SDKProvider.OculusSDK;
            }
            // Example : "OpenVR Display"
            else if (deviceName.StartsWith("openvr")) {
                return SDKProvider.OpenVR;
            }

            return SDKProvider.Unknown;
        }

        public virtual bool GetSupportsThumbTouch() {
            //if (IsOculusDevice && LoadedSDK == SDKProvider.OculusSDK) {
            //    return true;
            //}

            return true;
        }

        public virtual bool GetIsOculusDevice() {

            var primaryHMD = GetHMD();

            // OpenVR Format
            if (primaryHMD != null && primaryHMD.manufacturer == "Oculus") {
                return true;
            }

#if UNITY_2019_2_OR_NEWER
            return XRSettings.loadedDeviceName != null && XRSettings.loadedDeviceName.ToLower().Contains("oculus");
#else
            return true;
#endif
        }

        public virtual bool GetIsOculusQuest() {
#if UNITY_2019_2_OR_NEWER

            var primaryHMD = GetHMD();

            // Example : "OpenVR Headset(Oculus Quest)"
            if (primaryHMD != null && primaryHMD.name != null && primaryHMD.name.EndsWith("(Oculus Quest)")) {
                return true;
            }
            // Non-OpenVR version use "contains" on string. 
            else if (primaryHMD != null && primaryHMD.name != null && primaryHMD.name.Contains("Oculus Quest")) {
                return true;
            }

            //  Fallback to refresh rate
            return GetIsOculusDevice() && XRDevice.refreshRate == 72f;
#else
            if (Application.platform == RuntimePlatform.Android) {
                return true;
            }
            
            return false;
#endif
        }

        public virtual bool GetIsHTCDevice() {
            // Is HTC Device
#if UNITY_2019_2_OR_NEWER
            var primaryHMD = GetHMD();

            // OpenVR Format
            if (primaryHMD != null && primaryHMD.manufacturer == "HTC") {
                return true;
            }

            return XRSettings.loadedDeviceName.StartsWith("HTC");
#else
           return false;
#endif
        }

        public virtual bool GetIsPicoDevice() {
#if UNITY_2019_2_OR_NEWER
            return InputSource == XRInputSource.Pico || XRSettings.loadedDeviceName.StartsWith("Pico");
#else
            return InputSource == XRInputSource.Pico;
#endif
        }

        public InputDevice GetHMD() {
            InputDevices.GetDevices(devices);

            var hmds = new List<InputDevice>();
            var dc1 = InputDeviceCharacteristics.HeadMounted;
            InputDevices.GetDevicesWithCharacteristics(dc1, hmds);

            return hmds.FirstOrDefault();
        }


        /// <summary>
        /// Returns the name of the InputDevice if found. Returns String.empty if not found
        /// </summary>
        /// <returns>  The name of the InputDevice if found, or String.empty if not found</returns>
        public string GetHMDName() {
            var device = GetHMD();
            if(device != null) {
                return device.name;
            }

            return string.Empty;
        }

        public Vector3 GetHMDLocalPosition() {
            Vector3 localPosition;

            GetHMD().TryGetFeatureValue(CommonUsages.devicePosition, out localPosition);

            return localPosition;
        }

        public Quaternion GetHMDLocalRotation() {
            Quaternion localRotation;

            GetHMD().TryGetFeatureValue(CommonUsages.deviceRotation, out localRotation);

            return localRotation;
        }

        public InputDevice GetLeftController() {
            InputDevices.GetDevices(devices);

            var leftHandedControllers = new List<InputDevice>();
            var dc = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller;
            InputDevices.GetDevicesWithCharacteristics(dc, leftHandedControllers);
            return leftHandedControllers.FirstOrDefault();
        }

        public InputDevice GetRightController() {
            InputDevices.GetDevices(devices);

            var rightHandedControllers = new List<InputDevice>();
            var dc = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;
            InputDevices.GetDevicesWithCharacteristics(dc, rightHandedControllers);

            return rightHandedControllers.FirstOrDefault();
        }

        public Vector3 GetControllerLocalPosition(ControllerHand handSide) {
            Vector3 localPosition = Vector3.zero;

            if (handSide == ControllerHand.Left) {
                GetLeftController().TryGetFeatureValue(CommonUsages.devicePosition, out localPosition);
            }
            else if (handSide == ControllerHand.Right) {
                GetRightController().TryGetFeatureValue(CommonUsages.devicePosition, out localPosition);
            }

            return localPosition;
        }

        public Quaternion GetControllerLocalRotation(ControllerHand handSide) {
            Quaternion localRotation = Quaternion.identity;

            if (handSide == ControllerHand.Left) {
                GetLeftController().TryGetFeatureValue(CommonUsages.deviceRotation, out localRotation);
            }
            else if (handSide == ControllerHand.Right) {
                GetRightController().TryGetFeatureValue(CommonUsages.deviceRotation, out localRotation);
            }

            return localRotation;
        }

        public virtual ControllerType GetControllerType() {

            if (IsValveIndexController) {
                return ControllerType.Knuckles;
            }
            else if (IsOculusDevice) {
                return ControllerType.OculusTouch;
            }
            else if (IsHTCDevice) {
                return ControllerType.Wand;
            }

            return ControllerType.Unknown;
        }

        public Vector3 GetControllerVelocity(ControllerHand hand) {
            InputDevice inputDevice = hand == ControllerHand.Left ? GetLeftController() : GetRightController();
            return getFeatureUsage(inputDevice, CommonUsages.deviceVelocity);
        }

        public Vector3 GetControllerAngularVelocity(ControllerHand hand) {
            InputDevice inputDevice = hand == ControllerHand.Left ? GetLeftController() : GetRightController();
            return getFeatureUsage(inputDevice, CommonUsages.deviceAngularVelocity);
        }

        /// <summary>
        /// Get the name of the primary controller
        /// </summary>
        /// <returns>The name of the primary controller. Returns empty if no controller found</returns>
        public virtual string GetControllerName() {

            // First try right controller
            var rightHandedControllers = new List<InputDevice>();
            var dc = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;
            InputDevices.GetDevicesWithCharacteristics(dc, rightHandedControllers);
            var primaryRightController = rightHandedControllers.FirstOrDefault();

            // Return name of the found controller
            if (primaryRightController != null && !System.String.IsNullOrEmpty(primaryRightController.name)) {
                return primaryRightController.name;
            }

            // No right controller found, try the left
            var leftHandedControllers = new List<InputDevice>();
            dc = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller;
            InputDevices.GetDevicesWithCharacteristics(dc, leftHandedControllers);
            var primaryLeftController = leftHandedControllers.FirstOrDefault();

            // Return name of the found controller
            if (primaryLeftController != null && !System.String.IsNullOrEmpty(primaryLeftController.name)) {
                return primaryLeftController.name;
            }

            return string.Empty;
        }

        public virtual bool GetIsValveIndexController() {
            var rightHandedControllers = new List<InputDevice>();
            var dc = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;
            InputDevices.GetDevicesWithCharacteristics(dc, rightHandedControllers);
            var primaryRightController = rightHandedControllers.FirstOrDefault();

            // Are we using Valve Index Controllers?
            if (primaryRightController != null && !System.String.IsNullOrEmpty(primaryRightController.name)) {
                return primaryRightController.name.Contains("Knuckles");
            }

            return false;
        }

#if UNITY_2019_2_OR_NEWER
        float getFeatureUsage(InputDevice device, InputFeatureUsage<float> usage, bool clamp = true) {
            float val;
            device.TryGetFeatureValue(usage, out val);

            return Mathf.Clamp01(val);
        }

        bool getFeatureUsage(InputDevice device, InputFeatureUsage<bool> usage) {
            bool val;
            if (device.TryGetFeatureValue(usage, out val)) {
                return val;
            }

            return val;
        }

        Vector2 getFeatureUsage(InputDevice device, InputFeatureUsage<Vector2> usage) {
            Vector2 val;
            if (device.TryGetFeatureValue(usage, out val)) {
                return val;
            }

            return val;
        }

        Vector3 getFeatureUsage(InputDevice device, InputFeatureUsage<Vector3> usage) {
            Vector3 val;
            if (device.TryGetFeatureValue(usage, out val)) {
                return val;
            }

            return val;
        }
#endif

        bool setTrackingOrigin = false;
        public virtual void SetTrackingOriginMode(TrackingOriginModeFlags trackingOrigin) {
            // 2019.4 Needs to use XRDevice.SetTrackingSpaceType; TrySetTrackingOriginMode does not function properly.
            // *Removed from VRIF  v1.6 as XR plugin should properly set tracking space
#if UNITY_2019_4
            if (trackingOrigin == TrackingOriginModeFlags.Floor) {
#pragma warning disable
                XRDevice.SetTrackingSpaceType(TrackingSpaceType.RoomScale);
#pragma warning restore
            }
#endif
            StartCoroutine(changeOriginModeRoutine(trackingOrigin));
        }

        IEnumerator changeOriginModeRoutine(TrackingOriginModeFlags trackingOrigin) {

            // Wait one frame as Unity has an issue with calling this immediately
            yield return null;

            if(!setTrackingOrigin) {
                List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
                SubsystemManager.GetInstances(subsystems);
                int subSystemsCount = subsystems.Count;

                if (subSystemsCount > 0) {
                    for (int x = 0; x < subSystemsCount; x++) {
                        if (subsystems[x].TrySetTrackingOriginMode(trackingOrigin)) {
                            setTrackingOrigin = true;
                            // Debug.Log("Successfully set TrackingOriginMode to " + trackingOrigin);
                        }
                        else {
                            Debug.LogWarning("Failed to set TrackingOriginMode to " + trackingOrigin);
                        }
                    }
                }
                else {
                    // Debug.LogWarning("No subsystems detected. Unable to set Tracking Origin to " + trackingOrigin);
                }
            }
        }

        // Start Vibration on controller
        public void VibrateController(float frequency, float amplitude, float duration, ControllerHand hand) {
            
            if (InputSource == XRInputSource.OVRInput) {
                StartCoroutine(Vibrate(frequency, amplitude, duration, hand));
            }
            else if (InputSource == XRInputSource.SteamVR && SteamVRSupport) {
#if STEAM_VR_SDK
                if (hand == ControllerHand.Right) {
                    SteamVR_Actions.vRIF_Haptic.Execute(0, duration, frequency, amplitude, SteamVR_Input_Sources.RightHand);
                }
                else {
                    SteamVR_Actions.vRIF_Haptic.Execute(0, duration, frequency, amplitude, SteamVR_Input_Sources.LeftHand);
                }                
#endif
            }
            // Default / Fallback to XRInput
            else {
                if (hand == ControllerHand.Right) {
                    InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right, devices);
                }
                else {
                    InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Left, devices);
                }

                for (int x = 0; x < devices.Count; x++) {
                    HapticCapabilities capabilities;
                    if (devices[x].TryGetHapticCapabilities(out capabilities)) {
                        if (capabilities.supportsImpulse) {
                            uint channel = 0;
                            devices[x].SendHapticImpulse(channel, amplitude, duration);
                        }
                    }
                }
            }
        }

        IEnumerator Vibrate(float frequency, float amplitude, float duration, ControllerHand hand) {
#if OCULUS_INTEGRATION
            // Start vibration
            if (hand == ControllerHand.Right) {
                OVRInput.SetControllerVibration(frequency, amplitude, OVRInput.Controller.RTouch);
            }
            else if (hand == ControllerHand.Left) {
                OVRInput.SetControllerVibration(frequency, amplitude, OVRInput.Controller.LTouch);
            }

            yield return new WaitForSeconds(duration);

            // Stop vibration
            if (hand == ControllerHand.Right) {
                OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
            }
            else if (hand == ControllerHand.Left) {
                OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.LTouch);
            }
#else
            yield return new WaitForSeconds(duration);
#endif
        }
    }
}