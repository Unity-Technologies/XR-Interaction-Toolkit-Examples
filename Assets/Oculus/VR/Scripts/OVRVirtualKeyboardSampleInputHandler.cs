/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using UnityEngine;
using UnityEngine.EventSystems;

public class OVRVirtualKeyboardSampleInputHandler : MonoBehaviour
{
    private const float RAY_HIDE_BEHIND_DISTANCE = 0.15f;
    private const float RAY_HIDE_DISTANCE = RAY_HIDE_BEHIND_DISTANCE + 0.05f;
    private const float RAY_MAX_DISTANCE = 100.0f;
    private const float THUMBSTICK_DEADZONE = 0.2f;

    private static float ApplyDeadzone(float value)
    {
        if (value > THUMBSTICK_DEADZONE)
            return (value - THUMBSTICK_DEADZONE) / (1.0f - THUMBSTICK_DEADZONE);
        else if (value < -THUMBSTICK_DEADZONE)
            return (value + THUMBSTICK_DEADZONE) / (1.0f - THUMBSTICK_DEADZONE);
        return 0.0f;
    }

    public float AnalogStickX => ApplyDeadzone(OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).x +
                                               OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).x);

    public float AnalogStickY => ApplyDeadzone(OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y +
                                               OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y);

    public Vector3 InputRayPosition => inputModule.rayTransform.position;

    public Quaternion InputRayRotation =>
        interactionDevice_ == OVRInput.Controller.LHand
            ? inputModule.rayTransform.rotation * Quaternion.Euler(Vector3.forward * 180)
            : // Flip input rotation if left hand input
            inputModule.rayTransform.rotation;

    public OVRVirtualKeyboard OVRVirtualKeyboard;

    [SerializeField]
    private OVRRaycaster raycaster;

    [SerializeField]
    private OVRInputModule inputModule;

    [SerializeField]
    private LineRenderer leftLinePointer;

    [SerializeField]
    private LineRenderer rightLinePointer;

    private OVRInput.Controller? interactionDevice_;

    private void Start()
    {
        rightLinePointer.enabled = leftLinePointer.enabled = false;
    }

    private void Update()
    {
        UpdateInteractionAnchor();
        UpdateLineRenderer();
    }

    private void UpdateLineRenderer()
    {
        leftLinePointer.enabled = false;
        rightLinePointer.enabled = false;

        UpdateLineRendererFromSource(OVRVirtualKeyboard.InputSource.ControllerLeft);
        UpdateLineRendererFromSource(OVRVirtualKeyboard.InputSource.ControllerRight);
        UpdateLineRendererFromSource(OVRVirtualKeyboard.InputSource.HandLeft);
        UpdateLineRendererFromSource(OVRVirtualKeyboard.InputSource.HandRight);
    }

    private void UpdateLineRendererFromSource(OVRVirtualKeyboard.InputSource source)
    {
        Transform inputTransform = null;
        switch (source)
        {
            case OVRVirtualKeyboard.InputSource.ControllerLeft:
                inputTransform = OVRInput.IsControllerConnected(OVRInput.Controller.LTouch) &&
                (!OVRVirtualKeyboard.handLeft || !OVRVirtualKeyboard.handLeft.IsTracked)
                ? OVRVirtualKeyboard.leftControllerInputTransform : null;
                break;
            case OVRVirtualKeyboard.InputSource.ControllerRight:
                inputTransform = OVRInput.IsControllerConnected(OVRInput.Controller.RTouch) &&
                (!OVRVirtualKeyboard.handRight || !OVRVirtualKeyboard.handRight.IsTracked)
                ? OVRVirtualKeyboard.rightControllerInputTransform : null;

                break;
            case OVRVirtualKeyboard.InputSource.HandLeft:
                inputTransform = (OVRVirtualKeyboard.handLeft.IsPointerPoseValid)
                    ? OVRVirtualKeyboard.handLeft.PointerPose
                    : null;
                break;
            case OVRVirtualKeyboard.InputSource.HandRight:
                inputTransform = (OVRVirtualKeyboard.handRight.IsPointerPoseValid)
                    ? OVRVirtualKeyboard.handRight.PointerPose
                    : null;
                break;
        }

        if (inputTransform == null || inputTransform.position == Vector3.zero)
        {
            return;
        }

        if (OVRVirtualKeyboard &&
            OVRVirtualKeyboard.Collider &&
            (OVRVirtualKeyboard.Collider.ClosestPoint(inputTransform.position) -
                OVRVirtualKeyboard.transform.position).sqrMagnitude < RAY_HIDE_DISTANCE * RAY_HIDE_DISTANCE)
        {
            return;
        }

        bool isLeft = (source == OVRVirtualKeyboard.InputSource.ControllerLeft ||
                       source == OVRVirtualKeyboard.InputSource.HandLeft);
        LineRenderer linePointer = (isLeft) ? leftLinePointer : rightLinePointer;
        linePointer.enabled = true;
        linePointer.SetPosition(0, inputTransform.position);

        Vector3 rayOrigin = inputTransform.position +
                            RAY_HIDE_BEHIND_DISTANCE * (inputTransform.rotation * Vector3.back);
        var ray = new Ray(rayOrigin, inputTransform.rotation * Vector3.forward);
        if (Physics.Raycast(ray, out var hit, RAY_MAX_DISTANCE))
        {
            linePointer.SetPosition(1, hit.point);
        }
        else
        {
            linePointer.SetPosition(1, inputTransform.position + ray.direction * 2.5f);
        }
    }

    private void UpdateInteractionAnchor()
    {
        OVRInput.Controller activeController = OVRInput.Controller.None;

        var leftControllerExists = OVRVirtualKeyboard.leftControllerInputTransform != null;
        var leftControllerActive = leftControllerExists && OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger);
        activeController = (leftControllerActive) ? OVRInput.Controller.LTouch : activeController;

        var rightControllerExists = OVRVirtualKeyboard.rightControllerInputTransform != null;
        var rightControllerActive = rightControllerExists && OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger);
        activeController = (rightControllerActive) ? OVRInput.Controller.RTouch : activeController;

        var handLeftExists = OVRVirtualKeyboard.handLeft != null;
        var handLeftIsActive =
            handLeftExists && OVRVirtualKeyboard.handLeft.GetFingerIsPinching(OVRHand.HandFinger.Index);
        activeController = (handLeftIsActive) ? OVRInput.Controller.LHand : activeController;

        var handRightExists = OVRVirtualKeyboard.handRight != null;
        var handRightIsActive =
            handRightExists && OVRVirtualKeyboard.handRight.GetFingerIsPinching(OVRHand.HandFinger.Index);
        activeController = (handRightIsActive) ? OVRInput.Controller.RHand : activeController;

        if (activeController == OVRInput.Controller.None)
        {
            return;
        }

        // Set transforms for Unity UI interaction
        var dominantHandIsLeft =
            (activeController == OVRInput.Controller.LHand || activeController == OVRInput.Controller.LTouch);
        raycaster.pointer = (dominantHandIsLeft)
            ? OVRVirtualKeyboard.handLeft.gameObject
            : OVRVirtualKeyboard.handRight.gameObject;
        interactionDevice_ = activeController;
        inputModule.rayTransform = activeController switch
        {
            OVRInput.Controller.LHand => OVRVirtualKeyboard.handLeft.PointerPose,
            OVRInput.Controller.LTouch => OVRVirtualKeyboard.handLeft.transform,
            OVRInput.Controller.RHand => OVRVirtualKeyboard.handRight.PointerPose,
            _ => OVRVirtualKeyboard.handRight.transform
        };
    }
}
