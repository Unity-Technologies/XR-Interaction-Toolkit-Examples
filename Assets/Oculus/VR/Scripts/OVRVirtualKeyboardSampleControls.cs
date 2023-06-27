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

using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(OVRVirtualKeyboardSampleInputHandler))]
public class OVRVirtualKeyboardSampleControls : MonoBehaviour
{
    private struct OVRVirtualKeyboardBackup
    {
        private readonly InputField _textCommitField;
        private readonly Vector3 _position;
        private readonly Quaternion _rotation;
        private readonly Vector3 _scale;
        private readonly Transform _rightControllerInputTransform;
        private readonly Transform _leftControllerInputTransform;
        private readonly bool _controllerRayInteraction;
        private readonly bool _controllerDirectInteraction;
        private readonly LayerMask _controllerRaycastLayerMask;
        private readonly OVRHand _handLeft;
        private readonly OVRHand _handRight;
        private readonly bool _handRayInteraction;
        private readonly bool _handDirectInteraction;
        private readonly LayerMask _handRaycastLayerMask;

        public OVRVirtualKeyboardBackup(OVRVirtualKeyboard keyboard)
        {
            _textCommitField = keyboard.TextCommitField;
            _position = keyboard.transform.position;
            _rotation = keyboard.transform.rotation;
            _scale = keyboard.transform.localScale;

            _rightControllerInputTransform = keyboard.rightControllerInputTransform;
            _leftControllerInputTransform = keyboard.leftControllerInputTransform;
            _controllerRayInteraction = keyboard.controllerRayInteraction;
            _controllerDirectInteraction = keyboard.controllerDirectInteraction;
            _controllerRaycastLayerMask = keyboard.handRaycastLayerMask;

            _handLeft = keyboard.handLeft;
            _handRight = keyboard.handRight;
            _handRayInteraction = keyboard.handRayInteraction;
            _handDirectInteraction = keyboard.handDirectInteraction;
            _handRaycastLayerMask = keyboard.handRaycastLayerMask;
        }

        public void RestoreTo(OVRVirtualKeyboard keyboard)
        {
            keyboard.TextCommitField = _textCommitField;
            keyboard.transform.SetPositionAndRotation(_position, _rotation);
            keyboard.transform.localScale = _scale;

            keyboard.rightControllerInputTransform = _rightControllerInputTransform;
            keyboard.leftControllerInputTransform = _leftControllerInputTransform;
            keyboard.controllerRayInteraction = _controllerRayInteraction;
            keyboard.controllerDirectInteraction = _controllerDirectInteraction;
            keyboard.controllerRaycastLayerMask = _controllerRaycastLayerMask;

            keyboard.handLeft = _handLeft;
            keyboard.handRight = _handRight;
            keyboard.handRayInteraction = _handRayInteraction;
            keyboard.handDirectInteraction = _handDirectInteraction;
            keyboard.handRaycastLayerMask = _handRaycastLayerMask;
        }
    }

    private const float THUMBSTICK_DEADZONE = 0.2f;

    [SerializeField]
    private Button ShowButton;

    [SerializeField]
    private Button MoveButton;

    [SerializeField]
    private Button HideButton;

    [SerializeField]
    private Button MoveNearButton;

    [SerializeField]
    private Button MoveFarButton;

    [SerializeField]
    private Button DestroyKeyboardButton;

    [SerializeField]
    private OVRVirtualKeyboard keyboard;

    private OVRVirtualKeyboardSampleInputHandler inputHandler;

    private bool isMovingKeyboard_ = false;
    private bool isMovingKeyboardFinished_ = false;
    private float keyboardMoveDistance_ = 0.0f;
    private float keyboardScale_ = 1.0f;
    private OVRVirtualKeyboardBackup keyboardBackup;

    void Start()
    {
        inputHandler = GetComponent<OVRVirtualKeyboardSampleInputHandler>();

        ShowKeyboard();

        keyboard.KeyboardHidden += OnHideKeyboard;

        MoveNearButton.onClick.AddListener(MoveKeyboardNear);
        MoveFarButton.onClick.AddListener(MoveKeyboardFar);
        DestroyKeyboardButton.onClick.AddListener(DestroyKeyboard);
    }

