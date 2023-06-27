using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.XR;

#if OPENXR_1_6_OR_NEWER
using UnityEngine.XR.OpenXR.Features.Interactions;
#endif

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

#if OPENXR_1_6_OR_NEWER
// Opt-in Scripting Define Symbol to use Input System PoseControl with com.unity.xr.openxr@1.6
#if USE_INPUT_SYSTEM_POSE_CONTROL
using PoseState = UnityEngine.InputSystem.XR.PoseState;
#else
using PoseState = UnityEngine.XR.OpenXR.Input.Pose;
#endif
#endif

namespace UnityEngine.XR.Interaction.Toolkit.Samples.MetaGazeAdapter
{
    /// <summary>
    /// Use this component in your scene to automatically create an Eye Gaze input device and
    /// update its input state based on data from the Oculus <c>OVRPlugin</c> API. It also helps with requesting
    /// eye tracking permission.
    /// </summary>
    /// <remarks>
    /// This script requires the OpenXR Plugin (com.unity.xr.openxr) package.
    /// It additionally requires the <c>VR</c> folder to be imported from the Oculus Integration asset:
    /// https://developer.oculus.com/downloads/package/unity-integration/
    /// <br />
    /// This sample script was developed by Unity and is not associated with Meta.
    /// </remarks>
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_DeviceSimulator)]
    public class OculusEyeGazeInputAdapter : MonoBehaviour
    {
#if UNITY_ANDROID
        const string k_EyeTrackingPermission = "com.oculus.permission.EYE_TRACKING";
#endif

        enum Eye
        {
            Left = 0,
            Right = 1,
        }

        [SerializeField, Tooltip("Which eye to use for gaze.")]
        Eye m_Eye;
        
        [SerializeField, Tooltip("Whether to check against confidence threshold to set the EyeGaze input device pose state.")]
        bool m_CheckConfidence;

        [SerializeField, Range(0f, 1f), Tooltip("Ignore eye gaze if confidence is less than this value.")]
        float m_ConfidenceThreshold;

#if OPENXR_1_6_OR_NEWER
        PoseState m_PoseState;
        InputSystem.InputDevice m_EyeGazeDevice;
        InputControl m_PoseControl;
#if UNITY_ANDROID
        PermissionCallbacks m_PermissionCallbacks;
#endif

        bool m_OculusEyeTrackingStarted;
        OVRPlugin.EyeGazesState m_OculusEyeGazesState;
#endif

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void Awake()
        {
#if OPENXR_1_6_OR_NEWER
#if UNITY_ANDROID
            m_PermissionCallbacks = new PermissionCallbacks();
#endif

            m_PoseState = new PoseState
            {
                isTracked = default,
                trackingState = default,
                position = default,
                rotation = Quaternion.identity,
                velocity = default,
                angularVelocity = default,
            };

            // Register eye gaze layout
            InputSystem.InputSystem.RegisterLayout(typeof(EyeGazeInteraction.EyeGazeDevice),
                "EyeGaze",
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithProduct("Oculus Eye Gaze Adapter"));
#else
            Debug.LogError("Script requires OpenXR Plugin (com.unity.xr.openxr) package. Install using Window > Package Manager or click Fix on the related issue in Edit > Project Settings > XR Plug-in Management > Project Validation.", this);
#endif
        }

#if OPENXR_1_6_OR_NEWER
        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void OnEnable()
        {
#if UNITY_ANDROID
            // Bind to eye tracking permission callbacks
            m_PermissionCallbacks.PermissionGranted += OnPermissionGranted;
            m_PermissionCallbacks.PermissionDenied += OnPermissionDenied;
            m_PermissionCallbacks.PermissionDeniedAndDontAskAgain += OnPermissionDeniedAndDontAskAgain;
#endif

            m_OculusEyeTrackingStarted = StartEyeTracking();
            if (m_OculusEyeTrackingStarted)
                AddEyeGazeDevice();
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void OnDisable()
        {
#if UNITY_ANDROID
            if (m_PermissionCallbacks != null)
            {
                m_PermissionCallbacks.PermissionGranted -= OnPermissionGranted;
                m_PermissionCallbacks.PermissionDenied -= OnPermissionDenied;
                m_PermissionCallbacks.PermissionDeniedAndDontAskAgain -= OnPermissionDeniedAndDontAskAgain;
            }
#endif
            RemoveEyeGazeDevice();
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void Update()
        {
            ProcessPoseInput();

            if (m_EyeGazeDevice != null && m_PoseControl != null)
                InputSystem.InputSystem.QueueDeltaStateEvent(m_PoseControl, m_PoseState);
        }

        void ProcessPoseInput()
        {
            if (!m_OculusEyeTrackingStarted)
                return;

            if (!OVRPlugin.GetEyeGazesState(OVRPlugin.Step.Render, -1, ref m_OculusEyeGazesState))
                return;
            
            var eyeGazeState = m_OculusEyeGazesState.EyeGazes[(int)m_Eye];

            if (m_CheckConfidence && eyeGazeState.Confidence < m_ConfidenceThreshold)
                return;
            
            var isValid = eyeGazeState.IsValid;
            var pose = eyeGazeState.Pose.ToOVRPose();
            
            m_PoseState.isTracked = isValid;
            m_PoseState.trackingState = isValid ? InputTrackingState.Position | InputTrackingState.Rotation : InputTrackingState.None;
            m_PoseState.position = pose.position;
            m_PoseState.rotation = pose.orientation;
        }

        bool StartEyeTracking()
        {
            if (m_OculusEyeTrackingStarted)
                return true;
            
            if (!HasEyeTrackingPermission())
            {
#if UNITY_ANDROID
                RequestEyeTrackingPermission(m_PermissionCallbacks);
                Debug.Log("Requesting eye tracking permissions.", this);
                return false;
#endif
            }

            var eyeTrackingStarted = OVRPlugin.StartEyeTracking();
            if (!eyeTrackingStarted)
            {
                Debug.LogWarning("Failed to start eye tracking service.", this);
                return false;
            }

            Debug.Log("Eye tracking successfully started.", this);
            return true;
        }

        void AddEyeGazeDevice()
        {
            if (m_EyeGazeDevice != null && m_EyeGazeDevice.added)
                return;

            m_EyeGazeDevice = InputSystem.InputSystem.AddDevice<EyeGazeInteraction.EyeGazeDevice>();
            if (m_EyeGazeDevice == null)
            {
                Debug.LogError("Failed to create Eye Gaze device.", this);
                m_PoseControl = null;
                return;
            }

            m_PoseControl = m_EyeGazeDevice["pose"];
        }

        void RemoveEyeGazeDevice()
        {
            if (m_EyeGazeDevice != null && m_EyeGazeDevice.added)
            {
                InputSystem.InputSystem.RemoveDevice(m_EyeGazeDevice);
                m_PoseControl = null;
            }
        }

        static bool HasEyeTrackingPermission()
        {
#if UNITY_ANDROID
            return Permission.HasUserAuthorizedPermission(k_EyeTrackingPermission);
#else
            return true;
#endif
        }

#if UNITY_ANDROID
        static void RequestEyeTrackingPermission(PermissionCallbacks callbacks)
        {
            if (!HasEyeTrackingPermission())
                Permission.RequestUserPermission(k_EyeTrackingPermission, callbacks);
        }
        
        void OnPermissionGranted(string permissionName)
        {
            Debug.Log(permissionName + " permissions granted by the user.", this);
            Debug.Log("Starting eye tracking.", this);
            m_OculusEyeTrackingStarted = StartEyeTracking();
            if (m_OculusEyeTrackingStarted)
                AddEyeGazeDevice();
        }
        
        void OnPermissionDenied(string permissionName)
        {
            Debug.Log(permissionName + " permissions denied by the user.", this);
            m_OculusEyeTrackingStarted = false;
            RemoveEyeGazeDevice();
        }
        
        void OnPermissionDeniedAndDontAskAgain(string permissionName)
        {
            Debug.Log(permissionName + " permissions denied and don't ask again by the user.", this);
            m_OculusEyeTrackingStarted = false;
            RemoveEyeGazeDevice();
        }
#endif
#endif
    }
}