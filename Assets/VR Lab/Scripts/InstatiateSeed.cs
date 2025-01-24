using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class InstatiateSeed : MonoBehaviour
{

    [SerializeField]
    private GameObject seedPrefab;
    
    // Declare Interactable Interactor and Manager. Needs to be configured in inspector.
    public XRGrabInteractable grabSeedInteractable;
    public XRGrabInteractable grabPouchInteractable;
    public XRInteractionManager interactionManager;
    public XRBaseInteractor leftInteractorObject;
    public XRBaseInteractor rightInteractorObject;
    
    public void InstantiateObject()
    {
            // Instantiating an object
            GameObject newSeed = Instantiate(seedPrefab);
            // Access to the GrabInteractable component
            grabSeedInteractable = newSeed.GetComponent<XRGrabInteractable>();
            // Forces to release previously held interactable and selecting instantiated interactable object
            interactionManager.SelectExit(leftInteractorObject as IXRSelectInteractor, grabPouchInteractable as IXRSelectInteractable);
            interactionManager.SelectEnter(leftInteractorObject as IXRSelectInteractor, grabSeedInteractable as IXRSelectInteractable);
    }

}
