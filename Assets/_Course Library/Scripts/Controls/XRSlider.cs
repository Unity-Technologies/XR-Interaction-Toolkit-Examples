using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// An interactable that lets the push/pull a handle a long a linear track by a direct interactor
/// </summary>
public class XRSlider : XRBaseInteractable
{
    [Tooltip("The object that's grabbed and manipulated")]
    public Transform handle = null;

    [Tooltip("The start point of the track")]
    public Transform start = null;

    [Tooltip("The end point of the track")]
    public Transform end = null;

    [Tooltip("The initial value of the slider")]
    [Range(0, 1)] public float defaultValue = 0.0f;

    [Serializable] public class ValueChangeEvent : UnityEvent<float> { }

    // Whenever the slider's value changes
    public ValueChangeEvent OnValueChange = new ValueChangeEvent();

    public float Value { get; private set; } = 0.0f;

    private IXRSelectInteractor selectInteractor = null;
    private Vector3 selectPosition = Vector3.zero;
    private float startingValue = 0.0f;

    private void Start()
    {
        Value = defaultValue;
        ApplyValue(Value);
    }

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
        selectPosition = selectInteractor.transform.position;
        startingValue = Value;
    }

    private void EndGrab(SelectExitEventArgs eventArgs)
    {
        selectInteractor = null;
        selectPosition = Vector3.zero;
        startingValue = 0.0f;
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);

        if (isSelected)
        {
            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                Value = FindPullValue();
                ApplyValue(Value);
            }
        }
    }

    private float FindPullValue()
    {
        Vector3 pullDirection = selectInteractor.transform.position - selectPosition;
        Vector3 targetDirection = end.position - start.position;

        float maxLength = targetDirection.magnitude;
        targetDirection.Normalize();

        float pullValue = Vector3.Dot(pullDirection, targetDirection) / maxLength;
        pullValue += startingValue;

        return Mathf.Clamp(pullValue, 0.0f, 1.0f);
    }

    private void ApplyValue(float value)
    {
        SetHandlePosition(value);
        OnValueChange.Invoke(Value);
    }

    private void SetHandlePosition(float blend)
    {
        Vector3 newPosition = Vector3.Lerp(start.position, end.position, blend);
        handle.position = newPosition;
    }
}
