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
        
        m_GrabInteractable.firstHoverEntered.AddListener(OnFirstHoverEntered);
        m_GrabInteractable.lastHoverExited.AddListener(OnLastHoverExited);
        m_GrabInteractable.selectEntered.AddListener(OnSelectEntered);
        m_GrabInteractable.selectExited.AddListener(OnSelectExited);
    }

    
    protected void OnDisable()
    {
        m_GrabInteractable.firstHoverEntered.RemoveListener(OnFirstHoverEntered);
        m_GrabInteractable.lastHoverExited.RemoveListener(OnLastHoverExited);
        m_GrabInteractable.selectEntered.RemoveListener(OnSelectEntered);
        m_GrabInteractable.selectExited.RemoveListener(OnSelectExited);
    }

    protected virtual void OnSelectEntered(SelectEnterEventArgs args)
    {
        m_MeshRenderer.material.color = s_UnityCyan;
        m_Held = true;
    }

    protected virtual void OnSelectExited(SelectExitEventArgs args)
    {
        m_MeshRenderer.material.color = Color.white;
        m_Held = false;
    }

    protected virtual void OnLastHoverExited(HoverExitEventArgs args)
    {
        if (!m_Held)
        {
            m_MeshRenderer.material.color = Color.white;
        }
    }

    protected virtual void OnFirstHoverEntered(HoverEnterEventArgs args)
    {
        if (!m_Held)
        {
            m_MeshRenderer.material.color = s_UnityMagenta;
        }
    }
}
