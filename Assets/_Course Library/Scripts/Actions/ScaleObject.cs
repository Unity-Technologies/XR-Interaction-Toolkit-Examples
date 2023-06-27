using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ScaleObject : MonoBehaviour
{
    public Vector3 targetScale = Vector3.one;
    private Vector3 originalScale = Vector3.one;

    public void ApplyTargetScale(XRBaseInteractable interactable)
    {
        originalScale = interactable.transform.localScale;
        interactable.transform.localScale = targetScale;
    }

    public void ResetScale(XRBaseInteractable interactable)
    {
        interactable.transform.localScale = originalScale;
        originalScale = Vector3.one;
    }
}
