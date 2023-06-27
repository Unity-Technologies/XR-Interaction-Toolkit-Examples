using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// An interactable joystick that can move side to side, and forward and back by a direct interactor
/// </summary>
public class XRJoystick : XRBaseInteractable
{
    public enum JoystickType
    {
        None,
        Both,
        FrontBack,
        LeftRight,
    }

    [Tooltip("Joystick sensitivity")]
    public float rateOfChange = 0.1f;

    [Tooltip("Contols how the joystick behaves ")]
    public JoystickType leverType = JoystickType.Both;

    [Tooltip("The transform of the visual component of the joystick")]
    public Transform handle = null;

    [Serializable] public class ValueChangeEvent : UnityEvent<float> { }

    // When the joystick's x value changes
    public ValueChangeEvent OnXValueChange = new ValueChangeEvent();

    // When the joystick's y value changes
    public ValueChangeEvent OnYValueChange = new ValueChangeEvent();

    public Vector2 Value { get; private set; } = Vector2.zero;
    private IXRSelectInteractor selectInteractor = null;

    private Vector3 initialPosition = Vector3.zero;

    private readonly int minRotation = -30;
    private readonly int maxRotation = 30;

    protected override void OnEnable()
    {
        base.OnEnable();
        selectEntered.AddListener(StartGrab);
        selectExited.AddListener(EndGrab);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        selectEntered.RemoveListener(StartGrab);
        selectExited.RemoveListener(EndGrab);
    }

    private void StartGrab(SelectEnterEventArgs eventArgs)
    {
        selectInteractor = eventArgs.interactorObject;
        initialPosition = ConvertToLocal(selectInteractor.transform.position);
    }

    private void EndGrab(SelectExitEventArgs eventArgs)
    {
        selectInteractor = null;
        ResetRotation();
    }

    private void ResetRotation()
    {
        handle.localRotation = Quaternion.identity;
        initialPosition = Vector3.zero;
        SetValue(Vector3.zero);
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);

        if (isSelected)
        {
            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                Vector3 targetPosition = selectInteractor.transform.position;
                Vector3 newRotation = FindJoystickRotation(targetPosition);

                ApplyRotation(newRotation);
                SetValue(newRotation);
            }
        }
    }

    private Vector3 FindJoystickRotation(Vector3 target)
    {
        Vector3 currentPosition = ConvertToLocal(target);
        Vector3 positionDifference = currentPosition - initialPosition;
        positionDifference.y = rateOfChange;

        Vector3 finalRotation = Vector3.zero;

        if (leverType == JoystickType.FrontBack || leverType == JoystickType.Both)
        {
            float xRotation = GetRotationDifference(positionDifference.y, positionDifference.z);
            finalRotation.x = xRotation;
        }

        if (leverType == JoystickType.LeftRight || leverType == JoystickType.Both)
        {
            float zRotation = GetRotationDifference(positionDifference.y, positionDifference.x);
            finalRotation.z = zRotation;
        }

        return finalRotation;
    }

    private Vector3 ConvertToLocal(Vector3 target)
    {
        return transform.InverseTransformPoint(target);
    }

    private float GetRotationDifference(float yRelative, float xRelative)
    {
        float difference = Mathf.Atan2(xRelative, yRelative) * Mathf.Rad2Deg;
        difference = Mathf.Clamp(difference, minRotation, maxRotation);
        return difference;
    }

    private void ApplyRotation(Vector3 newRotation)
    {
        newRotation.z *= -1;
        handle.localRotation = Quaternion.Euler(newRotation);
    }

    private void SetValue(Vector3 rotation)
    {
        Value = CalculateValue(rotation);
        OnXValueChange.Invoke(Value.x);
        OnYValueChange.Invoke(Value.y);
    }

    private Vector2 CalculateValue(Vector3 rotation)
    {
        Vector2 newValue = Vector2.zero;
        newValue.x = Normalize(rotation.z);
        newValue.y = Normalize(rotation.x);
        return newValue;
    }

    private float Normalize(float value)
    {
        value = Mathf.InverseLerp(minRotation, maxRotation, value);
        value = Mathf.Lerp(-1, 1, value);
        return value;
    }
}