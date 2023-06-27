using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// An interactable that can be pressed by a direct interactor
/// </summary>
public class XRButton : XRBaseInteractable
{
    [Tooltip("The transform of the visual component of the button")]
    public Transform buttonTransform = null;

    [Tooltip("The distance the button can be pressed")]
    public float pressDistance = 0.1f;

    // When the button is pressed
    public UnityEvent OnPress = new UnityEvent();

    // When the button is released
    public UnityEvent OnRelease = new UnityEvent();

    private float yMin = 0.0f;
    private float yMax = 0.0f;

    private IXRHoverInteractor hoverInteractor = null;

    private float hoverHeight = 0.0f;
    private float startHeight = 0.0f;
    private bool previousPress = false;

    protected override void OnEnable()
    {
        base.OnEnable();
        hoverEntered.AddListener(StartPress);
        hoverExited.AddListener(EndPress);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        hoverEntered.RemoveListener(StartPress);
        hoverExited.RemoveListener(EndPress);
    }

    private void StartPress(HoverEnterEventArgs eventArgs)
    {
        hoverInteractor = eventArgs.interactorObject;
        hoverHeight = GetLocalYPosition(hoverInteractor.transform.position);
        startHeight = buttonTransform.localPosition.y;
    }

    private void EndPress(HoverExitEventArgs eventArgs)
    {
        hoverInteractor = null;
        hoverHeight = 0.0f;
        startHeight = 0.0f;
        ApplyHeight(yMax);
    }

    private void Start()
    {
        SetMinMax();
    }

    private void SetMinMax()
    {
        yMin = buttonTransform.localPosition.y - pressDistance;
        yMax = buttonTransform.localPosition.y;
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        if(updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
        {
            if (isHovered)
            {
                float height = FindButtonHeight();
                ApplyHeight(height);
            }
        }
    }

    private float FindButtonHeight()
    {
        float newHoverHeight = GetLocalYPosition(hoverInteractor.transform.position);
        float hoverDifference = hoverHeight - newHoverHeight;
        return startHeight - hoverDifference;
    }

    private float GetLocalYPosition(Vector3 position)
    {
        Vector3 localPosition = transform.InverseTransformPoint(position);
        return localPosition.y;
    }

    private void ApplyHeight(float position)
    {
        SetButtonPosition(position);
        CheckPress();
    }

    private void SetButtonPosition(float position)
    {
        Vector3 newPosition = buttonTransform.localPosition;
        newPosition.y = Mathf.Clamp(position, yMin, yMax);
        buttonTransform.localPosition = newPosition;
    }

    private void CheckPress()
    {
        bool inPosition = InPosition();

        if(inPosition != previousPress)
        {
            previousPress = inPosition;

            if(inPosition)
            {
                OnPress.Invoke();
            }
            else
            {
                OnRelease.Invoke();
            }
        }
    }

    private bool InPosition()
    {
        float threshold = yMin + (pressDistance * 0.5f);
        return buttonTransform.localPosition.y < threshold;
    }

    public override bool IsSelectableBy(IXRSelectInteractor interactor)
    {
        return false;
    }
}