    private void OnDestroy()
    {
        if (keyboard == null)
        {
            return;
        }

        keyboard.KeyboardHidden -= OnHideKeyboard;
        MoveNearButton.onClick.RemoveListener(MoveKeyboardNear);
        MoveFarButton.onClick.RemoveListener(MoveKeyboardFar);
        DestroyKeyboardButton.onClick.RemoveListener(DestroyKeyboard);
    }

    public void ShowKeyboard()
    {
        if (keyboard == null)
        {
            var go = new GameObject();
            keyboard = go.AddComponent<OVRVirtualKeyboard>();
            keyboardBackup.RestoreTo(keyboard);
            inputHandler.OVRVirtualKeyboard = keyboard;
        }

        keyboard.gameObject.SetActive(true);
        UpdateButtonInteractable();
    }

    public void MoveKeyboard()
    {
        if (!keyboard.gameObject.activeSelf) return;
        isMovingKeyboard_ = true;
        var kbTransform = keyboard.transform;
        keyboardMoveDistance_ = (inputHandler.InputRayPosition - kbTransform.position).magnitude;
        keyboardScale_ = kbTransform.localScale.x;
        UpdateButtonInteractable();
        keyboard.InputEnabled = false;
    }

    public void MoveKeyboardNear()
    {
        if (!keyboard.gameObject.activeSelf) return;
        keyboard.UseSuggestedLocation(OVRVirtualKeyboard.KeyboardPosition.Direct);
    }

    public void MoveKeyboardFar()
    {
        if (!keyboard.gameObject.activeSelf) return;
        keyboard.UseSuggestedLocation(OVRVirtualKeyboard.KeyboardPosition.Far);
    }

    public void HideKeyboard()
    {
        keyboard.gameObject.SetActive(false);
        isMovingKeyboard_ = false;
        UpdateButtonInteractable();
    }

    public void DestroyKeyboard()
    {
        if (keyboard != null)
        {
            keyboardBackup = new OVRVirtualKeyboardBackup(keyboard);
            GameObject.Destroy(keyboard.gameObject);
            keyboard = null;
            UpdateButtonInteractable();
        }
    }

    private void OnHideKeyboard()
    {
        UpdateButtonInteractable();
    }

    private void UpdateButtonInteractable()
    {
        var kbExists = keyboard != null;
        var kbActiveAndNotMoving = kbExists && keyboard.gameObject.activeSelf && !isMovingKeyboard_;
        ShowButton.interactable = !kbExists || !keyboard.gameObject.activeSelf;
        MoveButton.interactable = kbActiveAndNotMoving;
        MoveNearButton.interactable = kbActiveAndNotMoving;
        MoveFarButton.interactable = kbActiveAndNotMoving;
        HideButton.interactable = kbActiveAndNotMoving;
        DestroyKeyboardButton.interactable = kbExists;
    }

    void Update()
    {
        var isPressed = OVRInput.Get(
            OVRInput.Button.One | // right hand pinch
            OVRInput.Button.Three | // left hand pinch
            OVRInput.Button.PrimaryIndexTrigger |
            OVRInput.Button.SecondaryIndexTrigger,
            OVRInput.Controller.All);
        if (isMovingKeyboardFinished_ && !isPressed)
        {
            keyboard.InputEnabled = true;
            isMovingKeyboard_ = false;
            isMovingKeyboardFinished_ = false;
            UpdateButtonInteractable();
        }

        if (isMovingKeyboard_ && !isMovingKeyboardFinished_)
        {
            keyboardMoveDistance_ *= 1.0f + inputHandler.AnalogStickY * 0.01f;
            keyboardMoveDistance_ = Mathf.Clamp(keyboardMoveDistance_, 0.1f, 100.0f);

            keyboardScale_ += inputHandler.AnalogStickX * 0.01f;
            keyboardScale_ = Mathf.Clamp(keyboardScale_, 0.25f, 2.0f);

            var rotation = inputHandler.InputRayRotation;
            var kbTransform = keyboard.transform;
            kbTransform.SetPositionAndRotation(
                inputHandler.InputRayPosition + keyboardMoveDistance_ * (rotation * Vector3.forward),
                rotation);
            kbTransform.localScale = Vector3.one * keyboardScale_;

            if (isPressed)
            {
                // Delay the true finish by a frame
                isMovingKeyboardFinished_ = true;
            }
        }
    }
}
