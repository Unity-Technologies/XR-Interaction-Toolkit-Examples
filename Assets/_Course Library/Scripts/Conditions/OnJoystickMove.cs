using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Event for tracking the input of a joystick
/// </summary>
public class OnJoystickMove : MonoBehaviour
{
    [Tooltip("Actions to check")]
    public InputAction action = null;

    [Serializable] public class ValueChangeEvent : UnityEvent<float> { }

    // Called when the x value changes
    public ValueChangeEvent OnXValueChange = new ValueChangeEvent();

    // Called when the y value changes
    public ValueChangeEvent OnYValueChange = new ValueChangeEvent();

    private void Awake()
    {
        action.performed += OnValueChange;
        action.canceled += OnValueChange;
    }

    private void OnDestroy()
    {
        action.performed -= OnValueChange;
        action.canceled -= OnValueChange;
    }

    private void OnEnable()
    {
        action.Enable();
    }

    private void OnDisable()
    {
        action.Disable();
    }

    private void OnValueChange(InputAction.CallbackContext context)
    {
        Vector2 value = context.ReadValue<Vector2>();
        OnXValueChange.Invoke(value.x);
        OnYValueChange.Invoke(value.y);
    }
}
