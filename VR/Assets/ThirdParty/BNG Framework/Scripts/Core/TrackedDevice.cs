using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace BNG {

    /// <summary>
    /// A simple alternative to the TrackedPoseDriver component.
    /// Feel free to swap this out with a TrackedPoseDriver from the XR Legacy Input Helpers package or using the new Unity Input System
    /// </summary>
    public class TrackedDevice : MonoBehaviour {

        public TrackableDevice Device = TrackableDevice.HMD;

        protected InputDevice deviceToTrack;

        protected Vector3 initialLocalPosition;
        protected Quaternion initialLocalRotation;

        protected Vector3 currentLocalPosition;
        protected Quaternion currentLocalRotation;

        protected virtual void Awake() {
            initialLocalPosition = transform.localPosition;
            initialLocalRotation = transform.localRotation;
        }

        protected virtual void OnEnable() {
            Application.onBeforeRender += OnBeforeRender;
        }

        protected virtual void OnDisable() {
            Application.onBeforeRender -= OnBeforeRender;
        }

        protected virtual void Update() {
            RefreshDeviceStatus();

            UpdateDevice();
        }

        protected virtual void FixedUpdate() {
            UpdateDevice();
        }

        public virtual void RefreshDeviceStatus() {
            if (!deviceToTrack.isValid) {

                if (Device == TrackableDevice.HMD) {
                    deviceToTrack = InputBridge.Instance.GetHMD();
                }
                else if (Device == TrackableDevice.LeftController) {
                    deviceToTrack = InputBridge.Instance.GetLeftController();
                }
                else if (Device == TrackableDevice.RightController) {
                    deviceToTrack = InputBridge.Instance.GetRightController();
                }
            }
        }

        public virtual void UpdateDevice() {

            // Check and assign our device status
            if (deviceToTrack.isValid) {

                if (Device == TrackableDevice.HMD) {
                    transform.localPosition = currentLocalPosition = InputBridge.Instance.GetHMDLocalPosition();
                    transform.localRotation = currentLocalRotation = InputBridge.Instance.GetHMDLocalRotation();
                }
                else if (Device == TrackableDevice.LeftController) {
                    transform.localPosition = currentLocalPosition = InputBridge.Instance.GetControllerLocalPosition(ControllerHand.Left);
                    transform.localRotation = currentLocalRotation = InputBridge.Instance.GetControllerLocalRotation(ControllerHand.Left);
                }
                else if (Device == TrackableDevice.RightController) {
                    transform.localPosition = currentLocalPosition = InputBridge.Instance.GetControllerLocalPosition(ControllerHand.Right);
                    transform.localRotation = currentLocalRotation = InputBridge.Instance.GetControllerLocalRotation(ControllerHand.Right);
                }
            }
        }

        protected virtual void OnBeforeRender() {
            UpdateDevice();
        }
    }

    public enum TrackableDevice {
        HMD,
        LeftController,
        RightController
    }
}

