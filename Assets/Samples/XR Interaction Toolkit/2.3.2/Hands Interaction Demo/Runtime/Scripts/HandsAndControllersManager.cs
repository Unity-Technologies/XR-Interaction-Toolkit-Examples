using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
#if XR_HANDS
using UnityEngine.XR.Hands;
#endif

namespace UnityEngine.XR.Interaction.Toolkit.Samples.Hands
{
    /// <summary>
    /// Manages swapping between hands and controllers at runtime based on whether hands and controllers are tracked.
    /// </summary>
    /// <remarks>
    /// If hands begin tracking, this component will switch to the hand group of interactors.
    /// If the player wakes the motion controllers by grabbing them, this component will switch to the motion controller group of interactors.
    /// Additionally, if a controller has never been tracked, this component will wait to activate that GameObject until it is tracked.
    /// </remarks>
    public class HandsAndControllersManager : MonoBehaviour
    {
        enum Mode
        {
            None,
            MotionController,
            TrackedHand,
        }

        [Header("Hand Tracking")]
        [SerializeField]
        [Tooltip("GameObject representing the left hand group of interactors. Will toggle on when using hand tracking and off when using motion controllers.")]
        GameObject m_LeftHand;

        [SerializeField]
        [Tooltip("GameObject representing the right hand group of interactors. Will toggle on when using hand tracking and off when using motion controllers.")]
        GameObject m_RightHand;

        [Header("Motion Controllers")]
        [SerializeField]
        [Tooltip("GameObject representing the left motion controller group of interactors. Will toggle on when using motion controllers and off when using hand tracking.")]
        GameObject m_LeftController;

        [SerializeField]
        [Tooltip("GameObject representing the left motion controller group of interactors. Will toggle on when using motion controllers and off when using hand tracking.")]
        GameObject m_RightController;

#if XR_HANDS
        XRHandSubsystem m_HandSubsystem;

        /// <summary>
        /// Temporary list used when getting the subsystems.
        /// </summary>
        static readonly List<XRHandSubsystem> s_HandSubsystems = new List<XRHandSubsystem>();
#endif

        readonly TrackedDeviceMonitor m_TrackedDeviceMonitor = new TrackedDeviceMonitor();

        /// <summary>
        /// Used to store whether the motion controller has ever been <c>isTracked</c> at some point.
        /// This is to avoid enabling the controller visuals and interactors if the controller has never been held
        /// to avoid seeing it at origin, since both controller devices are added to Input System upon the first
        /// being picked up by the player.
        /// </summary>
        readonly HashSet<int> m_DevicesEverTracked = new HashSet<int>();

