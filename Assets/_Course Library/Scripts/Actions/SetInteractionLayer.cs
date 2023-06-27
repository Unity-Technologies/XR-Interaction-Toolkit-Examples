using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Set the interaction layer of an interactor
/// </summary>
public class SetInteractionLayer : MonoBehaviour
{
    [Tooltip("The layer that's switched to")]
    public InteractionLayerMask targetLayer = 0;

    private XRBaseInteractor interactor = null;
    private InteractionLayerMask originalLayer = 0;

    private void Awake()
    {
        interactor = GetComponent<XRBaseInteractor>();
        originalLayer = interactor.interactionLayers;
    }

    public void SetTargetLayer()
    {
        interactor.interactionLayers = targetLayer;
    }

    public void SetOriginalLayer()
    {
        interactor.interactionLayers = originalLayer;
    }

    public void ToggleTargetLayer(bool value)
    {
        if (value)
        {
            SetTargetLayer();
        }
        else
        {
            SetOriginalLayer();
        }
    }
}
