using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// This script either forces the selection or deselection of an interactable objects by the interactor this script is on.
/// </summary>

public class ManuallySelectObject : MonoBehaviour
{
    [Tooltip("What object are we selecting?")]
    public XRBaseInteractable interactable = null;

    private XRBaseControllerInteractor interactor = null;
    private XRInteractionManager interactionManager = null;

    private XRBaseControllerInteractor.InputTriggerType originalTriggerType;

    private void Awake()
    {
        interactor = GetComponent<XRBaseControllerInteractor>();
        interactionManager = interactor.interactionManager;
        originalTriggerType = interactor.selectActionTrigger;
    }

    public void ManuallySelect()
    {
        interactable.gameObject.SetActive(true);
        interactor.selectActionTrigger = XRBaseControllerInteractor.InputTriggerType.StateChange;
        interactionManager.SelectEnter(interactor as IXRSelectInteractor, interactable);
    }

    public void ManuallyDeselect()
    {
        interactionManager.SelectExit(interactor as IXRSelectInteractor, interactable);
        interactor.selectActionTrigger = originalTriggerType;
        interactable.gameObject.SetActive(false);
    }
}