        Mode m_LeftMode;
        Mode m_RightMode;

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void OnEnable()
        {
#if XR_HANDS
            if (m_HandSubsystem == null)
            {
                SubsystemManager.GetSubsystems(s_HandSubsystems);
                if (s_HandSubsystems.Count == 0)
                {
                    Debug.LogWarning("Hand Tracking Subsystem not found, can't subscribe to hand tracking status. Enable that feature in the OpenXR project settings and ensure OpenXR is enabled as the plug-in provider.", this);
                }
                else
                {
                    m_HandSubsystem = s_HandSubsystems[0];
                }
            }

            if (m_HandSubsystem != null)
                m_HandSubsystem.trackingAcquired += OnHandTrackingAcquired;
#else
            Debug.LogWarning("Script requires XR Hands (com.unity.xr.hands) package to switch to hand tracking groups. Install using Window > Package Manager or click Fix on the related issue in Edit > Project Settings > XR Plug-in Management > Project Validation.", this);
#endif

            InputSystem.InputSystem.onDeviceChange += OnDeviceChange;
            m_TrackedDeviceMonitor.trackingAcquired += OnControllerTrackingFirstAcquired;

            UpdateLeftMode();
            UpdateRightMode();
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void OnDisable()
        {
#if XR_HANDS
            if (m_HandSubsystem != null)
                m_HandSubsystem.trackingAcquired -= OnHandTrackingAcquired;
#endif

            InputSystem.InputSystem.onDeviceChange -= OnDeviceChange;
            if (m_TrackedDeviceMonitor != null)
            {
                m_TrackedDeviceMonitor.trackingAcquired -= OnControllerTrackingFirstAcquired;
                m_TrackedDeviceMonitor.ClearAllDevices();
            }
        }

        void SetLeftMode(Mode mode)
        {
            SafeSetActive(m_LeftHand, mode == Mode.TrackedHand);
            SafeSetActive(m_LeftController, mode == Mode.MotionController);
            m_LeftMode = mode;
        }

        void SetRightMode(Mode mode)
        {
            SafeSetActive(m_RightHand, mode == Mode.TrackedHand);
            SafeSetActive(m_RightController, mode == Mode.MotionController);
            m_RightMode = mode;
        }

        static void SafeSetActive(GameObject gameObject, bool active)
        {
            if (gameObject != null && gameObject.activeSelf != active)
                gameObject.SetActive(active);
        }

        void UpdateLeftMode()
        {
#if XR_HANDS
            if (m_HandSubsystem != null && m_HandSubsystem.leftHand.isTracked)
            {
                SetLeftMode(Mode.TrackedHand);
                return;
            }
#endif

            var controllerDevice = InputSystem.InputSystem.GetDevice<InputSystem.XR.XRController>(InputSystem.CommonUsages.LeftHand);
            UpdateMode(controllerDevice, SetLeftMode);
        }

        void UpdateRightMode()
        {
#if XR_HANDS
            if (m_HandSubsystem != null && m_HandSubsystem.rightHand.isTracked)
            {
                SetRightMode(Mode.TrackedHand);
                return;
            }
#endif

            var controllerDevice = InputSystem.InputSystem.GetDevice<InputSystem.XR.XRController>(InputSystem.CommonUsages.RightHand);
            UpdateMode(controllerDevice, SetRightMode);
        }

        void UpdateMode(InputSystem.XR.XRController controllerDevice, Action<Mode> setModeMethod)
        {
            if (controllerDevice == null)
            {
                setModeMethod(Mode.None);
                return;
            }

            if (m_DevicesEverTracked.Contains(controllerDevice.deviceId))
            {
                setModeMethod(Mode.MotionController);
            }
            else if (controllerDevice.isTracked.isPressed)
            {
                m_DevicesEverTracked.Add(controllerDevice.deviceId);
                setModeMethod(Mode.MotionController);
            }
            else
            {
                // Start monitoring for when the controller is tracked
                setModeMethod(Mode.None);
                m_TrackedDeviceMonitor.AddDevice(controllerDevice);
            }
        }

        void OnDeviceChange(InputSystem.InputDevice device, InputDeviceChange change)
        {
            if (!(device is InputSystem.XR.XRController controllerDevice))
                return;

            if (change == InputDeviceChange.Added ||
                change == InputDeviceChange.Reconnected ||
                change == InputDeviceChange.Enabled ||
                change == InputDeviceChange.UsageChanged)
            {
                if (!device.added)
                    return;

                var usages = device.usages;
                if (usages.Contains(InputSystem.CommonUsages.LeftHand))
                {
                    UpdateMode(controllerDevice, SetLeftMode);
                }
                else if (usages.Contains(InputSystem.CommonUsages.RightHand))
                {
                    UpdateMode(controllerDevice, SetRightMode);
                }
            }
            else if (change == InputDeviceChange.Removed ||
                     change == InputDeviceChange.Disconnected ||
                     change == InputDeviceChange.Disabled)
            {
                m_TrackedDeviceMonitor.RemoveDevice(controllerDevice);

                // Swap to hand tracking if tracked or turn off the controller
                var usages = device.usages;
                if (usages.Contains(InputSystem.CommonUsages.LeftHand))
                {
#if XR_HANDS
                    var mode = m_HandSubsystem != null && m_HandSubsystem.leftHand.isTracked ? Mode.TrackedHand : Mode.None;
#else
                    const Mode mode = Mode.None;
#endif

                    SetLeftMode(mode);
                }
                else if (usages.Contains(InputSystem.CommonUsages.RightHand))
                {
#if XR_HANDS
                    var mode = m_HandSubsystem != null && m_HandSubsystem.rightHand.isTracked ? Mode.TrackedHand : Mode.None;
#else
                    const Mode mode = Mode.None;
#endif

                    SetRightMode(mode);
                }
            }
        }

        void OnControllerTrackingFirstAcquired(TrackedDevice device)
        {
            if (!(device is InputSystem.XR.XRController))
                return;

            m_DevicesEverTracked.Add(device.deviceId);

            var usages = device.usages;
            if (usages.Contains(InputSystem.CommonUsages.LeftHand))
            {
                if (m_LeftMode == Mode.None)
                    SetLeftMode(Mode.MotionController);
            }
            else if (usages.Contains(InputSystem.CommonUsages.RightHand))
            {
                if (m_RightMode == Mode.None)
                    SetRightMode(Mode.MotionController);
            }
        }

#if XR_HANDS
        void OnHandTrackingAcquired(XRHand hand)
        {
            switch (hand.handedness)
            {
                case Handedness.Left:
                    SetLeftMode(Mode.TrackedHand);
                    break;
                case Handedness.Right:
                    SetRightMode(Mode.TrackedHand);
                    break;
            }
        }
#endif

        /// <summary>
        /// Helper class to monitor tracked devices from Input System and invoke an event
        /// when the device is tracked. Used in the behavior to keep a GameObject deactivated
        /// until the device becomes tracked, at which point the callback method can activate it.
        /// </summary>
        class TrackedDeviceMonitor
        {
            /// <summary>
            /// Event that is invoked one time when the device is tracked.
            /// </summary>
            /// <seealso cref="AddDevice"/>
            /// <seealso cref="TrackedDevice.isTracked"/>
            public event Action<TrackedDevice> trackingAcquired;

            readonly List<int> m_MonitoredDevices = new List<int>();

            bool m_SubscribedOnAfterUpdate;

            /// <summary>
            /// Add a tracked device to monitor and invoke <see cref="trackingAcquired"/>
            /// one time when the device is tracked. The device is automatically removed
            /// from being monitored when tracking is acquired.
            /// </summary>
            /// <param name="device"></param>
            /// <remarks>
            /// Waits until the next Input System update to read if the device is tracked.
            /// </remarks>
            public void AddDevice(TrackedDevice device)
            {
                // Start subscribing if necessary
                if (!m_MonitoredDevices.Contains(device.deviceId))
                {
                    m_MonitoredDevices.Add(device.deviceId);
                    SubscribeOnAfterUpdate();
                }
            }

            /// <summary>
            /// Stop monitoring the device for its tracked status.
            /// </summary>
            /// <param name="device"></param>
            public void RemoveDevice(TrackedDevice device)
            {
                // Stop subscribing if there are no devices left to monitor
                if (m_MonitoredDevices.Remove(device.deviceId) && m_MonitoredDevices.Count == 0)
                    UnsubscribeOnAfterUpdate();
            }

            /// <summary>
            /// Stop monitoring all devices for their tracked status.
            /// </summary>
            public void ClearAllDevices()
            {
                if (m_MonitoredDevices.Count > 0)
                {
                    m_MonitoredDevices.Clear();
                    UnsubscribeOnAfterUpdate();
                }
            }

            void SubscribeOnAfterUpdate()
            {
                if (!m_SubscribedOnAfterUpdate && m_MonitoredDevices.Count > 0)
                {
                    InputSystem.InputSystem.onAfterUpdate += OnAfterInputUpdate;
                    m_SubscribedOnAfterUpdate = true;
                }
            }

            void UnsubscribeOnAfterUpdate()
            {
                if (m_SubscribedOnAfterUpdate)
                {
                    InputSystem.InputSystem.onAfterUpdate -= OnAfterInputUpdate;
                    m_SubscribedOnAfterUpdate = false;
                }
            }

            void OnAfterInputUpdate()
            {
                for (var index = 0; index < m_MonitoredDevices.Count; ++index)
                {
                    if (!(InputSystem.InputSystem.GetDeviceById(m_MonitoredDevices[index]) is TrackedDevice device))
                        continue;

                    if (!device.isTracked.isPressed)
                        continue;

                    // Stop monitoring and invoke event
                    m_MonitoredDevices.RemoveAt(index);
                    --index;

                    trackingAcquired?.Invoke(device);
                }

                // Once all monitored devices have been tracked, unsubscribe from the Input System callback
                if (m_MonitoredDevices.Count == 0)
                    UnsubscribeOnAfterUpdate();
            }
        }
    }
}