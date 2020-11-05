using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRGrabInteractable))]
public class ComplexCube : MonoBehaviour
{
    XRGrabInteractable m_GrabInteractable;
    MeshRenderer m_MeshRenderer;
    
    static Color s_UnityMagenta = new Color(0.929f, 0.094f, 0.278f);
    static Color s_UnityCyan = new Color(0.019f, 0.733f, 0.827f);

    bool m_Held;

    protected void OnEnable()
    {
        m_GrabInteractable = GetComponent<XRGrabInteractable>();
        m_MeshRenderer = GetComponent<MeshRenderer>();
        
        m_GrabInteractable.onFirstHoverEntered.AddListener(OnFirstHoverEntered);
        m_GrabInteractable.onLastHoverExited.AddListener(OnLastHoverExited);
        m_GrabInteractable.onSelectEntered.AddListener(OnSelectEntered);
        m_GrabInteractable.onSelectExited.AddListener(OnSelectExited);
    }

    
    protected void OnDisable()
    {
        m_GrabInteractable.onFirstHoverEntered.RemoveListener(OnFirstHoverEntered);
        m_GrabInteractable.onLastHoverExited.RemoveListener(OnLastHoverExited);
        m_GrabInteractable.onSelectEntered.RemoveListener(OnSelectEntered);
        m_GrabInteractable.onSelectExited.RemoveListener(OnSelectExited);
    }

    protected virtual void OnSelectEntered(XRBaseInteractor interactor)
    {
        m_MeshRenderer.material.color = s_UnityCyan;
        m_Held = true;
    }

    protected virtual void OnSelectExited(XRBaseInteractor interactor)
    {
        m_MeshRenderer.material.color = Color.white;
        m_Held = false;
    }

    protected virtual void OnLastHoverExited(XRBaseInteractor interactor)
    {
        if (!m_Held)
        {
            m_MeshRenderer.material.color = Color.white;
        }
    }

    protected virtual void OnFirstHoverEntered(XRBaseInteractor interactor)
    {
        if (!m_Held)
        {
            m_MeshRenderer.material.color = s_UnityMagenta;
        }
    }
}
